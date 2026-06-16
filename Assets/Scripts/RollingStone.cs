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
    private AudioSource rollingAudioSource;
    private ClickableHighlight highlight;
    private SandwormManager swManager;

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

        highlight = GetComponent<ClickableHighlight>();
        swManager = FindFirstObjectByType<SandwormManager>();
    }

    void Update()
    {
        if (isBreaking) return;

        // --- NEU: Sperre in der Wüste (SandwormManager), bis alle Würmer platziert sind! ---
        if (swManager != null && !swManager.AllWormsPlaced)
        {
            // Verhindert das Aufleuchten (Highlight) der Falle
            if (highlight != null) highlight.enabled = false;
            return; // Beendet Update hier, Klick wird ignoriert
        }
        else
        {
            // Highlight wieder anschalten, sobald die Bauphase vorbei ist (oder gar keine Wüste da ist)
            if (highlight != null) highlight.enabled = true;
        }

        if (!isRolling && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            if (myCollider != null && myCollider.OverlapPoint(mousePos))
            {
                isRolling = true;
                Debug.Log("Stein startet Rollen.");

                // Highlight entfernen
                if (highlight != null) highlight.isTriggered = true;

                // --- NEU: Rolling Sound (Loop) starten ---
                if (SceneSoundManager.Instance != null && SceneSoundManager.Instance.stoneRollSound != null)
                {
                    rollingAudioSource = gameObject.AddComponent<AudioSource>();
                    rollingAudioSource.clip = SceneSoundManager.Instance.stoneRollSound;
                    rollingAudioSource.volume = SceneSoundManager.Instance.stoneRollVolume;
                    rollingAudioSource.loop = true;
                    rollingAudioSource.pitch = 0.5f; // Fängt tief/langsam an
                    rollingAudioSource.Play();
                }
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

            // --- NEU: Pitch an die Roll-Geschwindigkeit anpassen ---
            if (rollingAudioSource != null)
            {
                rollingAudioSource.pitch = Mathf.Lerp(0.5f, 1.5f, currentTargetSpeed / maxSpeed);
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isBreaking) return;

        // PRÜFUNG: Wer wurde getroffen?
        if (collision.gameObject.CompareTag("Player"))
        {
            if (SceneSoundManager.Instance != null) SceneSoundManager.Instance.PlayStoneHit();
            StartCoroutine(BreakSequence(collision.gameObject));
        }
        else
        {
            // Abprallen an Wänden (nur wenn es NICHT der Player ist)
            foreach (ContactPoint2D contact in collision.contacts)
            {
                if (Mathf.Abs(contact.normal.x) > 0.5f)
                {
                    if (SceneSoundManager.Instance != null) SceneSoundManager.Instance.PlayStoneHit();
                    ReverseDirection();
                    break;
                }
            }
        }
    }

    private IEnumerator BreakSequence(GameObject player)
    {
        isBreaking = true;
        
        // --- NEU: Rollen Sound stoppen ---
        if (rollingAudioSource != null)
        {
            rollingAudioSource.Stop();
        }

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
