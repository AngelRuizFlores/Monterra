using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TrainerDefinition", menuName = "Game/Trainer Definition")]
public sealed class TrainerDefinition : ScriptableObject
{
    [SerializeField] private string trainerName;
    [SerializeField] private Sprite trainerSprite;
    [SerializeField] private TrainerMonDefinition[] team = Array.Empty<TrainerMonDefinition>();

    public string TrainerName => trainerName;
    public Sprite TrainerSprite => trainerSprite;
    public IReadOnlyList<TrainerMonDefinition> Team => team;

    public bool IsValid(out string error)
    {
        if (string.IsNullOrWhiteSpace(trainerName))
        {
            error = $"{name}: el trainer debe tener nombre.";
            return false;
        }

        if (team == null || team.Length <= 0)
        {
            error = $"{name}: el trainer debe tener entre 1 y 6 mons.";
            return false;
        }

        if (team.Length > PlayerTeam.MAX_TEAM)
        {
            error = $"{name}: el trainer no puede tener más de {PlayerTeam.MAX_TEAM} mons.";
            return false;
        }

        for (int i = 0; i < team.Length; i++)
        {
            if (!team[i].IsValid(out error))
            {
                error = $"{name} [slot {i}]: {error}";
                return false;
            }
        }

        error = null;
        return true;
    }
}

[Serializable]
public struct TrainerMonDefinition
{
    [SerializeField] private MonSpecies species;
    [SerializeField] [Range(1, 100)] private int level;

    public MonSpecies Species => species;
    public int Level => Mathf.Max(1, level);

    public bool IsValid(out string error)
    {
        if (species == null)
        {
            error = "la especie no puede ser null.";
            return false;
        }

        if (level <= 0)
        {
            error = "el nivel debe ser mayor que 0.";
            return false;
        }

        error = null;
        return true;
    }
}