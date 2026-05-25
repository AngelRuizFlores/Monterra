using UnityEngine;

public class FootstepSurfaceZone : MonoBehaviour
{
    public FootstepSurface Surface => surface;

    [SerializeField] private FootstepSurface surface = FootstepSurface.Ground;
}

public enum FootstepSurface
{
    Ground,
    Grass,
    Water
}