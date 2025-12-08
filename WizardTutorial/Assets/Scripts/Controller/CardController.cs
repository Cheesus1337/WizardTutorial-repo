using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardController : MonoBehaviour , IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler

{

    [SerializeField] private TextMeshProUGUI valueTopRight;
    [SerializeField] private TextMeshProUGUI valueBottomLeft;

    private Vector3 _originalScale;
    private Vector3 _hoverScale;
    private Outline _outline;

    private void Awake()
    {
        _outline = this.GetComponent<Outline>();
        _originalScale = Vector3.one;
        _hoverScale = _originalScale * 1.1f; // Scale up by 10% on hover
    }


    public void OnPointerEnter(PointerEventData eventData)
    {

        Debug.Log("Pointer entered card area.");
        this.transform.localScale = _hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log("Pointer exited card area.");
        this.transform.localScale = _originalScale;
    }



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        valueTopRight.text = "1";
        valueBottomLeft.text = "1";
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("Card clicked!");
        SelectionAnimation();

    }

    private void SelectionAnimation()
        {
        if(_outline == null)
        {
            Debug.Log("Object " + name + " has no Outline component.");
            return;
        }
        // Toggle the outline effect - invert its current state
        _outline.enabled = !_outline.enabled;
   
    }
}
