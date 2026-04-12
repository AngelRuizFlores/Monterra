using UnityEngine;

public class HeadMirrorBehaviour : MonoBehaviour
{
    public SpriteRenderer body; 
    private SpriteRenderer head;

    void Awake()
    {
        head = GetComponent<SpriteRenderer>();
    }

    void LateUpdate()
    {
        if (!body) return;
        head.sprite = body.sprite;
        head.flipX  = body.flipX;
        head.flipY  = body.flipY;
        head.color  = body.color;
    }
}
