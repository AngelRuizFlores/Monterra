using UnityEngine;

public class ScrollingBackgroundUI : MonoBehaviour
{
    [Header("Backgrounds")]
    [SerializeField] private RectTransform bg1;
    [SerializeField] private RectTransform bg2;

    [Header("Settings")]
    [SerializeField] private float speed = 50f;

    private float width;

    private void Start()
    {
        width = bg1.rect.width;
    }

    private void Update()
    {
        Move(bg1);
        Move(bg2);

        if (bg1.anchoredPosition.x >= width)
        {
            bg1.anchoredPosition = new Vector2(bg2.anchoredPosition.x - width, bg1.anchoredPosition.y);
        }

        if (bg2.anchoredPosition.x >= width)
        {
            bg2.anchoredPosition = new Vector2(bg1.anchoredPosition.x - width, bg2.anchoredPosition.y);
        }
    }

    private void Move(RectTransform background)
    {
        background.anchoredPosition += Vector2.right * speed * Time.deltaTime;
    }
}