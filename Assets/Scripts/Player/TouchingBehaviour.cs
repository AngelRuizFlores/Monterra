using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class TouchingBehaviour : MonoBehaviour
{
    [Header("Events")]
    public UnityEvent OnTouchMon;
    public UnityEvent OnTouchTrainer;

    public WildMon lastWildMon;
    public TrainerBattleTrigger lastTrainer;

    private bool isTransitioning;

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (GameSessionState.IsEndGameActive)
        {
            return;
        }

        if (isTransitioning)
        {
            return;
        }

        if (col.TryGetComponent(out TrainerBattleTrigger trainer))
        {
            if (!trainer.IsDefeated)
            {
                lastTrainer = trainer;
                StartCoroutine(HandleTrainerTouch());
            }

            return;
        }

        if (col.TryGetComponent(out WildMon wild))
        {
            lastWildMon = wild;
            StartCoroutine(HandleWildMonTouch());
        }
    }

    private IEnumerator HandleWildMonTouch()
    {
        isTransitioning = true;

        if (GameSessionState.IsEndGameActive)
        {
            isTransitioning = false;
            yield break;
        }

        if (FadeController.Instance != null)
        {
            yield return FadeController.Instance.FadeOut();
        }

        if (GameSessionState.IsEndGameActive)
        {
            isTransitioning = false;
            yield break;
        }

        OnTouchMon?.Invoke();

        if (FadeController.Instance != null)
        {
            FadeController.Instance.StartFadeIn();
        }

        isTransitioning = false;
    }

    private IEnumerator HandleTrainerTouch()
    {
        isTransitioning = true;

        if (GameSessionState.IsEndGameActive)
        {
            isTransitioning = false;
            yield break;
        }

        if (FadeController.Instance != null)
        {
            yield return FadeController.Instance.FadeOut();
        }

        if (GameSessionState.IsEndGameActive)
        {
            isTransitioning = false;
            yield break;
        }

        OnTouchTrainer?.Invoke();

        if (FadeController.Instance != null)
        {
            FadeController.Instance.StartFadeIn();
        }

        isTransitioning = false;
    }
}