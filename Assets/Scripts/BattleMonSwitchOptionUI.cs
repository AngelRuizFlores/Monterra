using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleMonSwitchOptionUI : MonoBehaviour
{
    [SerializeField] private Button selectButton;
    [SerializeField] private Image monSprite;
    [SerializeField] private Image typeIcon;
    [SerializeField] private TMP_Text monName;
    [SerializeField] private TMP_Text monLevel;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private Slider hpSlider;

    private MonInstance boundMon;
    private Action<MonInstance> onSelected;

    private void Awake()
    {
        if (selectButton != null)
            selectButton.onClick.AddListener(HandlePressed);
    }

    public void Bind(MonInstance mon, Action<MonInstance> callback)
    {
        boundMon = mon;
        onSelected = callback;

        if (boundMon == null || boundMon.species == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        if (monSprite != null) monSprite.sprite = boundMon.species.backSprite;
        if (typeIcon != null) typeIcon.sprite = boundMon.species.typeSprite;
        if (monName != null) monName.text = boundMon.species.monName;
        if (monLevel != null) monLevel.text = $"Lvl {boundMon.level}";

        int maxHP = MonLevelSystem.GetMaxHP(boundMon);
        int currentHP = Mathf.Clamp(boundMon.currentHP, 0, maxHP);

        if (hpSlider != null)
        {
            hpSlider.maxValue = Mathf.Max(1, maxHP);
            hpSlider.value = currentHP;
        }

        if (hpText != null)
            hpText.text = $"{currentHP}/{Mathf.Max(1, maxHP)}";

        if (selectButton != null)
            selectButton.interactable = currentHP > 0;
    }

    private void HandlePressed()
    {
        if (boundMon == null || boundMon.currentHP <= 0)
            return;

        onSelected?.Invoke(boundMon);
    }
}