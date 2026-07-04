using Clipper2Lib;
using UnityEngine;

public class WeaponDirection : MonoBehaviour
{
    [SerializeField, Tooltip("playerShooter")]
    PlayerShooter playerShooter;

    [SerializeField, Tooltip("銃のスプライト")]
    private SpriteRenderer spriteRenderer;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        // 向くべき角度を計算する
        float angle = Mathf.Atan2(playerShooter.ShootAimTarget.y, playerShooter.ShootAimTarget.x) * Mathf.Rad2Deg;

        // オブジェクトのZ軸（2Dの回転軸）を計算した角度に回す
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // エイムのターゲットが左側（X座標が0未満）の場合はスプライトを上下反転させる
        if (playerShooter.ShootAimTarget.x < 0)
        {
            spriteRenderer.flipY = true;
        }
        else
        {
            spriteRenderer.flipY = false;
        }
    }
}
