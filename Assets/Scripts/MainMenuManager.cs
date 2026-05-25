using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject creditsPanel;

    [Header("Buttons")]
    [SerializeField] private GameObject continueButton;

    [Header("Scene Names")]
    [SerializeField] private string gameplaySceneName = "SampleScene";

    private bool isLoading;

    private const string Confirm = "Confirm";
    private const string Exit = "Exit";

    private void Start()
    {
        //PlayerPrefs.DeleteKey("OwnedSpeciesIds");
        //PlayerPrefs.Save();
        //Debug.Log("Owned mons reset.");
        RefreshContinueButton();
    }

    private void OnEnable()
    {
        RefreshContinueButton();
    }

    private void RefreshContinueButton()
    {
        if (continueButton != null)
            continueButton.SetActive(SaveGameManager.HasPlayableSave());
    }

    public void OnNewGamePressed()
    {
        if (isLoading)
            return;

        SaveGameManager.DeleteSave();
        GameStartMode.LoadGame = false;

        if (SoundManager.Instance != null)
            SoundManager.Instance.Play(Confirm, false);

        StartCoroutine(LoadSceneWithFade(gameplaySceneName));
    }

    public void OnContinuePressed()
    {
        if (isLoading)
            return;

        if (!SaveGameManager.HasPlayableSave())
        {
            Debug.LogWarning("No playable save data found.");
            RefreshContinueButton();
            return;
        }

        GameStartMode.LoadGame = true;

        if (SoundManager.Instance != null)
            SoundManager.Instance.Play(Confirm, false);

        StartCoroutine(LoadSceneWithFade(gameplaySceneName));
    }

    public void OnExitPressed()
    {
        if (isLoading)
            return;

        if (SoundManager.Instance != null)
            SoundManager.Instance.Play(Exit, false);

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

        if (SoundManager.Instance != null)
            SoundManager.Instance.Play(Confirm, false);

        StartCoroutine(OpenOptionsWithFade());
    }

    public void OnBackFromOptionsPressed()
    {
        if (isLoading)
            return;

        GameStartMode.LoadGame = false;

        if (SoundManager.Instance != null)
            SoundManager.Instance.Play(Exit, false);

        StartCoroutine(CloseOptionsWithFade());
    }

    public void OnCreditsPressed()
    {
        if (isLoading)
            return;

        GameStartMode.LoadGame = false;

        if (SoundManager.Instance != null)
            SoundManager.Instance.Play(Confirm, false);

        StartCoroutine(OpenCreditsWithFade());
    }

    public void OnBackFromCreditsPressed()
    {
        if (isLoading)
            return;

        if (SoundManager.Instance != null)
            SoundManager.Instance.Play(Exit, false);

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
        RefreshContinueButton();

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
        RefreshContinueButton();

        if (FadeController.Instance != null)
            yield return FadeController.Instance.FadeIn();
    }
}