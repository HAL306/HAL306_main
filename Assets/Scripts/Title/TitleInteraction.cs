using UnityEngine;

public class TitleInteraction : MonoBehaviour
{
    [SerializeField, Tooltip("入力無効時間")]
    private float _invalidTime = 1.0f;

    [SerializeField, Tooltip("タイトルUIアニメーター")]
    private Animator _titleUIAnimator;

    [SerializeField, Tooltip("タイトルカメラアニメーター")]
    private Animator _titleCameraAnimator;

    private void Update()
    {
        _invalidTime -= Time.deltaTime;
    }

    public void GameStart()
    {
        if (_invalidTime > 0.0f)
            return;

        _titleUIAnimator.SetTrigger("Transition");
        _titleCameraAnimator.SetTrigger("Transition");
    }
}
