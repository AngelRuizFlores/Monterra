using System.Collections.Generic;
using UnityEngine;

public static class MonLevelSystem
{
    public const int MAX_LEVEL = 18;

    private const int BASE_EXP = 30;
    private const int STEP_EXP = 10;

    private static readonly int[] PhaseCaps = { 5, 10, 15 };
    private static readonly int[] BaseExpBySource = { 60, 60, 120, 80 };

    public enum ExpSource
    {
        Wild,
        Capture,
        PlayerKill,
        Event
    }

    public static int ExpToNextLevel(int level)
    {
        return BASE_EXP + (level - 1) * STEP_EXP;
    }

    public static int GetMaxHP(MonInstance mon)
    {
        return mon.species.baseHP + mon.level * mon.species.hpPerLevel;
    }

    public static int GetAttack(MonInstance mon)
    {
        return mon.species.baseAttack + mon.level * mon.species.attackPerLevel;
    }

    public static int GetDefense(MonInstance mon)
    {
        return mon.species.baseDefense + mon.level;
    }

    public static int GetSpeed(MonInstance mon)
    {
        return mon.species.baseSpeed + mon.level;
    }

    public static int GetLevelCapForPhase(int phase)
    {
        if (phase >= 0 && phase < PhaseCaps.Length)
        {
            return PhaseCaps[phase];
        }

        return MAX_LEVEL;
    }

    public static int BaseExpFor(ExpSource source)
    {
        int index = (int)source;

        return index >= 0 && index < BaseExpBySource.Length ? BaseExpBySource[index] : 0;
    }

    public static bool AddExperience(MonInstance mon, ExpSource source, int phase)
    {
        if (mon?.species == null)
        {
            return false;
        }

        if (mon.level >= MAX_LEVEL)
        {
            return false;
        }

        int cap = Mathf.Min(GetLevelCapForPhase(phase), MAX_LEVEL);

        if (mon.level >= cap)
        {
            return false;
        }

        int amount = BaseExpFor(source);

        if (source == ExpSource.Wild)
        {
            amount = ApplyWildDiminishing(mon, amount);
        }
        else
        {
            mon.wildStreak = 0;
        }

        mon.experience += amount;

        bool leveledUp = false;

        while (mon.level < cap)
        {
            int need = ExpToNextLevel(mon.level);

            if (mon.experience < need)
            {
                break;
            }

            mon.experience -= need;

            LevelUp(mon);

            leveledUp = true;

            if (mon.level >= MAX_LEVEL)
            {
                break;
            }
        }

        if (mon.level >= cap)
        {
            int need = ExpToNextLevel(mon.level);
            mon.experience = Mathf.Clamp(mon.experience, 0, Mathf.Max(0, need - 1));
        }

        return leveledUp;
    }

    public static void InitMovesForCurrentLevel(MonInstance mon)
    {
        if (mon?.species == null)
        {
            return;
        }

        mon.moves.Clear();

        LearnMovesUpToLevel(mon);
    }

    public static float GetExpNormalized(MonInstance mon)
    {
        if (mon == null)
        {
            return 0f;
        }

        int need = ExpToNextLevel(mon.level);

        if (need <= 0)
        {
            return 0f;
        }

        return Mathf.Clamp01(mon.experience / (float)need);
    }

    public static MonInstance Clone(MonInstance original)
    {
        if (original == null)
        {
            return null;
        }

        MonInstance copy = new MonInstance
        {
            species = original.species,
            level = original.level,
            currentHP = original.currentHP,
            experience = original.experience,
            moves = original.moves != null ? new List<MoveData>(original.moves) : new List<MoveData>()
        };

        return copy;
    }

    private static int ApplyWildDiminishing(MonInstance mon, int baseAmount)
    {
        float now = Time.time;

        const float RESET_AFTER_SECONDS = 25f;

        if (now - mon.lastWildExpTime > RESET_AFTER_SECONDS)
        {
            mon.wildStreak = 0;
        }

        mon.wildStreak++;
        mon.lastWildExpTime = now;

        float multiplier = 1f;

        if (mon.wildStreak >= 7)
        {
            multiplier = 0.3f;
        }
        else if (mon.wildStreak >= 4)
        {
            multiplier = 0.6f;
        }

        int gained = Mathf.RoundToInt(baseAmount * multiplier);

        return Mathf.Max(1, gained);
    }

    private static void LevelUp(MonInstance mon)
    {
        int oldMax = GetMaxHP(mon);

        mon.level++;

        int newMax = GetMaxHP(mon);

        mon.currentHP = Mathf.Min(mon.currentHP + newMax - oldMax, newMax);

        LearnMovesUpToLevel(mon);
        TryEvolveByRules(mon);
    }

    private static void TryEvolveByRules(MonInstance mon)
    {
        EvolutionRule[] rules = mon.species.evolutions;

        if (rules == null || rules.Length == 0)
        {
            return;
        }

        EvolutionRule best = null;
        int bestLevel = int.MinValue;

        for (int i = 0; i < rules.Length; i++)
        {
            EvolutionRule rule = rules[i];

            if (rule == null || rule.evolvesTo == null)
            {
                continue;
            }

            int requiredLevel = Mathf.Max(1, rule.evolveAtLevel);

            if (mon.level >= requiredLevel && requiredLevel > bestLevel)
            {
                best = rule;
                bestLevel = requiredLevel;
            }
        }

        if (best == null)
        {
            return;
        }

        SaveGameManager.RegisterOwnedSpecies(mon.species.name);

        mon.species = best.evolvesTo;

        SaveGameManager.RegisterOwnedSpecies(mon.species.name);

        ClampHP(mon);
        LearnMovesUpToLevel(mon);
    }

    private static void LearnMovesUpToLevel(MonInstance mon)
    {
        LearnableMove[] learnset = mon.species.learnableMoves;

        if (learnset == null)
        {
            return;
        }

        for (int i = 0; i < learnset.Length; i++)
        {
            LearnableMove entry = learnset[i];
            MoveData move = entry?.move;

            if (move == null)
            {
                continue;
            }

            if (entry.learnAtLevel <= mon.level && !mon.moves.Contains(move))
            {
                LearnMove(mon, move);
            }
        }
    }

    private static void LearnMove(MonInstance mon, MoveData move)
    {
        const int MAX_MOVES = 4;

        if (mon.moves.Count < MAX_MOVES)
        {
            mon.moves.Add(move);
        }
        else
        {
            mon.moves[0] = move;
        }
    }

    private static void ClampHP(MonInstance mon)
    {
        int max = GetMaxHP(mon);
        mon.currentHP = Mathf.Clamp(mon.currentHP, 0, max);
    }
}