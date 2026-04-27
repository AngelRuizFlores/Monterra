using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class RandomMovementBehavior : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float changeDirectionInterval = 3f;
    [SerializeField] private float randomDirectionRange = 1f;

    [Header("Bounds")]
    [SerializeField] private Collider2D homeZone;
    [SerializeField] private float insideEpsilon = 0.02f;

    [Header("Player Proximity")]
    [SerializeField] private Transform player;
    [SerializeField] private float stopRadius = 1.2f;
    [SerializeField] private float resumeRadius = 1.6f;

    [Header("Separation")]
    [SerializeField] private LayerMask monMask;
    [SerializeField] private float separationRadius = 0.45f;
    [SerializeField] private float separationPadding = 0.05f;
    [SerializeField] private float separationForce = 1.2f;

    [Header("Directional Sprites")]
    [SerializeField] private bool useDirectionalSprites = false;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite upSprite;
    [SerializeField] private Sprite downSprite;
    [SerializeField] private Sprite leftSprite;
    [SerializeField] private Sprite rightSprite;

    private Rigidbody2D rb;
    private Collider2D myCollider;

    private Vector2 direction;
    private float timer;
    private bool stopped;

    private readonly Collider2D[] hits = new Collider2D[16];
    private ContactFilter2D monFilter;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        myCollider = GetComponent<Collider2D>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        monFilter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = monMask,
            useTriggers = false
        };
    }

    private void OnEnable()
    {
        ResetState();
    }

    private void Update()
    {
        if (IsPlayerClose())
            return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            PickRandomDirection();
            timer = changeDirectionInterval;
        }
    }

    private void FixedUpdate()
    {
        if (IsPlayerClose())
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            return;
        }

        Vector2 currentPosition = rb.position;

        Vector2 moveDirection = direction.sqrMagnitude > 0.0001f
            ? direction.normalized
            : Vector2.right;

        Vector2 nextPosition = currentPosition + moveDirection * moveSpeed * Time.fixedDeltaTime;

        if (homeZone != null && !homeZone.OverlapPoint(nextPosition))
        {
            nextPosition = PushInsideHome(nextPosition);
            PickRandomDirection();
            moveDirection = direction.normalized;
        }

        nextPosition += GetSeparationOffset(nextPosition);

        if (homeZone != null && !homeZone.OverlapPoint(nextPosition))
            nextPosition = PushInsideHome(nextPosition);

        rb.MovePosition(nextPosition);
        UpdateFacing(moveDirection);
    }

    public void ResetState()
    {
        stopped = false;
        timer = changeDirectionInterval;
        PickRandomDirection();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    public void SetHomeZone(Collider2D zone)
    {
        homeZone = zone;
    }

    public void SetPlayer(Transform targetPlayer)
    {
        player = targetPlayer;
    }

    private bool IsPlayerClose()
    {
        if (player == null || rb == null)
            return false;

        float distance = Vector2.Distance(rb.position, player.position);

        if (!stopped)
        {
            if (distance <= stopRadius)
                stopped = true;
        }
        else
        {
            if (distance >= resumeRadius)
                stopped = false;
        }

        return stopped;
    }

    private void PickRandomDirection()
    {
        direction = Random.insideUnitCircle * randomDirectionRange;

        if (direction.sqrMagnitude < 0.001f)
            direction = Vector2.right;
    }

    private void UpdateFacing(Vector2 moveDirection)
    {
        if (useDirectionalSprites)
        {
            UpdateDirectionalSprite(moveDirection);
            transform.rotation = Quaternion.identity;
            return;
        }

        FaceByX(moveDirection.x);
    }

    private void UpdateDirectionalSprite(Vector2 moveDirection)
    {
        if (spriteRenderer == null)
            return;

        if (moveDirection.sqrMagnitude < 0.0001f)
            return;

        Sprite selectedSprite;

        if (Mathf.Abs(moveDirection.x) > Mathf.Abs(moveDirection.y))
        {
            selectedSprite = moveDirection.x < 0f ? leftSprite : rightSprite;
        }
        else
        {
            selectedSprite = moveDirection.y < 0f ? downSprite : upSprite;
        }

        if (selectedSprite != null)
            spriteRenderer.sprite = selectedSprite;
    }

    private void FaceByX(float x)
    {
        transform.rotation = x < 0f
            ? Quaternion.Euler(0f, 180f, 0f)
            : Quaternion.Euler(0f, 0f, 0f);
    }

    private Vector2 PushInsideHome(Vector2 candidatePosition)
    {
        Vector2 closestPoint = homeZone.ClosestPoint(candidatePosition);
        Vector2 toCenter = (Vector2)homeZone.bounds.center - closestPoint;

        if (toCenter.sqrMagnitude > 0.0001f)
            closestPoint += toCenter.normalized * insideEpsilon;

        return closestPoint;
    }

    private Vector2 GetSeparationOffset(Vector2 position)
    {
        int count = Physics2D.OverlapCircle(
            position,
            separationRadius + separationPadding,
            monFilter,
            hits
        );

        if (count == 0)
            return Vector2.zero;

        Vector2 push = Vector2.zero;

        for (int i = 0; i < count; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null || hit == myCollider)
                continue;

            Vector2 closestPoint = hit.ClosestPoint(position);
            Vector2 offsetDirection = position - closestPoint;

            float distance = offsetDirection.magnitude;
            if (distance < 0.0001f || distance >= separationRadius)
                continue;

            float strength = (separationRadius - distance) / separationRadius;
            push += offsetDirection.normalized * strength;
        }

        if (push.sqrMagnitude < 0.0001f)
            return Vector2.zero;

        Vector2 offset = push * separationForce * Time.fixedDeltaTime;
        float maxStep = separationForce * Time.fixedDeltaTime;

        return Vector2.ClampMagnitude(offset, maxStep);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, separationRadius);
    }
#endif
}