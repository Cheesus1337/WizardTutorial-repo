using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Unity.Collections;

public enum GameState
{
    Setup,
    Bidding, // Vorhersage
    Playing,  // Ausspielen
    Scoring,   // Punktevergabe
    GameOver, // Spielende

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

    private List<ulong> playerIds = new List<ulong>();

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
            UpdateTrumpCardClientRpc(trumpCard.color, trumpCard.value);
        }
        else
        {
            // Sonderregel letzte Runde: Kein Trumpf
            UpdateTrumpCardClientRpc(CardEnums.CardColor.Red, (CardEnums.CardValue)99); // 99 als Code für "Kein Trumpf"
        }
    }

    // --- RPCs für Bidding ---

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
            if (!p.hasBidded) return; // Einer fehlt noch
        }

        // Alle haben geboten -> Spielphase startet!
        Debug.Log("Alle Gebote da! Phase wechselt zu Playing.");
        currentGameState.Value = GameState.Playing;
    }

    // --- Deine existierenden ClientRPCs ---
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