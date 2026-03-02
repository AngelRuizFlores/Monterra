using UnityEngine;
using System;

public class PlayerMon : MonoBehaviour
{
    public MonSpecies species;
    public int lvl = 1;
    [NonSerialized] public MonInstance instance;

    public void SetStarter(MonSpecies newSpecies)
    {
        if (newSpecies == null)
        {
            Debug.LogError("SetStarter recibió species null");
            return;
        }

        if (species == newSpecies && instance != null) return;

        species = newSpecies;
        lvl = 1;

        instance = null;
        InitIfNeeded();
    }

    public void InitIfNeeded()
    {
        if (instance != null) return;

        if (species == null)
        {
            Debug.LogError("PlayerMon sin species");
            return;
        }

        instance = new MonInstance();
        instance.species = species;
        instance.level = lvl;
        instance.experience = 0;

        instance.currentHP = MonLevelSystem.GetMaxHP(instance);
        MonLevelSystem.InitMovesForCurrentLevel(instance);
    }
}