using UnityEngine;

/// <summary>
/// 地形のパラメータ
/// 地形の種類ごとに設定する
/// </summary>
[CreateAssetMenu(fileName = "TerrainParameterA", menuName = "Scriptable Objects/TerrainParameterA")]
public class TerrainParameterA : ScriptableObject
{
    [Header("基本設定")]
    [SerializeField, Tooltip("地形のマテリアル")]
    private Material _material;

    [Header("破壊用ステータス")]
    [SerializeField, Tooltip("地形の削れやすさ倍率")]
    [Range(0.0f, 2.0f)]
    private float _destructibility = 1.0f;

    [SerializeField, Tooltip("地形の割れやすさ倍率")]
    [Range(0.0f, 2.0f)]
    private float _fractureMultiplier = 1.0f;

    [SerializeField, Tooltip("地形の密度")]
    [Range(0.0f, 20.0f)]
    private float _density = 5.0f;

    [Header("エフェクト設定")]
    [SerializeField, Tooltip("エフェクトのプレハブ")]
    private ParticleSystem _destructEffect;

    [SerializeField, Tooltip("エフェクト生成量")]
    private float _effectAmount = 30.0f;

    [SerializeField, Tooltip("破壊時に発生するオブジェクト")]
    private GameObject _destructObject;

    [SerializeField, Tooltip("破壊時に発生するオブジェクトの生成量")]
    private float _destructObjectAmount = 5.0f;

    [Header("サウンド設定")]
    [SerializeField, Tooltip("結晶の破壊音を再生するか")]
    private bool _isSoundEnabled = true;


    public Material Material => _material;
    public float Density => _density;
    public float Destructibility => _destructibility;
    public float FractureMultiplier => _fractureMultiplier;
    public ParticleSystem DestructEffect => _destructEffect;
    public float EffectAmount => _effectAmount;
    public GameObject DestructObject => _destructObject;
    public float DestructObjectAmount => _destructObjectAmount;
    public bool IsSoundEnabled => _isSoundEnabled;
}
