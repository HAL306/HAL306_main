using UnityEngine;
using UnityEngine.Tilemaps;

public class BaseTerrain : MonoBehaviour
{
    [SerializeField, Tooltip("ボス")]
    private BOSScharge _boss;

    [SerializeField, Tooltip("非表示ラインのオフセット")]
    private float _offset_x;

    private Material _material;

    private void Awake()
    {
        _material = GetComponent<TilemapRenderer>().material;
    }

    private void Update()
    {
        if (_boss && _boss.gameObject.activeInHierarchy)
        {
            _material.SetFloat("_BossPosition", _boss.transform.position.x + _offset_x);
        }
        else
        {
            _material.SetFloat("_BossPosition", -10000f);
        }
    }
}
