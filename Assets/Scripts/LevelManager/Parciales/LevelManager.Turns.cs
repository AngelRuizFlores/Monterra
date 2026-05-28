using System.Collections;
using UnityEngine;

public partial class LevelManager
{
    public void UsePlayerMove(MoveData move)
    {
        if (!IsBattleActive())
        {
            return;
        }

        if (state != BattleState.PlayerTurn)
        {
            return;
        }

        if (switchPopupUI != null && switchPopupUI.IsOpen)
        {
            return;
        }

        if (move == null)
        {
            return;
        }

        MonInstance player = playerMon != null ? playerMon.instance : null;
        MonInstance enemy = GetCurrentEnemyMon();

        if (player == null || enemy == null)
        {
            return;
        }

        if (!IsMonAlive(player) || !IsMonAlive(enemy))
        {
            return;
        }

        if (runningBattleRoutine != null)
        {
            StopCoroutine(runningBattleRoutine);
            runningBattleRoutine = null;
        }

        runningBattleRoutine = StartCoroutine(BattleTurnCoroutine(move));
    }

    private IEnumerator BattleTurnCoroutine(MoveData playerMove)
    {
        state = BattleState.Busy;

        SetPlayerCombatInputEnabled(false);

        yield return new WaitForSecondsRealtime(PlayerAttackStartDelay);

        MonInstance player = playerMon != null ? playerMon.instance : null;
        MonInstance enemy = GetCurrentEnemyMon();

        if (player == null || enemy == null)
        {
            EndBattle();
            yield break;
        }

        bool playerFirst = MonLevelSystem.GetSpeed(player) >= MonLevelSystem.GetSpeed(enemy);

        if (playerFirst)
        {
            yield return PlayerAttack(playerMove);

            if (battleEnding)
            {
                yield break;
            }

            if (IsEnemyDead())
            {
                yield return HandleEnemyDefeatAfterPlayerAttack();
                yield break;
            }

            yield return ResolveEnemyTurnCoroutine();

            if (battleEnding)
            {
                yield break;
            }

            if (IsPlayerDead())
            {
                yield return HandlePlayerDefeatAfterEnemyAction();
                yield break;
            }
        }
        else
        {
            yield return ResolveEnemyTurnCoroutine();

            if (battleEnding)
            {
                yield break;
            }

            if (IsPlayerDead())
            {
                yield return HandlePlayerDefeatAfterEnemyAction();
                yield break;
            }

            yield return PlayerAttack(playerMove);

            if (battleEnding)
            {
                yield break;
            }

            if (IsEnemyDead())
            {
                yield return HandleEnemyDefeatAfterPlayerAttack();
                yield break;
            }
        }

        runningBattleRoutine = null;

        EnterPlayerTurn();
    }

    private IEnumerator PlayerAttack(MoveData move)
    {
        MonInstance attacker = playerMon != null ? playerMon.instance : null;
        MonInstance defender = GetCurrentEnemyMon();

        if (move == null || attacker == null || defender == null)
        {
            yield break;
        }

        battleUI?.SetText($"{attacker.species.monName} used {move.moveName}.");

        Vector3 startPosition = battleUI != null ? battleUI.GetPlayerAttackOrigin() : Vector3.zero;
        Vector3 targetPosition = battleUI != null ? battleUI.GetEnemyHitPoint() : Vector3.zero;

        if (move.projectilePrefab != null)
        {
            yield return PlayAttackProjectile(move, startPosition, targetPosition);
        }

        PlayMoveSound(move);

        float multiplier = TypeChart.GetMultiplier(move.type, defender.species.type);
        int damage = ComputeDamage(attacker, defender, move);

        if (enemyHealth != null)
        {
            yield return enemyHealth.HurtAnimated(damage);
        }
        else
        {
            defender.currentHP = Mathf.Max(0, defender.currentHP - damage);
        }

        RefreshEnemyUI();

        yield return new WaitForSecondsRealtime(0.35f);

        string effectText = TypeChart.GetEffectText(multiplier);

        if (!string.IsNullOrEmpty(effectText))
        {
            battleUI?.SetText(effectText);
            yield return new WaitForSecondsRealtime(0.75f);
        }
    }

    private IEnumerator EnemyAttack(MoveData enemyMove)
    {
        MonInstance attacker = GetCurrentEnemyMon();
        MonInstance defender = playerMon != null ? playerMon.instance : null;

        if (attacker == null || defender == null)
        {
            yield break;
        }

        string moveName = enemyMove != null ? enemyMove.moveName : "Punch";

        battleUI?.SetText($"{attacker.species.monName} used {moveName}.");

        Vector3 startPosition = battleUI != null ? battleUI.GetEnemyAttackOrigin() : Vector3.zero;
        Vector3 targetPosition = battleUI != null ? battleUI.GetPlayerHitPoint() : Vector3.zero;

        if (enemyMove != null && enemyMove.projectilePrefab != null)
        {
            yield return PlayAttackProjectile(enemyMove, startPosition, targetPosition);
        }

        PlayMoveSound(enemyMove);

        float multiplier = 1f;
        int damage = 5;

        if (enemyMove != null)
        {
            multiplier = TypeChart.GetMultiplier(enemyMove.type, defender.species.type);
            damage = ComputeDamage(attacker, defender, enemyMove);
        }

        if (playerHealth != null)
        {
            yield return playerHealth.HurtAnimated(damage);
        }
        else
        {
            defender.currentHP = Mathf.Max(0, defender.currentHP - damage);
        }

        RefreshPlayerUI();

        yield return new WaitForSecondsRealtime(0.35f);

        string effectText = TypeChart.GetEffectText(multiplier);

        if (!string.IsNullOrEmpty(effectText))
        {
            battleUI?.SetText(effectText);
            yield return new WaitForSecondsRealtime(0.75f);
        }
    }

    private void EnterPlayerTurn()
    {
        if (!IsBattleActive())
        {
            return;
        }

        if (switchPopupUI != null && switchPopupUI.IsOpen)
        {
            return;
        }

        if (IsPlayerDead())
        {
            return;
        }

        state = BattleState.PlayerTurn;

        battleUI?.SetText($"What will {playerMon.instance.species.monName} do?");

        SetPlayerCombatInputEnabled(true);
    }

    private void SetPlayerCombatInputEnabled(bool enabled)
    {
        BattleInteractionLock.SetBlocked(!enabled);

        if (movesUI != null)
        {
            movesUI.SetInteractable(enabled);
        }

        if (battleUI != null)
        {
            battleUI.SetSwitchButtonInteractable(enabled);
        }
    }
}