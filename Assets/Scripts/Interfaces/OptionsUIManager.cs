using UnityEngine;

public class OptionsUIManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject mainMenuPanel;

    [Header("Context")]
    [SerializeField] private bool canOpenWithEscapeInGameplay = true;
    [SerializeField] private bool pauseGameWhenOpen = true;

    private bool isOpen;

    private void Start()
    {
        if (optionsPanel != null)
        {
            optionsPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (!canOpenWithEscapeInGameplay)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isOpen)
            {
                CloseOptions();
            }
            else
            {
                OpenOptionsFromGameplay();
            }
        }
    }

    public void OpenOptionsFromMenu()
    {
        isOpen = true;

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false);
        }

        if (optionsPanel != null)
        {
            optionsPanel.SetActive(true);
        }
    }

    public void OpenOptionsFromGameplay()
    {
        isOpen = true;

        if (optionsPanel != null)
        {
            optionsPanel.SetActive(true);
        }

        if (pauseGameWhenOpen)
        {
            Time.timeScale = 0f;
        }
    }

    public void CloseOptions()
    {
        isOpen = false;

        if (optionsPanel != null)
        {
            optionsPanel.SetActive(false);
        }

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }

        if (pauseGameWhenOpen)
        {
            Time.timeScale = 1f;
        }
    }

    public bool IsOpen()
    {
        return isOpen;
    }
}