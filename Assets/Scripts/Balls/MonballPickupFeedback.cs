using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MonballPickupFeedback : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private TMP_Text popupText;
    [SerializeField] private Image[] slotHighlights;

    [Header("Settings")]
    [SerializeField] private float displaySeconds = 1.5f;
    [SerializeField] private float pulseScale = 1.25f;
    [SerializeField] private float pulseSpeed = 8f;

    private Coroutine routine;

    private void Awake()
    {
        HideImmediate();
    }

    public void ShowUnlockedSlot(int slotIndex)
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        HideImmediate();

        routine = StartCoroutine(ShowRoutine(slotIndex));
    }

    private Image GetHighlight(int slotIndex)
    {
        if (slotHighlights == null)
        {
            return null;
        }

        if (slotIndex < 0 || slotIndex >= slotHighlights.Length)
        {
            Debug.LogWarning($"{nameof(MonballPickupFeedback)}: slotIndex fuera de rango: {slotIndex}.", this);
            return null;
        }

        return slotHighlights[slotIndex];
    }

    private void HideImmediate()
    {
        if (popupRoot != null)
        {
            popupRoot.SetActive(false);
        }

        if (slotHighlights == null)
        {
            return;
        }

        for (int i = 0; i < slotHighlights.Length; i++)
        {
            if (slotHighlights[i] == null)
            {
                continue;
            }

            slotHighlights[i].rectTransform.localScale = Vector3.one;
            slotHighlights[i].gameObject.SetActive(false);
        }
    }

    private IEnumerator ShowRoutine(int slotIndex)
    {
        if (popupRoot != null)
        {
            popupRoot.SetActive(true);
        }

        if (popupText != null)
        {
            popupText.text = "New team slot unlocked!";
        }

        Image highlight = GetHighlight(slotIndex);
        Vector3 originalScale = Vector3.one;

        if (highlight != null)
        {
            highlight.gameObject.SetActive(true);
            originalScale = highlight.rectTransform.localScale;
        }

        float elapsed = 0f;

        while (elapsed < displaySeconds)
        {
            elapsed += Time.unscaledDeltaTime;

            if (highlight != null)
            {
                float pulse = 1f + Mathf.Sin(elapsed * pulseSpeed) * (pulseScale - 1f);
                highlight.rectTransform.localScale = originalScale * pulse;
            }

            yield return null;
        }

        if (highlight != null)
        {
            highlight.rectTransform.localScale = originalScale;
        }

        HideImmediate();
        routine = null;
    }
}