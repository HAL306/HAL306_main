using System;
using UnityEngine;

public class GetTreasureCutsceneManager : MonoBehaviour
{
    [SerializeField] private PlayCutsceneEventChannel cutsceneEventCh;
    
    private int _cutsceneIdHash;

    private void Awake()
    {
        _cutsceneIdHash = Animator.StringToHash("AwakeBoss");
    }

    public void OnPlayerMoveOverrideFinished(string id, bool isCompleted)
    {
    }
}
