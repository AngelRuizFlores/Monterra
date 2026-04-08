using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private bool useFade = true;

    public void OnGameOver()
    {
        if (!gameObject.activeInHierarchy)
        {
            LoadMainMenuImmediate();
            return;
        }

        StartCoroutine(OnGameOverCoroutine());
    }

    private IEnumerator OnGameOverCoroutine()
    {
        Time.timeScale = 1f;

        if (useFade && FadeController.Instance != null)
            yield return FadeController.Instance.FadeOut();

        LoadMainMenuImmediate();
    }

    private void LoadMainMenuImmediate()
    {
        if (string.IsNullOrWhiteSpace(mainMenuSceneName))
        {
            Debug.LogError($"{nameof(GameOverManager)}: el nombre de la escena principal está vacío.", this);
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
        {
            Debug.LogError($"{nameof(GameOverManager)}: la escena '{mainMenuSceneName}' no está incluida en Build Settings.", this);
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}