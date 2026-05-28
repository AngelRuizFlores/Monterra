using UnityEngine;

public sealed class GameOverManager : MonoBehaviour
{
    [Header("Dependencies")]
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