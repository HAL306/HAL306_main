using UnityEngine;
using UnityEngine.Pool;

public class GunhitEffectManager : MonoBehaviour
{
    public static GunhitEffectManager Instance { get; private set; }

    [SerializeField] private GunhitEffect effectPrefab;
    [SerializeField] private int defaultCapacity = 10;
    [SerializeField] private int maxCapacity = 50;

    private IObjectPool<GunhitEffect> pool;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // プールの初期化
        pool = new ObjectPool<GunhitEffect>(
            createFunc: CreatePooledItem,
            actionOnGet: OnTakeFromPool,
            actionOnRelease: OnReturnedToPool,
            actionOnDestroy: OnDestroyPoolObject,
            collectionCheck: true,
            defaultCapacity: defaultCapacity,
            maxSize: maxCapacity
        );
    }

    // 新規生成時の処理
    private GunhitEffect CreatePooledItem()
    {
        GunhitEffect item = Instantiate(effectPrefab, transform);
        item.Initialize(ReleaseToPool);
        item.gameObject.SetActive(false);
        return item;
    }

    // プールから取り出された時
    private void OnTakeFromPool(GunhitEffect item)
    {
        // ここではアクティブ化せず、PlayAt内でアクティブ
    }

    // プールに返却された時
    private void OnReturnedToPool(GunhitEffect item)
    {
        item.gameObject.SetActive(false);
    }

    // プールの上限を超えて破棄される時
    private void OnDestroyPoolObject(GunhitEffect item)
    {
        Destroy(item.gameObject);
    }

    // 返却処理
    private void ReleaseToPool(GunhitEffect item)
    {
        pool.Release(item);
    }

    // 外部（弾など）から呼ぶ再生メソッド
    public void Play(Vector3 position, Quaternion rotation)
    {
        GunhitEffect effect = pool.Get();
        effect.PlayAt(position, rotation);
    }
}
