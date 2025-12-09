using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static CardEnums;

public class CardController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("UI References")]
    [SerializeField] private Image cardImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TextMeshProUGUI valueTopRight;
    [SerializeField] private TextMeshProUGUI valueBottomLeft;

    [Header("Settings")]
    [SerializeField] private CardThemeSO cardTheme;

    // Die Daten der Karte
    private CardData _myCardData;

    // Für Animation
    private Vector3 _originalScale;
    private Vector3 _hoverScale;
    private Outline _outline;

    private void Awake()
    {
        _outline = this.GetComponent<Outline>();
        _originalScale = transform.localScale;
        if (_originalScale == Vector3.zero) _originalScale = Vector3.one;
        _hoverScale = _originalScale * 1.1f;
    }

    // WICHTIG: KEINE Start() Methode hier! 
    // Start() würde nach Initialize() laufen und könnte Daten überschreiben.

    public void Initialize(CardData data)
    {
        _myCardData = data;
        Debug.Log($"CardController initialized mit: {_myCardData.color} {_myCardData.value}");
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        // Debugging: Prüfen ob Referenzen da sind
        if (valueTopRight == null) Debug.LogError($"{name}: 'Value Top Right' fehlt im Inspector!");
        if (cardTheme == null) Debug.LogError($"{name}: 'Card Theme' fehlt im Inspector!");

        // 1. Text setzen
        string textValue = GetValueString(_myCardData.value);

        if (valueTopRight != null) valueTopRight.text = textValue;
        if (valueBottomLeft != null) valueBottomLeft.text = textValue;

        // 2. Grafik & Farbe setzen
        if (cardTheme != null)
        {
            Color c = cardTheme.GetColor(_myCardData.color);

            if (valueTopRight != null) valueTopRight.color = c;
            if (valueBottomLeft != null) valueBottomLeft.color = c;

            if (cardImage != null)
            {
                cardImage.color = c;

                // Bild aus Liste holen
                int spriteIndex = (int)_myCardData.value - 1;
                if (cardTheme.valueSprites != null && spriteIndex >= 0 && spriteIndex < cardTheme.valueSprites.Count)
                {
                    cardImage.sprite = cardTheme.valueSprites[spriteIndex];
                }
            }
        }
    }

    private string GetValueString(CardValue val)
    {
        if (val == CardValue.Jester) return "N";
        if (val == CardValue.Wizard) return "Z";
        return ((int)val).ToString();
    }

    // --- Maus Events ---

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
        // Hier prüfen wir, ob die Daten noch da sind
        Debug.Log($"Karte angeklickt! Daten: {_myCardData.color} {_myCardData.value} (Sollte nicht Red 0 sein)");
        if (_outline != null) _outline.enabled = !_outline.enabled;
    }
}