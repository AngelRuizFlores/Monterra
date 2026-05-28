using System;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public sealed class TrainerBattleTrigger : MonoBehaviour
{
    [Header("Definition")]
    [SerializeField] private TrainerDefinition trainerDefinition;
    [SerializeField] private TrainerDefinition[] phaseDefinitions;

    [Header("Defeat")]
    [SerializeField] private bool disableColliderWhenDefeated = true;
    [SerializeField] private UnityEvent onDefeated;

    [SerializeField, HideInInspector] private string trainerId;

    private Collider2D cachedCollider;
    private bool defeated;

    public TrainerDefinition TrainerDefinition => trainerDefinition;
    public string TrainerId => trainerId;
    public bool IsDefeated => defeated;

    private void Awake()
    {
        cachedCollider = GetComponent<Collider2D>();

        if (TryGetComponent<WildMon>(out _))
        {
            Debug.LogError(
                $"{nameof(TrainerBattleTrigger)} on '{name}' cannot coexist with {nameof(WildMon)} on the same GameObject.",
                this
            );
        }

        if (string.IsNullOrWhiteSpace(trainerId))
        {
            trainerId = Guid.NewGuid().ToString("N");

            Debug.LogWarning(
                $"{nameof(TrainerBattleTrigger)} on '{name}' had no trainerId. A new one was generated at runtime: {trainerId}",
                this
            );
        }

        if (trainerDefinition == null)
        {
            Debug.LogError(
                $"{nameof(TrainerBattleTrigger)} on '{name}' has no {nameof(TrainerDefinition)} assigned.",
                this
            );

            return;
        }

        if (!trainerDefinition.IsValid(out string error))
        {
            Debug.LogError(
                $"{nameof(TrainerBattleTrigger)} on '{name}' is invalid: {error}",
                this
            );
        }

        ApplyDefeatedState(defeated);
    }

    public bool CanStartBattle(out string error)
    {
        if (defeated)
        {
            error = "this trainer has already been defeated.";
            return false;
        }

        if (trainerDefinition == null)
        {
            error = "TrainerDefinition is missing.";
            return false;
        }

        if (!trainerDefinition.IsValid(out error))
        {
            return false;
        }

        error = null;
        return true;
    }

    public void MarkAsDefeated()
    {
        if (defeated)
        {
            return;
        }

        defeated = true;

        ApplyDefeatedState(true);
        StopMovement();

        gameObject.SetActive(false);

        onDefeated?.Invoke();
    }

    public void ApplyDefeatedFromSave()
    {
        defeated = true;

        ApplyDefeatedState(true);
        StopMovement();

        gameObject.SetActive(false);
    }

    public void SetTrainerDefinitionForPhase(int phase)
    {
        if (phaseDefinitions == null || phaseDefinitions.Length == 0)
        {
            return;
        }

        int index = Mathf.Clamp(phase, 0, phaseDefinitions.Length - 1);
        TrainerDefinition newDefinition = phaseDefinitions[index];

        if (newDefinition == null)
        {
            return;
        }

        if (!newDefinition.IsValid(out string error))
        {
            Debug.LogWarning($"{nameof(TrainerBattleTrigger)}: phase definition invalid: {error}", this);
            return;
        }

        trainerDefinition = newDefinition;
    }

    private void ApplyDefeatedState(bool isDefeated)
    {
        if (disableColliderWhenDefeated && cachedCollider != null)
        {
            cachedCollider.enabled = !isDefeated;
        }
    }

    private void StopMovement()
    {
        RandomMovementBehavior movement = GetComponent<RandomMovementBehavior>();

        if (movement != null)
        {
            movement.enabled = false;
        }

        Rigidbody2D rigidbody2D = GetComponent<Rigidbody2D>();

        if (rigidbody2D != null)
        {
            rigidbody2D.linearVelocity = Vector2.zero;
            rigidbody2D.angularVelocity = 0f;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(trainerId))
        {
            trainerId = Guid.NewGuid().ToString("N");
        }
    }

    [ContextMenu("Regenerate Trainer ID")]
    private void RegenerateTrainerId()
    {
        trainerId = Guid.NewGuid().ToString("N");

        Debug.Log(
            $"{nameof(TrainerBattleTrigger)} on '{name}' regenerated trainerId: {trainerId}",
            this
        );
    }
#endif
}