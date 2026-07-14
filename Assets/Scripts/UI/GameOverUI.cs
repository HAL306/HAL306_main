using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    [SerializeField, Tooltip("リトライボタン")]
    private GameObject retry;


    [SerializeField, Tooltip("あきらめボタン")]
    private GameObject quit;

    [SerializeField, Tooltip("フェード")]
    private FadeUI fade;

    private Animator retryAnim;
    private Animator quitAnim;

    [SerializeField] private PlayCutsceneEventChannel cutsceneEventCh;
    private int _retryCutsceneIdHash;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        retry.GetComponent<Button>().onClick.AddListener(OnClickRetry);
        quit.GetComponent<Button>().onClick.AddListener(OnClickQuit);
        retryAnim = retry.GetComponent<Animator>();


        _retryCutsceneIdHash = Animator.StringToHash("Retry");
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    private void OnStayRetry()
    {
    }

    private void OnClickRetry()
    {
        cutsceneEventCh.PlayCutscene(_retryCutsceneIdHash);

    }
    private void OnClickQuit()
    {
        fade.StartFadeOut(LoadTitleScene);
    }

    // timelineから呼ぶ関数
    public void StartFade()
    {
        fade.StartFadeOut(LoadGameScene);
    }

    private void LoadTitleScene()
    {
        SceneManager.LoadScene("ModeSelectScene");
    }

    private void LoadGameScene()
    {
        SceneManager.LoadScene("AlphaGameScene");
    }
}
