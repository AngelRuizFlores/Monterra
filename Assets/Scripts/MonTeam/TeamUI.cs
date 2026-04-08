using System.Collections;
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

    [Header("Small Slots (5)")]
    [SerializeField] private Image[] smallContentIcons;
    [SerializeField] private Image[] smallLockIcons;
    [SerializeField] private Button[] deleteButtons;

    [Header("Popup")]
    [SerializeField] private ConfirmPopupUI confirmPopup;

    [Header("Evolution FX")]
    [SerializeField] private GameObject evolutionPanel;
    [SerializeField] private TextMeshProUGUI evolutionText;
    [SerializeField] private Image evolutionFlash;
    [SerializeField] private float evolutionFlashDuration = 0.18f;
    [SerializeField] private float evolutionPanelDuration = 1.35f;
    [SerializeField] private float evolutionPulseScale = 1.18f;
    [SerializeField] private int evolutionPulseCount = 2;

    [Header("Visual")]
    [SerializeField] private Sprite lockSprite;
    [SerializeField] private Color filledColor = new Color32(255, 255, 255, 255);

    private readonly int[] displayedTeamIndices = { -1, -1, -1, -1, -1 };

    private MonInstance lastTrackedActiveMon;
    private MonSpecies lastActiveSpecies;
    private int lastActiveLevel = -1;
    private Coroutine evolutionRoutine;
    private Vector3 activeIconBaseScale = Vector3.one;

    private void OnEnable()
    {
        if (team != null)
            team.OnChanged += RefreshAll;

        if (activeIcon != null)
            activeIconBaseScale = activeIcon.rectTransform.localScale;

        if (evolutionPanel != null)
            evolutionPanel.SetActive(false);

        if (evolutionFlash != null)
        {
            Color c = evolutionFlash.color;
            c.a = 0f;
            evolutionFlash.color = c;
        }

        HookDeleteButtons();
        RefreshAll();
        CacheCurrentActiveState();
    }

    private void OnDisable()
    {
        if (team != null)
            team.OnChanged -= RefreshAll;
    }

    private void Update()
    {
        RefreshHud();
        DetectEvolutionOfActiveMon();
    }

    private void HookDeleteButtons()
    {
        if (deleteButtons == null) return;

        for (int i = 0; i < deleteButtons.Length; i++)
        {
            int smallSlotIndex = i;
            Button b = deleteButtons[i];
            if (b == null) continue;

            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(() =>
            {
                if (team == null || confirmPopup == null) return;

                int teamIndex = (smallSlotIndex >= 0 && smallSlotIndex < displayedTeamIndices.Length)
                    ? displayedTeamIndices[smallSlotIndex]
                    : -1;

                if (teamIndex < 0 || teamIndex >= team.UnlockedSlots) return;

                MonInstance inst = team.team[teamIndex];
                if (inst == null || inst.species == null) return;

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

        MonInstance active = team.GetActiveMon();
        SyncPlayerMonMeta(active);
        RefreshActiveVisuals(active);
        RefreshSmallSlots();
        RefreshHud();
    }

    private void RefreshSmallSlots()
    {
        for (int i = 0; i < displayedTeamIndices.Length; i++)
            displayedTeamIndices[i] = -1;

        if (team == null) return;

        int writeIndex = 0;
        int limit = Mathf.Min(team.UnlockedSlots, team.team.Length);

        for (int teamIndex = 0; teamIndex < limit; teamIndex++)
        {
            if (teamIndex == team.ActiveIndex)
                continue;

            if (writeIndex >= displayedTeamIndices.Length)
                break;

            displayedTeamIndices[writeIndex] = teamIndex;
            writeIndex++;
        }

        int unlockedSmallSlots = Mathf.Clamp(team.UnlockedSlots - 1, 0, displayedTeamIndices.Length);

        for (int i = 0; i < displayedTeamIndices.Length; i++)
        {
            Image content = (smallContentIcons != null && i < smallContentIcons.Length) ? smallContentIcons[i] : null;
            Image lockImg = (smallLockIcons != null && i < smallLockIcons.Length) ? smallLockIcons[i] : null;
            Button delBtn = (deleteButtons != null && i < deleteButtons.Length) ? deleteButtons[i] : null;

            bool slotUnlocked = i < unlockedSmallSlots;
            int mappedTeamIndex = displayedTeamIndices[i];
            MonInstance inst = (slotUnlocked && mappedTeamIndex >= 0 && mappedTeamIndex < team.team.Length)
                ? team.team[mappedTeamIndex]
                : null;

            if (lockImg != null)
            {
                lockImg.sprite = lockSprite;
                lockImg.enabled = !slotUnlocked;
                lockImg.color = filledColor;
            }

            if (content != null)
            {
                if (!slotUnlocked)
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
                bool showDelete = slotUnlocked && inst != null && inst.species != null;
                delBtn.gameObject.SetActive(showDelete);
                delBtn.interactable = showDelete;
            }
        }
    }

    private void RefreshActiveVisuals(MonInstance active)
    {
        if (activeIcon == null) return;

        if (active != null && active.species != null)
        {
            activeIcon.sprite = active.species.frontSprite;
            activeIcon.color = filledColor;

            if (battleUI != null && team != null && team.playerMon != null)
                battleUI.ShowPlayerMon(team.playerMon);
        }
        else
        {
            activeIcon.sprite = null;
            activeIcon.color = filledColor;
        }
    }

    private void RefreshHud()
    {
        if (team == null) return;

        MonInstance active = team.GetActiveMon();
        if (active == null || active.species == null) return;

        SyncPlayerMonMeta(active);

        if (activeIcon != null)
        {
            activeIcon.sprite = active.species.frontSprite;
            activeIcon.color = filledColor;
        }

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

        if (name_Mon != null)
            name_Mon.text = active.species.monName;

        if (typeIcon != null)
        {
            typeIcon.sprite = active.species.typeSprite;
            typeIcon.color = filledColor;
        }

        if (level_Mon != null)
            level_Mon.text = "Lvl " + active.level;
    }

    private void DetectEvolutionOfActiveMon()
    {
        if (team == null) return;

        MonInstance active = team.GetActiveMon();
        if (active == null || active.species == null)
        {
            lastTrackedActiveMon = null;
            lastActiveSpecies = null;
            lastActiveLevel = -1;
            return;
        }

        bool sameMon = ReferenceEquals(lastTrackedActiveMon, active);
        bool speciesChanged = sameMon && lastActiveSpecies != null && lastActiveSpecies != active.species;

        if (speciesChanged)
        {
            if (evolutionRoutine != null)
                StopCoroutine(evolutionRoutine);

            evolutionRoutine = StartCoroutine(PlayEvolutionSequence(lastActiveSpecies, active.species));
        }

        lastTrackedActiveMon = active;
        lastActiveSpecies = active.species;
        lastActiveLevel = active.level;
    }

    private IEnumerator PlayEvolutionSequence(MonSpecies fromSpecies, MonSpecies toSpecies)
    {
        if (fromSpecies == null || toSpecies == null)
            yield break;

        if (evolutionPanel != null)
            evolutionPanel.SetActive(true);

        if (evolutionText != null)
            evolutionText.text = $"{fromSpecies.monName} evolucionó a {toSpecies.monName}!";

        yield return FlashWhite();
        yield return PulseActiveIcon();

        RefreshAll();

        yield return new WaitForSecondsRealtime(evolutionPanelDuration);

        if (evolutionPanel != null)
            evolutionPanel.SetActive(false);

        evolutionRoutine = null;
    }

    private IEnumerator FlashWhite()
    {
        if (evolutionFlash == null)
            yield break;

        Color c = evolutionFlash.color;

        float t = 0f;
        while (t < evolutionFlashDuration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(0f, 0.9f, t / evolutionFlashDuration);
            c.a = a;
            evolutionFlash.color = c;
            yield return null;
        }

        t = 0f;
        while (t < evolutionFlashDuration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(0.9f, 0f, t / evolutionFlashDuration);
            c.a = a;
            evolutionFlash.color = c;
            yield return null;
        }

        c.a = 0f;
        evolutionFlash.color = c;
    }

    private IEnumerator PulseActiveIcon()
    {
        if (activeIcon == null)
            yield break;

        RectTransform rt = activeIcon.rectTransform;
        if (rt == null)
            yield break;

        Vector3 start = activeIconBaseScale;
        Vector3 peak = activeIconBaseScale * evolutionPulseScale;

        for (int i = 0; i < evolutionPulseCount; i++)
        {
            float t = 0f;
            while (t < 0.12f)
            {
                t += Time.unscaledDeltaTime;
                rt.localScale = Vector3.Lerp(start, peak, t / 0.12f);
                yield return null;
            }

            t = 0f;
            while (t < 0.12f)
            {
                t += Time.unscaledDeltaTime;
                rt.localScale = Vector3.Lerp(peak, start, t / 0.12f);
                yield return null;
            }
        }

        rt.localScale = start;
    }

    private void SyncPlayerMonMeta(MonInstance active)
    {
        if (team == null || team.playerMon == null || active == null || active.species == null)
            return;

        if (ReferenceEquals(team.playerMon.instance, active))
        {
            team.playerMon.species = active.species;
            team.playerMon.lvl = active.level;
        }
    }

    private void CacheCurrentActiveState()
    {
        MonInstance active = team != null ? team.GetActiveMon() : null;
        lastTrackedActiveMon = active;
        lastActiveSpecies = active != null ? active.species : null;
        lastActiveLevel = active != null ? active.level : -1;
    }
}