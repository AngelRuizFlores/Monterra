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
#endif
}