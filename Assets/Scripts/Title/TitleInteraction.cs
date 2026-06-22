using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleInteraction : MonoBehaviour
{
    [SerializeField, Tooltip("入力無効時間")]
    private float _invalidTime = 1.0f;

    [SerializeField, Tooltip("遷移時間")]
    private float _transitionTime = 1.0f;

    [SerializeField, Tooltip("タイトルUIアニメーター")]
    private Animator _titleUIAnimator;

    [SerializeField, Tooltip("タイトルカメラアニメーター")]
    private Animator _titleCameraAnimator;

    [SerializeField, Tooltip("次のシーン名")]
    private string _nextSceneName;

    private bool _transitionFlag = false;

    private void Update()
    {
        _invalidTime -= Time.deltaTime;

        if(_transitionFlag)
        {
            _transitionTime -= Time.deltaTime;
            if (_transitionTime < 0.0f)
            {
                SceneManager.LoadScene(_nextSceneName);
            }
        }
    }

    public void GameStart()
    {
        if (_invalidTime > 0.0f)
            return;

        _titleUIAnimator.SetTrigger("Transition");
        _titleCameraAnimator.SetTrigger("Transition");
        _transitionFlag = true;
    }
}
