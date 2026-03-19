using UnityEngine;


// Controls player movement in my 2D platformer game "Ascend".
// Includes smooth horizontal movement, running, jumping,
// coyote time, jump buffering, sliding,
// and simple ledge grabbing.

public class PlayerMovement : MonoBehaviour
{
    private Animator animator; // Reference to the Animator component for controlling animations
    private SpriteRenderer sr; // Reference to the SpriteRenderer component for flipping the sprite based on movement direction

    private bool isClimbingLedge; // Tracks whether the player is currently climbing up from a ledge grab
    private Vector3 climbStartPos; // Starting position of the player when beginning to climb a ledge
    private Vector3 climbTargetPos; // Target position the player moves towards when climbing a ledge (slightly above and forward of the grab point)
    private float climbProgress; // Tracks the progress of the ledge climb (0 = start, 1 = fully climbed)
    
    private BoxCollider2D boxCollider; // Reference to the BoxCollider2D component for adjusting collider size during sliding

    private Vector2 originalColliderSize; // Stores the normal size of the player's collider
    private Vector2 originalColliderOffset; // Stores the normal offset of the player's collider
    private Vector2 activeSlideColliderSize; // Stores the runtime slide collider size after clamping it to safe values
    private Vector2 activeSlideColliderOffset; // Stores the runtime slide collider offset so the feet stay planted during a slide


    public float climbDuration = 0.25f;   // Time it takes to climb up from a ledge grab
    // Movement Settings
    [Header("Movement")]
    public float moveSpeed = 5f;           // Maximum horizontal speed
    public float acceleration = 60f;       // How quickly the player reaches max speed
    public float deceleration = 80f;       // How quickly the player slows to a stop
    public float runMultiplier = 1.5f;     // Speed increase when holding Shift

    // Jump Settings
    [Header("Jump")]
    public float jumpForce = 14f;          // Upward velocity applied during jump
    public float coyoteTime = 0.1f;        // Time allowed to jump after leaving ground
    public float jumpBufferTime = 0.1f;    // Time jump input is remembered before landing

    // Sliding Settings
    [Header("Slide")]
    public float slideSpeed = 12f;         // Speed applied during a slide
    public float slideDuration = 0.35f;    // How long the slide lasts

    [Header("Slide Collider")]
    public Vector2 slideColliderSize = new Vector2(1f, 0.42f); // Size of the collider while sliding
    public Vector2 slideColliderOffset = new Vector2(0f, -0.25f); // Offset of the collider while sliding

    [Header("Slide Jump")]
    public float slideJumpBoost = 1.1f; // Multiplier to slightly boost jump when jumping out of a slide

    [Header("Slide Momentum")]
    public float postSlideMomentum = 0.85f; // Keeps some horizontal speed after slide ends

    // Tracks whether slide momentum should be applied once after the slide finishes
    private bool applyPostSlideMomentum;

    // Ground Detection
    [Header("Ground Check")]
    public Transform groundCheck;          // Position at the player's feet
    public float groundCheckRadius = 0.2f; // Size of detection circle
    public LayerMask groundLayer;          // Defines what counts as ground

        // Ledge Grab Settings
    [Header("Ledge Grab")]
    public Transform rightLedgeCheck;       // Position used to detect a wall on the player's right side
    public Transform rightLedgeCheckTop;    // Position used to check for empty space above a right ledge
    public Transform leftLedgeCheck;        // Position used to detect a wall on the player's left side
    public Transform leftLedgeCheckTop;     // Position used to check for empty space above a left ledge
    public float ledgeCheckDistance = 0.35f; // Distance of the ledge detection raycasts
    public float ledgeHangDuration = 1.2f;   // Maximum time the player can hang before slipping off
    public float ledgeRegrabCooldown = 0.2f; // Delay before the same ledge can be grabbed again

    

    // Private Variables
    private Rigidbody2D rb;

    private float horizontalInput;         // Stores raw horizontal input value
    private bool isGrounded;               // Tracks whether the player is currently touching the ground

    private float coyoteCounter;           // Counts down the remaining time for coyote time after leaving the ground
    private float jumpBufferCounter;       // Counts down the remaining time for jump buffering after pressing jump

    private bool isSliding;                // Tracks whether the player is currently sliding
    private float slideTimer;              // Counts down the remaining slide time
    private float slideDirection;          // Stores which direction the player slides in

    private Vector3 originalScale;         // Stores the player's normal scale

    private bool isGrabbingLedge;         // Tracks whether the player is currently holding onto a ledge
    private float gravityBeforeLedgeGrab; // Stores the player's normal gravity before ledge grabbing
    private float ledgeHangTimer;         // Counts down how long the player can hang from a ledge
    private float ledgeCooldownTimer;     // Prevents instantly re-grabbing a ledge after falling

    private float facingDirection = 1f;    // Stores the direction the player is facing (1 = right, -1 = left)

    void Start()
    {   
        sr = GetComponent<SpriteRenderer>(); // Cache SpriteRenderer reference for flipping the sprite
        animator = GetComponent<Animator>(); // Cache Animator reference for controlling animations
        // Cache Rigidbody reference for performance
        rb = GetComponent<Rigidbody2D>();

        // Store the player's normal size so it can be restored after sliding
        originalScale = transform.localScale;

        // Store the normal gravity so it can be restored after climbing
        gravityBeforeLedgeGrab = rb.gravityScale;

        boxCollider = GetComponent<BoxCollider2D>(); // Cache BoxCollider reference for adjusting collider size during sliding
        originalColliderSize = boxCollider.size; // Store the normal size of the collider to restore after sliding
        originalColliderOffset = boxCollider.offset; // Store the normal offset of the collider to restore after sliding
        CacheSlideColliderShape();
    }

    void Update()
    {
        if (isClimbingLedge)
        {
            animator.SetFloat("Speed", 0f);
            animator.SetBool("IsLedgeGrabbing", false);
            animator.SetBool("IsClimbingLedge", true);
            animator.SetBool("IsJumping", false);
            animator.SetBool("IsHardFalling", false);
            

            climbProgress += Time.deltaTime / climbDuration;
            transform.position = Vector3.Lerp(climbStartPos, climbTargetPos, climbProgress);

            if (climbProgress >= 1f)
            {
                isClimbingLedge = false;
                rb.gravityScale = gravityBeforeLedgeGrab;
            }

            return;
        }

        // Read horizontal input (-1 = left, 1 = right)
        horizontalInput = Input.GetAxisRaw("Horizontal");

        if (horizontalInput > 0)
        {
            sr.flipX = false;
        }
        else if (horizontalInput < 0)
        {
            sr.flipX = true;
        }

        if (isGrabbingLedge || isClimbingLedge)
        {
            animator.SetFloat("Speed", 0f);
        }
        else
        {
            animator.SetFloat("Speed", Mathf.Abs(horizontalInput));
        }
        animator.SetBool("IsLedgeGrabbing", isGrabbingLedge);
        animator.SetBool("IsClimbingLedge", isClimbingLedge);
        animator.SetBool("IsJumping", !isGrounded && !isGrabbingLedge && !isClimbingLedge);
        animator.SetFloat("VerticalSpeed", rb.linearVelocity.y);
        animator.SetBool("IsHardFalling", rb.linearVelocity.y < -15f && !isGrounded && !isGrabbingLedge && !isClimbingLedge);
        

        // Apply run multiplier if Shift is held
        if (Input.GetKey(KeyCode.LeftShift))
        {
            horizontalInput *= runMultiplier;
        }

        // Update the direction the player is facing based on movement input
        if (horizontalInput > 0.1f)
        {
            facingDirection = 1f;
        }
        else if (horizontalInput < -0.1f)
        {
            facingDirection = -1f;
        }

        
        // Check if player is touching the ground using a small overlap circle
        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );

        // If grounded, refresh the coyote timer
        if (isGrounded)
        {
            coyoteCounter = coyoteTime;
        }
        else
        {
            // Reduce coyote timer while airborne
            coyoteCounter -= Time.deltaTime;
        }

        if (ledgeCooldownTimer > 0f)
        {
            ledgeCooldownTimer -= Time.deltaTime;
        }

        // Check for a ledge only while airborne and not already sliding or grabbing
        if (!isGrounded && !isSliding && !isGrabbingLedge && ledgeCooldownTimer <= 0f)
        {
            CheckLedgeGrab();
        }

        // While grabbing a ledge, allow the player to climb up using W or Space
        if (isGrabbingLedge)
        {
            ledgeHangTimer -= Time.deltaTime;

            if (ledgeHangTimer <= 0f)
            {
                ReleaseLedge();
                return;
            }

            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space))
            {
                ClimbLedge();
            }

            return;
        }

        // Start a slide when Left Control is pressed,
        // if the player is grounded, moving, and not already sliding
        if (Input.GetKeyDown(KeyCode.LeftControl) && isGrounded && Mathf.Abs(horizontalInput) > 0.1f && !isSliding)
        {
            isSliding = true;
            slideTimer = slideDuration;
            slideDirection = Mathf.Sign(horizontalInput);

            // Visually squash the player to indicate sliding
            // transform.localScale = new Vector3(originalScale.x * 1.3f, originalScale.y * 0.6f, originalScale.z);
            animator.Play("Slide", 0, 0f);
            boxCollider.size = activeSlideColliderSize; // Adjust collider size for sliding without widening into nearby geometry
            boxCollider.offset = activeSlideColliderOffset; // Keep the slide collider aligned with the player's feet
        }

        // Reduce the slide timer while sliding
        if (isSliding)
        {
            slideTimer -= Time.deltaTime;

            // If the player presses jump during a slide, cancel the slide and jump immediately
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W))
            {
                // End the slide
                isSliding = false;

                // Restore the player's normal collider before jumping
                boxCollider.size = originalColliderSize;
                boxCollider.offset = originalColliderOffset;

                // Reset vertical velocity so the jump is consistent
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);

                // Apply a slightly boosted jump out of the slide
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce * slideJumpBoost);

                // Clear timers so the jump cannot be double-triggered
                coyoteCounter = 0f;
                jumpBufferCounter = 0f;
                isGrounded = false;

                // Enable momentum carry after the slide jump
                applyPostSlideMomentum = true;
            }

            // End the slide when the timer runs out
            else if (slideTimer <= 0f)
            {
                // Check whether there is enough room above the player to stand up safely
                bool blockedAbove = Physics2D.OverlapBox(
                    (Vector2)transform.position + originalColliderOffset,
                    originalColliderSize,
                    0f,
                    groundLayer
                );

                // Only stand back up if there is space
                if (!blockedAbove)
                {
                    // End the slide
                    isSliding = false;

                    // Restore the player's normal collider
                    boxCollider.size = originalColliderSize;
                    boxCollider.offset = originalColliderOffset;

                    // Enable one burst of momentum carry after the slide finishes
                    applyPostSlideMomentum = true;
                }
                else
                {
                    // Keep the player sliding for a little longer until there is room to stand
                    slideTimer = 0.1f;
                }
            }
        }

        // Store jump input briefly using jump buffering
        // Allows the player to jump using either Space or W
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W))
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        // Execute jump only if:
        // - jump was pressed recently
        // - player is on the ground or still within coyote time
        if (jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            // Reset vertical velocity to ensure a consistent jump height
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);

            // Apply upward jump velocity
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            coyoteCounter = 0f;

            // Player is now airborne, so clear the buffered jump
            isGrounded = false;
            jumpBufferCounter = 0f;
        }

        animator.SetBool("IsSliding", isSliding);
    }

    void FixedUpdate()
    {
        // Prevent normal movement forces while the player is grabbing a ledge
        if (isGrabbingLedge || isClimbingLedge)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Use slide speed instead of normal movement speed while sliding
        float targetSpeed;

        if (isSliding)
        {
            // While sliding, force the player to move at slide speed
            targetSpeed = slideDirection * slideSpeed;
        }
        else if (applyPostSlideMomentum)
        {
            // After the slide ends, keep some of the slide speed for a smoother transition
            targetSpeed = slideDirection * slideSpeed * postSlideMomentum;

            // Apply this only once
            applyPostSlideMomentum = false;
        }
        else
        {
            // Normal movement when not sliding
            targetSpeed = horizontalInput * moveSpeed;
        }

        // Find the difference between current and desired speed
        float speedDifference = targetSpeed - rb.linearVelocity.x;

        // Choose acceleration or deceleration depending on movement state
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f)
            ? acceleration
            : deceleration;

        // Apply force proportionally for smooth acceleration
        float movement = speedDifference * accelRate;

        rb.AddForce(movement * Vector2.right);
    }

    void CacheSlideColliderShape()
    {
        // Never let the slide collider become wider or taller than the standing collider,
        // otherwise it can snag on ceilings or get pushed into the ground.
        float slideWidth = Mathf.Clamp(slideColliderSize.x, 0.05f, originalColliderSize.x);
        float slideHeight = Mathf.Clamp(slideColliderSize.y, 0.05f, originalColliderSize.y);

        activeSlideColliderSize = new Vector2(slideWidth, slideHeight);

        float standingBottom = originalColliderOffset.y - (originalColliderSize.y * 0.5f);
        float slideOffsetY = standingBottom + (slideHeight * 0.5f);

        activeSlideColliderOffset = new Vector2(slideColliderOffset.x, slideOffsetY);
    }

           
    // Checks whether the player has reached a ledge that can be grabbed
    void CheckLedgeGrab()
    {
        Transform activeLedgeCheck;
        Transform activeLedgeCheckTop;
        Vector2 rayDirection;

        // Choose the correct ledge check points based on facing direction
        if (facingDirection > 0f)
        {
            activeLedgeCheck = rightLedgeCheck;
            activeLedgeCheckTop = rightLedgeCheckTop;
            rayDirection = Vector2.right;
        }
        else
        {
            activeLedgeCheck = leftLedgeCheck;
            activeLedgeCheckTop = leftLedgeCheckTop;
            rayDirection = Vector2.left;
        }

        // Cast a ray forward at chest height to detect a wall
        RaycastHit2D wallHit = Physics2D.Raycast(
            activeLedgeCheck.position,
            rayDirection,
            ledgeCheckDistance,
            groundLayer
        );

        // Cast a second ray forward at head height to check for empty space
        RaycastHit2D topHit = Physics2D.Raycast(
            activeLedgeCheckTop.position,
            rayDirection,
            ledgeCheckDistance,
            groundLayer
        );

        // A ledge is detected if there is a wall at the lower point but no wall above it
        if (wallHit && !topHit)
        {
            GrabLedge();
        }
    }

    // Freezes the player in place when a ledge is grabbed
    void GrabLedge()
    {
        // Mark the player as grabbing a ledge
        isGrabbingLedge = true;

        // Reset the ledge hang timer
        ledgeHangTimer = ledgeHangDuration;
        
        // Cancel sliding if a ledge is grabbed
        isSliding = false;

        // Restore the normal collider in case the player was sliding
        boxCollider.size = originalColliderSize;
        boxCollider.offset = originalColliderOffset;

        // Stop all movement so the player hangs still
        rb.linearVelocity = Vector2.zero;

        // Temporarily disable gravity so the player does not fall
        rb.gravityScale = 0f;
    }

    // Moves the player up and onto the platform after climbing
    void ClimbLedge()
    {
        isGrabbingLedge = false;
        isClimbingLedge = true;

        climbStartPos = transform.position;
        climbTargetPos = transform.position + new Vector3(facingDirection * 0.5f, 1f, 0f);
        climbProgress = 0f;

        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
        ledgeCooldownTimer = ledgeRegrabCooldown;

        animator.SetBool("IsLedgeGrabbing", false);
        animator.SetBool("IsClimbingLedge", true);
        animator.Play("LedgeClimb", 0, 0f);
    }
    // Makes the player lose their grip and fall after hanging too long
    void ReleaseLedge()
    {
        isGrabbingLedge = false;
        rb.gravityScale = gravityBeforeLedgeGrab;
        ledgeCooldownTimer = ledgeRegrabCooldown;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, -2f);
    }
    
}
