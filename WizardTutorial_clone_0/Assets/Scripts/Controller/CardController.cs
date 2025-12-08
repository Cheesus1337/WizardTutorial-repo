using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static CardEnums;

public class CardController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("UI References")]
    [SerializeField] private Image cardImage;      // Das Hauptbild der Karte (z.B. Zauberer-Icon)
    [SerializeField] private Image backgroundImage; // Der farbige Hintergrund/Rahmen
    [SerializeField] private TextMeshProUGUI valueTopRight;
    [SerializeField] private TextMeshProUGUI valueBottomLeft;

    [Header("Data")]
    // Wir speichern hier die echten Daten der Karte
    public CardData cardData;

    // Referenz auf unser Grafik-Thema (muss im Inspector zugewiesen oder per Code geladen werden)
    [SerializeField] private CardThemeSO cardTheme;

    private Vector3 _originalScale;
    private Vector3 _hoverScale;
    private Outline _outline;

    private void Awake()
    {
        _outline = this.GetComponent<Outline>();
        _originalScale = Vector3.one; // Standardgröße merken (Vorsicht: Falls Prefab skaliert ist, hier transform.localScale nutzen)
        // Sicherheitshalber: Falls das Objekt im Editor schon skaliert ist, nehmen wir diese Skalierung als Basis
        if (transform.localScale != Vector3.one) _originalScale = transform.localScale;

        _hoverScale = _originalScale * 1.1f;
    }

    // Diese Methode wird vom Spiel aufgerufen, wenn die Karte "geboren" wird
    public void Initialize(CardData data)
    {
        this.cardData = data;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (cardTheme == null)
        {
            Debug.LogError($"Kein CardThemeSO im CardController '{name}' zugewiesen!");
            return;
        }

        // 1. Texte setzen
        string displayValue = GetDisplayString(cardData.value);
        valueTopRight.text = displayValue;
        valueBottomLeft.text = displayValue;

        // 2. Farben setzen
        Color cardColor = cardTheme.GetColor(cardData.color);

        // Texte einfärben
        valueTopRight.color = cardColor;
        valueBottomLeft.color = cardColor;

        // Optional: Wenn du den Hintergrund einfärben willst (abhängig von deinem Design)
        if (backgroundImage != null)
        {
            backgroundImage.color = Color.white; // Oder cardColor, je nach Design-Wunsch
        }

        // 3. Bild setzen (Sprite)
        if (cardImage != null)
        {
            // Wir holen uns das Bild basierend auf dem Wert (Index im Enum casten)
            // Achtung: Das setzt voraus, dass die Liste im SO exakt die gleiche Reihenfolge hat wie das Enum!
            int spriteIndex = (int)cardData.value - 1; // -1 weil Enum bei 1 anfängt, Liste bei 0

            if (spriteIndex >= 0 && spriteIndex < cardTheme.valueSprites.Count)
            {
                cardImage.sprite = cardTheme.valueSprites[spriteIndex];
                cardImage.color = cardColor; // Das Bild in der Farbe der Karte tönen
            }
            else
            {
                Debug.LogWarning($"Kein Sprite für Index {spriteIndex} ({cardData.value}) gefunden.");
            }
        }
    }

    // Hilfsfunktion: Wandelt den Enum-Wert in kurzen Text um (1-13, N, Z)
    private string GetDisplayString(CardValue val)
    {
        if (val == CardValue.Jester) return "N";
        if (val == CardValue.Wizard) return "Z";
        return ((int)val).ToString();
    }

    // --- Interaction Events (bleiben fast gleich) ---

    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.localScale = _hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = _originalScale;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"Karte angeklickt: {cardData.color} {cardData.value}");
        SelectionAnimation();
    }

    private void SelectionAnimation()
    {
        if (_outline != null)
        {
            _outline.enabled = !_outline.enabled;
        }
    }
}