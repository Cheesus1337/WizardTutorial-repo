using Unity.Netcode;
using UnityEngine;

public class NetworkManagerCleanup : MonoBehaviour
{
    void Awake()
    {
        // Wenn bereits ein NetworkManager existiert (vom vorherigen Spiel),
        // dann zerstöre den HIER in der Szene platzierten, damit der alte weiterlebt.
        if (NetworkManager.Singleton != null && NetworkManager.Singleton != GetComponent<NetworkManager>())
        {
            Destroy(gameObject);
        }
    }
}