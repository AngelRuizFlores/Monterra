using UnityEngine;

public class PlayerWaterVisual : MonoBehaviour
{
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private GameObject headMaskRoot;

    private int waterContacts;

    private void Awake()
    {
        ApplyVisualState(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        FootstepSurfaceZone zone = other.GetComponent<FootstepSurfaceZone>();

        if (zone == null)
        {
            return;
        }

        if (zone.Surface != FootstepSurface.Water)
        {
            return;
        }

        waterContacts++;
        ApplyVisualState(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        FootstepSurfaceZone zone = other.GetComponent<FootstepSurfaceZone>();

        if (zone == null)
        {
            return;
        }

        if (zone.Surface != FootstepSurface.Water)
        {
            return;
        }

        waterContacts = Mathf.Max(0, waterContacts - 1);
        ApplyVisualState(waterContacts > 0);
    }

    private void ApplyVisualState(bool inWater)
    {
        if (bodyRenderer != null)
        {
            bodyRenderer.enabled = !inWater;
        }

        if (headMaskRoot != null)
        {
            headMaskRoot.SetActive(inWater);
        }
    }
}