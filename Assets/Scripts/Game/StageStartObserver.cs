using System;
using UnityEngine;

public class StageStartObserver : MonoBehaviour
{
    [SerializeField] private CutsceneState cutsceneState;
    [SerializeField] private GameObject playerShooter;
    [SerializeField] private GameObject playerRocketLauncher;
    [SerializeField] private GameObject tutorialCollision;

    private int _cutsceneNameHash;

    private void Awake()
    {
        _cutsceneNameHash = Animator.StringToHash("GetTreasure");
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    private void OnEnable()
    {
        cutsceneState.OnCutsceneStopped += OnStageStart;
    }

    private void OnDisable()
    {
        cutsceneState.OnCutsceneStopped -= OnStageStart;
    }

    private void OnStageStart(int cutsceneHash, bool isCompleted)
    {
        if (cutsceneHash == _cutsceneNameHash)
        {
            playerShooter.SetActive(true);
            playerRocketLauncher.SetActive(true);
            tutorialCollision.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}