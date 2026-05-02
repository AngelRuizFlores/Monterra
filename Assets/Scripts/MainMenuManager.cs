using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject creditsPanel;

    [Header("Scene Names")]
    [SerializeField] private string gameplaySceneName = "SampleScene";

    private bool isLoading;

    public void OnNewGamePressed()
    {
        if (isLoading)
            return;

        GameStartMode.LoadGame = false;
        StartCoroutine(LoadSceneWithFade(gameplaySceneName));
    }

    public void OnContinuePressed()
    {
        if (isLoading)
            return;

        if (!SaveGameManager.HasSave())
        {
            Debug.LogWarning("No save data found.");
            return;
        }

        GameStartMode.LoadGame = true;
        StartCoroutine(LoadSceneWithFade(gameplaySceneName));
    }

    public void OnExitPressed()
    {
        if (isLoading)
            return;

        Debug.Log("Closing game.");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OnOptionsPressed()
    {
        if (isLoading)
            return;

        StartCoroutine(OpenOptionsWithFade());
    }

    public void OnBackFromOptionsPressed()
    {
        if (isLoading)
            return;

        StartCoroutine(CloseOptionsWithFade());
    }

    public void OnCreditsPressed()
    {
        if (isLoading)
            return;

        StartCoroutine(OpenCreditsWithFade());
    }

    public void OnBackFromCreditsPressed()
    {
        if (isLoading)
            return;

        StartCoroutine(CloseCreditsWithFade());
    }

    private IEnumerator LoadSceneWithFade(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("Scene name is empty.");
            yield break;
        }

        isLoading = true;

        if (FadeController.Instance != null)
            yield return FadeController.Instance.FadeOut();
        else
            Debug.LogWarning("FadeController was not found in the scene.");

        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator OpenOptionsWithFade()
    {
        if (FadeController.Instance != null)
            yield return FadeController.Instance.FadeOut();

        mainMenuPanel.SetActive(false);
        optionsPanel.SetActive(true);

        if (FadeController.Instance != null)
            yield return FadeController.Instance.FadeIn();
    }

    private IEnumerator CloseOptionsWithFade()
    {
        if (FadeController.Instance != null)
            yield return FadeController.Instance.FadeOut();

        optionsPanel.SetActive(false);
        mainMenuPanel.SetActive(true);

        if (FadeController.Instance != null)
            yield return FadeController.Instance.FadeIn();
    }

    private IEnumerator OpenCreditsWithFade()
    {
        if (FadeController.Instance != null)
            yield return FadeController.Instance.FadeOut();

        mainMenuPanel.SetActive(false);
        creditsPanel.SetActive(true);

        if (FadeController.Instance != null)
            yield return FadeController.Instance.FadeIn();
    }

    private IEnumerator CloseCreditsWithFade()
    {
        if (FadeController.Instance != null)
            yield return FadeController.Instance.FadeOut();

        creditsPanel.SetActive(false);
        mainMenuPanel.SetActive(true);

        if (FadeController.Instance != null)
            yield return FadeController.Instance.FadeIn();
    }
}