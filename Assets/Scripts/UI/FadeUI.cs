using System;
using UnityEngine;
using UnityEngine.UI;

public class FadeUI : MonoBehaviour
{
    [SerializeField, Tooltip("フェード画像")]
    private Image _image;

    [SerializeField,Tooltip("フェードインの速度")]
    private float _fadeInSpeed = 1.0f;

    [SerializeField, Tooltip("フェードアウトの速度")]
    private float _fadeOutSpeed = 1.0f;

    [SerializeField, Tooltip("フェード割合")]
    private float _fadeRatio = 1.0f;

    enum FadeState
    {
        NONE, FADE_IN, FADE_OUT,
    }

    private FadeState _fadeState = FadeState.FADE_IN;
    private Action _finishAction;

    public void StartFadeIn()
    {
        if (_fadeState == FadeState.FADE_IN)
            return;

        _fadeState = FadeState.FADE_IN;
    }

    public void StartFadeOut(Action finishAction)
    {
        if (_fadeState == FadeState.FADE_OUT)
            return;

        _fadeState = FadeState.FADE_OUT;
        _finishAction = finishAction;
    }

    private void Update()
    {
        // フェード更新
        switch (_fadeState)
        {
        case FadeState.NONE:
            break;

        case FadeState.FADE_IN:
            _fadeRatio = Mathf.Max(_fadeRatio - _fadeInSpeed * Time.deltaTime, 0.0f);
            break;

        case FadeState.FADE_OUT:
            _fadeRatio = Mathf.Min(_fadeRatio + _fadeOutSpeed * Time.deltaTime, 1.0f);
            break;
        }
        Color color = _image.color;
        color.a = _fadeRatio;
        _image.color = color;

        // フェード終了処理
        if (_fadeState == FadeState.FADE_IN && _fadeRatio <= 0.0f)
        {
            _fadeState = FadeState.NONE;
        }
        if (_fadeState == FadeState.FADE_OUT && _fadeRatio >= 1.0f)
        {
            _fadeState = FadeState.NONE;
            _finishAction.Invoke();
        }
    }
}
