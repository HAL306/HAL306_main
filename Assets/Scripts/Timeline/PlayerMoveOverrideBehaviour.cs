using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// プレイヤーの移動をオーバーライドするTimelineのクリップの実行時処理
/// </summary>
public class PlayerMoveOverrideBehaviour : PlayableBehaviour
{
    public OverrideTimelineMode mode;
    public float targetX;
    public float speed;
    public string eventId;
    
    public GameObject directorObject;

    private PlayerMoveOverrideState _stateAsset;
    private bool _isClipActive = false;
    private bool _hasInitialized = false;
    private bool _callbackFired = false;
    
    private object _callbackComponent;
    private MethodInfo _callbackMethod;

    // クリップの再生範囲に入った時に呼ばれる
    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        _isClipActive = true;
        _hasInitialized = false;
        _callbackFired = false;
    }

    // クリップの再生範囲から出た時に呼ばれる
    public override void OnBehaviourPause(Playable playable, FrameData info)
    {
        _isClipActive = false;

        // まだコールバックが発火しておらず、初期化済みの場合、強制的にオーバーライドを解除する
        if (_stateAsset != null && !_callbackFired && _hasInitialized)
        {
            if (_stateAsset.MoveOverridingState != PlayerMoveOverrideState.OverrideState.None)
            {
                _stateAsset.StopOverride();
            }
        }
        
        _hasInitialized = false;
    }

    // クリップ再生中、毎フレーム呼ばれる
    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        if (!_isClipActive) return;

        // 初回フレームのみ初期化処理を実行
        if (!_hasInitialized)
        {
            _stateAsset = playerData as PlayerMoveOverrideState;
            if (_stateAsset != null)
            {
                InitializeOverride(playable);
            }
            _hasInitialized = true;
        }
    }

    private void InitializeOverride(Playable playable)
    {
        CacheTargetMethod();
        
        Action<bool> callback = (isCompleted) =>
        {
            _callbackFired = true;
            if (_callbackMethod != null && _callbackComponent != null)
            {
                _callbackMethod.Invoke(_callbackComponent, new object[] { eventId, isCompleted });
            }
        };

        if (mode == OverrideTimelineMode.Constant)
        {
            // Constantモードの場合、アセットの長さは強制解除のタイミングとして利用
            _stateAsset.SetOverrideConstant(targetX, speed, callback);
        }
        else if (mode == OverrideTimelineMode.ByTime)
        {
            // ByTimeモードの場合、クリップの長さをそのまま移動時間として利用
            float duration = (float)playable.GetDuration();
            _stateAsset.SetOverrideByTime(targetX, duration, callback);
        }
    }
    
    private void CacheTargetMethod()
    {
        if (directorObject == null) return;

        var components = directorObject.GetComponents<MonoBehaviour>();
        foreach (var comp in components)
        {
            var method = comp.GetType().GetMethod("OnPlayerMoveOverrideFinished", 
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (method != null)
            {
                _callbackComponent = comp;
                _callbackMethod = method;
                break;
            }
        }
    }
}