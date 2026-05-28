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

public enum EvolutionStage
{
    Base = 0,
    Stage1 = 1,
    Stage2 = 2
}

[CreateAssetMenu(fileName = "NewMon", menuName = "Monterra/Mon Species")]
public class MonSpecies : ScriptableObject
{
    [Header("Basic Info")]
    public string monName;
    public MonType type;

    [Header("Base Stats")]
    public int baseHP;
    public int hpPerLevel = 5;
    public int baseAttack;
    public int attackPerLevel = 5;
    public int baseDefense;
    public int baseSpeed;

    [Header("Visuals")]
    public Sprite frontSprite;
    public Sprite backSprite;
    public Sprite typeSprite;

    [Header("Audio")]
    public string battleCrySoundName;

    [Header("Capture")]
    [Range(1, 100)] public int baseCatchRate = 50;
    public EvolutionStage evolutionStage = EvolutionStage.Base;

    [Header("Learnset")]
    public LearnableMove[] learnableMoves;

    [Header("Evolution")]
    public EvolutionRule[] evolutions;
}