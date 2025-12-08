using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameplayMenu : MonoBehaviour
{
    // Hier KEINE Start-Methode mit Deck-Logik! Das macht nur der GameManager.

    // Button: "Start Round"
    public void OnStartGameClicked()
    {
        // Wir prüfen, ob wir der Server/Host sind
        if (NetworkManager.Singleton.IsServer)
        {
            Debug.Log("UI: Start Round geklickt -> Sende an GameManager");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartGame();
            }
            else
            {
                Debug.LogError("Fehler: GameManager Instanz nicht gefunden!");
            }
        }
        else
        {
            Debug.Log("Nur der Host kann das Spiel starten.");
        }
    }

    // Button: "Disconnect"
    public void Disconnect()
    {
        NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene("MainMenu");
    }
}