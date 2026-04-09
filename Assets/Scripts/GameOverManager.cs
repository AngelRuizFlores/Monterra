using UnityEngine;

public class GameOverManager : MonoBehaviour
{
    [SerializeField] private MainMenuLoader mainMenuLoader;

    public void OnGameOver()
    {
        if (mainMenuLoader == null)
        {
            Debug.LogError($"{nameof(GameOverManager)}: falta asignar {nameof(MainMenuLoader)}.", this);
            return;
        }

        mainMenuLoader.LoadMainMenu();
    }
}