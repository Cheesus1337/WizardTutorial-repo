using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameplayMenu : MonoBehaviour
{
    // Singleton-Instanz: Damit der GameManager einfach "GameplayMenu.Instance" rufen kann
    public static GameplayMenu Instance { get; private set; }

    [Header("UI References")]
    public Transform handContainer; // Hier werden die Karten-Objekte als "Kinder" reingehängt

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

    private void OnDestroy()
    {
        // Sauber machen beim Beenden
        if (Instance == this)
        {
            Instance = null;
        }
    }
}