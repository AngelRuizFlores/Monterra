using System.Collections.Generic;
using UnityEngine;

public partial class LevelManager
{
    private void LoadGameIfExists()
    {
        SaveData data = SaveGameManager.Load();

        if (data == null)
        {
            Debug.Log("[LOAD] No save found.");
            return;
        }

        PlayerTeam team = GetPlayerTeam();

        if (team == null || playerMon == null)
        {
            Debug.LogWarning("[LOAD] PlayerTeam or PlayerMon not found.");
            return;
        }

        playerMon.transform.position = data.playerPosition;

        for (int i = 0; i < team.team.Length; i++)
        {
            team.team[i] = null;
        }

        team.SetUnlockedSlotsFromSave(data.unlockedSlots);

        int count = Mathf.Min(data.team.Count, team.team.Length);

        for (int i = 0; i < count; i++)
        {
            MonSaveData monData = data.team[i];
            MonSpecies species = FindSpeciesById(monData.speciesId);

            if (species == null)
            {
                Debug.LogWarning($"[LOAD] Species not found: {monData.speciesId}");
                continue;
            }

            MonInstance instance = new MonInstance
            {
                species = species,
                level = monData.level,
                currentHP = monData.currentHP,
                experience = monData.experience
            };

            MonLevelSystem.InitMovesForCurrentLevel(instance);

            team.team[i] = instance;
        }

        team.EnsureValidActiveMon();
        playerMon.InitIfNeeded();
        team.NotifyChanged();

        battleUI?.ShowPlayerMon(playerMon);
        battleUI?.SetPlayerExp(playerMon.instance);

        movesUI?.Refresh();

        ApplySavedTrainerState(data);
        ApplySavedStormState(data);

        music?.StartWorldMusic();

        Debug.Log("[LOAD] Game loaded correctly.");
    }

    private MonSpecies FindSpeciesById(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || allSpecies == null)
        {
            return null;
        }

        for (int i = 0; i < allSpecies.Length; i++)
        {
            if (allSpecies[i] == null)
            {
                continue;
            }

            if (allSpecies[i].name == id)
            {
                return allSpecies[i];
            }
        }

        return null;
    }

    private void ApplySavedTrainerState(SaveData data)
    {
        if (data == null)
        {
            return;
        }

        HashSet<string> defeated = new HashSet<string>(data.defeatedTrainerIds);
        HashSet<string> removed = new HashSet<string>(data.removedTrainerIds);

        TrainerBattleTrigger[] trainers = FindObjectsByType<TrainerBattleTrigger>(FindObjectsSortMode.None);

        for (int i = 0; i < trainers.Length; i++)
        {
            TrainerBattleTrigger trainer = trainers[i];

            if (trainer == null || string.IsNullOrWhiteSpace(trainer.TrainerId))
            {
                continue;
            }

            if (defeated.Contains(trainer.TrainerId))
            {
                trainer.ApplyDefeatedFromSave();
                continue;
            }

            if (removed.Contains(trainer.TrainerId))
            {
                trainer.gameObject.SetActive(false);
                continue;
            }

            trainer.SetTrainerDefinitionForPhase(data.stormPhase);
        }

        Debug.Log($"[LOAD] Trainers applied. Defeated={defeated.Count}, Removed={removed.Count}");
    }

    private void ApplySavedStormState(SaveData data)
    {
        if (data == null || !data.hasStormState)
        {
            return;
        }

        StormOverlayController storm = FindFirstObjectByType<StormOverlayController>();

        if (storm == null)
        {
            Debug.LogWarning("[LOAD] StormOverlayController not found.");
            return;
        }

        storm.LoadStormState(data.stormPhase, data.stormCenter, data.stormRadius);

        Debug.Log($"[LOAD] Storm loaded. Phase={data.stormPhase}, Radius={data.stormRadius}");
    }
}