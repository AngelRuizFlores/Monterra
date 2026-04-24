using System;

[Serializable]
public class EnemyBarkContext
{
    public string trainerId;
    public string trainerName;
    public string trainerPersonality;
    public string eventType;

    public string enemyMonName;
    public string playerMonName;

    public int enemyCurrentHP;
    public int enemyMaxHP;
    public int playerCurrentHP;
    public int playerMaxHP;

    public string extraInfo;
}