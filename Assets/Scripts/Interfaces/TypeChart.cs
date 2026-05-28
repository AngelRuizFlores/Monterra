public static class TypeChart
{
    public static float GetMultiplier(MonType attack, MonType defend)
    {
        switch (attack)
        {
            case MonType.Fire:
                if (defend == MonType.Grass || defend == MonType.Shadow)
                {
                    return 2f;
                }

                if (defend == MonType.Water || defend == MonType.Earth)
                {
                    return 0.5f;
                }

                break;

            case MonType.Water:
                if (defend == MonType.Fire || defend == MonType.Earth)
                {
                    return 2f;
                }

                if (defend == MonType.Grass || defend == MonType.Light)
                {
                    return 0.5f;
                }

                break;

            case MonType.Grass:
                if (defend == MonType.Water || defend == MonType.Earth)
                {
                    return 2f;
                }

                if (defend == MonType.Fire || defend == MonType.Shadow)
                {
                    return 0.5f;
                }

                break;

            case MonType.Light:
                if (defend == MonType.Water || defend == MonType.Shadow)
                {
                    return 2f;
                }

                if (defend == MonType.Earth)
                {
                    return 0.5f;
                }

                break;

            case MonType.Earth:
                if (defend == MonType.Light || defend == MonType.Fire)
                {
                    return 2f;
                }

                if (defend == MonType.Water || defend == MonType.Grass)
                {
                    return 0.5f;
                }

                break;

            case MonType.Shadow:
                if (defend == MonType.Grass || defend == MonType.Light)
                {
                    return 2f;
                }

                if (defend == MonType.Fire)
                {
                    return 0.5f;
                }

                break;
        }

        return 1f;
    }

    public static string GetEffectText(float multiplier)
    {
        if (multiplier >= 2f)
        {
            return "It's super effective!";
        }

        if (multiplier <= 0.5f)
        {
            return "It's not very effective...";
        }

        return "";
    }
}