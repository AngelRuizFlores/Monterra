using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject optionsPanel;

    [Header("Scene Names")]
    [SerializeField] private string gameplaySceneName = "SampleScene";

    private bool isLoading;

    public void OnNewGamePressed()
    {
        if (isLoading)
            return;

        StartCoroutine(LoadSceneWithFade(gameplaySceneName));
    }

    public void OnContinuePressed()
    {
        if (isLoading)
            return;

        if (!HasSaveData())
        {
            Debug.LogWarning("No save data found.");
            return;
        }

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

    private IEnumerator LoadSceneWithFade(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("Scene name is empty.");
            yield break;
        }

        isLoading = true;

        if (FadeController.Instance != null)
        {
            yield return FadeController.Instance.FadeOut();
        }
        else
        {
            Debug.LogWarning("FadeController was not found in the scene.");
        }

        SceneManager.LoadScene(sceneName);
    }

    private bool HasSaveData()
    {
        return PlayerPrefs.HasKey("HasSave");
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
}