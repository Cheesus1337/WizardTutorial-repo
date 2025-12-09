using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using static CardEnums;

public enum GameState
{
    Setup,
    Bidding,        // Vorhersage
    Playing,        // Ausspielen
    TrickCompleted, // Stich beendet (optionaler Wartezustand)
    Scoring,        // Punktevergabe (Rundenende)
    GameOver        // Spielende
}

// Hilfs-Container für die Auswertung
public struct PlayedCard
{
    public ulong playerId;
    public CardData cardData;

    public PlayedCard(ulong player, CardData card)
    {
        playerId = player;
        cardData = card;
    }
}

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    [SerializeField] private int currentRound = 1;
    [Header("References")]
    [SerializeField] private GameObject cardPrefab;

    // --- Status und Daten ---
    public NetworkVariable<GameState> currentGameState = new NetworkVariable<GameState>(GameState.Setup);

    // Die synchronisierte Liste aller Spielerdaten
    public NetworkList<WizardPlayerData> playerDataList;

    // Speichert, an welcher Stelle im Array "playerIds" wir gerade sind
    public NetworkVariable<int> activePlayerIndex = new NetworkVariable<int>(0);

    private List<ulong> playerIds = new List<ulong>();

    // Logik-Speicher
    private List<PlayedCard> currentTrickCards = new List<PlayedCard>();
    private CardColor currentTrumpColor;
    private bool isTrumpActive = false;
    private int tricksPlayedInRound = 0;

    // (tempTrickWinnerId wird aktuell nicht benötigt, da wir sofort weitermachen, 
    // aber wir lassen es drin, falls wir später Pausen einbauen wollen)
    private ulong tempTrickWinnerId;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this);
        else Instance = this;

        playerDataList = new NetworkList<WizardPlayerData>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

            if (NetworkManager.Singleton.ConnectedClientsIds.Count > 0)
            {
                foreach (ulong uid in NetworkManager.Singleton.ConnectedClientsIds)
                    OnClientConnected(uid);
            }
            Debug.Log("GameManager gestartet (Server Mode)");
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (IsServer)
        {
            if (!playerIds.Contains(clientId))
            {
                playerIds.Add(clientId);

                WizardPlayerData newPlayer = new WizardPlayerData
                {
                    clientId = clientId,
                    playerName = $"Spieler {clientId}",
                    score = 0,
                    currentBid = 0,
                    tricksTaken = 0,
                    hasBidded = false
                };
                playerDataList.Add(newPlayer);
            }
        }
    }

    public void StartGame()
    {
        if (!IsServer) return;
        currentRound = 1;
        StartRound();
    }

    private void StartRound()
    {
        tricksPlayedInRound = 0; // Reset für neue Runde
        Debug.Log($"Starte Runde {currentRound}.");

        // 1. Status auf "Bidding" setzen
        currentGameState.Value = GameState.Bidding;

        // 2. Ansagen zurücksetzen
        for (int i = 0; i < playerDataList.Count; i++)
        {
            var data = playerDataList[i];
            data.hasBidded = false;
            data.currentBid = 0;
            data.tricksTaken = 0;
            playerDataList[i] = data;
        }

        // 3. Karten verteilen & Trumpf
        List<CardData> deck = DeckBuilder.GenerateStandardDeck();
        DeckBuilder.ShuffleDeck(deck);

        foreach (ulong clientId in playerIds)
        {
            List<CardData> handCards = new List<CardData>();
            for (int i = 0; i < currentRound; i++)
            {
                if (deck.Count > 0) { handCards.Add(deck[0]); deck.RemoveAt(0); }
            }

            int[] colors = new int[handCards.Count];
            int[] values = new int[handCards.Count];
            for (int k = 0; k < handCards.Count; k++) { colors[k] = (int)handCards[k].color; values[k] = (int)handCards[k].value; }

            ClientRpcParams clientRpcParams = new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { clientId } } };
            ReceiveHandCardsClientRpc(colors, values, clientRpcParams);
        }

        if (deck.Count > 0)
        {
            CardData trumpCard = deck[0];
            currentTrumpColor = trumpCard.color;

            if (trumpCard.value == CardValue.Jester)
            {
                isTrumpActive = false;
                Debug.Log("Narr als Trumpf -> Kein Trumpf in dieser Runde.");
            }
            else
            {
                isTrumpActive = true;
                Debug.Log($"Trumpf ist: {currentTrumpColor}");
            }
            UpdateTrumpCardClientRpc(trumpCard.color, trumpCard.value);
        }
        else
        {
            isTrumpActive = false;
            Debug.Log("Keine Karten mehr -> Kein Trumpf.");
            UpdateTrumpCardClientRpc(CardColor.Red, (CardValue)99);
        }
    }

    // --- RPCs: Bidding ---

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SubmitBidServerRpc(int bid, RpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        Debug.Log($"Client {senderId} sagt {bid} Stiche an.");

        for (int i = 0; i < playerDataList.Count; i++)
        {
            if (playerDataList[i].clientId == senderId)
            {
                var data = playerDataList[i];
                data.currentBid = bid;
                data.hasBidded = true;
                playerDataList[i] = data;
                break;
            }
        }
        CheckAllBidsReceived();
    }

    private void CheckAllBidsReceived()
    {
        foreach (var p in playerDataList)
        {
            if (!p.hasBidded) return;
        }

        Debug.Log("Alle Gebote da! Phase wechselt zu Playing.");
        currentGameState.Value = GameState.Playing;

        if (IsServer)
        {
            // Startspieler festlegen (einfachheitshalber rotierend nach Runde)
            activePlayerIndex.Value = (currentRound - 1) % playerIds.Count;
            Debug.Log($"Spieler {playerIds[activePlayerIndex.Value]} darf beginnen.");
        }
    }

    // --- RPCs: Playing ---

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void PlayCardServerRpc(int colorInt, int valueInt, RpcParams rpcParams = default)
    {
        if (currentGameState.Value != GameState.Playing) return;

        ulong senderId = rpcParams.Receive.SenderClientId;
        ulong activeId = playerIds[activePlayerIndex.Value];

        if (senderId != activeId)
        {
            Debug.LogWarning($"Spieler {senderId} wollte legen, ist aber nicht dran. Dran ist: {activeId}");
            return;
        }

        // Karte speichern
        CardData playedCard = new CardData((CardColor)colorInt, (CardValue)valueInt);
        currentTrickCards.Add(new PlayedCard(senderId, playedCard));

        Debug.Log($"Spieler {senderId} spielt: {playedCard}");

        // Allen Clients anzeigen
        PlayCardClientRpc(colorInt, valueInt, senderId);

        // Prüfen ob Stich voll ist
        if (currentTrickCards.Count == playerIds.Count)
        {
            EvaluateTrick();
        }
        else
        {
            activePlayerIndex.Value = (activePlayerIndex.Value + 1) % playerIds.Count;
        }
    }

    private void EvaluateTrick()
    {
        Debug.Log("Werte Stich aus...");
        ulong winnerId = 0;
        bool winnerFound = false;

        // 1. Zauberer
        foreach (var entry in currentTrickCards)
        {
            if (entry.cardData.value == CardValue.Wizard)
            {
                winnerId = entry.playerId;
                winnerFound = true;
                break;
            }
        }

        // 2. Trumpf
        if (!winnerFound && isTrumpActive)
        {
            int highestValue = -1;
            foreach (var entry in currentTrickCards)
            {
                if (entry.cardData.color == currentTrumpColor &&
                    entry.cardData.value != CardValue.Jester &&
                    entry.cardData.value != CardValue.Wizard)
                {
                    int val = (int)entry.cardData.value;
                    if (val > highestValue)
                    {
                        highestValue = val;
                        winnerId = entry.playerId;
                        winnerFound = true;
                    }
                }
            }
        }

        // 3. Bedienfarbe
        if (!winnerFound)
        {
            CardColor leadColor = currentTrickCards[0].cardData.color;
            bool leadColorFound = false;

            // Suche erste Nicht-Narr Karte für Farbe
            foreach (var entry in currentTrickCards)
            {
                if (entry.cardData.value != CardValue.Jester)
                {
                    leadColor = entry.cardData.color;
                    leadColorFound = true;
                    break;
                }
            }

            if (leadColorFound)
            {
                int highestValue = -1;
                foreach (var entry in currentTrickCards)
                {
                    if (entry.cardData.color == leadColor &&
                        entry.cardData.value != CardValue.Jester &&
                        entry.cardData.value != CardValue.Wizard)
                    {
                        int val = (int)entry.cardData.value;
                        if (val > highestValue)
                        {
                            highestValue = val;
                            winnerId = entry.playerId;
                            winnerFound = true;
                        }
                    }
                }
            }
            else
            {
                // Nur Narren -> Erster Spieler gewinnt
                winnerId = currentTrickCards[0].playerId;
            }
        }

        ProcessTrickResult(winnerId);
    }

    private void ProcessTrickResult(ulong winnerId)
    {
        // 1. Daten Update (Stich zählen)
        for (int i = 0; i < playerDataList.Count; i++)
        {
            if (playerDataList[i].clientId == winnerId)
            {
                var data = playerDataList[i];
                data.tricksTaken++;
                playerDataList[i] = data; // Scoreboard Update
                break;
            }
        }

        tricksPlayedInRound++;

        // 2. Entscheidung: Runde vorbei oder weiter?
        if (tricksPlayedInRound == currentRound)
        {
            // --- RUNDENENDE ---
            Debug.Log("Runde beendet! Berechne Punkte.");
            // Hier räumen wir NICHT ab, damit man das Ergebnis sieht!
            CalculateScores();
        }
        else
        {
            // --- NÄCHSTER STICH ---
            // Tisch abräumen (automatisch weiter)
            EndTrickClientRpc(winnerId);

            // Nächster Spieler ist der Gewinner
            int winnerIndex = playerIds.IndexOf(winnerId);
            activePlayerIndex.Value = winnerIndex;

            // Liste leeren
            currentTrickCards.Clear();
        }
    }

    private void CalculateScores()
    {
        // Punkte berechnen
        for (int i = 0; i < playerDataList.Count; i++)
        {
            var data = playerDataList[i];
            int points = 0;
            int diff = Mathf.Abs(data.currentBid - data.tricksTaken);

            if (diff == 0) points = 20 + (10 * data.tricksTaken);
            else points = -(10 * diff);

            data.score += points;
            playerDataList[i] = data;
        }

        // Status auf Scoring setzen (Scoreboard wird bunt)
        currentGameState.Value = GameState.Scoring;

        currentTrickCards.Clear();

        // Button "Nächste Runde" beim Host anzeigen
        ShowRoundEndButtonClientRpc();
    }

    // Dieser RPC wird vom Button geklickt
    [Rpc(SendTo.Server)]
    public void StartNextRoundServerRpc()
    {
        if (currentGameState.Value != GameState.Scoring) return;

        // Jetzt Tisch aufräumen
        ClearTableClientRpc();

        currentRound++;
        // TODO: Max Rounds checken
        StartRound();
    }

    // --- Client RPCs ---

    [ClientRpc]
    private void EndTrickClientRpc(ulong winnerId)
    {
        // Wird NUR mitten in der Runde aufgerufen
        if (GameplayMenu.Instance != null)
        {
            GameplayMenu.Instance.ClearTable();
        }
    }

    [ClientRpc]
    private void ShowRoundEndButtonClientRpc()
    {
        if (IsServer && GameplayMenu.Instance != null)
        {
            GameplayMenu.Instance.ShowNextStepButton(true);
        }
    }

    [ClientRpc]
    private void ClearTableClientRpc()
    {
        // Wird beim Start der neuen Runde aufgerufen
        if (GameplayMenu.Instance != null)
        {
            GameplayMenu.Instance.ClearTable();
            GameplayMenu.Instance.ShowNextStepButton(false); // Button ausblenden
        }
    }

    [ClientRpc]
    private void ReceiveHandCardsClientRpc(int[] colors, int[] values, ClientRpcParams clientRpcParams = default)
    {
        if (GameplayMenu.Instance == null) return;
        GameplayMenu.Instance.ClearHand();
        for (int i = 0; i < colors.Length; i++)
        {
            CardData data = new CardData((CardEnums.CardColor)colors[i], (CardEnums.CardValue)values[i]);
            if (cardPrefab != null)
            {
                GameObject newCard = Instantiate(cardPrefab, GameplayMenu.Instance.handContainer);
                CardController controller = newCard.GetComponent<CardController>();
                if (controller != null) controller.Initialize(data);
            }
        }
    }

    [ClientRpc]
    private void PlayCardClientRpc(int color, int value, ulong playerId)
    {
        CardData data = new CardData((CardEnums.CardColor)color, (CardEnums.CardValue)value);

        // 1. Auf den Tisch legen
        if (GameplayMenu.Instance != null)
        {
            GameplayMenu.Instance.PlaceCardOnTable(data, cardPrefab, playerId);
        }

        // 2. Aus eigener Hand entfernen
        if (playerId == NetworkManager.Singleton.LocalClientId)
        {
            if (GameplayMenu.Instance != null && GameplayMenu.Instance.handContainer != null)
            {
                foreach (Transform child in GameplayMenu.Instance.handContainer)
                {
                    CardController cc = child.GetComponent<CardController>();
                    if (cc != null && cc.CardDataEquals(data))
                    {
                        Destroy(child.gameObject);
                        break;
                    }
                }
            }
        }
    }

    [ClientRpc]
    private void UpdateTrumpCardClientRpc(CardEnums.CardColor color, CardEnums.CardValue value)
    {
        if ((int)value == 99) return; // Kein Trumpf

        CardData trumpData = new CardData(color, value);
        if (GameplayMenu.Instance != null)
        {
            GameplayMenu.Instance.ShowTrumpCard(trumpData, cardPrefab);
        }
    }
}