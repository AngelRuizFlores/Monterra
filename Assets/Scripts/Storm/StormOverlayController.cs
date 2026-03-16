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
    [SerializeField] private float damageMarginViewport = 0.01f; // Pequeña zona de seguridad para rozar sin daño

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
        
        // Debug mejorado cada N frames
        if (Time.frameCount % 60 == 0)
        {
            float fullHeightWorld = (worldCam != null) ? worldCam.orthographicSize * 2f : 0f;
            float radiusNormalized = (fullHeightWorld > 0f) ? (radiusWorld / fullHeightWorld) : 0f;
            Debug.Log($"[Storm State] Centro: {centerWorld:F2} | Radio: {radiusWorld:F2}u ({radiusNormalized:F3}norm) | Fase: {CurrentPhase} | CamHeight: {fullHeightWorld:F2}u");
        }
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
        if (worldCam == null)
        {
            Debug.LogError("[StormOverlayController] worldCam no está asignada en IsInside()");
            return false;
        }

        // ✅ CRUCIAL: Usar el mismo espacio coordinado que el shader (VIEWPORT SPACE)
        
        // 1. Convertir posiciones a VIEWPORT SPACE (0-1), exactamente como ApplyToShader()
        Vector3 playerViewport = worldCam.WorldToViewportPoint(worldPos);
        Vector3 centerViewport = worldCam.WorldToViewportPoint(centerWorld);
        
        // 2. Calcular distancia EN VIEWPORT SPACE (como hace el shader)
        float distanceViewport = Vector2.Distance(playerViewport, centerViewport);
        
        // 3. Obtener radio en VIEWPORT SPACE (exactamente como ApplyToShader())
        float fullHeightWorld = worldCam.orthographicSize * 2f;
        float radiusViewport = (fullHeightWorld > 0f) ? (radiusWorld / fullHeightWorld) : 0.1f;
        
        // 4. Restar margen de seguridad: permite rozar sin recibir daño
        float radiusWithMargin = radiusViewport - damageMarginViewport;

        // Si está DENTRO del círculo seguro → NO está en tormenta
        if (distanceViewport <= radiusWithMargin)
        {
            Debug.Log($"[Storm] ✓ DENTRO ZONA SEGURA" +
                $" | WorldPos: {worldPos:F2} | ViewportPos: {playerViewport:F2}" +
                $" | Dist: {distanceViewport:F3}vp | RadioDaño: {radiusWithMargin:F3}vp (visual: {radiusViewport:F3}vp) | IsInStorm: FALSE");
            return false;
        }

        // Si está FUERA del círculo seguro pero dentro del mapa → SÍ está en tormenta
        if (playBounds != null)
        {
            Bounds b = playBounds.bounds;
            bool inBounds = b.Contains(worldPos);

            Debug.Log($"[Storm] ✗ FUERA ZONA SEGURA" +
                $" | WorldPos: {worldPos:F2} | ViewportPos: {playerViewport:F2}" +
                $" | Dist: {distanceViewport:F3}vp | RadioDaño: {radiusWithMargin:F3}vp (visual: {radiusViewport:F3}vp) | EnMapa: {inBounds} | IsInStorm: {inBounds}");

            return inBounds;
        }

        // Si no hay límites definidos, cualquier punto fuera del círculo es tormenta
        Debug.Log($"[Storm] ! SIN BOUNDS" +
            $" | WorldPos: {worldPos:F2} | ViewportPos: {playerViewport:F2}" +
            $" | Dist: {distanceViewport:F3}vp | RadioDaño: {radiusWithMargin:F3}vp (visual: {radiusViewport:F3}vp) | IsInStorm: TRUE");
        return true;
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