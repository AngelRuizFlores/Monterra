using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BellHealStation : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private float cooldownSeconds = 15f;

    [Header("Audio")]
    [SerializeField] private string bellAudioName = "Bell";

    private bool isOnCooldown;
    private Collider2D triggerCollider;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider2D>();

        if (triggerCollider == null)
        {
            Debug.LogError("BellHealStation requires a Collider2D.", this);
            enabled = false;
            return;
        }

        if (!triggerCollider.isTrigger)
            triggerCollider.isTrigger = true;

        if (cooldownSeconds <= 0f)
            cooldownSeconds = 15f;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!enabled || isOnCooldown)
            return;

        if (!IsInLayerMask(collision.gameObject.layer, playerLayer))
            return;

        PlayerTeam playerTeam = collision.GetComponentInParent<PlayerTeam>();

        if (playerTeam == null)
        {
            Debug.LogWarning("BellHealStation could not find PlayerTeam on the player.", collision);
            return;
        }

        int healed = HealTeam(playerTeam);

        if (healed > 0)
        {
            PlayBellSound();
            StartCoroutine(CooldownRoutine());
        }
    }

    private int HealTeam(PlayerTeam team)
    {
        if (team.team == null)
            return 0;

        int healedCount = 0;
        int limit = Mathf.Min(team.UnlockedSlots, team.team.Length);

        for (int i = 0; i < limit; i++)
        {
            MonInstance mon = team.team[i];

            if (mon == null || mon.species == null)
                continue;

            int maxHP = MonLevelSystem.GetMaxHP(mon);

            if (mon.currentHP < maxHP)
            {
                mon.currentHP = maxHP;
                healedCount++;
            }
        }

        return healedCount;
    }

    private void PlayBellSound()
    {
        if (SoundManager.Instance == null)
        {
            Debug.LogWarning("SoundManager not found.");
            return;
        }

        SoundManager.Instance.PlaySound(bellAudioName, false);
    }

    private IEnumerator CooldownRoutine()
    {
        isOnCooldown = true;
        yield return new WaitForSeconds(cooldownSeconds);
        isOnCooldown = false;
    }

    private bool IsInLayerMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }
}