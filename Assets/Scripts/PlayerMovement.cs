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
    
    public float jumpBufferTime = 0.15f;
    float jumpBufferCounter;
    
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

    [Header("Water")]
    public float waterSwimSpeed = 8f;
    public float bobbingAmplitude = 0.2f;
    public float bobbingFrequency = 2f;
    [Tooltip("Verschiebt den Spieler hoch oder runter, damit er exakt auf der Wasserlinie schwimmt (z.B. -0.5 oder 0.2)")]
    public float waterSurfaceOffset = -0.5f;
    
    [Header("Water Animation")]
    [Tooltip("Sprite, das angezeigt wird, wenn der Spieler im Wasser schwimmt, aber sich NICHT bewegt (Idle).")]
    public Sprite waterIdleSprite;
    [Tooltip("Animations-Geschwindigkeit für die Walk-Animation, während der Spieler im Wasser schwimmt.")]
    public float waterWalkAnimSpeed = 0.6f;

    bool isInWater;
    float waterTimer;
    float waterSurfaceY;

    [Header("Physics")]
    public Rigidbody2D rb;
    public float fallMultiplier = 7f;
    public float lowJumpMultiplier = 5f;
    
    [Header("One-Way Platforms")]
    [Tooltip("Der genaue Name des Layers in Unity für die Plattformen (z.B. 'OneWayPlatform')")]
    public string oneWayLayerName = "OneWayPlatform";
    public float fallThroughDuration = 0.05f; // Noch kleiner, damit er direkt wieder fest wird
    private bool wasPushingDown = false;

    [Header("Audio")]
    public float stepInterval = 0.35f; // Wie viele Sekunden zwischen jedem Schritt?
    private float stepTimer = 0f;

    [Header("Poison Effect")]
    public float poisonSpeedMultiplier = 0.5f;
    private bool isInPoison = false;
    
    Vector2 moveInput;
    bool isGrounded;
    public Transform groundCheck;
    public Vector2 groundCheckSize = new Vector2(0.5f, 0.1f);
    public LayerMask whatIsGround;
    [Tooltip("Puffer-Zeit (Sekunden), um nerviges Animations-Flackern auf unebenen Tilemaps zu ignorieren")]
    public float groundGracePeriod = 0.1f;
    private float groundGraceTimer;

    [Header("Animation")]
    public Animator animator;
    public SpriteRenderer spriteRenderer;

    bool SpaceIsActuallyHeld()
    {
        return Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
    }

    void Update()
    {
        bool prevJumpHeld = jumpButtonHeld;
        jumpButtonHeld = SpaceIsActuallyHeld();
        
        bool isPushingDown = moveInput.y < -0.5f;

        // === ONE-WAY PLATFORM DROP (NUR S DRÜCKEN) ===
        if (isPushingDown && !wasPushingDown && isGrounded && !isOnLiane && !actuallyClimbing && !isInWater)
        {
            DropThroughPlatform();
        }
        wasPushingDown = isPushingDown;

        // === JUMP BUFFERING (Taste kurz vor dem Landen drücken) ===
        if (jumpButtonHeld && !prevJumpHeld)
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        if (jumpBufferCounter > 0f)
        {
            // Wenn man den Jump-Buffer hat, versuche zu springen (OnSpacePressed ist jetzt sicher)
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

    void DropThroughPlatform()
    {
        int oneWayLayer = LayerMask.NameToLayer(oneWayLayerName);
        if (oneWayLayer == -1) return;

        Collider2D[] grounds = Physics2D.OverlapBoxAll(groundCheck.position, groundCheckSize, 0f);
        Collider2D[] myCols = GetComponents<Collider2D>();
        
        bool dropped = false;
        foreach (Collider2D groundCol in grounds)
        {
            if (groundCol.gameObject.layer == oneWayLayer)
            {
                foreach (Collider2D myCol in myCols)
                {
                    StartCoroutine(IgnorePlatformTemporarily(myCol, groundCol));
                }
                dropped = true;
            }
        }

        if (dropped)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -2f);
        }
    }

    private System.Collections.IEnumerator IgnorePlatformTemporarily(Collider2D playerCol, Collider2D platformCol)
    {
        Physics2D.IgnoreCollision(playerCol, platformCol, true);
        
        // Wartet exakt den eingestellten Wert (im Inspector änderbar!)
        yield return new WaitForSeconds(fallThroughDuration);
        
        if (playerCol != null && platformCol != null)
        {
            Physics2D.IgnoreCollision(playerCol, platformCol, false);
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
        if (isOnLiane || isInWater) return;

        if (actuallyClimbing)
        {
            actuallyClimbing = false;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpBufferCounter = 0f; // Buffer leeren, da gesprungen
        }
        else if (coyoteCounter > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            coyoteCounter = 0f;
            jumpBufferCounter = 0f; // Buffer leeren
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
        // 1. BODEN-CHECK (Mit Anti-Flacker-Puffer für Tilemaps)
        bool groundOverlap = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, whatIsGround);
        
        // Wir prüfen zusätzlich, ob wir IRGENDWAS berühren (auch Pilze oder Trigger)
        bool touchingAnything = rb.IsTouchingLayers(Physics2D.AllLayers);
        
        bool actualGround = groundOverlap || (touchingAnything && Mathf.Abs(rb.linearVelocity.y) < 0.1f);

        // Puffer-Timer: Verhindert, dass der Player bei einem 1-Frame-Hüpfer sofort die Jump-Animation startet
        if (actualGround)
        {
            groundGraceTimer = groundGracePeriod;
        }
        else
        {
            groundGraceTimer -= Time.fixedDeltaTime;
        }

        isGrounded = groundGraceTimer > 0f;

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

        // Liane & Water check per Tag
        isTouchingLiane = false;
        isInWater = false;
        currentLiane = null;
        currentLianeRb = null;
        
        ContactFilter2D filter = new ContactFilter2D();
        filter.useTriggers = true;
        filter.SetLayerMask(Physics2D.AllLayers);
        Collider2D[] hits = new Collider2D[10];
        int count = Physics2D.OverlapCircle(transform.position, lianeDetectionRadius, filter, hits);
        for (int i = 0; i < count; i++)
        {
            if (hits[i] != null)
            {
                if (hits[i].CompareTag("Liane"))
                {
                    isTouchingLiane = true;
                    currentLiane = hits[i].transform;
                    currentLianeRb = hits[i].GetComponent<Rigidbody2D>();
                }
                else if (hits[i].CompareTag("Water"))
                {
                    isInWater = true;
                    // Die oberste Kante des Water-Triggers ist unsere Wasseroberfläche
                    waterSurfaceY = hits[i].bounds.max.y;
                }
            }
        }

        if (isInWater)
        {
            rb.gravityScale = 0f;
            
            // X Movement (Schwimmen)
            float currentSpeed = waterSwimSpeed;
            if (isInPoison) currentSpeed *= poisonSpeedMultiplier;
            rb.linearVelocity = new Vector2(moveInput.x * currentSpeed, 0f);

            // Y Movement (Bobbing & Auftauchen)
            waterTimer += Time.fixedDeltaTime;
            
            // Spieler treibt sanft nach oben zur Wasseroberfläche und wippt dort
            // Das Offset bestimmt, ob der Körper tief oder flach im Wasser hängt.
            float targetY = waterSurfaceY + waterSurfaceOffset + Mathf.Sin(waterTimer * bobbingFrequency) * bobbingAmplitude;
            
            // Sanftes Auftauchen (Lerp)
            float newY = Mathf.Lerp(transform.position.y, targetY, Time.fixedDeltaTime * 5f);
            
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }
        else if (actuallyClimbing)
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
            float currentSpeed = speed;
            if (isInPoison) currentSpeed *= poisonSpeedMultiplier;

            float targetSpeed = moveInput.x * currentSpeed;
            float speedDif = targetSpeed - rb.linearVelocity.x;
            float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;
            float movement = speedDif * accelRate;

            rb.AddForce(movement * Vector2.right, ForceMode2D.Force);

            // --- NEU: Footstep Sounds abspielen ---
            if (isGrounded && Mathf.Abs(moveInput.x) > 0.01f)
            {
                stepTimer += Time.fixedDeltaTime;
                if (stepTimer >= stepInterval)
                {
                    stepTimer = 0f;
                    if (SceneSoundManager.Instance != null)
                    {
                        SceneSoundManager.Instance.PlayFootstep();
                    }
                }
            }
            else
            {
                // Timer sofort füllen, damit der allererste Schritt beim Anlaufen sofort hörbar ist!
                stepTimer = stepInterval;
            }

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

        UpdateAnimations();
    }

    void UpdateAnimations()
    {
        if (animator == null) return;

        // 1. FLIPPEN (Blickrichtung)
        if (moveInput.x > 0.01f && spriteRenderer != null)
        {
            spriteRenderer.flipX = false;
        }
        else if (moveInput.x < -0.01f && spriteRenderer != null)
        {
            spriteRenderer.flipX = true;
        }

        // 2. PARAMETER ÜBERGEBEN
        bool isMoving = Mathf.Abs(moveInput.x) > 0.01f;

        if (isInWater)
        {
            if (isMoving)
            {
                // Wenn wir im Wasser schwimmen: Normale Walk-Animation, aber angepasste Geschwindigkeit!
                animator.enabled = true;
                animator.speed = waterWalkAnimSpeed;
                
                animator.SetBool("isWalking", true);
                animator.SetBool("isGrounded", true); // Fake "Grounded", damit Walk abspielt
                animator.SetFloat("yVelocity", 0f);
                animator.SetBool("isClimbing", false);
            }
            else
            {
                // Wenn wir im Wasser stillstehen (Idle): Animator komplett ausschalten und statisches Sprite setzen!
                animator.enabled = false;
                if (waterIdleSprite != null && spriteRenderer != null)
                {
                    spriteRenderer.sprite = waterIdleSprite;
                }
            }
        }
        else
        {
            // --- Normales Verhalten (Land) ---
            animator.enabled = true;
            animator.speed = 1f; // Zurücksetzen auf Normal-Geschwindigkeit

            animator.SetBool("isWalking", isMoving);
            animator.SetBool("isGrounded", isGrounded);
            animator.SetFloat("yVelocity", rb.linearVelocity.y);
            animator.SetBool("isClimbing", actuallyClimbing || isOnLiane);
        }
    }

    public void SetInPoison(bool value)
    {
        isInPoison = value;
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