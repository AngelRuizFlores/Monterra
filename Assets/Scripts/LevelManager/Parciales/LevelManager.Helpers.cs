using UnityEngine;

public partial class LevelManager
{
    private bool IsBattleActive()
    {
        return !battleEnding && encounterType != EncounterType.None && state != BattleState.Inactive;
    }

    private bool IsEnemyDead()
    {
        MonInstance enemy = GetCurrentEnemyMon();

        return enemy == null || enemy.currentHP <= 0;
    }

    private bool IsPlayerDead()
    {
        return playerMon == null || playerMon.instance == null || playerMon.instance.currentHP <= 0;
    }

    private bool IsEnemyAbleToAct()
    {
        MonInstance enemy = GetCurrentEnemyMon();

        return enemy != null && enemy.currentHP > 0;
    }

    private MonInstance GetCurrentEnemyMon()
    {
        switch (encounterType)
        {
            case EncounterType.Wild:
                return currentWild != null ? currentWild.instance : null;

            case EncounterType.Trainer:
                if (currentTrainerRoster == null)
                {
                    return null;
                }

                if (currentTrainerEnemyIndex < 0 || currentTrainerEnemyIndex >= currentTrainerRoster.Count)
                {
                    return null;
                }

                return currentTrainerRoster[currentTrainerEnemyIndex];

            default:
                return null;
        }
    }

    private static int ComputeDamage(MonInstance attacker, MonInstance defender, MoveData move)
    {
        int attack = MonLevelSystem.GetAttack(attacker);
        int defense = MonLevelSystem.GetDefense(defender);

        int baseDamage = move.power + attack / 4 - defense / 6;
        baseDamage = Mathf.Max(1, baseDamage);

        float multiplier = TypeChart.GetMultiplier(move.type, defender.species.type);
        int finalDamage = Mathf.RoundToInt(baseDamage * multiplier);

        return Mathf.Max(1, finalDamage);
    }

    private PlayerTeam GetPlayerTeam()
    {
        return playerMon != null ? playerMon.GetComponent<PlayerTeam>() : null;
    }

    private static bool IsMonAlive(MonInstance mon)
    {
        return mon != null && mon.currentHP > 0;
    }

    private int GetCurrentPhase()
    {
        return 3;
    }
}