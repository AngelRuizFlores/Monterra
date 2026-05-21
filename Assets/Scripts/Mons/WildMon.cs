using UnityEngine;
using System;

public class WildMon : MonoBehaviour
{
    [Header("Base data (ScriptableObject)")]
    public MonSpecies species;

    [Header("Spawn rules")]
    public int minLevel = 1;
    public int maxLevel = 3;

    [NonSerialized] public MonInstance instance;

    private CreatureGenerator sourceGenerator;

    private void OnEnable()
    {
        Init();
    }

    public void Init()
    {
        if (species == null)
        {
            Debug.LogError($"{name} no tiene MonSpecies asignado.", this);
            return;
        }

        int lvl = UnityEngine.Random.Range(minLevel, maxLevel + 1);

        instance = new MonInstance
        {
            species = species,
            level = lvl,
            experience = 0
        };

        instance.currentHP = MonLevelSystem.GetMaxHP(instance);
        MonLevelSystem.InitMovesForCurrentLevel(instance);
    }

    public void SetSourceGenerator(CreatureGenerator generator)
    {
        sourceGenerator = generator;
    }

    public void NotifyBattleStarted()
    {
        if (sourceGenerator != null)
            sourceGenerator.HandleWildBattleStarted(this);
    }
}