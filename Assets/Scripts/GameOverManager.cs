using UnityEngine;

public sealed class GameOverManager : MonoBehaviour
{
    [SerializeField] private EndGameSequenceController endGameSequenceController;

    public void OnGameOver()
    {
        if (endGameSequenceController == null)
        {
            Debug.LogError($"{nameof(GameOverManager)}: falta asignar {nameof(EndGameSequenceController)}.", this);
            return;
        }

        endGameSequenceController.PlayDefeatSequence();
    }
}