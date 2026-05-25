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
    [SerializeField] private float timeBetweenQueuedHints = 1f;

    [Header("References")]
    [SerializeField] private PlayerTeam playerTeam;
    [SerializeField] private GameObject battleCanvas;

    private readonly List<HintData> remainingHints = new();
    private readonly Queue<HintData> hintQueue = new();

    private bool notificationsEnabled = true;
    private bool starterHintStarted;
    private Coroutine hintRoutine;
    private Coroutine queueRoutine;

    private const string PrefKey = "CompanionHintsEnabled";

    private void Awake()
    {
        notificationsEnabled = true;
        PlayerPrefs.SetInt(PrefKey, 1);
        PlayerPrefs.Save();

        if (root != null)
            root.SetActive(false);

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
        if (starterHintStarted)
            return;

        starterHintStarted = true;

        if (!notificationsEnabled)
            return;

        if (hintRoutine != null)
            StopCoroutine(hintRoutine);

        hintRoutine = StartCoroutine(HintRoutine());
    }

    public void ShowStormClosingHint()
    {
        QueueHint(new HintData(
            "The storm is closing in! Press M to open the map!",
            "DropletStormMap"
        ));
    }

    private void ToggleNotifications()
    {
        notificationsEnabled = !notificationsEnabled;

        PlayerPrefs.SetInt(PrefKey, notificationsEnabled ? 1 : 0);
        PlayerPrefs.Save();

        RefreshToggleText();

        if (!notificationsEnabled)
        {
            StopAllHintCoroutines();
            HideRoot();
            hintQueue.Clear();
            return;
        }

        if (starterHintStarted)
        {
            if (hintRoutine != null)
                StopCoroutine(hintRoutine);

            hintRoutine = StartCoroutine(HintRoutine());
        }
    }

    private IEnumerator HintRoutine()
    {
        yield return new WaitForSecondsRealtime(firstHintDelay);

        if (notificationsEnabled && !IsInBattle())
            ShowStarterStrengthHint();

        while (notificationsEnabled)
        {
            yield return new WaitForSecondsRealtime(hintInterval);

            if (!notificationsEnabled)
                yield break;

            if (IsInBattle())
                continue;

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
    }

    private void ShowStarterStrengthHint()
    {
        MonInstance starter = GetStarterMon();

        if (starter == null || starter.species == null)
            return;

        MonType type = starter.species.type;

        MonType[] strengths = GetStrengths(type);
        MonType[] weaknesses = GetWeaknesses(type);

        if (strengths.Length >= 2)
        {
            QueueHint(new HintData(
                $"Good choice! Your Mon is strong against {ColorType(strengths[0])} and {ColorType(strengths[1])}.",
                "DropletStarterStrong"
            ));
        }

        if (weaknesses.Length >= 2)
        {
            remainingHints.Add(new HintData(
                $"Be careful! Your Mon is weak against {ColorType(weaknesses[0])} and {ColorType(weaknesses[1])}.",
                "DropletStarterWeak"
            ));
        }
    }

    private MonInstance GetStarterMon()
    {
        if (playerTeam == null || playerTeam.team == null || playerTeam.team.Length == 0)
            return null;

        return playerTeam.team[0];
    }

    private void ShowRandomUnusedHint()
    {
        if (remainingHints.Count == 0)
            BuildHintPool();

        if (remainingHints.Count == 0)
            return;

        int index = Random.Range(0, remainingHints.Count);
        HintData hint = remainingHints[index];
        remainingHints.RemoveAt(index);

        QueueHint(hint);
    }

    private void QueueHint(HintData hint)
    {
        if (!notificationsEnabled)
            return;

        if (hint == null || string.IsNullOrWhiteSpace(hint.message))
            return;

        if (IsInBattle())
            return;

        hintQueue.Enqueue(hint);

        if (queueRoutine == null)
            queueRoutine = StartCoroutine(ProcessHintQueue());
    }

    private IEnumerator ProcessHintQueue()
    {
        while (hintQueue.Count > 0 && notificationsEnabled)
        {
            if (IsInBattle())
            {
                hintQueue.Clear();
                break;
            }

            HintData hint = hintQueue.Dequeue();
            yield return ShowHintRoutine(hint);

            if (hintQueue.Count > 0)
                yield return new WaitForSecondsRealtime(timeBetweenQueuedHints);
        }

        queueRoutine = null;
    }

    private IEnumerator ShowHintRoutine(HintData hint)
    {

        if (root == null || hintText == null)
        {
            yield break;
        }

        hintText.text = hint.message;
        RefreshToggleText();

        root.SetActive(true);

        if (SoundManager.Instance != null && !string.IsNullOrWhiteSpace(hint.soundName))
            SoundManager.Instance.Play(hint.soundName, false);

        yield return new WaitForSecondsRealtime(hintDisplaySeconds);

        HideRoot();
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

        if (queueRoutine != null)
            StopCoroutine(queueRoutine);

        hintRoutine = null;
        queueRoutine = null;
    }

    private void RefreshToggleText()
    {
        if (toggleText == null)
            return;

        toggleText.text = notificationsEnabled
            ? "Press P to disable"
            : "Press P to enable";
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
                return new[] { MonType.Shadow, MonType.Water };

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

    private string ColorType(MonType type)
    {
        return $"<color={GetTypeHex(type)}>{type}</color>";
    }

    private string GetTypeHex(MonType type)
    {
        switch (type)
        {
            case MonType.Water:
                return "#46AAFF";

            case MonType.Fire:
                return "#FF5A3C";

            case MonType.Grass:
                return "#50D25A";

            case MonType.Light:
                return "#FFD750";

            case MonType.Shadow:
                return "#8C3CC8";

            case MonType.Earth:
                return "#AA783C";

            default:
                return "#FFFFFF";
        }
    }

    public void DisableCompanion()
    {
        notificationsEnabled = false;

        StopAllHintCoroutines();
        HideRoot();
        hintQueue.Clear();

        PlayerPrefs.SetInt(PrefKey, 0);
        PlayerPrefs.Save();
    }
}