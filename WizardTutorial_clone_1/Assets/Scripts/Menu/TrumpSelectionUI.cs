using UnityEngine;
using UnityEngine.UI;
using static CardEnums; // Damit wir CardColor direkt nutzen kˆnnen

public class TrumpSelectionUI : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button btnRed;
    [SerializeField] private Button btnBlue;
    [SerializeField] private Button btnGreen;
    [SerializeField] private Button btnYellow;

    private void Start()
    {
        // Listener hinzuf¸gen: Wenn geklickt wird, Funktion mit Farbe aufrufen
        btnRed.onClick.AddListener(() => OnColorSelected(CardColor.Red));
        btnBlue.onClick.AddListener(() => OnColorSelected(CardColor.Blue));
        btnGreen.onClick.AddListener(() => OnColorSelected(CardColor.Green));
        btnYellow.onClick.AddListener(() => OnColorSelected(CardColor.Yellow));

        
    }

    private void OnColorSelected(CardColor selectedColor)
    {
        // 1. Fenster schlieﬂen (Wichtig: Client-Seite)
        gameObject.SetActive(false);

        // 2. Auswahl an den Server senden
        Debug.Log($"[Client] Farbe {selectedColor} ausgew‰hlt.");
        GameManager.Instance.SubmitTrumpSelectionServerRpc(selectedColor);
    }
}