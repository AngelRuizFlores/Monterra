using UnityEngine;

[CreateAssetMenu(
    fileName = "NewMove",
    menuName = "Monterra/Move"
)]
public class MoveData : ScriptableObject
{
    public string moveName;
    public MonType type;
    public int power;
    public int accuracy;
}

