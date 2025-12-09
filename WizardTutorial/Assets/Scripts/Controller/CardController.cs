using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static CardEnums;
using System;

public class CardController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("UI References")]
    [SerializeField] private Image cardImage;      // Das Hauptbild der Karte (z.B. Zauberer-Icon)
    [SerializeField] private Image backgroundImage; // Der farbige Hintergrund/Rahmen
    [SerializeField] private TextMeshProUGUI valueTopRight;
    [SerializeField] private TextMeshProUGUI valueBottomLeft;

    [Header("Settings")]
    [SerializeField] private CardThemeSO cardTheme; // Verweis auf deine Grafiken

    [Header("Data")]
    // Wir speichern hier die echten Daten der Karte
    public CardData cardData;

    // Referenz auf unser Grafik-Thema (muss im Inspector zugewiesen oder per Code geladen werden)

    // Interne Daten
    private CardData _myCardData;
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
        // 1. Text setzen (Zahl oder Buchstabe)
        string textValue = GetDisplayString(_myCardData.value);
        if (valueTopRight) valueTopRight.text = textValue;
        if (valueBottomLeft) valueBottomLeft.text = textValue;

        // 2. Grafik & Farbe setzen (falls Theme vorhanden)
        if (cardTheme != null)
        {
            Color c = cardTheme.GetColor(_myCardData.color);

            // Texte einfärben
            if (valueTopRight) valueTopRight.color = c;
            if (valueBottomLeft) valueBottomLeft.color = c;

            // Optional: Bild setzen (falls du Icons hast)
            // Hier könnten wir später cardImage.sprite = ... setzen
            if (cardImage != null)
            {
                cardImage.color = c; // Färbt das Mittelbild in der Kartenfarbe
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