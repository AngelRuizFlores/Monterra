using UnityEngine;

[RequireComponent(typeof(Camera))]
public class LetterboxCamera : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float targetAspect = 16f / 9f;
    [SerializeField] private bool applyContinuously = true;

    private Camera cameraComponent;

    private void Awake()
    {
        cameraComponent = GetComponent<Camera>();
        ApplyLetterbox();
    }

    private void Start()
    {
        ApplyLetterbox();
    }

    private void Update()
    {
        if (applyContinuously)
        {
            ApplyLetterbox();
        }
    }

    private void ApplyLetterbox()
    {
        if (cameraComponent == null)
        {
            return;
        }

        float windowAspect = (float)Screen.width / Screen.height;
        float scaleHeight = windowAspect / targetAspect;

        Rect rect = new Rect(0f, 0f, 1f, 1f);

        if (scaleHeight < 1f)
        {
            rect.width = 1f;
            rect.height = scaleHeight;
            rect.x = 0f;
            rect.y = (1f - scaleHeight) * 0.5f;
        }
        else
        {
            float scaleWidth = 1f / scaleHeight;

            rect.width = scaleWidth;
            rect.height = 1f;
            rect.x = (1f - scaleWidth) * 0.5f;
            rect.y = 0f;
        }

        cameraComponent.rect = rect;
    }
}