using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// カットシーン再生リクエストを受け取り、自身のIDと一致したらTimelineを再生するコンポーネント
/// </summary>
[RequireComponent(typeof(PlayableDirector))]
public class CutsceneListener : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField, Tooltip("このカットシーンの固有ID")] 
    private string cutsceneIDString; 
    
    [Header("References")]
    [SerializeField] private PlayCutsceneEventChannel eventChannel;
    [SerializeField] private CutsceneState cutsceneState;

    private int _cutsceneHash;
    private PlayableDirector _director;

    private void Awake()
    {
        _director = GetComponent<PlayableDirector>();
    }

    private void Start()
    {
        _cutsceneHash = Animator.StringToHash(cutsceneIDString);
        _director.RebuildGraph();
    }

    private void OnEnable()
    {
        eventChannel.OnPlayRequested += OnPlayRequested;
        _director.played += OnDirectorPlayed;
        _director.stopped += OnDirectorStopped;
    }

    private void OnDisable()
    {
        eventChannel.OnPlayRequested -= OnPlayRequested;
        _director.played -= OnDirectorPlayed;
        _director.stopped -= OnDirectorStopped;
    }

    // 再生リクエスト受信
    private void OnPlayRequested(int requestedHash)
    {
        if (requestedHash == _cutsceneHash)
        {
            _director.Play();
        }
        else
        {
            _director.Stop();
        }
    }

    // Timeline開始時
    private void OnDirectorPlayed(PlayableDirector d)
    {
        cutsceneState.SetPlaying(_cutsceneHash);
    }

    // Timeline終了時
    private void OnDirectorStopped(PlayableDirector d)
    {
        // 正常に最後まで完走したか判定
        bool isCompleted = d.time >= (d.duration - 0.01f);
        cutsceneState.StopPlaying(_cutsceneHash, isCompleted);
    }
}