using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class FrogController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float jumpInterval = 1.0f; 
    [SerializeField] private float jumpForceX = 5.0f;
    [SerializeField] private float jumpForceY = 8.0f;
    [SerializeField] private float jumpDelay = 0.15f; 
    
    [Header("Combat Settings")]
    [SerializeField] private int damageAmount = 1;

    [Header("State")]
    [SerializeField] private bool isControlled = false;

    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer spriteRenderer;
    private float jumpTimer;
    private Camera mainCamera;
    private bool isJumping = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        mainCamera = Camera.main;
    }

    private void Start()
    {
        jumpTimer = 0; 
        anim.Play("IdleFrogAnim");
    }

    private void Update()
    {
        bool isJuicy = JuiceManager.Instance == null || JuiceManager.Instance.isJuicy;
        if (anim != null) anim.enabled = isJuicy;

        HandleInput();

        if (isControlled)
        {
            UpdateJumpLogic();
        }
        else
        {
            // NUR wenn nicht gesteuert, geht er in den Idle-Zustand
            if (anim != null && anim.enabled && !anim.GetCurrentAnimatorStateInfo(0).IsName("IdleFrogAnim"))
            {
                anim.Play("IdleFrogAnim");
            }
            isJumping = false;
            jumpTimer = 0; 
        }
    }

    private void HandleInput()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Vector2 worldPos = mainCamera.ScreenToWorldPoint(mousePos);
            RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);

            if (hit.collider != null && hit.collider.gameObject == gameObject)
            {
                isControlled = true;
                jumpTimer = 0; 
            }
            else if (isControlled)
            {
                isControlled = false;
            }
        }
    }

    private void UpdateJumpLogic()
    {
        if (!isJumping)
        {
            jumpTimer -= Time.deltaTime;
            if (jumpTimer <= 0)
            {
                StartCoroutine(JumpRoutine());
                jumpTimer = jumpInterval;
            }
        }

        // Landung erkennen
        if (isJumping && Mathf.Abs(rb.linearVelocity.y) < 0.05f && rb.linearVelocity.y <= 0)
        {
            // HIER: Wir rufen KEIN anim.Play("IdleFrogAnim") auf!
            // Der Frosch bleibt in der Jump-Animation (oder dem letzten Frame davon),
            // solange er kontrolliert wird.
            isJumping = false;
        }
    }

    private IEnumerator JumpRoutine()
    {
        isJumping = true;

        if (Mouse.current == null) yield break;

        // Jump Animation von VORNE starten (Frame 0 erzwingen)
        if (anim != null && anim.enabled) anim.Play("JumpFrogAnim", 0, 0f);

        yield return new WaitForSeconds(jumpDelay);

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector3 worldMousePos = mainCamera.ScreenToWorldPoint(mousePos);
        float direction = worldMousePos.x > transform.position.x ? 1 : -1;

        spriteRenderer.flipX = direction > 0;
        rb.linearVelocity = new Vector2(direction * jumpForceX, jumpForceY);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            HealthSystem playerHealth = collision.gameObject.GetComponent<HealthSystem>();
            if (playerHealth != null) playerHealth.TakeDamage(damageAmount);
        }
    }
}
