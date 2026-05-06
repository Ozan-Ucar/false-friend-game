using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class RollingStone : MonoBehaviour
{
    [Header("Geschwindigkeit")]
    public float maxSpeed = 8f;
    public float acceleration = 2f;
    public float startDirection = 1f;

    [Header("Zerbrechen Effekt")]
    public GameObject shardPrefab;
    public int shardCount = 6;
    public float fadeOutTime = 0.8f;

    private Rigidbody2D rb;
    private bool isRolling = false;
    private bool isBreaking = false;
    private float currentDirection;
    private float currentTargetSpeed = 0f;
    private Collider2D myCollider;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        myCollider = GetComponent<CircleCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        currentDirection = Mathf.Sign(startDirection);

        // RESET: Stein muss am Anfang 100% sichtbar sein
        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, 1f);
        }
    }

    void Update()
    {
        if (isBreaking) return;

        if (!isRolling && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            if (myCollider != null && myCollider.OverlapPoint(mousePos))
            {
                isRolling = true;
                Debug.Log("Stein startet Rollen.");
            }
        }
    }

    void FixedUpdate()
    {
        if (isRolling && !isBreaking)
        {
            currentTargetSpeed = Mathf.MoveTowards(currentTargetSpeed, maxSpeed, acceleration * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector2(currentDirection * currentTargetSpeed, rb.linearVelocity.y);
            rb.angularVelocity = -currentDirection * currentTargetSpeed * 50f;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isBreaking) return;

        // PRÜFUNG: Wer wurde getroffen?
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("Stein fadet jetzt, weil ich den PLAYER (" + collision.gameObject.name + ") getroffen habe!");
            StartCoroutine(BreakSequence(collision.gameObject));
        }
        else
        {
            // Abprallen an Wänden (nur wenn es NICHT der Player ist)
            foreach (ContactPoint2D contact in collision.contacts)
            {
                if (Mathf.Abs(contact.normal.x) > 0.5f)
                {
                    ReverseDirection();
                    break;
                }
            }
        }
    }

    private IEnumerator BreakSequence(GameObject player)
    {
        isBreaking = true;
        
        HealthSystem health = player.GetComponent<HealthSystem>();
        if (health != null) health.TakeDamage(2);

        if (shardPrefab != null)
        {
            for (int i = 0; i < shardCount; i++)
            {
                GameObject shard = Instantiate(shardPrefab, transform.position, Quaternion.identity);
                Rigidbody2D shardRb = shard.GetComponent<Rigidbody2D>();
                if (shardRb != null)
                {
                    Vector2 randomDir = new Vector2(Random.Range(-1f, 1f), Random.Range(0.5f, 1.5f));
                    shardRb.AddForce(randomDir * 5f, ForceMode2D.Impulse);
                }
            }
        }

        rb.linearVelocity = Vector2.zero;
        rb.isKinematic = true;
        if (myCollider != null) myCollider.enabled = false;

        // FADE LOGIK
        if (spriteRenderer != null)
        {
            float elapsed = 0f;
            Color startColor = spriteRenderer.color;
            while (elapsed < fadeOutTime)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1, 0, elapsed / fadeOutTime);
                spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                yield return null;
            }
        }

        Destroy(gameObject);
    }

    void ReverseDirection()
    {
        currentDirection *= -1f;
        rb.linearVelocity = new Vector2(currentDirection * 2f, rb.linearVelocity.y);
    }
}
