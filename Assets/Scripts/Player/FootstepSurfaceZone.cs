using UnityEngine;

public class FootstepSurfaceZone : MonoBehaviour
{
    [Header("Surface")]
    [SerializeField] private FootstepSurface surface = FootstepSurface.Ground;

    public FootstepSurface Surface => surface;
}

public enum FootstepSurface
{
    Ground,
    Grass,
    Water
}