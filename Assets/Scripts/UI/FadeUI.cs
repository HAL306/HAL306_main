using System;
using UnityEngine;
using UnityEngine.UI;

public class FadeUI : MonoBehaviour
{
    [SerializeField, Tooltip("フェード画像")]
    private Image _image;

    [SerializeField, Tooltip("開始直後のフェードイン待ち時間")]
    private float _fadeDelay = 0.1f;

    [SerializeField,Tooltip("フェードインの速度")]
    private float _fadeInSpeed = 1.0f;

    [SerializeField, Tooltip("フェードアウトの速度")]
    private float _fadeOutSpeed = 1.0f;

    [SerializeField, Tooltip("フェード割合")]
    private float _fadeRatio = 1.0f;

    [SerializeField, Tooltip("フェードインカーブ")]
    private AnimationCurve _fadeInCurve;

    [SerializeField, Tooltip("フェードアウトカーブ")]
    private AnimationCurve _fadeOutCurve;

    [SerializeField, Tooltip("フェードの初期状態")]
    private FadeState _fadeState = FadeState.FADE_IN;

    [SerializeField, Tooltip("タイムスケールの影響を受けるかどうか")]
    private bool _useTimeScale = true;

    private RectTransform imageRect;   // フェード画像のrectTransform

    enum FadeState
    {
        NONE, FADE_IN, FADE_OUT,
    }

    private Action _finishAction;

    public bool StartFadeIn()
    {
        if (_fadeState != FadeState.NONE)
            return false;

        _fadeState = FadeState.FADE_IN;
        return true;
    }

    public bool StartFadeOut(Action finishAction = null)
    {
        if (_fadeState != FadeState.NONE)
            return false;

        _fadeState = FadeState.FADE_OUT;
        _finishAction = finishAction;
        return true;
    }

    public void StartFadeOutNoReturn()
    {
        _fadeState = FadeState.FADE_OUT;
    }

    // フェードの中心を設定する
    public void SetPosition(Vector3 pos)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
            return;
        Vector3 screenPos = mainCamera.WorldToScreenPoint(pos);
        screenPos.z = 0.0f;

        // 画像を移動させる
        imageRect.position = screenPos;
    }

    // フェード終了時に呼ばれる関数を登録する
    public void SetFinishAction(Action action)
    {
        _finishAction += action;
    }

    private void Start()
    {
        _image.material.SetFloat("_FadeRatio", _fadeRatio);
        imageRect = _image.gameObject.GetComponent<RectTransform>();
    }

    private void Update()
    {
        _fadeDelay = Mathf.Max(0.0f, _fadeDelay - Time.deltaTime);
        if (_fadeDelay > 0.0f)
            return;

        float materialFadeRatio = 0.0f;

        float deltaTime = 0.0f;
        if (_useTimeScale)
        {
            deltaTime = Time.deltaTime;
        }
        else
        {
            deltaTime = Time.unscaledDeltaTime;
        }

        // フェード更新
        switch (_fadeState)
        {
            case FadeState.NONE:
                break;

            case FadeState.FADE_IN:
                _fadeRatio = Mathf.Max(_fadeRatio - _fadeInSpeed * deltaTime, 0.0f);
                materialFadeRatio = _fadeInCurve.Evaluate(_fadeRatio);
                break;

            case FadeState.FADE_OUT:
                _fadeRatio = Mathf.Min(_fadeRatio + _fadeOutSpeed * deltaTime, 1.0f);
                materialFadeRatio = _fadeOutCurve.Evaluate(1.0f - _fadeRatio);
                break;
        }

        
        _image.material.SetFloat("_FadeRatio", materialFadeRatio);

        // フェード終了処理
        if (_fadeState == FadeState.FADE_IN && _fadeRatio <= 0.0f)
        {
            _fadeState = FadeState.NONE;
        }
        if (_fadeState == FadeState.FADE_OUT && _fadeRatio >= 1.0f)
        {
            _fadeState = FadeState.NONE;
            if(_finishAction != null)
                _finishAction.Invoke();
        }
    }
}
