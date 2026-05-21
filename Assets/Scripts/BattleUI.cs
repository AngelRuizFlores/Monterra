using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleUI : MonoBehaviour
{
    [Header("Enemy")]
    [SerializeField] private Image enemySprite;
    [SerializeField] private TextMeshProUGUI enemyNameText;
    [SerializeField] private TextMeshProUGUI enemyLevelText;
    [SerializeField] private Image enemyTypeIcon;
    [SerializeField] private Slider enemyHP;

    [Header("Player")]
    [SerializeField] private Image playerSprite;
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI playerLevelText;
    [SerializeField] private Image playerTypeIcon;
    [SerializeField] private Slider playerHP;
    [SerializeField] private Slider playerEXP;

    [Header("Actions")]
    [SerializeField] private Button switchButton;
    [SerializeField] private TextMeshProUGUI battleText;

    [Header("VFX")]
    [SerializeField] private RectTransform effectsContainer;
    [SerializeField] private RectTransform playerAttackOrigin;
    [SerializeField] private RectTransform enemyAttackOrigin;
    [SerializeField] private RectTransform playerHitPoint;
    [SerializeField] private RectTransform enemyHitPoint;

     private const string Confirm = "Confirm";
     private const string Exit = "Exit";
      private const string Switch = "Switch";


    private System.Action onSwitchPressed;

    public void BindSwitchAction(System.Action action)
    {
        onSwitchPressed = action;

        if (switchButton == null)
            return;

        switchButton.onClick.RemoveAllListeners();
        switchButton.onClick.AddListener(() => onSwitchPressed?.Invoke());
    }

    public void ShowWildMon(WildMon wild)
    {
        if (wild == null || wild.instance == null)
        {
            ClearEnemyMon();
            return;
        }

        ShowEnemyMon(wild.instance);
    }

    public void ShowEnemyMon(MonInstance mon)
    {
        if (mon == null || mon.species == null)
        {
            ClearEnemyMon();
            return;
        }

        if (enemySprite != null)
        {
            enemySprite.sprite = mon.species.frontSprite;
            enemySprite.enabled = mon.species.frontSprite != null;
        }

        if (enemyNameText != null)
            enemyNameText.text = mon.species.monName;

        if (enemyLevelText != null)
            enemyLevelText.text = $"LVL {mon.level}";

        if (enemyTypeIcon != null)
        {
            enemyTypeIcon.sprite = mon.species.typeSprite;
            enemyTypeIcon.enabled = mon.species.typeSprite != null;
        }

        UpdateEnemyHP(mon.currentHP, MonLevelSystem.GetMaxHP(mon));
    }

    public void ClearEnemyMon()
    {
        if (enemySprite != null)
        {
            enemySprite.sprite = null;
            enemySprite.enabled = false;
        }

        if (enemyNameText != null)
            enemyNameText.text = string.Empty;

        if (enemyLevelText != null)
            enemyLevelText.text = string.Empty;

        if (enemyTypeIcon != null)
        {
            enemyTypeIcon.sprite = null;
            enemyTypeIcon.enabled = false;
        }

        if (enemyHP != null)
        {
            enemyHP.minValue = 0;
            enemyHP.maxValue = 1;
            enemyHP.value = 0;
        }
    }

    public void ShowPlayerMon(PlayerMon playerMon)
    {
        if (playerMon == null || playerMon.instance == null || playerMon.instance.species == null)
            return;

        MonInstance mon = playerMon.instance;

        if (playerSprite != null)
        {
            playerSprite.sprite = mon.species.backSprite != null
                ? mon.species.backSprite
                : mon.species.frontSprite;

            playerSprite.enabled = playerSprite.sprite != null;
            playerSprite.color = Color.white;
        }

        if (playerNameText != null)
            playerNameText.text = mon.species.monName;

        if (playerLevelText != null)
            playerLevelText.text = $"LVL {mon.level}";

        if (playerTypeIcon != null)
        {
            playerTypeIcon.sprite = mon.species.typeSprite;
            playerTypeIcon.enabled = mon.species.typeSprite != null;
            playerTypeIcon.color = Color.white;
        }

        UpdatePlayerHP(mon.currentHP, MonLevelSystem.GetMaxHP(mon));
        SetPlayerExp(mon);
    }

    public void UpdateEnemyHP(int current, int max)
    {
        if (enemyHP == null)
            return;

        int safeMax = Mathf.Max(1, max);
        enemyHP.minValue = 0;
        enemyHP.maxValue = safeMax;
        enemyHP.value = Mathf.Clamp(current, 0, safeMax);
    }

    public void UpdatePlayerHP(int current, int max)
    {
        if (playerHP == null)
            return;

        int safeMax = Mathf.Max(1, max);
        playerHP.minValue = 0;
        playerHP.maxValue = safeMax;
        playerHP.value = Mathf.Clamp(current, 0, safeMax);
    }

    public void SetPlayerExp(MonInstance mon)
    {
        if (playerEXP == null || mon == null)
            return;

        int expToNext = Mathf.Max(1, MonLevelSystem.ExpToNextLevel(mon.level));
        playerEXP.minValue = 0;
        playerEXP.maxValue = expToNext;
        playerEXP.value = Mathf.Clamp(mon.experience, 0, expToNext);
    }

    public void SetText(string text)
    {
        if (battleText != null)
            battleText.text = text ?? string.Empty;
    }

    public void SetSwitchButtonInteractable(bool interactable)
    {
        if (switchButton != null)
            switchButton.interactable = interactable;
    }

    public RectTransform GetEffectsContainer()
    {
        return effectsContainer;
    }

    public Vector2 GetPlayerAttackOrigin()
    {
        return playerAttackOrigin != null ? playerAttackOrigin.anchoredPosition : Vector2.zero;
    }

    public Vector2 GetEnemyAttackOrigin()
    {
        return enemyAttackOrigin != null ? enemyAttackOrigin.anchoredPosition : Vector2.zero;
    }

    public Vector2 GetPlayerHitPoint()
    {
        return playerHitPoint != null ? playerHitPoint.anchoredPosition : Vector2.zero;
    }

    public Vector2 GetEnemyHitPoint()
    {
        return enemyHitPoint != null ? enemyHitPoint.anchoredPosition : Vector2.zero;
    }

    public void PlayConfirmSound()
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.Play(Confirm, false);
    }

    public void PlayExitSound()
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.Play(Exit, false);
    }

    public void PlaySwitchSound()
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.Play(Switch, false);
    }
}