using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEngine;

public class ConnectionGUI : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 300));

        if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient)
        {
            StatusLabel();
        }
        GUILayout.EndArea();
    }
       
      private void StatusLabel()
    {
        string mode = "";

       mode = NetworkManager.Singleton.IsHost ? "Host" :
              NetworkManager.Singleton.IsServer ? "Server" :
              NetworkManager.Singleton.IsClient ? "Client" : "Offline";

    

        GUILayout.Label("Current Mode: " + mode);

        /*if (GUILayout.Button("Connect to Server"))
        {
            Debug.Log("Connecting to server...");
            // Add your connection logic here
        }*/

       
    }
}
