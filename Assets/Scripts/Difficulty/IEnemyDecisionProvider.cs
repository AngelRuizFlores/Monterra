using UnityEngine;
using System;

public interface IEnemyDecisionProvider
{
    EnemyDecisionResult Decide(EnemyDecisionContext context);
}
