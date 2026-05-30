using UnityEngine;
using UnityEngine.InputSystem;

public class MapController : MonoBehaviour
{
    [SerializeField] private GameObject mapUI;
    [SerializeField] private GameObject battleCanvas;

    private void Awake()
    {
        if (mapUI != null)
        {
            mapUI.SetActive(false);
        }
    }

    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (IsBattleActive())
        {
            if (mapUI != null && mapUI.activeSelf)
            {
                mapUI.SetActive(false);
            }

            return;
        }

        if (Keyboard.current.mKey.wasPressedThisFrame)
        {
            ToggleMap();
        }
    }

    private void ToggleMap()
    {
        if (mapUI == null)
        {
            return;
        }

        mapUI.SetActive(!mapUI.activeSelf);
    }

    private bool IsBattleActive()
    {
        return battleCanvas != null && battleCanvas.activeInHierarchy;
    }
}