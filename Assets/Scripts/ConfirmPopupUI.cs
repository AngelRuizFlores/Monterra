using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ConfirmPopupUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    private Action onYes;

    void Awake()
    {
        if (yesButton != null) yesButton.onClick.AddListener(Yes);
        if (noButton != null) noButton.onClick.AddListener(Hide);
        Hide();
    }

    public void Show(string message, Action onYesCallback)
    {
        onYes = onYesCallback;
        if (messageText != null) messageText.text = message;
        if (root != null) root.SetActive(true);
        else gameObject.SetActive(true);
    }

    public void Hide()
    {
        onYes = null;
        if (root != null) root.SetActive(false);
        else gameObject.SetActive(false);
    }

    void Yes()
    {
        var cb = onYes;
        Hide();
        cb?.Invoke();
    }
}