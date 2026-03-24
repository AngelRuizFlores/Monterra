using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleUI : MonoBehaviour
{
    [Header("Enemy")]
    [SerializeField] private Image enemySprite;
    [SerializeField] private Image enemyTypeIcon;
    [SerializeField] private TMP_Text enemyName;
    [SerializeField] private TMP_Text enemyLevel;
    [SerializeField] private Slider enemyHP;

    [Header("Player")]
    [SerializeField] private Image playerSprite;
    [SerializeField] private Image playerTypeIcon;
    [SerializeField] private TMP_Text playerName;
    [SerializeField] private TMP_Text playerLevel;
    [SerializeField] private Slider playerHP;
    [SerializeField] private Slider playerExpSlider;

    [Header("Text")]
    [SerializeField] private TMP_Text battleText;

    [Header("Actions")]
    [SerializeField] private Button switchMonButton;

    [Header("Battle FX Anchors")]
    [SerializeField] private RectTransform playerAttackOrigin;
    [SerializeField] private RectTransform enemyAttackOrigin;
    [SerializeField] private RectTransform playerHitPoint;
    [SerializeField] private RectTransform enemyHitPoint;
    [SerializeField] private RectTransform effectsContainer;

    private Action onSwitchRequested;

    public void BindSwitchAction(Action callback)
    {
        onSwitchRequested = callback;

        if (switchMonButton == null)
            return;

        switchMonButton.onClick.RemoveListener(HandleSwitchButtonPressed);
        switchMonButton.onClick.AddListener(HandleSwitchButtonPressed);
    }

    public void SetSwitchButtonInteractable(bool interactable)
    {
        if (switchMonButton != null)
            switchMonButton.interactable = interactable;
    }

    public void SetPlayerExp(MonInstance mon)
    {
        if (playerExpSlider == null || mon == null)
            return;

        float normalized = MonLevelSystem.GetExpNormalized(mon);
        playerExpSlider.value = normalized * 100f;
    }

    public void SetText(string msg)
    {
        if (battleText != null)
            battleText.text = msg ?? string.Empty;
    }

    public void ShowWildMon(WildMon wild)
    {
        if (wild == null || wild.instance == null || wild.instance.species == null)
            return;

        MonInstance inst = wild.instance;
        MonSpecies sp = inst.species;

        if (enemySprite != null) enemySprite.sprite = sp.frontSprite;
        if (enemyName != null) enemyName.text = sp.monName;
        if (enemyLevel != null) enemyLevel.text = $"Lvl {inst.level}";
        if (enemyTypeIcon != null) enemyTypeIcon.sprite = sp.typeSprite;

        int maxHP = MonLevelSystem.GetMaxHP(inst);
        if (enemyHP != null)
        {
            enemyHP.maxValue = maxHP;
            enemyHP.value = Mathf.Clamp(inst.currentHP, 0, maxHP);
        }
    }

    public void ShowPlayerMon(PlayerMon player)
    {
        if (player == null)
            return;

        player.InitIfNeeded();

        if (player.instance == null || player.instance.species == null)
            return;

        MonInstance inst = player.instance;
        MonSpecies sp = inst.species;

        if (playerSprite != null) playerSprite.sprite = sp.backSprite;
        if (playerName != null) playerName.text = sp.monName;
        if (playerLevel != null) playerLevel.text = $"Lvl {inst.level}";
        if (playerTypeIcon != null) playerTypeIcon.sprite = sp.typeSprite;

        int maxHP = MonLevelSystem.GetMaxHP(inst);
        if (playerHP != null)
        {
            playerHP.maxValue = maxHP;
            playerHP.value = Mathf.Clamp(inst.currentHP, 0, maxHP);
        }

        SetPlayerExp(inst);
    }

    public void UpdateEnemyHP(int current, int max)
    {
        if (enemyHP == null)
            return;

        enemyHP.maxValue = Mathf.Max(1, max);
        enemyHP.value = Mathf.Clamp(current, 0, (int)enemyHP.maxValue);
    }

    public void UpdatePlayerHP(int current, int max)
    {
        if (playerHP == null)
            return;

        playerHP.maxValue = Mathf.Max(1, max);
        playerHP.value = Mathf.Clamp(current, 0, (int)playerHP.maxValue);
    }

    private void HandleSwitchButtonPressed()
    {
        onSwitchRequested?.Invoke();
    }

   public Vector2 GetPlayerAttackOrigin() => playerAttackOrigin != null ? playerAttackOrigin.anchoredPosition : Vector2.zero;
    public Vector2 GetEnemyAttackOrigin() => enemyAttackOrigin != null ? enemyAttackOrigin.anchoredPosition : Vector2.zero;
    public Vector2 GetPlayerHitPoint() => playerHitPoint != null ? playerHitPoint.anchoredPosition : Vector2.zero;
    public Vector2 GetEnemyHitPoint() => enemyHitPoint != null ? enemyHitPoint.anchoredPosition : Vector2.zero;
    public RectTransform GetEffectsContainer() => effectsContainer;

}