using UnityEngine;

public class HeartBehaviour : MonoBehaviour
{
    [SerializeField] HealthBehaviour healthBehaviour;
    public int healAmount = 20;

    private const string HeartSound = "HeartSound";

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            healthBehaviour.Heal(healAmount);

            // 🔊 sonido
            if (SoundManager.Instance != null)
                SoundManager.Instance.Play(HeartSound, false);

            Destroy(gameObject);
        }
    }
}