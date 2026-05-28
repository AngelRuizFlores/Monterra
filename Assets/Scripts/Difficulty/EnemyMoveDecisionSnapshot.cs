using System;

[Serializable]
public class EnemyMoveDecisionSnapshot
{
    public int index;
    public string moveName;
    public string type;
    public int power;
    public float expectedMultiplierVsTarget;
}