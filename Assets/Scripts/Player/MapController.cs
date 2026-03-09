using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class MapController : MonoBehaviour
{
    public GameObject mapUI;

    void Awake()
    {
        mapUI.SetActive(false);
    }
    void Update()
    {
        if (Keyboard.current.mKey.wasPressedThisFrame && mapUI.activeSelf == false)
        {            
           mapUI.SetActive(true);
        }
        else if (Keyboard.current.mKey.wasPressedThisFrame && mapUI.activeSelf == true)
        {
            mapUI.SetActive(false);
        }
    }
}
