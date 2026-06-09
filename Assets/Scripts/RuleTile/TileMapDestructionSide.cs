using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileMapDestructionSide : MonoBehaviour
{
    [SerializeField]
    private LayerMask targetLayer;

    private Collider2D[] myColliders;

    private void Awake()
    {
        myColliders =
            GetComponents<Collider2D>();
    }

    private void OnTriggerStay2D(
        Collider2D collision)
    {
        if (((1 << collision.gameObject.layer)
            & targetLayer) == 0)
            return;

        DestroyTilemap tilemap =
            collision.gameObject
            .GetComponent<DestroyTilemap>();

        if (tilemap == null)
            return;

        foreach(Collider2D collider in myColliders)
        {
            tilemap.BreakTilesInBounds(collider.bounds);
        }
    }
}