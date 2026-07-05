using UnityEngine;

public class UVScroll : MonoBehaviour
{
    // スクロール速度（X軸、Y軸）
    [SerializeField] private Vector2 scrollSpeed = new Vector2(0.1f, 0f);

    private Material targetMaterial;

    void Start()
    {
        // レンダラーからマテリアルを取得
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            targetMaterial = renderer.material;
        }
    }

    void Update()
    {
        if (targetMaterial != null)
        {
            // 時間の経過に合わせてオフセットを計算
            Vector2 offset = scrollSpeed * Time.time;

            // マテリアルのメインテクスチャのオフセットを更新
            targetMaterial.mainTextureOffset = offset;
        }
    }
}