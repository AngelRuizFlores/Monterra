using UnityEngine;

public sealed class WinGameManager : MonoBehaviour
{
    [SerializeField] private EndGameSequenceController endGameSequenceController;

    public void OnWin()
    {
        if (endGameSequenceController == null)
        {
            Debug.LogError($"{nameof(WinGameManager)}: falta asignar {nameof(EndGameSequenceController)}.", this);
            return;
        }

        endGameSequenceController.PlayVictorySequence();
    }
}