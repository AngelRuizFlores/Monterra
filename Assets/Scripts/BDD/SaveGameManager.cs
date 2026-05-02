using System.IO;
using UnityEngine;

public static class SaveGameManager
{
    private static string SavePath => Application.persistentDataPath + "/savegame.json";

    public static void Save(PlayerTeam playerTeam, Transform playerTransform)
    {
        SaveData data = Load() ?? new SaveData();

        data.playerPosition = playerTransform.position;
        data.unlockedSlots = playerTeam.UnlockedSlots;

        data.team.Clear();

        foreach (var mon in playerTeam.GetOwnedMons())
        {
            if (mon == null || mon.species == null)
                continue;

            data.team.Add(new MonSaveData
            {
                speciesId = mon.species.name,
                level = mon.level,
                currentHP = mon.currentHP,
                experience = mon.experience
            });
        }

        SaveTrainerState(data);
        SaveStormState(data);

        SaveRaw(data);

        Debug.Log("[SAVE] Game saved at: " + SavePath);
        Debug.Log($"[SAVE] Unlocked slots: {data.unlockedSlots}, Mons saved: {data.team.Count}");
    }

    private static void SaveTrainerState(SaveData data)
    {
        data.defeatedTrainerIds.Clear();
        data.removedTrainerIds.Clear();

        TrainerBattleTrigger[] trainers = Object.FindObjectsByType<TrainerBattleTrigger>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < trainers.Length; i++)
        {
            TrainerBattleTrigger trainer = trainers[i];

            if (trainer == null || string.IsNullOrWhiteSpace(trainer.TrainerId))
                continue;

            if (trainer.IsDefeated)
            {
                data.defeatedTrainerIds.Add(trainer.TrainerId);
                continue;
            }

            if (!trainer.gameObject.activeInHierarchy)
            {
                data.removedTrainerIds.Add(trainer.TrainerId);
            }
        }

        Debug.Log($"[SAVE] Defeated trainers: {data.defeatedTrainerIds.Count}, Removed trainers: {data.removedTrainerIds.Count}");
    }

    private static void SaveStormState(SaveData data)
    {
        StormOverlayController storm = Object.FindFirstObjectByType<StormOverlayController>();

        if (storm == null)
            return;

        data.hasStormState = true;
        data.stormPhase = storm.CurrentPhase;
        data.stormCenter = storm.GetCenterWorld();
        data.stormRadius = storm.GetRadiusWorld();

        Debug.Log($"[SAVE] Storm phase={data.stormPhase}, radius={data.stormRadius}");
    }

    public static SaveData Load()
    {
        if (!File.Exists(SavePath))
            return null;

        string json = File.ReadAllText(SavePath);
        return JsonUtility.FromJson<SaveData>(json);
    }

    public static bool HasSave()
    {
        return File.Exists(SavePath);
    }

    public static void DeleteSave()
    {
        if (File.Exists(SavePath))
            File.Delete(SavePath);
    }

    public static void SaveRaw(SaveData data)
    {
        if (data == null)
            return;

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);
    }

    public static void RegisterCollectedPokeball(string pokeballId)
    {
        if (string.IsNullOrWhiteSpace(pokeballId))
            return;

        SaveData data = Load() ?? new SaveData();

        if (!data.collectedPokeballIds.Contains(pokeballId))
            data.collectedPokeballIds.Add(pokeballId);

        SaveRaw(data);

        Debug.Log($"[SAVE] Collected pokeball registered: {pokeballId}");
    }
}