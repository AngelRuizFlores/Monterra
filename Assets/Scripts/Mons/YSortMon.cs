using UnityEngine;

public class YSortMon : MonoBehaviour
{
    [Header("Renderers")]
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private SpriteRenderer headRenderer;

    [Header("Sorting")]
    [SerializeField] private int ySortFactor = 100;
    [SerializeField] private int headOffset = 1;

    private void LateUpdate()
    {
        int order = Mathf.RoundToInt(-transform.position.y * ySortFactor);

        if (bodyRenderer)
        {
            bodyRenderer.sortingOrder = order;
        }

        if (headRenderer)
        {
            headRenderer.sortingOrder = order + headOffset;
        }
    }
}