using UnityEngine;

public sealed class TrainerSafePoint : MonoBehaviour
{
    [SerializeField] private bool available = true;

    public bool Available => available;
    public Vector3 Position => transform.position;
}