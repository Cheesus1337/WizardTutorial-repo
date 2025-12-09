using Unity.Netcode;
using UnityEngine;

public class ScoreboardUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform contentContainer; // Das Objekt mit Vertical Layout Group
    [SerializeField] private GameObject rowPrefab;       // Das Prefab der Zeile
    [SerializeField] private GameObject scoreboardPanel; // Das ganze Fenster (zum An/Ausschalten)

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            // Abonnieren: Wenn sich Daten ändern -> Refresh
            GameManager.Instance.playerDataList.OnListChanged += OnPlayerDataChanged;
            // Abonnieren: Wenn Phase wechselt (z.B. zu Bidding) -> Automatisch öffnen
            GameManager.Instance.currentGameState.OnValueChanged += OnGameStateChanged;
        }

        // Startzustand: Geschlossen
        if (scoreboardPanel) scoreboardPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.playerDataList.OnListChanged -= OnPlayerDataChanged;
            GameManager.Instance.currentGameState.OnValueChanged -= OnGameStateChanged;
        }
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
        // Nur aktualisieren, wenn offen (spart Performance)
        if (scoreboardPanel != null && scoreboardPanel.activeSelf) RefreshBoard();
    }

    private void OnGameStateChanged(GameState prev, GameState current)
    {
        // KOMFORT: Wenn Bidding startet, poppt das Fenster automatisch auf!
        if (current == GameState.Bidding)
        {
            if (scoreboardPanel) scoreboardPanel.SetActive(true);
        }
        RefreshBoard();
    }

    private void RefreshBoard()
    {
        if (contentContainer == null || rowPrefab == null || GameManager.Instance == null) return;

        // 1. Alles löschen
        foreach (Transform child in contentContainer) Destroy(child.gameObject);

        // 2. Neu aufbauen für alle Spieler in der Liste
        foreach (var playerData in GameManager.Instance.playerDataList)
        {
            GameObject newRow = Instantiate(rowPrefab, contentContainer);
            var script = newRow.GetComponent<PlayerRowUI>();
            if (script != null) script.SetupRow(playerData);
        }
    }
}