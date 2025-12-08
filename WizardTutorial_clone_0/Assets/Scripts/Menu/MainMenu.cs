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
        NetworkManager.Singleton.StartHost();
        NetworkManager.Singleton.SceneManager.LoadScene("GamePlay", LoadSceneMode.Single);
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