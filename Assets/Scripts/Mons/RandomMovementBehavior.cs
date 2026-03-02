using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class RandomMovementBehavior : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float changeDirectionInterval = 3f;
    [SerializeField] private float randomDirectionRange = 1f;

    [Header("Limits (optional)")]
    [SerializeField] private Collider2D homeZone;
    [SerializeField] private float insideEpsilon = 0.02f;

    [Header("Player stop (optional)")]
    [SerializeField] private Transform player;
    [SerializeField] private float stopRadius = 1.2f;
    [SerializeField] private float resumeRadius = 1.6f;

    [Header("Separation (Mon layer)")]
    [SerializeField] private LayerMask monMask;
    [SerializeField] private float separationRadius = 0.45f;
    [SerializeField] private float separationPadding = 0.05f;
    [SerializeField] private float separationForce = 1.2f;

    private Rigidbody2D rb;
    private Collider2D myCol;

    private Vector2 direction;
    private float timer;
    private bool stopped;

    private readonly Collider2D[] hits = new Collider2D[16];
    private ContactFilter2D monFilter;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        myCol = GetComponent<Collider2D>();

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // Solo “cuerpos” de Mon (no triggers)
        monFilter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = monMask,
            useTriggers = false
        };
    }

    void OnEnable()
    {
        ResetState();
    }

    void Update()
    {
        // Si está parado por player, no hace falta cambiar dirección
        if (PlayerIsClose()) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            PickRandomDirection();
            timer = changeDirectionInterval;
        }
    }

    void FixedUpdate()
    {
        if (PlayerIsClose())
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            return;
        }

        Vector2 pos = rb.position;

        // Dirección base
        Vector2 moveDir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        Vector2 nextPos = pos + moveDir * moveSpeed * Time.fixedDeltaTime;

        // Mantener dentro del home (si existe)
        if (homeZone && !homeZone.OverlapPoint(nextPos))
        {
            nextPos = PushInsideHome(nextPos);
            PickRandomDirection(); // para no quedarse pegado
            moveDir = direction.normalized;
        }

        // Separación suave (sin empuje físico)
        nextPos += SeparationOffset(nextPos);

        // Re-chequeo por si la separación te sacó fuera
        if (homeZone && !homeZone.OverlapPoint(nextPos))
            nextPos = PushInsideHome(nextPos);

        rb.MovePosition(nextPos);

        // Mira por X (si quieres mantenerlo)
        FaceByX(moveDir.x);
    }

    public void ResetState()
    {
        stopped = false;
        timer = changeDirectionInterval;
        PickRandomDirection();

        if (rb)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    private bool PlayerIsClose()
    {
        if (!player || !rb) return false;

        float d = Vector2.Distance(rb.position, player.position);

        if (!stopped)
        {
            if (d <= stopRadius) stopped = true;
        }
        else
        {
            if (d >= resumeRadius) stopped = false;
        }

        return stopped;
    }

    private void PickRandomDirection()
    {
        direction = Random.insideUnitCircle * randomDirectionRange;
        if (direction.sqrMagnitude < 0.001f)
            direction = Vector2.right;
    }

    private void FaceByX(float x)
    {
        transform.rotation = (x < 0) ? Quaternion.Euler(0, 180, 0) : Quaternion.Euler(0, 0, 0);
    }

    private Vector2 PushInsideHome(Vector2 candidate)
    {
        // Empuja hacia dentro del collider de zona
        Vector2 closest = homeZone.ClosestPoint(candidate);
        Vector2 toCenter = (Vector2)homeZone.bounds.center - closest;

        if (toCenter.sqrMagnitude > 0.0001f)
            closest += toCenter.normalized * insideEpsilon;

        return closest;
    }

    private Vector2 SeparationOffset(Vector2 pos)
    {
        int count = Physics2D.OverlapCircle(pos, separationRadius + separationPadding, monFilter, hits);
        if (count == 0) return Vector2.zero;

        Vector2 push = Vector2.zero;

        for (int i = 0; i < count; i++)
        {
            var h = hits[i];
            if (!h) continue;
            if (h == myCol) continue;

            // punto real más cercano (mucho mejor que bounds.center)
            Vector2 closest = h.ClosestPoint(pos);
            Vector2 dir = pos - closest;

            float dist = dir.magnitude;
            if (dist < 0.0001f) continue;

            float minDist = separationRadius;
            if (dist >= minDist) continue;

            float t = (minDist - dist) / minDist; // 0..1
            push += dir.normalized * t;
        }

        if (push.sqrMagnitude < 0.0001f) return Vector2.zero;

        // empuje suave proporcional
        Vector2 offset = push * separationForce * Time.fixedDeltaTime;

        // clamp para evitar saltos raros cuando hay muchos juntos
        float maxStep = separationForce * Time.fixedDeltaTime;
        return Vector2.ClampMagnitude(offset, maxStep);
    }

    public void SetHomeZone(Collider2D zone) => homeZone = zone;
    public void SetPlayer(Transform p) => player = p;

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, separationRadius);
    }
#endif
}
