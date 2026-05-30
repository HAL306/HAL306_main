using UnityEngine;


/// <summary>
/// 地形のパラメータ
/// </summary>
[CreateAssetMenu(fileName = "TerrainParameter", menuName = "Scriptable Objects/TerrainParameter")]
public class TerrainParameter : ScriptableObject
{
    [SerializeField, Tooltip("破壊時エフェクト")]
    private GameObject _destructEffect;

    [SerializeField, Tooltip("地形の削れやすさ倍率")]
    [Range(0.0f, 2.0f)]
    private float _destructibility = 1.0f;

    [SerializeField, Tooltip("地形の割れやすさ倍率")]
    [Range(0.0f, 2.0f)]
    private float _fractureMultiplier = 1.0f;

    [SerializeField, Tooltip("地形の密度")]
    [Range(0.0f, 20.0f)]
    private float _density = 5.0f;


    public GameObject DestructEffect => _destructEffect;
    public float Destructibility => _destructibility;
    public float FractureMultiplier => _fractureMultiplier;
    public float Density => _density;
}
