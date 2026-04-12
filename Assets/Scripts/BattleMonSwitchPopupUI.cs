using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleMonSwitchPopupUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Button cancelButton;

    [Header("List")]
    [SerializeField] private Transform contentParent;
    [SerializeField] private BattleMonSwitchOptionUI optionPrefab;

    private readonly List<BattleMonSwitchOptionUI> spawnedOptions = new();

    private Action<MonInstance> onSelected;
    private Action onCancelled;
    private bool isMandatory;

    public bool IsOpen => root != null && root.activeSelf;

    private void Awake()
    {
        if (cancelButton != null)
            cancelButton.onClick.AddListener(HandleCancelPressed);

        HideImmediate();
    }

    public void Show(
        string title,
        IReadOnlyList<MonInstance> options,
        bool mandatory,
        Action<MonInstance> onOptionSelected,
        Action onCancelPressed)
    {
        if (root == null)
        {
            Debug.LogError($"{nameof(BattleMonSwitchPopupUI)}: popup root is not assigned.");
            return;
        }

        if (contentParent == null)
        {
            Debug.LogError($"{nameof(BattleMonSwitchPopupUI)}: content parent is not assigned.");
            return;
        }

        if (optionPrefab == null)
        {
            Debug.LogError($"{nameof(BattleMonSwitchPopupUI)}: option prefab is not assigned.");
            return;
        }

        isMandatory = mandatory;
        onSelected = onOptionSelected;
        onCancelled = onCancelPressed;

        if (titleText != null)
            titleText.text = title ?? string.Empty;

        if (cancelButton != null)
            cancelButton.gameObject.SetActive(!isMandatory);

        RebuildOptions(options);
        root.SetActive(true);
    }

    public void Hide()
    {
        if (root != null)
            root.SetActive(false);

        ClearOptions();
        onSelected = null;
        onCancelled = null;
        isMandatory = false;
    }

    public void HideImmediate()
    {
        ClearOptions();

        if (root != null)
            root.SetActive(false);

        onSelected = null;
        onCancelled = null;
        isMandatory = false;
    }

    private void RebuildOptions(IReadOnlyList<MonInstance> options)
    {
        ClearOptions();

        if (options == null)
            return;

        for (int i = 0; i < options.Count; i++)
        {
            MonInstance mon = options[i];
            if (mon == null)
                continue;

            BattleMonSwitchOptionUI option = Instantiate(optionPrefab, contentParent);
            option.Bind(mon, HandleOptionSelected);
            spawnedOptions.Add(option);
        }
    }

    private void ClearOptions()
    {
        for (int i = 0; i < spawnedOptions.Count; i++)
        {
            if (spawnedOptions[i] != null)
                Destroy(spawnedOptions[i].gameObject);
        }

        spawnedOptions.Clear();
    }

    private void HandleOptionSelected(MonInstance selectedMon)
    {
        onSelected?.Invoke(selectedMon);
    }

    private void HandleCancelPressed()
    {
        if (isMandatory)
            return;

        Action cancelCallback = onCancelled;

        if (root != null)
            root.SetActive(false);

        ClearOptions();

        onSelected = null;
        onCancelled = null;
        isMandatory = false;

        cancelCallback?.Invoke();
    }
}