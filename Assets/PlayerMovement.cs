using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 14f; 
    public float acceleration = 80f;
    public float deceleration = 80f;

    [Header("Jump")]
    public float jumpForce = 18f;
    public float coyoteTime = 0.15f;
    float coyoteCounter;
    bool jumpButtonHeld;

    [Header("Climbing")]
    public float climbSpeed = 8f;
    bool isTouchingLadder;
    bool actuallyClimbing;
    public LayerMask whatIsLadder;

    [Header("Liane")]
    public float lianeClimbSpeed = 5f;
    public float lianeDetectionRadius = 0.6f;
    public float lianeExitCooldown = 0.3f;
    public float lianeSideJumpForce = 10f;
    public float lianePushTorque = 15f;
    public float lianeSnapSpeed = 15f;
    public float lianeSwingDuration = 0.4f; // Wie lange die Kraft angewendet wird
    bool isTouchingLiane;
    bool isOnLiane;
    float lianeExitTimer;
    Transform currentLiane;
    Rigidbody2D currentLianeRb;

    // Swing-Kraft über Zeit
    float swingTimer;
    float swingDirection; // -1 oder +1
    Rigidbody2D swingTargetRb;

    [Header("Physics")]
    public Rigidbody2D rb;
    public float fallMultiplier = 7f;
    public float lowJumpMultiplier = 5f;
    
    Vector2 moveInput;
    bool isGrounded;
    public Transform groundCheck;
    public Vector2 groundCheckSize = new Vector2(0.5f, 0.1f);
    public LayerMask whatIsGround;

    bool SpaceIsActuallyHeld()
    {
        return Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
    }

    void Update()
    {
        bool prevJumpHeld = jumpButtonHeld;
        jumpButtonHeld = SpaceIsActuallyHeld();

        if (jumpButtonHeld && !prevJumpHeld)
        {
            OnSpacePressed();
        }
        if (!jumpButtonHeld && prevJumpHeld)
        {
            OnSpaceReleased();
        }

        if (isGrounded)
        {
            coyoteCounter = coyoteTime;
        }
        else
        {
            coyoteCounter -= Time.deltaTime;
        }

        if (lianeExitTimer > 0f)
        {
            lianeExitTimer -= Time.deltaTime;
        }

        if (isTouchingLadder && Mathf.Abs(moveInput.y) > 0.01f)
        {
            actuallyClimbing = true;
        }

        if (!isTouchingLadder)
        {
            actuallyClimbing = false;
        }

        // === LIANE LOGIC ===
        if (isTouchingLiane && !isOnLiane && moveInput.y > 0.01f && lianeExitTimer <= 0f)
        {
            isOnLiane = true;

            if (currentLiane != null && currentLianeRb != null)
            {
                float enterSide = Mathf.Sign(transform.position.x - currentLiane.position.x);
                
                // Starte sanftes Schwingen über Zeit
                swingDirection = -enterSide;
                swingTimer = lianeSwingDuration;
                swingTargetRb = currentLianeRb;
            }
        }

        if (isOnLiane && Mathf.Abs(moveInput.x) > 0.01f)
        {
            ExitLiane(new Vector2(moveInput.x * lianeSideJumpForce, jumpForce * 0.5f), true);
            return;
        }

        if (isGrounded && isOnLiane)
        {
            ExitLiane(Vector2.zero, false);
        }

        if (!isTouchingLiane && isOnLiane)
        {
            ExitLiane(Vector2.zero, false);
        }
    }

    void ExitLiane(Vector2 playerVelocity, bool applyPush)
    {
        if (applyPush && currentLianeRb != null && playerVelocity.magnitude > 1f)
        {
            // Sanftes Gegenschwingen beim Abspringen
            float exitDirection = Mathf.Sign(playerVelocity.x);
            swingDirection = exitDirection;
            swingTimer = lianeSwingDuration * 0.5f;
            swingTargetRb = currentLianeRb;
        }

        isOnLiane = false;
        lianeExitTimer = lianeExitCooldown;
        rb.gravityScale = 1f;
        rb.linearVelocity = playerVelocity;
        
        currentLianeRb = null;
    }

    void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    void OnSpacePressed()
    {
        if (isOnLiane) return;

        if (actuallyClimbing)
        {
            actuallyClimbing = false;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
        else if (coyoteCounter > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            coyoteCounter = 0f;
        }
    }

    void OnSpaceReleased()
    {
        if (isOnLiane)
        {
            ExitLiane(new Vector2(rb.linearVelocity.x, jumpForce), true);
        }
    }

    void FixedUpdate()
    {
        isGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, whatIsGround);
        isTouchingLadder = rb.IsTouchingLayers(whatIsLadder);

        // === SANFTES SCHWINGEN ÜBER ZEIT ===
        if (swingTimer > 0f && swingTargetRb != null)
        {
            // Kraft wird jeden Frame angewendet und wird schwächer über Zeit
            float strength = (swingTimer / lianeSwingDuration); // 1.0 → 0.0 fade out
            swingTargetRb.AddTorque(swingDirection * lianePushTorque * strength, ForceMode2D.Force);
            swingTimer -= Time.fixedDeltaTime;

            if (swingTimer <= 0f)
            {
                swingTargetRb = null;
            }
        }

        // Liane check per Tag
        isTouchingLiane = false;
        currentLiane = null;
        currentLianeRb = null;
        ContactFilter2D filter = new ContactFilter2D();
        filter.useTriggers = true;
        filter.SetLayerMask(Physics2D.AllLayers);
        Collider2D[] hits = new Collider2D[10];
        int count = Physics2D.OverlapCircle(transform.position, lianeDetectionRadius, filter, hits);
        for (int i = 0; i < count; i++)
        {
            if (hits[i] != null && hits[i].CompareTag("Liane"))
            {
                isTouchingLiane = true;
                currentLiane = hits[i].transform;
                currentLianeRb = hits[i].GetComponent<Rigidbody2D>();
                break;
            }
        }

        if (actuallyClimbing)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = new Vector2(moveInput.x * speed, moveInput.y * climbSpeed);
        }
        else if (isOnLiane)
        {
            rb.gravityScale = 0f;

            // Flüssiges Snapping zur Liane
            if (currentLiane != null)
            {
                float targetX = currentLiane.position.x;
                float currentX = transform.position.x;
                float newX = Mathf.MoveTowards(currentX, targetX, lianeSnapSpeed * Time.fixedDeltaTime);
                transform.position = new Vector3(newX, transform.position.y, transform.position.z);
            }

            if (moveInput.y > 0.01f)
            {
                rb.linearVelocity = new Vector2(0f, lianeClimbSpeed);
            }
            else if (moveInput.y < -0.01f)
            {
                rb.linearVelocity = new Vector2(0f, -lianeClimbSpeed);
            }
            else if (jumpButtonHeld)
            {
                rb.linearVelocity = Vector2.zero;
            }
            else
            {
                ExitLiane(new Vector2(rb.linearVelocity.x, -1f), false);
            }
        }
        else
        {
            float targetSpeed = moveInput.x * speed;
            float speedDif = targetSpeed - rb.linearVelocity.x;
            float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;
            float movement = speedDif * accelRate;

            rb.AddForce(movement * Vector2.right, ForceMode2D.Force);

            if (rb.linearVelocity.y < 0)
            {
                rb.gravityScale = fallMultiplier;
            }
            else if (rb.linearVelocity.y > 0 && !jumpButtonHeld)
            {
                rb.gravityScale = lowJumpMultiplier;
            }
            else
            {
                rb.gravityScale = 1f;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawCube(groundCheck.position, groundCheckSize);
        }

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, lianeDetectionRadius);
    }
}