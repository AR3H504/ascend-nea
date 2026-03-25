using System.Reflection;
using UnityEngine;


// Controls player movement in my 2D platformer game "Ascend".
// Includes smooth horizontal movement, running, jumping,
// coyote time, jump buffering, sliding,
// and simple ledge grabbing.

public class PlayerMovement : MonoBehaviour
{
    private Animator animator; // Reference to the Animator component for controlling animations
    private SpriteRenderer sr; // Reference to the SpriteRenderer component for flipping the sprite based on movement direction
    private AudioSource oneShotAudioSource;
    private AudioSource footstepLoopSource;
    private AudioSource slideLoopSource;

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
    public float runMultiplier = 1.5f;     // Speed increase while holding the sprint input
    public bool sprintEnabled = true;      // Master switch so other systems can still disable sprint completely
    public bool useSprintHeightGate;       // Enables sprint only when the player is above the unlock height
    public float sprintUnlockYHeight;      // Sprint becomes available at or above this world Y position

    // Jump Settings
    [Header("Jump")]
    public float jumpForce = 14f;          // Upward velocity applied during jump
    public float coyoteTime = 0.1f;        // Time allowed to jump after leaving ground
    public float jumpBufferTime = 0.1f;    // Time jump input is remembered before landing

    // Sliding Settings
    [Header("Slide")]
    public float slideSpeed = 12f;         // Speed applied during a slide
    public float slideDuration = 0.5f;    // How long the slide lasts
    [Range(0f, 1f)]
    public float slideHoldPoseTime = 0.8f; // Normalized point in the slide clip to hold when the player cannot stand up yet

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
    public LayerMask ledgeGrabbableLayer;   // Only colliders on this layer can be grabbed as ledges
    public float ledgeCheckDistance = 0.35f; // Distance of the ledge detection raycasts
    public float maxLedgeGrabHeight = 0.6f; // Prevents snapping to ledges that are far above the player, such as tall walls or big blocks
    public float ledgeHangDuration = 1.2f;   // Maximum time the player can hang before slipping off
    public float ledgeRegrabCooldown = 0.2f; // Delay before the same ledge can be grabbed again
    // Private Variables
    private Rigidbody2D rb;

    private float horizontalInput;         // Stores raw horizontal input value
    private bool isGrounded;               // Tracks whether the player is currently touching the ground
    private Collider2D groundedCollider;   // Stores which collider the player is currently standing on
    private Rigidbody2D activePlatformBody; // Stores the rigidbody of the platform under the player
    private MovingPlatform2D activePlatformScript; // Stores the moving platform script under the player
    private Vector2 activePlatformVelocity; // Stores the platform movement for the current physics step

    private float coyoteCounter;           // Counts down the remaining time for coyote time after leaving the ground
    private float jumpBufferCounter;       // Counts down the remaining time for jump buffering after pressing jump

    private bool sprintToggleActive;       // Tracks whether sprint has been toggled on with Shift
    private bool isSliding;                // Tracks whether the player is currently sliding
    private bool isHoldingSlidePose;       // Tracks whether the slide animation is frozen on a low hold pose
    private bool queuedClimbSlide;         // Lets the player buffer a slide input during ledge climb for tight climb-then-slide sections
    private float slideTimer;              // Counts down the remaining slide time
    private float slideDirection;          // Stores which direction the player slides in

    private Vector3 originalScale;         // Stores the player's normal scale

    private bool isGrabbingLedge;         // Tracks whether the player is currently holding onto a ledge
    private float gravityBeforeLedgeGrab; // Stores the player's normal gravity before ledge grabbing
    private float ledgeHangTimer;         // Counts down how long the player can hang from a ledge
    private float ledgeCooldownTimer;     // Prevents instantly re-grabbing a ledge after falling
    private float facingDirection = 1f;    // Stores the direction the player is facing (1 = right, -1 = left)
    public Vector2 ledgeHangOffset = new Vector2(0.35f, -0.15f); // Offset from the ledge corner to place the player into a clean hang pose
    private bool wasGroundedLastFrame;

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
        CacheLedgeGrabbableLayer();
        InitializeAudioSources();
    }

    void Update()
    {
        if (isClimbingLedge)
        {
            if (Input.GetKey(KeyCode.LeftControl))
            {
                queuedClimbSlide = true;
            }

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

                if (!CanStandUp())
                {
                    if (queuedClimbSlide)
                    {
                        BeginSlide(facingDirection);
                    }
                    else
                    {
                        transform.position = climbStartPos;
                        ReleaseLedge();
                    }
                }

                queuedClimbSlide = false;
            }

            return;
        }

        // Read horizontal input (-1 = left, 1 = right)
        horizontalInput = Input.GetAxisRaw("Horizontal");

        if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
        {
            sprintToggleActive = !sprintToggleActive;
        }

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

        // Apply the sprint multiplier while sprint is toggled on.
        if (CanSprint() && sprintToggleActive)
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
        groundedCollider = Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            GetSolidCollisionMask()
        );
        isGrounded = groundedCollider != null;

        if (!wasGroundedLastFrame && isGrounded)
        {
            PlayOneShot(GameAudio.LandingClip);
        }

        activePlatformScript = GetActiveMovingPlatformComponent();
        activePlatformBody = GetActivePlatformBody();
        activePlatformVelocity = GetActivePlatformVelocity();

        animator.SetBool("IsLedgeGrabbing", isGrabbingLedge);
        animator.SetBool("IsClimbingLedge", isClimbingLedge);
        animator.SetBool("IsJumping", !isGrounded && !isGrabbingLedge && !isClimbingLedge);
        animator.SetFloat("VerticalSpeed", rb.linearVelocity.y);
        animator.SetBool("IsHardFalling", rb.linearVelocity.y < -10f && !isGrounded && !isGrabbingLedge && !isClimbingLedge);

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
        if (!isGrounded && !isGrabbingLedge && ledgeCooldownTimer <= 0f)
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
        if (Input.GetKeyDown(KeyCode.LeftControl) && Mathf.Abs(horizontalInput) > 0.1f && !isSliding)
        {
            BeginSlide(Mathf.Sign(horizontalInput));
        }

        // Reduce the slide timer while sliding
        if (isSliding)
        {
            slideTimer -= Time.deltaTime;

            // If the configured slide lasts longer than the authored animation clip,
            // freeze on the low slide pose instead of showing the final bad frame.
            if (!isHoldingSlidePose && HasReachedSlideHoldPose())
            {
                HoldSlidePose();
            }

            // If the player presses jump during a slide, cancel the slide and jump immediately
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W))
            {
                // Only let the player jump out of a slide once there is room to stand,
                // otherwise keep them in the low slide collider under the obstacle.
                if (isGrounded && CanStandUp())
                {
                    // End the slide
                    isSliding = false;
                    isHoldingSlidePose = false;
                    animator.speed = 1f;

                    // Restore the player's normal collider before jumping
                    boxCollider.size = originalColliderSize;
                    boxCollider.offset = originalColliderOffset;

                    // Reset vertical velocity so the jump is consistent
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);

                    // Apply a slightly boosted jump out of the slide
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, GetModifiedJumpForce() * slideJumpBoost);
                    PlayOneShot(GameAudio.JumpClip);

                    // Clear timers so the jump cannot be double-triggered
                    coyoteCounter = 0f;
                    jumpBufferCounter = 0f;
                    isGrounded = false;

                    // Enable momentum carry after the slide jump
                    applyPostSlideMomentum = true;
                }
                else
                {
                    HoldSlidePose();
                    slideTimer = 0.1f;
                }
            }

            // End the slide when the timer runs out
            else if (slideTimer <= 0f)
            {
                // Only stand back up if there is space
                if (CanStandUp())
                {
                    // End the slide
                    isSliding = false;
                    isHoldingSlidePose = false;
                    animator.speed = 1f;

                    // Restore the player's normal collider
                    boxCollider.size = originalColliderSize;
                    boxCollider.offset = originalColliderOffset;

                    // Enable one burst of momentum carry after the slide finishes
                    applyPostSlideMomentum = true;
                }
                else
                {
                    // Keep the player low and freeze on a late slide pose until there is room to stand.
                    HoldSlidePose();
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
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, GetModifiedJumpForce());
            PlayOneShot(GameAudio.JumpClip);
            coyoteCounter = 0f;

            // Player is now airborne, so clear the buffered jump
            isGrounded = false;
            jumpBufferCounter = 0f;
        }

        animator.SetBool("IsSliding", isSliding);
        UpdateLoopingAudio();
        wasGroundedLastFrame = isGrounded;
    }

    void FixedUpdate()
    {
        // Prevent normal movement forces while the player is grabbing a ledge
        if (isGrabbingLedge || isClimbingLedge)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        ApplyPlatformVerticalVelocity();

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

        // Blend the platform's horizontal movement into the player's own target speed
        // so standing still on a platform does not cause the controller to fight it.
        targetSpeed += activePlatformVelocity.x;

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

    bool CanStandUp()
    {
        Vector2 standingSize = GetWorldColliderSize(originalColliderSize);
        Vector2 slideSize = GetWorldColliderSize(activeSlideColliderSize);
        Vector2 standingOffset = GetWorldColliderOffset(originalColliderOffset);
        Vector2 standingCenter = (Vector2)transform.position + standingOffset;
        float headroomHeight = standingSize.y - slideSize.y;

        if (headroomHeight <= 0.001f)
        {
            return true;
        }

        // Only check the extra space above the current slide collider. This avoids
        // counting the floor as an obstruction while still preventing the player
        // from standing up into a ceiling.
        float standingTop = standingCenter.y + (standingSize.y * 0.5f);
        float slideTop = standingCenter.y - (standingSize.y * 0.5f) + slideSize.y;
        float headroomCenterY = (standingTop + slideTop) * 0.5f;
        Vector2 headroomSize = new Vector2(standingSize.x * 0.95f, Mathf.Max(headroomHeight - 0.01f, 0.01f));
        Vector2 headroomCenter = new Vector2(standingCenter.x, headroomCenterY);

        Collider2D hit = Physics2D.OverlapBox(
            headroomCenter,
            headroomSize,
            0f,
            GetSolidCollisionMask()
        );

        return hit == null;
    }

    void HoldSlidePose()
    {
        if (isHoldingSlidePose)
        {
            return;
        }

        isHoldingSlidePose = true;
        animator.speed = 1f;
        animator.Play("Slide", 0, Mathf.Clamp01(slideHoldPoseTime));
        animator.Update(0f);
        animator.speed = 0f;
    }

    bool HasReachedSlideHoldPose()
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        if (!stateInfo.IsName("Slide"))
        {
            return false;
        }

        float normalizedTime = stateInfo.normalizedTime % 1f;
        return normalizedTime >= Mathf.Clamp01(slideHoldPoseTime);
    }

    Vector2 GetWorldColliderSize(Vector2 localSize)
    {
        Vector3 lossyScale = transform.lossyScale;

        return new Vector2(
            Mathf.Abs(localSize.x * lossyScale.x),
            Mathf.Abs(localSize.y * lossyScale.y)
        );
    }

    Vector2 GetWorldColliderOffset(Vector2 localOffset)
    {
        Vector3 lossyScale = transform.lossyScale;

        return new Vector2(
            localOffset.x * lossyScale.x,
            localOffset.y * lossyScale.y
        );
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
            ledgeGrabbableLayer
        );

        // Cast a second ray forward at head height to check for empty space
        RaycastHit2D topHit = Physics2D.Raycast(
            activeLedgeCheckTop.position,
            rayDirection,
            ledgeCheckDistance,
            ledgeGrabbableLayer
        );

        // A ledge is detected if there is a wall at the lower point but no wall above it
        if (wallHit && !topHit)
        {
            float ledgeTopY = wallHit.collider.bounds.max.y;
            float maxReachableTopY = activeLedgeCheckTop.position.y + maxLedgeGrabHeight;

            if (ledgeTopY <= maxReachableTopY)
            {
                GrabLedge(wallHit);
                return;
            }
        }
    }

    void CacheLedgeGrabbableLayer()
    {
        if (ledgeGrabbableLayer.value != 0)
        {
            return;
        }

        int namedLayerMask = LayerMask.GetMask("LedgeGrabbable");
        ledgeGrabbableLayer = namedLayerMask != 0 ? namedLayerMask : groundLayer;
    }

    LayerMask GetSolidCollisionMask()
    {
        return groundLayer | ledgeGrabbableLayer;
    }

    MovingPlatform2D GetActiveMovingPlatformComponent()
    {
        if (!isGrounded || groundedCollider == null)
        {
            return null;
        }

        return groundedCollider.GetComponentInParent<MovingPlatform2D>();
    }

    Rigidbody2D GetActivePlatformBody()
    {
        if (!isGrounded || groundedCollider == null)
        {
            return null;
        }

        // If carry is off on this platform, do not inherit its movement.
        if (!IsPlatformCarryEnabled())
        {
            return null;
        }

        Rigidbody2D platformBody = groundedCollider.attachedRigidbody;

        if (platformBody == null || platformBody == rb)
        {
            return null;
        }

        if (platformBody.bodyType != RigidbodyType2D.Kinematic)
        {
            return null;
        }

        return platformBody;
    }

    bool IsPlatformCarryEnabled()
    {
        if (activePlatformScript == null)
        {
            return false;
        }

        // Read the carry setting from the moving platform script.
        PropertyInfo carryProperty = typeof(MovingPlatform2D).GetProperty("CarryPlayer");

        if (carryProperty != null)
        {
            object propertyValue = carryProperty.GetValue(activePlatformScript);

            if (propertyValue is bool boolValue)
            {
                return boolValue;
            }
        }

        FieldInfo carryField = typeof(MovingPlatform2D).GetField("carryPlayer", BindingFlags.NonPublic | BindingFlags.Instance);

        if (carryField != null)
        {
            object fieldValue = carryField.GetValue(activePlatformScript);

            if (fieldValue is bool boolValue)
            {
                return boolValue;
            }
        }

        return true;
    }

    Vector2 GetActivePlatformVelocity()
    {
        if (activePlatformBody == null)
        {
            return Vector2.zero;
        }

        // Use the platform rigidbody velocity so the player can move with it.
        return activePlatformBody.linearVelocity;
    }

    void ApplyPlatformVerticalVelocity()
    {
        if (activePlatformBody == null)
        {
            return;
        }

        if (Mathf.Abs(activePlatformVelocity.y) <= 0.0001f)
        {
            return;
        }

        // Match the platform's up and down speed while the player is standing on it.
        // This is more stable than moving the player with MovePosition each frame.
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, activePlatformVelocity.y);
    }

    void BeginSlide(float direction)
    {
        isSliding = true;
        isHoldingSlidePose = false;
        slideTimer = slideDuration;
        slideDirection = direction == 0f ? facingDirection : Mathf.Sign(direction);

        animator.speed = 1f;
        animator.Play("Slide", 0, 0f);
        boxCollider.size = activeSlideColliderSize;
        boxCollider.offset = activeSlideColliderOffset;
        UpdateLoopingAudio();
    }

    public void SetSprintEnabled(bool enabled)
    {
        sprintEnabled = enabled;

        if (!enabled)
        {
            sprintToggleActive = false;
        }
    }

    public void SetSprintHeightGate(bool enabled, float unlockYHeight)
    {
        useSprintHeightGate = enabled;
        sprintUnlockYHeight = unlockYHeight;
    }

    float GetModifiedJumpForce()
    {
        if (InventoryManager.Instance == null)
        {
            return jumpForce;
        }

        return jumpForce * InventoryManager.Instance.GetJumpBoostMultiplier();
    }

    void InitializeAudioSources()
    {
        oneShotAudioSource = gameObject.AddComponent<AudioSource>();
        oneShotAudioSource.playOnAwake = false;

        footstepLoopSource = gameObject.AddComponent<AudioSource>();
        footstepLoopSource.playOnAwake = false;
        footstepLoopSource.loop = true;
        footstepLoopSource.clip = GameAudio.FootstepClip;
        footstepLoopSource.volume = 0.75f;

        slideLoopSource = gameObject.AddComponent<AudioSource>();
        slideLoopSource.playOnAwake = false;
        slideLoopSource.loop = true;
        slideLoopSource.clip = GameAudio.SlideClip;
        slideLoopSource.volume = 0.9f;
    }

    void UpdateLoopingAudio()
    {
        bool shouldPlayFootsteps = isGrounded &&
                                   !isSliding &&
                                   !isGrabbingLedge &&
                                   !isClimbingLedge &&
                                   Mathf.Abs(horizontalInput) > 0.1f &&
                                   GameAudio.FootstepClip != null;

        if (shouldPlayFootsteps)
        {
            if (!footstepLoopSource.isPlaying)
            {
                footstepLoopSource.clip = GameAudio.FootstepClip;
                footstepLoopSource.Play();
            }
        }
        else if (footstepLoopSource.isPlaying)
        {
            footstepLoopSource.Stop();
        }

        bool shouldPlaySlide = isSliding && GameAudio.SlideClip != null;

        if (shouldPlaySlide)
        {
            if (!slideLoopSource.isPlaying)
            {
                slideLoopSource.clip = GameAudio.SlideClip;
                slideLoopSource.Play();
            }
        }
        else if (slideLoopSource.isPlaying)
        {
            slideLoopSource.Stop();
        }
    }

    void PlayOneShot(AudioClip clip)
    {
        if (oneShotAudioSource == null || clip == null)
        {
            return;
        }

        oneShotAudioSource.PlayOneShot(clip);
    }

    bool CanSprint()
    {
        if (!sprintEnabled)
        {
            return false;
        }

        if (!useSprintHeightGate)
        {
            return true;
        }

        return transform.position.y >= sprintUnlockYHeight;
    }

    // Freezes the player in place when a ledge is grabbed
    void GrabLedge(RaycastHit2D wallHit)
    {
        // Mark the player as grabbing a ledge
        isGrabbingLedge = true;

        // Reset the ledge hang timer
        ledgeHangTimer = ledgeHangDuration;
        
        // Cancel sliding if a ledge is grabbed
        isSliding = false;
        isHoldingSlidePose = false;
        animator.speed = 1f;

        // Restore the normal collider in case the player was sliding
        boxCollider.size = originalColliderSize;
        boxCollider.offset = originalColliderOffset;

        // Snap the player to a consistent hang position based on the detected ledge face.
        Bounds wallBounds = wallHit.collider.bounds;
        float hangX = facingDirection > 0f
            ? wallBounds.min.x - ledgeHangOffset.x
            : wallBounds.max.x + ledgeHangOffset.x;
        float hangY = wallBounds.max.y + ledgeHangOffset.y;
        transform.position = new Vector3(hangX, hangY, transform.position.z);

        // Stop all movement so the player hangs still
        rb.linearVelocity = Vector2.zero;

        // Temporarily disable gravity so the player does not fall
        rb.gravityScale = 0f;

        // Force the ledge grab animation immediately so the frozen slide frame does not linger.
        animator.SetBool("IsSliding", false);
        animator.SetBool("IsLedgeGrabbing", true);
        animator.Play("LedgeGrab", 0, 0f);
    }

    // Moves the player up and onto the platform after climbing
    void ClimbLedge()
    {
        isGrabbingLedge = false;
        isClimbingLedge = true;
        queuedClimbSlide = false;

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
        animator.speed = 1f;
        rb.gravityScale = gravityBeforeLedgeGrab;
        ledgeCooldownTimer = ledgeRegrabCooldown;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, -2f);
    }
    
}


