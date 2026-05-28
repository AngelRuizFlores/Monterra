using System;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public struct BattleBiomeBackgroundEntry
{
    public BattleBiome biome;
    public Sprite background;
}

public sealed class BattleBackgroundSelector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image targetImage;

    [Header("Backgrounds")]
    [SerializeField] private Sprite defaultBackground;
    [SerializeField] private BattleBiomeBackgroundEntry[] entries;

    public void ApplyBackground(BattleBiome biome)
    {
        if (targetImage == null)
        {
            Debug.LogError($"{nameof(BattleBackgroundSelector)}: targetImage no asignada.", this);
            return;
        }

        Sprite selected = defaultBackground;

        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i].biome != biome)
            {
                continue;
            }

            if (entries[i].background != null)
            {
                selected = entries[i].background;
            }

            break;
        }

        targetImage.sprite = selected;
        targetImage.preserveAspect = false;
    }
}