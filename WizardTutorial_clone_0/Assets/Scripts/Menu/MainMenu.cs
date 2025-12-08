using Unity.Netcode;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    
   
    public void StartHost()
    {
               Debug.Log("Start Host button clicked");
        // Implement host starting logic here
        NetworkManager.Singleton.StartHost();
        NetworkManager.Singleton.SceneManager.LoadScene("GamePlay", UnityEngine.SceneManagement.LoadSceneMode.Single);

    }
    public void StartServer()
    {
        Debug.Log("Start Server button clicked");
        // Implement host starting logic here
        NetworkManager.Singleton.StartServer();
        NetworkManager.Singleton.SceneManager.LoadScene("GamePlay", UnityEngine.SceneManagement.LoadSceneMode.Single);

    }
    public void StartClient()
    {
        Debug.Log("Start Client button clicked");
        // Implement host starting logic here
        NetworkManager.Singleton.StartClient();

    }

}

