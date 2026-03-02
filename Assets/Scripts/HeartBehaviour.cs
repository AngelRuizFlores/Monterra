using UnityEngine;
using UnityEngine.Events;

public class HeartBehaviour : MonoBehaviour
{
    [SerializeField] HealthBehaviour healthBehaviour;
    public int healAmount = 20;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            healthBehaviour.Heal(healAmount);
            Destroy(gameObject);
        }
    }
}
