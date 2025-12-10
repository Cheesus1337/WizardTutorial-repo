using System.Text;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class GameplayMenu : NetworkBehaviour
{
    public static GameplayMenu Instance;

    [Header("UI Elements")]
    [SerializeField] private GameObject startButton;
    [SerializeField] private TextMeshProUGUI nextStepButtonLabel;
    [SerializeField] private GameObject nextStepButton;
    [SerializeField] private TextMeshProUGUI lobbyInfoText;

    [Header("Podium Settings")]
    [SerializeField] private GameObject podiumPanel;
    // Hier müssen deine 3 Text-Objekte aus der SCENE rein (Platz1, Platz2, Platz3)
    [SerializeField] private TextMeshProUGUI[] podiumSlots;

    [Header("Containers")]
    public Transform handContainer;
    public Transform tableContainer;
    public Transform trumpContainer;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this);
        else Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (startButton != null) startButton.SetActive(NetworkManager.Singleton.IsServer);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.playerDataList.OnListChanged += OnLobbyListChanged;
            GameManager.Instance.currentGameState.OnValueChanged += OnGameStateChanged;
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

    private void OnLobbyListChanged(NetworkListEvent<WizardPlayerData> changeEvent)
    {
        UpdateLobbyText();
    }

    private void OnGameStateChanged(GameState prev, GameState current)
    {
        if (current != GameState.Setup)
        {
            if (lobbyInfoText != null) lobbyInfoText.text = "";
            if (startButton != null) startButton.SetActive(false);
        }
        else
        {
            UpdateLobbyText();
            if (IsServer && startButton != null) startButton.SetActive(true);
        }
    }

    private void UpdateLobbyText()
    {
        if (GameManager.Instance == null || GameManager.Instance.currentGameState.Value != GameState.Setup) return;
        if (lobbyInfoText == null) return;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"<b>Warte auf Host... ({GameManager.Instance.playerDataList.Count} Spieler)</b>");
        sb.AppendLine("");
        foreach (var player in GameManager.Instance.playerDataList)
        {
            sb.AppendLine($"- {player.playerName}");
        }
        lobbyInfoText.text = sb.ToString();
    }

    // --- Podium Logik ---
    public void ShowPodium(PlayerResult[] results)
    {
        Debug.Log($"[Podium] ShowPodium aufgerufen mit {results.Length} Ergebnissen.");

        if (podiumPanel != null) podiumPanel.SetActive(true);

        for (int i = 0; i < podiumSlots.Length; i++)
        {
            if (podiumSlots[i] == null) continue;

            if (i < results.Length)
            {
                // Aktivieren
                podiumSlots[i].gameObject.SetActive(true);

                string newText = $"{results[i].playerName}\n{results[i].score} Punkte";
                Debug.Log($"[Podium] Setze Slot {i} auf Text: '{newText}'");

                // Text setzen
                podiumSlots[i].text = newText;
            }
            else
            {
                // Deaktivieren (wenn weniger Spieler als Plätze)
                podiumSlots[i].gameObject.SetActive(false);
            }
        }
    }

    // --- Buttons & Helper ---
    public void OnStartGameClicked() { if (GameManager.Instance != null) GameManager.Instance.StartGame(); }
    public void OnNextStepClicked() { ShowNextStepButton(false); GameManager.Instance.StartNextRoundServerRpc(); }
    public void HideStartButton() { if (startButton != null) startButton.SetActive(false); }
    public void ShowNextStepButton(bool show) { if (nextStepButton != null) nextStepButton.SetActive(show); }
    public void SetNextStepButtonText(bool isLastRound) { if (nextStepButtonLabel != null) nextStepButtonLabel.text = isLastRound ? "Showdown" : "Nächste Runde"; }
    public void ClearHand() { foreach (Transform child in handContainer) Destroy(child.gameObject); }
    public void ClearTable() { foreach (Transform child in tableContainer) Destroy(child.gameObject); }
    public void PlaceCardOnTable(CardData cardData, GameObject cardPrefab, ulong playerId)
    {
        GameObject cardObj = Instantiate(cardPrefab, tableContainer);
        CardController cc = cardObj.GetComponent<CardController>();
        if (cc != null) cc.Initialize(cardData);
    }
    public void ShowTrumpCard(CardData cardData, GameObject cardPrefab)
    {
        foreach (Transform child in trumpContainer) Destroy(child.gameObject);
        GameObject cardObj = Instantiate(cardPrefab, trumpContainer);
        CardController cc = cardObj.GetComponent<CardController>();
        if (cc != null) cc.Initialize(cardData);
    }
}