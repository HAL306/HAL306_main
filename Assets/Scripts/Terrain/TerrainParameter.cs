using UnityEngine;


/// <summary>
/// 地形のパラメータ
/// </summary>
[CreateAssetMenu(fileName = "TerrainParameter", menuName = "Scriptable Objects/TerrainParameter")]
public class TerrainParameter : ScriptableObject
{
    [SerializeField, Tooltip("地形のマテリアル")]
    private Material _material;

    [SerializeField, Tooltip("破壊時エフェクト")]
    private ParticleSystem _destructEffect;

    [SerializeField, Tooltip("地形の削れやすさ倍率")]
    [Range(0.0f, 2.0f)]
    private float _destructibility = 1.0f;

    [SerializeField, Tooltip("地形の割れやすさ倍率")]
    [Range(0.0f, 2.0f)]
    private float _fractureMultiplier = 1.0f;

    [SerializeField, Tooltip("地形の密度")]
    [Range(0.0f, 20.0f)]
    private float _density = 5.0f;

    [SerializeField, Tooltip("エフェクト生成量")]
    private float _effectAmount = 30.0f;

    public Material Material => _material;
    public ParticleSystem DestructEffect => _destructEffect;
    public float Destructibility => _destructibility;
    public float FractureMultiplier => _fractureMultiplier;
    public float Density => _density;
    public float EffectAmount => _effectAmount;
}
