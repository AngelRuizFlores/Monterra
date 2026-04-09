using UnityEngine;

public static class GameSessionState
{
    public static void RestoreGlobalStateForMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }
}