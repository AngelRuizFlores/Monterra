using UnityEngine;
using UnityEngine.InputSystem;

public class TeamKeyboardInput : MonoBehaviour
{
    [SerializeField] private PlayerTeam team;

    void Awake()
    {
        if (team == null) team = GetComponent<PlayerTeam>() ?? GetComponentInParent<PlayerTeam>();
    }

    void Update()
    {
        if (team == null) return;

        if (Time.timeScale == 0f) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.digit1Key.wasPressedThisFrame) TrySwap(1);
        else if (kb.digit2Key.wasPressedThisFrame) TrySwap(2);
        else if (kb.digit3Key.wasPressedThisFrame) TrySwap(3);
        else if (kb.digit4Key.wasPressedThisFrame) TrySwap(4);
        else if (kb.digit5Key.wasPressedThisFrame) TrySwap(5);
    }

    void TrySwap(int idx)
    {
        if (idx <= 0 || idx >= PlayerTeam.MAX_TEAM) return;
        if (idx >= team.UnlockedSlots) return;

        var inst = team.team[idx];
        if (inst == null || inst.species == null) return;

        team.SwapToFront(idx);
    }
}