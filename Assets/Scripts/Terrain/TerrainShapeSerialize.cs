using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 地形形状をシリアライズするためのコンポーネント
/// エディタ上で地形形状を編集する場合に必要になります
/// </summary>
[ExecuteAlways]
public class TerrainShapeSerialize : MonoBehaviour
{
    [SerializeField]
    private List<Vector2> _points;          // 頂点リスト

    public List<Vector2> Points
    {
        get => _points;
        set => _points = value;
    }


    // TerrainShapeの地形形状を再構築する
    public void Rebuild()
    {
        TerrainShape terrainShape = GetComponent<TerrainShape>();
        if (terrainShape != null)
        {
            terrainShape.Initialize(_points);
        }
    }


    private void Awake()
    {
        // 実行中のみAwakeで初期化
        if (Application.isPlaying)
        {
            Rebuild();
        }
    }

    private void OnEnable()
    {
        // エディタ上で変更を反映するために、OnEnableで初期化
        if (!Application.isPlaying)
        {
            Rebuild();
        }
    }
}
