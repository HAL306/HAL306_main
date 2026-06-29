using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.Events;

public enum OverrideTimelineMode
{
    Constant,
    ByTime
}

[Serializable]
public class OverrideFinishedEvent : UnityEvent<bool> { }

/// <summary>
/// プレイヤーの移動をオーバーライドするTimelineのクリップ
/// </summary>
public class PlayerMoveOverrideClip : PlayableAsset, ITimelineClipAsset
{
    [Header("Override Settings")]
    [Tooltip("オーバーライドのモード")]
    public OverrideTimelineMode overrideMode = OverrideTimelineMode.Constant;

    [Tooltip("目標X座標")]
    public float targetX;

    [Tooltip("移動速度 (Constantモード時のみ使用)")]
    public float speed = 5f;

    [Tooltip("このクリップの識別ID")]
    public string eventId;

    public ClipCaps clipCaps => ClipCaps.None;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<PlayerMoveOverrideBehaviour>.Create(graph);
        var behaviour = playable.GetBehaviour();
        
        behaviour.mode = overrideMode;
        behaviour.targetX = targetX;
        behaviour.speed = speed;
        behaviour.eventId = eventId;
        behaviour.directorObject = owner;
        
        return playable;
    }
}