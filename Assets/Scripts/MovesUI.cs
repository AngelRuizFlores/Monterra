using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MovesUI : MonoBehaviour
{
    [Header("4 Buttons")]
    public Button[] moveButtons;     
    public TMP_Text[] moveTexts;     

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
        if (playerMon == null || playerMon.instance == null) return;
        if (moveButtons == null || moveButtons.Length < 4) return;
        if (moveTexts == null || moveTexts.Length < 4) return;

        var moves = playerMon.instance.moves;

        for (int i = 0; i < 4; i++)
        {
            bool hasMove = (moves != null && i < moves.Count && moves[i] != null);

            moveButtons[i].interactable = hasMove;

            moveButtons[i].onClick.RemoveAllListeners();
            if (hasMove)
            {
                int index = i;
                moveButtons[i].onClick.AddListener(() => OnClickMove(index));
            }

            moveTexts[i].text = hasMove ? moves[i].moveName : "-";
        }
    }

    private void OnClickMove(int index)
    {
        var move = playerMon.instance.moves[index];
        levelManager.UsePlayerMove(move);
    }

   public void SetInteractable(bool value)
    {
        foreach (var btn in moveButtons)
        {
            if (btn != null)
                btn.interactable = value;
        }
    }


}
