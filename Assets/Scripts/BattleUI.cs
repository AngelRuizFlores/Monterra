using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattleUI : MonoBehaviour
{
    public Image enemySprite;
    public Image enemyTypeIcon;
    public TMP_Text enemyName;
    public TMP_Text enemyLevel;
    public Slider enemyHP;

    public Image playerSprite;
    public Image playerTypeIcon;
    public TMP_Text playerName;
    public TMP_Text playerLevel;
    public Slider playerHP;

    public TMP_Text battleText;

    [SerializeField] private Slider playerExpSlider;

    public void SetPlayerExp(MonInstance mon)
    {
        if (playerExpSlider == null || mon == null) return;
        float normalized = MonLevelSystem.GetExpNormalized(mon);
        playerExpSlider.value = normalized * 100f;
    }

    public void SetText(string msg)
    {
        if (battleText) battleText.text = msg;
    }

    public void ShowWildMon(WildMon wild)
    {
        var inst = wild.instance;
        var sp = inst.species;

        if (enemySprite) enemySprite.sprite = sp.frontSprite;
        if (enemyName) enemyName.text = sp.monName;
        if (enemyLevel) enemyLevel.text = "Lvl " + inst.level;
        if (enemyTypeIcon != null) enemyTypeIcon.sprite = sp.typeSprite;

        int maxHP = MonLevelSystem.GetMaxHP(inst);
        if (enemyHP)
        {
            enemyHP.maxValue = maxHP;
            enemyHP.value = inst.currentHP;
        }
    }

    public void ShowPlayerMon(PlayerMon player)
    {
        player.InitIfNeeded();

        var inst = player.instance;
        var sp = inst.species;

        if (playerSprite) playerSprite.sprite = sp.backSprite;
        if (playerName) playerName.text = sp.monName;
        if (playerLevel) playerLevel.text = "Lvl " + inst.level;
        if (playerTypeIcon != null) playerTypeIcon.sprite = sp.typeSprite;

        int maxHP = MonLevelSystem.GetMaxHP(inst);
        if (playerHP)
        {
            playerHP.maxValue = maxHP;
            playerHP.value = inst.currentHP;
        }

        SetPlayerExp(inst);
    }

    public void UpdateEnemyHP(int current, int max)
    {
        if (enemyHP == null) return;
        enemyHP.maxValue = max;
        enemyHP.value = current;
    }

    public void UpdatePlayerHP(int current, int max)
    {
        if (playerHP == null) return;
        playerHP.maxValue = max;
        playerHP.value = current;
    }
}