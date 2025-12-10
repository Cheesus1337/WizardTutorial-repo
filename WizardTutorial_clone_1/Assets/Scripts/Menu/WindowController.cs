using UnityEngine;
using UnityEngine.EventSystems;

public class WindowController : MonoBehaviour, IPointerDownHandler
{
    [Header("Settings")]
    [SerializeField] private RectTransform windowRectTransform;
    [SerializeField] private Vector2 minSize = new Vector2(300, 200);
    [SerializeField] private Vector2 maxSize = new Vector2(1920, 1080);

    // Speicher für Berechnungen
    private Vector2 originalLocalMousePos; // Maus-Startposition im PARENT-System
    private Vector2 originalSizeDelta;     // Fenster-Startgröße
    private Vector3 originalPanelPos;      // Fenster-Startposition
    private bool isResizing = false;

    private void Awake()
    {
        if (windowRectTransform == null) windowRectTransform = GetComponent<RectTransform>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        windowRectTransform.SetAsLastSibling(); // Fenster nach vorne holen

        // Startposition für Dragging merken (im Parent-System)
        RectTransform parentRect = windowRectTransform.parent as RectTransform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, eventData.pressEventCamera, out originalLocalMousePos);
        originalPanelPos = windowRectTransform.localPosition;
    }

    // --- WRAPPER FÜR EDITOR ---
    public void OnTitleBarPointerDown(BaseEventData data)
    {
        OnPointerDown((PointerEventData)data);
    }

    // --- 1. DRAGGING (Verschieben) ---
    public void OnTitleBarDrag(BaseEventData data)
    {
        if (isResizing) return;

        PointerEventData eventData = (PointerEventData)data;
        Vector2 currentMousePos;
        RectTransform parentRect = windowRectTransform.parent as RectTransform;

        // Wir berechnen die Mausposition im Parent-System
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, eventData.pressEventCamera, out currentMousePos))
        {
            Vector3 offset = currentMousePos - originalLocalMousePos;
            windowRectTransform.localPosition = originalPanelPos + offset;
        }
    }

    // --- 2. RESIZING (Vergrößern) ---
    public void OnResizeHandleDown(BaseEventData data)
    {
        PointerEventData eventData = (PointerEventData)data;
        isResizing = true;

        // WICHTIG: Wir merken uns die Mausposition im PARENT-System, nicht im Fenster selbst!
        RectTransform parentRect = windowRectTransform.parent as RectTransform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, eventData.pressEventCamera, out originalLocalMousePos);

        originalSizeDelta = windowRectTransform.sizeDelta;
        originalPanelPos = windowRectTransform.localPosition; // Position auch merken, falls Pivot nicht passt
    }

    public void OnResizeHandleDrag(BaseEventData data)
    {
        if (!isResizing) return;

        PointerEventData eventData = (PointerEventData)data;
        Vector2 currentMousePos;
        RectTransform parentRect = windowRectTransform.parent as RectTransform;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, eventData.pressEventCamera, out currentMousePos))
        {
            // Differenz berechnen
            Vector2 dragDelta = currentMousePos - originalLocalMousePos;

            // Neue Größe berechnen
            // X wächst nach rechts (+), Y wächst nach unten (in Unity UI ist unten oft minus, also -deltaY)
            float newWidth = originalSizeDelta.x + dragDelta.x;
            float newHeight = originalSizeDelta.y - dragDelta.y;

            // Limits anwenden
            newWidth = Mathf.Clamp(newWidth, minSize.x, maxSize.x);
            newHeight = Mathf.Clamp(newHeight, minSize.y, maxSize.y);

            windowRectTransform.sizeDelta = new Vector2(newWidth, newHeight);
        }
    }

    public void OnResizeHandleUp(BaseEventData data)
    {
        isResizing = false;
    }
}