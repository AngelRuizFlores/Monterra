using System;

[Serializable]
public class EnemyDecisionResult
{
    public EnemyDecisionAction action;
    public int index;
    public string reason;
    public bool isFallback;
}