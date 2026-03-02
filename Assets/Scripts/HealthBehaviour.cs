using UnityEngine;
using UnityEngine.Events;

public class HealthBehaviour : MonoBehaviour
{
    public UnityEvent<int, int> OnHurt;
    public UnityEvent<int, int> OnHeal;
    public UnityEvent OnDie;

    private MonInstance instance;

    public void Init(MonInstance monInstance)
    {
        instance = monInstance;
        OnHeal?.Invoke(instance.currentHP, GetMaxHP());
    }

    int GetMaxHP()
    {
        return instance.species.baseHP + (instance.level * 2);
    }

    public void Hurt(int damage)
    {
        Debug.Log($"{instance.species.monName} recibe {damage} daño. HP: {instance.currentHP}");

        if (instance == null) return;

        instance.currentHP -= damage;
        if (instance.currentHP < 0)
            instance.currentHP = 0;

        OnHurt?.Invoke(instance.currentHP, GetMaxHP());

        if (instance.currentHP == 0)
            OnDie?.Invoke();
    }

    public void Heal(int amount)
    {
        if (instance == null) return;

        instance.currentHP += amount;
        if (instance.currentHP > GetMaxHP())
            instance.currentHP = GetMaxHP();

        OnHeal?.Invoke(instance.currentHP, GetMaxHP());
    }

    public void Clear()
    {
        instance = null;
    }

}
