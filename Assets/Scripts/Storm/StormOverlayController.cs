using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class StormOverlayController : MonoBehaviour
{
    [SerializeField] private Image overlayImage;
    [SerializeField] private Camera worldCam;
    [SerializeField] private BoxCollider2D playBounds;

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
        radiusWorld = (phaseEndRadius != null && phaseEndRadius.Length > 0) ? phaseEndRadius[0] : 25f;
    }

    void Start()
    {
        PickRandomCenter();
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

        for (int i = 1; i < phaseEndRadius.Length; i++)
        {
            // 1) Pausa: NO cambia nada
            float wait = (phaseWaitTime != null && i < phaseWaitTime.Length) ? phaseWaitTime[i] : 0f;
            if (wait > 0f) yield return new WaitForSeconds(wait);

            // 2) Prepara siguiente fase
            float startRadius = radiusWorld;
            float targetRadius = phaseEndRadius[i];

            Vector2 startCenter = centerWorld;
            Vector2 targetCenter = PickNextCenterFortnite(startCenter, startRadius, targetRadius);

            float duration = (phaseShrinkTime != null && i < phaseShrinkTime.Length) ? phaseShrinkTime[i] : 1f;
            if (duration <= 0f) duration = 0.01f;

            CurrentPhase = i;

            // 3) Shrink + desplazamiento SUAVE del centro (sensación de continuidad)
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

    private Vector2 PickNextCenterFortnite(Vector2 currentCenter, float startRadius, float targetRadius)
    {
        if (playBounds == null) return currentCenter;

        // para que el círculo nuevo quepa dentro del actual
        float maxOffset = Mathf.Max(0f, startRadius - targetRadius);

        Bounds b = playBounds.bounds;

        for (int tries = 0; tries < 30; tries++)
        {
            Vector2 candidate = currentCenter + (Random.insideUnitCircle * maxOffset);

            // opcional: evitar que el centro se vaya fuera del área jugable
            candidate.x = Mathf.Clamp(candidate.x, b.min.x, b.max.x);
            candidate.y = Mathf.Clamp(candidate.y, b.min.y, b.max.y);

            // sigue estando dentro del offset permitido
            if (Vector2.Distance(candidate, currentCenter) <= maxOffset + 0.001f)
                return candidate;
        }

        return currentCenter;
    }

    private void PickRandomCenter()
    {
        if (playBounds == null)
        {
            centerWorld = Vector2.zero;
            return;
        }

        Bounds b = playBounds.bounds;

        centerWorld = new Vector2(
            Random.Range(b.min.x, b.max.x),
            Random.Range(b.min.y, b.max.y)
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