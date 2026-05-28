using UnityEngine;

public class PlayerFootsteps : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float minMoveDistance = 0.005f;
    [SerializeField] private float stepInterval = 0.28f;

    [Header("Sounds")]
    [SerializeField] private string[] groundSounds =
    {
        "FootstepGround1",
        "FootstepGround2"
    };

    [SerializeField] private string grassSound = "FootstepGrass";
    [SerializeField] private string waterSound = "FootstepWater";

    private FootstepSurface currentSurface = FootstepSurface.Ground;
    private Vector3 lastPosition;
    private float stepTimer;

    private void Awake()
    {
        lastPosition = transform.position;
        stepTimer = stepInterval;
    }

    private void Update()
    {
        float distance = Vector3.Distance(transform.position, lastPosition);
        lastPosition = transform.position;

        if (distance < minMoveDistance)
        {
            return;
        }

        stepTimer += Time.unscaledDeltaTime;

        if (stepTimer >= stepInterval)
        {
            stepTimer = 0f;
            PlayStep();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        FootstepSurfaceZone zone = other.GetComponent<FootstepSurfaceZone>();

        if (zone == null)
        {
            return;
        }

        currentSurface = zone.Surface;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        FootstepSurfaceZone zone = other.GetComponent<FootstepSurfaceZone>();

        if (zone == null)
        {
            return;
        }

        currentSurface = zone.Surface;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        FootstepSurfaceZone zone = other.GetComponent<FootstepSurfaceZone>();

        if (zone == null)
        {
            return;
        }

        currentSurface = FootstepSurface.Ground;
    }

    private void PlayStep()
    {
        string soundName = GetSoundName();

        if (SoundManager.Instance != null && !string.IsNullOrWhiteSpace(soundName))
        {
            SoundManager.Instance.Play(soundName, false);
            return;
        }

        Debug.LogWarning("[Footsteps] SoundManager missing or soundName empty.");
    }

    private string GetSoundName()
    {
        switch (currentSurface)
        {
            case FootstepSurface.Grass:
                return grassSound;

            case FootstepSurface.Water:
                return waterSound;

            default:
                return GetRandomSound(groundSounds);
        }
    }

    private string GetRandomSound(string[] sounds)
    {
        if (sounds == null || sounds.Length == 0)
        {
            return string.Empty;
        }

        return sounds[UnityEngine.Random.Range(0, sounds.Length)];
    }
}