using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class SimplePlayerController : MonoBehaviour
{
    [Header("Bewegung")]
    public float moveSpeed = 7f;
    public float jumpForce = 14f;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private bool isGrounded;
    private float horizontalInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        rb.freezeRotation = true; 
    }

    private bool isAutoWalking = false;
    private float autoWalkTargetX;
    private System.Action onAutoWalkReached;

    public void WalkTo(float targetX, System.Action onReached)
    {
        isAutoWalking = true;
        autoWalkTargetX = targetX;
        onAutoWalkReached = onReached;
    }

    void Update()
    {
        horizontalInput = 0f;

        if (isAutoWalking)
        {
            float diff = autoWalkTargetX - transform.position.x;
            if (Mathf.Abs(diff) < 0.1f)
            {
                // Ziel erreicht
                isAutoWalking = false;
                horizontalInput = 0f;
                onAutoWalkReached?.Invoke();
                onAutoWalkReached = null;
            }
            else
            {
                // In Richtung Ziel laufen
                horizontalInput = Mathf.Sign(diff);
            }
        }
        else if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                horizontalInput = -1f;
            else if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                horizontalInput = 1f;

            if (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame)
            {
                Jump();
            }
        }

        if (horizontalInput > 0)
            spriteRenderer.flipX = false;
        else if (horizontalInput < 0)
            spriteRenderer.flipX = true;

        UpdateAnimations();
    }

    void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        CheckGround(collision);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        CheckGround(collision);
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        // Wenn wir den Boden verlassen (z.B. Springen oder Klippe runterfallen)
        isGrounded = false;
    }

    private void CheckGround(Collision2D collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).normal.y > 0.5f)
            {
                isGrounded = true;
                return;
            }
        }
    }

    private void Jump()
    {
        if (isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
    }

    private void UpdateAnimations()
    {
        bool isWalking = Mathf.Abs(horizontalInput) > 0.1f;
        animator.SetBool("isWalking", isWalking);
        animator.SetBool("isGrounded", isGrounded);
    }
}
