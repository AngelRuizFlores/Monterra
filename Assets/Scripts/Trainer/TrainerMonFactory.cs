using System.Collections.Generic;
using UnityEngine;

public static class TrainerMonFactory
{
    public static MonInstance Create(TrainerMonDefinition definition)
    {
        if (!definition.IsValid(out _))
            return null;

        MonInstance mon = new MonInstance
        {
            species = definition.Species,
            level = Mathf.Max(1, definition.Level),
            experience = 0,
            moves = BuildMoves(definition.Species, definition.Level)
        };

        mon.currentHP = MonLevelSystem.GetMaxHP(mon);
        return mon;
    }

    public static List<MonInstance> CreateRoster(TrainerDefinition definition)
    {
        List<MonInstance> roster = new List<MonInstance>();

        if (definition == null || !definition.IsValid(out _))
            return roster;

        IReadOnlyList<TrainerMonDefinition> team = definition.Team;
        for (int i = 0; i < team.Count; i++)
        {
            MonInstance mon = Create(team[i]);
            if (mon != null)
                roster.Add(mon);
        }

        return roster;
    }

    private static List<MoveData> BuildMoves(MonSpecies species, int level)
    {
        List<MoveData> result = new List<MoveData>();
        if (species == null || species.learnableMoves == null)
            return result;

        for (int i = 0; i < species.learnableMoves.Length; i++)
        {
            LearnableMove learnable = species.learnableMoves[i];
            if (learnable.move == null)
                continue;

            if (learnable.learnAtLevel > level)
                continue;

            result.Add(learnable.move);
        }

        int trimStart = Mathf.Max(0, result.Count - 4);
        if (trimStart > 0)
            result.RemoveRange(0, trimStart);

        return result;
    }
}