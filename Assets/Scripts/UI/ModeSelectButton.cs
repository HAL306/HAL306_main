using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class ModeSelectButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField, Tooltip("Ž©g‚ÌRectTransform")]
    private RectTransform _rectTransform;

    private bool _isSelected;


    public RectTransform RectTransform => _rectTransform;
    public bool IsSelected => _isSelected;


    public void OnPointerEnter(PointerEventData eventData)
    {
        _isSelected = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isSelected = false;
    }
}
