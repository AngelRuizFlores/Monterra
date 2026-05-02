using System.Collections.Generic;
using UnityEngine;

public sealed class PokeballWorldManager : MonoBehaviour
{
    [SerializeField] private PokeballSpawnPoint[] pokeballs;
    [SerializeField] private int activePokeballCount = 12;

    private void Start()
    {
        if (pokeballs == null || pokeballs.Length == 0)
            pokeballs = FindObjectsByType<PokeballSpawnPoint>(FindObjectsSortMode.None);

        if (GameStartMode.LoadGame)
            ApplyLoadedPokeballs();
        else
            GenerateNewPokeballs();
    }

    private void GenerateNewPokeballs()
    {
        SaveData data = SaveGameManager.Load() ?? new SaveData();

        data.activePokeballIds.Clear();
        data.collectedPokeballIds.Clear();

        List<PokeballSpawnPoint> pool = new List<PokeballSpawnPoint>(pokeballs);

        for (int i = 0; i < pool.Count; i++)
        {
            int r = Random.Range(i, pool.Count);
            (pool[i], pool[r]) = (pool[r], pool[i]);
        }

        int count = Mathf.Min(activePokeballCount, pool.Count);

        for (int i = 0; i < pokeballs.Length; i++)
            pokeballs[i].gameObject.SetActive(false);

        for (int i = 0; i < count; i++)
        {
            PokeballSpawnPoint point = pool[i];

            if (point == null)
                continue;

            point.gameObject.SetActive(true);
            data.activePokeballIds.Add(point.PokeballId);
        }

        SaveGameManager.SaveRaw(data);

        Debug.Log($"[POKEBALLS] New game generated {data.activePokeballIds.Count} active pokeballs.");
    }

    private void ApplyLoadedPokeballs()
    {
        SaveData data = SaveGameManager.Load();

        if (data == null)
        {
            GenerateNewPokeballs();
            return;
        }

        HashSet<string> active = new HashSet<string>(data.activePokeballIds);
        HashSet<string> collected = new HashSet<string>(data.collectedPokeballIds);

        for (int i = 0; i < pokeballs.Length; i++)
        {
            PokeballSpawnPoint point = pokeballs[i];

            if (point == null)
                continue;

            bool shouldExist =
                active.Contains(point.PokeballId) &&
                !collected.Contains(point.PokeballId);

            point.gameObject.SetActive(shouldExist);
        }

        Debug.Log("[POKEBALLS] Loaded pokeball state.");
    }
}