using UnityEngine;

public class HeadMirrorBehaviour : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer body;

    private SpriteRenderer head;

    private void Awake()
    {
        head = GetComponent<SpriteRenderer>();
    }

    private void LateUpdate()
    {
        if (!body)
        {
            return;
        }

        head.sprite = body.sprite;
        head.flipX = body.flipX;
        head.flipY = body.flipY;
        head.color = body.color;
    }
}