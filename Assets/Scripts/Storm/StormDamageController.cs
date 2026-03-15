using System;
using UnityEngine;
using UnityEngine.Events;

public class StormDamageController : MonoBehaviour
{
    [SerializeField] private StormOverlayController stormController;
    [SerializeField] private PlayerTeam playerTeam;
    
    // ✅ NUEVO: Referencia explícita a la posición del jugador
    [SerializeField] private Transform playerTransform;
    
    [SerializeField] private float damageIntervalSeconds = 2f;
    [SerializeField] private int damagePerTick = 10;
    [SerializeField] private float gracePeriodSeconds = 0f;

    [SerializeField] private UnityEvent onGameEnd = new UnityEvent();

    private float timeSinceLastDamage;
    private float timeSinceGameStart;

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
        if (!ValidateDependencies())
            return;

        timeSinceGameStart += Time.deltaTime;

        bool isCurrentlyInStorm = stormController.IsInside(playerTransform.position);

        if (!isCurrentlyInStorm)
        {
            timeSinceLastDamage = 0f;
            return;
        }

        // No aplicar daño durante el período de gracia inicial
        if (timeSinceGameStart < gracePeriodSeconds)
        {
            return;
        }

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

            Debug.Log($"[Storm] {creature.species.monName} recibe {damagePerTick} daño. HP: {creature.currentHP}");

            if (creature.currentHP > 0)
                aliveCount++;
        }

        if (aliveCount == 0)
        {
            TriggerGameEnd();
        }
    }

    private void TriggerGameEnd()
    {
        Debug.Log("[Game Over] Todas las criaturas del jugador han sido derrotadas por la tormenta.");
        onGameEnd?.Invoke();
    }

    private bool ValidateDependencies()
    {
        if (stormController == null)
        {
            Debug.LogError("[StormDamageController] StormOverlayController no está asignado.", gameObject);
            return false;
        }

        if (playerTeam == null)
        {
            Debug.LogError("[StormDamageController] PlayerTeam no está asignado.", gameObject);
            return false;
        }

        // ✅ NUEVO: Valida playerTransform
        if (playerTransform == null)
        {
            Debug.LogError("[StormDamageController] Player Transform no está asignado.", gameObject);
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