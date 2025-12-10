using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement; // Wichtig für SceneManager
 // <- Das brauchst du meist nicht, wenn die Enums global sind

public class MainMenu : MonoBehaviour
{

    [Header("UI")]
    [SerializeField] private TMP_InputField nameInputField;

    // Statische Variable, um den Namen in die Game-Szene zu speichern
    public static string LocalPlayerName = "Wizard";

    // Start wird automatisch von Unity aufgerufen, wenn die Szene startet
    private void Start()
    {
        // Alten Namen laden oder Standard setzen
        if (nameInputField != null)
        {
            nameInputField.text = PlayerPrefs.GetString("PlayerName", "Wizard " + Random.Range(100, 999));
            // Listener, falls sich der Text ändert
            nameInputField.onValueChanged.AddListener(OnNameChanged);
            // Initial setzen
            LocalPlayerName = nameInputField.text;
        }
    }

    public void OnNameChanged(string newName)
    {
        LocalPlayerName = newName;
        PlayerPrefs.SetString("PlayerName", newName);
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

    public void StartServer()
    {
        Debug.Log("Start Server button clicked");
        NetworkManager.Singleton.StartServer();
        NetworkManager.Singleton.SceneManager.LoadScene("GamePlay", LoadSceneMode.Single);
    }

    public void StartClient()
    {
        Debug.Log($"Starte Client als {LocalPlayerName}...");
        bool success = NetworkManager.Singleton.StartClient();
        if (success) Debug.Log("Client gestartet.");
        else Debug.LogError("Client Start fehlgeschlagen!");
    }
}