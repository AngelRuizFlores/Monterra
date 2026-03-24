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

    public void Play(Vector2 startAnchoredPos, Vector2 endAnchoredPos, Action onArriveCallback)
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        onArrive = onArriveCallback;
        rectTransform.anchoredPosition = startAnchoredPos;

        if (flipXByDirection && image != null)
            image.rectTransform.localScale = new Vector3(
                endAnchoredPos.x < startAnchoredPos.x ? -1f : 1f,
                1f,
                1f
            );

        StartCoroutine(TravelCoroutine(startAnchoredPos, endAnchoredPos));
    }

    private IEnumerator TravelCoroutine(Vector2 startAnchoredPos, Vector2 endAnchoredPos)
    {
        float duration = Mathf.Max(0.01f, travelTime);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            rectTransform.anchoredPosition = Vector2.Lerp(startAnchoredPos, endAnchoredPos, t);
            yield return null;
        }

        rectTransform.anchoredPosition = endAnchoredPos;
        onArrive?.Invoke();
    }
}