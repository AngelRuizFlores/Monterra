using System;

public static class BattleInteractionLock
{
    public static bool IsBlocked { get; private set; }

    public static event Action<bool> OnChanged;

    public static void SetBlocked(bool blocked)
    {
        if (IsBlocked == blocked)
        {
            return;
        }

        IsBlocked = blocked;
        OnChanged?.Invoke(IsBlocked);
    }
}