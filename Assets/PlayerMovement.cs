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

    [Header("Physics")]
    public Rigidbody2D rb;
    public float fallMultiplier = 7f;
    public float lowJumpMultiplier = 5f;
    
    Vector2 moveInput;
    bool isGrounded;
    public Transform groundCheck;
    public Vector2 groundCheckSize = new Vector2(0.5f, 0.1f);
    public LayerMask whatIsGround;

    void Update()
    {
        if (isGrounded)
        {
            coyoteCounter = coyoteTime;
        }
        else
        {
            coyoteCounter -= Time.deltaTime;
        }

        // klettern
        if (isTouchingLadder && Mathf.Abs(moveInput.y) > 0.01f)
        {
            actuallyClimbing = true;
        }

        // stop klettern
        if (!isTouchingLadder)
        {
            actuallyClimbing = false;
        }
    }

    void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    void OnJump(InputValue value)
    {
        jumpButtonHeld = value.isPressed;

        if (jumpButtonHeld)
        {
            if (actuallyClimbing)
            {
                // abspringen
                actuallyClimbing = false;
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            }
            else if (coyoteCounter > 0f)
            {
                // sprung
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                coyoteCounter = 0f;
            }
        }
    }

    void FixedUpdate()
    {
        isGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, whatIsGround);
        
        // leiter check
        isTouchingLadder = rb.IsTouchingLayers(whatIsLadder);

        if (actuallyClimbing)
        {
            rb.gravityScale = 0f;
            // kletter movement
            rb.linearVelocity = new Vector2(moveInput.x * speed, moveInput.y * climbSpeed);
        }
        else
        {
            // movement
            float targetSpeed = moveInput.x * speed;
            float speedDif = targetSpeed - rb.linearVelocity.x;
            float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;
            float movement = speedDif * accelRate;

            rb.AddForce(movement * Vector2.right, ForceMode2D.Force);

            // gravity logic
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
    }
}
