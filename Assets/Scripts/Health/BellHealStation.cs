using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BellHealStation : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private LayerMask playerLayer;

    [Header("Audio")]
    [SerializeField] private string bellAudioName = "Bell";

    private Collider2D triggerCollider;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider2D>();

        if (triggerCollider == null)
        {
            Debug.LogError($"{nameof(BellHealStation)} requires a Collider2D.", this);
            enabled = false;
            return;
        }

        if (!triggerCollider.isTrigger)
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        TryHealFromCollision(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        TryHealFromCollision(collision);
    }

    private void TryHealFromCollision(Collider2D collision)
    {
        if (!enabled)
        {
            return;
        }

        if (!IsInLayerMask(collision.gameObject.layer, playerLayer))
        {
            return;
        }

        PlayerTeam playerTeam = collision.GetComponentInParent<PlayerTeam>();

        if (playerTeam == null)
        {
            Debug.LogWarning($"{nameof(BellHealStation)} could not find PlayerTeam on the player.", collision);
            return;
        }

        int healed = HealTeam(playerTeam);

        if (healed <= 0)
        {
            return;
        }

        PlayBellSound();
    }

    private int HealTeam(PlayerTeam team)
    {
        if (team == null || team.team == null)
        {
            return 0;
        }

        int healedCount = 0;
        int limit = Mathf.Min(team.UnlockedSlots, team.team.Length);

        for (int i = 0; i < limit; i++)
        {
            MonInstance mon = team.team[i];

            if (mon == null || mon.species == null)
            {
                continue;
            }

            int maxHP = MonLevelSystem.GetMaxHP(mon);

            if (mon.currentHP < maxHP)
            {
                mon.currentHP = maxHP;
                healedCount++;
            }
        }

        if (healedCount > 0)
        {
            team.NotifyChanged();
        }

        return healedCount;
    }

    private void PlayBellSound()
    {
        if (SoundManager.Instance == null)
        {
            Debug.LogWarning($"{nameof(BellHealStation)}: SoundManager not found.");
            return;
        }

        SoundManager.Instance.PlaySound(bellAudioName, false);
    }

    private bool IsInLayerMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }
}