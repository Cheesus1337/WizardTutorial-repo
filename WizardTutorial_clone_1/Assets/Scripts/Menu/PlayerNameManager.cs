using UnityEngine;
using UnityEngine.UIElements;
using Unity.Services.Authentication;
using Unity.Services.Core;
using System.Threading.Tasks;

public class PlayerNameManager : MonoBehaviour
{
    [Header("UI Referenz")]
    [SerializeField] private UIDocument uiDocument;

    private TextField nameInput;
    // WICHTIG: Dieser Key muss identisch mit dem im GameManager sein!
    private const string PlayerNameKey = "WizardPlayerName";

    async void Start()
    {
        // 1. Services initialisieren falls nötig
        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
                await UnityServices.InitializeAsync();
        }
        catch { /* Ignorieren */ }

        // 2. UI Setup
        SetupUI();

        // 3. Event abonnieren: Wenn wir uns einloggen, Namen hochladen
        AuthenticationService.Instance.SignedIn += OnSignedIn;

        // Falls wir schon eingeloggt sind (durch Building Blocks), direkt updaten
        if (AuthenticationService.Instance.IsSignedIn)
        {
            UpdateUnityAuthName();
        }
    }

    private void OnDisable()
    {
        try
        {
            AuthenticationService.Instance.SignedIn -= OnSignedIn;
        }
        catch { }
    }

    private void SetupUI()
    {
        if (uiDocument == null) return;

        var root = uiDocument.rootVisualElement;
        // Suche das Feld. Hinweis: In deiner UXML hieß es evtl. "PlayerNameInput"
        nameInput = root.Q<TextField>("PlayerNameInput");

        if (nameInput != null)
        {
            // Laden
            string savedName = PlayerPrefs.GetString(PlayerNameKey, "Zauberer " + Random.Range(100, 999));
            nameInput.value = savedName;

            // Speichern und Unity-Update bei Änderung
            nameInput.RegisterValueChangedCallback(evt =>
            {
                string newName = evt.newValue;
                if (!string.IsNullOrWhiteSpace(newName))
                {
                    // A) Lokal speichern (für GameManager)
                    PlayerPrefs.SetString(PlayerNameKey, newName);
                    PlayerPrefs.Save();

                    // B) Unity Auth updaten (für Lobby Anzeige)
                    UpdateUnityAuthName();
                }
            });
        }
    }

    private void OnSignedIn()
    {
        UpdateUnityAuthName();
    }

    private async void UpdateUnityAuthName()
    {
        if (!AuthenticationService.Instance.IsSignedIn) return;

        string currentName = PlayerPrefs.GetString(PlayerNameKey, "UnknownWizard");

        // Kleiner Check um API-Spam zu vermeiden (Name muss anders sein)
        try
        {
            // Optional: Man könnte prüfen, ob der Name schon stimmt.
            // Aber UpdatePlayerNameAsync ist robust genug.
            await AuthenticationService.Instance.UpdatePlayerNameAsync(currentName);
            Debug.Log($"Unity Auth Name erfolgreich aktualisiert auf: {currentName}");
        }
        catch (System.Exception e)
        {
            // Rate Limit errors können hier passieren, wenn man schnell tippt. Ist okay.
             Debug.LogWarning(e.Message); 
        }
    }
}