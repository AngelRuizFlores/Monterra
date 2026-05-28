using UnityEngine;

public class HeartBehaviour : MonoBehaviour
{
    private const string HeartSound = "HeartSound";

    [Header("References")]
    [SerializeField] private HealthBehaviour healthBehaviour;

    [Header("Settings")]
    [SerializeField] private int healAmount = 20;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer != LayerMask.NameToLayer("Player"))
        {
            return;
        }

        healthBehaviour.Heal(healAmount);

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.Play(HeartSound, false);
        }

        Destroy(gameObject);
    }
}