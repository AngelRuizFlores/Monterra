using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

public class LevelManager : MonoBehaviour
{
    private const float TurnDelay = 1f;
    private const string MonCatchSoundName = "MonCatch";
    private const string CatchAttemptSoundName = "CatchAttempt";
    private const string CatchFailSoundName = "CatchFail";

    [Header("Battle UI")]
    [SerializeField] private GameObject battleCanvas;
    [SerializeField] private BattleUI battleUI;
    [SerializeField] private MovesUI movesUI;
    [SerializeField] private BattleMonSwitchPopupUI switchPopupUI;

    [Header("Player")]
    [SerializeField] private TouchingBehaviour playerTouching;
    [SerializeField] private PlayerMon playerMon;
    [SerializeField] private TrainerBattleProgress trainerBattleProgress;

    [Header("Health")]
    [SerializeField] private HealthBehaviour enemyHealth;
    [SerializeField] private HealthBehaviour playerHealth;

    [Header("Music")]
    [SerializeField] private MusicGame music;

    [Header("Sound")]
    [SerializeField] private SoundManager soundManager;

    [Header("Events")]
    [SerializeField] private UnityEvent onPlayerPartyDefeated;
    [SerializeField] private UnityEvent onWin;
    [SerializeField] private EnemyDecisionMode enemyDecisionMode = EnemyDecisionMode.Classic;

    [Header("Enemy AI Debug")]
    [SerializeField] private bool logEnemyDecisionContext = false;

    [Header("Enemy AI API")]
    [SerializeField] private EnemyApiClient enemyApiClient;

    [Header("Battle Background")]
    [SerializeField] private BattleBackgroundSelector battleBackgroundSelector;

    [Header("Enemy Bark API")]
    [SerializeField] private EnemyBarkApiClient enemyBarkApiClient;
    [SerializeField] private bool enableApiBarks = true;

    private WildMon currentWild;
    private TrainerBattleTrigger currentTrainer;
    private readonly List<MonInstance> currentTrainerRoster = new();
    private int currentTrainerEnemyIndex = -1;

    private Coroutine runningBattleRoutine;
    private bool battleEnding;
    private bool switchResolutionInProgress;
    private PostBattleAction pendingPostBattleAction = PostBattleAction.None;

    private BattleState state = BattleState.Inactive;
    private EncounterType encounterType = EncounterType.None;

    private enum BattleState
    {
        Inactive,
        PlayerTurn,
        Busy,
        WaitingForForcedSwitch
    }

    private enum EncounterType
    {
        None,
        Wild,
        Trainer
    }

    private enum PostBattleAction
    {
        None,
        GameOver,
        Victory
    }

    private enum EnemyTurnResolution
    {
        None,
        UsedMove,
        SwitchedMon
    }

    private void Awake()
    {
        if (battleCanvas != null)
            battleCanvas.SetActive(false);

        playerMon?.InitIfNeeded();

        if (battleUI != null)
            battleUI.BindSwitchAction(TryOpenManualSwitchPopup);

        if (switchPopupUI != null)
            switchPopupUI.HideImmediate();

        if (soundManager == null)
            soundManager = SoundManager.Instance;

        state = BattleState.Inactive;
        encounterType = EncounterType.None;
    }

    public void StartBattle()
    {
        if (!CanStartWildBattle(out currentWild))
            return;

        PlayerTeam team = GetPlayerTeam();
        if (team == null || !team.EnsureValidActiveMon())
        {
            Debug.LogWarning($"{nameof(LevelManager)}: no living mon is available to start the battle.");
            onPlayerPartyDefeated?.Invoke();
            return;
        }

        ResetBattleSession();
        encounterType = EncounterType.Wild;

        EnsureInstances(currentWild);
        ApplyBattleBackground();
        ShowBattleUI();
        SetupEnemyHealth();
        SetupPlayerHealth();
        SetupMovesUI();

        if (switchPopupUI != null)
            switchPopupUI.HideImmediate();

        Time.timeScale = 0f;
        state = BattleState.PlayerTurn;
        battleUI?.SetText($"What will {playerMon.instance.species.monName} do?");

        StartCoroutine(PlayWildBattleCryDelayed(0.2f));
        SetPlayerCombatInputEnabled(true);
    }

    public void StartTrainerBattle()
    {
        if (!CanStartTrainerBattle(out TrainerBattleTrigger trainer))
            return;

        PlayerTeam team = GetPlayerTeam();
        if (team == null || !team.EnsureValidActiveMon())
        {
            Debug.LogWarning($"{nameof(LevelManager)}: no living mon is available to start the trainer battle.");
            onPlayerPartyDefeated?.Invoke();
            return;
        }

        ResetBattleSession();
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
            switchPopupUI.HideImmediate();

        Time.timeScale = 0f;
        state = BattleState.Busy;

        string trainerName = currentTrainer.TrainerDefinition != null
            ? currentTrainer.TrainerDefinition.TrainerName
            : "Trainer";

        TryShowTrainerIntro(trainerName, currentTrainer.TrainerDefinition != null ? currentTrainer.TrainerDefinition.TrainerSprite : null);

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

    public void UsePlayerMove(MoveData move)
    {
        if (!IsBattleActive())
            return;

        if (state != BattleState.PlayerTurn)
            return;

        if (switchPopupUI != null && switchPopupUI.IsOpen)
            return;

        if (move == null)
            return;

        MonInstance player = playerMon != null ? playerMon.instance : null;
        MonInstance enemy = GetCurrentEnemyMon();

        if (player == null || enemy == null)
            return;

        if (!IsMonAlive(player) || !IsMonAlive(enemy))
            return;

        if (runningBattleRoutine != null)
        {
            StopCoroutine(runningBattleRoutine);
            runningBattleRoutine = null;
        }

        runningBattleRoutine = StartCoroutine(BattleTurnCoroutine(move));
    }

    public void TryOpenManualSwitchPopup()
    {
        if (!IsBattleActive())
            return;

        if (state != BattleState.PlayerTurn)
            return;

        if (switchPopupUI == null)
        {
            Debug.LogError($"{nameof(LevelManager)}: switch popup reference is missing.");
            return;
        }

        if (switchPopupUI.IsOpen || switchResolutionInProgress)
            return;

        List<MonInstance> candidates = GetSwitchCandidates(excludeCurrentActive: true);
        if (candidates.Count <= 0)
        {
            battleUI?.SetText("No other living mons are available.");
            return;
        }

        OpenSwitchPopup(candidates, forced: false);
    }

    public void TryCapture()
    {
        if (encounterType == EncounterType.Trainer)
        {
            battleUI?.SetText("You cannot catch a trainer's mons.");
            return;
        }

        if (!IsBattleActive())
            return;

        if (state != BattleState.PlayerTurn)
            return;

        if (currentWild == null || currentWild.instance == null || playerMon == null || playerMon.instance == null)
            return;

        if (!IsMonAlive(currentWild.instance))
            return;

        PlayerTeam team = GetPlayerTeam();
        if (team == null)
            return;

        int freeSlot = team.GetNextFreeSlotIndex();
        if (freeSlot < 0)
        {
            battleUI?.SetText("There is no room left in your team.");
            return;
        }

        if (runningBattleRoutine != null)
        {
            StopCoroutine(runningBattleRoutine);
            runningBattleRoutine = null;
        }

        runningBattleRoutine = StartCoroutine(TryCaptureCoroutine());
    }

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

    private IEnumerator EndBattleWithFade()
    {
        if (battleEnding)
            yield break;

        battleEnding = true;
        switchResolutionInProgress = false;

        if (FadeController.Instance != null)
            yield return FadeController.Instance.FadeOut();

        if (switchPopupUI != null)
            switchPopupUI.HideImmediate();

        SetPlayerCombatInputEnabled(false);

        Time.timeScale = 1f;
        music?.StartWorldMusic();

        if (battleCanvas != null)
            battleCanvas.SetActive(false);

        CleanupEncounterReferences();
        state = BattleState.Inactive;
        encounterType = EncounterType.None;

        if (FadeController.Instance != null)
            FadeController.Instance.StartFadeIn();

        PostBattleAction action = pendingPostBattleAction;
        pendingPostBattleAction = PostBattleAction.None;

        switch (action)
        {
            case PostBattleAction.GameOver:
                onPlayerPartyDefeated?.Invoke();
                break;

            case PostBattleAction.Victory:
                onWin?.Invoke();
                break;
        }
    }

    private IEnumerator PlayWildBattleCryDelayed(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        PlayWildBattleCry();
    }

    private IEnumerator PlayTrainerBattleCryDelayed(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        PlayEnemyBattleCry();
    }

    private IEnumerator TryCaptureCoroutine()
    {
        state = BattleState.Busy;
        SetPlayerCombatInputEnabled(false);

        if (currentWild == null || currentWild.instance == null || playerMon == null || playerMon.instance == null)
        {
            runningBattleRoutine = null;
            EnterPlayerTurn();
            yield break;
        }

        battleUI?.SetText($"Trying to catch {currentWild.instance.species.monName}.");
        PlaySound(CatchAttemptSoundName, false);

        yield return new WaitForSecondsRealtime(1f);

        bool captured = CatchSystem.TryCatch(
            playerMon.instance,
            currentWild.instance,
            out float chance,
            out float roll
        );

        Debug.Log($"Catch roll={roll:F2} chance={chance:F2}");

        if (captured)
        {
            PlayerTeam team = GetPlayerTeam();
            MonInstance newMon = MonLevelSystem.Clone(currentWild.instance);

            if (team == null || newMon == null)
            {
                runningBattleRoutine = null;
                EnterPlayerTurn();
                yield break;
            }

            if (!team.TryAddToNextFreeSlot(newMon))
            {
                battleUI?.SetText("There is no room left in your team.");
                runningBattleRoutine = null;
                EnterPlayerTurn();
                yield break;
            }

            int phase = GetCurrentPhase();
            bool leveledUp = MonLevelSystem.AddExperience(
                playerMon.instance,
                MonLevelSystem.ExpSource.Capture,
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

            yield return CaptureSuccessCoroutine(newMon);
            yield break;
        }

        yield return CaptureFailCoroutine();
    }

    private IEnumerator CaptureSuccessCoroutine(MonInstance capturedMon)
    {
        battleUI?.SetText($"{capturedMon.species.monName} was caught.");
        PlaySound(MonCatchSoundName, false);

        yield return new WaitForSecondsRealtime(1.2f);

        DespawnCurrentWild();
        EndBattle();
    }

    private IEnumerator CaptureFailCoroutine()
    {
        battleUI?.SetText($"{currentWild.instance.species.monName} broke free.");
        PlaySound(CatchFailSoundName, false);

        yield return new WaitForSecondsRealtime(0.8f);

        if (IsEnemyAbleToAct())
        {
            yield return ResolveEnemyTurnCoroutine();
            if (battleEnding)
                yield break;

            if (IsPlayerDead())
            {
                yield return HandlePlayerDefeatAfterEnemyAction();
                yield break;
            }
        }

        runningBattleRoutine = null;
        EnterPlayerTurn();
    }

    private bool CanStartWildBattle(out WildMon wild)
    {
        wild = null;

        if (!ValidateBattleDependencies())
            return false;

        if (playerTouching == null || playerMon == null)
            return false;

        wild = playerTouching.lastWildMon;
        if (wild == null)
            return false;

        return true;
    }

    private bool CanStartTrainerBattle(out TrainerBattleTrigger trainer)
    {
        trainer = null;

        if (!ValidateBattleDependencies())
            return false;

        if (playerTouching == null || playerMon == null)
            return false;

        trainer = playerTouching.lastTrainer;
        if (trainer == null)
            return false;

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
            wild.Init();

        playerMon?.InitIfNeeded();
    }

    private void ShowBattleUI()
    {
        if (battleCanvas == null || battleUI == null || playerMon == null || playerMon.instance == null)
            return;

        battleCanvas.SetActive(true);

        if (encounterType == EncounterType.Wild && currentWild != null && currentWild.instance != null)
        {
            battleUI.ShowWildMon(currentWild);
            battleUI.SetText($"{currentWild.instance.species.monName} appeared.");
        }
        else
        {
            battleUI.ShowEnemyMon(GetCurrentEnemyMon());
        }

        battleUI.ShowPlayerMon(playerMon);
        battleUI.SetPlayerExp(playerMon.instance);
    }

    private void SetupMovesUI()
    {
        if (movesUI == null || playerMon == null)
            return;

        movesUI.Setup(playerMon, this);
        movesUI.Refresh();
        movesUI.SetInteractable(true);

        if (battleUI != null)
            battleUI.SetSwitchButtonInteractable(true);
    }

    private void SetupEnemyHealth()
    {
        MonInstance enemyMon = GetCurrentEnemyMon();
        if (enemyMon == null)
            return;

        enemyHealth.Init(enemyMon);
        RefreshEnemyUI();
    }

    private void SetupPlayerHealth()
    {
        if (playerMon == null || playerMon.instance == null)
            return;

        playerHealth.Init(playerMon.instance);
        RefreshPlayerUI();
    }

    private IEnumerator BattleTurnCoroutine(MoveData playerMove)
    {
        state = BattleState.Busy;
        SetPlayerCombatInputEnabled(false);

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
                yield break;

            if (IsEnemyDead())
            {
                yield return HandleEnemyDefeatAfterPlayerAttack();
                yield break;
            }

            yield return ResolveEnemyTurnCoroutine();
            if (battleEnding)
                yield break;

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
                yield break;

            if (IsPlayerDead())
            {
                yield return HandlePlayerDefeatAfterEnemyAction();
                yield break;
            }

            yield return PlayerAttack(playerMove);
            if (battleEnding)
                yield break;

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
            yield break;

        battleUI?.SetText($"{attacker.species.monName} used {move.moveName}.");

        Vector3 startPos = battleUI != null ? battleUI.GetPlayerAttackOrigin() : Vector3.zero;
        Vector3 targetPos = battleUI != null ? battleUI.GetEnemyHitPoint() : Vector3.zero;

        if (move.projectilePrefab != null)
            yield return PlayAttackProjectile(move, startPos, targetPos);

        PlayMoveSound(move);

        float mult = TypeChart.GetMultiplier(move.type, defender.species.type);
        int dmg = ComputeDamage(attacker, defender, move);

        if (enemyHealth != null)
            yield return enemyHealth.HurtAnimated(dmg);
        else
            defender.currentHP = Mathf.Max(0, defender.currentHP - dmg);

        RefreshEnemyUI();

        yield return new WaitForSecondsRealtime(0.35f);

        string effectText = TypeChart.GetEffectText(mult);
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
            yield break;

        string moveName = enemyMove != null ? enemyMove.moveName : "Punch";

        battleUI?.SetText($"{attacker.species.monName} used {moveName}.");

        Vector3 startPos = battleUI != null ? battleUI.GetEnemyAttackOrigin() : Vector3.zero;
        Vector3 targetPos = battleUI != null ? battleUI.GetPlayerHitPoint() : Vector3.zero;

        if (enemyMove != null && enemyMove.projectilePrefab != null)
            yield return PlayAttackProjectile(enemyMove, startPos, targetPos);

        PlayMoveSound(enemyMove);

        float mult = 1f;
        int dmg = 5;

        if (enemyMove != null)
        {
            mult = TypeChart.GetMultiplier(enemyMove.type, defender.species.type);
            dmg = ComputeDamage(attacker, defender, enemyMove);
        }

        if (playerHealth != null)
            yield return playerHealth.HurtAnimated(dmg);
        else
            defender.currentHP = Mathf.Max(0, defender.currentHP - dmg);

        RefreshPlayerUI();

        yield return new WaitForSecondsRealtime(0.35f);

        string effectText = TypeChart.GetEffectText(mult);
        if (!string.IsNullOrEmpty(effectText))
        {
            battleUI?.SetText(effectText);
            yield return new WaitForSecondsRealtime(0.75f);
        }
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

        currentTrainerEnemyIndex = nextIndex;
        MonInstance nextEnemy = GetCurrentEnemyMon();

        SetupEnemyHealth();
        RefreshEnemyUI();
        battleUI?.ShowEnemyMon(nextEnemy);

        string trainerName = currentTrainer != null && currentTrainer.TrainerDefinition != null
            ? currentTrainer.TrainerDefinition.TrainerName
            : "Trainer";

        battleUI?.SetText($"{trainerName} sends out {nextEnemy.species.monName}.");
        PlayEnemyBattleCry();

        yield return new WaitForSecondsRealtime(TurnDelay);

        runningBattleRoutine = null;
        EnterPlayerTurn();
    }

    private IEnumerator HandlePlayerDefeatAfterEnemyAction()
    {
        runningBattleRoutine = null;

        if (!IsPlayerDead())
        {
            EnterPlayerTurn();
            yield break;
        }

        if (playerMon != null && playerMon.instance != null && playerMon.instance.species != null)
        {
            battleUI?.SetText($"{playerMon.instance.species.monName} fainted.");
            yield return new WaitForSecondsRealtime(TurnDelay);
        }

        List<MonInstance> replacements = GetSwitchCandidates(excludeCurrentActive: true);
        if (replacements.Count > 0)
        {
            OpenSwitchPopup(replacements, forced: true);
            yield break;
        }

        yield return FinalLoseAndExit();
    }

    private IEnumerator ManualSwitchTurnCoroutine(MonInstance selectedMon)
    {
        runningBattleRoutine = null;
        switchResolutionInProgress = true;
        state = BattleState.Busy;
        SetPlayerCombatInputEnabled(false);

        if (!TrySwitchActivePlayerMon(selectedMon))
        {
            switchResolutionInProgress = false;
            EnterPlayerTurn();
            yield break;
        }

        battleUI?.SetText($"Go, {playerMon.instance.species.monName}.");
        yield return new WaitForSecondsRealtime(TurnDelay);

        if (IsEnemyAbleToAct())
        {
            yield return ResolveEnemyTurnCoroutine();
            if (battleEnding)
            {
                switchResolutionInProgress = false;
                yield break;
            }

            if (IsPlayerDead())
            {
                switchResolutionInProgress = false;
                yield return HandlePlayerDefeatAfterEnemyAction();
                yield break;
            }
        }

        switchResolutionInProgress = false;
        EnterPlayerTurn();
    }

    private IEnumerator ForcedSwitchResolutionCoroutine(MonInstance selectedMon)
    {
        runningBattleRoutine = null;
        switchResolutionInProgress = true;
        state = BattleState.Busy;
        SetPlayerCombatInputEnabled(false);

        if (!TrySwitchActivePlayerMon(selectedMon))
        {
            switchResolutionInProgress = false;

            List<MonInstance> replacements = GetSwitchCandidates(excludeCurrentActive: true);
            if (replacements.Count > 0)
            {
                OpenSwitchPopup(replacements, forced: true);
                yield break;
            }

            yield return FinalLoseAndExit();
            yield break;
        }

        battleUI?.SetText($"{playerMon.instance.species.monName} enters the battle.");
        yield return new WaitForSecondsRealtime(TurnDelay);

        switchResolutionInProgress = false;
        EnterPlayerTurn();
    }

    private void OpenSwitchPopup(List<MonInstance> candidates, bool forced)
    {
        if (switchPopupUI == null)
        {
            Debug.LogError($"{nameof(LevelManager)}: no switch popup is configured.");
            return;
        }

        if (candidates == null || candidates.Count == 0)
        {
            if (forced)
            {
                if (runningBattleRoutine != null)
                {
                    StopCoroutine(runningBattleRoutine);
                    runningBattleRoutine = null;
                }

                StartCoroutine(FinalLoseAndExit());
            }
            return;
        }

        state = forced ? BattleState.WaitingForForcedSwitch : BattleState.Busy;
        SetPlayerCombatInputEnabled(false);

        string title = forced
            ? "Your mon fainted. Choose the next mon."
            : "Choose the mon you want to send out.";

        switchPopupUI.Show(
            title,
            candidates,
            forced,
            OnSwitchOptionSelected,
            OnSwitchPopupCancelled
        );
    }

    private void OnSwitchOptionSelected(MonInstance selectedMon)
    {
        if (selectedMon == null || !IsBattleActive())
            return;

        if (switchPopupUI != null)
            switchPopupUI.Hide();

        if (runningBattleRoutine != null)
        {
            StopCoroutine(runningBattleRoutine);
            runningBattleRoutine = null;
        }

        bool forced = state == BattleState.WaitingForForcedSwitch;

        runningBattleRoutine = StartCoroutine(
            forced
                ? ForcedSwitchResolutionCoroutine(selectedMon)
                : ManualSwitchTurnCoroutine(selectedMon)
        );
    }

    private void OnSwitchPopupCancelled()
    {
        if (state == BattleState.WaitingForForcedSwitch)
            return;

        if (!IsBattleActive())
            return;

        EnterPlayerTurn();
    }

    private bool TrySwitchActivePlayerMon(MonInstance selectedMon)
    {
        if (playerMon == null || selectedMon == null)
            return false;

        if (!IsMonAlive(selectedMon))
            return false;

        PlayerTeam team = GetPlayerTeam();
        if (team == null || team.team == null)
            return false;

        int limit = Mathf.Min(team.UnlockedSlots, team.team.Length);
        int selectedIndex = -1;

        for (int i = 0; i < limit; i++)
        {
            if (ReferenceEquals(team.team[i], selectedMon))
            {
                selectedIndex = i;
                break;
            }
        }

        if (selectedIndex < 0)
            return false;

        if (ReferenceEquals(playerMon.instance, selectedMon))
            return false;

        if (!team.SetActiveIndex(selectedIndex))
            return false;

        SetupPlayerHealth();
        battleUI?.ShowPlayerMon(playerMon);
        battleUI?.SetPlayerExp(playerMon.instance);
        movesUI?.Setup(playerMon, this);
        movesUI?.Refresh();

        return true;
    }

    private void EnterPlayerTurn()
    {
        if (!IsBattleActive())
            return;

        if (switchPopupUI != null && switchPopupUI.IsOpen)
            return;

        if (IsPlayerDead())
            return;

        state = BattleState.PlayerTurn;
        battleUI?.SetText($"What will {playerMon.instance.species.monName} do?");
        SetPlayerCombatInputEnabled(true);
    }

    private void SetPlayerCombatInputEnabled(bool enabled)
    {
        if (movesUI != null)
            movesUI.SetInteractable(enabled);

        if (battleUI != null)
            battleUI.SetSwitchButtonInteractable(enabled);
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
        bool leveledUp = MonLevelSystem.AddExperience(playerMon.instance, MonLevelSystem.ExpSource.Wild, phase);

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
            currentTrainer.MarkAsDefeated();

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
            return;

        List<MonInstance> ownedMons = team.GetOwnedMons();
        int phase = GetCurrentPhase();

        for (int i = 0; i < ownedMons.Count; i++)
        {
            MonInstance mon = ownedMons[i];
            if (mon == null || mon.species == null)
                continue;

            MonLevelSystem.AddExperience(mon, MonLevelSystem.ExpSource.PlayerKill, phase);
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
            yield break;

        state = BattleState.Busy;
        SetPlayerCombatInputEnabled(false);

        pendingPostBattleAction = PostBattleAction.GameOver;
        EndBattle();
    }

    private MoveData GetRandomEnemyMove()
    {
        MonInstance enemy = GetCurrentEnemyMon();
        if (enemy == null || enemy.moves == null || enemy.moves.Count == 0)
            return null;

        for (int i = 0; i < 10; i++)
        {
            MoveData candidate = enemy.moves[UnityEngine.Random.Range(0, enemy.moves.Count)];
            if (candidate != null)
                return candidate;
        }

        return null;
    }

    private int GetRandomEnemyMoveIndex()
    {
        MonInstance enemy = GetCurrentEnemyMon();
        if (enemy == null || enemy.moves == null || enemy.moves.Count == 0)
            return -1;

        for (int i = 0; i < 10; i++)
        {
            int index = UnityEngine.Random.Range(0, enemy.moves.Count);
            if (enemy.moves[index] != null)
                return index;
        }

        for (int i = 0; i < enemy.moves.Count; i++)
        {
            if (enemy.moves[i] != null)
                return i;
        }

        return -1;
    }

    private MoveData GetEnemyMoveByIndex(int index)
    {
        MonInstance enemy = GetCurrentEnemyMon();
        if (enemy == null || enemy.moves == null)
            return null;

        if (index < 0 || index >= enemy.moves.Count)
            return null;

        return enemy.moves[index];
    }

    private MoveData ResolveClassicEnemyMove()
    {
        int moveIndex = GetRandomEnemyMoveIndex();
        return GetEnemyMoveByIndex(moveIndex);
    }
    private void RefreshEnemyUI()
    {
        MonInstance enemy = GetCurrentEnemyMon();
        if (enemy == null)
            return;

        battleUI?.UpdateEnemyHP(enemy.currentHP, MonLevelSystem.GetMaxHP(enemy));
    }

    private void RefreshPlayerUI()
    {
        if (playerMon == null || playerMon.instance == null)
            return;

        battleUI?.UpdatePlayerHP(playerMon.instance.currentHP, MonLevelSystem.GetMaxHP(playerMon.instance));
        battleUI?.SetPlayerExp(playerMon.instance);
    }

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
                    return null;

                if (currentTrainerEnemyIndex < 0 || currentTrainerEnemyIndex >= currentTrainerRoster.Count)
                    return null;

                return currentTrainerRoster[currentTrainerEnemyIndex];
        }

        return null;
    }

    private int GetNextAliveTrainerMonIndex(int startAfterIndex)
    {
        if (currentTrainerRoster == null || currentTrainerRoster.Count == 0)
            return -1;

        int start = Mathf.Max(startAfterIndex + 1, 0);
        for (int i = start; i < currentTrainerRoster.Count; i++)
        {
            if (IsMonAlive(currentTrainerRoster[i]))
                return i;
        }

        return -1;
    }

    private static int ComputeDamage(MonInstance attacker, MonInstance defender, MoveData move)
    {
        int atk = MonLevelSystem.GetAttack(attacker);
        int def = MonLevelSystem.GetDefense(defender);

        int baseDmg = move.power + (atk / 4) - (def / 6);
        baseDmg = Mathf.Max(1, baseDmg);

        float mult = TypeChart.GetMultiplier(move.type, defender.species.type);
        int finalDmg = Mathf.RoundToInt(baseDmg * mult);

        return Mathf.Max(1, finalDmg);
    }

    private PlayerTeam GetPlayerTeam()
    {
        return playerMon != null ? playerMon.GetComponent<PlayerTeam>() : null;
    }

    private List<MonInstance> GetSwitchCandidates(bool excludeCurrentActive)
    {
        List<MonInstance> result = new();
        PlayerTeam team = GetPlayerTeam();

        if (team == null || team.team == null)
            return result;

        MonInstance currentActive = playerMon != null ? playerMon.instance : null;
        int limit = Mathf.Min(team.UnlockedSlots, team.team.Length);

        for (int i = 0; i < limit; i++)
        {
            MonInstance candidate = team.team[i];
            if (candidate == null)
                continue;

            if (!IsMonAlive(candidate))
                continue;

            if (excludeCurrentActive && ReferenceEquals(candidate, currentActive))
                continue;

            result.Add(candidate);
        }

        return result;
    }

    private static bool IsMonAlive(MonInstance mon)
    {
        return mon != null && mon.currentHP > 0;
    }

    private void DespawnCurrentWild()
    {
        if (currentWild != null)
            currentWild.gameObject.SetActive(false);
    }

    private int GetCurrentPhase()
    {
        return 3;
    }

    private void PlayWildBattleCry()
    {
        if (currentWild == null || currentWild.instance == null || currentWild.instance.species == null)
            return;

        PlaySound(currentWild.instance.species.battleCrySoundName, false);
    }

    private void PlayEnemyBattleCry()
    {
        MonInstance enemy = GetCurrentEnemyMon();
        if (enemy == null || enemy.species == null)
            return;

        PlaySound(enemy.species.battleCrySoundName, false);
    }

    private void PlayMoveSound(MoveData move)
    {
        if (move == null)
            return;

        PlaySound(move.attackSoundName, false);
    }

    private void PlaySound(string soundName, bool loop)
    {
        if (string.IsNullOrWhiteSpace(soundName))
            return;

        SoundManager manager = soundManager != null ? soundManager : SoundManager.Instance;
        if (manager == null)
        {
            Debug.LogWarning($"{nameof(LevelManager)}: no {nameof(SoundManager)} is available to play '{soundName}'.", this);
            return;
        }

        manager.Play(soundName, loop);
    }

    private IEnumerator PlayAttackProjectile(MoveData move, Vector2 startPos, Vector2 targetPos)
    {
        if (move == null || move.projectilePrefab == null || battleUI == null || battleUI.GetEffectsContainer() == null)
            yield break;

        bool arrived = false;

        AttackVfxUIProjectile projectileInstance = Instantiate(
            move.projectilePrefab,
            battleUI.GetEffectsContainer()
        );

        projectileInstance.Play(startPos, targetPos, () => arrived = true);

        while (!arrived)
            yield return null;

        Destroy(projectileInstance.gameObject);
    }

    private void CleanupEncounterReferences()
    {
        if (playerTouching != null)
        {
            playerTouching.lastWildMon = null;
            playerTouching.lastTrainer = null;
        }

        currentWild = null;
        currentTrainer = null;
        currentTrainerRoster.Clear();
        currentTrainerEnemyIndex = -1;

        battleUI?.ClearEnemyMon();
    }

    private void TryShowTrainerIntro(string trainerName, Sprite trainerSprite)
    {
        if (battleUI == null)
            return;

        MethodInfo method = battleUI.GetType().GetMethod(
            "ShowTrainerIntro",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        );

        if (method != null)
            method.Invoke(battleUI, new object[] { trainerName, trainerSprite });
    }

    private EnemyDecisionContext BuildEnemyDecisionContext()
    {
        string trainerId = currentTrainer != null ? currentTrainer.TrainerId : "wild";
        string trainerName =
            currentTrainer != null && currentTrainer.TrainerDefinition != null
                ? currentTrainer.TrainerDefinition.TrainerName
                : "Wild";

        MonInstance enemy = GetCurrentEnemyMon();
        MonInstance player = playerMon != null ? playerMon.instance : null;

        return new EnemyDecisionContext
        {
            trainerId = trainerId,
            trainerName = trainerName,
            turnNumber = 0,
            canSwitch = CanEnemySwitch(),
            enemyActive = BuildMonDecisionSnapshot(enemy, player),
            playerActive = BuildMonDecisionSnapshot(player, enemy),
            enemyBench = BuildEnemyBenchSnapshots()
        };
    }

    private IEnemyDecisionProvider CreateDecisionProvider()
    {
        MonInstance enemy = GetCurrentEnemyMon();

        switch (enemyDecisionMode)
        {
            case EnemyDecisionMode.Classic:
                return new ClassicEnemyDecisionProvider(enemy);

            case EnemyDecisionMode.HardApi:
                return new HardApiEnemyDecisionProvider(enemy);

            default:
                return new ClassicEnemyDecisionProvider(enemy);
        }
    }

    private EnemyDecisionResult ResolveEnemyDecision()
    {
        EnemyDecisionContext context = BuildEnemyDecisionContext();
        LogEnemyDecisionContext(context);

        IEnemyDecisionProvider provider = CreateDecisionProvider();

        if (provider == null)
        {
            return new EnemyDecisionResult
            {
                action = EnemyDecisionAction.UseMove,
                index = -1,
                reason = "provider_null",
                isFallback = true
            };
        }

        EnemyDecisionResult result = provider.Decide(context);

        if (result == null)
        {
            return new EnemyDecisionResult
            {
                action = EnemyDecisionAction.UseMove,
                index = -1,
                reason = "decision_null",
                isFallback = true
            };
        }

        return result;
    }
    private EnemyMonDecisionSnapshot BuildMonDecisionSnapshot(MonInstance source, MonInstance target)
    {
        if (source == null || source.species == null)
            return null;

        EnemyMoveDecisionSnapshot[] moveSnapshots;

        if (source.moves == null || source.moves.Count == 0)
        {
            moveSnapshots = Array.Empty<EnemyMoveDecisionSnapshot>();
        }
        else
        {
            moveSnapshots = new EnemyMoveDecisionSnapshot[source.moves.Count];

            for (int i = 0; i < source.moves.Count; i++)
            {
                MoveData move = source.moves[i];

                moveSnapshots[i] = new EnemyMoveDecisionSnapshot
                {
                    index = i,
                    moveName = move != null ? move.moveName : "Unknown",
                    type = move != null ? move.type.ToString() : "Unknown",
                    power = move != null ? move.power : 0,
                    expectedMultiplierVsTarget = (move != null && target != null && target.species != null)
                        ? TypeChart.GetMultiplier(move.type, target.species.type)
                        : 1f
                };
            }
        }

        return new EnemyMonDecisionSnapshot
        {
            speciesName = source.species.monName,
            type = source.species.type.ToString(),
            level = source.level,
            currentHP = source.currentHP,
            maxHP = MonLevelSystem.GetMaxHP(source),
            attack = MonLevelSystem.GetAttack(source),
            defense = MonLevelSystem.GetDefense(source),
            speed = MonLevelSystem.GetSpeed(source),
            moves = moveSnapshots
        };
    }

    private EnemyMonDecisionSnapshot[] BuildEnemyBenchSnapshots()
    {
        if (encounterType != EncounterType.Trainer || currentTrainerRoster == null || currentTrainerRoster.Count == 0)
            return Array.Empty<EnemyMonDecisionSnapshot>();

        List<EnemyMonDecisionSnapshot> result = new();
        MonInstance player = playerMon != null ? playerMon.instance : null;

        for (int i = 0; i < currentTrainerRoster.Count; i++)
        {
            if (i == currentTrainerEnemyIndex)
                continue;

            MonInstance candidate = currentTrainerRoster[i];
            if (!IsMonAlive(candidate))
                continue;

            EnemyMonDecisionSnapshot snapshot = BuildMonDecisionSnapshot(candidate, player);
            if (snapshot != null)
                result.Add(snapshot);
        }

        return result.ToArray();
    }

    private bool CanEnemySwitch()
    {
        if (encounterType != EncounterType.Trainer || currentTrainerRoster == null || currentTrainerRoster.Count == 0)
            return false;

        for (int i = 0; i < currentTrainerRoster.Count; i++)
        {
            if (i == currentTrainerEnemyIndex)
                continue;

            if (IsMonAlive(currentTrainerRoster[i]))
                return true;
        }

        return false;
    }

    private void LogEnemyDecisionContext(EnemyDecisionContext context)
    {
        if (!logEnemyDecisionContext || context == null)
            return;

        string json = JsonUtility.ToJson(context, true);
        Debug.Log($"[Enemy AI] Decision Context:\n{json}", this);
    }

    private bool TrySwitchEnemyMon(int targetIndex)
    {
        if (encounterType != EncounterType.Trainer)
            return false;

        if (currentTrainerRoster == null)
            return false;

        if (targetIndex < 0 || targetIndex >= currentTrainerRoster.Count)
            return false;

        if (targetIndex == currentTrainerEnemyIndex)
            return false;

        MonInstance candidate = currentTrainerRoster[targetIndex];

        if (!IsMonAlive(candidate))
            return false;

        currentTrainerEnemyIndex = targetIndex;

        SetupEnemyHealth();
        RefreshEnemyUI();
        battleUI?.ShowEnemyMon(candidate);

        string trainerName = currentTrainer != null && currentTrainer.TrainerDefinition != null
            ? currentTrainer.TrainerDefinition.TrainerName
            : "Trainer";

        battleUI?.SetText($"{trainerName} sends out {candidate.species.monName}.");

        PlayEnemyBattleCry();

        return true;
    }

    private IEnumerator ResolveEnemyTurnCoroutine()
    {
        // MODO HARD API
        if (enemyDecisionMode == EnemyDecisionMode.HardApi && enemyApiClient != null)
        {
            EnemyDecisionContext context = BuildEnemyDecisionContext();
            LogEnemyDecisionContext(context);

            bool finished = false;
            EnemyApiDecisionResponse apiResponse = null;
            string error = null;

            yield return enemyApiClient.RequestDecision(
                context,
                response =>
                {
                    apiResponse = response;
                    finished = true;
                },
                err =>
                {
                    error = err;
                    finished = true;
                }
            );

            if (!finished || apiResponse == null)
            {
                Debug.LogWarning($"[Enemy AI] API failed -> {error}");
                yield return EnemyAttack(ResolveClassicEnemyMove());
                yield break;
            }

            // Interpretar respuesta
            if (apiResponse.action == "switch_mon")
            {
                bool switched = TrySwitchEnemyMon(apiResponse.index);

                if (switched)
                {
                    yield return new WaitForSecondsRealtime(TurnDelay);
                    yield break;
                }
            }

            if (apiResponse.action == "use_move")
            {
                MoveData move = GetEnemyMoveByIndex(apiResponse.index);

                if (move != null)
                {
                    yield return EnemyAttack(move);
                    yield break;
                }
            }

            // fallback
            yield return EnemyAttack(ResolveClassicEnemyMove());
            yield break;
        }

        // MODO CLÁSICO
        EnemyDecisionResult decision = ResolveEnemyDecision();

        if (decision == null)
        {
            yield return EnemyAttack(ResolveClassicEnemyMove());
            yield break;
        }

        if (decision.action == EnemyDecisionAction.SwitchMon)
        {
            bool switched = TrySwitchEnemyMon(decision.index);

            if (switched)
            {
                yield return new WaitForSecondsRealtime(TurnDelay);
                yield break;
            }

            yield return EnemyAttack(ResolveClassicEnemyMove());
            yield break;
        }

        if (decision.action == EnemyDecisionAction.UseMove)
        {
            MoveData selectedMove = GetEnemyMoveByIndex(decision.index);

            if (selectedMove != null)
            {
                yield return EnemyAttack(selectedMove);
                yield break;
            }
        }

        yield return EnemyAttack(ResolveClassicEnemyMove());
    }

    private IEnumerator TestEnemyApiRequestCoroutine()
    {
        if (enemyApiClient == null)
        {
            Debug.LogError("[Enemy AI] EnemyApiClient reference is missing.", this);
            yield break;
        }

        EnemyDecisionContext context = BuildEnemyDecisionContext();
        LogEnemyDecisionContext(context);

        bool completed = false;

        yield return enemyApiClient.RequestDecision(
            context,
            response =>
            {
                completed = true;
                Debug.Log(
                    $"[Enemy AI] API success -> action={response.action}, index={response.index}, reason={response.reason}",
                    this
                );
            },
            error =>
            {
                completed = true;
                Debug.LogError($"[Enemy AI] API error -> {error}", this);
            }
        );

        if (!completed)
        {
            Debug.LogWarning("[Enemy AI] API request finished without success/error callback.", this);
        }
    }

    [ContextMenu("Test Enemy API Request")]
    private void TestEnemyApiRequest()
    {
        StartCoroutine(TestEnemyApiRequestCoroutine());
    }

    private BattleBiome ResolveBattleBiome()
    {
        if (playerMon == null)
            return BattleBiome.Default;

        Vector2 position = playerMon.transform.position;
        Collider2D[] hits = Physics2D.OverlapPointAll(position);

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null)
                continue;

            BattleBiomeZone zone = hits[i].GetComponent<BattleBiomeZone>();
            if (zone != null)
                return zone.Biome;
        }

        return BattleBiome.Default;
    }

    private void ApplyBattleBackground()
{
    if (battleBackgroundSelector == null)
        return;

    BattleBiome biome = ResolveBattleBiome();
    battleBackgroundSelector.ApplyBackground(biome);
}

private EnemyBarkContext BuildEnemyBarkContext(string eventType, string extraInfo = "")
{
    MonInstance enemy = GetCurrentEnemyMon();
    MonInstance player = playerMon != null ? playerMon.instance : null;

    string trainerId = currentTrainer != null ? currentTrainer.TrainerId : "wild";
    string trainerName = currentTrainer != null && currentTrainer.TrainerDefinition != null
        ? currentTrainer.TrainerDefinition.TrainerName
        : "Wild";

    string personality = currentTrainer != null && currentTrainer.TrainerDefinition != null
        ? currentTrainer.TrainerDefinition.BarkPersonality
        : "Neutral tone.";

    return new EnemyBarkContext
    {
        trainerId = trainerId,
        trainerName = trainerName,
        trainerPersonality = personality,
        eventType = eventType,

        enemyMonName = enemy != null && enemy.species != null ? enemy.species.monName : "Enemy",
        playerMonName = player != null && player.species != null ? player.species.monName : "Player mon",

        enemyCurrentHP = enemy != null ? enemy.currentHP : 0,
        enemyMaxHP = enemy != null ? MonLevelSystem.GetMaxHP(enemy) : 0,
        playerCurrentHP = player != null ? player.currentHP : 0,
        playerMaxHP = player != null ? MonLevelSystem.GetMaxHP(player) : 0,

        extraInfo = extraInfo
    };
}

private IEnumerator TryShowEnemyBark(string eventType, string fallbackText = null, string extraInfo = "")
{
    if (!enableApiBarks || enemyBarkApiClient == null || encounterType != EncounterType.Trainer)
    {
        if (!string.IsNullOrWhiteSpace(fallbackText))
            battleUI?.SetText(fallbackText);

        yield break;
    }

    EnemyBarkContext context = BuildEnemyBarkContext(eventType, extraInfo);

    string bark = null;

    yield return enemyBarkApiClient.RequestBark(
        context,
        response =>
        {
            bark = response.bark;
        },
        error =>
        {
            Debug.LogWarning($"[Enemy Bark API] Failed: {error}", this);
        }
    );

    if (!string.IsNullOrWhiteSpace(bark))
    {
        battleUI?.SetText(bark);
        yield return new WaitForSecondsRealtime(1.5f);
        yield break;
    }

    if (!string.IsNullOrWhiteSpace(fallbackText))
    {
        battleUI?.SetText(fallbackText);
        yield return new WaitForSecondsRealtime(1f);
    }
}
    
}
