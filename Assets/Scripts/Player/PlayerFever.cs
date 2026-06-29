using LibTessDotNet;
using UnityEngine;

public class PlayerFever : MonoBehaviour
{
    [SerializeField, Tooltip("必要チャージ量")]
    private float maxCharge = 100.0f;

    [SerializeField, Tooltip("アサルトチャージ倍率")]
    private float asultChrgeRatio = 1.1f;

    [SerializeField, Tooltip("ロケランチャージ倍率")]
    private float rocketChrgeRatio = 0.9f;

    [SerializeField, Tooltip("フィーバーの時間")]
    private float feverTime = 10.0f;

    [SerializeField, Tooltip("フィーバー中の移動速度倍率")]
    private float speedRatio = 1.2f;

    private float charge = 0.0f;    // 現在のチャージ量
    private bool isFever = false;   // フィーバーかどうか
    private float timer = 0.0f;     // フィーバーの時間を測る

    private PlayerShooter playerShooter;
    private PlayerRocketShooter playerRocketShooter;

    // チャージする
    // area : 破壊した面積
    public void Charge(float area)
    {
        charge += area;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(isFever)
        {
            timer += Time.deltaTime;

            if (feverTime < timer)
                EndFever();
        }
        else
        {
            if (maxCharge < charge)
                StartFever();
            
        }
    }

    // フィーバー開始処理
    private void StartFever()
    {
        isFever = true;
        charge = 0.0f;
        Debug.Log("開始");
    }

    // フィーバー終了処理
    private void EndFever()
    {
        timer = 0.0f;
        isFever = false;
    }

    public void SetPlayerShooter(PlayerShooter ps)
    {
        playerShooter = ps;
    }
    public void SetPlayerRocketShooter(PlayerRocketShooter rs)
    {
        playerRocketShooter = rs;
    }
}
