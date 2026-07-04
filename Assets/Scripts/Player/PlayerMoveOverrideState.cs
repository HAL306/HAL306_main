using System;
using UnityEngine;

/// <summary>
/// プレイヤー移動のオーバーライド状態を保持し、変化があった際にイベントを発火するデータコンテナ
/// </summary>
[CreateAssetMenu(fileName = "PlayerMoveOverrideState", menuName = "States/PlayerMoveOverrideState")]
public class PlayerMoveOverrideState : ScriptableObject
{
    public enum OverrideState
    {
        None,
        Speed,
        Time,
    }
    
    [SerializeField, Tooltip("プレイヤー移動のオーバーライド状態")] 
    private OverrideState _moveOverridingState;
    [SerializeField, Tooltip("目標X座標")] 
    private float _overrideMoveTargetX;
    [SerializeField, Tooltip("移動速度")] 
    private float _overrideMoveSpeed;
    [SerializeField, Tooltip("移動時間")] 
    private float _overrideMoveTime;
    [SerializeField, Tooltip("オーバーライド終了時に呼び出されるコールバック")] 
    private Action<bool> _currentCallback;
    
    /// <summary>
    /// プレイヤー移動のオーバーライド状態
    /// </summary>
    public OverrideState MoveOverridingState => _moveOverridingState;
    /// <summary>
    /// 目標X座標
    /// </summary>
    public float OverrideMoveTargetX => _overrideMoveTargetX;
    /// <summary>
    /// 移動速度
    /// </summary>
    public float OverrideMoveSpeed => _overrideMoveSpeed;
    /// <summary>
    /// 移動時間
    /// </summary>
    public float OverrideMoveTime
    {
        get => _overrideMoveTime;
        set => _overrideMoveTime = value;
    }

    /// <summary>
    /// 固定速度での移動オーバーライドを設定するメソッド
    /// </summary>
    /// <param name="targetX">移動の目標X座標</param>
    /// <param name="speed">移動速度</param>
    /// <param name="callback">オーバーライド終了時に呼び出されるコールバック</param>
    public void SetOverrideConstant(float targetX, float speed, Action<bool> callback)
    {
        if (_currentCallback != null)
        {
            _currentCallback(false);
        }
        
        _moveOverridingState = OverrideState.Speed;
        _overrideMoveTargetX = targetX;
        _overrideMoveSpeed = speed;
        _overrideMoveTime = 0f;
        _currentCallback = callback;
    }
    
    /// <summary>
    /// 固定時間での移動オーバーライドを設定するメソッド
    /// </summary>
    /// <param name="targetX">移動の目標X座標</param>
    /// <param name="time">移動速度</param>
    /// <param name="callback">オーバーライド終了時に呼び出されるコールバック</param>
    public void SetOverrideByTime(float targetX, float time, Action<bool> callback)
    {
        if (_currentCallback != null)
        {
            _currentCallback(false);
        }
        
        _moveOverridingState = OverrideState.Time;
        _overrideMoveTargetX = targetX;
        _overrideMoveSpeed = 0f;
        _overrideMoveTime = time;
        _currentCallback = callback;
    }

    /// <summary>
    /// オーバーライドを中断するメソッド
    /// </summary>
    public void StopOverride()
    {
        _moveOverridingState = OverrideState.None;
        _overrideMoveTargetX = 0f;
        _overrideMoveSpeed = 0f;
        _overrideMoveTime = 0f;
        
        if (_currentCallback != null)
        {
            _currentCallback(false);
        }
        
        _currentCallback = null;
    }
    
    /// <summary>
    /// <b><see cref="PlayerMove"/>以外での呼び出し禁止</b><br/>
    /// オーバーライドが完了した際に呼び出すメソッド
    /// </summary>
    public void FinishOverride()
    {
        _moveOverridingState = OverrideState.None;
        _overrideMoveTargetX = 0f;
        _overrideMoveSpeed = 0f;
        _overrideMoveTime = 0f;
        
        if (_currentCallback != null)
        {
            _currentCallback(true);
        }
        
        _currentCallback = null;
    }

    private void OnDisable()
    {
        // エディタ停止時の状態リセット
        _moveOverridingState = OverrideState.None;
        _overrideMoveTargetX = 0f;
        _overrideMoveSpeed = 0f;
        _overrideMoveTime = 0f;
        _currentCallback = null;
    }
}