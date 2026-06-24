using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class ModeSelectButton : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField, Tooltip("このボタンのRectTransform")]
    private RectTransform _rectTransform;

    [SerializeField, Tooltip("選択項目の拡大率")]
    private float _selectScale = 1.5f;

    [SerializeField, Tooltip("選択項目の拡大率の変化速度")]
    private float _selectScaleSpeed = 10.0f;

    [SerializeField, Tooltip("決定時の動作")]
    private UnityEvent _buttonAction;

    private bool _isSelected;


    public RectTransform RectTransform => _rectTransform;
    public bool IsSelected => _isSelected;


    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    private void Update()
    {
        // 拡大率を更新する
        Vector3 scale = _rectTransform.localScale;
        float targetScale = (_isSelected) ? _selectScale : 1.0f;
        if (Application.isPlaying)
        {
            scale.x = Mathf.MoveTowards(scale.x, targetScale, Time.deltaTime * _selectScaleSpeed);
            scale.y = Mathf.MoveTowards(scale.y, targetScale, Time.deltaTime * _selectScaleSpeed);
        }
        else
        {
            scale.x = targetScale;
            scale.y = targetScale;
        }
        _rectTransform.localScale = scale;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_buttonAction != null)
            _buttonAction.Invoke();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isSelected = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isSelected = false;
    }
}
