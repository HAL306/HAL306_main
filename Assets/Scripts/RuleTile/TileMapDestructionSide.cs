using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileMapDestructionSide : MonoBehaviour
{
    [SerializeField]
    private LayerMask targetLayer;

    private Collider2D myCollider;

    private void Awake()
    {
        myCollider =
            GetComponent<Collider2D>();
    }

    private void OnCollisionStay2D(
        Collision2D collision)
    {
        if (((1 << collision.gameObject.layer)
            & targetLayer) == 0)
            return;

        DestroyTilemap tilemap =
            collision.gameObject
            .GetComponent<DestroyTilemap>();

        if (tilemap == null)
            return;

        tilemap.BreakTilesInBounds(
            myCollider.bounds);
    }
}