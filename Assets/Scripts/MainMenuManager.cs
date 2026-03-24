using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
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
}