using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ModeSelectUI : MonoBehaviour
{
    [SerializeField, Tooltip("ギア回転コンポーネント")]
    private GearRotate _gearRotate;

    [SerializeField, Tooltip("フェードUI")]
    private FadeUI _fadeUI;

    [SerializeField, Tooltip("初期開始シーン")]
    private string _newGameSceneName;

    [SerializeField, Tooltip("ステージセレクトシーン")]
    private string _stageSelectSceneName;

    [SerializeField, Tooltip("タイトルシーン")]
    private string _titleSceneName;

    [SerializeField, Tooltip("選択ボタン")]
    private List<ModeSelectButton> _modeSelectButtons;

    [SerializeField, Tooltip("移動オフセット")]
    private Vector2 _moveOffset;

    [SerializeField, Tooltip("移動速度")]
    private float _moveSpeed = 10.0f;

    private List<Vector2> _defaultPosList;


    private void Start()
    {
        _defaultPosList = new List<Vector2>();
        foreach (var button in _modeSelectButtons)
        {
            _defaultPosList.Add(button.RectTransform.anchoredPosition);
        }
    }

    private void Update()
    {
        foreach (var button in _modeSelectButtons)
        {
            Vector2 targetPos;
            if (button.IsSelected)
            {
                targetPos = _defaultPosList[_modeSelectButtons.IndexOf(button)] + _moveOffset;
            }
            else
            {
                targetPos = _defaultPosList[_modeSelectButtons.IndexOf(button)];
            }

            Vector2 pos = Vector2.Lerp(button.RectTransform.anchoredPosition, targetPos, Time.deltaTime * _moveSpeed);
            button.RectTransform.anchoredPosition = pos;
        }
    }

    public void OnChangeSelectButton()
    {
        _gearRotate.RotateGears();
    }

    public void NewGame()
    {
        GameProgress.ResetProgress();
        _fadeUI.StartFadeOut(() => { SceneManager.LoadScene(_newGameSceneName); });
    }

    public void ContinueGame()
    {
        _fadeUI.StartFadeOut(() => { SceneManager.LoadScene(_stageSelectSceneName); });
    }

    public void ToTitle()
    {
        _fadeUI.StartFadeOut(() => { SceneManager.LoadScene(_titleSceneName); });
    }
}
