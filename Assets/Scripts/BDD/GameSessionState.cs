using UnityEngine;

public static class GameSessionState
{
    public static bool IsEndGameActive { get; private set; }

    public static void BeginEndGame()
    {
        IsEndGameActive = true;
        BattleInteractionLock.SetBlocked(true);
    }

    public static void RestoreGlobalStateForMenu()
    {
        IsEndGameActive = false;
        BattleInteractionLock.SetBlocked(false);
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }
}