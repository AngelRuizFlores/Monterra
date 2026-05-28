using UnityEngine;

public class BattleActions : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HealthBehaviour enemyHealth;

    public void DealDamageToEnemy(int damage)
    {
        if (enemyHealth == null)
        {
            return;
        }

        enemyHealth.Hurt(damage);
    }
}