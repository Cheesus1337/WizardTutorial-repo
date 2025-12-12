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
        if (NetworkManager.Singleton == null) return;

        // Sind wir verbunden UND in der MainMenu Szene?
        if (NetworkManager.Singleton.IsListening &&
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainMenu")
        {
            // Host Button Logik
            if (NetworkManager.Singleton.IsHost)
            {
                if (startBtn != null && startBtn.style.display == DisplayStyle.None)
                {
                    Debug.Log("Zurück in der Lobby: Host-Button wieder aktivieren.");
                    startBtn.style.display = DisplayStyle.Flex;
                }
            }

            // Client Logik (Button verstecken)
            if (!NetworkManager.Singleton.IsHost && startBtn != null)
            {
                startBtn.style.display = DisplayStyle.None;
            }

            // Sicherstellen, dass das Lobby-UI (Spielerliste) sichtbar ist
            // Das "QuickJoin" UI Document könnte durch den Reload deaktiviert sein
            if (quickJoinUIDocument != null && quickJoinUIDocument.rootVisualElement != null)
            {
                // Manchmal verstecken die Building Blocks das Root-Element beim Start
                // Hier zwingen wir es zurück
                if (quickJoinUIDocument.rootVisualElement.style.display == DisplayStyle.None)
                    quickJoinUIDocument.rootVisualElement.style.display = DisplayStyle.Flex;
            }
        }
        else
        {
            // Nicht verbunden oder im Spiel -> Start Button weg
            if (startBtn != null) startBtn.style.display = DisplayStyle.None;
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