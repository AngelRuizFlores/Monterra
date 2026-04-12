using System;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public sealed class TrainerBattleTrigger : MonoBehaviour
{
    [SerializeField] private TrainerDefinition trainerDefinition;
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
            return false;

        error = null;
        return true;
    }

    public void MarkAsDefeated()
    {
        if (defeated)
            return;

        defeated = true;
        ApplyDefeatedState(true);
        onDefeated?.Invoke();
    }

    private void ApplyDefeatedState(bool isDefeated)
    {
        if (disableColliderWhenDefeated && cachedCollider != null)
            cachedCollider.enabled = !isDefeated;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(trainerId))
            trainerId = Guid.NewGuid().ToString("N");
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