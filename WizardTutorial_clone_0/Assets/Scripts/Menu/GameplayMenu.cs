using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameplayMenu : MonoBehaviour
{
    
    public void Disconnect()
        {
        NetworkManager.Singleton.Shutdown();
        

    }

}
