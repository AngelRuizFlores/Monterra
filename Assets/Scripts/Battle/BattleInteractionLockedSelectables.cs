using UnityEngine;
using UnityEngine.UI;

public class BattleInteractionLockedSelectables : MonoBehaviour
{
    [Header("Selectables To Lock")]
    [SerializeField] private Selectable[] selectables;

    private bool[] previousInteractableStates;
    private bool hasStoredStates;

    private void Awake()
    {
        EnsureCache();
    }

    private void OnEnable()
    {
        BattleInteractionLock.OnChanged += ApplyLockState;

        if (BattleInteractionLock.IsBlocked)
        {
            ApplyLockState(true);
        }
    }

    private void OnDisable()
    {
        BattleInteractionLock.OnChanged -= ApplyLockState;
    }

    private void EnsureCache()
    {
        int length = selectables != null ? selectables.Length : 0;

        if (previousInteractableStates != null && previousInteractableStates.Length == length)
        {
            return;
        }

        previousInteractableStates = new bool[length];
        hasStoredStates = false;
    }

    private void ApplyLockState(bool blocked)
    {
        if (selectables == null)
        {
            return;
        }

        EnsureCache();

        if (blocked)
        {
            StoreCurrentStates();
            SetInteractable(false);
            return;
        }

        RestorePreviousStates();
    }

    private void StoreCurrentStates()
    {
        for (int i = 0; i < selectables.Length; i++)
        {
            Selectable selectable = selectables[i];

            if (selectable == null)
            {
                continue;
            }

            previousInteractableStates[i] = selectable.interactable;
        }

        hasStoredStates = true;
    }

    private void SetInteractable(bool value)
    {
        for (int i = 0; i < selectables.Length; i++)
        {
            Selectable selectable = selectables[i];

            if (selectable == null)
            {
                continue;
            }

            selectable.interactable = value;
        }
    }

    private void RestorePreviousStates()
    {
        if (!hasStoredStates)
        {
            return;
        }

        for (int i = 0; i < selectables.Length; i++)
        {
            Selectable selectable = selectables[i];

            if (selectable == null)
            {
                continue;
            }

            selectable.interactable = previousInteractableStates[i];
        }

        hasStoredStates = false;
    }
}