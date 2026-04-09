using System.Collections.Generic;
using UnityEngine;

public sealed class TrainerBattleProgress : MonoBehaviour
{
    [SerializeField] private int requiredVictories = 5;

    private readonly HashSet<string> defeatedTrainerIds = new HashSet<string>();

    public int RequiredVictories => Mathf.Max(1, requiredVictories);
    public int CurrentVictories => defeatedTrainerIds.Count;

    public bool TryRegisterVictory(TrainerBattleTrigger trainer)
    {
        if (trainer == null)
        {
            Debug.LogWarning($"{nameof(TrainerBattleProgress)}: trainer null al registrar victoria.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(trainer.TrainerId))
        {
            Debug.LogWarning($"{nameof(TrainerBattleProgress)}: el trainer '{trainer.name}' no tiene TrainerId válido.");
            return false;
        }

        bool added = defeatedTrainerIds.Add(trainer.TrainerId);

        Debug.Log(
            $"{nameof(TrainerBattleProgress)}: trainer='{trainer.name}', id='{trainer.TrainerId}', " +
            $"added={added}, victorias={CurrentVictories}/{RequiredVictories}"
        );

        return added;
    }

    public bool HasReachedRequiredVictories()
    {
        bool reached = CurrentVictories >= RequiredVictories;
        Debug.Log($"{nameof(TrainerBattleProgress)}: HasReachedRequiredVictories={reached} ({CurrentVictories}/{RequiredVictories})");
        return reached;
    }

    public void ResetProgress()
    {
        defeatedTrainerIds.Clear();
        Debug.Log($"{nameof(TrainerBattleProgress)}: progreso reiniciado.");
    }
}