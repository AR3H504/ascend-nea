using UnityEngine;

// Moves a 2D platform back and forth.
// The platform can move left and right or up and down.
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class MovingPlatform2D : MonoBehaviour
{
    public enum MovementAxis
    {
        Horizontal,
        Vertical
    }

    [Header("Movement")]
    [SerializeField] private MovementAxis movementAxis = MovementAxis.Horizontal; // Chooses horizontal or vertical movement
    [SerializeField] private float moveDistance = 3f; // How far the platform moves from its start position
    [SerializeField] private float moveSpeed = 2f; // How fast the platform moves
    [SerializeField] private float waitTimeAtEnds = 0.2f; // Time to wait before moving back
    [SerializeField] private bool startMovingTowardPositiveDirection = true; // Starts by moving right or up when turned on

    [Header("Player Carry")]
    [SerializeField] private bool carryPlayer = true; // Lets the player move with the platform while standing on it

    private Rigidbody2D rb;
    private Vector2 startPosition;
    private Vector2 targetPosition;
    private Vector2 currentDestination;
    private Vector2 previousPosition;
    private float waitTimer; // Counts down the pause time at each end
    private Vector2 currentFrameDelta; // Stores how far the platform moved this physics step

    public bool CarryPlayer
    {
        get { return carryPlayer; }
    }

    public bool IsCarryPlayerEnabled()
    {
        return carryPlayer;
    }

    public Vector2 GetCurrentFrameDelta()
    {
        return currentFrameDelta;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        SetupRigidbody();
    }

    private void Start()
    {
        // Save the start point and build the second point from the move distance.
        startPosition = rb.position;
        targetPosition = startPosition + GetMovementOffset();
        currentDestination = startMovingTowardPositiveDirection ? targetPosition : startPosition;
        previousPosition = rb.position;
    }

    private void FixedUpdate()
    {
        Vector2 positionBeforeMove = rb.position;
        currentFrameDelta = Vector2.zero;

        // Stop for a moment when the platform reaches an end point.
        if (waitTimer > 0f)
        {
            waitTimer -= Time.fixedDeltaTime;
            rb.linearVelocity = Vector2.zero;
            previousPosition = rb.position;
            return;
        }

        Vector2 nextPosition = Vector2.MoveTowards(rb.position, currentDestination, moveSpeed * Time.fixedDeltaTime);
        rb.MovePosition(nextPosition);
        previousPosition = positionBeforeMove;
        currentFrameDelta = nextPosition - positionBeforeMove;

        // Update the rigidbody velocity so other scripts can read platform movement.
        rb.linearVelocity = currentFrameDelta / Time.fixedDeltaTime;

        // Switch to the other point once this one is reached.
        if (Vector2.Distance(rb.position, currentDestination) <= 0.01f)
        {
            currentDestination = currentDestination == targetPosition ? startPosition : targetPosition;
            waitTimer = waitTimeAtEnds;
        }
    }

    private Vector2 GetMovementOffset()
    {
        if (movementAxis == MovementAxis.Horizontal)
        {
            return Vector2.right * moveDistance;
        }

        return Vector2.up * moveDistance;
    }

    private void SetupRigidbody()
    {
        // Use a kinematic rigidbody so the platform can be moved by script.
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private void OnValidate()
    {
        moveDistance = Mathf.Max(0f, moveDistance);
        moveSpeed = Mathf.Max(0f, moveSpeed);
        waitTimeAtEnds = Mathf.Max(0f, waitTimeAtEnds);
    }

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            SetupRigidbody();
        }
    }
}
