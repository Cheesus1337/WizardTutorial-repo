using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

// ACHTUNG: Jetzt erben wir von MonoBehaviour, NICHT mehr von NetworkBehaviour!
public class SessionGameStarter : MonoBehaviour
{
    private void Start()
    {
        // Wir abonnieren das Event: "Wenn sich jemand verbindet..."
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
    }

    private void OnDestroy()
    {
        // Sauber bleiben: Event wieder abbestellen, wenn das Objekt zerstört wird
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    // Diese Methode wird aufgerufen, wenn IRGENDJEMAND (auch wir selbst) sich verbindet
    private void OnClientConnected(ulong clientId)
    {
        // 1. Sind wir der Server/Host? (Nur der Chef darf die Szene wechseln)
        if (NetworkManager.Singleton.IsServer)
        {
            // 2. Sind WIR es, die sich gerade verbunden haben?
            // (Damit das Spiel nicht neu lädt, wenn später ein Freund dazu kommt)
            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                Debug.Log("Ich (Host) habe mich verbunden! Starte 'GamePlay' Szene...");

                // Szenenwechsel über das Netzwerk-System
                NetworkManager.Singleton.SceneManager.LoadScene("GamePlay", LoadSceneMode.Single);
            }
        }
    }
}