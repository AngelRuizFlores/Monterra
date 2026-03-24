using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class TouchingBehaviour : MonoBehaviour
{
    public UnityEvent OnTouchMon;
    public UnityEvent OnTouchTrainer;

    public WildMon lastWildMon;

    private bool isTransitioning = false;

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (isTransitioning) return;

        if (col.TryGetComponent<WildMon>(out var wild))
        {
            lastWildMon = wild;
            Debug.Log("Tocaste un WildMon: " + wild.species.monName);

            StartCoroutine(HandleWildMonTouch());
        }
    }

    private IEnumerator HandleWildMonTouch()
    {
        isTransitioning = true;

        if (FadeController.Instance != null)
            yield return FadeController.Instance.FadeOut();

        OnTouchMon?.Invoke();

        if (FadeController.Instance != null)
            FadeController.Instance.StartFadeIn();

        isTransitioning = false;
    }
}