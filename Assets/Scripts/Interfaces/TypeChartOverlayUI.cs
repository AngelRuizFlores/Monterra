using UnityEngine;
using UnityEngine.InputSystem;

public class TypeChartOverlayUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject root;

    [Header("References")]
    [SerializeField] private GameObject chooseMonCanvas;

    private void Awake()
    {
        if (root != null)
        {
            root.SetActive(false);
        }
    }

    private void Update()
    {
        if (BattleInteractionLock.IsBlocked)
        {
            return;
        }

        if (chooseMonCanvas != null && chooseMonCanvas.activeInHierarchy)
        {
            return;
        }

        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            Toggle();
        }
    }

    public void Close()
    {
        if (root != null)
        {
            root.SetActive(false);
        }
    }

    public void Open()
    {
        if (BattleInteractionLock.IsBlocked)
        {
            return;
        }

        if (root != null)
        {
            root.SetActive(true);
        }
    }

    private void Toggle()
    {
        if (root == null)
        {
            return;
        }

        root.SetActive(!root.activeSelf);
    }
}