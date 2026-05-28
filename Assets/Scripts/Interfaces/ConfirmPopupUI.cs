using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConfirmPopupUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    private Action onYes;

    private void Awake()
    {
        if (yesButton != null)
        {
            yesButton.onClick.AddListener(Yes);
        }

        if (noButton != null)
        {
            noButton.onClick.AddListener(Hide);
        }

        Hide();
    }

    public void Show(string message, Action onYesCallback)
    {
        onYes = onYesCallback;

        if (messageText != null)
        {
            messageText.text = message;
        }

        if (root != null)
        {
            root.SetActive(true);
            return;
        }

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        onYes = null;

        if (root != null)
        {
            root.SetActive(false);
            return;
        }

        gameObject.SetActive(false);
    }

    private void Yes()
    {
        Action callback = onYes;

        Hide();

        callback?.Invoke();
    }
}