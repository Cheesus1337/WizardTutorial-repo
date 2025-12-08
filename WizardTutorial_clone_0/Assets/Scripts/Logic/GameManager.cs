using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static CardEnums;

public class GameManager : NetworkBehaviour
{
    // Singleton-Pattern: Damit wir von überall einfach "GameManager.Instance" aufrufen können
    public static GameManager Instance { get; private set; }

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
        }
    }

    // --- RPCs: Kommunikation Server -> Client ---

    [ClientRpc]
    private void ReceiveHandCardsClientRpc(int[] colors, int[] values, ClientRpcParams clientRpcParams = default)
    {
        // Dieser Code wird auf dem Client ausgeführt, der die Karten bekommt
        Debug.Log($"Ich (Client {NetworkManager.Singleton.LocalClientId}) habe {colors.Length} Karten bekommen!");

        // Hier müssen wir die Arrays wieder in echte CardData umwandeln
        List<CardData> myHand = new List<CardData>();
        for (int i = 0; i < colors.Length; i++)
        {
            CardData card = new CardData((CardColor)colors[i], (CardValue)values[i]);
            myHand.Add(card);
            Debug.Log($"  - Karte: {card}");
        }

        // TODO: In Phase 3 werden wir diese Karten hier visuell anzeigen!
    }
}