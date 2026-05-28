public class HardApiEnemyDecisionProvider : IEnemyDecisionProvider
{
    private readonly MonInstance enemyMon;

    public HardApiEnemyDecisionProvider(MonInstance enemyMon)
    {
        this.enemyMon = enemyMon;
    }

    public EnemyDecisionResult Decide(EnemyDecisionContext context)
    {
        ClassicEnemyDecisionProvider fallbackProvider = new ClassicEnemyDecisionProvider(enemyMon);
        EnemyDecisionResult fallbackResult = fallbackProvider.Decide(context);

        if (fallbackResult == null)
        {
            return new EnemyDecisionResult
            {
                action = EnemyDecisionAction.UseMove,
                index = -1,
                reason = "hardapi_placeholder_null_fallback",
                isFallback = true
            };
        }

        fallbackResult.reason = "hardapi_placeholder_classic_fallback";
        fallbackResult.isFallback = true;

        return fallbackResult;
    }
}