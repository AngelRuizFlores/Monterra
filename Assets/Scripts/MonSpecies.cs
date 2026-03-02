using System;
using UnityEngine;

[Serializable]
public class LearnableMove
{
    public MoveData move;
    public int learnAtLevel;
}

[Serializable]
public class EvolutionRule
{
    public MonSpecies evolvesTo;
    public int evolveAtLevel = 10;
}

[CreateAssetMenu(fileName = "NewMon", menuName = "Monterra/Mon Species")]
public class MonSpecies : ScriptableObject
{
    [Header("Basic Info")]
    public string monName;
    public MonType type;

    [Header("Base Stats")]
    public int baseHP;
    public int baseAttack;
    public int baseDefense;
    public int baseSpeed;

    [Header("Visuals")]
    public Sprite frontSprite;
    public Sprite backSprite;
    public Sprite typeSprite;

    [Header("Learnset")]
    public LearnableMove[] learnableMoves;

    [Header("Evolution")]
    public EvolutionRule[] evolutions;

}
