using UnityEngine;

/// <summary>
/// カットシーンの再生状態を保持し、変化があった際にイベントを発火するデータコンテナ
/// </summary>
[CreateAssetMenu(fileName = "CutsceneState", menuName = "States/CutsceneState")]
public class CutsceneState : ScriptableObject
{
    [SerializeField, Tooltip("現在再生中のカットシーンのハッシュID（再生されていない場合は0）")] 
    private int currentCutsceneHash = 0;

    /// <summary>
    /// カットシーンが開始した際に呼び出される
    /// </summary>
    /// <param name="cutsceneHash">開始されたカットシーンのハッシュID</param>
    public delegate void CutsceneStartedHandler(int cutsceneHash);

    /// <summary>
    /// カットシーンが終了または中断した際に呼び出される
    /// </summary>
    /// <param name="cutsceneHash">終了したカットシーンのハッシュID</param>
    /// <param name="isCompleted">最後まで再生された場合は true、中断された場合は false</param>
    public delegate void CutsceneStoppedHandler(int cutsceneHash, bool isCompleted);

    public event CutsceneStartedHandler OnCutsceneStarted;
    public event CutsceneStoppedHandler OnCutsceneStopped;

    /// <summary>
    /// カットシーンが再生中か
    /// </summary>
    public bool IsPlaying => currentCutsceneHash != 0;
    
    /// <summary>
    /// 現在再生中のカットシーンのハッシュID（再生されていなければ0）
    /// </summary>
    public int CurrentCutsceneHash => currentCutsceneHash;

    /// <summary>
    /// 再生状態を更新するメソッド（再生開始時）
    /// </summary>
    /// <param name="cutsceneHash">再生開始したカットシーンのハッシュID</param>
    public void SetPlaying(int cutsceneHash)
    {
        if (currentCutsceneHash == cutsceneHash) return;
        currentCutsceneHash = cutsceneHash;
        OnCutsceneStarted?.Invoke(currentCutsceneHash);
    }

    /// <summary>
    /// 再生状態を更新するメソッド（再生終了・中断時）
    /// </summary>
    /// <param name="cutsceneHash">再生終了したカットシーンのハッシュID</param>
    /// <param name="isCompleted">最後まで再生されたか</param>
    public void StopPlaying(int cutsceneHash, bool isCompleted)
    {
        if (currentCutsceneHash == 0) return;
        if (currentCutsceneHash == cutsceneHash)
        {
            currentCutsceneHash = 0;
        }
        OnCutsceneStopped?.Invoke(cutsceneHash, isCompleted); 
    }

    private void OnDisable()
    {
        // エディタ停止時の状態リセット
        currentCutsceneHash = 0;
    }
}