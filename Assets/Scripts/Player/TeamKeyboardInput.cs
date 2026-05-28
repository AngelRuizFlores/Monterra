using UnityEngine;
using UnityEngine.InputSystem;

public class TeamKeyboardInput : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerTeam team;

    private void Awake()
    {
        if (team == null)
        {
            team = GetComponent<PlayerTeam>() ?? GetComponentInParent<PlayerTeam>();
        }
    }

    private void Update()
    {
        if (team == null)
        {
            return;
        }

        if (Time.timeScale == 0f)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        if (keyboard.digit1Key.wasPressedThisFrame)
        {
            TrySwap(1);
        }
        else if (keyboard.digit2Key.wasPressedThisFrame)
        {
            TrySwap(2);
        }
        else if (keyboard.digit3Key.wasPressedThisFrame)
        {
            TrySwap(3);
        }
        else if (keyboard.digit4Key.wasPressedThisFrame)
        {
            TrySwap(4);
        }
        else if (keyboard.digit5Key.wasPressedThisFrame)
        {
            TrySwap(5);
        }
    }

    private void TrySwap(int index)
    {
        if (index <= 0 || index >= PlayerTeam.MAX_TEAM)
        {
            return;
        }

        if (index >= team.UnlockedSlots)
        {
            return;
        }

        MonInstance instance = team.team[index];

        if (instance == null || instance.species == null)
        {
            return;
        }

        team.SwapToFront(index);
    }
}