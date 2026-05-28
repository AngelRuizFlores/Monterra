using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class LevelManager
{
    private MoveData GetRandomEnemyMove()
    {
        MonInstance enemy = GetCurrentEnemyMon();

        if (enemy == null || enemy.moves == null || enemy.moves.Count == 0)
        {
            return null;
        }

        for (int i = 0; i < 10; i++)
        {
            MoveData candidate = enemy.moves[UnityEngine.Random.Range(0, enemy.moves.Count)];

            if (candidate != null)
            {
                return candidate;
            }
        }

        return null;
    }

    private int GetRandomEnemyMoveIndex()
    {
        MonInstance enemy = GetCurrentEnemyMon();

        if (enemy == null || enemy.moves == null || enemy.moves.Count == 0)
        {
            return -1;
        }

        for (int i = 0; i < 10; i++)
        {
            int index = UnityEngine.Random.Range(0, enemy.moves.Count);

            if (enemy.moves[index] != null)
            {
                return index;
            }
        }

        for (int i = 0; i < enemy.moves.Count; i++)
        {
            if (enemy.moves[i] != null)
            {
                return i;
            }
        }

        return -1;
    }

    private MoveData GetEnemyMoveByIndex(int index)
    {
        MonInstance enemy = GetCurrentEnemyMon();

        if (enemy == null || enemy.moves == null)
        {
            return null;
        }

        if (index < 0 || index >= enemy.moves.Count)
        {
            return null;
        }

        return enemy.moves[index];
    }

    private MoveData ResolveClassicEnemyMove()
    {
        int moveIndex = GetRandomEnemyMoveIndex();

        return GetEnemyMoveByIndex(moveIndex);
    }

    private EnemyDecisionContext BuildEnemyDecisionContext()
    {
        string trainerId = currentTrainer != null ? currentTrainer.TrainerId : "wild";

        string trainerName = currentTrainer != null && currentTrainer.TrainerDefinition != null
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
        {
            return null;
        }

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
                    expectedMultiplierVsTarget = move != null && target != null && target.species != null
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
        {
            return Array.Empty<EnemyMonDecisionSnapshot>();
        }

        List<EnemyMonDecisionSnapshot> result = new List<EnemyMonDecisionSnapshot>();
        MonInstance player = playerMon != null ? playerMon.instance : null;

        for (int i = 0; i < currentTrainerRoster.Count; i++)
        {
            if (i == currentTrainerEnemyIndex)
            {
                continue;
            }

            MonInstance candidate = currentTrainerRoster[i];

            if (!IsMonAlive(candidate))
            {
                continue;
            }

            EnemyMonDecisionSnapshot snapshot = BuildMonDecisionSnapshot(candidate, player);

            if (snapshot != null)
            {
                result.Add(snapshot);
            }
        }

        return result.ToArray();
    }

    private bool CanEnemySwitch()
    {
        if (encounterType != EncounterType.Trainer || currentTrainerRoster == null || currentTrainerRoster.Count == 0)
        {
            return false;
        }

        for (int i = 0; i < currentTrainerRoster.Count; i++)
        {
            if (i == currentTrainerEnemyIndex)
            {
                continue;
            }

            if (IsMonAlive(currentTrainerRoster[i]))
            {
                return true;
            }
        }

        return false;
    }

    private void LogEnemyDecisionContext(EnemyDecisionContext context)
    {
        if (!logEnemyDecisionContext || context == null)
        {
            return;
        }

        string json = JsonUtility.ToJson(context, true);

        Debug.Log($"[Enemy AI] Decision Context:\n{json}", this);
    }

    private bool TrySwitchEnemyMon(int targetIndex, bool showVisuals = true)
    {
        if (encounterType != EncounterType.Trainer)
        {
            return false;
        }

        if (currentTrainerRoster == null)
        {
            return false;
        }

        if (targetIndex < 0 || targetIndex >= currentTrainerRoster.Count)
        {
            return false;
        }

        if (targetIndex == currentTrainerEnemyIndex)
        {
            return false;
        }

        MonInstance candidate = currentTrainerRoster[targetIndex];

        if (!IsMonAlive(candidate))
        {
            return false;
        }

        currentTrainerEnemyIndex = targetIndex;

        SetupEnemyHealth();
        RefreshEnemyUI();

        if (showVisuals)
        {
            battleUI?.ShowEnemyMon(candidate);
            PlayEnemyBattleCry();
        }

        return true;
    }

    private IEnumerator ResolveEnemyTurnCoroutine()
    {
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

            if (apiResponse.action == "switch_mon")
            {
                bool switched = false;

                yield return TrySwitchEnemyMonWithBall(apiResponse.index, result => switched = result);

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

            yield return EnemyAttack(ResolveClassicEnemyMove());
            yield break;
        }

        EnemyDecisionResult decision = ResolveEnemyDecision();

        if (decision == null)
        {
            yield return EnemyAttack(ResolveClassicEnemyMove());
            yield break;
        }

       if (decision.action == EnemyDecisionAction.SwitchMon)
        {
            bool switched = false;

            yield return TrySwitchEnemyMonWithBall(decision.index, result => switched = result);

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

    private IEnumerator TrySwitchEnemyMonWithBall(int targetIndex, Action<bool> onComplete)
    {
        if (currentTrainerRoster == null || targetIndex < 0 || targetIndex >= currentTrainerRoster.Count)
        {
            onComplete?.Invoke(false);
            yield break;
        }

        MonInstance candidate = currentTrainerRoster[targetIndex];

        if (candidate == null || candidate.species == null)
        {
            onComplete?.Invoke(false);
            yield break;
        }

        string trainerName = currentTrainer != null && currentTrainer.TrainerDefinition != null
            ? currentTrainer.TrainerDefinition.TrainerName
            : "Trainer";

        battleUI?.SetEnemySpriteVisible(false);

        bool switched = TrySwitchEnemyMon(targetIndex, false);

        if (!switched)
        {
            battleUI?.SetEnemySpriteVisible(true);
            onComplete?.Invoke(false);
            yield break;
        }

        battleUI?.SetText($"{trainerName} sends out {candidate.species.monName}.");

        yield return PlaySwitchBall(false);

        battleUI?.ShowEnemyMon(candidate);
        battleUI?.SetEnemySpriteVisible(true);

        PlayEnemyBattleCry();

        onComplete?.Invoke(true);
    }
}