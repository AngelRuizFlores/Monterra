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
        if (trainer == null || string.IsNullOrWhiteSpace(trainer.TrainerId))
            return false;

        return defeatedTrainerIds.Add(trainer.TrainerId);
    }

    public bool HasReachedRequiredVictories()
    {
        return CurrentVictories >= RequiredVictories;
    }

    public void ResetProgress()
    {
        defeatedTrainerIds.Clear();
    }
}