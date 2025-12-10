using System.Text; // Wichtig für StringBuilder
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class GameplayMenu : NetworkBehaviour
{
    public static GameplayMenu Instance;

    [Header("UI Elements")]
    [SerializeField] private GameObject startButton;
    [SerializeField] private TextMeshProUGUI nextStepButtonLabel; // Aus vorherigem Schritt
    [SerializeField] private GameObject nextStepButton;

    // --- NEU: Textfeld für die Lobby-Liste ---
    [Header("Lobby Info")]
    [SerializeField] private TextMeshProUGUI lobbyInfoText;

    [Header("Containers")]
    public Transform handContainer;
    public Transform tableContainer;
    public Transform trumpContainer;
    public GameObject podiumPanel;
    public Transform podiumContainer;
    public GameObject podiumRowPrefab;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this);
        else Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        // 1. Start-Button nur für den Host sichtbar machen
        if (startButton != null)
        {
            // NetworkManager.Singleton.IsServer ist true für den Host
            startButton.SetActive(NetworkManager.Singleton.IsServer);
        }

        // 2. Events abonnieren, um die Lobby-Liste zu aktualisieren
        if (GameManager.Instance != null)
        {
            GameManager.Instance.playerDataList.OnListChanged += OnLobbyListChanged;
            GameManager.Instance.currentGameState.OnValueChanged += OnGameStateChanged;

            // Initiale Anzeige
            UpdateLobbyText();
        }
    }

    public override void OnNetworkDespawn()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.playerDataList.OnListChanged -= OnLobbyListChanged;
            GameManager.Instance.currentGameState.OnValueChanged -= OnGameStateChanged;
        }
    }

    // --- Event Handler ---

    private void OnLobbyListChanged(NetworkListEvent<WizardPlayerData> changeEvent)
    {
        UpdateLobbyText();
    }

    private void OnGameStateChanged(GameState prev, GameState current)
    {
        // Wenn das Spiel startet (Phase nicht mehr Setup), Lobby-Text & Button ausblenden
        if (current != GameState.Setup)
        {
            if (lobbyInfoText != null) lobbyInfoText.text = "";
            if (startButton != null) startButton.SetActive(false);
        }
        else
        {
            // Falls wir (z.B. nach GameOver) wieder im Setup sind
            UpdateLobbyText();
            if (IsServer && startButton != null) startButton.SetActive(true);
        }
    }

    private void UpdateLobbyText()
    {
        // Nur im Setup-Screen anzeigen
        if (GameManager.Instance == null || GameManager.Instance.currentGameState.Value != GameState.Setup) return;
        if (lobbyInfoText == null) return;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"<b>Warte auf Host... ({GameManager.Instance.playerDataList.Count} Spieler)</b>");
        sb.AppendLine(""); // Leerzeile

        foreach (var player in GameManager.Instance.playerDataList)
        {
            sb.AppendLine($"- {player.playerName}");
        }

        lobbyInfoText.text = sb.ToString();
    }

    // --- Buttons ---

    public void OnStartGameClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartGame();
        }
    }

    public void OnNextStepClicked()
    {
        ShowNextStepButton(false);
        GameManager.Instance.StartNextRoundServerRpc();
    }

    // --- Visual Helper ---

    public void HideStartButton()
    {
        if (startButton != null) startButton.SetActive(false);
        if (lobbyInfoText != null) lobbyInfoText.text = ""; // Sicherstellen, dass Text weg ist
    }

    public void ShowNextStepButton(bool show)
    {
        if (nextStepButton != null) nextStepButton.SetActive(show);
    }

    public void SetNextStepButtonText(bool isLastRound)
    {
        if (nextStepButtonLabel != null)
        {
            nextStepButtonLabel.text = isLastRound ? "Showdown" : "Nächste Runde";
        }
    }

    public void ClearHand()
    {
        foreach (Transform child in handContainer) Destroy(child.gameObject);
    }

    public void ClearTable()
    {
        foreach (Transform child in tableContainer) Destroy(child.gameObject);
    }

    public void PlaceCardOnTable(CardData cardData, GameObject cardPrefab, ulong playerId)
    {
        GameObject cardObj = Instantiate(cardPrefab, tableContainer);
        CardController cc = cardObj.GetComponent<CardController>();
        if (cc != null) cc.Initialize(cardData);

        // Optional: Den Namen des Spielers über/unter die Karte schreiben?
        // Das könnte man später noch hinzufügen.
    }

    public void ShowTrumpCard(CardData cardData, GameObject cardPrefab)
    {
        foreach (Transform child in trumpContainer) Destroy(child.gameObject);
        GameObject cardObj = Instantiate(cardPrefab, trumpContainer);
        CardController cc = cardObj.GetComponent<CardController>();
        if (cc != null) cc.Initialize(cardData);
    }

    public void ShowPodium(PlayerResult[] results)
    {
        if (podiumPanel != null) podiumPanel.SetActive(true);
        foreach (Transform child in podiumContainer) Destroy(child.gameObject);

        for (int i = 0; i < results.Length; i++)
        {
            GameObject row = Instantiate(podiumRowPrefab, podiumContainer);
            TextMeshProUGUI text = row.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = $"{i + 1}. {results[i].playerName} ({results[i].score} Pkt)";
            }
        }
    }
}