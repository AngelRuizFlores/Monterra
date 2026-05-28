using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class AttackVfxUIProjectile : MonoBehaviour
{
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private Image image;
    [SerializeField] private float travelTime = 0.35f;
    [SerializeField] private bool flipXByDirection = true;

    private Action onArrive;

    private void Reset()
    {
        rectTransform = GetComponent<RectTransform>();
        image = GetComponent<Image>();
    }

    public void Play(Vector2 startPosition, Vector2 endPosition, Action onArriveCallback)
    {
        EnsureReferences();

        onArrive = onArriveCallback;
        rectTransform.anchoredPosition = startPosition;

        if (flipXByDirection && image != null)
        {
            float scaleX = endPosition.x < startPosition.x ? -1f : 1f;
            image.rectTransform.localScale = new Vector3(scaleX, 1f, 1f);
        }

        StartCoroutine(Travel(startPosition, endPosition));
    }

    public void SetSprite(Sprite sprite)
    {
        EnsureReferences();

        if (image != null)
        {
            image.sprite = sprite;
            image.enabled = sprite != null;
        }
    }

    private void EnsureReferences()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        if (image == null)
        {
            image = GetComponent<Image>();
        }
    }

    private IEnumerator Travel(Vector2 startPosition, Vector2 endPosition)
    {
        float duration = Mathf.Max(0.01f, travelTime);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            rectTransform.anchoredPosition = Vector2.Lerp(startPosition, endPosition, t);

            yield return null;
        }

        rectTransform.anchoredPosition = endPosition;
        onArrive?.Invoke();
    }
}