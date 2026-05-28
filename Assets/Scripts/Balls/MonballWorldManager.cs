using System.Collections.Generic;
using UnityEngine;

public sealed class MonballWorldManager : MonoBehaviour
{
    [Header("All Pokeballs")]
    [SerializeField] private MonballSpawnPoint[] pokeballs;

    [Header("Guaranteed Spawn Pokeballs")]
    [SerializeField] private MonballSpawnPoint[] guaranteedPokeballs;

    [Header("Random Pokeballs")]
    [SerializeField] private int randomPokeballCount = 32;

    private void Start()
    {
        if (pokeballs == null || pokeballs.Length == 0)
        {
            pokeballs = FindObjectsByType<MonballSpawnPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        }

        if (GameStartMode.LoadGame)
        {
            ApplyLoadedPokeballs();
        }
        else
        {
            GenerateNewPokeballs();
        }
    }

    private void GenerateNewPokeballs()
    {
        SaveData data = SaveGameManager.Load() ?? new SaveData();

        data.activePokeballIds.Clear();
        data.collectedPokeballIds.Clear();

        for (int i = 0; i < pokeballs.Length; i++)
        {
            if (pokeballs[i] != null)
            {
                pokeballs[i].gameObject.SetActive(false);
            }
        }

        AddGuaranteedPokeballs(data);

        List<MonballSpawnPoint> pool = BuildRandomPool();

        for (int i = 0; i < pool.Count; i++)
        {
            int randomIndex = Random.Range(i, pool.Count);
            (pool[i], pool[randomIndex]) = (pool[randomIndex], pool[i]);
        }

        int count = Mathf.Min(randomPokeballCount, pool.Count);

        for (int i = 0; i < count; i++)
        {
            MonballSpawnPoint point = pool[i];

            if (point == null)
            {
                continue;
            }

            point.gameObject.SetActive(true);

            if (!data.activePokeballIds.Contains(point.MonballId))
            {
                data.activePokeballIds.Add(point.MonballId);
            }
        }

        SaveGameManager.SaveRaw(data);

        Debug.Log($"[MONBALLS] New game generated {data.activePokeballIds.Count} active MONballs. Guaranteed={CountGuaranteed()}, Random={count}");
    }

    private void AddGuaranteedPokeballs(SaveData data)
    {
        if (guaranteedPokeballs == null)
        {
            return;
        }

        for (int i = 0; i < guaranteedPokeballs.Length; i++)
        {
            MonballSpawnPoint point = guaranteedPokeballs[i];

            if (point == null)
            {
                continue;
            }

            point.gameObject.SetActive(true);

            if (!data.activePokeballIds.Contains(point.MonballId))
            {
                data.activePokeballIds.Add(point.MonballId);
            }
        }
    }

    private List<MonballSpawnPoint> BuildRandomPool()
    {
        List<MonballSpawnPoint> pool = new List<MonballSpawnPoint>();
        HashSet<string> guaranteedIds = new HashSet<string>();

        if (guaranteedPokeballs != null)
        {
            for (int i = 0; i < guaranteedPokeballs.Length; i++)
            {
                if (guaranteedPokeballs[i] != null)
                {
                    guaranteedIds.Add(guaranteedPokeballs[i].MonballId);
                }
            }
        }

        for (int i = 0; i < pokeballs.Length; i++)
        {
            MonballSpawnPoint point = pokeballs[i];

            if (point == null)
            {
                continue;
            }

            if (guaranteedIds.Contains(point.MonballId))
            {
                continue;
            }

            pool.Add(point);
        }

        return pool;
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
            MonballSpawnPoint point = pokeballs[i];

            if (point == null)
            {
                continue;
            }

            bool shouldExist = active.Contains(point.MonballId) && !collected.Contains(point.MonballId);

            point.gameObject.SetActive(shouldExist);
        }

        Debug.Log("[MONBALLS] Loaded MONball state.");
    }

    private int CountGuaranteed()
    {
        int count = 0;

        if (guaranteedPokeballs == null)
        {
            return count;
        }

        for (int i = 0; i < guaranteedPokeballs.Length; i++)
        {
            if (guaranteedPokeballs[i] != null)
            {
                count++;
            }
        }

        return count;
    }
}