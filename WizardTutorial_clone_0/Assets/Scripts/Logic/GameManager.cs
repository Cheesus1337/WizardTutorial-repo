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

// --- WICHTIG: Dieses Struct muss hier PUBLIC stehen, damit GameplayMenu es sieht ---
public struct PlayerResult : INetworkSerializable
{
    public FixedString64Bytes playerName;
    public int score;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref playerName);
        serializer.SerializeValue(ref score);
    }
}
// ----------------------------------------------------------------------------------

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
    [SerializeField] private int maxRounds = 3;

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

                if (!serverHandCards.ContainsKey(clientId))
                    serverHandCards.Add(clientId, new List<CardData>());
            }
        }
    }

    // Helper für UI
    public bool IsPlayerTurn(ulong clientId)
    {
        // Wir nutzen die synchronisierte NetworkList statt der lokalen playerIds Liste
        if (playerDataList == null || playerDataList.Count == 0) return false;

        int index = activePlayerIndex.Value;

        // Sicherheitscheck: Ist der Index gültig?
        if (index >= 0 && index < playerDataList.Count)
        {
            // Wir vergleichen die ID mit der ID im synchronisierten Datensatz
            return playerDataList[index].clientId == clientId;
        }
        return false;
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

        // --- Dealer Rotation ---
        if (IsServer)
        {
            // Wir nutzen playerDataList.Count statt playerIds.Count für Konsistenz
            int count = playerDataList.Count;
            if (count > 0)
            {
                int dealerIndex = (currentRound - 1) % count;
                int starterIndex = (dealerIndex + 1) % count;
                activePlayerIndex.Value = starterIndex;
                Debug.Log($"Runde {currentRound}: Starter Index ist {starterIndex}");
            }
        }

        // Karten verteilen
        List<CardData> deck = DeckBuilder.GenerateStandardDeck();
        DeckBuilder.ShuffleDeck(deck);

        foreach (ulong clientId in playerIds)
        {
            serverHandCards[clientId].Clear();
            List<CardData> handCards = new List<CardData>();
            for (int i = 0; i < currentRound; i++)
            {
                if (deck.Count > 0) { handCards.Add(deck[0]); deck.RemoveAt(0); }
            }
            serverHandCards[clientId].AddRange(handCards);

            int[] colors = new int[handCards.Count];
            int[] values = new int[handCards.Count];
            for (int k = 0; k < handCards.Count; k++) { colors[k] = (int)handCards[k].color; values[k] = (int)handCards[k].value; }

            ClientRpcParams clientRpcParams = new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { clientId } } };
            ReceiveHandCardsClientRpc(colors, values, clientRpcParams);
        }

        // Trumpf
        if (deck.Count > 0)
        {
            CardData trumpCard = deck[0];
            currentTrumpColor = trumpCard.color;
            isTrumpActive = (trumpCard.value != CardValue.Jester);
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

        if (senderId != playerIds[activePlayerIndex.Value]) return; // Nicht dran

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

        // Nächster Spieler darf bieten
        activePlayerIndex.Value = (activePlayerIndex.Value + 1) % playerIds.Count;

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
            // Starter beginnt auch das Ausspielen (links vom Dealer)
            int dealerIndex = (currentRound - 1) % playerIds.Count;
            int starterIndex = (dealerIndex + 1) % playerIds.Count;
            activePlayerIndex.Value = starterIndex;
        }
    }

    // --- Playing Logic ---

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void PlayCardServerRpc(int colorInt, int valueInt, RpcParams rpcParams = default)
    {
        if (currentGameState.Value != GameState.Playing) return;

        ulong senderId = rpcParams.Receive.SenderClientId;
        ulong activeId = playerIds[activePlayerIndex.Value];

        if (senderId != activeId) return;

        CardData cardToPlay = new CardData((CardColor)colorInt, (CardValue)valueInt);

        if (!IsValidMove(senderId, cardToPlay))
        {
            Debug.LogWarning($"Regelverstoß: {senderId}");
            return;
        }

        RemoveCardFromServerHand(senderId, cardToPlay);
        currentTrickCards.Add(new PlayedCard(senderId, cardToPlay));
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

    // --- Validierung & Auswertung ---
    private bool IsValidMove(ulong playerId, CardData cardToPlay)
    {
        if (!PlayerHasCard(playerId, cardToPlay)) return false;
        if (cardToPlay.value == CardValue.Wizard || cardToPlay.value == CardValue.Jester) return true;
        if (currentTrickCards.Count == 0) return true;

        CardColor leadColor = GetLeadColor();
        if (leadColor == CardColor.Red && IsLeadColorUndefined()) return true;

        if (PlayerHasColor(playerId, leadColor))
        {
            return cardToPlay.color == leadColor;
        }
        return true;
    }

    private CardColor GetLeadColor()
    {
        foreach (var played in currentTrickCards)
        {
            if (played.cardData.value != CardValue.Jester && played.cardData.value != CardValue.Wizard)
                return played.cardData.color;
            if (played.cardData.value == CardValue.Wizard) return CardColor.Red;
        }
        return CardColor.Red;
    }

    private bool IsLeadColorUndefined()
    {
        foreach (var played in currentTrickCards)
        {
            if (played.cardData.value == CardValue.Wizard) return true;
            if (played.cardData.value != CardValue.Jester) return false;
        }
        return true;
    }

    private bool PlayerHasCard(ulong playerId, CardData card)
    {
        if (!serverHandCards.ContainsKey(playerId)) return false;
        foreach (var c in serverHandCards[playerId])
            if (c.color == card.color && c.value == card.value) return true;
        return false;
    }

    private bool PlayerHasColor(ulong playerId, CardColor color)
    {
        if (!serverHandCards.ContainsKey(playerId)) return false;
        foreach (var c in serverHandCards[playerId])
            if (c.color == color && c.value != CardValue.Wizard && c.value != CardValue.Jester) return true;
        return false;
    }

    private void RemoveCardFromServerHand(ulong playerId, CardData card)
    {
        if (!serverHandCards.ContainsKey(playerId)) return;
        var hand = serverHandCards[playerId];
        for (int i = 0; i < hand.Count; i++)
        {
            if (hand[i].color == card.color && hand[i].value == card.value) { hand.RemoveAt(i); break; }
        }
    }

    private void EvaluateTrick()
    {
        ulong winnerId = 0;
        bool winnerFound = false;

        // 1. Zauberer
        foreach (var entry in currentTrickCards) { if (entry.cardData.value == CardValue.Wizard) { winnerId = entry.playerId; winnerFound = true; break; } }

        // 2. Trumpf
        if (!winnerFound && isTrumpActive)
        {
            int highestValue = -1;
            foreach (var entry in currentTrickCards)
            {
                if (entry.cardData.color == currentTrumpColor && entry.cardData.value != CardValue.Jester && entry.cardData.value != CardValue.Wizard)
                {
                    int val = (int)entry.cardData.value;
                    if (val > highestValue) { highestValue = val; winnerId = entry.playerId; winnerFound = true; }
                }
            }
        }

        // 3. Farbe
        if (!winnerFound)
        {
            CardColor leadColor = currentTrickCards[0].cardData.color;
            bool leadColorFound = false;
            foreach (var entry in currentTrickCards) { if (entry.cardData.value != CardValue.Jester) { leadColor = entry.cardData.color; leadColorFound = true; break; } }

            if (leadColorFound)
            {
                int highestValue = -1;
                foreach (var entry in currentTrickCards)
                {
                    if (entry.cardData.color == leadColor && entry.cardData.value != CardValue.Jester && entry.cardData.value != CardValue.Wizard)
                    {
                        int val = (int)entry.cardData.value;
                        if (val > highestValue) { highestValue = val; winnerId = entry.playerId; winnerFound = true; }
                    }
                }
            }
            else { winnerId = currentTrickCards[0].playerId; }
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

        if (currentRound >= maxRounds)
        {
            EndGame();
        }
        else
        {
            currentRound++;
            StartRound();
        }
    }

    // --- SPIELENDE LOGIK (NEU: Mit PlayerResult) ---
    private void EndGame()
    {
        Debug.Log("Spiel ist vorbei!");
        currentGameState.Value = GameState.GameOver;

        // Liste erstellen und sortieren für Podium
        List<PlayerResult> results = new List<PlayerResult>();
        foreach (var p in playerDataList)
        {
            PlayerResult res = new PlayerResult { playerName = p.playerName, score = p.score };
            results.Add(res);
        }
        results.Sort((a, b) => b.score.CompareTo(a.score)); // Sortieren nach Score absteigend

        ShowPodiumClientRpc(results.ToArray());
    }

    // --- CLIENT RPCs ---

    // NEU: Empfängt die Liste statt nur eine ID
    [ClientRpc]
    private void ShowPodiumClientRpc(PlayerResult[] results)
    {
        if (GameplayMenu.Instance != null)
        {
            // WICHTIG: Das Menü muss diese Methode haben!
            GameplayMenu.Instance.ShowPodium(results);
        }
    }

    [ClientRpc] private void EndTrickClientRpc(ulong winnerId) { if (GameplayMenu.Instance != null) GameplayMenu.Instance.ClearTable(); }
    [ClientRpc] private void ShowRoundEndButtonClientRpc() { if (IsServer && GameplayMenu.Instance != null) GameplayMenu.Instance.ShowNextStepButton(true); }
    [ClientRpc] private void ClearTableClientRpc() { if (GameplayMenu.Instance != null) { GameplayMenu.Instance.ClearTable(); GameplayMenu.Instance.ShowNextStepButton(false); } }

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
                    if (cc != null && cc.CardDataEquals(data)) { Destroy(child.gameObject); break; }
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