using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement; // Wichtig für SceneManager
 using static CardEnums;// <- Das brauchst du meist nicht, wenn die Enums global sind

public class MainMenu : MonoBehaviour
{
    

    // Start wird automatisch von Unity aufgerufen, wenn die Szene startet
    private void Start()
    {
        
    }

    

    // --- Deine Buttons ---

    public void StartHost()
    {
        Debug.Log("Start Host button clicked");
        // Versuch, den Host zu starten
        bool success = NetworkManager.Singleton.StartHost();

        if (success)
        {
            Debug.Log("Host erfolgreich gestartet.");
            // Hier könnte man Szenen laden oder UI ausblenden
            NetworkManager.Singleton.SceneManager.LoadScene("GamePlay", LoadSceneMode.Single);
        }
        else
        {
            Debug.LogError("Host konnte nicht gestartet werden! (Port besetzt?)");
            // Optional: Dem Spieler eine Fehlermeldung im UI anzeigen
        }
        
    }

    public void StartServer()
    {
        Debug.Log("Start Server button clicked");
        NetworkManager.Singleton.StartServer();
        NetworkManager.Singleton.SceneManager.LoadScene("GamePlay", LoadSceneMode.Single);
    }

    public void StartClient()
    {
        Debug.Log("Start Client button clicked");
        NetworkManager.Singleton.StartClient();
    }
}