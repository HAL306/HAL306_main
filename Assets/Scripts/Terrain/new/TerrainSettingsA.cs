using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 地形の詳細設定
/// 基本的に一つだけ用意する
/// </summary>
[CreateAssetMenu(fileName = "TerrainSettingsA", menuName = "Scriptable Objects/TerrainSettingsA")]
public class TerrainSettingsA : ScriptableObject
{
    /// <summary>
    /// インスペクターで値が変更された際に発火するイベント
    /// </summary>
    public event Action onValuesChanged;

    [Header("基本設定")]
    [SerializeField, Tooltip("空の地形のプレハブ")]
    private TerrainContextA _terrainPrefab;

    [SerializeField, Tooltip("ベース地形のレイヤー")]
    private LayerMask _baseTerrainLayer;

    [SerializeField, Tooltip("地形の最小サイズ")]
    [Range(0.0f, 1.0f)]
    private float _minArea = 0.05f;

    [SerializeField, Tooltip("地形の当たり判定簡略化レベル")]
    [Range(0.0f, 1.0f)]
    private float _simplificationLevel = 0.5f;

    [Header("ドット描画設定")]
    [SerializeField, Tooltip("地形全体のドットサイズ（配置間隔・描画サイズ共通）")]
    [Range(0.01f, 0.5f)]
    private float _dotSize = 0.125f;

    [SerializeField, Tooltip("エッジ（輪郭）の太さ倍率 (ドットサイズに対する倍率。例: 0.8〜1.5)")]
    [Range(0.1f, 3.0f)]
    private float _edgeWidthMultiplier = 0.8f;

    [Header("破壊用基本設定")]
    [SerializeField, Tooltip("破壊円の頂点数")]
    [Range(4, 16)]
    private int _circleVertex = 6;

    [Header("ひび割れ用設定")]
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

    [Header("サウンド設定")]
    [SerializeField, Tooltip("小さいひび割れ音（複数からランダム再生）")]
    private AudioClip[] _smallCrackSounds;

    [SerializeField, Tooltip("大きいひび割れ音（複数からランダム再生・完全破壊時にも使用）")]
    private AudioClip[] _bigCrackSounds;

    [SerializeField, Tooltip("小と大のひびを区別する面積のしきい値")]
    private float _bigCrackAreaThreshold = 1.0f;

    [SerializeField, Tooltip("再生ピッチの範囲（最小）")]
    [Range(0.1f, 3.0f)]
    private float _pitchMin = 0.85f;

    [SerializeField, Tooltip("再生ピッチの範囲（最大）")]
    [Range(0.1f, 3.0f)]
    private float _pitchMax = 1.15f;

    [SerializeField, Tooltip("再生音量の範囲（最小）")]
    [Range(0.0f, 1.0f)]
    private float _volumeMin = 0.7f;

    [SerializeField, Tooltip("再生音量の範囲（最大）")]
    [Range(0.0f, 1.0f)]
    private float _volumeMax = 1.0f;

    [SerializeField, Tooltip("同種の音の最短再生間隔（秒）")]
    [Range(0.0f, 1.0f)]
    private float _soundInterval = 0.03f;

    // プロパティ
    public TerrainContextA TerrainPrefab => _terrainPrefab;
    public LayerMask BaseTerrainLayer => _baseTerrainLayer;
    public float MinArea => _minArea;
    public float SimplificationLevel => _simplificationLevel;
    public float DotSize => _dotSize;
    public float EdgeWidthMultiplier => _edgeWidthMultiplier;
    public int CircleVertex => _circleVertex;
    public float CrackDistance => _crackDistance;
    public float CrackWidth => _crackWidth;
    public float CrackWeight => _crackWeight;
    public int CrackDivision => _crackDivision;
    public float CrackNoise => _crackNoise;
    public AudioClip[] SmallCrackSounds => _smallCrackSounds;
    public AudioClip[] BigCrackSounds => _bigCrackSounds;
    public float BigCrackAreaThreshold => _bigCrackAreaThreshold;
    public float PitchMin => _pitchMin;
    public float PitchMax => _pitchMax;
    public float VolumeMin => _volumeMin;
    public float VolumeMax => _volumeMax;
    public float SoundInterval => _soundInterval;

    private void OnValidate()
    {
        // 外部（TerrainContextA 等）へ変更を通知
        onValuesChanged?.Invoke();

#if UNITY_EDITOR
        // 非再生時、シーンビューを即座に再描画
        if (!Application.isPlaying)
        {
            SceneView.RepaintAll();
        }
#endif
    }
}