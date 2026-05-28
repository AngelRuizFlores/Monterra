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

    public int wildStreak;
    public float lastWildExpTime;
}