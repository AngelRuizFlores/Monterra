using System.Collections.Generic;
using UnityEngine;

public sealed class TrainerStormDirector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private StormOverlayController stormController;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Transform playerTransform;

    [Header("Safe Points")]
    [SerializeField] private TrainerSafePoint[] safePoints;
    [SerializeField] private float minDistanceFromPlayer = 8f;
    [SerializeField] private float minDistanceBetweenTrainers = 2f;

    [Header("Trainer Limits By Phase")]
    [SerializeField] private int[] maxAliveTrainersByPhase = { 12, 9, 6, 3, 2 };

    [Header("Relocation")]
    [SerializeField] private bool relocateOnlyWhenOffscreen = true;
    [SerializeField] private float viewportMargin = 0.1f;

    private readonly List<TrainerBattleTrigger> trainers = new();

    private void Awake()
    {
        if (stormController == null)
            stormController = FindFirstObjectByType<StormOverlayController>();

        if (worldCamera == null)
            worldCamera = Camera.main;

        trainers.Clear();
        trainers.AddRange(FindObjectsByType<TrainerBattleTrigger>(FindObjectsSortMode.None));

        if (safePoints == null || safePoints.Length == 0)
            safePoints = FindObjectsByType<TrainerSafePoint>(FindObjectsSortMode.None);
    }

    private void OnEnable()
    {
        if (stormController != null)
            stormController.OnPhaseChanged += HandleStormPhaseChanged;
    }

    private void OnDisable()
    {
        if (stormController != null)
            stormController.OnPhaseChanged -= HandleStormPhaseChanged;
    }

    private void HandleStormPhaseChanged(int phase)
    {
        int aliveBefore = GetAliveTrainers().Count;

        Debug.Log($"[TrainerStormDirector] Phase {phase} START. Alive trainers BEFORE: {aliveBefore}", this);

        UpgradeTrainersForPhase(phase);
        CullExtraTrainers(phase);
        RelocateTrainersOutsideSafeZone();

        int aliveAfter = GetAliveTrainers().Count;

        Debug.Log($"[TrainerStormDirector] Phase {phase} END. Alive trainers AFTER: {aliveAfter}", this);
    }

    private void UpgradeTrainersForPhase(int phase)
    {
        for (int i = 0; i < trainers.Count; i++)
        {
            TrainerBattleTrigger trainer = trainers[i];

            if (trainer == null || trainer.IsDefeated || !trainer.gameObject.activeInHierarchy)
                continue;

            trainer.SetTrainerDefinitionForPhase(phase);
        }
    }

    private void CullExtraTrainers(int phase)
    {
        int maxAlive = GetMaxAliveForPhase(phase);
        List<TrainerBattleTrigger> alive = GetAliveTrainers();

        if (alive.Count <= maxAlive)
            return;

        Vector2 safeCenter = stormController.GetCenterWorld();

        alive.Sort((a, b) =>
        {
            float distA = Vector2.Distance(a.transform.position, safeCenter);
            float distB = Vector2.Distance(b.transform.position, safeCenter);
            return distB.CompareTo(distA);
        });

        int amountToRemove = alive.Count - maxAlive;

        Debug.Log($"[TrainerStormDirector] CULLING -> Alive={alive.Count}, MaxAllowed={maxAlive}, Removing={amountToRemove}", this);

        for (int i = 0; i < amountToRemove; i++)
        {
            TrainerBattleTrigger trainer = alive[i];

            if (trainer == null)
                continue;

            Debug.Log($"[TrainerStormDirector] REMOVED -> {trainer.name}", trainer);

            trainer.gameObject.SetActive(false);
        }
    }

    private void RelocateTrainersOutsideSafeZone()
    {
        List<TrainerBattleTrigger> alive = GetAliveTrainers();

        for (int i = 0; i < alive.Count; i++)
        {
            TrainerBattleTrigger trainer = alive[i];

            if (trainer == null)
                continue;

            if (stormController.IsInsideSafeZone(trainer.transform.position))
                continue;

            if (relocateOnlyWhenOffscreen && IsVisibleToCamera(trainer.transform.position))
                continue;

            TrainerSafePoint point = FindValidSafePoint(alive);

            if (point == null)
            {
                Debug.LogWarning($"{nameof(TrainerStormDirector)}: no valid safe point found for {trainer.name}.", this);
                continue;
            }

            Debug.Log($"[TrainerStormDirector] RELOCATED -> {trainer.name} to {point.name}", trainer);

            trainer.transform.position = point.Position;

            DisableRandomMovementAfterRelocation(trainer);
        }
    }

    private void DisableRandomMovementAfterRelocation(TrainerBattleTrigger trainer)
    {
        if (trainer == null)
            return;

        RandomMovementBehavior movement = trainer.GetComponent<RandomMovementBehavior>();
        if (movement != null)
            movement.enabled = false;

        Rigidbody2D rb = trainer.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    private TrainerSafePoint FindValidSafePoint(List<TrainerBattleTrigger> activeTrainers)
    {
        if (safePoints == null || safePoints.Length == 0)
            return null;

        List<TrainerSafePoint> candidates = new();

        for (int i = 0; i < safePoints.Length; i++)
        {
            TrainerSafePoint point = safePoints[i];

            if (point == null || !point.Available)
                continue;

            if (!stormController.IsInsideSafeZone(point.Position))
                continue;

            if (playerTransform != null &&
                Vector2.Distance(point.Position, playerTransform.position) < minDistanceFromPlayer)
                continue;

            if (IsTooCloseToOtherTrainer(point.Position, activeTrainers))
                continue;

            candidates.Add(point);
        }

        if (candidates.Count == 0)
            return null;

        int index = Random.Range(0, candidates.Count);
        return candidates[index];
    }

    private bool IsTooCloseToOtherTrainer(Vector3 position, List<TrainerBattleTrigger> activeTrainers)
    {
        for (int i = 0; i < activeTrainers.Count; i++)
        {
            TrainerBattleTrigger trainer = activeTrainers[i];

            if (trainer == null || !trainer.gameObject.activeInHierarchy)
                continue;

            if (Vector2.Distance(position, trainer.transform.position) < minDistanceBetweenTrainers)
                return true;
        }

        return false;
    }

    private bool IsVisibleToCamera(Vector3 worldPosition)
    {
        if (worldCamera == null)
            return false;

        Vector3 viewport = worldCamera.WorldToViewportPoint(worldPosition);

        return viewport.z > 0f &&
               viewport.x >= -viewportMargin &&
               viewport.x <= 1f + viewportMargin &&
               viewport.y >= -viewportMargin &&
               viewport.y <= 1f + viewportMargin;
    }

    private List<TrainerBattleTrigger> GetAliveTrainers()
    {
        List<TrainerBattleTrigger> alive = new();

        for (int i = 0; i < trainers.Count; i++)
        {
            TrainerBattleTrigger trainer = trainers[i];

            if (trainer == null)
                continue;

            if (trainer.IsDefeated)
                continue;

            if (!trainer.gameObject.activeInHierarchy)
                continue;

            alive.Add(trainer);
        }

        return alive;
    }

    private int GetMaxAliveForPhase(int phase)
    {
        if (maxAliveTrainersByPhase == null || maxAliveTrainersByPhase.Length == 0)
            return 12;

        int index = Mathf.Clamp(phase, 0, maxAliveTrainersByPhase.Length - 1);
        return Mathf.Max(1, maxAliveTrainersByPhase[index]);
    }
}