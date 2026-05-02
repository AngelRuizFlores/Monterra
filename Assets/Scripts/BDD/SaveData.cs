using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SaveData
{
    public Vector3 playerPosition;
    public int unlockedSlots;
    public List<MonSaveData> team = new List<MonSaveData>();
    public List<string> activePokeballIds = new List<string>();
    public List<string> collectedPokeballIds = new List<string>();

    public bool hasStormState;
    public int stormPhase;
    public Vector2 stormCenter;
    public float stormRadius;

    public List<string> defeatedTrainerIds = new List<string>();
    public List<string> removedTrainerIds = new List<string>();
}

[Serializable]
public class MonSaveData
{
    public string speciesId;
    public int level;
    public int currentHP;
    public int experience;
}