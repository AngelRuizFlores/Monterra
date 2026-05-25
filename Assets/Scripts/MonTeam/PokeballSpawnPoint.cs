using System;
using UnityEngine;

public sealed class PokeballSpawnPoint : MonoBehaviour
{
    [SerializeField, HideInInspector] private string pokeballId;

    public string PokeballId => pokeballId;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(pokeballId))
            pokeballId = Guid.NewGuid().ToString("N");
    }

    [ContextMenu("Regenerate Pokeball ID")]
    private void RegeneratePokeballId()
    {
        pokeballId = Guid.NewGuid().ToString("N");
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}