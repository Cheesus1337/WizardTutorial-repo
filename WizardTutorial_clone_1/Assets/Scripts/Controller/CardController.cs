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

    // NEU: Damit wir die Karte (z.B. als Trumpf) kleiner machen können
    // und sie auch nach dem Hover/Klick klein bleibt.
    public void SetBaseScale(float scaleFactor)
    {
        _originalScale = Vector3.one * scaleFactor;
        _hoverScale = _originalScale * 1.1f; // Hover bleibt 10% größer als die neue Basis
        transform.localScale = _originalScale; // Sofort anwenden
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
        // 1. Debugging: Was passiert hier?
        Debug.Log($"Klick auf: {_myCardData.color} {_myCardData.value}. Phase: {GameManager.Instance.currentGameState.Value}");

        // 2. Check: Spielphase
        if (GameManager.Instance == null || GameManager.Instance.currentGameState.Value != GameState.Playing)
        {
            Debug.Log("Klick ignoriert: Nicht die Spielphase.");
            return;
        }

        // 3. Server Call
        // Wir casten die Enums zu ints, da RPCs einfache Typen mögen
        GameManager.Instance.PlayCardServerRpc((int)_myCardData.color, (int)_myCardData.value);

        // Outline ausmachen (optional, da Karte eh zerstört wird)
        if (_outline != null) _outline.enabled = false;
    }

    public bool CardDataEquals(CardData other)
    {
        return _myCardData.color == other.color && _myCardData.value == other.value;
    }
}