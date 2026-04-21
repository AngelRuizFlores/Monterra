using UnityEngine;

[DisallowMultipleComponent]
public sealed class BattleBiomeZone : MonoBehaviour
{
    [SerializeField] private BattleBiome biome = BattleBiome.Default;

    public BattleBiome Biome => biome;

    private void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.isTrigger = true;
    }
}