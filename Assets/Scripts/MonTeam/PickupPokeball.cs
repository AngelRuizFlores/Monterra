using UnityEngine;

public class PickupPokeball : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D col)
    {
        var team = col.GetComponent<PlayerTeam>();
        if (team == null) team = col.GetComponentInParent<PlayerTeam>();
        if (team == null) return;

        bool ok = team.UnlockNextSlot();
        if (ok) gameObject.SetActive(false);
    }
}
