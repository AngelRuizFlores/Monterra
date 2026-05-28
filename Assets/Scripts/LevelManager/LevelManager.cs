using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public partial class LevelManager : MonoBehaviour
{
    [Header("Battle UI")]
    [SerializeField] private GameObject battleCanvas;
    [SerializeField] private BattleUI battleUI;
    [SerializeField] private MovesUI movesUI;
    [SerializeField] private BattleMonSwitchPopupUI switchPopupUI;

    [Header("Player")]
    [SerializeField] private TouchingBehaviour playerTouching;
    [SerializeField] private PlayerMon playerMon;
    [SerializeField] private TrainerBattleProgress trainerBattleProgress;

    [Header("Health")]
    [SerializeField] private HealthBehaviour enemyHealth;
    [SerializeField] private HealthBehaviour playerHealth;

    [Header("Music")]
    [SerializeField] private MusicGame music;

    [Header("Sound")]
    [SerializeField] private SoundManager soundManager;

    [Header("Events")]
    [SerializeField] private UnityEvent onPlayerPartyDefeated;
    [SerializeField] private UnityEvent onWin;

    [Header("Enemy AI")]
    [SerializeField] private EnemyDecisionMode enemyDecisionMode = EnemyDecisionMode.Classic;
    [SerializeField] private bool logEnemyDecisionContext = false;
    [SerializeField] private EnemyApiClient enemyApiClient;

    [Header("Enemy Bark API")]
    [SerializeField] private EnemyBarkApiClient enemyBarkApiClient;
    [SerializeField] private bool enableApiBarks = true;

    [Header("Battle Background")]
    [SerializeField] private BattleBackgroundSelector battleBackgroundSelector;

    [Header("Save System")]
    [SerializeField] private MonSpecies[] allSpecies;

    [Header("Capture VFX")]
    [SerializeField] private AttackVfxUIProjectile captureBallProjectilePrefab;
    [SerializeField] private Sprite captureBallThrowSprite;
    [SerializeField] private Sprite captureBallSuccessSprite;
    [SerializeField] private Sprite captureBallFailSprite;
    [SerializeField] private float captureBallResultSeconds = 0.75f;
    [SerializeField] private float captureSuccessHoldSeconds = 2f;

    private const float TurnDelay = 1f;
    private const float PlayerAttackStartDelay = 1.3f;
    private const string MonCatchSoundName = "MonCatch";
    private const string CatchAttemptSoundName = "CatchAttempt";
    private const string CatchFailSoundName = "CatchFail";

    private readonly List<MonInstance> currentTrainerRoster = new();
    private AttackVfxUIProjectile activeCaptureBallVfx;

    private WildMon currentWild;
    private TrainerBattleTrigger currentTrainer;
    private int currentTrainerEnemyIndex = -1;

    private Coroutine runningBattleRoutine;
    private bool battleEnding;
    private bool switchResolutionInProgress;
    private PostBattleAction pendingPostBattleAction = PostBattleAction.None;

    private BattleState state = BattleState.Inactive;
    private EncounterType encounterType = EncounterType.None;

    private enum BattleState
    {
        Inactive,
        PlayerTurn,
        Busy,
        WaitingForForcedSwitch
    }

    private enum EncounterType
    {
        None,
        Wild,
        Trainer
    }

    private enum PostBattleAction
    {
        None,
        GameOver,
        Victory
    }

    private enum EnemyTurnResolution
    {
        None,
        UsedMove,
        SwitchedMon
    }

    private void Awake()
    {
        if (battleCanvas != null)
        {
            battleCanvas.SetActive(false);
        }

        playerMon?.InitIfNeeded();

        if (battleUI != null)
        {
            battleUI.BindSwitchAction(TryOpenManualSwitchPopup);
        }

        if (switchPopupUI != null)
        {
            switchPopupUI.HideImmediate();
        }

        if (soundManager == null)
        {
            soundManager = SoundManager.Instance;
        }

        state = BattleState.Inactive;
        encounterType = EncounterType.None;
    }

    private void Start()
    {
        if (GameStartMode.LoadGame)
        {
            LoadGameIfExists();
        }
    }
}