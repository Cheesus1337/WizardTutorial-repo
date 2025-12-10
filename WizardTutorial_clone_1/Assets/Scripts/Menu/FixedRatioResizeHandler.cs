using UnityEngine;
using UnityEngine.EventSystems;

public class FixedRatioResizeHandler : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    [Header("Settings")]
    public Vector2 minSize = new Vector2(300, 400); // Passe das an deine Startgröße an!
    public Vector2 maxSize = new Vector2(1000, 1200);

    private RectTransform panelRectTransform;
    private Vector2 originalLocalPointerPosition;
    private Vector2 originalSizeDelta;
    private Vector3 originalPanelPosition;
    private float aspectRatio; // Das Verhältnis Breite zu Höhe

    void Awake()
    {
        // Wir holen uns das Panel (das Eltern-Objekt des Griffs)
        panelRectTransform = transform.parent.GetComponent<RectTransform>();
    }

    public void OnPointerDown(PointerEventData data)
    {
        // Panel nach vorne holen
        panelRectTransform.SetAsLastSibling();

        // Startwerte speichern
        RectTransformUtility.ScreenPointToLocalPointInRectangle(panelRectTransform, data.position, data.pressEventCamera, out originalLocalPointerPosition);
        originalSizeDelta = panelRectTransform.sizeDelta;
        originalPanelPosition = panelRectTransform.localPosition;

        // WICHTIG: Wir merken uns das aktuelle Verhältnis beim Klick!
        // Beispiel: Breite 400 / Höhe 600 = 0.66
        aspectRatio = originalSizeDelta.x / originalSizeDelta.y;
    }

    public void OnDrag(PointerEventData data)
    {
        if (panelRectTransform == null) return;

        Vector2 localPointerPosition;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(panelRectTransform, data.position, data.pressEventCamera, out localPointerPosition))
        {
            Vector3 offsetToOriginal = localPointerPosition - originalLocalPointerPosition;

            // 1. Neue Breite berechnen (Wir lassen die Breite führen)
            float newWidth = originalSizeDelta.x + offsetToOriginal.x;

            // 2. Neue Höhe BASIEREND auf der Breite berechnen (Ratio erzwingen!)
            // Formel: Höhe = Breite / Ratio
            float newHeight = newWidth / aspectRatio;

            // 3. Limits prüfen (für beide Achsen)
            if (newWidth < minSize.x || newHeight < minSize.y)
            {
                newWidth = Mathf.Max(minSize.x, newHeight * aspectRatio);
                newHeight = newWidth / aspectRatio;
            }
            if (newWidth > maxSize.x || newHeight > maxSize.y)
            {
                newWidth = Mathf.Min(maxSize.x, newHeight * aspectRatio);
                newHeight = newWidth / aspectRatio;
            }

            // 4. Tatsächliche Änderung ermitteln
            float widthChange = newWidth - originalSizeDelta.x;
            float heightChange = newHeight - originalSizeDelta.y;

            // 5. Größe anwenden
            panelRectTransform.sizeDelta = new Vector2(newWidth, newHeight);

            // 6. POSITION KORRIGIEREN (Pivot Center Fix)
            // Verschiebt das Panel, damit es so aussieht, als würde nur die Ecke gezogen
            float xPosOffset = widthChange * 0.5f;
            float yPosOffset = heightChange * -0.5f;

            panelRectTransform.localPosition = originalPanelPosition + new Vector3(xPosOffset, yPosOffset, 0);
        }
    }
}