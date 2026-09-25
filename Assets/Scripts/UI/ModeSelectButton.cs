using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class ModeSelectButton : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField, Tooltip("ホバー時の動作")]
    private UnityEvent _hoverAction;

    [SerializeField, Tooltip("決定時の動作")]
    private UnityEvent _buttonAction;

    private bool _isSelected;
    private RectTransform _rectTransform;

    public RectTransform RectTransform => _rectTransform;
    public bool IsSelected => _isSelected;


    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_buttonAction != null)
            _buttonAction.Invoke();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isSelected = true;
        if (_hoverAction != null)
            _hoverAction.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isSelected = false;
    }
}
