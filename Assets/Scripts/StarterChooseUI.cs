using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StarterChooseUI : MonoBehaviour
{
    [SerializeField] private List<MonSpecies> allStarters = new();

    [SerializeField] private Image[] slotImages;
    [SerializeField] private TMP_Text[] slotNames;
    [SerializeField] private Button[] slotButtons;

   [SerializeField] private PlayerTeam team;
    [SerializeField] private GameObject chooseCanvas;
    [SerializeField] private CompanionHintsUI companionHintsUI;

    [Header("Player Start Position")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform waterStartPoint;
    [SerializeField] private Transform fireStartPoint;
    [SerializeField] private Transform grassStartPoint;
    [SerializeField] private Transform lightStartPoint;
    [SerializeField] private Transform shadowStartPoint;
    [SerializeField] private Transform earthStartPoint;

    [SerializeField] private Color waterColor  = new Color32(70, 170, 255, 255);
    [SerializeField] private Color fireColor   = new Color32(255, 90, 60, 255);
    [SerializeField] private Color grassColor  = new Color32(80, 210, 90, 255);
    [SerializeField] private Color lightColor  = new Color32(255, 215, 80, 255);
    [SerializeField] private Color shadowColor = new Color32(140, 60, 200, 255);
    [SerializeField] private Color earthColor  = new Color32(170, 120, 60, 255);

  private readonly MonSpecies[] shown = new MonSpecies[3];
    private bool isChoosing = false;
    private const string Music = "ChooseMusic";
    private const string Confirm = "Switch";
    private const float ChooseStartDelay = 1.5f;


    private void Start() {
        if (chooseCanvas != null)
            if (SoundManager.Instance != null)
            SoundManager.Instance.Play(Music, true);
    }

    private void OnEnable()
    {
        if (GameStartMode.LoadGame)
        {
            if (chooseCanvas != null)
                chooseCanvas.SetActive(false);
            else
                gameObject.SetActive(false);

            Time.timeScale = 1f;
            return;
        }

        if (team == null) return;
        if (allStarters == null || allStarters.Count < 3) return;

        Time.timeScale = 0f;
        FindFirstObjectByType<MusicGame>()?.StopMusic();

        Roll3Random();
        Paint();
        HookButtons();
    }

    private void OnDisable()
    {
        Time.timeScale = 1f;
    }

    private void Roll3Random()
    {
        var temp = new List<MonSpecies>(allStarters);

        for (int i = 0; i < temp.Count; i++)
        {
            int r = Random.Range(i, temp.Count);
            (temp[i], temp[r]) = (temp[r], temp[i]);
        }

        shown[0] = temp[0];
        shown[1] = temp[1];
        shown[2] = temp[2];
    }

    private void Paint()
    {
        for (int i = 0; i < 3; i++)
        {
            var sp = shown[i];
            if (sp == null) continue;

            if (slotImages != null && i < slotImages.Length && slotImages[i] != null)
                slotImages[i].sprite = sp.frontSprite;

            if (slotNames != null && i < slotNames.Length && slotNames[i] != null)
                slotNames[i].text = sp.monName;

            if (slotButtons == null || i >= slotButtons.Length || slotButtons[i] == null)
                continue;

            var txt = slotButtons[i].GetComponentInChildren<TMP_Text>();
            if (txt != null)
                txt.text = sp.type + " Type";

            Color typeColor = GetTypeColor(sp.type);

            var img = slotButtons[i].GetComponent<Image>();
            if (img != null)
                img.color = typeColor;

            var colors = slotButtons[i].colors;
            colors.normalColor = typeColor;
            colors.highlightedColor = typeColor * 1.1f;
            colors.pressedColor = typeColor * 0.9f;
            slotButtons[i].colors = colors;
        }
    }

    private void HookButtons()
    {
        for (int i = 0; i < 3; i++)
        {
            if (slotButtons == null || i >= slotButtons.Length || slotButtons[i] == null)
                continue;

            int idx = i;
            slotButtons[i].onClick.RemoveAllListeners();
            slotButtons[i].onClick.AddListener(() => Choose(idx));
        }
    }

    private void Choose(int idx)
    {
        if (isChoosing) return;
        if (idx < 0 || idx >= shown.Length) return;

        var chosen = shown[idx];
        if (chosen == null) return;

        StartCoroutine(ChooseRoutine(chosen));
    }

   private IEnumerator ChooseRoutine(MonSpecies chosen)
    {
        isChoosing = true;
        SetButtonsInteractable(false);
        PlayConfirmSound();

        yield return new WaitForSecondsRealtime(ChooseStartDelay);

        var starter = new MonInstance
        {
            species = chosen,
            level = 3,
            experience = 0
        };

        starter.currentHP = MonLevelSystem.GetMaxHP(starter);
        MonLevelSystem.InitMovesForCurrentLevel(starter);

        team.InitWithStarter(starter);

        if (companionHintsUI != null)
            companionHintsUI.StartStarterHints();

        if (FadeController.Instance != null)
            yield return FadeController.Instance.FadeOut();

        MovePlayerToStarterPoint(chosen);

        Time.timeScale = 1f;

        if (chooseCanvas != null)
            chooseCanvas.SetActive(false);

        FindFirstObjectByType<MusicGame>()?.StartWorldMusic();

        if (FadeController.Instance != null)
            FadeController.Instance.StartFadeIn();
    }

    private void MovePlayerToStarterPoint(MonSpecies chosen)
    {
        if (chosen == null || playerTransform == null)
            return;

        Transform point = GetStartPoint(chosen.type);

        if (point == null)
            return;

        playerTransform.position = point.position;
    }

    private Transform GetStartPoint(MonType type)
    {
        switch (type)
        {
            case MonType.Water:
                return waterStartPoint;

            case MonType.Fire:
                return fireStartPoint;

            case MonType.Grass:
                return grassStartPoint;

            case MonType.Light:
                return lightStartPoint;

            case MonType.Shadow:
                return shadowStartPoint;

            case MonType.Earth:
                return earthStartPoint;

            default:
                return null;
        }
    }

    private void SetButtonsInteractable(bool value)
    {
        if (slotButtons == null) return;

        for (int i = 0; i < slotButtons.Length; i++)
        {
            if (slotButtons[i] != null)
                slotButtons[i].interactable = value;
        }
    }

    private Color GetTypeColor(MonType type)
    {
        switch (type)
        {
            case MonType.Water:
                return waterColor;

            case MonType.Fire:
                return fireColor;

            case MonType.Grass:
                return grassColor;

            case MonType.Light:
                return lightColor;

            case MonType.Shadow:
                return shadowColor;

            case MonType.Earth:
                return earthColor;

            default:
                return shadowColor;
        }
    }

    public void PlayConfirmSound()
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.Play(Confirm, false);
    }
}