using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MovesUI : MonoBehaviour
{
    [Header("4 Buttons")]
    public Button[] moveButtons;
    public TMP_Text[] moveTexts;

    [Header("Type Colors")]
    [SerializeField] private Color waterColor  = new Color32(70, 170, 255, 255);
    [SerializeField] private Color fireColor   = new Color32(255, 90, 60, 255);
    [SerializeField] private Color grassColor  = new Color32(80, 210, 90, 255);
    [SerializeField] private Color lightColor  = new Color32(255, 215, 80, 255);
    [SerializeField] private Color shadowColor = new Color32(140, 60, 200, 255);
    [SerializeField] private Color earthColor  = new Color32(170, 120, 60, 255);
    [SerializeField] private Color emptyColor  = new Color32(120, 120, 120, 255);

    private PlayerMon playerMon;
    private LevelManager levelManager;

    public void Setup(PlayerMon player, LevelManager lm)
    {
        playerMon = player;
        levelManager = lm;

        Refresh();
    }

    public void Refresh()
    {
        if (moveButtons == null || moveButtons.Length < 4)
            return;

        if (moveTexts == null || moveTexts.Length < 4)
            return;

        if (playerMon == null || playerMon.instance == null)
        {
            ClearButtons();
            return;
        }

        var moves = playerMon.instance.moves;

        for (int i = 0; i < 4; i++)
        {
            bool hasMove = moves != null && i < moves.Count && moves[i] != null;

            moveButtons[i].interactable = hasMove;
            moveButtons[i].onClick.RemoveAllListeners();

            if (hasMove)
            {
                MoveData move = moves[i];

                int index = i;
                moveButtons[i].onClick.AddListener(() => OnClickMove(index));

                moveTexts[i].text = $"{move.moveName}  DMG {move.power}";
                ApplyButtonColor(moveButtons[i], GetTypeColor(move.type));
            }
            else
            {
                moveTexts[i].text = "-";
                ApplyButtonColor(moveButtons[i], emptyColor);
            }
        }
    }

    private void ClearButtons()
    {
        for (int i = 0; i < 4; i++)
        {
            if (moveButtons[i] != null)
            {
                moveButtons[i].interactable = false;
                moveButtons[i].onClick.RemoveAllListeners();
                ApplyButtonColor(moveButtons[i], emptyColor);
            }

            if (moveTexts[i] != null)
                moveTexts[i].text = "-";
        }
    }

    private void ApplyButtonColor(Button button, Color color)
    {
        if (button == null)
            return;

        Image image = button.GetComponent<Image>();
        if (image != null)
            image.color = color;

        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = color * 1.1f;
        colors.pressedColor = color * 0.9f;
        colors.selectedColor = color;
        colors.disabledColor = emptyColor;
        button.colors = colors;
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
                return emptyColor;
        }
    }

    private void OnClickMove(int index)
    {
        if (playerMon == null || playerMon.instance == null)
            return;

        if (playerMon.instance.moves == null)
            return;

        if (index < 0 || index >= playerMon.instance.moves.Count)
            return;

        MoveData move = playerMon.instance.moves[index];

        if (move == null)
            return;

        levelManager.UsePlayerMove(move);
    }

    public void SetInteractable(bool value)
    {
        if (moveButtons == null)
            return;

        foreach (var btn in moveButtons)
        {
            if (btn != null)
                btn.interactable = value;
        }
    }
}