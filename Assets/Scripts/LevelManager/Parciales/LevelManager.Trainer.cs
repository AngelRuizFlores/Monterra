using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class LevelManager
{
    private IEnumerator BeginTrainerBattleCoroutine(string trainerName)
    {
        SetPlayerCombatInputEnabled(false);

        yield return TryShowEnemyBark(
            "battle_start",
            $"{trainerName} challenges you.",
            "The trainer is starting a battle against the player."
        );

        MonInstance enemyMon = GetCurrentEnemyMon();

        if (enemyMon == null || enemyMon.species == null)
        {
            EndBattle();
            yield break;
        }

        battleUI?.SetText($"{trainerName} sends out {enemyMon.species.monName}.");

        yield return new WaitForSecondsRealtime(1f);

        state = BattleState.PlayerTurn;

        battleUI?.SetText($"What will {playerMon.instance.species.monName} do?");

        SetPlayerCombatInputEnabled(true);

        StartCoroutine(PlayTrainerBattleCryDelayed(0.2f));
    }

    private IEnumerator HandleEnemyDefeatAfterPlayerAttack()
    {
        if (encounterType == EncounterType.Wild)
        {
            yield return WinWildAndExit();
            yield break;
        }

        yield return HandleTrainerEnemyDefeat();
    }

    private IEnumerator HandleTrainerEnemyDefeat()
    {
        MonInstance enemyMon = GetCurrentEnemyMon();

        if (enemyMon == null || enemyMon.species == null)
        {
            EndBattle();
            yield break;
        }

        battleUI?.SetText($"{enemyMon.species.monName} fainted.");

        yield return new WaitForSecondsRealtime(TurnDelay);

        int nextIndex = GetNextAliveTrainerMonIndex(currentTrainerEnemyIndex);

        if (nextIndex < 0)
        {
            yield return WinTrainerAndExit();
            yield break;
        }

        battleUI?.SetEnemySpriteVisible(false);

        currentTrainerEnemyIndex = nextIndex;

        MonInstance nextEnemy = GetCurrentEnemyMon();

        if (nextEnemy == null || nextEnemy.species == null)
        {
            EndBattle();
            yield break;
        }

        SetupEnemyHealth();
        RefreshEnemyUI();

        string trainerName = currentTrainer != null && currentTrainer.TrainerDefinition != null
            ? currentTrainer.TrainerDefinition.TrainerName
            : "Trainer";

        battleUI?.SetText($"{trainerName} sends out {nextEnemy.species.monName}.");

        yield return PlaySwitchBall(false);

        battleUI?.ShowEnemyMon(nextEnemy);
        battleUI?.SetEnemySpriteVisible(true);

        PlayEnemyBattleCry();

        yield return new WaitForSecondsRealtime(TurnDelay);

        runningBattleRoutine = null;

        EnterPlayerTurn();
    }

    private IEnumerator WinWildAndExit()
    {
        if (currentWild == null || currentWild.instance == null || playerMon == null || playerMon.instance == null)
        {
            EndBattle();
            yield break;
        }

        battleUI?.SetText($"{currentWild.instance.species.monName} fainted.");

        yield return new WaitForSecondsRealtime(TurnDelay);

        int phase = GetCurrentPhase();

        bool leveledUp = MonLevelSystem.AddExperience(
            playerMon.instance,
            MonLevelSystem.ExpSource.Wild,
            phase
        );

        battleUI?.ShowPlayerMon(playerMon);
        battleUI?.SetPlayerExp(playerMon.instance);

        if (leveledUp)
        {
            movesUI?.Refresh();
            battleUI?.SetText($"{playerMon.instance.species.monName} grew to Lv. {playerMon.instance.level}.");
            yield return new WaitForSecondsRealtime(TurnDelay);
        }

        DespawnCurrentWild();
        EndBattle();
    }

    private IEnumerator WinTrainerAndExit()
    {
        string trainerName = currentTrainer != null && currentTrainer.TrainerDefinition != null
            ? currentTrainer.TrainerDefinition.TrainerName
            : "Trainer";

        battleUI?.SetText($"You defeated {trainerName}.");

        yield return new WaitForSecondsRealtime(TurnDelay);

        AwardExperienceToWholePlayerTeam();

        if (currentTrainer != null)
        {
            currentTrainer.MarkAsDefeated();
        }

        bool reachedVictoryGoal = false;

        if (trainerBattleProgress != null && currentTrainer != null)
        {
            bool registered = trainerBattleProgress.TryRegisterVictory(currentTrainer);
            reachedVictoryGoal = trainerBattleProgress.HasNoLivingTrainers();

            Debug.Log(
                $"{nameof(LevelManager)}: trainer victory against '{currentTrainer.name}', " +
                $"registered={registered}, reachedVictoryGoal={reachedVictoryGoal}"
            );
        }
        else
        {
            Debug.LogWarning(
                $"{nameof(LevelManager)}: final victory could not be checked. " +
                $"trainerBattleProgress null={trainerBattleProgress == null}, currentTrainer null={currentTrainer == null}"
            );
        }

        if (reachedVictoryGoal)
        {
            Debug.Log($"{nameof(LevelManager)}: required victory count reached. OnWin will be invoked.");
            pendingPostBattleAction = PostBattleAction.Victory;
        }

        EndBattle();
    }

    private void AwardExperienceToWholePlayerTeam()
    {
        PlayerTeam team = GetPlayerTeam();

        if (team == null)
        {
            return;
        }

        List<MonInstance> ownedMons = team.GetOwnedMons();
        int phase = GetCurrentPhase();

        for (int i = 0; i < ownedMons.Count; i++)
        {
            MonInstance mon = ownedMons[i];

            if (mon == null || mon.species == null)
            {
                continue;
            }

            MonLevelSystem.AddExperience(mon, MonLevelSystem.ExpSource.PlayerKill, phase);
            team.RefreshTeamUI();
        }

        if (playerMon != null && playerMon.instance != null)
        {
            battleUI?.ShowPlayerMon(playerMon);
            battleUI?.SetPlayerExp(playerMon.instance);
        }

        movesUI?.Refresh();
    }

    private IEnumerator FinalLoseAndExit()
    {
        if (battleEnding)
        {
            yield break;
        }

        state = BattleState.Busy;

        SetPlayerCombatInputEnabled(false);

        pendingPostBattleAction = PostBattleAction.GameOver;

        EndBattle();
    }

    private int GetNextAliveTrainerMonIndex(int startAfterIndex)
    {
        if (currentTrainerRoster == null || currentTrainerRoster.Count == 0)
        {
            return -1;
        }

        int start = Mathf.Max(startAfterIndex + 1, 0);

        for (int i = start; i < currentTrainerRoster.Count; i++)
        {
            if (IsMonAlive(currentTrainerRoster[i]))
            {
                return i;
            }
        }

        return -1;
    }
}