using UnityEngine.Timeline;
using UnityEngine.Playables;

/// <summary>
/// プレイヤーの移動をオーバーライドするTimelineのトラック
/// </summary>
[TrackColor(1.0f, 0.3f, 0.0f)]
[TrackClipType(typeof(PlayerMoveOverrideClip))]
[TrackBindingType(typeof(PlayerMoveOverrideState))]
public class PlayerMoveOverrideTrack : TrackAsset
{
}