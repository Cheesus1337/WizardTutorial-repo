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
    public Transform tableArea;
    public GameObject trumpLabel;

    [Header("Buttons")]
    public GameObject nextStepButton;

    [Header("Podium UI")]
    public GameObject podiumPanel; // Das Panel mit den Plätzen
    public TextMeshProUGUI firstPlaceText;
    public TextMeshProUGUI secondPlaceText;
    public TextMeshProUGUI thirdPlaceText;

    [Header("Main UI")]
    public GameObject startGameButton;

    private void Awake()
    {
        Instance = this;
        if (trumpLabel != null) trumpLabel.SetActive(false);
        if (nextStepButton != null) nextStepButton.SetActive(false);
        if (podiumPanel != null) podiumPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // --- Buttons ---
    public void OnStartGameClicked()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            var gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager != null) gameManager.StartGame();
        }
    }

    public void OnNextStepClicked()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            var gm = FindFirstObjectByType<GameManager>();
            if (gm != null) gm.StartNextRoundServerRpc();
        }
    }

    public void OnMainMenuClicked()
    {
        NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene("MainMenu");
    }

    public void Disconnect()
    {
        NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene("MainMenu");
    }

    public void HideStartButton()
    {
        if (startGameButton != null) startGameButton.SetActive(false);
    }

    // --- Methoden für RPCs ---

    public void ShowPodium(PlayerResult[] results)
    {
        if (podiumPanel == null) return;

        podiumPanel.SetActive(true);
        podiumPanel.transform.SetAsLastSibling(); // Ganz nach vorne holen

        // Texte leeren
        if (firstPlaceText) firstPlaceText.text = "";
        if (secondPlaceText) secondPlaceText.text = "";
        if (thirdPlaceText) thirdPlaceText.text = "";

        // Plätze füllen
        if (results.Length > 0 && firstPlaceText != null)
            firstPlaceText.text = $"1. {results[0].playerName} ({results[0].score} Pkt)";

        if (results.Length > 1 && secondPlaceText != null)
            secondPlaceText.text = $"2. {results[1].playerName} ({results[1].score} Pkt)";

        if (results.Length > 2 && thirdPlaceText != null)
            thirdPlaceText.text = $"3. {results[2].playerName} ({results[2].score} Pkt)";
    }

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