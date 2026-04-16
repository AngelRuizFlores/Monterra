using UnityEngine;

public class ClassicEnemyDecisionProvider : IEnemyDecisionProvider
{
    private readonly MonInstance enemyMon;

    public ClassicEnemyDecisionProvider(MonInstance enemyMon)
    {
        this.enemyMon = enemyMon;
    }

    public EnemyDecisionResult Decide(EnemyDecisionContext context)
    {
        int moveIndex = GetRandomValidMoveIndex();

        return new EnemyDecisionResult
        {
            action = EnemyDecisionAction.UseMove,
            index = moveIndex,
            reason = "classic_random",
            isFallback = false
        };
    }

    private int GetRandomValidMoveIndex()
    {
        if (enemyMon == null || enemyMon.moves == null || enemyMon.moves.Count == 0)
            return -1;

        for (int i = 0; i < 10; i++)
        {
            int index = Random.Range(0, enemyMon.moves.Count);
            if (enemyMon.moves[index] != null)
                return index;
        }

        for (int i = 0; i < enemyMon.moves.Count; i++)
        {
            if (enemyMon.moves[i] != null)
                return i;
        }

        return -1;
    }
}