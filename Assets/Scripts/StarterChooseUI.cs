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

    [SerializeField] private Color waterColor  = new Color32(70, 170, 255, 255);
    [SerializeField] private Color fireColor   = new Color32(255, 90, 60, 255);
    [SerializeField] private Color grassColor  = new Color32(80, 210, 90, 255);
    [SerializeField] private Color lightColor  = new Color32(255, 215, 80, 255);
    [SerializeField] private Color shadowColor = new Color32(140, 60, 200, 255);
    [SerializeField] private Color earthColor  = new Color32(170, 120, 60, 255);

    private readonly MonSpecies[] shown = new MonSpecies[3];
    private bool isChoosing = false;

    private void OnEnable()
    {
        if (team == null) return;
        if (allStarters == null || allStarters.Count < 3) return;

        Time.timeScale = 0f;

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

        var starter = new MonInstance
        {
            species = chosen,
            level = 1,
            experience = 0
        };

        starter.currentHP = MonLevelSystem.GetMaxHP(starter);
        MonLevelSystem.InitMovesForCurrentLevel(starter);

        team.InitWithStarter(starter);

        if (FadeController.Instance != null)
            yield return FadeController.Instance.FadeOut();

        Time.timeScale = 1f;

        if (FadeController.Instance != null)
            FadeController.Instance.StartFadeIn();

        if (chooseCanvas != null)
            chooseCanvas.SetActive(false);
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
            case MonType.Water:  return waterColor;
            case MonType.Fire:   return fireColor;
            case MonType.Grass:  return grassColor;
            case MonType.Light:  return lightColor;
            case MonType.Shadow: return shadowColor;
            case MonType.Earth:  return earthColor;
            default:             return shadowColor;
        }
    }
}