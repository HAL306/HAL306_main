using UnityEngine;


/// <summary>
/// 地形の詳細設定
/// </summary>
[CreateAssetMenu(fileName = "TerrainSettings", menuName = "Scriptable Objects/TerrainSettings")]
public class TerrainSettings : ScriptableObject
{
    [SerializeField, Tooltip("空の地形のプレハブ")]
    private TerrainContext _terrainPrefab;

    [SerializeField, Tooltip("地形の最小サイズ")]
    [Range(0.0f, 1.0f)]
    private float _minArea = 0.05f;

    [SerializeField, Tooltip("破壊円の頂点数")]
    [Range(4, 16)]
    private int _circleVertex = 6;


    [Header("ひび割れ設定")]
    [SerializeField, Tooltip("基本ひび割れ距離")]
    [Range(0.0f, 10.0f)]
    private float _crackDistance = 5.0f;

    [SerializeField, Tooltip("ひび割れの幅")]
    [Range(0.0f, 0.2f)]
    private float _crackWidth = 0.1f;

    [SerializeField, Tooltip("ひび割れの破壊範囲の余白")]
    [Range(0.0f, 0.1f)]
    private float _crackWeight = 0.02f;

    [SerializeField, Tooltip("ひび割れの分割数")]
    [Range(0, 5)]
    private int _crackDivision = 1;

    [SerializeField, Tooltip("ひび割れの歪み")]
    [Range(0.0f, 1.0f)]
    private float _crackNoise = 0.6f;


    [Header("描画設定")]
    [SerializeField, Tooltip("ドットのサイズ")]
    [Range(0.0f, 1.0f)]
    private float _dotSize = 0.05f;



    public TerrainContext BaseTerrainPrefab => _terrainPrefab;
    public float MinArea => _minArea;
    public int CircleVertex => _circleVertex;
    public float CrackDistance => _crackDistance;
    public float CrackWidth => _crackWidth;
    public float CrackWeight => _crackWeight;
    public int CrackDivision => _crackDivision;
    public float CrackNoise => _crackNoise;

    public float DotSize => _dotSize;
    //public float MinImpulse => _minImpulse;
    //public float ImpulseToRadius => _impulseToRadius;

}
