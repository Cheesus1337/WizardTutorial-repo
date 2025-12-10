using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement; // Wichtig für SceneManager


public class MainMenu : MonoBehaviour
{

    [Header("UI")]
    [SerializeField] private TMP_InputField nameInputField;

    // Statische Variable, um den Namen in die Game-Szene zu speichern
    public static string LocalPlayerName = "Wizard";

    // Start wird automatisch von Unity aufgerufen, wenn die Szene startet
    private void Start()
    {
        // 1. Alten Namen laden oder einen zufälligen Default generieren
        string defaultName = "Wizard " + Random.Range(100, 999);
        string savedName = PlayerPrefs.GetString("PlayerName", defaultName);

        // 2. InputField initialisieren
        if (nameInputField != null)
        {
            nameInputField.text = savedName;

            // WICHTIG: Das Setzen von .text im Code feuert NICHT das onValueChanged Event.
            // Wir müssen die Variable also hier einmal manuell synchronisieren.
            LocalPlayerName = savedName;

            // Listener registrieren: Feuert nur, wenn der Spieler tippt
            nameInputField.onValueChanged.AddListener(OnNameChanged);
        }

    }

    public void OnNameChanged(string newName)
    {
        // Name im statischen Speicher aktualisieren (für GameManager)
        LocalPlayerName = newName;

        // Name auf der Festplatte speichern (für nächsten Neustart)
        PlayerPrefs.SetString("PlayerName", newName);
        PlayerPrefs.Save(); // Erzwingt das sofortige Schreiben auf die Disk
    }


    // --- Buttons ---

    public void StartHost()
    {
        Debug.Log($"Starte Host als {LocalPlayerName}...");
        bool success = NetworkManager.Singleton.StartHost();

        if (success) 
        { 
        Debug.Log("Host gestartet.");
        NetworkManager.Singleton.SceneManager.LoadScene("GamePlay", LoadSceneMode.Single);
        }
        else 
            Debug.LogError("Host Start fehlgeschlagen!");

               
    }

   /* public void StartServer()
    {
        Debug.Log("Start Server button clicked");
        NetworkManager.Singleton.StartServer();
        NetworkManager.Singleton.SceneManager.LoadScene("GamePlay", LoadSceneMode.Single);
    }*/

    public void StartClient()
    {
        Debug.Log($"Starte Client als {LocalPlayerName}...");
        bool success = NetworkManager.Singleton.StartClient();
        if (success) Debug.Log("Client gestartet.");
        else Debug.LogError("Client Start fehlgeschlagen!");
    }
}