using UnityEngine;

public class BossTerrainDestruct : MonoBehaviour
{
    [Tooltip("地形破壊の半径")]
    [SerializeField] private float destructRadius = 1.0f;  // 地形破壊の半径

    [Tooltip("地形破壊のひび割れパラメータ")]
    [SerializeField] private CrackParameter crackParameter;  // 地形破壊のひび割れパラメータ
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        // 攻撃中のみ
        if (!enabled) return;

        // Fieldタグ以外に触れている場合は処理しない
        if (collision.gameObject.layer != LayerMask.NameToLayer("Field")) return;

        // 触れている地形からTerrainContextを取得する
        TerrainContext terrain = collision.gameObject.GetComponentInParent<TerrainContext>();

        // TerrainContextが無ければ破壊できない
        if (terrain == null) return;

        // 地形破壊処理を行う
        terrain.Destruct(transform.position, destructRadius, crackParameter);
    }
}
