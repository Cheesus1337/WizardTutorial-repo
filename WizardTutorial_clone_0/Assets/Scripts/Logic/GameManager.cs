using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using static CardEnums;

public enum GameState
{
    Setup,
    Bidding, // Vorhersage
    Playing,  // Ausspielen
    Scoring,   // Punktevergabe
    GameOver, // Spielende

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

    // --- NEU: Status und Daten ---
    public NetworkVariable<GameState> currentGameState = new NetworkVariable<GameState>(GameState.Setup);

    // Die synchronisierte Liste aller Spielerdaten
    public NetworkList<WizardPlayerData> playerDataList;

    // Speichert, an welcher Stelle im Array "playerIds" wir gerade sind (0 bis Anzahl Spieler)
    public NetworkVariable<int> activePlayerIndex = new NetworkVariable<int>(0);

    private List<ulong> playerIds = new List<ulong>();

    // Logik-Speicher
    private List<PlayedCard> currentTrickCards = new List<PlayedCard>();
    private CardColor currentTrumpColor;
    private bool isTrumpActive = false; // Gibt es überhaupt einen Trumpf? (Nein in letzter Runde oder bei Zauberer-Trumpf)
    private int tricksPlayedInRound = 0;


    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this);
        else Instance = this;

        // Liste initialisieren
        playerDataList = new NetworkList<WizardPlayerData>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

            // Host registrieren
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

                // Neuen Spieler zur Daten-Liste hinzufügen
                WizardPlayerData newPlayer = new WizardPlayerData
                {
                    clientId = clientId,
                    playerName = $"Spieler {clientId}", // Vorerst simpler Name
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
        tricksPlayedInRound = 0; // Reset
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
            playerDataList[i] = data; // Zurückschreiben triggert Update
        }

        // 3. Karten verteilen & Trumpf (Dein bestehender Code)
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

            // Speichern für die Logik!
            currentTrumpColor = trumpCard.color;

            // Sonderregel: Wenn Zauberer aufgedeckt wird -> Dealer wählt Trumpf (lassen wir für Tutorial simpel: Farbe des Zauberers zählt)
            // Sonderregel: Wenn Narr aufgedeckt wird -> Kein Trumpf
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
            UpdateTrumpCardClientRpc(CardColor.Red, (CardValue)99); // 99 = Kein Trumpf
        }
    }

   
    // Ein Client sendet seine Ansage an den Server
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SubmitBidServerRpc(int bid, RpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        Debug.Log($"Client {senderId} sagt {bid} Stiche an.");
         
        // Wir suchen den Spieler in der Liste und updaten ihn
        for (int i = 0; i < playerDataList.Count; i++)
        {
            if (playerDataList[i].clientId == senderId)
            {
                var data = playerDataList[i];
                data.currentBid = bid;
                data.hasBidded = true;
                playerDataList[i] = data; // Update schreiben
                break;
            }
        }

        // Check: Haben alle geboten?
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

        // NEU: Startspieler festlegen (der links vom Dealer)
        // Einfachheitshalber fangen wir aktuell immer bei Index 0 an. 
        // Später machen wir das dynamisch basierend auf der Rundennummer.
        if (IsServer)
        {
            activePlayerIndex.Value = (currentRound - 1) % playerIds.Count;
            Debug.Log($"Spieler {playerIds[activePlayerIndex.Value]} darf beginnen.");
        }
    }

    // --- NEU: Karte ausspielen Logik ---

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void PlayCardServerRpc(int colorInt, int valueInt, RpcParams rpcParams = default)
    {
        // 1. Validierung: Ist das Spiel überhaupt im Gange?
        if (currentGameState.Value != GameState.Playing) return;

        ulong senderId = rpcParams.Receive.SenderClientId;

        // 2. Validierung: Ist der Spieler dran?
        ulong activeId = playerIds[activePlayerIndex.Value];
        if (senderId != activeId)
        {
            Debug.LogWarning($"Spieler {senderId} wollte legen, ist aber nicht dran. Dran ist: {activeId}");
            return;
        }

        // 3. Validierung: Besitzt er die Karte? (Sicherheit)
        // Das überspringen wir für dieses Tutorial, wir vertrauen dem Client vorerst.
        // Auch die "Bedienpflicht" (Regeln) lassen wir kurz weg, damit wir testen können.

        Debug.Log($"Spieler {senderId} spielt Karte: {(CardEnums.CardColor)colorInt} {(CardEnums.CardValue)valueInt}");

        // 1. Speichern in der Liste
        CardData playedCard = new CardData((CardColor)colorInt, (CardValue)valueInt);
        currentTrickCards.Add(new PlayedCard(senderId, playedCard));

        Debug.Log($"Spieler {senderId} spielt: {playedCard}");

        // 2. Visualisieren (Alle Clients)
        PlayCardClientRpc(colorInt, valueInt, senderId);

        // 3. Nächster Spieler oder Auswerten?
        if (currentTrickCards.Count == playerIds.Count)
        {
            // Stich ist voll -> Auswerten!
            // Wir warten kurz (Coroutine), damit man die letzte Karte noch sieht, bevor abgeräumt wird?
            // Fürs Erste machen wir es direkt.
            EvaluateTrick();
        }
        else
        {
            // Nächster Spieler ist dran
            activePlayerIndex.Value = (activePlayerIndex.Value + 1) % playerIds.Count;
        }
    }
    private void EvaluateTrick()
    {
        Debug.Log("Werte Stich aus...");
        ulong winnerId = 0; // Fallback
        bool winnerFound = false;

        // --- PRIO 1: ZAUBERER ---
        foreach (var entry in currentTrickCards)
        {
            if (entry.cardData.value == CardValue.Wizard)
            {
                winnerId = entry.playerId;
                winnerFound = true;
                Debug.Log($"Zauberer gefunden! Gewinner: {winnerId}");
                break; // Der ERSTE Zauberer gewinnt sofort
            }
        }

        // --- PRIO 2: TRUMPF ---
        if (!winnerFound && isTrumpActive)
        {
            int highestValue = -1;

            foreach (var entry in currentTrickCards)
            {
                // Ist es ein Trumpf? (Und kein Narr/Zauberer, obwohl Zauberer oben schon weg sind)
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
            if (winnerFound) Debug.Log($"Höchster Trumpf gewinnt: {winnerId}");
        }

        // --- PRIO 3: BEDIENFARBE ---
        if (!winnerFound)
        {
            // Bedienfarbe ermitteln (Erste Karte, die kein Narr ist)
            CardColor leadColor = currentTrickCards[0].cardData.color;
            bool leadColorFound = false;

            // Suche die erste Nicht-Narr Karte für die Farbe
            foreach (var entry in currentTrickCards)
            {
                if (entry.cardData.value != CardValue.Jester)
                {
                    leadColor = entry.cardData.color;
                    leadColorFound = true;
                    break;
                }
            }

            if (leadColorFound) // Normalfall
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
                Debug.Log($"Höchste Farbe ({leadColor}) gewinnt: {winnerId}");
            }
            else
            {
                // Sonderfall PRIO 4: NUR Narren im Stich
                // Der erste Spieler gewinnt
                winnerId = currentTrickCards[0].playerId;
                Debug.Log("Nur Narren! Erster Spieler gewinnt.");
            }
        }

        // --- ENDE DES STICHS ---
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
                playerDataList[i] = data;
                break;
            }
        }

        // 2. Tisch aufräumen & Gewinner beginnt
        EndTrickClientRpc(winnerId);

        // 3. Zähler erhöhen
        tricksPlayedInRound++;

        // 4. CHECK: Ist die Runde vorbei?
        if (tricksPlayedInRound == currentRound)
        {
            Debug.Log("Runde beendet! Berechne Punkte...");
            CalculateScores();
        }
        else
        {
            // Runde geht weiter -> Gewinner spielt aus
            int winnerIndex = playerIds.IndexOf(winnerId);
            activePlayerIndex.Value = winnerIndex;
            currentTrickCards.Clear();
        }
    }

    private void CalculateScores()
    {
        // Punkte berechnen nach Wizard-Regeln
        for (int i = 0; i < playerDataList.Count; i++)
        {
            var data = playerDataList[i];
            int points = 0;
            int diff = Mathf.Abs(data.currentBid - data.tricksTaken);

            if (diff == 0)
            {
                // Richtig getippt: 20 Punkte + 10 pro Stich
                points = 20 + (10 * data.tricksTaken);
            }
            else
            {
                // Falsch getippt: -10 Punkte pro Stich Abweichung
                points = -(10 * diff);
            }

            data.score += points; // Auf Gesamtpunkte addieren
            playerDataList[i] = data; // Schreiben triggert Sync

            Debug.Log($"Spieler {data.clientId}: {points} Punkte diese Runde. Total: {data.score}");
        }

        // Status ändern -> Das signalisiert dem UI "Runde vorbei, Farben zeigen!"
        currentGameState.Value = GameState.Scoring;

        // Liste leeren (wichtig, sonst bleiben Kartenreste für die nächste Runde im Speicher)
        currentTrickCards.Clear();

        // TODO: Hier könnte man später einen Timer starten, um automatisch die nächste Runde zu beginnen
        // Z.B. Invoke("StartNextRound", 5f);
    }

    // Eine Hilfsmethode, um manuell oder per Timer weiterzumachen
    public void StartNextRound()
    {
        if (!IsServer) return;

        currentRound++;
        // TODO: Check Max Rounds (60 / Spieleranzahl)
        StartRound();
    }

    // --- Deine existierenden ClientRPCs ---

    [ClientRpc]
    private void EndTrickClientRpc(ulong winnerId)
    {
        Debug.Log($"Stich geht an Spieler {winnerId}!");

        // Tisch leeren
        if (GameplayMenu.Instance != null)
        {
            // Hier wäre eine kleine Verzögerung cool (Coroutine), damit man sieht wer gewonnen hat
            // Für jetzt: Hartes Löschen
            GameplayMenu.Instance.ClearTable();
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

        // 1. Auf den Tisch legen (Visuell)
        if (GameplayMenu.Instance != null)
        {
            GameplayMenu.Instance.PlaceCardOnTable(data, cardPrefab, playerId);
        }

        // 2. WICHTIG: Wenn ICH der Spieler war, muss die Karte aus meiner Hand verschwinden!
        if (playerId == NetworkManager.Singleton.LocalClientId)
        {
            // Wir suchen die Karte in unserer Hand und zerstören sie
            if (GameplayMenu.Instance != null && GameplayMenu.Instance.handContainer != null)
            {
                foreach (Transform child in GameplayMenu.Instance.handContainer)
                {
                    CardController cc = child.GetComponent<CardController>();
                    if (cc != null && cc.CardDataEquals(data)) // Wir brauchen eine Vergleichsmethode im Controller!
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
        if ((int)value == 99)
        {
            // Kein Trumpf Logik hier (optional)
            return;
        }

        CardData trumpData = new CardData(color, value);
        if (GameplayMenu.Instance != null)
        {
            GameplayMenu.Instance.ShowTrumpCard(trumpData, cardPrefab);
        }
    }
}