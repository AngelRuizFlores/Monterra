using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class MainMenuLoader : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private bool useFade = true;

    [Header("Gameplay UI To Hide")]
    [SerializeField] private GameObject hudCanvas;
    [SerializeField] private GameObject battleCanvas;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject chooseCanvas;

    private bool isLoading;

    public void LoadMainMenu()
    {
        if (isLoading)
        {
            return;
        }

        StartCoroutine(LoadMainMenuCoroutine());
    }

    private void HideGameplayUI()
    {
        if (optionsPanel != null)
        {
            optionsPanel.SetActive(false);
        }

        if (battleCanvas != null)
        {
            battleCanvas.SetActive(false);
        }

        if (chooseCanvas != null)
        {
            chooseCanvas.SetActive(false);
        }

        if (hudCanvas != null)
        {
            hudCanvas.SetActive(false);
        }
    }

   private static void RestoreGlobalState()
    {
        GameSessionState.RestoreGlobalStateForMenu();
    }

    private IEnumerator LoadMainMenuCoroutine()
    {
        isLoading = true;
        HideGameplayUI();
        RestoreGlobalState();

        if (FadeController.Instance != null && useFade)
        {
            yield return FadeController.Instance.FadeOut();
        }

        Debug.Log("Cargando MainMenu...");

        SceneManager.LoadScene(mainMenuSceneName);
    }
}