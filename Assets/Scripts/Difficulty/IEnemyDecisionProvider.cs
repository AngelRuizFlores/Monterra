public interface IEnemyDecisionProvider
{
    EnemyDecisionResult Decide(EnemyDecisionContext context);
}