using System.Collections;
using UnityEngine;

public partial class LevelManager
{
    public void StartBattle()
    {
        if (!CanStartWildBattle(out currentWild))
        {
            return;
        }

        PlayerTeam team = GetPlayerTeam();

        if (team == null || !team.EnsureValidActiveMon())
        {
            Debug.LogWarning($"{nameof(LevelManager)}: no living mon is available to start the battle.");
            onPlayerPartyDefeated?.Invoke();
            return;
        }

        ResetBattleSession();

        battleUI?.SetText(string.Empty);

        encounterType = EncounterType.Wild;

        EnsureInstances(currentWild);
        currentWild.NotifyBattleStarted();

        ApplyBattleBackground();
        ShowBattleUI();
        SetupEnemyHealth();
        SetupPlayerHealth();
        SetupMovesUI();

        if (switchPopupUI != null)
        {
            switchPopupUI.HideImmediate();
        }

        Time.timeScale = 0f;
        state = BattleState.PlayerTurn;

        battleUI?.SetText($"What will {playerMon.instance.species.monName} do?");

        StartCoroutine(PlayWildBattleCryDelayed(0.2f));

        SetPlayerCombatInputEnabled(true);
    }

    public void StartTrainerBattle()
    {
        if (!CanStartTrainerBattle(out TrainerBattleTrigger trainer))
        {
            return;
        }

        PlayerTeam team = GetPlayerTeam();

        if (team == null || !team.EnsureValidActiveMon())
        {
            Debug.LogWarning($"{nameof(LevelManager)}: no living mon is available to start the trainer battle.");
            onPlayerPartyDefeated?.Invoke();
            return;
        }

        ResetBattleSession();

        battleUI?.SetText(string.Empty);

        encounterType = EncounterType.Trainer;
        currentTrainer = trainer;

        currentTrainerRoster.Clear();
        currentTrainerRoster.AddRange(TrainerMonFactory.CreateRoster(trainer.TrainerDefinition));

        currentTrainerEnemyIndex = GetNextAliveTrainerMonIndex(-1);

        if (currentTrainerEnemyIndex < 0)
        {
            Debug.LogError($"{nameof(LevelManager)}: trainer '{trainer.name}' has no valid mons for battle.", trainer);
            CleanupEncounterReferences();
            return;
        }

        playerMon?.InitIfNeeded();

        ApplyBattleBackground();
        ShowBattleUI();
        SetupEnemyHealth();
        SetupPlayerHealth();
        SetupMovesUI();

        if (switchPopupUI != null)
        {
            switchPopupUI.HideImmediate();
        }

        Time.timeScale = 0f;
        state = BattleState.Busy;

        string trainerName = currentTrainer.TrainerDefinition != null
            ? currentTrainer.TrainerDefinition.TrainerName
            : "Trainer";

        Sprite trainerSprite = currentTrainer.TrainerDefinition != null
            ? currentTrainer.TrainerDefinition.TrainerSprite
            : null;

        TryShowTrainerIntro(trainerName, trainerSprite);

        StartCoroutine(BeginTrainerBattleCoroutine(trainerName));
    }

    public void EndBattle()
    {
        if (runningBattleRoutine != null)
        {
            StopCoroutine(runningBattleRoutine);
            runningBattleRoutine = null;
        }

        StartCoroutine(EndBattleWithFade());
    }

    private IEnumerator EndBattleWithFade()
    {
        if (battleEnding)
        {
            yield break;
        }

        battleEnding = true;
        switchResolutionInProgress = false;

        if (FadeController.Instance != null)
        {
            yield return FadeController.Instance.FadeOut();
        }

        if (switchPopupUI != null)
        {
            switchPopupUI.HideImmediate();
        }

        SetPlayerCombatInputEnabled(false);

        Time.timeScale = 1f;

        music?.StartWorldMusic();

        if (battleCanvas != null)
        {
            battleCanvas.SetActive(false);
        }

        CleanupEncounterReferences();

        state = BattleState.Inactive;
        encounterType = EncounterType.None;

        BattleInteractionLock.SetBlocked(false);    

        if (FadeController.Instance != null)
        {
            FadeController.Instance.StartFadeIn();
        }

        PostBattleAction action = pendingPostBattleAction;
        pendingPostBattleAction = PostBattleAction.None;

        switch (action)
        {
            case PostBattleAction.GameOver:
                SaveGameManager.DeleteSave();
                GameStartMode.LoadGame = false;
                onPlayerPartyDefeated?.Invoke();
                break;

            case PostBattleAction.Victory:
                SaveGameManager.DeleteSave();
                GameStartMode.LoadGame = false;
                onWin?.Invoke();
                break;
        }
    }

    private bool CanStartWildBattle(out WildMon wild)
    {
        wild = null;

        if (!ValidateBattleDependencies())
        {
            return false;
        }

        if (playerTouching == null || playerMon == null)
        {
            return false;
        }

        wild = playerTouching.lastWildMon;

        if (wild == null)
        {
            return false;
        }

        return true;
    }

    private bool CanStartTrainerBattle(out TrainerBattleTrigger trainer)
    {
        trainer = null;

        if (!ValidateBattleDependencies())
        {
            return false;
        }

        if (playerTouching == null || playerMon == null)
        {
            return false;
        }

        trainer = playerTouching.lastTrainer;

        if (trainer == null)
        {
            return false;
        }

        if (!trainer.CanStartBattle(out string error))
        {
            Debug.LogWarning($"{nameof(LevelManager)}: trainer battle cannot start for '{trainer.name}': {error}", trainer);
            return false;
        }

        return true;
    }

    private bool ValidateBattleDependencies()
    {
        if (battleCanvas == null || battleUI == null || movesUI == null)
        {
            Debug.LogError($"{nameof(LevelManager)}: missing battle UI references.");
            return false;
        }

        if (enemyHealth == null || playerHealth == null)
        {
            Debug.LogError($"{nameof(LevelManager)}: missing health bar references.");
            return false;
        }

        if (GetPlayerTeam() == null)
        {
            Debug.LogError($"{nameof(LevelManager)}: {nameof(PlayerTeam)} was not found on the player.");
            return false;
        }

        return true;
    }

    private void ResetBattleSession()
    {
        battleEnding = false;
        switchResolutionInProgress = false;
        pendingPostBattleAction = PostBattleAction.None;

        if (runningBattleRoutine != null)
        {
            StopCoroutine(runningBattleRoutine);
            runningBattleRoutine = null;
        }
    }

    private void EnsureInstances(WildMon wild)
    {
        if (wild != null && wild.instance == null)
        {
            wild.Init();
        }

        playerMon?.InitIfNeeded();
    }

    private void ShowBattleUI()
    {
        if (battleCanvas == null || battleUI == null || playerMon == null || playerMon.instance == null)
        {
            return;
        }

        battleUI.SetText(string.Empty);

        battleCanvas.SetActive(true);

        if (encounterType == EncounterType.Wild && currentWild != null && currentWild.instance != null)
        {
            battleUI.ShowWildMon(currentWild);
            battleUI.SetText($"{currentWild.instance.species.monName} appeared.");
        }
        else
        {
            battleUI.ShowEnemyMon(GetCurrentEnemyMon());
            battleUI.SetText(string.Empty);
        }

        battleUI.ShowPlayerMon(playerMon);
        battleUI.SetPlayerExp(playerMon.instance);
    }

    private void SetupMovesUI()
    {
        if (movesUI == null || playerMon == null)
        {
            return;
        }

        movesUI.Setup(playerMon, this);
        movesUI.Refresh();
        movesUI.SetInteractable(true);

        if (battleUI != null)
        {
            battleUI.SetSwitchButtonInteractable(true);
        }
    }

    private void SetupEnemyHealth()
    {
        MonInstance enemyMon = GetCurrentEnemyMon();

        if (enemyMon == null)
        {
            return;
        }

        enemyHealth.Init(enemyMon);
        RefreshEnemyUI();
    }

    private void SetupPlayerHealth()
    {
        if (playerMon == null || playerMon.instance == null)
        {
            return;
        }

        playerHealth.Init(playerMon.instance);
        RefreshPlayerUI();
    }

    private void CleanupEncounterReferences()
    {
        if (playerTouching != null)
        {
            playerTouching.lastWildMon = null;
            playerTouching.lastTrainer = null;
        }

        ClearCaptureBallVfx();

        currentWild = null;
        currentTrainer = null;

        currentTrainerRoster.Clear();

        currentTrainerEnemyIndex = -1;

        battleUI?.SetEnemySpriteVisible(true);
        battleUI?.ClearEnemyMon();
        battleUI?.SetText(string.Empty);
    }
}