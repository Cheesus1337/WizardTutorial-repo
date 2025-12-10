using UnityEngine;
using UnityEngine.EventSystems; // Wichtig für den Doppelklick

public class ScoreboardScaler : MonoBehaviour, IPointerClickHandler
{
    [Header("Einstellungen")]
    [Tooltip("Auf welche Größe soll verkleinert werden? 0.7 bedeutet 70% der Originalgröße (also 30% kleiner).")]
    [Range(0.1f, 1.5f)]
    public float zielSkalierung = 0.7f; // Hier kannst du im Inspector 0.7, 0.75, etc. eingeben

    private Vector3 originalScale;
    private bool istVerkleinert = false;
    private RectTransform rectTransform;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        // Wir merken uns, wie groß das Objekt beim Start war (meistens 1, 1, 1)
        originalScale = rectTransform.localScale;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Prüfung auf Doppelklick (linke Maustaste)
        if (eventData.clickCount == 2 && eventData.button == PointerEventData.InputButton.Left)
        {
            ToggleScale();
        }
    }

    private void ToggleScale()
    {
        istVerkleinert = !istVerkleinert;

        if (istVerkleinert)
        {
            // Verkleinern: Wir setzen die Skalierung auf den eingestellten Faktor (x, y und z)
            rectTransform.localScale = originalScale * zielSkalierung;
        }
        else
        {
            // Zurücksetzen: Wir stellen die Originalgröße wieder her
            rectTransform.localScale = originalScale;
        }
    }
}