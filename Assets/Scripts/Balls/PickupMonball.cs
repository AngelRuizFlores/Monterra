using UnityEngine;

[RequireComponent(typeof(MonballSpawnPoint))]
public class PickupMonball : MonoBehaviour
{
    private const string BallSound = "BallSound";

    private MonballSpawnPoint spawnPoint;

    private void Awake()
    {
        spawnPoint = GetComponent<MonballSpawnPoint>();
    }

    private void OnEnable()
    {
        if (spawnPoint == null)
        {
            spawnPoint = GetComponent<MonballSpawnPoint>();
        }

        if (spawnPoint == null)
        {
            return;
        }

        if (SaveGameManager.IsPokeballCollected(spawnPoint.MonballId))
        {
            gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (spawnPoint != null && SaveGameManager.IsPokeballCollected(spawnPoint.MonballId))
        {
            gameObject.SetActive(false);
            return;
        }

        PlayerTeam team = col.GetComponent<PlayerTeam>();

        if (team == null)
        {
            team = col.GetComponentInParent<PlayerTeam>();
        }

        if (team == null)
        {
            return;
        }

        bool ok = team.UnlockNextSlot();

        if (!ok)
        {
            return;
        }

        int unlockedTeamSlotIndex = team.UnlockedSlots - 1;
        int unlockedVisualSlotIndex = unlockedTeamSlotIndex - 1;

        MonballPickupFeedback feedback = FindFirstObjectByType<MonballPickupFeedback>();

        if (feedback != null)
        {
            feedback.ShowUnlockedSlot(unlockedVisualSlotIndex);
        }

        if (spawnPoint != null)
        {
            SaveGameManager.RegisterCollectedPokeball(spawnPoint.MonballId);
        }

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.Play(BallSound, false);
        }

        gameObject.SetActive(false);
    }
}