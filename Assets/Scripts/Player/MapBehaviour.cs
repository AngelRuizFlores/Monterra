using UnityEngine;
using UnityEngine.UI;

public class MapOverlayController : MonoBehaviour
{
    [SerializeField] private RectTransform mapImage;
    [SerializeField] private RectTransform playerMarker;
    [SerializeField] private Image mapImageComponent;

    [SerializeField] private Transform player;
    [SerializeField] private StormOverlayController storm;
    [SerializeField] private BoxCollider2D playBounds;

    [SerializeField] private float mapLeftPadding = 0f;
    [SerializeField] private float mapRightPadding = 0f;
    [SerializeField] private float mapTopPadding = 0f;
    [SerializeField] private float mapBottomPadding = 0f;

    private Material mapMat;

    private static readonly int CenterId = Shader.PropertyToID("_Center");
    private static readonly int RadiusId = Shader.PropertyToID("_Radius");

    void Awake()
    {
        if (mapImageComponent != null && mapImageComponent.material != null)
        {
            mapMat = new Material(mapImageComponent.material);
            mapImageComponent.material = mapMat;
        }
    }

    void Update()
    {
        UpdatePlayer();
        UpdateStorm();
    }

    private void UpdatePlayer()
    {
        if (playerMarker == null || player == null || mapImage == null || playBounds == null) return;

        Vector2 normalized = WorldToNormalized(player.position);
        playerMarker.anchoredPosition = NormalizedToMapPosition(normalized);
    }

    private void UpdateStorm()
    {
        if (storm == null || mapMat == null || playBounds == null || mapImage == null) return;

        Bounds b = playBounds.bounds;
        Vector2 normalized = WorldToNormalized(storm.GetCenterWorld());
        Vector2 uv = NormalizedToOverlayUV(normalized);

        mapMat.SetVector(CenterId, new Vector4(uv.x, uv.y, 0f, 0f));

        float fullWidth = mapImage.rect.width;
        float fullHeight = mapImage.rect.height;

        float usableWidth = fullWidth - mapLeftPadding - mapRightPadding;
        float usableHeight = fullHeight - mapTopPadding - mapBottomPadding;

        float radiusWorld = storm.GetRadiusWorld();
        float radiusX = (radiusWorld / b.size.x) * (usableWidth / fullWidth);
        float radiusY = (radiusWorld / b.size.y) * (usableHeight / fullHeight);
        float radiusUV = Mathf.Max(radiusX, radiusY);

        mapMat.SetFloat(RadiusId, radiusUV);
    }

    private Vector2 WorldToNormalized(Vector2 worldPos)
    {
        Bounds b = playBounds.bounds;

        float x = Mathf.InverseLerp(b.min.x, b.max.x, worldPos.x);
        float y = Mathf.InverseLerp(b.min.y, b.max.y, worldPos.y);

        return new Vector2(x, y);
    }

    private Vector2 NormalizedToMapPosition(Vector2 normalized)
    {
        float fullWidth = mapImage.rect.width;
        float fullHeight = mapImage.rect.height;

        float usableWidth = fullWidth - mapLeftPadding - mapRightPadding;
        float usableHeight = fullHeight - mapTopPadding - mapBottomPadding;

        float x = (-fullWidth * 0.5f) + mapLeftPadding + normalized.x * usableWidth;
        float y = (-fullHeight * 0.5f) + mapBottomPadding + normalized.y * usableHeight;

        return new Vector2(x, y);
    }

    private Vector2 NormalizedToOverlayUV(Vector2 normalized)
    {
        float fullWidth = mapImage.rect.width;
        float fullHeight = mapImage.rect.height;

        float usableWidth = fullWidth - mapLeftPadding - mapRightPadding;
        float usableHeight = fullHeight - mapTopPadding - mapBottomPadding;

        float x = (mapLeftPadding + normalized.x * usableWidth) / fullWidth;
        float y = (mapBottomPadding + normalized.y * usableHeight) / fullHeight;

        return new Vector2(x, y);
    }
}