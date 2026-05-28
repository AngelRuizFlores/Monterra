using UnityEngine;

public class RotationBehaviour : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float rotationSpeed = 90f;
    [SerializeField] private bool useUnscaledTime = false;
    [SerializeField] private bool randomizeStartRotation = true;
    [SerializeField] private bool randomizeDirection = true;

    private float direction = 1f;

    private void Awake()
    {
        if (randomizeStartRotation)
        {
            transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        }

        if (randomizeDirection)
        {
            direction = Random.value < 0.5f ? -1f : 1f;
        }
    }

    private void Update()
    {
        float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        transform.Rotate(0f, 0f, rotationSpeed * direction * deltaTime);
    }
}