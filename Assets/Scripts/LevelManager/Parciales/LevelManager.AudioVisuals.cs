using System.Collections;
using System.Reflection;
using UnityEngine;

public partial class LevelManager
{
    private IEnumerator PlayWildBattleCryDelayed(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);

        PlayWildBattleCry();
    }

    private IEnumerator PlayTrainerBattleCryDelayed(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);

        PlayEnemyBattleCry();
    }

    private void RefreshEnemyUI()
    {
        MonInstance enemy = GetCurrentEnemyMon();

        if (enemy == null)
        {
            return;
        }

        battleUI?.UpdateEnemyHP(enemy.currentHP, MonLevelSystem.GetMaxHP(enemy));
    }

    private void RefreshPlayerUI()
    {
        if (playerMon == null || playerMon.instance == null)
        {
            return;
        }

        battleUI?.UpdatePlayerHP(playerMon.instance.currentHP, MonLevelSystem.GetMaxHP(playerMon.instance));
        battleUI?.SetPlayerExp(playerMon.instance);
    }

    private void PlayWildBattleCry()
    {
        if (currentWild == null || currentWild.instance == null || currentWild.instance.species == null)
        {
            return;
        }

        PlaySound(currentWild.instance.species.battleCrySoundName, false);
    }

    private void PlayEnemyBattleCry()
    {
        MonInstance enemy = GetCurrentEnemyMon();

        if (enemy == null || enemy.species == null)
        {
            return;
        }

        PlaySound(enemy.species.battleCrySoundName, false);
    }

    private void PlayMoveSound(MoveData move)
    {
        if (move == null)
        {
            return;
        }

        PlaySound(move.attackSoundName, false);
    }

    private void PlaySound(string soundName, bool loop)
    {
        if (string.IsNullOrWhiteSpace(soundName))
        {
            return;
        }

        SoundManager manager = soundManager != null ? soundManager : SoundManager.Instance;

        if (manager == null)
        {
            Debug.LogWarning($"{nameof(LevelManager)}: no {nameof(SoundManager)} is available to play '{soundName}'.", this);
            return;
        }

        manager.Play(soundName, loop);
    }

    private IEnumerator PlayAttackProjectile(MoveData move, Vector2 startPosition, Vector2 targetPosition)
    {
        if (move == null || move.projectilePrefab == null || battleUI == null || battleUI.GetEffectsContainer() == null)
        {
            yield break;
        }

        bool arrived = false;

        AttackVfxUIProjectile projectileInstance = Instantiate(
            move.projectilePrefab,
            battleUI.GetEffectsContainer()
        );

        projectileInstance.Play(startPosition, targetPosition, () => arrived = true);

        while (!arrived)
        {
            yield return null;
        }

        Destroy(projectileInstance.gameObject);
    }

    private void TryShowTrainerIntro(string trainerName, Sprite trainerSprite)
    {
        if (battleUI == null)
        {
            return;
        }

        MethodInfo method = battleUI.GetType().GetMethod(
            "ShowTrainerIntro",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        );

        if (method != null)
        {
            method.Invoke(battleUI, new object[] { trainerName, trainerSprite });
        }
    }

    private BattleBiome ResolveBattleBiome()
    {
        if (playerMon == null)
        {
            return BattleBiome.Default;
        }

        Vector2 position = playerMon.transform.position;
        Collider2D[] hits = Physics2D.OverlapPointAll(position);

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null)
            {
                continue;
            }

            BattleBiomeZone zone = hits[i].GetComponent<BattleBiomeZone>();

            if (zone != null)
            {
                return zone.Biome;
            }
        }

        return BattleBiome.Default;
    }

    private void ApplyBattleBackground()
    {
        if (battleBackgroundSelector == null)
        {
            return;
        }

        BattleBiome biome = ResolveBattleBiome();

        battleBackgroundSelector.ApplyBackground(biome);
    }
}