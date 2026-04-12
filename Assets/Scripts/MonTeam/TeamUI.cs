using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TeamUI : MonoBehaviour
{
    [SerializeField] public PlayerTeam team;
    [SerializeField] private BattleUI battleUI;

    [Header("Active Mon")]
    [SerializeField] private Image activeIcon;
    [SerializeField] private Slider playerHP;
    [SerializeField] private Slider playerEXP;
    [SerializeField] private TextMeshProUGUI monNameText;
    [SerializeField] private TextMeshProUGUI monLevelText;
    [SerializeField] private Image typeIcon;

    [Header("Small Slots")]
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
            Color color = evolutionFlash.color;
            color.a = 0f;
            evolutionFlash.color = color;
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
        if (deleteButtons == null)
            return;

        for (int i = 0; i < deleteButtons.Length; i++)
        {
            int smallSlotIndex = i;
            Button button = deleteButtons[i];
            if (button == null)
                continue;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                if (team == null || confirmPopup == null)
                    return;

                int teamIndex = smallSlotIndex >= 0 && smallSlotIndex < displayedTeamIndices.Length
                    ? displayedTeamIndices[smallSlotIndex]
                    : -1;

                if (teamIndex < 0 || teamIndex >= team.UnlockedSlots)
                    return;

                MonInstance instance = team.team[teamIndex];
                if (instance == null || instance.species == null)
                    return;

                string monName = instance.species.monName;
                confirmPopup.Show($"Remove {monName}?", () =>
                {
                    team.RemoveAt(teamIndex);
                });
            });
        }
    }

    public void RefreshAll()
    {
        if (team == null)
            return;

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

        if (team == null)
            return;

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
            Image content = smallContentIcons != null && i < smallContentIcons.Length ? smallContentIcons[i] : null;
            Image lockImage = smallLockIcons != null && i < smallLockIcons.Length ? smallLockIcons[i] : null;
            Button deleteButton = deleteButtons != null && i < deleteButtons.Length ? deleteButtons[i] : null;

            bool slotUnlocked = i < unlockedSmallSlots;
            int mappedTeamIndex = displayedTeamIndices[i];
            MonInstance instance = slotUnlocked && mappedTeamIndex >= 0 && mappedTeamIndex < team.team.Length
                ? team.team[mappedTeamIndex]
                : null;

            if (lockImage != null)
            {
                lockImage.sprite = lockSprite;
                lockImage.enabled = !slotUnlocked;
                lockImage.color = filledColor;
            }

            if (content != null)
            {
                if (!slotUnlocked)
                {
                    content.sprite = null;
                    content.color = filledColor;
                }
                else if (instance == null || instance.species == null)
                {
                    content.sprite = null;
                    content.color = filledColor;
                }
                else
                {
                    content.sprite = instance.species.frontSprite;
                    content.color = filledColor;
                }
            }

            if (deleteButton != null)
            {
                bool showDelete = slotUnlocked && instance != null && instance.species != null;
                deleteButton.gameObject.SetActive(showDelete);
                deleteButton.interactable = showDelete;
            }
        }
    }

    private void RefreshActiveVisuals(MonInstance active)
    {
        if (activeIcon == null)
            return;

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
        if (team == null)
            return;

        MonInstance active = team.GetActiveMon();
        if (active == null || active.species == null)
            return;

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

        if (monNameText != null)
            monNameText.text = active.species.monName;

        if (typeIcon != null)
        {
            typeIcon.sprite = active.species.typeSprite;
            typeIcon.color = filledColor;
        }

        if (monLevelText != null)
            monLevelText.text = $"Lv. {active.level}";
    }

    private void DetectEvolutionOfActiveMon()
    {
        if (team == null)
            return;

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
            evolutionText.text = $"{fromSpecies.monName} evolved into {toSpecies.monName}.";

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

        Color color = evolutionFlash.color;

        float elapsed = 0f;
        while (elapsed < evolutionFlashDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(0f, 0.9f, elapsed / evolutionFlashDuration);
            color.a = alpha;
            evolutionFlash.color = color;
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < evolutionFlashDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(0.9f, 0f, elapsed / evolutionFlashDuration);
            color.a = alpha;
            evolutionFlash.color = color;
            yield return null;
        }

        color.a = 0f;
        evolutionFlash.color = color;
    }

    private IEnumerator PulseActiveIcon()
    {
        if (activeIcon == null)
            yield break;

        RectTransform rectTransform = activeIcon.rectTransform;
        if (rectTransform == null)
            yield break;

        Vector3 startScale = activeIconBaseScale;
        Vector3 peakScale = activeIconBaseScale * evolutionPulseScale;

        for (int i = 0; i < evolutionPulseCount; i++)
        {
            float elapsed = 0f;
            while (elapsed < 0.12f)
            {
                elapsed += Time.unscaledDeltaTime;
                rectTransform.localScale = Vector3.Lerp(startScale, peakScale, elapsed / 0.12f);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < 0.12f)
            {
                elapsed += Time.unscaledDeltaTime;
                rectTransform.localScale = Vector3.Lerp(peakScale, startScale, elapsed / 0.12f);
                yield return null;
            }
        }

        rectTransform.localScale = startScale;
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