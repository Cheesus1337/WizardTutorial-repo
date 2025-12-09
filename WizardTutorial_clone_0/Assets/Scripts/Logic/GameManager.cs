using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using static CardEnums;

public enum GameState
{
    Setup,
    Bidding,
    Playing,
    TrickCompleted,
    Scoring,
    GameOver
}

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

    public NetworkVariable<GameState> currentGameState = new NetworkVariable<GameState>(GameState.Setup);
    public NetworkList<WizardPlayerData> playerDataList;
    public NetworkVariable<int> activePlayerIndex = new NetworkVariable<int>(0);

    private List<ulong> playerIds = new List<ulong>();

    // Logik-Speicher
    private List<PlayedCard> currentTrickCards = new List<PlayedCard>();
    private CardColor currentTrumpColor;
    private bool isTrumpActive = false;
    private int tricksPlayedInRound = 0;

    // --- NEU: Server-Gedächtnis für Handkarten (für Regel-Check) ---
    private Dictionary<ulong, List<CardData>> serverHandCards = new Dictionary<ulong, List<CardData>>();

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

                // Speicherplatz für Handkarten anlegen
                if (!serverHandCards.ContainsKey(clientId))
                    serverHandCards.Add(clientId, new List<CardData>());
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
        tricksPlayedInRound = 0;
        Debug.Log($"Starte Runde {currentRound}.");

        currentGameState.Value = GameState.Bidding;

        for (int i = 0; i < playerDataList.Count; i++)
        {
            var data = playerDataList[i];
            data.hasBidded = false;
            data.currentBid = 0;
            data.tricksTaken = 0;
            playerDataList[i] = data;
        }

        List<CardData> deck = DeckBuilder.GenerateStandardDeck();
        DeckBuilder.ShuffleDeck(deck);

        // --- HIER SPEICHERN WIR JETZT DIE KARTEN AUCH AUF DEM SERVER ---
        foreach (ulong clientId in playerIds)
        {
            serverHandCards[clientId].Clear(); // Alte Hand löschen

            List<CardData> handCards = new List<CardData>();
            for (int i = 0; i < currentRound; i++)
            {
                if (deck.Count > 0)
                {
                    CardData drawnCard = deck[0];
                    handCards.Add(drawnCard);
                    deck.RemoveAt(0);
                }
            }

            // Speichern für Validierung!
            serverHandCards[clientId].AddRange(handCards);

            // Senden an Client
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
            }
            else
            {
                isTrumpActive = true;
            }
            UpdateTrumpCardClientRpc(trumpCard.color, trumpCard.value);
        }
        else
        {
            isTrumpActive = false;
            UpdateTrumpCardClientRpc(CardColor.Red, (CardValue)99);
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SubmitBidServerRpc(int bid, RpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

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

        currentGameState.Value = GameState.Playing;

        if (IsServer)
        {
            activePlayerIndex.Value = (currentRound - 1) % playerIds.Count;
        }
    }

    // --- RPCs: Playing ---

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void PlayCardServerRpc(int colorInt, int valueInt, RpcParams rpcParams = default)
    {
        if (currentGameState.Value != GameState.Playing) return;

        ulong senderId = rpcParams.Receive.SenderClientId;
        ulong activeId = playerIds[activePlayerIndex.Value];

        if (senderId != activeId) return;

        CardData cardToPlay = new CardData((CardColor)colorInt, (CardValue)valueInt);

        // --- REGEL-CHECK ---
        if (!IsValidMove(senderId, cardToPlay))
        {
            Debug.LogWarning($"Spieler {senderId} wollte {cardToPlay} legen - REGELVERSTOẞ! (Bedienpflicht)");
            // Wir brechen einfach ab. Der Client bekommt keine Antwort -> Karte bleibt in der Hand.
            return;
        }

        // Karte aus Server-Gedächtnis entfernen
        RemoveCardFromServerHand(senderId, cardToPlay);

        currentTrickCards.Add(new PlayedCard(senderId, cardToPlay));
        Debug.Log($"Spieler {senderId} spielt legal: {cardToPlay}");

        PlayCardClientRpc(colorInt, valueInt, senderId);

        if (currentTrickCards.Count == playerIds.Count)
        {
            EvaluateTrick();
        }
        else
        {
            activePlayerIndex.Value = (activePlayerIndex.Value + 1) % playerIds.Count;
        }
    }

    // --- NEU: Die Regel-Polizei ---
    private bool IsValidMove(ulong playerId, CardData cardToPlay)
    {
        // 1. Karten-Besitz prüfen (Sicherheit)
        // Hat der Spieler diese Karte überhaupt auf der Hand?
        if (!PlayerHasCard(playerId, cardToPlay))
        {
            Debug.LogError($"Cheat-Versuch? Spieler {playerId} hat Karte {cardToPlay} nicht!");
            return false;
        }

        // 2. Zauberer und Narren sind IMMER erlaubt
        if (cardToPlay.value == CardValue.Wizard || cardToPlay.value == CardValue.Jester)
        {
            return true;
        }

        // 3. Ist es die erste Karte im Stich? -> Alles erlaubt
        if (currentTrickCards.Count == 0)
        {
            return true;
        }

        // 4. Bedienpflicht prüfen
        CardColor leadColor = GetLeadColor();

        // Wenn es keine Bedienfarbe gibt (z.B. nur Narren oder erster war Zauberer), ist alles erlaubt
        if (leadColor == CardColor.Red && IsLeadColorUndefined())
        {
            return true;
        }

        // Hat der Spieler die Bedienfarbe auf der Hand?
        if (PlayerHasColor(playerId, leadColor))
        {
            // JA: Er muss bedienen! (Außer er spielt Zauberer/Narr, siehe Punkt 2)
            if (cardToPlay.color == leadColor)
            {
                return true; // Er bedient brav
            }
            else
            {
                return false; // Er hat die Farbe, spielt aber was anderes -> VERBOTEN!
            }
        }
        else
        {
            // NEIN: Er hat die Farbe nicht -> Er darf alles spielen (abwerfen oder trumpfen)
            return true;
        }
    }

    // Hilfsmethode: Bestimmt die angespielte Farbe
    private CardColor GetLeadColor()
    {
        foreach (var played in currentTrickCards)
        {
            if (played.cardData.value != CardValue.Jester && played.cardData.value != CardValue.Wizard)
            {
                return played.cardData.color;
            }
            if (played.cardData.value == CardValue.Wizard) return CardColor.Red; // Zauberer -> Farbe egal (Dummy Return)
        }
        return CardColor.Red; // Fallback (nur Narren)
    }

    // Hilfsmethode: Prüft ob Farbe "definiert" ist (also ob eine Nicht-Narr/Zauberer Karte liegt)
    private bool IsLeadColorUndefined()
    {
        foreach (var played in currentTrickCards)
        {
            if (played.cardData.value == CardValue.Wizard) return true; // Zauberer eröffnet -> keine Farbe
            if (played.cardData.value != CardValue.Jester) return false; // Farbe gefunden!
        }
        return true; // Nur Narren oder leer
    }

    private bool PlayerHasCard(ulong playerId, CardData card)
    {
        if (!serverHandCards.ContainsKey(playerId)) return false;
        foreach (var c in serverHandCards[playerId])
        {
            if (c.color == card.color && c.value == card.value) return true;
        }
        return false;
    }

    private bool PlayerHasColor(ulong playerId, CardColor color)
    {
        if (!serverHandCards.ContainsKey(playerId)) return false;
        foreach (var c in serverHandCards[playerId])
        {
            // Zauberer und Narren zählen NICHT als Farbe
            if (c.color == color && c.value != CardValue.Wizard && c.value != CardValue.Jester)
                return true;
        }
        return false;
    }

    private void RemoveCardFromServerHand(ulong playerId, CardData card)
    {
        if (!serverHandCards.ContainsKey(playerId)) return;

        var hand = serverHandCards[playerId];
        for (int i = 0; i < hand.Count; i++)
        {
            if (hand[i].color == card.color && hand[i].value == card.value)
            {
                hand.RemoveAt(i);
                break;
            }
        }
    }
    // ----------------------------------------------------

    private void EvaluateTrick()
    {
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
                winnerId = currentTrickCards[0].playerId;
            }
        }

        ProcessTrickResult(winnerId);
    }

    private void ProcessTrickResult(ulong winnerId)
    {
        for (int i = 0; i < playerDataList.Count; i++)
        {
            if (playerDataList[i].clientId == winnerId)
            {
                var data = playerDataList[i];
                data.tricksTaken++;
                playerDataList[i] = data;
                break;
            }
        }

        tricksPlayedInRound++;

        if (tricksPlayedInRound == currentRound)
        {
            Debug.Log("Runde beendet! Berechne Punkte.");
            CalculateScores();
        }
        else
        {
            EndTrickClientRpc(winnerId);
            int winnerIndex = playerIds.IndexOf(winnerId);
            activePlayerIndex.Value = winnerIndex;
            currentTrickCards.Clear();
        }
    }

    private void CalculateScores()
    {
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

        currentGameState.Value = GameState.Scoring;
        currentTrickCards.Clear();
        ShowRoundEndButtonClientRpc();
    }

    [Rpc(SendTo.Server)]
    public void StartNextRoundServerRpc()
    {
        if (currentGameState.Value != GameState.Scoring) return;
        ClearTableClientRpc();
        currentRound++;
        StartRound();
    }

    [ClientRpc]
    private void EndTrickClientRpc(ulong winnerId)
    {
        if (GameplayMenu.Instance != null) GameplayMenu.Instance.ClearTable();
    }

    [ClientRpc]
    private void ShowRoundEndButtonClientRpc()
    {
        if (IsServer && GameplayMenu.Instance != null) GameplayMenu.Instance.ShowNextStepButton(true);
    }

    [ClientRpc]
    private void ClearTableClientRpc()
    {
        if (GameplayMenu.Instance != null)
        {
            GameplayMenu.Instance.ClearTable();
            GameplayMenu.Instance.ShowNextStepButton(false);
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
        if (GameplayMenu.Instance != null) GameplayMenu.Instance.PlaceCardOnTable(data, cardPrefab, playerId);

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
        if ((int)value == 99) return;
        CardData trumpData = new CardData(color, value);
        if (GameplayMenu.Instance != null) GameplayMenu.Instance.ShowTrumpCard(trumpData, cardPrefab);
    }
}