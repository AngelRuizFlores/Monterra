using System.Collections.Generic;
using UnityEngine;

public sealed class TrainerBattleProgress : MonoBehaviour
{
    private readonly HashSet<string> defeatedTrainerIds = new();
    private readonly List<TrainerBattleTrigger> allTrainers = new();

    public int CurrentVictories => defeatedTrainerIds.Count;
    public int RemainingAliveTrainers => CountAliveTrainers();

    private void Awake()
    {
        RefreshTrainerList();
    }

    public void RefreshTrainerList()
    {
        allTrainers.Clear();
        allTrainers.AddRange(FindObjectsByType<TrainerBattleTrigger>(FindObjectsSortMode.None));

        Debug.Log($"{nameof(TrainerBattleProgress)}: trainers encontrados={allTrainers.Count}", this);
    }

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
            $"{nameof(TrainerBattleProgress)}: trainer derrotado='{trainer.name}', " +
            $"added={added}, victorias={CurrentVictories}, vivos_restantes={RemainingAliveTrainers}",
            this
        );

        return added;
    }

    public bool HasNoLivingTrainers()
    {
        int remaining = CountAliveTrainers();
        bool finished = remaining <= 0;

        Debug.Log($"{nameof(TrainerBattleProgress)}: HasNoLivingTrainers={finished}, vivos_restantes={remaining}", this);

        return finished;
    }

    public bool HasReachedRequiredVictories()
    {
        return HasNoLivingTrainers();
    }

    public void ResetProgress()
    {
        defeatedTrainerIds.Clear();
        RefreshTrainerList();

        Debug.Log($"{nameof(TrainerBattleProgress)}: progreso reiniciado.");
    }

    private int CountAliveTrainers()
    {
        int alive = 0;

        for (int i = 0; i < allTrainers.Count; i++)
        {
            TrainerBattleTrigger trainer = allTrainers[i];

            if (trainer == null)
            {
                continue;
            }

            if (trainer.IsDefeated)
            {
                continue;
            }

            if (!trainer.gameObject.activeInHierarchy)
            {
                continue;
            }

            alive++;
        }

        return alive;
    }
}