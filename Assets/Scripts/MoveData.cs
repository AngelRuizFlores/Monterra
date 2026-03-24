using UnityEngine;

[CreateAssetMenu(
    fileName = "NewMove",
    menuName = "Monterra/Move"
)]


public class MoveData : ScriptableObject
{
    [Header("VFX")]
    public AttackVfxUIProjectile projectilePrefab;
    public GameObject impactPrefab;
    [Header("Move Info")]
    public string moveName;
    public MonType type;
    public int power;
    public int accuracy;

    [Header("Audio")]
    public string attackSoundName;
}