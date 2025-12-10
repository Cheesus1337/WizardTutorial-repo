using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameplayMenu : MonoBehaviour
{
    public static GameplayMenu Instance { get; private set; }

    [Header("UI References")]
    public Transform handContainer;
    public Transform trumpCardPosition;
    public Transform tableArea; // WICHTIG: Muss verknüpft sein!

    public GameObject trumpLabel;

    [Header("Buttons")]
    public GameObject nextStepButton; // Der "Nächste Runde" Button

    [Header("Game Over UI")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI winnerText;

    [Header("Podium UI")]
    public GameObject podiumPanel; // Das neue Panel
                                   // Wir machen es einfach: 3 Textfelder für die Top 3
    public TextMeshProUGUI firstPlaceText;
    public TextMeshProUGUI secondPlaceText;
    public TextMeshProUGUI thirdPlaceText;


    private void Awake()
    {
        Instance = this;
        if (trumpLabel != null) trumpLabel.SetActive(false);
        if (nextStepButton != null) nextStepButton.SetActive(false);
    }

    // In Awake/Start: Panel verstecken!
    // if (gameOverPanel != null) gameOverPanel.SetActive(false);

    public void ShowPodium(PlayerResult[] results)
    {
        if (podiumPanel == null) return;

        podiumPanel.SetActive(true);
        podiumPanel.transform.SetAsLastSibling(); // Nach vorne holen

        // Textfelder leeren (falls weniger als 3 Spieler)
        if (firstPlaceText) firstPlaceText.text = "";
        if (secondPlaceText) secondPlaceText.text = "";
        if (thirdPlaceText) thirdPlaceText.text = "";

        // 1. Platz
        if (results.Length > 0 && firstPlaceText != null)
        {
            firstPlaceText.text = $"1. {results[0].playerName} ({results[0].score} Pkt)";
        }

        // 2. Platz
        if (results.Length > 1 && secondPlaceText != null)
        {
            secondPlaceText.text = $"2. {results[1].playerName} ({results[1].score} Pkt)";
        }

        // 3. Platz
        if (results.Length > 2 && thirdPlaceText != null)
        {
            thirdPlaceText.text = $"3. {results[2].playerName} ({results[2].score} Pkt)";
        }
    }

    // Die alte ShowGameOverScreen Methode kannst du löschen oder ignorieren.

    // Für den Button "Zurück zum Menü"
    public void OnMainMenuClicked()
    {
        NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene("MainMenu");
    }


    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // --- Button Events ---

    public void OnStartGameClicked()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            var gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager != null) gameManager.StartGame();
        }
    }

    // HIER WAR DER FEHLER: Wir rufen jetzt die richtige Methode auf
    public void OnNextStepClicked()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            var gm = FindFirstObjectByType<GameManager>();
            if (gm != null)
            {
                // NEU: StartNextRoundServerRpc statt ContinueGameServerRpc
                gm.StartNextRoundServerRpc();
            }
        }
    }

    public void Disconnect()
    {
        NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene("MainMenu");
    }

    // --- Visualisierungsmethoden ---

    public void ClearHand()
    {
        if (handContainer == null) return;
        foreach (Transform child in handContainer) Destroy(child.gameObject);
    }

    public void PlaceCardOnTable(CardData cardData, GameObject cardPrefab, ulong clientId)
    {
        if (tableArea == null || cardPrefab == null) return;

        GameObject playedCard = Instantiate(cardPrefab, tableArea);

        CardController controller = playedCard.GetComponent<CardController>();
        if (controller != null)
        {
            controller.SetBaseScale(0.8f);
            controller.Initialize(cardData);
        }
    }

    public void ClearTable()
    {
        if (tableArea == null) return;
        foreach (Transform child in tableArea) Destroy(child.gameObject);
    }

    public void ShowTrumpCard(CardData cardData, GameObject cardPrefab)
    {
        if (trumpCardPosition == null) return;
        foreach (Transform child in trumpCardPosition) Destroy(child.gameObject);

        if (cardPrefab != null)
        {
            GameObject trumpCard = Instantiate(cardPrefab, trumpCardPosition);
            var controller = trumpCard.GetComponent<CardController>();
            if (controller != null)
            {
                controller.SetBaseScale(0.7f);
                controller.Initialize(cardData);
            }
            if (trumpLabel != null) trumpLabel.SetActive(true);
        }
    }

    public void ShowNextStepButton(bool show)
    {
        if (nextStepButton != null) nextStepButton.SetActive(show);
    }
}