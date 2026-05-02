using System;
using UnityEngine;
using UnityEngine.Events;

public class StormDamageController : MonoBehaviour
{
    [SerializeField] private StormOverlayController stormController;
    [SerializeField] private PlayerTeam playerTeam;
    [SerializeField] private Transform playerTransform;

    [SerializeField] private float damageIntervalSeconds = 2f;
    [SerializeField] private int damagePerTick = 10;
    [SerializeField] private float gracePeriodSeconds = 0f;

    [SerializeField] private UnityEvent onGameEnd = new UnityEvent();

    private float timeSinceLastDamage;
    private float timeSinceGameStart;
    private bool gameEnded;

    private void OnValidate()
    {
        if (damageIntervalSeconds <= 0f)
            damageIntervalSeconds = 2f;

        if (damagePerTick < 1)
            damagePerTick = 1;

        if (gracePeriodSeconds < 0f)
            gracePeriodSeconds = 0f;
    }

    private void Update()
    {
        if (gameEnded)
        return;
        if (!ValidateDependencies())
            return;

        timeSinceGameStart += Time.deltaTime;

        bool isInStorm = stormController.IsInside(playerTransform.position);

        if (!isInStorm)
        {
            timeSinceLastDamage = 0f;
            return;
        }

        if (timeSinceGameStart < gracePeriodSeconds)
            return;

        timeSinceLastDamage += Time.deltaTime;

        if (timeSinceLastDamage >= damageIntervalSeconds)
        {
            ApplyStormDamage();
            timeSinceLastDamage = 0f;
        }
    }

    private void ApplyStormDamage()
    {
        if (!ValidateDependencies())
            return;

        int aliveCount = 0;
        int affectedCount = 0;

        for (int i = 0; i < playerTeam.team.Length; i++)
        {
            MonInstance creature = playerTeam.team[i];

            if (creature == null || creature.species == null)
                continue;

            if (creature.currentHP <= 0)
                continue;

            creature.currentHP -= damagePerTick;

            if (creature.currentHP < 0)
                creature.currentHP = 0;

            affectedCount++;

            if (creature.currentHP > 0)
                aliveCount++;
        }

        if (aliveCount == 0)
            TriggerGameEnd();
    }

   private void TriggerGameEnd()
    {
        if (gameEnded)
            return;

        gameEnded = true;

        Debug.Log("Game Over: all player creatures were defeated by the storm.");
        onGameEnd?.Invoke();
    }

    private bool ValidateDependencies()
    {
        if (stormController == null)
        {
            Debug.LogError("StormDamageController: StormOverlayController is not assigned.", gameObject);
            return false;
        }

        if (playerTeam == null)
        {
            Debug.LogError("StormDamageController: PlayerTeam is not assigned.", gameObject);
            return false;
        }

        if (playerTransform == null)
        {
            Debug.LogError("StormDamageController: Player Transform is not assigned.", gameObject);
            return false;
        }

        return true;
    }

    public void AddGameOverListener(UnityAction callback)
    {
        if (callback != null)
            onGameEnd.AddListener(callback);
    }

    public void RemoveGameOverListener(UnityAction callback)
    {
        if (callback != null)
            onGameEnd.RemoveListener(callback);
    }
}