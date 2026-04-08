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

        if (trainerDefinition == null)
        {
            Debug.LogError($"{nameof(TrainerBattleTrigger)} en '{name}' no tiene {nameof(TrainerDefinition)} asignado.", this);
            return;
        }

        if (!trainerDefinition.IsValid(out string error))
            Debug.LogError($"{nameof(TrainerBattleTrigger)} inválido en '{name}': {error}", this);

        ApplyDefeatedState(defeated);
    }

    public bool CanStartBattle(out string error)
    {
        if (defeated)
        {
            error = "este trainer ya fue derrotado.";
            return false;
        }

        if (trainerDefinition == null)
        {
            error = "falta el TrainerDefinition.";
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

    private void ApplyDefeatedState(bool value)
    {
        if (disableColliderWhenDefeated && cachedCollider != null)
            cachedCollider.enabled = !value;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(trainerId))
            trainerId = Guid.NewGuid().ToString("N");
    }
#endif
}