using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections;

public class StageSelectUI : MonoBehaviour
{
    [Header("参照登録")]
    [SerializeField, Tooltip("ステージポイント")]
    private List<StagePoint> _stagePoints;

    [SerializeField, Tooltip("選択ピン")]
    private GameObject _selectPin;

    [SerializeField, Tooltip("確認画面パネル")]
    private GameObject _confirmPanel;

    [SerializeField, Tooltip("ステージ情報UI")]
    private StageInfoUI _stageInfoUI;

    [Header("方向判定設定")]
    [SerializeField, Tooltip("上下方向判定の許容角度")]
    [Range(1f, 90f)]
    private float _directionAngleThreshold = 60f;

    [Header("InputAction登録")]
    [SerializeField, Tooltip("戻る")]
    private InputActionReference _cancelAction;

    [SerializeField, Tooltip("決定")]
    private InputActionReference _decideAction;

    [Header("ピンアニメーション設定")]
    [SerializeField, Tooltip("アニメーション開始時のオフセット（右上から刺さるように移動）")]
    private Vector2 _pinStartOffset = new Vector2(0.8f, 0.8f);

    [SerializeField, Tooltip("ピンが刺さるまでの時間")]
    private float _pinAnimDuration = 0.15f;

    [SerializeField, Tooltip("移動の速度カーブ")]
    private AnimationCurve _pinAnimCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [SerializeField, Tooltip("戻る先のシーン名")]
    private string _backSceneName;

    private Coroutine _pinAnimCoroutine;

    private StagePoint _currentSelect;

    public StagePoint CurrentSelect => _currentSelect;

    private void Awake()
    {
        int continueIndex = Mathf.Clamp(GameProgress.NextStageIndex, 0, _stagePoints.Count - 1);
        _currentSelect = _stagePoints.Find(sp => sp.StageIndex == continueIndex) ?? _stagePoints[0];
        _selectPin.transform.position = _currentSelect.transform.position;
    }

    private void OnEnable()
    {
        if (_cancelAction != null)
            _cancelAction.action.performed += OnCancel;
        if (_decideAction != null)
            _decideAction.action.performed += OnDecide;
    }

    private void OnDisable()
    {
        if (_cancelAction != null)
            _cancelAction.action.performed -= OnCancel;
        if (_decideAction != null)
            _decideAction.action.performed -= OnDecide;
    }

    private void Update()
    {
        if (_confirmPanel != null && _confirmPanel.activeSelf)
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
            StageSelect();

        if (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame)
            MoveHorizontal(-1);
        else if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame)
            MoveHorizontal(1);
        else if (Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame)
            MoveDirection(Vector2.up);
        else if (Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame)
            MoveDirection(Vector2.down);
    }

    // マウスクリックでの選択
    public void StageSelect()
    {
        Vector2 worldPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        foreach (StagePoint stagePoint in _stagePoints)
        {
            if (stagePoint.Collider.OverlapPoint(worldPos))
            {
                Select(stagePoint);
                return;
            }
        }
    }

    // 戻る入力受付（ゲームパッドBボタン等）
    public void OnCancel(InputAction.CallbackContext context)
    {
        GoBack();
    }

    // 決定入力受付（Enterキー・ゲームパッドAボタン等）
    public void OnDecide(InputAction.CallbackContext context)
    {
        Decide();
    }

    private void GoBack()
    {
        if (_confirmPanel != null && _confirmPanel.activeSelf)
            _confirmPanel.SetActive(false);
        else
            SceneManager.LoadScene(_backSceneName);
    }

    private void Decide()
    {
        if (_confirmPanel != null && _confirmPanel.activeSelf)
            _stageInfoUI.StageStart();
        else
            _stageInfoUI.OpenConfirm();
    }

    // X座標のみを見て左右方向に最も近いステージを選択する
    private void MoveHorizontal(int direction)
    {
        float currentX = _currentSelect.transform.position.x;
        StagePoint best = null;
        float bestDiff = float.MaxValue;

        foreach (StagePoint candidate in _stagePoints)
        {
            if (candidate == _currentSelect) continue;

            float diff = (candidate.transform.position.x - currentX) * direction;
            if (diff <= 0f) continue;

            if (diff < bestDiff)
            {
                bestDiff = diff;
                best = candidate;
            }
        }

        if (best != null)
            Select(best);
    }

    // 指定方向に最も近い（角度・距離基準）ステージを選択する
    private void MoveDirection(Vector2 direction)
    {
        Vector2 currentPos = _currentSelect.transform.position;
        StagePoint best = null;
        float bestScore = float.MaxValue;

        foreach (StagePoint candidate in _stagePoints)
        {
            if (candidate == _currentSelect) continue;

            Vector2 offset = (Vector2)candidate.transform.position - currentPos;
            float angle = Vector2.Angle(direction, offset);
            if (angle > _directionAngleThreshold) continue;

            float score = offset.magnitude + angle * 0.05f;
            if (score < bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        if (best != null)
            Select(best);
    }

    private void Select(StagePoint stagePoint)
    {
        _currentSelect = stagePoint;

        if (_pinAnimCoroutine != null)
            StopCoroutine(_pinAnimCoroutine);

        _pinAnimCoroutine = StartCoroutine(AnimatePin(stagePoint.transform.position));
    }

    private IEnumerator AnimatePin(Vector3 target)
    {
        Vector3 start = target + (Vector3)_pinStartOffset;
        float elapsed = 0f;

        while (elapsed < _pinAnimDuration)
        {
            elapsed += Time.deltaTime;
            float t = _pinAnimCurve.Evaluate(Mathf.Clamp01(elapsed / _pinAnimDuration));
            _selectPin.transform.position = Vector3.Lerp(start, target, t);
            yield return null;
        }

        _selectPin.transform.position = target;
    }
}