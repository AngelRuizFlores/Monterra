using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerTeam : MonoBehaviour
{
    public const int MAX_TEAM = 6;

    [SerializeField] public PlayerMon playerMon;
    [SerializeField] private int unlockedSlots = 1;

    public MonInstance[] team = new MonInstance[MAX_TEAM];

    public int UnlockedSlots => Mathf.Clamp(unlockedSlots, 1, MAX_TEAM);
    public int ActiveIndex { get; private set; }

    public event Action OnChanged;

    private void Awake()
    {
        if (team == null || team.Length != MAX_TEAM)
            team = new MonInstance[MAX_TEAM];
    }

    public MonInstance GetActiveMon()
    {
        if (team == null)
            return null;

        if (ActiveIndex < 0 || ActiveIndex >= team.Length)
            return null;

        return team[ActiveIndex];
    }

    public void InitWithStarter(MonInstance starter)
    {
        unlockedSlots = 1;

        for (int i = 0; i < MAX_TEAM; i++)
            team[i] = null;

        team[0] = starter;
        ActiveIndex = 0;

        ApplyActiveToPlayerMon();
        OnChanged?.Invoke();
    }

    public bool UnlockNextSlot()
    {
        if (unlockedSlots >= MAX_TEAM)
            return false;

        unlockedSlots++;

        int newIndex = unlockedSlots - 1;
        if (newIndex >= 0 && newIndex < team.Length)
            team[newIndex] = null;

        OnChanged?.Invoke();
        return true;
    }

    public int GetNextFreeSlotIndex()
    {
        int limit = UnlockedSlots;

        for (int i = 0; i < limit; i++)
        {
            if (team[i] == null)
                return i;

            if (team[i].species == null)
                return i;
        }

        return -1;
    }

    public bool TryAddToNextFreeSlot(MonInstance mon)
    {
        if (mon == null)
            return false;

        int idx = GetNextFreeSlotIndex();
        if (idx < 0)
            return false;

        team[idx] = mon;

        if (playerMon != null && playerMon.instance == null)
        {
            ActiveIndex = idx;
            ApplyActiveToPlayerMon();
        }

        OnChanged?.Invoke();
        return true;
    }

    public bool SetActiveIndex(int idx)
    {
        if (idx < 0 || idx >= UnlockedSlots)
            return false;

        if (team[idx] == null)
            return false;

        ActiveIndex = idx;
        ApplyActiveToPlayerMon();
        OnChanged?.Invoke();
        return true;
    }

    public bool SwapToFront(int idx)
    {
        if (idx < 0 || idx >= UnlockedSlots)
            return false;

        if (team[idx] == null)
            return false;

        if (idx == 0)
        {
            ActiveIndex = 0;
            ApplyActiveToPlayerMon();
            OnChanged?.Invoke();
            return true;
        }

        (team[0], team[idx]) = (team[idx], team[0]);

        ActiveIndex = 0;
        ApplyActiveToPlayerMon();
        OnChanged?.Invoke();
        return true;
    }

    public bool RemoveAt(int idx)
    {
        if (idx < 0 || idx >= UnlockedSlots)
            return false;

        if (team[idx] == null)
            return false;

        team[idx] = null;

        if (idx == ActiveIndex && playerMon != null)
        {
            playerMon.instance = null;
            playerMon.species = null;
            playerMon.lvl = 1;
        }

        OnChanged?.Invoke();
        return true;
    }

    public int CountAliveMons()
    {
        int count = 0;
        int limit = Mathf.Min(UnlockedSlots, team.Length);

        for (int i = 0; i < limit; i++)
        {
            if (team[i] != null && team[i].species != null && team[i].currentHP > 0)
                count++;
        }

        return count;
    }

    public bool AreAllMonsFainted()
    {
        return CountAliveMons() <= 0;
    }

    public List<MonInstance> GetOwnedMons()
    {
        List<MonInstance> result = new List<MonInstance>();
        int limit = Mathf.Min(UnlockedSlots, team.Length);

        for (int i = 0; i < limit; i++)
        {
            if (team[i] != null && team[i].species != null)
                result.Add(team[i]);
        }

        return result;
    }

    private void ApplyActiveToPlayerMon()
    {
        if (playerMon == null)
            return;

        playerMon.instance = GetActiveMon();

        if (playerMon.instance != null && playerMon.instance.species != null)
        {
            playerMon.species = playerMon.instance.species;
            playerMon.lvl = playerMon.instance.level;
        }
    }
}