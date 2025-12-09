using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameplayMenu : MonoBehaviour
{
    // Singleton-Instanz: Damit der GameManager einfach "GameplayMenu.Instance" rufen kann
    public static GameplayMenu Instance { get; private set; }

    [Header("UI References")]
    public Transform handContainer; // Hier werden die Karten-Objekte als "Kinder" reingehängt
    public Transform trumpCardPosition; // Position der Trumpfkarte

    public GameObject trumpLabel;

    private void Awake()
    {
        // SICHERE VARIANTE:
        // Wir setzen die Instanz einfach neu, egal was vorher war.
        // Da wir die Szene jedes Mal neu laden, ist das hier sicher.
        if (Instance != null && Instance != this)
        {
            // Optional: Warnung loggen, aber NICHT zerstören, wenn wir uns unsicher sind
            Debug.LogWarning("Alte GameplayMenu Instanz gefunden und überschrieben.");
        }

        Instance = this;

        
        if (trumpLabel != null) trumpLabel.SetActive(false);
    }

    public void OnStartGameClicked()
    {
        // Wir suchen den GameManager (dieser muss in der Szene existieren!)
        if (NetworkManager.Singleton.IsServer)
        {
            // Neue Methode in Unity 2023+, früher FindObjectOfType
            var gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager != null)
            {
                gameManager.StartGame();
            }
            else
            {
                Debug.LogError("GameManager nicht gefunden!");
            }
        }
    }

    public void Disconnect()
    {
        NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene("MainMenu");
    }

    // Hilfsfunktion: Hand leeren (z.B. bei Rundenbeginn, damit keine alten Karten bleiben)
    public void ClearHand()
    {
        if (handContainer == null) return;

        foreach (Transform child in handContainer)
        {
            Destroy(child.gameObject);
        }
    }

    public void ShowTrumpCard(CardData cardData, GameObject cardPrefab)
    {
        if (trumpCardPosition == null) return;

        // 1. Alte Karte löschen
        foreach (Transform child in trumpCardPosition) Destroy(child.gameObject);

        // 2. Neue Karte erstellen
        if (cardPrefab != null)
        {
            GameObject trumpCard = Instantiate(cardPrefab, trumpCardPosition);

            // Controller holen
            var controller = trumpCard.GetComponent<CardController>();

            if (controller != null)
            {
                // WICHTIG: Erst skalieren, dann initialisieren
                controller.SetBaseScale(0.7f); // 0.7 oder 0.8, je nach Geschmack
                controller.Initialize(cardData);
            }

            // 3. Text explizit aktivieren
            if (trumpLabel != null)
            {
                Debug.Log("Aktiviere Trumpf-Label!"); // Debug-Log zur Sicherheit
                trumpLabel.SetActive(true);
            }
            else
            {
                Debug.LogError("Trump Label Referenz fehlt im GameplayMenu!");
            }
        }
    }

    private void OnDestroy()
    {
        // Sauber machen beim Beenden
        if (Instance == this)
        {
            Instance = null;
        }
    }
}