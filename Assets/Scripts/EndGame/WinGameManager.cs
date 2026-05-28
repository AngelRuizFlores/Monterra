using UnityEngine;

public sealed class WinGameManager : MonoBehaviour
{
    [Header("Dependencies")]
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