using System;
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
    [SerializeField] private float damageMarginViewport = 0.01f;

    public int CurrentPhase { get; private set; }

    public event Action<int> OnPhaseChanged;
    private Material materialInstance;
    private Vector2 centerWorld;
    private float radiusWorld;
    private Coroutine stormRoutine;
    private bool loadedFromSave;

    private static readonly int CenterId = Shader.PropertyToID("_Center");
    private static readonly int RadiusId = Shader.PropertyToID("_Radius");
    private static readonly int FeatherId = Shader.PropertyToID("_Feather");

    private void Awake()
    {
        if (overlayImage == null)
            overlayImage = GetComponent<Image>();

        if (worldCam == null)
            worldCam = Camera.main;

        materialInstance = overlayImage != null ? overlayImage.material : null;

        if (materialInstance != null)
            materialInstance.SetFloat(FeatherId, featherUV);

        CurrentPhase = 0;
        radiusWorld = GetInitialRadius();
    }

   private void Start()
    {
        if (!loadedFromSave)
        {
            PickInitialCenter();
            ApplyToShader();
            stormRoutine = StartCoroutine(StormPhases());
        }
    }

    private void Update()
    {
        ApplyToShader();
    }

    private IEnumerator StormPhases()
{
    if (phaseEndRadius == null || phaseEndRadius.Length == 0)
        yield break;

    CurrentPhase = 0;
    ApplyToShader();

    for (int i = 0; i < phaseEndRadius.Length; i++)
    {
        float wait = phaseWaitTime != null && i < phaseWaitTime.Length
            ? phaseWaitTime[i]
            : 0f;

        if (wait > 0f)
            yield return new WaitForSeconds(wait);

        float startRadius = radiusWorld;
        float targetRadius = Mathf.Min(phaseEndRadius[i], startRadius);

        Vector2 startCenter = centerWorld;
        Vector2 targetCenter = PickNextCenter(startCenter, startRadius, targetRadius);

        float duration = phaseShrinkTime != null && i < phaseShrinkTime.Length
            ? phaseShrinkTime[i]
            : 1f;

        if (duration <= 0f)
            duration = 0.01f;

        CurrentPhase = i + 1;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            radiusWorld = Mathf.Lerp(startRadius, targetRadius, t);
            centerWorld = Vector2.Lerp(startCenter, targetCenter, t);

            yield return null;
        }

        radiusWorld = targetRadius;
        centerWorld = targetCenter;

        OnPhaseChanged?.Invoke(CurrentPhase);
    }
}

    private float GetInitialRadius()
    {
        if (playBounds == null)
            return phaseEndRadius != null && phaseEndRadius.Length > 0 ? phaseEndRadius[0] : 25f;

        Bounds bounds = playBounds.bounds;
        float maxRadiusX = bounds.extents.x - initialRadiusPadding;
        float maxRadiusY = bounds.extents.y - initialRadiusPadding;
        float maxRadius = Mathf.Max(0.1f, Mathf.Min(maxRadiusX, maxRadiusY));

        if (phaseEndRadius != null && phaseEndRadius.Length > 0)
            return Mathf.Max(maxRadius, phaseEndRadius[0]);

        return maxRadius;
    }

    private Vector2 PickNextCenter(Vector2 currentCenter, float startRadius, float targetRadius)
    {
        if (playBounds == null)
            return currentCenter;

        float maxOffset = Mathf.Max(0f, startRadius - targetRadius);
        Bounds bounds = playBounds.bounds;

        float minX = bounds.min.x + targetRadius;
        float maxX = bounds.max.x - targetRadius;
        float minY = bounds.min.y + targetRadius;
        float maxY = bounds.max.y - targetRadius;

        if (minX > maxX || minY > maxY)
            return currentCenter;

        for (int i = 0; i < 50; i++)
        {
            Vector2 candidate = currentCenter + UnityEngine.Random.insideUnitCircle * maxOffset;
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

        Bounds bounds = playBounds.bounds;

        float minX = bounds.min.x + radiusWorld;
        float maxX = bounds.max.x - radiusWorld;
        float minY = bounds.min.y + radiusWorld;
        float maxY = bounds.max.y - radiusWorld;

        if (minX > maxX || minY > maxY)
        {
            centerWorld = bounds.center;
            return;
        }

        centerWorld = new Vector2(
            UnityEngine.Random.Range(minX, maxX),
           UnityEngine.Random.Range(minY, maxY)
        );
    }

    private void ApplyToShader()
    {
        if (materialInstance == null || worldCam == null)
            return;

        Vector3 viewportCenter = worldCam.WorldToViewportPoint(centerWorld);
        materialInstance.SetVector(CenterId, new Vector4(viewportCenter.x, viewportCenter.y, 0f, 0f));

        float fullHeightWorld = worldCam.orthographicSize * 2f;
        float radiusUV = fullHeightWorld > 0f ? radiusWorld / fullHeightWorld : 0.1f;

        materialInstance.SetFloat(RadiusId, radiusUV);
    }

    public bool IsInside(Vector3 worldPosition)
    {
        if (worldCam == null)
        {
            Debug.LogError($"{nameof(StormOverlayController)}: worldCam is not assigned in IsInside().");
            return false;
        }

        Vector3 objectViewport = worldCam.WorldToViewportPoint(worldPosition);
        Vector3 centerViewport = worldCam.WorldToViewportPoint(centerWorld);

        float distanceViewport = Vector2.Distance(objectViewport, centerViewport);

        float fullHeightWorld = worldCam.orthographicSize * 2f;
        float radiusViewport = fullHeightWorld > 0f ? radiusWorld / fullHeightWorld : 0.1f;
        float radiusWithMargin = radiusViewport - damageMarginViewport;

        if (distanceViewport <= radiusWithMargin)
            return false;

        if (playBounds != null)
            return playBounds.bounds.Contains(worldPosition);

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

    public bool IsInsideSafeZone(Vector3 worldPosition)
    {
        return Vector2.Distance(worldPosition, centerWorld) <= radiusWorld;
    }
    public void LoadStormState(int phase, Vector2 center, float radius)
    {
        if (stormRoutine != null)
        {
            StopCoroutine(stormRoutine);
            stormRoutine = null;
        }

        loadedFromSave = true;

        CurrentPhase = Mathf.Max(0, phase);
        centerWorld = center;
        radiusWorld = Mathf.Max(0.1f, radius);

        ApplyToShader();

        stormRoutine = StartCoroutine(StormPhases());
    }
}