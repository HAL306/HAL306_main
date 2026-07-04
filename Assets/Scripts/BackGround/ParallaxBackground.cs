using UnityEngine;

/// <summary>
/// 視差スクロールを行うコンポーネント
/// カメラの絶対座標を基準にして視差スクロールを行う
/// 下端のみクランプし、背景がカメラの下端より内側に来ないようにする
/// </summary>
public class ParallaxBackground : MonoBehaviour
{
    [SerializeField, Tooltip("視差スクロール係数（0=動かない、1=カメラと同速）")]
    [Range(0.0f, 1.0f)]
    private float _parallaxFactor_X = 0.5f;

    [SerializeField, Tooltip("視差スクロール係数（0=動かない、1=カメラと同速）")]
    [Range(0.0f, 1.0f)]
    private float _parallaxFactor_Y = 0.5f;

    private Camera _cam;
    private SpriteRenderer[] _spriteRenderers;
    private Vector3 _startPos;
    private Vector3 _startCamPos;

    public float ParallaxFactorY => _parallaxFactor_Y;
    public Vector3 StartPos => _startPos;

    private void Awake()
    {
        _cam = Camera.main;
        _spriteRenderers = GetComponentsInChildren<SpriteRenderer>();

        if (_spriteRenderers.Length == 0)
        {
            Debug.LogError($"{name}: SpriteRendererが見つかりません");
        }

        _startPos = transform.position;
        _startCamPos = _cam.transform.position;
    }

    private void LateUpdate()
    {
        Vector3 camDelta = _cam.transform.position - _startCamPos;

        float x = _startPos.x + camDelta.x * _parallaxFactor_X;
        float y = _startPos.y + camDelta.y * _parallaxFactor_Y;
        y = ClampToCoverCamera(y);

        transform.position = new Vector3(x, y, _startPos.z);
    }

    private float ClampToCoverCamera(float desiredY)
    {
        // 下端のみクランプする（上端は制限しない、要望により一時的な簡易対応）
        float maxY = GetMaxY();
        return Mathf.Min(desiredY, maxY);
    }

    private Bounds GetCombinedBounds()
    {
        Bounds combined = _spriteRenderers[0].bounds;
        for (int i = 1; i < _spriteRenderers.Length; i++)
        {
            combined.Encapsulate(_spriteRenderers[i].bounds);
        }
        return combined;
    }

    // 上端がカメラ上端を覆うための最小Y座標
    public float GetMinY()
    {
        Bounds bounds = GetCombinedBounds();
        float topExtent = bounds.max.y - transform.position.y;
        float camTop = _cam.transform.position.y + _cam.orthographicSize;
        return camTop - topExtent;
    }

    // 下端がカメラ下端を覆うための最大Y座標
    public float GetMaxY()
    {
        Bounds bounds = GetCombinedBounds();
        float bottomExtent = transform.position.y - bounds.min.y;
        float camBottom = _cam.transform.position.y - _cam.orthographicSize;
        return camBottom + bottomExtent;
    }

    // タイル複製時に同じ視差設定をコピーするための公開メソッド
    public void CopySettingsFrom(ParallaxBackground source)
    {
        _parallaxFactor_X = source._parallaxFactor_X;
        _parallaxFactor_Y = source._parallaxFactor_Y;
    }

    // 外部から視差の許容範囲を確認したい場合のために残す（GroupSyncに依存しない）
    public float GetMinOffset() => GetMinY() - _startPos.y;
    public float GetMaxOffset() => GetMaxY() - _startPos.y;
}