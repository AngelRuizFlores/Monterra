public interface IHealable
{
    int CurrentHealth { get; }
    int MaxHealth { get; }
    bool IsInitialized { get; }

    void Heal(int amount);
    void HealToFull();
}