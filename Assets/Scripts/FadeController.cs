using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class FadeController : MonoBehaviour
{
    public static FadeController Instance { get; private set; }

    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 1.5f;

    private Coroutine currentFadeCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        SetAlpha(1f);
        SetRaycastBlocking(true);
        StartFadeIn();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartFadeIn();
    }

    public IEnumerator FadeOut()
    {
        yield return StartFade(1f);
    }

    public IEnumerator FadeIn()
    {
        yield return StartFade(0f);
    }

    public IEnumerator FadeOutThenIn(float holdBlackTime = 0f)
    {
        yield return StartFade(1f);

        if (holdBlackTime > 0f)
            yield return new WaitForSecondsRealtime(holdBlackTime);

        yield return StartFade(0f);
    }

    public void StartFadeIn()
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (currentFadeCoroutine != null)
            StopCoroutine(currentFadeCoroutine);

        currentFadeCoroutine = StartCoroutine(FadeRoutine(0f));
    }

    private IEnumerator StartFade(float targetAlpha)
    {
        if (!gameObject.activeInHierarchy)
            yield break;

        if (currentFadeCoroutine != null)
            StopCoroutine(currentFadeCoroutine);

        currentFadeCoroutine = StartCoroutine(FadeRoutine(targetAlpha));
        yield return currentFadeCoroutine;
    }

    private IEnumerator FadeRoutine(float targetAlpha)
    {
        if (fadeImage == null)
        {
            Debug.LogError("FadeController: falta asignar fadeImage.");
            yield break;
        }

        float startAlpha = fadeImage.color.a;
        float elapsed = 0f;

        SetRaycastBlocking(true);

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            SetAlpha(alpha);
            yield return null;
        }

        SetAlpha(targetAlpha);
        SetRaycastBlocking(targetAlpha > 0.01f);
        currentFadeCoroutine = null;
    }

    private void SetAlpha(float alpha)
    {
        if (fadeImage == null)
            return;

        Color c = fadeImage.color;
        c.a = alpha;
        fadeImage.color = c;
    }

    private void SetRaycastBlocking(bool shouldBlock)
    {
        if (fadeImage == null)
            return;

        fadeImage.raycastTarget = shouldBlock;
    }
}