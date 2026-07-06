using System;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.SceneManagement; 
public class PlayerKiller : MonoBehaviour
{
    [SerializeField, Tooltip("フェード")]
    private FadeUI fade;


    [SerializeField] private PlayCutsceneEventChannel cutsceneEventCh;
    private int _cutsceneIdHash;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _cutsceneIdHash = Animator.StringToHash("PlayerDeath");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnTriggerStay2D(
        Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        Time.timeScale = 0.0f; // 時間を止める

        fade.SetFinishAction(ChangeScene);
        fade.SetPosition(collision.transform.position);
        cutsceneEventCh.PlayCutscene(_cutsceneIdHash);
    }

    private void ChangeScene()
    {
        Time.timeScale = 1.0f; // 動かす
        SceneManager.LoadScene("GameOver");
    }
}
