using System;
using UnityEngine;

public class StageClearObserver : MonoBehaviour
{
    [SerializeField] private CutsceneState cutsceneState;

    private int _cutsceneNameHash;

    private void Awake()
    {
        _cutsceneNameHash = Animator.StringToHash("StageClear");
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    private void OnEnable()
    {
        cutsceneState.OnCutsceneStopped += OnStageClear;
    }

    private void OnStageClear(int cutsceneHash, bool isCompleted)
    {
        if (cutsceneHash == _cutsceneNameHash)
        {
            // ここにリザルト遷移処理を書く
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}