using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class DestroyTilemap : MonoBehaviour
{
    private Tilemap tilemap;

    private void Awake()
    {
        tilemap = GetComponent<Tilemap>();
    }

    public void BreakTile(Vector2 worldPosition)
    {
        // ワールド座標 → タイル座標
        Vector3Int cellPosition =
            tilemap.WorldToCell(worldPosition);

        // タイルが存在するなら削除
        if (tilemap.HasTile(cellPosition))
        {
            tilemap.SetTile(cellPosition, null);
        }
    }
    public void BreakTilesInBounds(Bounds bounds)
    {
        Vector3Int min =
            tilemap.WorldToCell(bounds.min);

        Vector3Int max =
            tilemap.WorldToCell(bounds.max);

        for (int x = min.x; x <= max.x; x++)
        {
            for (int y = min.y; y <= max.y; y++)
            {
                Vector3Int cellPos =
                    new Vector3Int(x, y, 0);

                TileBase tile =
                    tilemap.GetTile(cellPos);

                if (tile != null)
                {
                    tilemap.SetTile(cellPos, null);
                }
            }
        }
    }
}
