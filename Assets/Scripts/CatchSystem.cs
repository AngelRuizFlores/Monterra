using UnityEngine;

public static class CatchSystem
{
    private const float MinCatchChance = 5f;
    private const float MaxCatchChance = 95f;

    public static float CalculateCatchChance(MonInstance playerMon, MonInstance wildMon)
    {
        if (playerMon == null || wildMon == null || wildMon.species == null)
            return 0f;

        float chance = wildMon.species.baseCatchRate;

        chance += CalculateHpBonus(wildMon);
        chance += CalculateLevelBonus(playerMon, wildMon);
        chance += CalculateEvolutionModifier(wildMon.species.evolutionStage);

        return Mathf.Clamp(chance, MinCatchChance, MaxCatchChance);
    }

    public static bool TryCatch(MonInstance playerMon, MonInstance wildMon, out float finalChance, out float roll)
    {
        finalChance = CalculateCatchChance(playerMon, wildMon);
        roll = Random.Range(0f, 100f);
        return roll <= finalChance;
    }

    private static float CalculateHpBonus(MonInstance wildMon)
    {
        int maxHp = Mathf.Max(1, MonLevelSystem.GetMaxHP(wildMon));
        float hpRatio = Mathf.Clamp01((float)wildMon.currentHP / maxHp);

        if (hpRatio > 0.75f) return -20f;
        if (hpRatio > 0.40f) return 0f;
        if (hpRatio > 0.15f) return 15f;
        return 30f;
    }

    private static float CalculateLevelBonus(MonInstance playerMon, MonInstance wildMon)
    {
        int delta = playerMon.level - wildMon.level;
        return Mathf.Clamp(delta * 2f, -10f, 10f);
    }

    private static float CalculateEvolutionModifier(EvolutionStage stage)
    {
        switch (stage)
        {
            case EvolutionStage.Base:
                return 10f;

            case EvolutionStage.Stage1:
                return 0f;

            case EvolutionStage.Stage2:
                return -12f;

            default:
                return 0f;
        }
    }
}