using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public sealed class HealthBehaviour : MonoBehaviour, IHealable
{
    public UnityEvent<int, int> OnHurt;
    public UnityEvent<int, int> OnHeal;
    public UnityEvent OnDie;

    [SerializeField] private float animationDurationPerPoint = 0.03f;
    [SerializeField] private float minAnimationDuration = 0.15f;
    [SerializeField] private float maxAnimationDuration = 0.9f;

    private MonInstance instance;

    public int CurrentHealth => instance != null ? instance.currentHP : 0;
    public int MaxHealth => instance != null ? CalculateMaxHP(instance) : 0;
    public bool IsInitialized => instance != null;

    public void Init(MonInstance monInstance)
    {
        if (monInstance == null)
        {
            Debug.LogError($"{nameof(HealthBehaviour)}.{nameof(Init)} received a null MonInstance.", this);
            return;
        }

        if (monInstance.species == null)
        {
            Debug.LogError($"{nameof(HealthBehaviour)}.{nameof(Init)} received a MonInstance with a null species.", this);
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
            Debug.LogWarning($"{nameof(HealthBehaviour)}.{nameof(Hurt)} was called before initialization.", this);
            return;
        }

        if (damage <= 0)
        {
            Debug.LogWarning($"{nameof(HealthBehaviour)}.{nameof(Hurt)} received invalid damage: {damage}.", this);
            return;
        }

        instance.currentHP = Mathf.Max(0, instance.currentHP - damage);
        OnHurt?.Invoke(instance.currentHP, MaxHealth);

        if (instance.currentHP == 0)
            OnDie?.Invoke();
    }

    public IEnumerator HurtAnimated(int damage)
    {
        if (instance == null)
        {
            Debug.LogWarning($"{nameof(HealthBehaviour)}.{nameof(HurtAnimated)} was called before initialization.", this);
            yield break;
        }

        if (damage <= 0)
        {
            Debug.LogWarning($"{nameof(HealthBehaviour)}.{nameof(HurtAnimated)} received invalid damage: {damage}.", this);
            yield break;
        }

        int startHP = instance.currentHP;
        int targetHP = Mathf.Max(0, startHP - damage);

        if (targetHP == startHP)
            yield break;

        float duration = Mathf.Clamp(
            (startHP - targetHP) * animationDurationPerPoint,
            minAnimationDuration,
            maxAnimationDuration
        );

        float elapsed = 0f;
        int lastBroadcastHP = startHP;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            int visualHP = Mathf.RoundToInt(Mathf.Lerp(startHP, targetHP, t));
            visualHP = Mathf.Clamp(visualHP, targetHP, startHP);

            if (visualHP != lastBroadcastHP)
            {
                lastBroadcastHP = visualHP;
                OnHurt?.Invoke(visualHP, MaxHealth);
            }

            yield return null;
        }

        instance.currentHP = targetHP;
        OnHurt?.Invoke(instance.currentHP, MaxHealth);

        if (instance.currentHP == 0)
            OnDie?.Invoke();
    }

    public void Heal(int amount)
    {
        if (instance == null)
        {
            Debug.LogWarning($"{nameof(HealthBehaviour)}.{nameof(Heal)} was called before initialization.", this);
            return;
        }

        if (amount <= 0)
        {
            Debug.LogWarning($"{nameof(HealthBehaviour)}.{nameof(Heal)} received invalid healing: {amount}.", this);
            return;
        }

        int previousHP = instance.currentHP;
        instance.currentHP = Mathf.Min(MaxHealth, instance.currentHP + amount);

        if (instance.currentHP != previousHP)
            OnHeal?.Invoke(instance.currentHP, MaxHealth);
    }

    public IEnumerator HealAnimated(int amount)
    {
        if (instance == null)
        {
            Debug.LogWarning($"{nameof(HealthBehaviour)}.{nameof(HealAnimated)} was called before initialization.", this);
            yield break;
        }

        if (amount <= 0)
        {
            Debug.LogWarning($"{nameof(HealthBehaviour)}.{nameof(HealAnimated)} received invalid healing: {amount}.", this);
            yield break;
        }

        int startHP = instance.currentHP;
        int targetHP = Mathf.Min(MaxHealth, startHP + amount);

        if (targetHP == startHP)
            yield break;

        float duration = Mathf.Clamp(
            (targetHP - startHP) * animationDurationPerPoint,
            minAnimationDuration,
            maxAnimationDuration
        );

        float elapsed = 0f;
        int lastBroadcastHP = startHP;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            int visualHP = Mathf.RoundToInt(Mathf.Lerp(startHP, targetHP, t));
            visualHP = Mathf.Clamp(visualHP, startHP, targetHP);

            if (visualHP != lastBroadcastHP)
            {
                lastBroadcastHP = visualHP;
                OnHeal?.Invoke(visualHP, MaxHealth);
            }

            yield return null;
        }

        instance.currentHP = targetHP;
        OnHeal?.Invoke(instance.currentHP, MaxHealth);
    }

    public void HealToFull()
    {
        if (instance == null)
        {
            Debug.LogWarning($"{nameof(HealthBehaviour)}.{nameof(HealToFull)} was called before initialization.", this);
            return;
        }

        if (instance.currentHP == MaxHealth)
            return;

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