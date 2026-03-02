using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TeamUI : MonoBehaviour
{
    [SerializeField] public PlayerTeam team;
    [SerializeField] private BattleUI battleUI;

    [Header("Active (Big)")]
    [SerializeField] private Image activeIcon;
    [SerializeField] private Slider playerHP;
    [SerializeField] private Slider playerEXP;
    [SerializeField] private TextMeshProUGUI name_Mon;
    [SerializeField] private TextMeshProUGUI level_Mon;
    [SerializeField] private Image typeIcon;

    [Header("Small Slots (5) -> team[1..5]")]
    [SerializeField] private Image[] smallContentIcons;
    [SerializeField] private Image[] smallLockIcons;
    [SerializeField] private Button[] deleteButtons;

    [Header("Popup")]
    [SerializeField] private ConfirmPopupUI confirmPopup;

    [Header("Visual")]
    [SerializeField] private Sprite lockSprite;
    [SerializeField] private Color filledColor = new Color32(255, 255, 255, 255);

    void OnEnable()
    {
        Debug.Log("TeamUI OnEnable");
        if (team != null) team.OnChanged += RefreshAll;
        HookDeleteButtons();
        RefreshAll();
    }

    void OnDisable()
    {
        if (team != null) team.OnChanged -= RefreshAll;
    }

    void Update()
    {
        RefreshHud();
    }

    void HookDeleteButtons()
    {
        if (deleteButtons == null) return;

        for (int i = 0; i < deleteButtons.Length; i++)
        {
            int teamIndex = i + 1;
            var b = deleteButtons[i];
            if (b == null) continue;

            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(() =>
            {
                if (team == null) return;
                if (teamIndex >= team.UnlockedSlots) return;
                var inst = team.team[teamIndex];
                if (inst == null || inst.species == null) return;

                if (confirmPopup == null) return;

                string monName = inst.species.monName;
                confirmPopup.Show($"¿Eliminar a {monName}?", () =>
                {
                    team.RemoveAt(teamIndex);
                });
            });
        }
    }

    public void RefreshAll()
    {
        if (team == null) return;

        var active = team.team[0];
        if (activeIcon != null)
        {
            if (active != null && active.species != null)
            {
                activeIcon.sprite = active.species.frontSprite;
                activeIcon.color = filledColor;

                if (battleUI != null && team.playerMon != null)
                    battleUI.ShowPlayerMon(team.playerMon);
            }
            else
            {
                activeIcon.sprite = null;
                activeIcon.color = filledColor;
            }
        }

        for (int i = 0; i < 5; i++)
        {
            int teamIndex = i + 1;

            var content = (smallContentIcons != null && i < smallContentIcons.Length) ? smallContentIcons[i] : null;
            var lockImg = (smallLockIcons != null && i < smallLockIcons.Length) ? smallLockIcons[i] : null;
            var delBtn = (deleteButtons != null && i < deleteButtons.Length) ? deleteButtons[i] : null;

            bool unlocked = teamIndex < team.UnlockedSlots;
            var inst = unlocked ? team.team[teamIndex] : null;

            if (lockImg != null)
            {
                lockImg.sprite = lockSprite;
                lockImg.enabled = !unlocked;
                lockImg.color = filledColor;
            }

            if (content != null)
            {
                if (!unlocked)
                {
                    content.sprite = null;
                    content.color = filledColor;
                }
                else if (inst == null || inst.species == null)
                {
                    content.sprite = null;
                    content.color = filledColor;
                }
                else
                {
                    content.sprite = inst.species.frontSprite;
                    content.color = filledColor;
                }
            }

            if (delBtn != null)
            {
                bool showDelete = unlocked && inst != null && inst.species != null;
                delBtn.gameObject.SetActive(showDelete);
                delBtn.interactable = showDelete;
            }
        }

        RefreshHud();
    }

    void RefreshHud()
    {
        if (team == null) return;
        var active = team.team[0];
        if (active == null || active.species == null) return;

        if (playerHP != null)
        {
            int maxHP = MonLevelSystem.GetMaxHP(active);
            playerHP.minValue = 0;
            playerHP.maxValue = maxHP;
            playerHP.value = Mathf.Clamp(active.currentHP, 0, maxHP);
        }

        if (playerEXP != null)
        {
            int expToNext = MonLevelSystem.ExpToNextLevel(active.level);
            playerEXP.minValue = 0;
            playerEXP.maxValue = expToNext;
            playerEXP.value = Mathf.Clamp(active.experience, 0, expToNext);
        }

        if (name_Mon != null) name_Mon.text = active.species.monName;

        if (typeIcon != null)
        {
            typeIcon.sprite = active.species.typeSprite;
            typeIcon.color = filledColor;
        }

        if (level_Mon != null) level_Mon.text = "Lvl " + active.level;
    }
}