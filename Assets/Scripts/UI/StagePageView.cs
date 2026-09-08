using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StagePageView : MonoBehaviour
{
    [SerializeField] private RectTransform _panelRect;
    [SerializeField] private TMP_Text _stageInfoText;
    [SerializeField] private Image _stageImageUI;

    [Header("登場アニメーション設定")]
    [SerializeField] private Vector2 _enterOffset = new Vector2(400f, -300f);
    [SerializeField] private float _enterRotation = -25f;
    [SerializeField] private float _enterDuration = 0.35f;
    [SerializeField] private AnimationCurve _enterCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [SerializeField] private Button _goButton;
    public Button GoButton => _goButton;

    private Vector2 _restPosition;

    private void Awake()
    {
        _restPosition = _panelRect.anchoredPosition;
    }

    public void SetContent(string infoText, Sprite image)
    {
        _stageInfoText.text = infoText;
        if (_stageImageUI != null)
            _stageImageUI.sprite = image;
    }

    public void PlayEnterAnimation()
    {
        StartCoroutine(EnterAnimation());
    }

    private IEnumerator EnterAnimation()
    {
        Vector2 startPos = _restPosition + _enterOffset;
        float elapsed = 0f;

        while (elapsed < _enterDuration)
        {
            elapsed += Time.deltaTime;
            float t = _enterCurve.Evaluate(Mathf.Clamp01(elapsed / _enterDuration));

            _panelRect.anchoredPosition = Vector2.Lerp(startPos, _restPosition, t);
            _panelRect.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(_enterRotation, 0f, t));

            yield return null;
        }

        _panelRect.anchoredPosition = _restPosition;
        _panelRect.localEulerAngles = Vector3.zero;
    }
}