using Unity.Netcode;
using UnityEngine;

public class ScoreboardUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform contentContainer;
    [SerializeField] private GameObject rowPrefab;
    [SerializeField] private GameObject scoreboardPanel;

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.playerDataList.OnListChanged += OnPlayerDataChanged;
            GameManager.Instance.currentGameState.OnValueChanged += OnGameStateChanged;

            // --- NEU: Auch auf Spielerwechsel hören! ---
            GameManager.Instance.activePlayerIndex.OnValueChanged += OnTurnChanged;
        }

        if (scoreboardPanel) scoreboardPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.playerDataList.OnListChanged -= OnPlayerDataChanged;
            GameManager.Instance.currentGameState.OnValueChanged -= OnGameStateChanged;

            // --- NEU: Abbestellen ---
            GameManager.Instance.activePlayerIndex.OnValueChanged -= OnTurnChanged;
        }
    }

    // --- NEU: Event-Handler für Spielerwechsel ---
    private void OnTurnChanged(int prev, int current)
    {
        // Wenn ein neuer Spieler dran ist, müssen wir prüfen, ob Buttons angezeigt werden sollen
        if (scoreboardPanel.activeSelf) RefreshBoard();
    }

    public void ToggleScoreboard()
    {
        if (scoreboardPanel != null)
        {
            scoreboardPanel.SetActive(!scoreboardPanel.activeSelf);
            if (scoreboardPanel.activeSelf) RefreshBoard();
        }
    }

    private void OnPlayerDataChanged(NetworkListEvent<WizardPlayerData> changeEvent)
    {
        if (scoreboardPanel != null && scoreboardPanel.activeSelf) RefreshBoard();
    }

    private void OnGameStateChanged(GameState prev, GameState current)
    {
        if (current == GameState.Bidding)
        {
            if (scoreboardPanel) scoreboardPanel.SetActive(true);
        }
        RefreshBoard();
    }

    private void RefreshBoard()
    {
        if (contentContainer == null || rowPrefab == null || GameManager.Instance == null) return;

        foreach (Transform child in contentContainer) Destroy(child.gameObject);

        foreach (var playerData in GameManager.Instance.playerDataList)
        {
            GameObject newRow = Instantiate(rowPrefab, contentContainer);
            var script = newRow.GetComponent<PlayerRowUI>();
            if (script != null) script.SetupRow(playerData);
        }
    }
}