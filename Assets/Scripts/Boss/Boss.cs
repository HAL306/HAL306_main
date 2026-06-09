using UnityEngine;

public class Boss : MonoBehaviour
{
    [SerializeField]
    private float destructRadius = 0.5f;
    [SerializeField]
    private CrackParameter crackParameter;
    [SerializeField]
    private Transform player;

    private float moveX;

    Rigidbody2D rb;
    public float speed = 3.0f;
    public float boostSpeed = 6.0f;
    public float boostDistance = 5.0f;
    public float moveHeight = 2.0f;
    public float moveSpeed = 2.0f;

    float startY;
    private float destructTimer;

    private Transform t;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        moveX = transform.position.x;
        t = GetComponent<Transform>();
    }
    void FixedUpdate()
    {
        if (player == null)
        {
            Debug.Log("player‚ª“ü‚Á‚Ä‚¢‚È‚¢");
            return;
        }
        float currentSpeed = speed;

        float distance = Mathf.Abs(transform.position.x - player.position.x);
        if (distance > boostDistance)
        {
            currentSpeed = boostSpeed;
        }
        moveX += currentSpeed * Time.fixedDeltaTime;

        float y = player.position.y + Mathf.Sin(Time.time * moveSpeed) * moveHeight;

        rb.MovePosition(new Vector2(moveX, y));
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Field"))
        {
            TerrainContext terrain = collision.GetComponent<TerrainContext>();

            if (terrain == null)
            {
                return;
            }

            destructTimer += Time.deltaTime;

            if (destructTimer >= 0.2f)
            {
                terrain.Destruct(transform.position, destructRadius, crackParameter);
                destructTimer = 0.0f;
            }
        }
    }
}
