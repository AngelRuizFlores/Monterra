using UnityEngine;

public sealed class WinGameManager : MonoBehaviour
{
    [SerializeField] private MainMenuLoader mainMenuLoader;

    public void OnWin()
    {
        if (mainMenuLoader == null)
        {
            Debug.LogError($"{nameof(WinGameManager)}: falta asignar {nameof(MainMenuLoader)}.", this);
            return;
        }

        mainMenuLoader.LoadMainMenu();
    }
}