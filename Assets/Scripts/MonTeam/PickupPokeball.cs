using UnityEngine;

[RequireComponent(typeof(PokeballSpawnPoint))]
public class PickupPokeball : MonoBehaviour
{
    private const string BallSound = "BallSound";

    private PokeballSpawnPoint spawnPoint;

    private void Awake()
    {
        spawnPoint = GetComponent<PokeballSpawnPoint>();
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        var team = col.GetComponent<PlayerTeam>();
        if (team == null)
            team = col.GetComponentInParent<PlayerTeam>();

        if (team == null)
            return;

        bool ok = team.UnlockNextSlot();

        if (!ok)
            return;

        int unlockedSlotIndex = team.UnlockedSlots - 1;

        PokeballPickupFeedback feedback = FindFirstObjectByType<PokeballPickupFeedback>();
        if (feedback != null)
            feedback.ShowUnlockedSlot(unlockedSlotIndex);

        if (spawnPoint != null)
            SaveGameManager.RegisterCollectedPokeball(spawnPoint.PokeballId);

        if (SoundManager.Instance != null)
            SoundManager.Instance.Play(BallSound, false);

        gameObject.SetActive(false);
    }
}