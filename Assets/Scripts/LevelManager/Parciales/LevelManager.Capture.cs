using System.Collections;
using UnityEngine;

public partial class LevelManager
{
    public void TryCapture()
    {
        if (encounterType == EncounterType.Trainer)
        {
            battleUI?.SetText("You cannot catch a trainer's mons.");
            return;
        }

        if (!IsBattleActive())
        {
            return;
        }

        if (state != BattleState.PlayerTurn)
        {
            return;
        }

        if (currentWild == null || currentWild.instance == null || playerMon == null || playerMon.instance == null)
        {
            return;
        }

        if (!IsMonAlive(currentWild.instance))
        {
            return;
        }

        PlayerTeam team = GetPlayerTeam();

        if (team == null)
        {
            return;
        }

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

        yield return PlayCaptureBallThrow();

        bool captured = CatchSystem.TryCatch(
            playerMon.instance,
            currentWild.instance,
            out float chance,
            out float roll
        );

        Debug.Log($"Catch roll={roll:F2} chance={chance:F2}");

        if (captured)
        {
            yield return ShowCaptureBallResult(true);

            PlayerTeam team = GetPlayerTeam();
            MonInstance newMon = MonLevelSystem.Clone(currentWild.instance);

            if (team == null || newMon == null)
            {
                ClearCaptureBallVfx();
                runningBattleRoutine = null;
                EnterPlayerTurn();
                yield break;
            }

            if (!team.TryAddToNextFreeSlot(newMon))
            {
                ClearCaptureBallVfx();
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

            team.RefreshTeamUI();

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

        yield return ShowCaptureBallResult(false);
        yield return CaptureFailCoroutine();
    }

   private IEnumerator CaptureSuccessCoroutine(MonInstance capturedMon)
    {
        battleUI?.SetText($"{capturedMon.species.monName} was caught.");

        StopBattleMusicForCapture();
        PlaySound(MonCatchSoundName, false);

        yield return new WaitForSecondsRealtime(captureSuccessHoldSeconds);

        ClearCaptureBallVfx();
        DespawnCurrentWild();
        EndBattle();
    }

   private IEnumerator CaptureFailCoroutine()
    {
        battleUI?.SetText($"{currentWild.instance.species.monName} broke free.");

        PlaySound(CatchFailSoundName, false);

        yield return new WaitForSecondsRealtime(0.8f);

        ClearCaptureBallVfx();

        battleUI?.SetEnemySpriteVisible(true);

        if (IsEnemyAbleToAct())
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
        }

        runningBattleRoutine = null;

        EnterPlayerTurn();
    }

    private IEnumerator PlayCaptureBallThrow()
    {
        ClearCaptureBallVfx();

        if (captureBallProjectilePrefab == null || battleUI == null || battleUI.GetEffectsContainer() == null)
        {
            yield return new WaitForSecondsRealtime(1.3f);
            battleUI?.SetEnemySpriteVisible(false);
            yield break;
        }

        bool arrived = false;

        activeCaptureBallVfx = Instantiate(
            captureBallProjectilePrefab,
            battleUI.GetEffectsContainer()
        );

        activeCaptureBallVfx.SetSprite(captureBallThrowSprite);

        Vector2 startPosition = battleUI.GetPlayerAttackOrigin();
        Vector2 targetPosition = battleUI.GetEnemyHitPoint();

        activeCaptureBallVfx.Play(startPosition, targetPosition, () => arrived = true);

        while (!arrived)
        {
            yield return null;
        }

        battleUI.SetEnemySpriteVisible(false);

        yield return new WaitForSecondsRealtime(0.25f);
    }

    private IEnumerator ShowCaptureBallResult(bool captured)
    {
        if (activeCaptureBallVfx == null)
        {
            yield return new WaitForSecondsRealtime(captureBallResultSeconds);
            yield break;
        }

        Sprite resultSprite = captured
            ? captureBallSuccessSprite
            : captureBallFailSprite;

        activeCaptureBallVfx.SetSprite(resultSprite);

        yield return new WaitForSecondsRealtime(captureBallResultSeconds);
    }

    private void ClearCaptureBallVfx()
    {
        if (activeCaptureBallVfx == null)
        {
            return;
        }

        Destroy(activeCaptureBallVfx.gameObject);
        activeCaptureBallVfx = null;
    }

    private void DespawnCurrentWild()
    {
        if (currentWild != null)
        {
            currentWild.gameObject.SetActive(false);
        }
    }

    private void StopBattleMusicForCapture()
    {
        if (music != null)
        {
            music.StopMusic();
            return;
        }

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.ResetSound();
        }
    }
}