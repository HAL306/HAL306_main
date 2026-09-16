using UnityEngine;

public class MuzzleFlashPlayer : MonoBehaviour
{
    [SerializeField] private ParticleSystem muzzleFlash;

    [Tooltip("射撃が止まってからエフェクトを停止するまでの猶予時間(秒)")]
    [SerializeField] private float stopDelay = 0.15f;

    private float timer = 0f;
    private bool isPlaying = false;

    void Update()
    {
        // 再生中のみタイマーを減算
        if (isPlaying)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                StopMuzzleFlash();
            }
        }
    }

    /// <summary>
    /// 弾を発射するたびに呼ぶ関数
    /// </summary>
    public void Play()
    {
        // タイマーをリセット（射撃間隔内に入力があればタイマーが延長される）
        timer = stopDelay;

        // まだ再生されていなければ再生開始
        //if (!isPlaying)
        {
            isPlaying = true;
            muzzleFlash.Play();
        }
    }

    private void StopMuzzleFlash()
    {
        isPlaying = false;
        // StopEmitting: 新しい発生だけ止め、既に出ているパーティクルは最後まで描画させる
        // 即座にパッと消したい場合は StopEmittingAndClear を指定
        muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }
}
