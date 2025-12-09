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

    private void Awake()
    {
        Instance = this;
        if (trumpLabel != null) trumpLabel.SetActive(false);
        if (nextStepButton != null) nextStepButton.SetActive(false);
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