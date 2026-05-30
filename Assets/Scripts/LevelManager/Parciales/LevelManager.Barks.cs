using System.Collections;
using UnityEngine;

public partial class LevelManager
{
    private EnemyBarkContext BuildEnemyBarkContext(string eventType, string extraInfo = "")
    {
        MonInstance enemy = GetCurrentEnemyMon();
        MonInstance player = playerMon != null ? playerMon.instance : null;

        string trainerId = currentTrainer != null ? currentTrainer.TrainerId : "wild";

        string trainerName = currentTrainer != null && currentTrainer.TrainerDefinition != null
            ? currentTrainer.TrainerDefinition.TrainerName
            : "Wild";

        string personality = currentTrainer != null && currentTrainer.TrainerDefinition != null
            ? currentTrainer.TrainerDefinition.BarkPersonality
            : "Neutral tone.";

        return new EnemyBarkContext
        {
            trainerId = trainerId,
            trainerName = trainerName,
            trainerPersonality = personality,
            eventType = eventType,

            enemyMonName = enemy != null && enemy.species != null ? enemy.species.monName : "Enemy",
            playerMonName = player != null && player.species != null ? player.species.monName : "Player mon",

            enemyCurrentHP = enemy != null ? enemy.currentHP : 0,
            enemyMaxHP = enemy != null ? MonLevelSystem.GetMaxHP(enemy) : 0,
            playerCurrentHP = player != null ? player.currentHP : 0,
            playerMaxHP = player != null ? MonLevelSystem.GetMaxHP(player) : 0,

            extraInfo = extraInfo
        };
    }

    private IEnumerator TryShowEnemyBark(string eventType, string fallbackText = null, string extraInfo = "")
    {
        if (!enableApiBarks || enemyBarkApiClient == null || encounterType != EncounterType.Trainer)
        {
            if (!string.IsNullOrWhiteSpace(fallbackText))
            {
                battleUI?.SetText(fallbackText);
            }

            yield break;
        }

        EnemyBarkContext context = BuildEnemyBarkContext(eventType, extraInfo);
        string bark = null;

        yield return enemyBarkApiClient.RequestBark(
            context,
            response =>
            {
                bark = response.bark;
            },
            error =>
            {
                Debug.LogWarning($"[Enemy Bark API] Failed: {error}", this);
            }
        );

        if (!string.IsNullOrWhiteSpace(bark))
        {
            battleUI?.SetText(bark);
            yield return new WaitForSecondsRealtime(3f);
            yield break;
        }

        if (!string.IsNullOrWhiteSpace(fallbackText))
        {
            battleUI?.SetText(fallbackText);
            yield return new WaitForSecondsRealtime(3f);
        }
    }
}