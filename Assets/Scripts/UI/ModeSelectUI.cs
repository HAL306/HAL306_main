using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ModeSelectUI : MonoBehaviour
{
    [SerializeField, Tooltip("フェードUI")]
    private FadeUI _fadeUI;

    [SerializeField, Tooltip("整列要素")]
    private List<ModeSelectButton> _layoutElements;

    [SerializeField, Tooltip("配置間隔")]
    private float _layoutSpace = 0.0f;

    private void Update()
    {
        UpdateLayout();
    }

    private void OnValidate()
    {
        UpdateLayout();
    }

    private void UpdateLayout()
    {
        // 整列要素の高さの合計を求める
        float sumHeight = 0.0f;
        for (int i = 0; i < _layoutElements.Count; ++i)
        {
            sumHeight += _layoutElements[i].RectTransform.rect.height * _layoutElements[i].RectTransform.lossyScale.y;
        }
        sumHeight += _layoutSpace * (_layoutElements.Count - 1);
        float currentPosY = sumHeight * 0.5f;

        // 整列要素を中央に配置する
        for (int i = 0; i < _layoutElements.Count; ++i)
        {
            float hs = _layoutElements[i].RectTransform.rect.height * _layoutElements[i].RectTransform.lossyScale.y * 0.5f;
            currentPosY -= hs;
            currentPosY -= _layoutSpace * 0.5f;

            Vector3 localPos = _layoutElements[i].RectTransform.localPosition;
            localPos.y = currentPosY;
            _layoutElements[i].RectTransform.localPosition = localPos;

            currentPosY -= hs;
        }
    }

    public void NewGame()
    {
        _fadeUI.StartFadeOut(() => { SceneManager.LoadScene("StageSelectScene"); });
    }
}
