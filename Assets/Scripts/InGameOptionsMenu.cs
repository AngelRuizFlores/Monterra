using UnityEngine;
using UnityEngine.InputSystem;

public sealed class InGameOptionsMenu : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject hudToHide;

    [Header("Navigation")]
    [SerializeField] private MainMenuLoader mainMenuLoader;

    [Header("Behaviour")]
    [SerializeField] private bool pauseAudioListener = false;

    public bool IsOpen => optionsPanel != null && optionsPanel.activeSelf;

    private bool isBusy;

    private void Awake()
    {
        if (optionsPanel != null)
            optionsPanel.SetActive(false);
    }

    private void Update()
    {
        if (isBusy)
            return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (IsOpen)
                ResumeGame();
            else
                OpenOptions();
        }
    }

    public void OpenOptions()
    {
        if (isBusy)
            return;

        if (optionsPanel == null)
        {
            Debug.LogError($"{nameof(InGameOptionsMenu)}: falta asignar el panel de opciones.", this);
            return;
        }

        optionsPanel.SetActive(true);

        if (hudToHide != null)
            hudToHide.SetActive(false);

        SetGamePaused(true);
    }

    public void ExitOptions()
    {
        ResumeGame();
    }

    public void ResumeGame()
    {
        if (isBusy)
            return;

        if (optionsPanel != null)
            optionsPanel.SetActive(false);

        if (hudToHide != null)
            hudToHide.SetActive(true);

        SetGamePaused(false);
    }

    public void GoToMenu()
    {
        if (isBusy)
            return;

        if (mainMenuLoader == null)
        {
            Debug.LogError($"{nameof(InGameOptionsMenu)}: falta asignar {nameof(MainMenuLoader)}.", this);
            return;
        }

        isBusy = true;

        if (optionsPanel != null)
            optionsPanel.SetActive(false);

        if (hudToHide != null)
            hudToHide.SetActive(true);

        SetGamePaused(false);
        mainMenuLoader.LoadMainMenu();
    }

    private void SetGamePaused(bool paused)
    {
        Time.timeScale = paused ? 0f : 1f;
        SetAudioPaused(paused);
    }

    private void SetAudioPaused(bool paused)
    {
        if (!pauseAudioListener)
            return;

        AudioListener.pause = paused;
    }

    private void OnDisable()
    {
        if (!isBusy)
        {
            Time.timeScale = 1f;
            SetAudioPaused(false);
        }
    }

    private void OnDestroy()
    {
        if (!isBusy)
        {
            Time.timeScale = 1f;
            SetAudioPaused(false);
        }
    }
}