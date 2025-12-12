using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{
    [Header("UI Referenzen")]
    [SerializeField] private Button startGameButton; // Hier den neuen Button reinziehen

    void Start()
    {
        // Sicherheitshalber Button verstecken und Listener hinzufügen
        if (startGameButton != null)
        {
            startGameButton.gameObject.SetActive(false);
            startGameButton.onClick.AddListener(OnStartGameClicked);
        }
    }

    void Update()
    {
        // Wir prüfen in jedem Frame den Status (einfachste Methode für UI Updates)
        if (NetworkManager.Singleton != null)
        {
            // Sind wir verbunden UND sind wir der Host (Server)?
            if (NetworkManager.Singleton.IsHost)
            {
                // Button zeigen!
                if (startGameButton != null && !startGameButton.gameObject.activeSelf)
                {
                    startGameButton.gameObject.SetActive(true);
                }
            }
            // Falls wir nur Client sind oder getrennt wurden -> Button verstecken
            else
            {
                if (startGameButton != null && startGameButton.gameObject.activeSelf)
                {
                    startGameButton.gameObject.SetActive(false);
                }
            }
        }
    }

    private void OnStartGameClicked()
    {
        Debug.Log("Host startet das Spiel!");
        // Dies ist der Befehl, der alle Clients in die GamePlay-Szene zieht!
        NetworkManager.Singleton.SceneManager.LoadScene("GamePlay", LoadSceneMode.Single);
    }
}