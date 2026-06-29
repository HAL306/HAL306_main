using LibTessDotNet;
using System.Collections.Generic;
using UnityEngine;

public class PlayerFever : MonoBehaviour
{
    [SerializeField, Tooltip("必要チャージ量")]
    private float maxCharge = 20.0f;


    [SerializeField, Tooltip("フィーバーの時間")]
    private float feverTime = 10.0f;

    [SerializeField, Tooltip("フィーバー中の移動速度倍率")]
    private float speedRatio = 1.2f;

    private float charge = 0.0f;    // 現在のチャージ量
    private bool isFever = false;   // フィーバーかどうか
    private float timer = 0.0f;     // フィーバーの時間を測る

    private PlayerShooter playerShooter;
    private PlayerRocketShooter playerRocketShooter;

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
        timer = 0.0f;
        isFever = true;
        playerRocketShooter.SetFever(true);
        playerShooter.SetFever(true);
    }

    // フィーバー終了処理
    private void EndFever()
    {
        charge = 0.0f;
        isFever = false;
        playerRocketShooter.SetFever(false);
        playerShooter.SetFever(false);
    }
    // チャージする
    // area : 破壊した面積
    public void Charge(float area)
    {
        charge += area;
    }

    // 割合を取得 UI用
    public float GetRate()
    {
        float rate = 0.0f;

        if (isFever)
        {
            rate = (feverTime - timer) / feverTime;
        }
        else
        {
            rate = charge / maxCharge;
        }
        rate = Mathf.Clamp01(rate);
        return rate;
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
