using UnityEngine;
using System;
using System.Collections.Generic;


[Serializable]
public class MonInstance
{
    public MonSpecies species;
    public int level;
    public int currentHP;
    public int experience;
    public List<MoveData> moves = new List<MoveData>();

    // --- Battle Royale anti-farm (diminishing returns) ---
    public int wildStreak;          // cuántos salvajes seguidos has matado recientemente
    public float lastWildExpTime;   // última vez que ganaste exp por salvaje

}
