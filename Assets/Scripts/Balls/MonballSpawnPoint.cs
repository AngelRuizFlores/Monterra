using System;
using UnityEngine;

public sealed class MonballSpawnPoint : MonoBehaviour
{
    [SerializeField, HideInInspector] private string monballId;

    public string MonballId => monballId;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(monballId))
        {
            monballId = Guid.NewGuid().ToString("N");
        }
    }

    [ContextMenu("Regenerate Monball ID")]
    private void RegenerateMonballId()
    {
        monballId = Guid.NewGuid().ToString("N");
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}