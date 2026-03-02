using UnityEngine;
using UnityEngine.Events;

public class TouchingBehaviour : MonoBehaviour
{
    public UnityEvent OnTouchMon;
    public UnityEvent OnTouchTrainer;

    public WildMon lastWildMon;

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (col.TryGetComponent<WildMon>(out var wild))
        {
            lastWildMon = wild;
            Debug.Log("Tocaste un WildMon: " + wild.species.monName);

            OnTouchMon?.Invoke();
        }
    }
}
