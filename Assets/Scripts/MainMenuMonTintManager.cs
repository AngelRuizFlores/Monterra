using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuMonTintManager : MonoBehaviour
{
    [Serializable]
    public class MenuMonImage
    {
        public MonSpecies species;
        public Image image;
    }

    [Header("Menu Mons")]
    [SerializeField] private MenuMonImage[] mons;

    [Header("Type Tint Colors")]
    [SerializeField] private Color waterTint = new Color32(70, 170, 255, 180);
    [SerializeField] private Color fireTint = new Color32(255, 90, 60, 180);
    [SerializeField] private Color grassTint = new Color32(80, 210, 90, 180);
    [SerializeField] private Color lightTint = new Color32(255, 215, 80, 180);
    [SerializeField] private Color shadowTint = new Color32(140, 60, 200, 180);
    [SerializeField] private Color earthTint = new Color32(170, 120, 60, 180);

    private void Start()
    {
        ApplyMenuColors();
    }

    private void OnEnable()
    {
        ApplyMenuColors();
    }

    private void ApplyMenuColors()
    {
        HashSet<string> ownedSpeciesIds = SaveGameManager.GetOwnedSpeciesIds();

        for (int i = 0; i < mons.Length; i++)
        {
            MenuMonImage entry = mons[i];

            if (entry == null || entry.image == null || entry.species == null)
                continue;

            bool isOwned = ownedSpeciesIds.Contains(entry.species.name);

            entry.image.color = isOwned
                ? Color.white
                : GetTintColor(entry.species.type);
        }
    }

    private HashSet<string> GetOwnedSpeciesIds()
    {
        HashSet<string> result = new HashSet<string>();

        SaveData data = SaveGameManager.Load();

        if (data == null || data.team == null)
            return result;

        for (int i = 0; i < data.team.Count; i++)
        {
            MonSaveData mon = data.team[i];

            if (mon == null || string.IsNullOrWhiteSpace(mon.speciesId))
                continue;

            result.Add(mon.speciesId);
        }

        return result;
    }

    private Color GetTintColor(MonType type)
    {
        switch (type)
        {
            case MonType.Water:
                return waterTint;

            case MonType.Fire:
                return fireTint;

            case MonType.Grass:
                return grassTint;

            case MonType.Light:
                return lightTint;

            case MonType.Shadow:
                return shadowTint;

            case MonType.Earth:
                return earthTint;

            default:
                return Color.white;
        }
    }
}