using UnityEngine;

/// <summary>
/// X軸方向に背景を無限ループさせる（エッジを見せないようにする）
/// スクリプト付きオブジェクトを複製すると無限増殖するため、
/// タイルは見た目だけの空オブジェクトとして手動生成する
/// </summary>
public class ParallaxLoopX : MonoBehaviour
{
    [SerializeField, Tooltip("左右に複製する枚数。1なら左右1枚ずつ（計3枚構成）")]
    private int _loopCount = 1;

    [SerializeField, Tooltip("タイル間の隙間調整（0で隙間なし、負の値で重ねる）")]
    private float _gapOffset = 0f;

    private Camera _cam;
    private SpriteRenderer _sourceRenderer;
    private float _tileWidth;
    private Transform[] _tiles;

    private void Awake()
    {
        _cam = Camera.main;
        _sourceRenderer = GetComponent<SpriteRenderer>();

        if (_sourceRenderer == null)
        {
            Debug.LogError($"{name}: SpriteRendererが見つかりません");
            enabled = false;
            return;
        }

        _tileWidth = _sourceRenderer.bounds.size.x + _gapOffset;

        int totalTiles = _loopCount * 2 + 1;
        _tiles = new Transform[totalTiles];
        _tiles[0] = transform;

        for (int i = 1; i <= _loopCount; i++)
        {
            _tiles[i] = CreateTile(transform.position + new Vector3(_tileWidth * i, 0, 0), $"{name}_R{i}");
            _tiles[_loopCount + i] = CreateTile(transform.position + new Vector3(-_tileWidth * i, 0, 0), $"{name}_L{i}");
        }
    }

    private Transform CreateTile(Vector3 position, string tileName)
    {
        GameObject tile = new GameObject(tileName);
        tile.transform.SetParent(transform.parent);
        tile.transform.position = position;
        tile.transform.localScale = transform.localScale;
        tile.transform.rotation = transform.rotation;

        SpriteRenderer sr = tile.AddComponent<SpriteRenderer>();
        sr.sprite = _sourceRenderer.sprite;
        sr.sortingLayerID = _sourceRenderer.sortingLayerID;
        sr.sortingOrder = _sourceRenderer.sortingOrder;
        sr.color = _sourceRenderer.color;
        sr.flipX = _sourceRenderer.flipX;
        sr.flipY = _sourceRenderer.flipY;

        // 元オブジェクトにParallaxBackgroundが付いていれば、タイルにも同じ設定でコピーする
        ParallaxBackground sourceParallax = GetComponent<ParallaxBackground>();
        if (sourceParallax != null)
        {
            ParallaxBackground tileParallax = tile.AddComponent<ParallaxBackground>();
            tileParallax.CopySettingsFrom(sourceParallax);
        }

        return tile.transform;
    }

    private void LateUpdate()
    {
        float camX = _cam.transform.position.x;

        foreach (Transform tile in _tiles)
        {
            float distance = camX - tile.position.x;

            if (Mathf.Abs(distance) >= _tileWidth * (_loopCount + 0.5f))
            {
                float direction = Mathf.Sign(distance);
                tile.position += new Vector3(_tileWidth * (_loopCount * 2 + 1) * direction, 0, 0);
            }
        }
    }

    public void CameraReset()
    {
        _cam = Camera.main;
    }
}