using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class CompanionHintsUI : MonoBehaviour
{
    [System.Serializable]
    private class HintData
    {
        public string message;
        public string soundName;

        public HintData(string message, string soundName)
        {
            this.message = message;
            this.soundName = soundName;
        }
    }

    [Header("UI")]
    [SerializeField] private GameObject root;
    [SerializeField] private Image companionImage;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private TMP_Text toggleText;

    [Header("Settings")]
    [SerializeField] private float firstHintDelay = 5f;
    [SerializeField] private float hintInterval = 90f;
    [SerializeField] private float hintDisplaySeconds = 7f;

    [Header("References")]
    [SerializeField] private PlayerTeam playerTeam;
    [SerializeField] private GameObject battleCanvas;

    private readonly List<HintData> remainingHints = new();

    private bool notificationsEnabled = true;
    private bool starterHintStarted;
    private Coroutine hintRoutine;
    private Coroutine displayRoutine;

    private const string PrefKey = "CompanionHintsEnabled";

    private void Awake()
    {
        notificationsEnabled = true;
        PlayerPrefs.SetInt(PrefKey, 1);
        PlayerPrefs.Save();

        Debug.Log($"[CompanionHints] Awake. notificationsEnabled={notificationsEnabled}", this);

        if (root != null)
            root.SetActive(false);
        else
            Debug.LogWarning("[CompanionHints] Root is not assigned.", this);

        RefreshToggleText();
        BuildHintPool();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
            ToggleNotifications();
    }

    public void StartStarterHints()
    {
        Debug.Log("[CompanionHints] StartStarterHints called.", this);

        if (starterHintStarted)
        {
            Debug.Log("[CompanionHints] Starter hints already started.", this);
            return;
        }

        starterHintStarted = true;

        if (!notificationsEnabled)
        {
            Debug.Log("[CompanionHints] Notifications are disabled.", this);
            return;
        }

        if (hintRoutine != null)
            StopCoroutine(hintRoutine);

        Debug.Log("[CompanionHints] Starting hint routine.", this);
        hintRoutine = StartCoroutine(HintRoutine());
    }

    public void ShowStormClosingHint()
    {
        Debug.Log("[CompanionHints] ShowStormClosingHint called.", this);

        ShowHint(new HintData(
            "The storm is closing in! Press M to open the map!",
            "DropletStormMap"
        ));
    }

    private void ToggleNotifications()
    {
        notificationsEnabled = !notificationsEnabled;

        PlayerPrefs.SetInt(PrefKey, notificationsEnabled ? 1 : 0);
        PlayerPrefs.Save();

        Debug.Log($"[CompanionHints] ToggleNotifications. notificationsEnabled={notificationsEnabled}", this);

        RefreshToggleText();

        if (!notificationsEnabled)
        {
            StopAllHintCoroutines();
            HideRoot();
            return;
        }

        if (starterHintStarted)
        {
            StopAllHintCoroutines();
            hintRoutine = StartCoroutine(HintRoutine());
        }
    }

    private IEnumerator HintRoutine()
    {
        Debug.Log("[CompanionHints] HintRoutine started. Waiting first delay.", this);

        yield return new WaitForSecondsRealtime(firstHintDelay);

        Debug.Log("[CompanionHints] First delay finished.", this);

        if (notificationsEnabled && !IsInBattle())
        {
            Debug.Log("[CompanionHints] Showing starter strength hint.", this);
            ShowStarterStrengthHint();
        }
        else
        {
            Debug.Log(
                $"[CompanionHints] Starter hint blocked. notificationsEnabled={notificationsEnabled}, IsInBattle={IsInBattle()}",
                this
            );
        }

        while (notificationsEnabled)
        {
            yield return new WaitForSecondsRealtime(hintInterval);

            if (!notificationsEnabled)
                yield break;

            if (IsInBattle())
            {
                Debug.Log("[CompanionHints] Random hint skipped because player is in battle.", this);
                continue;
            }

            Debug.Log("[CompanionHints] Showing random unused hint.", this);
            ShowRandomUnusedHint();
        }
    }

    private void BuildHintPool()
    {
        remainingHints.Clear();

        remainingHints.Add(new HintData(
            "To win the game, you must defeat all trainers!",
            "DropletDefeatTrainers"
        ));

        remainingHints.Add(new HintData(
            "Want to release a Mon? Press the red X above its slot.",
            "DropletReleaseMon"
        ));

        remainingHints.Add(new HintData(
            "You can heal your team by interacting with a Bell.",
            "DropletHealBell"
        ));

        remainingHints.Add(new HintData(
            "To capture Mons, you need MonBalls. They are scattered across the map!",
            "DropletMonBalls"
        ));

        remainingHints.Add(new HintData(
            "Your Mons gain experience in battle, and some can evolve!",
            "DropletExperienceEvolution"
        ));

        remainingHints.Add(new HintData(
            "You can switch Mons by pressing the key assigned to each slot.",
            "DropletSlotSwitch"
        ));

        remainingHints.Add(new HintData(
            "During battle, you can switch Mons by pressing the SWITCH button.",
            "DropletBattleSwitch"
        ));

        Debug.Log($"[CompanionHints] Hint pool built. Count={remainingHints.Count}", this);
    }

    private void ShowStarterStrengthHint()
    {
        Debug.Log("[CompanionHints] ShowStarterStrengthHint called.", this);

        MonInstance starter = GetStarterMon();

        if (starter == null)
        {
            Debug.LogWarning("[CompanionHints] Starter is null.", this);
            return;
        }

        if (starter.species == null)
        {
            Debug.LogWarning("[CompanionHints] Starter species is null.", this);
            return;
        }

        Debug.Log($"[CompanionHints] Starter found: {starter.species.monName}, type={starter.species.type}", this);

        MonType type = starter.species.type;

        MonType[] strengths = GetStrengths(type);
        MonType[] weaknesses = GetWeaknesses(type);

        Debug.Log($"[CompanionHints] Strengths={strengths.Length}, Weaknesses={weaknesses.Length}", this);

        if (strengths.Length >= 2)
        {
            ShowHint(new HintData(
                $"Good choice! Your starter is strong against {strengths[0]} and {strengths[1]}.",
                "DropletStarterStrong"
            ));
        }

        if (weaknesses.Length >= 2)
        {
            remainingHints.Add(new HintData(
                $"Be careful! Your starter is weak against {weaknesses[0]} and {weaknesses[1]}.",
                "DropletStarterWeak"
            ));

            Debug.Log("[CompanionHints] Starter weak hint added to random pool.", this);
        }
    }

    private MonInstance GetStarterMon()
    {
        if (playerTeam == null)
        {
            Debug.LogWarning("[CompanionHints] PlayerTeam is null.", this);
            return null;
        }

        if (playerTeam.team == null)
        {
            Debug.LogWarning("[CompanionHints] PlayerTeam.team is null.", this);
            return null;
        }

        if (playerTeam.team.Length == 0)
        {
            Debug.LogWarning("[CompanionHints] PlayerTeam.team is empty.", this);
            return null;
        }

        return playerTeam.team[0];
    }

    private void ShowRandomUnusedHint()
    {
        if (remainingHints.Count == 0)
        {
            Debug.Log("[CompanionHints] Remaining hints empty. Rebuilding pool.", this);
            BuildHintPool();
        }

        if (remainingHints.Count == 0)
        {
            Debug.LogWarning("[CompanionHints] No hints available after rebuild.", this);
            return;
        }

        int index = Random.Range(0, remainingHints.Count);
        HintData hint = remainingHints[index];
        remainingHints.RemoveAt(index);

        Debug.Log($"[CompanionHints] Random hint selected. Remaining={remainingHints.Count}", this);

        ShowHint(hint);
    }

    private void ShowHint(HintData hint)
    {
        Debug.Log($"[CompanionHints] ShowHint called. Message={(hint != null ? hint.message : "NULL")}", this);

        if (!notificationsEnabled)
        {
            Debug.Log("[CompanionHints] ShowHint blocked: notifications disabled.", this);
            return;
        }

        if (hint == null || string.IsNullOrWhiteSpace(hint.message))
        {
            Debug.LogWarning("[CompanionHints] ShowHint blocked: hint null or empty.", this);
            return;
        }

        if (root == null || hintText == null)
        {
            Debug.LogWarning(
                $"[CompanionHints] ShowHint blocked: root null={root == null}, hintText null={hintText == null}",
                this
            );
            return;
        }

        if (IsInBattle())
        {
            Debug.Log("[CompanionHints] ShowHint blocked: player is in battle.", this);
            return;
        }

        if (displayRoutine != null)
            StopCoroutine(displayRoutine);

        Debug.Log("[CompanionHints] Starting display routine.", this);
        displayRoutine = StartCoroutine(ShowHintRoutine(hint));
    }

    private IEnumerator ShowHintRoutine(HintData hint)
    {
        Debug.Log($"[CompanionHints] Showing hint on UI: {hint.message} | sound={hint.soundName}", this);

        hintText.text = hint.message;
        RefreshToggleText();

        root.SetActive(true);

        if (SoundManager.Instance != null && !string.IsNullOrWhiteSpace(hint.soundName))
        {
            Debug.Log($"[CompanionHints] Playing sound: {hint.soundName}", this);
            SoundManager.Instance.Play(hint.soundName, false);
        }
        else
        {
            Debug.LogWarning(
                $"[CompanionHints] Sound skipped. SoundManager null={SoundManager.Instance == null}, soundName={hint.soundName}",
                this
            );
        }

        yield return new WaitForSecondsRealtime(hintDisplaySeconds);

        Debug.Log("[CompanionHints] Hiding hint.", this);
        HideRoot();
        displayRoutine = null;
    }

    private void HideRoot()
    {
        if (root != null)
            root.SetActive(false);
    }

    private void StopAllHintCoroutines()
    {
        if (hintRoutine != null)
            StopCoroutine(hintRoutine);

        if (displayRoutine != null)
            StopCoroutine(displayRoutine);

        hintRoutine = null;
        displayRoutine = null;
    }

    private void RefreshToggleText()
    {
        if (toggleText == null)
            return;

        toggleText.text = notificationsEnabled
            ? "Press P to disable notifications"
            : "Press P to enable notifications";
    }

    private bool IsInBattle()
    {
        return battleCanvas != null && battleCanvas.activeInHierarchy;
    }

    private MonType[] GetStrengths(MonType type)
    {
        switch (type)
        {
            case MonType.Water:
                return new[] { MonType.Fire, MonType.Earth };

            case MonType.Fire:
                return new[] { MonType.Grass, MonType.Shadow };

            case MonType.Grass:
                return new[] { MonType.Water, MonType.Earth };

            case MonType.Light:
                return new[] { MonType.Shadow, MonType.Fire };

            case MonType.Shadow:
                return new[] { MonType.Light, MonType.Grass };

            case MonType.Earth:
                return new[] { MonType.Fire, MonType.Light };

            default:
                return new MonType[0];
        }
    }

    private MonType[] GetWeaknesses(MonType type)
    {
        switch (type)
        {
            case MonType.Water:
                return new[] { MonType.Grass, MonType.Light };

            case MonType.Fire:
                return new[] { MonType.Water, MonType.Earth };

            case MonType.Grass:
                return new[] { MonType.Fire, MonType.Shadow };

            case MonType.Light:
                return new[] { MonType.Shadow, MonType.Earth };

            case MonType.Shadow:
                return new[] { MonType.Light, MonType.Fire };

            case MonType.Earth:
                return new[] { MonType.Water, MonType.Grass };

            default:
                return new MonType[0];
        }
    }
}