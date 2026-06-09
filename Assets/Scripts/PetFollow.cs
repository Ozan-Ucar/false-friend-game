using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class PetFollow : MonoBehaviour
{
    [Header("Verknüpfungen")]
    [Tooltip("Zieh hier deinen Player rein")]
    public Transform player;
    [Tooltip("Wähle hier deinen Boden-Layer aus (z.B. 'Ground' oder 'Default')")]
    public LayerMask groundLayer;

    [Header("Einstellungen")]
    [Tooltip("Wie viel Abstand das Pet hält")]
    public float followDistance = 1.5f;
    [Tooltip("Gehgeschwindigkeit")]
    public float moveSpeed = 6f;
    [Tooltip("Sprungkraft")]
    public float jumpForce = 12f;

    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer sr;
    private bool isGrounded;
    private float nextJumpTime;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();

        // Sorgt dafür, dass das Pet nicht umfällt!
        rb.freezeRotation = true;
    }

    void Update()
    {
        if (player == null) return;

        // 1. Simpler Boden-Check (ein Strahl leicht nach unten)
        // (Länge 0.7f musst du evtl. im Skript anpassen, falls dein Sprite extrem groß/klein ist)
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 0.7f, groundLayer);
        isGrounded = hit.collider != null;

        float distanceX = player.position.x - transform.position.x;
        float distanceY = player.position.y - transform.position.y;

        bool isWalking = false;

        // 2. Bewegung (Dem Spieler hinterher)
        if (Mathf.Abs(distanceX) > followDistance)
        {
            isWalking = true;
            float dir = Mathf.Sign(distanceX);
            rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);
            
            // In die Laufrichtung gucken
            sr.flipX = dir < 0; 
        }
        else
        {
            // Sanft abbremsen, wenn nah genug
            rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.8f, rb.linearVelocity.y);
        }

        // 3. Springen (Wenn der Spieler auf eine Plattform hüpft)
        if (distanceY > 1.2f && isGrounded && Time.time > nextJumpTime)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            nextJumpTime = Time.time + 0.3f; // Kurzer Cooldown
        }

        // 4. Animationen ansteuern!
        anim.SetBool("isWalking", isWalking);
        anim.SetBool("isJumping", !isGrounded);
    }
}
