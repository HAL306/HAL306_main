using UnityEngine;
using System.Collections.Generic;
using System;

public class ModeSelectUI : MonoBehaviour
{
    [SerializeField, Tooltip("整列要素")]
    private List<ModeSelectButton> _layoutElements;

    [SerializeField, Tooltip("配置間隔")]
    private float _layoutSpace = 0.0f;

    [SerializeField, Tooltip("選択項目の拡大率")]
    private float _selectScale = 1.5f;

    [SerializeField, Tooltip("選択項目の拡大率の変化速度")]
    private float _selectScaleSpeed = 10.0f;
    
    [SerializeField, Tooltip("選択中の項目")]
    private int _selectIndex = 0;

    public Action buttonAction;

    public int SelectIndex => _selectIndex;

    const int Select_None = -1;

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
        // 選択中の項目の拡大率を更新する
        for (int i = 0; i < _layoutElements.Count; ++i)
        {
            Vector3 scale = _layoutElements[i].RectTransform.localScale;
            float targetScale = (_layoutElements[i].IsSelected) ? _selectScale : 1.0f;
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
            _layoutElements[i].RectTransform.localScale = scale;
        }

        // 整列要素の高さの合計を求める
        float sumHeight = 0.0f;
        for (int i = 0; i < _layoutElements.Count; ++i)
        {
            sumHeight += _layoutElements[i].RectTransform.rect.height * _layoutElements[i].RectTransform.lossyScale.y;
        }
        sumHeight += _layoutSpace * (_layoutElements.Count - 1);
        float currentPosY = this.transform.position.y + sumHeight * 0.5f;

        // 整列要素を中央に配置する
        for (int i = 0; i < _layoutElements.Count; ++i)
        {
            float hs = _layoutElements[i].RectTransform.rect.height * _layoutElements[i].RectTransform.lossyScale.y * 0.5f;
            currentPosY -= hs;
            currentPosY -= _layoutSpace * 0.5f;

            Vector3 pos = _layoutElements[i].RectTransform.position;
            pos.y = currentPosY;
            _layoutElements[i].RectTransform.position = pos;

            currentPosY -= hs;
        }
    }
}
