using UnityEngine;
using UnityEngine.UIElements;
using Unity.Services.Authentication;
using Unity.Services.Core;
using System.Threading.Tasks;

public class PlayerNameManager : MonoBehaviour
{
    [Header("UI Referenz")]
    // Hier ziehst du das GameObject rein, das dein UI anzeigt (HostArea)
    [SerializeField] private UIDocument uiDocument;

    private TextField nameInput;
    private const string PlayerNameKey = "WizardPlayerName";

    async void Start()
    {
        // 1. Services sicherstellen
        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
                await UnityServices.InitializeAsync();
        }
        catch { /* Schon an */ }

        // 2. UI suchen
        if (uiDocument != null)
        {
            var root = uiDocument.rootVisualElement;
            // Wir suchen das Feld, das du in Schritt 1 "PlayerNameInput" genannt hast
            nameInput = root.Q<TextField>("PlayerNameInput");

            if (nameInput != null)
            {
                // Laden
                string savedName = PlayerPrefs.GetString(PlayerNameKey, "Zauberer " + Random.Range(100, 999));
                nameInput.value = savedName;

                // Speichern bei Änderung
                nameInput.RegisterValueChangedCallback(evt => {
                    string newName = evt.newValue;
                    if (!string.IsNullOrWhiteSpace(newName))
                    {
                        PlayerPrefs.SetString(PlayerNameKey, newName);
                        PlayerPrefs.Save();
                    }
                });
            }
            else
            {
                Debug.LogError("PlayerNameInput nicht im UXML gefunden! Hast du den Namen im UI Builder gesetzt?");
            }
        }

        // 3. Wenn eingeloggt -> Namen an Unity senden
        AuthenticationService.Instance.SignedIn += UpdateUnityName;
    }

    private async void UpdateUnityName()
    {
        string name = PlayerPrefs.GetString(PlayerNameKey, "UnknownWizard");
        try
        {
            await AuthenticationService.Instance.UpdatePlayerNameAsync(name);
            Debug.Log("Unity Name gesetzt auf: " + name);
        }
        catch { /* Rate Limit ignoriere ich hier mal */ }
    }
}