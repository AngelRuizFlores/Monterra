using UnityEngine;
using UnityEngine.Events;

public sealed class HealthBehaviour : MonoBehaviour, IHealable
{
    public UnityEvent<int, int> OnHurt;
    public UnityEvent<int, int> OnHeal;
    public UnityEvent OnDie;

    private MonInstance instance;

    public int CurrentHealth => instance != null ? instance.currentHP : 0;
    public int MaxHealth => instance != null ? CalculateMaxHP(instance) : 0;
    public bool IsInitialized => instance != null;

    public void Init(MonInstance monInstance)
    {
        if (monInstance == null)
        {
            Debug.LogError($"{nameof(HealthBehaviour)}.{nameof(Init)} recibió un MonInstance null.", this);
            return;
        }

        if (monInstance.species == null)
        {
            Debug.LogError($"{nameof(HealthBehaviour)}.{nameof(Init)} recibió un MonInstance con species null.", this);
            return;
        }

        instance = monInstance;
        instance.currentHP = Mathf.Clamp(instance.currentHP, 0, CalculateMaxHP(instance));
        OnHeal?.Invoke(instance.currentHP, MaxHealth);
    }

    public void Hurt(int damage)
    {
        if (instance == null)
        {
            Debug.LogWarning($"{nameof(HealthBehaviour)}.{nameof(Hurt)} llamado sin inicializar.", this);
            return;
        }

        if (damage <= 0)
        {
            Debug.LogWarning($"{nameof(HealthBehaviour)}.{nameof(Hurt)} recibió un daño no válido: {damage}.", this);
            return;
        }

        instance.currentHP = Mathf.Max(0, instance.currentHP - damage);
        OnHurt?.Invoke(instance.currentHP, MaxHealth);

        if (instance.currentHP == 0)
        {
            OnDie?.Invoke();
        }
    }

    public void Heal(int amount)
    {
        if (instance == null)
        {
            Debug.LogWarning($"{nameof(HealthBehaviour)}.{nameof(Heal)} llamado sin inicializar.", this);
            return;
        }

        if (amount <= 0)
        {
            Debug.LogWarning($"{nameof(HealthBehaviour)}.{nameof(Heal)} recibió una curación no válida: {amount}.", this);
            return;
        }

        int previousHp = instance.currentHP;
        instance.currentHP = Mathf.Min(MaxHealth, instance.currentHP + amount);

        if (instance.currentHP != previousHp)
        {
            OnHeal?.Invoke(instance.currentHP, MaxHealth);
        }
    }

    public void HealToFull()
    {
        if (instance == null)
        {
            Debug.LogWarning($"{nameof(HealthBehaviour)}.{nameof(HealToFull)} llamado sin inicializar.", this);
            return;
        }

        if (instance.currentHP == MaxHealth)
        {
            return;
        }

        instance.currentHP = MaxHealth;
        OnHeal?.Invoke(instance.currentHP, MaxHealth);
    }

    public void Clear()
    {
        instance = null;
    }

    private static int CalculateMaxHP(MonInstance monInstance)
    {
        return monInstance.species.baseHP + (monInstance.level * 2);
    }
}