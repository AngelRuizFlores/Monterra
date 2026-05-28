using System;

[Serializable]
public class EnemyDecisionContext
{
    public string trainerId;
    public string trainerName;
    public int turnNumber;
    public bool canSwitch;

    public EnemyMonDecisionSnapshot enemyActive;
    public EnemyMonDecisionSnapshot playerActive;
    public EnemyMonDecisionSnapshot[] enemyBench;
}