using System;

[Serializable]
public class EnemyMonDecisionSnapshot
{
    public string speciesName;
    public string type;
    public int level;
    public int currentHP;
    public int maxHP;
    public int attack;
    public int defense;
    public int speed;
    public EnemyMoveDecisionSnapshot[] moves;
}