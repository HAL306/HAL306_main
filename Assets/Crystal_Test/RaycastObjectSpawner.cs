using UnityEngine;

public class RaycastObjectSpawner : MonoBehaviour
{
    [Header("生成するオブジェクト")]
    [SerializeField]
    private GameObject spawnPrefab;

    [Header("Rayの設定")]
    [SerializeField]
    private float rayDistance = 10.0f;

    [SerializeField]
    private LayerMask terrainLayer;

    [SerializeField]
    private Vector2 rayDirection = Vector2.down;

    [Header("デバッグ")]
    [SerializeField]
    private bool spawnOnStart = true;

    private void Start()
    {
        if (spawnOnStart)
        {
            SpawnObject();
        }
    }

    public void SpawnObject()
    {
        Vector2 rayOrigin = transform.position;

        // Inspectorで設定した方向を正規化
        Vector2 direction = rayDirection.normalized;

        RaycastHit2D hit = Physics2D.Raycast(
            rayOrigin,
            direction,
            rayDistance,
            terrainLayer
        );

        if (hit.collider == null)
        {
            Debug.Log("地形にRayが当たりませんでした。");

            Debug.DrawRay(
                rayOrigin,
                direction * rayDistance,
                Color.red,
                5.0f
            );

            return;
        }

        Vector2 spawnPosition = hit.point;
        Vector2 terrainNormal = hit.normal;

        // 地面の法線を表示
        Debug.DrawRay(
            hit.point,
            hit.normal * 2.0f,
            Color.green,
            5.0f
        );

        // Vector2.upを地形法線方向へ向ける
        float angle =
            Mathf.Atan2(
                terrainNormal.y,
                terrainNormal.x
            ) * Mathf.Rad2Deg - 90.0f;

        Quaternion spawnRotation =
            Quaternion.Euler(
                0.0f,
                0.0f,
                angle
            );

        // オブジェクト生成
        GameObject obj = Instantiate(
            spawnPrefab,
            spawnPosition,
            spawnRotation
        );

        GrowObject growObject = obj.GetComponent<GrowObject>();

        if (growObject == null)
        {
            Debug.LogError(
                "生成したPrefabにGrowObjectがありません。",
                obj
            );

            return;
        }

        // 地面情報を渡す
        growObject.SetCutPlane(
            spawnPosition,
            terrainNormal
        );

        // 成長開始
        growObject.StartGrow();

        Debug.Log(
            $"Hit Point : {hit.point}\n" +
            $"Normal    : {hit.normal}\n" +
            $"Angle     : {angle}"
        );

        // Ray自体を表示
        Debug.DrawRay(
            rayOrigin,
            direction * rayDistance,
            Color.red,
            5.0f
        );
    }
}


