using System;
using UnityEngine;

public class GunhitEffect : MonoBehaviour
{
    [SerializeField] private ParticleSystem particleSys;
    private Action<GunhitEffect> returnAction;

    private void Awake()
    {
        if (particleSys == null)
            particleSys = GetComponent<ParticleSystem>();
    }

    // プール初期化時に返却用コールバックを登録
    public void Initialize(Action<GunhitEffect> returnToPool)
    {
        returnAction = returnToPool;
    }

    // 指定位置・回転に移動させて再生
    public void PlayAt(Vector3 position, Quaternion rotation)
    {
        transform.SetPositionAndRotation(position, rotation);
        gameObject.SetActive(true);
        particleSys.Play();
    }

    // Stop Action: Disable によって非アクティブ化されたら自動でプールへ返却
    private void OnDisable()
    {
        returnAction?.Invoke(this);
    }
}
