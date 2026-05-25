using UnityEngine;
using UnityEngine.InputSystem;

public class TypeChartOverlayUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private GameObject chooseMonCanvas;

    private void Awake()
    {
        if (root != null)
            root.SetActive(false);
    }

    private void Update()
    {
        if (chooseMonCanvas != null && chooseMonCanvas.activeInHierarchy)
            return;

        if (Keyboard.current == null)
            return;

        if (Keyboard.current.tKey.wasPressedThisFrame)
            Toggle();
    }

    private void Toggle()
    {
        if (root == null)
            return;

        root.SetActive(!root.activeSelf);
    }

    public void Close()
    {
        if (root != null)
            root.SetActive(false);
    }
    public void Open()
    {
        if (root != null)
            root.SetActive(true);
    }
}