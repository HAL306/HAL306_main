using System;
using UnityEngine;

/// <summary>
/// カットシーンの再生リクエストを中継するイベントチャンネル
/// </summary>
[CreateAssetMenu(fileName = "PlayCutsceneEventChannel", menuName = "Events/PlayCutsceneEventChannel")]
public class PlayCutsceneEventChannel : ScriptableObject
{
    // カットシーンのハッシュIDを渡す
    public event Action<int> OnPlayRequested;

    /// <summary>
    /// カットシーンを再生する
    /// </summary>
    /// <param name="cutsceneHash">再生するカットシーンのハッシュID</param>
    public void PlayCutscene(int cutsceneHash)
    {
        OnPlayRequested?.Invoke(cutsceneHash);
    }
    
    /// <summary>
    /// カットシーンを再生する
    /// </summary>
    /// <param name="cutsceneIDString">再生するカットシーンのID文字列</param>
    public void PlayCutscene(string cutsceneIDString)
    {
        OnPlayRequested?.Invoke(Animator.StringToHash(cutsceneIDString));
    }
}