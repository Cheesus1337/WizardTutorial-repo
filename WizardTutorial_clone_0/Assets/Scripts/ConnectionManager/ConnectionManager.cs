using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ConnectionManager : MonoBehaviour
{


    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectedCallback;
        NetworkManager.Singleton.OnServerStopped += OnServerStopped;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectedCallback;
        NetworkManager.Singleton.OnServerStopped -= OnServerStopped;
    }


    private void OnServerStopped(bool wasClient)
    {
        Debug.Log("Server stopped");
        SceneManager.LoadScene("MainMenu");
    }

    private void OnClientConnectedCallback(ulong clientId)
    {

        Debug.Log($"Client connected: {clientId}");
    }

    private void OnClientDisconnectedCallback(ulong clientId)
    {
        Debug.Log($"Client disconnected: {clientId}");

        if(!NetworkManager.Singleton.IsServer)
        {
            SceneManager.LoadScene("MainMenu");
        }
        
    }

}
