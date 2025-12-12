using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements; // WICHTIG für UI Toolkit

public class LobbyManager : MonoBehaviour
{
    [Header("Referenzen")]
    [SerializeField] private UIDocument quickJoinUIDocument;

    private Button startBtn;

    void OnEnable()
    {
        if (quickJoinUIDocument == null)
        {
            Debug.LogError("LobbyManager: UI Document ist nicht zugewiesen!");
            return;
        }

        var root = quickJoinUIDocument.rootVisualElement;

        // Suche nach dem Button-Namen aus dem UI Builder
        startBtn = root.Q<Button>("StartGameBtn");

        if (startBtn != null)
        {
            Debug.Log("LobbyManager: 'StartGameBtn' gefunden und verknüpft.");
            startBtn.clicked += OnStartGameClicked;
            //startBtn.style.display = DisplayStyle.Flex; zum testen immer anzeigen
            startBtn.style.display = DisplayStyle.None; // Erstmal verstecken
        }
        else
        {
            Debug.LogError("LobbyManager: Button 'StartGameBtn' wurde im UXML NICHT gefunden! Prüfe den Namen im UI Builder.");
        }
    }

    void Update()
    {
        // 1. Button Referenz Check
        if (startBtn == null) return;

        // 2. DER KNACKPUNKT: Finden wir den NetworkManager?
        if (NetworkManager.Singleton == null)
        {
            // Damit wir nicht 60x pro Sekunde gespammt werden, nur alle paar Sekunden warnen
            if (Time.frameCount % 300 == 0)
                Debug.LogWarning("ALARM: 'NetworkManager.Singleton' ist NULL! Das Skript findet den NetworkManager nicht.");

            return; // Hier bricht er ab!
        }

        // 3. Status Check (Läuft er?)
        if (!NetworkManager.Singleton.IsListening)
        {
            if (Time.frameCount % 300 == 0)
                Debug.Log("NetworkManager gefunden, aber noch nicht verbunden (IsListening = false).");
            return;
        }

        // 4. Host Logik
        if (NetworkManager.Singleton.IsHost)
        {
            if (startBtn.style.display == DisplayStyle.None)
            {
                Debug.Log("BIN HOST! Button AN.");
                startBtn.style.display = DisplayStyle.Flex;
            }
        }
        else // Client
        {
            if (startBtn.style.display == DisplayStyle.Flex)
            {
                startBtn.style.display = DisplayStyle.None;
            }
        }
    }

    private void OnStartGameClicked()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            Debug.Log("Host startet das Spiel! Lade Szene...");
            NetworkManager.Singleton.SceneManager.LoadScene("GamePlay", LoadSceneMode.Single);
        }
    }
}