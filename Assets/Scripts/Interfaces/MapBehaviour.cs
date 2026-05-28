using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapOverlayController : MonoBehaviour
{
    [Header("Map")]
    [SerializeField] private RectTransform mapImage;
    [SerializeField] private Image mapImageComponent;

    [Header("Markers")]
    [SerializeField] private RectTransform playerMarker;
    [SerializeField] private RectTransform trainerMarkerPrefab;
    [SerializeField] private RectTransform trainerMarkersContainer;

    [Header("Bell Markers")]
    [SerializeField] private Transform[] bells;
    [SerializeField] private RectTransform bellMarkerPrefab;
    [SerializeField] private RectTransform bellMarkersContainer;

    [Header("World References")]
    [SerializeField] private Transform player;
    [SerializeField] private StormOverlayController storm;
    [SerializeField] private BoxCollider2D playBounds;

    [Header("Map Padding")]
    [SerializeField] private float mapLeftPadding = 0f;
    [SerializeField] private float mapRightPadding = 0f;
    [SerializeField] private float mapTopPadding = 0f;
    [SerializeField] private float mapBottomPadding = 0f;

    private static readonly int CenterId = Shader.PropertyToID("_Center");
    private static readonly int RadiusId = Shader.PropertyToID("_Radius");

    private readonly List<TrainerBattleTrigger> trainers = new();
    private readonly Dictionary<TrainerBattleTrigger, RectTransform> trainerMarkers = new();
    private readonly List<RectTransform> bellMarkers = new();

    private Material mapMaterial;

    private void Awake()
    {
        if (mapImageComponent != null && mapImageComponent.material != null)
        {
            mapMaterial = new Material(mapImageComponent.material);
            mapImageComponent.material = mapMaterial;
        }

        BuildTrainerMarkers();
        BuildBellMarkers();
    }

    private void Update()
    {
        UpdatePlayer();
        UpdateTrainerMarkers();
        UpdateBellMarkers();
        UpdateStorm();
    }

    private void BuildTrainerMarkers()
    {
        trainers.Clear();
        trainerMarkers.Clear();

        if (trainerMarkerPrefab == null || mapImage == null)
        {
            return;
        }

        if (trainerMarkersContainer == null)
        {
            trainerMarkersContainer = mapImage;
        }

        trainers.AddRange(FindObjectsByType<TrainerBattleTrigger>(FindObjectsSortMode.None));

        for (int i = 0; i < trainers.Count; i++)
        {
            TrainerBattleTrigger trainer = trainers[i];

            if (trainer == null)
            {
                continue;
            }

            RectTransform marker = Instantiate(trainerMarkerPrefab, trainerMarkersContainer);
            marker.gameObject.SetActive(true);

            ApplyTrainerSpriteToMarker(trainer, marker);

            trainerMarkers.Add(trainer, marker);
        }
    }

    private void BuildBellMarkers()
    {
        bellMarkers.Clear();

        if (bells == null || bellMarkerPrefab == null || mapImage == null)
        {
            return;
        }

        if (bellMarkersContainer == null)
        {
            bellMarkersContainer = mapImage;
        }

        for (int i = 0; i < bells.Length; i++)
        {
            if (bells[i] == null)
            {
                continue;
            }

            RectTransform marker = Instantiate(bellMarkerPrefab, bellMarkersContainer);
            marker.gameObject.SetActive(true);

            bellMarkers.Add(marker);
        }
    }

    private void ApplyTrainerSpriteToMarker(TrainerBattleTrigger trainer, RectTransform marker)
    {
        if (trainer == null || marker == null)
        {
            return;
        }

        SpriteRenderer trainerSpriteRenderer = trainer.GetComponent<SpriteRenderer>();

        if (trainerSpriteRenderer == null || trainerSpriteRenderer.sprite == null)
        {
            return;
        }

        Image markerImage = marker.GetComponent<Image>();

        if (markerImage == null)
        {
            return;
        }

        markerImage.sprite = trainerSpriteRenderer.sprite;
        markerImage.color = Color.white;
        markerImage.preserveAspect = true;
        markerImage.enabled = true;
    }

    private void UpdatePlayer()
    {
        if (playerMarker == null || player == null || mapImage == null || playBounds == null)
        {
            return;
        }

        Vector2 normalized = WorldToNormalized(player.position);
        playerMarker.anchoredPosition = NormalizedToMapPosition(normalized);
    }

    private void UpdateTrainerMarkers()
    {
        if (playBounds == null || mapImage == null)
        {
            return;
        }

        foreach (KeyValuePair<TrainerBattleTrigger, RectTransform> pair in trainerMarkers)
        {
            TrainerBattleTrigger trainer = pair.Key;
            RectTransform marker = pair.Value;

            if (marker == null)
            {
                continue;
            }

            bool shouldShow = trainer != null && trainer.gameObject.activeInHierarchy && !trainer.IsDefeated;

            marker.gameObject.SetActive(shouldShow);

            if (!shouldShow)
            {
                continue;
            }

            Vector2 normalized = WorldToNormalized(trainer.transform.position);
            marker.anchoredPosition = NormalizedToMapPosition(normalized);
        }
    }

    private void UpdateBellMarkers()
    {
        if (bells == null || bellMarkers.Count == 0 || playBounds == null || mapImage == null)
        {
            return;
        }

        int markerIndex = 0;

        for (int i = 0; i < bells.Length; i++)
        {
            Transform bell = bells[i];

            if (bell == null)
            {
                continue;
            }

            if (markerIndex >= bellMarkers.Count)
            {
                break;
            }

            RectTransform marker = bellMarkers[markerIndex];

            if (marker == null)
            {
                markerIndex++;
                continue;
            }

            bool shouldShow = bell.gameObject.activeInHierarchy;
            marker.gameObject.SetActive(shouldShow);

            if (shouldShow)
            {
                Vector2 normalized = WorldToNormalized(bell.position);
                marker.anchoredPosition = NormalizedToMapPosition(normalized);
            }

            markerIndex++;
        }
    }

    private void UpdateStorm()
    {
        if (storm == null || mapMaterial == null || playBounds == null || mapImage == null)
        {
            return;
        }

        Bounds bounds = playBounds.bounds;
        Vector2 normalized = WorldToNormalized(storm.GetCenterWorld());
        Vector2 uv = NormalizedToOverlayUV(normalized);

        mapMaterial.SetVector(CenterId, new Vector4(uv.x, uv.y, 0f, 0f));

        float fullWidth = mapImage.rect.width;
        float fullHeight = mapImage.rect.height;

        float usableWidth = fullWidth - mapLeftPadding - mapRightPadding;
        float usableHeight = fullHeight - mapTopPadding - mapBottomPadding;

        float radiusWorld = storm.GetRadiusWorld();
        float radiusX = (radiusWorld / bounds.size.x) * (usableWidth / fullWidth);
        float radiusY = (radiusWorld / bounds.size.y) * (usableHeight / fullHeight);
        float radiusUV = Mathf.Max(radiusX, radiusY);

        mapMaterial.SetFloat(RadiusId, radiusUV);
    }

    private Vector2 WorldToNormalized(Vector2 worldPosition)
    {
        Bounds bounds = playBounds.bounds;

        float x = Mathf.InverseLerp(bounds.min.x, bounds.max.x, worldPosition.x);
        float y = Mathf.InverseLerp(bounds.min.y, bounds.max.y, worldPosition.y);

        return new Vector2(x, y);
    }

    private Vector2 NormalizedToMapPosition(Vector2 normalized)
    {
        float fullWidth = mapImage.rect.width;
        float fullHeight = mapImage.rect.height;

        float usableWidth = fullWidth - mapLeftPadding - mapRightPadding;
        float usableHeight = fullHeight - mapTopPadding - mapBottomPadding;

        float x = mapLeftPadding + normalized.x * usableWidth - fullWidth * 0.5f;
        float y = mapBottomPadding + normalized.y * usableHeight - fullHeight * 0.5f;

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