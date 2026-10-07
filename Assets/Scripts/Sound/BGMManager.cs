using UnityEngine;

public class BGMManager : MonoBehaviour
{
    // どこからでもアクセスできるように静的インスタンスを用意
    public static BGMManager Instance { get; private set; }

    [SerializeField] private AudioSource bgmSource;

    private void Awake()
    {
        // シングルトンの保証処理
        if (Instance == null)
        {
            Instance = this;
            // シーン遷移してもこのオブジェクトを破棄しない
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // 既に存在する場合は重複して生成された方を破棄
            Destroy(gameObject);
        }
    }

    /// BGMを再生する(同じ曲が既に流れている場合は無視9
    public void PlayBGM(AudioClip clip, float volume = 1.0f)
    {
        if (clip == null)
        {
            bgmSource.Stop();

            return;
        }
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        bgmSource.clip = clip;
        bgmSource.volume = volume;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    /// BGMを停止する
    public void StopBGM()
    {
        bgmSource.Stop();
    }

    /// 音量を変更する（0.0f ～ 1.0f）
    public void SetVolume(float volume)
    {
        bgmSource.volume = Mathf.Clamp01(volume);
    }
}