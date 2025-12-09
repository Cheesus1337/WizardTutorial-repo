using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static CardEnums;

public class GameManager : NetworkBehaviour
{
    // Singleton-Pattern: Damit wir von überall einfach "GameManager.Instance" aufrufen können
    public static GameManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private GameObject cardPrefab; // Hier ziehen wir das Karten-Prefab rein

    [Header("Game Settings")]
    [SerializeField] private int currentRound = 1;

    // Speichert, welche Client-ID zu welchem Spieler gehört (wichtig für Zug-Reihenfolge)
    private List<ulong> playerIds = new List<ulong>();

    private void Awake()
    {
        // Sicherstellen, dass es nur einen GameManager gibt
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    // Wird aufgerufen, sobald das Objekt im Netzwerk gespawnt ist
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // 1. Auf zukünftige Verbindungen hören
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

            // 2. WICHTIG: Bereits verbundene Clients (wie den Host selbst) nachtragen!
            // Wenn wir das nicht tun, fehlt der Host in der Liste, weil er schon vor dem Spawnen da war.
            if (NetworkManager.Singleton.ConnectedClientsIds.Count > 0)
            {
                foreach (ulong uid in NetworkManager.Singleton.ConnectedClientsIds)
                {
                    OnClientConnected(uid);
                }
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
                Debug.Log($"Spieler registriert: {clientId}. Total: {playerIds.Count}");
            }
        }
    }

    // Diese Methode starten wir manuell (z.B. über einen Button "Spiel starten" in der Lobby)
    public void StartGame()
    {
        if (!IsServer) return; // Nur der Server darf das Spiel starten!

        Debug.Log("Spiel wird gestartet...");
        currentRound = 1;

        // Runde beginnen
        StartRound();
    }

    private void StartRound()
    {
        Debug.Log($"Starte Runde {currentRound}. Verteile {currentRound} Karten...");

        // 1. Deck erstellen
        List<CardData> deck = DeckBuilder.GenerateStandardDeck();

        // 2. Deck mischen
        DeckBuilder.ShuffleDeck(deck);

        // 3. Karten an Spieler verteilen
        foreach (ulong clientId in playerIds)
        {
            // Wir nehmen 'currentRound' viele Karten vom Deck
            List<CardData> handCards = new List<CardData>();
            for (int i = 0; i < currentRound; i++)
            {
                if (deck.Count > 0)
                {
                    handCards.Add(deck[0]); // Oberste Karte nehmen
                    deck.RemoveAt(0);       // Aus dem Deck löschen
                }
            }

            // JETZT kommt der Netzwerk-Teil: Wir schicken die Daten an den Client
            // Da wir komplexe Strukturen (List<CardData>) schwerer schicken können,
            // zerlegen wir sie in einfache Arrays aus Integers.
            int[] colors = new int[handCards.Count];
            int[] values = new int[handCards.Count];

            for (int k = 0; k < handCards.Count; k++)
            {
                colors[k] = (int)handCards[k].color;
                values[k] = (int)handCards[k].value;
            }

            // Sende die Karten NUR an diesen einen Client (ClientRpcParams)
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { clientId }
                }
            };

            ReceiveHandCardsClientRpc(colors, values, clientRpcParams);
            Debug.Log($"Habe {handCards.Count} Karten an Client {clientId} gesendet.");
        }

        if (deck.Count > 0)
        {
            CardData trumpCard = deck[0];
            deck.RemoveAt(0);

            Debug.Log($"Trumpfkarte gezogen: {trumpCard.color} {trumpCard.value}");

            // An ALLE senden (kein ClientRpcParams nötig = Broadcast)
            UpdateTrumpCardClientRpc(trumpCard.color, trumpCard.value);
        }
        else
        {
            Debug.Log("Keine Karten mehr im Deck -> Kein Trumpf in dieser Runde.");
            // Optional: Ein "Leeres" Signal senden, um alte Trümpfe zu löschen
        }
    }



    // --- RPCs: Kommunikation Server -> Client ---

    [ClientRpc]
    private void UpdateTrumpCardClientRpc(CardColor color, CardValue value)
    {
        CardData trumpData = new CardData(color, value);

        if (GameplayMenu.Instance != null)
        {
            // Wir nutzen das gleiche CardPrefab, das wir schon für die Hand haben
            GameplayMenu.Instance.ShowTrumpCard(trumpData, cardPrefab);
        }
    }


    [ClientRpc]
    private void ReceiveHandCardsClientRpc(int[] colors, int[] values, ClientRpcParams clientRpcParams = default)
    {
        Debug.Log($"Client empfängt {colors.Length} Karten.");

        // 1. Prüfen, ob wir das Menü finden
        if (GameplayMenu.Instance == null)
        {
            Debug.LogError("GameplayMenu Instance nicht gefunden! Kann Karten nicht anzeigen.");
            return;
        }

        // 2. Hand leeren (Aufräumen vor neuer Runde)
        GameplayMenu.Instance.ClearHand();

        // 3. Neue Karten erstellen
        for (int i = 0; i < colors.Length; i++)
        {
            // Daten aus den Integers rekonstruieren
            CardData data = new CardData((CardColor)colors[i], (CardValue)values[i]);

            if (cardPrefab != null)
            {
                // Karte als "Kind" des handContainer im Menu instanziieren
                GameObject newCard = Instantiate(cardPrefab, GameplayMenu.Instance.handContainer);

                // Controller holen und initialisieren
                CardController controller = newCard.GetComponent<CardController>();
                if (controller != null)
                {
                    controller.Initialize(data);
                }
            }
            else
            {
                Debug.LogError("CardPrefab ist im GameManager nicht zugewiesen!");
            }
        }
    }
}