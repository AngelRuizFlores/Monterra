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

    private readonly List<BattleMonSwitchOptionUI> spawnedOptions = new List<BattleMonSwitchOptionUI>();

    private Action<MonInstance> onSelected;
    private Action onCancelled;
    private bool mandatory;

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
        bool isMandatory,
        Action<MonInstance> onOptionSelected,
        Action onCancelPressed)
    {
        if (root == null)
        {
            Debug.LogError($"{nameof(BattleMonSwitchPopupUI)}: Falta asignar el root del popup.");
            return;
        }

        if (contentParent == null)
        {
            Debug.LogError($"{nameof(BattleMonSwitchPopupUI)}: Falta asignar el contentParent.");
            return;
        }

        if (optionPrefab == null)
        {
            Debug.LogError($"{nameof(BattleMonSwitchPopupUI)}: Falta asignar el prefab de opción.");
            return;
        }

        mandatory = isMandatory;
        onSelected = onOptionSelected;
        onCancelled = onCancelPressed;

        if (titleText != null)
            titleText.text = title ?? string.Empty;

        if (cancelButton != null)
            cancelButton.gameObject.SetActive(!mandatory);

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
        mandatory = false;
    }

    public void HideImmediate()
    {
        ClearOptions();

        if (root != null)
            root.SetActive(false);

        onSelected = null;
        onCancelled = null;
        mandatory = false;
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

            BattleMonSwitchOptionUI item = Instantiate(optionPrefab, contentParent);
            item.Bind(mon, HandleOptionSelected);
            spawnedOptions.Add(item);
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
        if (mandatory)
            return;

        Action cancelCallback = onCancelled;

        if (root != null)
            root.SetActive(false);

        ClearOptions();

        onSelected = null;
        onCancelled = null;
        mandatory = false;

        cancelCallback?.Invoke();
    }
}