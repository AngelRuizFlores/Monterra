using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class LevelManager
{
    public void TryOpenManualSwitchPopup()
    {
        if (!IsBattleActive())
        {
            return;
        }

        if (state != BattleState.PlayerTurn)
        {
            return;
        }

        if (switchPopupUI == null)
        {
            Debug.LogError($"{nameof(LevelManager)}: switch popup reference is missing.");
            return;
        }

        if (switchPopupUI.IsOpen || switchResolutionInProgress)
        {
            return;
        }

        List<MonInstance> candidates = GetSwitchCandidates(excludeCurrentActive: true);

        if (candidates.Count <= 0)
        {
            battleUI?.SetText("No other living mons are available.");
            return;
        }

        OpenSwitchPopup(candidates, false);
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
            OpenSwitchPopup(replacements, true);
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

        battleUI?.SetPlayerSpriteVisible(false);

        if (!TrySwitchActivePlayerMon(selectedMon, false))
        {
            battleUI?.SetPlayerSpriteVisible(true);
            switchResolutionInProgress = false;
            EnterPlayerTurn();
            yield break;
        }

        battleUI?.SetText($"Go, {playerMon.instance.species.monName}.");

        yield return PlaySwitchBall(true);

        battleUI?.ShowPlayerMon(playerMon);
        battleUI?.SetPlayerSpriteVisible(true);

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

        battleUI?.SetPlayerSpriteVisible(false);

        if (!TrySwitchActivePlayerMon(selectedMon, false))
        {
            battleUI?.SetPlayerSpriteVisible(true);
            switchResolutionInProgress = false;

            List<MonInstance> replacements = GetSwitchCandidates(excludeCurrentActive: true);

            if (replacements.Count > 0)
            {
                OpenSwitchPopup(replacements, true);
                yield break;
            }

            yield return FinalLoseAndExit();
            yield break;
        }

        battleUI?.SetText($"{playerMon.instance.species.monName} enters the battle.");

        yield return PlaySwitchBall(true);

        battleUI?.ShowPlayerMon(playerMon);
        battleUI?.SetPlayerSpriteVisible(true);

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
        {
            return;
        }

        if (switchPopupUI != null)
        {
            switchPopupUI.Hide();
        }

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
        {
            return;
        }

        if (!IsBattleActive())
        {
            return;
        }

        EnterPlayerTurn();
    }

    private bool TrySwitchActivePlayerMon(MonInstance selectedMon, bool showVisuals = true)
    {
        if (playerMon == null || selectedMon == null)
        {
            return false;
        }

        if (!IsMonAlive(selectedMon))
        {
            return false;
        }

        PlayerTeam team = GetPlayerTeam();

        if (team == null || team.team == null)
        {
            return false;
        }

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
        {
            return false;
        }

        if (ReferenceEquals(playerMon.instance, selectedMon))
        {
            return false;
        }

        if (!team.SetActiveIndex(selectedIndex))
        {
            return false;
        }

        SetupPlayerHealth();

        if (showVisuals)
        {
            battleUI?.ShowPlayerMon(playerMon);
            battleUI?.SetPlayerExp(playerMon.instance);
        }

        movesUI?.Setup(playerMon, this);
        movesUI?.Refresh();

        return true;
    }

    private List<MonInstance> GetSwitchCandidates(bool excludeCurrentActive)
    {
        List<MonInstance> result = new List<MonInstance>();
        PlayerTeam team = GetPlayerTeam();

        if (team == null || team.team == null)
        {
            return result;
        }

        MonInstance currentActive = playerMon != null ? playerMon.instance : null;
        int limit = Mathf.Min(team.UnlockedSlots, team.team.Length);

        for (int i = 0; i < limit; i++)
        {
            MonInstance candidate = team.team[i];

            if (candidate == null)
            {
                continue;
            }

            if (!IsMonAlive(candidate))
            {
                continue;
            }

            if (excludeCurrentActive && ReferenceEquals(candidate, currentActive))
            {
                continue;
            }

            result.Add(candidate);
        }

        return result;
    }

    private IEnumerator PlaySwitchBall(bool playerSide)
    {
        if (captureBallProjectilePrefab == null || battleUI == null || battleUI.GetEffectsContainer() == null)
        {
            yield break;
        }

        bool arrived = false;

        AttackVfxUIProjectile ball = Instantiate(
            captureBallProjectilePrefab,
            battleUI.GetEffectsContainer()
        );

        ball.SetSprite(captureBallThrowSprite);

        Vector2 startPosition = playerSide
            ? battleUI.GetPlayerAttackOrigin()
            : battleUI.GetEnemyAttackOrigin();

        Vector2 targetPosition = playerSide
            ? battleUI.GetPlayerHitPoint()
            : battleUI.GetEnemyHitPoint();

        ball.Play(startPosition, targetPosition, () => arrived = true);

        while (!arrived)
        {
            yield return null;
        }

        yield return new WaitForSecondsRealtime(0.15f);

        if (ball != null)
        {
            Destroy(ball.gameObject);
        }
    }
}