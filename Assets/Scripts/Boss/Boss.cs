using UnityEngine;

public class Boss : MonoBehaviour
{
    [SerializeField]
    private float destructRadius = 0.5f;
    [SerializeField]
    private CrackParameter crackParameter;

    Rigidbody2D rb;
    public float speed = 3.0f;
    public float moveHeight = 2.0f;
    public float moveSpeed = 2.0f;

    float startY;
    private float destructTimer;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        startY = transform.position.y;
    }
    void FixedUpdate()
    {
        float y = startY + Mathf.Sin(Time.time * (speed * 0.5f)) * moveHeight;

        rb.MovePosition(new Vector2(transform.position.x + speed * Time.fixedDeltaTime, y));
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        Debug.Log("触れている: " + collision.name);

        if (collision.CompareTag("Field"))
        {
            Debug.Log("Fieldタグに当たった");

            TerrainContext terrain = collision.GetComponent<TerrainContext>();

            if (terrain == null)
            {
                Debug.Log("TerrainContextが見つからない");
                return;
            }

            destructTimer += Time.deltaTime;

            if (destructTimer >= 0.2f)
            {
                Debug.Log("地形破壊実行");
                terrain.Destruct(transform.position, destructRadius, crackParameter);
                destructTimer = 0.0f;
            }
        }
    }
}
