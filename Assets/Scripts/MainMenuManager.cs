using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject optionsPanel;
    [Header("Scene Names")]
    [SerializeField] private string gameplaySceneName = "SampleScene";

    private bool isLoading = false;

    // -------------------------
    // NEW GAME
    // -------------------------
    public void OnNewGamePressed()
    {
        if (isLoading) return;

        StartCoroutine(LoadSceneWithFade(gameplaySceneName));
    }

    // -------------------------
    // CONTINUE (opcional futuro)
    // -------------------------
    public void OnContinuePressed()
    {
        if (isLoading) return;

        if (!HasSaveData())
        {
            Debug.LogWarning("No hay partida guardada.");
            return;
        }

        StartCoroutine(LoadSceneWithFade(gameplaySceneName));
    }

    // -------------------------
    // EXIT GAME
    // -------------------------
    public void OnExitPressed()
    {
        if (isLoading) return;

        Debug.Log("Saliendo del juego...");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // -------------------------
    // LOAD SCENE WITH FADE
    // -------------------------
    private IEnumerator LoadSceneWithFade(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("Nombre de escena vacío.");
            yield break;
        }

        isLoading = true;

        if (FadeController.Instance != null)
        {
            yield return FadeController.Instance.FadeOut();
        }
        else
        {
            Debug.LogWarning("No se encontró FadeController en la escena.");
        }

        SceneManager.LoadScene(sceneName);
    }

    // -------------------------
    // SAVE CHECK (placeholder)
    // -------------------------
    private bool HasSaveData()
    {
        // Cambia esto cuando tengas sistema de guardado real
        return PlayerPrefs.HasKey("HasSave");
    }

    public void OnOptionsPressed()
    {
        if (isLoading) return;
        StartCoroutine(OpenOptionsWithFade());
    }

    public void OnBackFromOptionsPressed()
    {
        if (isLoading) return;
        StartCoroutine(CloseOptionsWithFade());
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