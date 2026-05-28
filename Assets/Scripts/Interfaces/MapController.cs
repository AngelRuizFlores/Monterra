using UnityEngine;
using UnityEngine.InputSystem;

public class MapController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject mapUI;

    private void Awake()
    {
        mapUI.SetActive(false);
    }

    private void Update()
    {
        if (BattleInteractionLock.IsBlocked)
        {
            return;
        }

        if (Keyboard.current == null || mapUI == null)
        {
            return;
        }

        if (Keyboard.current.mKey.wasPressedThisFrame)
        {
            mapUI.SetActive(!mapUI.activeSelf);
        }
    }

    public void CloseMap()
    {
        mapUI.SetActive(false);
    }
}