using UnityEngine;

public class Boss : MonoBehaviour
{
    Rigidbody2D rb;
    public float speed = 3.0f;
    public float moveHeight = 2.0f;
    public float moveSpeed = 2.0f;

    float startY;

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

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Field"))
        {
            Destroy(collision.gameObject);
        }
    }
}
