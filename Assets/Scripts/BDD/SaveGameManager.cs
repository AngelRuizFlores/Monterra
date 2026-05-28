using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class SaveGameManager
{
    private const string OwnedSpeciesKey = "OwnedSpeciesIds";

    private static string SavePath => Application.persistentDataPath + "/savegame.json";

    public static void Save(PlayerTeam playerTeam, Transform playerTransform)
    {
        SaveData data = Load() ?? new SaveData();

        data.playerPosition = playerTransform.position;
        data.unlockedSlots = playerTeam.UnlockedSlots;

        data.team.Clear();

        foreach (MonInstance mon in playerTeam.GetOwnedMons())
        {
            if (mon == null || mon.species == null)
            {
                continue;
            }

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

    public static SaveData Load()
    {
        if (!File.Exists(SavePath))
        {
            return null;
        }

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
        {
            File.Delete(SavePath);
        }
    }

    public static void SaveRaw(SaveData data)
    {
        if (data == null)
        {
            return;
        }

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);
    }

    public static void RegisterCollectedPokeball(string pokeballId)
    {
        if (string.IsNullOrWhiteSpace(pokeballId))
        {
            return;
        }

        SaveData data = Load() ?? new SaveData();

        if (data.activePokeballIds == null)
        {
            data.activePokeballIds = new List<string>();
        }

        if (data.collectedPokeballIds == null)
        {
            data.collectedPokeballIds = new List<string>();
        }

        data.activePokeballIds.RemoveAll(id => id == pokeballId);

        if (!data.collectedPokeballIds.Contains(pokeballId))
        {
            data.collectedPokeballIds.Add(pokeballId);
        }

        SaveRaw(data);

        Debug.Log($"[SAVE] Collected pokeball registered: {pokeballId}");
    }

    public static bool HasPlayableSave()
    {
        SaveData data = Load();

        if (data == null)
        {
            return false;
        }

        if (data.team == null || data.team.Count == 0)
        {
            return false;
        }

        for (int i = 0; i < data.team.Count; i++)
        {
            if (data.team[i] != null && !string.IsNullOrWhiteSpace(data.team[i].speciesId))
            {
                return true;
            }
        }

        return false;
    }

    public static void RegisterOwnedSpecies(PlayerTeam playerTeam)
    {
        if (playerTeam == null)
        {
            return;
        }

        List<MonInstance> mons = playerTeam.GetOwnedMons();

        for (int i = 0; i < mons.Count; i++)
        {
            if (mons[i] == null || mons[i].species == null)
            {
                continue;
            }

            RegisterOwnedSpecies(mons[i].species.name);
        }
    }

    public static void RegisterOwnedSpecies(string speciesId)
    {
        if (string.IsNullOrWhiteSpace(speciesId))
        {
            return;
        }

        string current = PlayerPrefs.GetString(OwnedSpeciesKey, "");
        List<string> ids = new List<string>(current.Split('|'));

        if (!ids.Contains(speciesId))
        {
            ids.Add(speciesId);
        }

        PlayerPrefs.SetString(OwnedSpeciesKey, string.Join("|", ids));
        PlayerPrefs.Save();
    }

    public static HashSet<string> GetOwnedSpeciesIds()
    {
        string current = PlayerPrefs.GetString(OwnedSpeciesKey, "");
        HashSet<string> result = new HashSet<string>();

        string[] ids = current.Split('|');

        for (int i = 0; i < ids.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(ids[i]))
            {
                result.Add(ids[i]);
            }
        }

        return result;
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
            {
                continue;
            }

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
        {
            return;
        }

        data.hasStormState = true;
        data.stormPhase = storm.CurrentPhase;
        data.stormCenter = storm.GetCenterWorld();
        data.stormRadius = storm.GetRadiusWorld();

        Debug.Log($"[SAVE] Storm phase={data.stormPhase}, radius={data.stormRadius}");
    }

    public static bool IsPokeballCollected(string pokeballId)
    {
        if (string.IsNullOrWhiteSpace(pokeballId))
        {
            return false;
        }

        SaveData data = Load();

        if (data == null || data.collectedPokeballIds == null)
        {
            return false;
        }

        return data.collectedPokeballIds.Contains(pokeballId);
    }

    public static void ResetOwnedSpecies()
    {
        PlayerPrefs.DeleteKey("OwnedSpeciesIds");
        PlayerPrefs.Save();

        Debug.Log("[SAVE] Owned species reset.");
    }
}