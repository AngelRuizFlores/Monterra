using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class LevelManager : MonoBehaviour
{
    private const float TurnDelay = 1f;

    [Header("Battle UI")]
    [SerializeField] private GameObject battleCanvas;
    [SerializeField] private BattleUI battleUI;
    [SerializeField] private MovesUI movesUI;
    [SerializeField] private BattleMonSwitchPopupUI switchPopupUI;

    [Header("Player")]
    [SerializeField] private TouchingBehaviour playerTouching;
    [SerializeField] private PlayerMon playerMon;

    [Header("Health")]
    [SerializeField] private HealthBehaviour enemyHealth;
    [SerializeField] private HealthBehaviour playerHealth;

    [Header("Music")]
    [SerializeField] private MusicGame music;

    [Header("Events")]
    [SerializeField] private UnityEvent onPlayerPartyDefeated;

    private WildMon currentWild;
    private Coroutine runningBattleRoutine;

    private enum BattleState
    {
        Inactive,
        PlayerTurn,
        Busy,
        WaitingForForcedSwitch
    }

    private BattleState state = BattleState.Inactive;
    private bool battleEnding;
    private bool switchResolutionInProgress;

    void Awake()
    {
        if (battleCanvas != null)
            battleCanvas.SetActive(false);

        playerMon?.InitIfNeeded();

        if (battleUI != null)
            battleUI.BindSwitchAction(TryOpenManualSwitchPopup);

        if (switchPopupUI != null)
            switchPopupUI.HideImmediate();

        state = BattleState.Inactive;
    }

    public void StartBattle()
    {
        if (!CanStartBattle(out currentWild))
            return;

        battleEnding = false;
        switchResolutionInProgress = false;

        EnsureInstances(currentWild);
        ShowBattleUI(currentWild);

        enemyHealth?.Init(currentWild.instance);
        playerHealth?.Init(playerMon.instance);

        SetupMovesUI();

        if (switchPopupUI != null)
            switchPopupUI.HideImmediate();

        Time.timeScale = 0f;
        state = BattleState.PlayerTurn;
        battleUI?.SetText($"¿Qué hará {playerMon.instance.species.monName}?");
        SetPlayerCombatInputEnabled(true);
    }

    public void EndBattle()
    {
        if (runningBattleRoutine != null)
        {
            StopCoroutine(runningBattleRoutine);
            runningBattleRoutine = null;
        }

        battleEnding = true;
        switchResolutionInProgress = false;

        if (switchPopupUI != null)
            switchPopupUI.HideImmediate();

        SetPlayerCombatInputEnabled(false);

        Time.timeScale = 1f;
        music?.StartWorldMusic();

        if (battleCanvas != null)
            battleCanvas.SetActive(false);

        if (playerTouching != null)
            playerTouching.lastWildMon = null;

        currentWild = null;
        state = BattleState.Inactive;
    }

    public void UsePlayerMove(MoveData move)
    {
        if (!IsBattleActive())
            return;

        if (state != BattleState.PlayerTurn)
            return;

        if (switchPopupUI != null && switchPopupUI.IsOpen)
            return;

        if (move == null || currentWild == null || currentWild.instance == null || playerMon == null || playerMon.instance == null)
            return;

        if (!IsMonAlive(playerMon.instance) || !IsMonAlive(currentWild.instance))
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
            Debug.LogError($"{nameof(LevelManager)}: No hay referencia al popup de cambio de mon.");
            return;
        }

        if (switchPopupUI.IsOpen || switchResolutionInProgress)
            return;

        List<MonInstance> candidates = GetSwitchCandidates(excludeCurrentActive: true);
        if (candidates.Count <= 0)
        {
            battleUI?.SetText("No hay otros mons vivos disponibles.");
            return;
        }

        OpenSwitchPopup(candidates, forced: false);
    }

    public void TryCapture()
    {
        if (!IsBattleActive())
            return;

        if (currentWild == null || playerMon == null)
            return;

        PlayerTeam team = GetPlayerTeam();
        if (team == null)
            return;

        MonInstance newMon = MonLevelSystem.Clone(currentWild.instance);
        if (newMon == null)
        {
            Debug.LogError($"{nameof(LevelManager)}: No se pudo clonar el mon salvaje.");
            return;
        }

        if (!team.TryAddToNextFreeSlot(newMon))
        {
            battleUI?.SetText("No tienes espacio en el equipo.");
            return;
        }

        battleUI?.SetText($"{newMon.species.monName} fue capturado!");
        DespawnCurrentWild();
        EndBattle();
    }

    private bool CanStartBattle(out WildMon wild)
    {
        wild = null;

        if (playerTouching == null || playerMon == null)
            return false;

        wild = playerTouching.lastWildMon;
        if (wild == null)
            return false;

        if (battleCanvas == null || battleUI == null || movesUI == null)
            return false;

        if (enemyHealth == null || playerHealth == null)
            return false;

        if (GetPlayerTeam() == null)
        {
            Debug.LogError($"{nameof(LevelManager)}: No se encontró {nameof(PlayerTeam)} en el player.");
            return false;
        }

        return true;
    }

    private void EnsureInstances(WildMon wild)
    {
        if (wild != null && wild.instance == null)
            wild.Init();

        playerMon?.InitIfNeeded();
    }

    private void ShowBattleUI(WildMon wild)
    {
        if (battleCanvas == null || battleUI == null || wild == null || wild.instance == null || playerMon == null || playerMon.instance == null)
            return;

        battleCanvas.SetActive(true);

        battleUI.ShowWildMon(wild);
        battleUI.ShowPlayerMon(playerMon);
        battleUI.SetPlayerExp(playerMon.instance);
        battleUI.SetText($"¡Apareció {wild.instance.species.monName}!");
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

    private IEnumerator BattleTurnCoroutine(MoveData playerMove)
    {
        state = BattleState.Busy;
        SetPlayerCombatInputEnabled(false);

        bool playerFirst = MonLevelSystem.GetSpeed(playerMon.instance) >= MonLevelSystem.GetSpeed(currentWild.instance);

        if (playerFirst)
        {
            yield return PlayerAttack(playerMove);
            if (battleEnding) yield break;

            if (IsWildDead())
            {
                yield return WinWildAndExit();
                yield break;
            }

            yield return EnemyAttack();
            if (battleEnding) yield break;

            if (IsPlayerDead())
            {
                yield return HandlePlayerDefeatAfterEnemyAction();
                yield break;
            }
        }
        else
        {
            yield return EnemyAttack();
            if (battleEnding) yield break;

            if (IsPlayerDead())
            {
                yield return HandlePlayerDefeatAfterEnemyAction();
                yield break;
            }

            yield return PlayerAttack(playerMove);
            if (battleEnding) yield break;

            if (IsWildDead())
            {
                yield return WinWildAndExit();
                yield break;
            }
        }

        runningBattleRoutine = null;
        EnterPlayerTurn();
    }

    private IEnumerator PlayerAttack(MoveData move)
    {
        if (move == null || playerMon == null || playerMon.instance == null || currentWild == null || currentWild.instance == null)
            yield break;

        MonInstance attacker = playerMon.instance;
        MonInstance defender = currentWild.instance;

        battleUI?.SetText($"{attacker.species.monName} usó {move.moveName}!");

        float mult = TypeChart.GetMultiplier(move.type, defender.species.type);
        int dmg = ComputeDamage(attacker, defender, move);

        enemyHealth?.Hurt(dmg);
        RefreshEnemyUI();

        yield return new WaitForSecondsRealtime(TurnDelay);

        string effectText = TypeChart.GetEffectText(mult);
        if (!string.IsNullOrEmpty(effectText))
        {
            battleUI?.SetText(effectText);
            yield return new WaitForSecondsRealtime(TurnDelay);
        }
    }

    private IEnumerator EnemyAttack()
    {
        if (currentWild == null || currentWild.instance == null || playerMon == null || playerMon.instance == null)
            yield break;

        MonInstance attacker = currentWild.instance;
        MonInstance defender = playerMon.instance;

        MoveData enemyMove = GetRandomEnemyMove();
        string moveName = enemyMove != null ? enemyMove.moveName : "Punch";

        battleUI?.SetText($"{attacker.species.monName} usó {moveName}!");

        float mult = 1f;
        int dmg;

        if (enemyMove != null)
        {
            mult = TypeChart.GetMultiplier(enemyMove.type, defender.species.type);
            dmg = ComputeDamage(attacker, defender, enemyMove);
        }
        else
        {
            dmg = 5;
        }

        playerHealth?.Hurt(dmg);
        RefreshPlayerUI();

        yield return new WaitForSecondsRealtime(TurnDelay);

        string effectText = TypeChart.GetEffectText(mult);
        if (!string.IsNullOrEmpty(effectText))
        {
            battleUI?.SetText(effectText);
            yield return new WaitForSecondsRealtime(TurnDelay);
        }
    }

    private IEnumerator HandlePlayerDefeatAfterEnemyAction()
    {
        runningBattleRoutine = null;

        if (!IsPlayerDead())
        {
            EnterPlayerTurn();
            yield break;
        }

        battleUI?.SetText($"¡{playerMon.instance.species.monName} se debilitó!");
        yield return new WaitForSecondsRealtime(TurnDelay);

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

        battleUI?.SetText($"¡Adelante, {playerMon.instance.species.monName}!");
        yield return new WaitForSecondsRealtime(TurnDelay);

        if (!IsWildDead() && IsEnemyAbleToAct())
        {
            yield return EnemyAttack();
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

        battleUI?.SetText($"¡{playerMon.instance.species.monName} entra al combate!");
        yield return new WaitForSecondsRealtime(TurnDelay);

        switchResolutionInProgress = false;
        EnterPlayerTurn();
    }

    private void OpenSwitchPopup(List<MonInstance> candidates, bool forced)
    {
        if (switchPopupUI == null)
        {
            Debug.LogError($"{nameof(LevelManager)}: No hay popup de cambio configurado.");
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
            ? "Tu mon se ha debilitado. Elige el siguiente mon."
            : "Elige el mon que quieres sacar.";

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

        playerHealth?.Init(playerMon.instance);
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
        battleUI?.SetText($"¿Qué hará {playerMon.instance.species.monName}?");
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

        battleUI?.SetText($"¡{currentWild.instance.species.monName} se debilitó!");
        yield return new WaitForSecondsRealtime(TurnDelay);

        int phase = GetCurrentPhase();
        bool leveledUp = MonLevelSystem.AddExperience(playerMon.instance, MonLevelSystem.ExpSource.Wild, phase);

        battleUI?.ShowPlayerMon(playerMon);
        battleUI?.SetPlayerExp(playerMon.instance);

        if (leveledUp)
        {
            movesUI?.Refresh();
            battleUI?.SetText($"¡{playerMon.instance.species.monName} subió a Nv. {playerMon.instance.level}!");
            yield return new WaitForSecondsRealtime(TurnDelay);
        }

        DespawnCurrentWild();
        EndBattle();
    }

    private IEnumerator FinalLoseAndExit()
    {
        if (battleEnding)
            yield break;

        battleEnding = true;
        state = BattleState.Busy;
        SetPlayerCombatInputEnabled(false);

        if (playerMon != null && playerMon.instance != null)
        {
            battleUI?.SetText($"¡{playerMon.instance.species.monName} se debilitó!");
            yield return new WaitForSecondsRealtime(TurnDelay);
        }

        onPlayerPartyDefeated?.Invoke();
        EndBattle();
    }

    private MoveData GetRandomEnemyMove()
    {
        if (currentWild == null || currentWild.instance == null)
            return null;

        List<MoveData> moves = currentWild.instance.moves;
        if (moves == null || moves.Count == 0)
            return null;

        for (int i = 0; i < 10; i++)
        {
            MoveData candidate = moves[UnityEngine.Random.Range(0, moves.Count)];
            if (candidate != null)
                return candidate;
        }

        return null;
    }

    private void RefreshEnemyUI()
    {
        if (currentWild == null || currentWild.instance == null)
            return;

        battleUI?.UpdateEnemyHP(currentWild.instance.currentHP, MonLevelSystem.GetMaxHP(currentWild.instance));
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
        return !battleEnding && currentWild != null && state != BattleState.Inactive;
    }

    private bool IsWildDead()
    {
        return currentWild == null || currentWild.instance == null || currentWild.instance.currentHP <= 0;
    }

    private bool IsPlayerDead()
    {
        return playerMon == null || playerMon.instance == null || playerMon.instance.currentHP <= 0;
    }

    private bool IsEnemyAbleToAct()
    {
        return currentWild != null && currentWild.instance != null && currentWild.instance.currentHP > 0;
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
        List<MonInstance> result = new List<MonInstance>();
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

    private bool IsTeamMember(MonInstance mon)
    {
        if (mon == null)
            return false;

        PlayerTeam team = GetPlayerTeam();
        if (team == null || team.team == null)
            return false;

        int limit = Mathf.Min(team.UnlockedSlots, team.team.Length);

        for (int i = 0; i < limit; i++)
        {
            if (ReferenceEquals(team.team[i], mon))
                return true;
        }

        return false;
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
}