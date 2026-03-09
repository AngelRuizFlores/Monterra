using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class StormOverlayController : MonoBehaviour
{
    [SerializeField] private Image overlayImage;
    [SerializeField] private Camera worldCam;
    [SerializeField] private BoxCollider2D playBounds;

    [SerializeField] private float initialRadiusPadding = 1f;
    [SerializeField] private float[] phaseEndRadius = { 25f, 18f, 12f, 6f };
    [SerializeField] private float[] phaseShrinkTime = { 20f, 18f, 15f, 10f };
    [SerializeField] private float[] phaseWaitTime = { 5f, 4f, 3f, 2f };

    [SerializeField] private float featherUV = 0.02f;

    public int CurrentPhase { get; private set; }

    private Material mat;
    private Vector2 centerWorld;
    private float radiusWorld;

    private static readonly int CenterId = Shader.PropertyToID("_Center");
    private static readonly int RadiusId = Shader.PropertyToID("_Radius");
    private static readonly int FeatherId = Shader.PropertyToID("_Feather");

    void Awake()
    {
        if (overlayImage == null) overlayImage = GetComponent<Image>();
        if (worldCam == null) worldCam = Camera.main;

        mat = overlayImage != null ? overlayImage.material : null;
        if (mat != null) mat.SetFloat(FeatherId, featherUV);

        CurrentPhase = 0;
        radiusWorld = GetInitialRadius();
    }

    void Start()
    {
        PickInitialCenter();
        ApplyToShader();
        StartCoroutine(StormPhases());
    }

    void Update()
    {
        ApplyToShader();
    }

    private IEnumerator StormPhases()
    {
        if (phaseEndRadius == null || phaseEndRadius.Length == 0) yield break;

        CurrentPhase = 0;
        ApplyToShader();

        for (int i = 0; i < phaseEndRadius.Length; i++)
        {
            float wait = (phaseWaitTime != null && i < phaseWaitTime.Length) ? phaseWaitTime[i] : 0f;
            if (wait > 0f) yield return new WaitForSeconds(wait);

            float startRadius = radiusWorld;
            float targetRadius = Mathf.Min(phaseEndRadius[i], startRadius);

            Vector2 startCenter = centerWorld;
            Vector2 targetCenter = PickNextCenterFortnite(startCenter, startRadius, targetRadius);

            float duration = (phaseShrinkTime != null && i < phaseShrinkTime.Length) ? phaseShrinkTime[i] : 1f;
            if (duration <= 0f) duration = 0.01f;

            CurrentPhase = i + 1;

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / duration);

                radiusWorld = Mathf.Lerp(startRadius, targetRadius, k);
                centerWorld = Vector2.Lerp(startCenter, targetCenter, k);

                yield return null;
            }

            radiusWorld = targetRadius;
            centerWorld = targetCenter;
        }
    }

    private float GetInitialRadius()
    {
        if (playBounds == null)
            return (phaseEndRadius != null && phaseEndRadius.Length > 0) ? phaseEndRadius[0] : 25f;

        Bounds b = playBounds.bounds;
        float maxRadiusX = b.extents.x - initialRadiusPadding;
        float maxRadiusY = b.extents.y - initialRadiusPadding;
        float maxRadius = Mathf.Max(0.1f, Mathf.Min(maxRadiusX, maxRadiusY));

        if (phaseEndRadius != null && phaseEndRadius.Length > 0)
            return Mathf.Max(maxRadius, phaseEndRadius[0]);

        return maxRadius;
    }

    private Vector2 PickNextCenterFortnite(Vector2 currentCenter, float startRadius, float targetRadius)
    {
        if (playBounds == null) return currentCenter;

        float maxOffset = Mathf.Max(0f, startRadius - targetRadius);
        Bounds b = playBounds.bounds;

        float minX = b.min.x + targetRadius;
        float maxX = b.max.x - targetRadius;
        float minY = b.min.y + targetRadius;
        float maxY = b.max.y - targetRadius;

        if (minX > maxX || minY > maxY)
            return currentCenter;

        for (int tries = 0; tries < 50; tries++)
        {
            Vector2 candidate = currentCenter + Random.insideUnitCircle * maxOffset;
            candidate.x = Mathf.Clamp(candidate.x, minX, maxX);
            candidate.y = Mathf.Clamp(candidate.y, minY, maxY);

            if (Vector2.Distance(candidate, currentCenter) <= maxOffset + 0.001f)
                return candidate;
        }

        return new Vector2(
            Mathf.Clamp(currentCenter.x, minX, maxX),
            Mathf.Clamp(currentCenter.y, minY, maxY)
        );
    }

    private void PickInitialCenter()
    {
        if (playBounds == null)
        {
            centerWorld = Vector2.zero;
            return;
        }

        Bounds b = playBounds.bounds;

        float minX = b.min.x + radiusWorld;
        float maxX = b.max.x - radiusWorld;
        float minY = b.min.y + radiusWorld;
        float maxY = b.max.y - radiusWorld;

        if (minX > maxX || minY > maxY)
        {
            centerWorld = b.center;
            return;
        }

        centerWorld = new Vector2(
            Random.Range(minX, maxX),
            Random.Range(minY, maxY)
        );
    }

    private void ApplyToShader()
    {
        if (mat == null || worldCam == null) return;

        Vector3 v = worldCam.WorldToViewportPoint(centerWorld);
        mat.SetVector(CenterId, new Vector4(v.x, v.y, 0f, 0f));

        float fullHeightWorld = worldCam.orthographicSize * 2f;
        float radiusUV = (fullHeightWorld > 0f) ? (radiusWorld / fullHeightWorld) : 0.1f;

        mat.SetFloat(RadiusId, radiusUV);
    }

    public bool IsInside(Vector3 worldPos)
    {
        return Vector2.Distance(worldPos, centerWorld) <= radiusWorld;
    }

    public Vector2 GetCenterWorld()
    {
        return centerWorld;
    }

    public float GetRadiusWorld()
    {
        return radiusWorld;
    }
}