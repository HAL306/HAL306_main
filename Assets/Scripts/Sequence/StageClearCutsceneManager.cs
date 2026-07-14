using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{
    [SerializeField] private PlayCutsceneEventChannel cutsceneEventCh;

    private int _cutsceneIdHash;

    private void Awake()
    {
        _cutsceneIdHash = Animator.StringToHash("StageClear2");
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnPlayerMoveOverrideFinished(string id, bool isCompleted)
    {
        cutsceneEventCh.PlayCutscene(_cutsceneIdHash);
    }
}
