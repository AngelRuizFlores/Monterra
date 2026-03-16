using System.Collections;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [Header("Battle UI")]
    [SerializeField] private GameObject battleCanvas;
    [SerializeField] private BattleUI battleUI;
    [SerializeField] private MovesUI movesUI;

    [Header("Player")]
    [SerializeField] private TouchingBehaviour playerTouching;
    [SerializeField] private PlayerMon playerMon;

    [Header("Health")]
    [SerializeField] private HealthBehaviour enemyHealth;
    [SerializeField] private HealthBehaviour playerHealth;

    [Header("Music")]
    [SerializeField] private MusicGame music;

    private WildMon currentWild;

    private enum BattleState { PlayerTurn, Busy }
    private BattleState state;

    private const float TurnDelay = 2f;

    void Awake()
    {
        battleCanvas?.SetActive(false);
        playerMon?.InitIfNeeded();
        state = BattleState.PlayerTurn;
    }

    public void StartBattle()
    {
        if (!CanStartBattle(out currentWild)) return;

        EnsureInstances(currentWild);
        ShowBattleUI(currentWild);

        enemyHealth?.Init(currentWild.instance);
        playerHealth?.Init(playerMon.instance);

        SetupMovesUI();

        Time.timeScale = 0f;
        state = BattleState.PlayerTurn;
    }

    public void EndBattle()
    {
        Time.timeScale = 1f;
        music?.StartWorldMusic();
        battleCanvas?.SetActive(false);

        if (playerTouching != null) playerTouching.lastWildMon = null;

        currentWild = null;
        state = BattleState.PlayerTurn;
    }

    public void UsePlayerMove(MoveData move)
    {
        if (state != BattleState.PlayerTurn) return;
        if (move == null || currentWild == null) return;

        StartCoroutine(BattleTurnCoroutine(move));
    }

    public void TryCapture()
    {
        if (currentWild == null || playerMon == null) return;

        var team = playerMon.GetComponent<PlayerTeam>();
        if (team == null) return;

        var newMon = MonLevelSystem.Clone(currentWild.instance);

        if (!team.TryAddToNextFreeSlot(newMon))
        {
            battleUI?.SetText("No tienes espacio en el equipo.");
            //Debug.Log($"UnlockedSlots={UnlockedSlots} | team0={(team[0]==null?"null":team[0].species?.monName)} | team1={(team[1]==null?"null":team[1].species?.monName)}");
            return;
        }

        battleUI?.SetText($"{newMon.species.monName} fue capturado!");
        DespawnCurrentWild();
        EndBattle();
    }

    private bool CanStartBattle(out WildMon wild)
    {
        wild = null;

        if (playerTouching == null || playerMon == null) return false;

        wild = playerTouching.lastWildMon;
        if (wild == null) return false;

        if (battleCanvas == null || battleUI == null) return false;
        if (enemyHealth == null || playerHealth == null) return false;

        return true;
    }

    private void EnsureInstances(WildMon wild)
    {
        if (wild.instance == null) wild.Init();
        playerMon.InitIfNeeded();
    }

    private void ShowBattleUI(WildMon wild)
    {
        battleCanvas.SetActive(true);

        battleUI.ShowWildMon(wild);
        battleUI.ShowPlayerMon(playerMon);
        battleUI.SetPlayerExp(playerMon.instance);
        battleUI.SetText($"¡Apareció {wild.instance.species.monName}!");
    }

    private void SetupMovesUI()
    {
        if (movesUI == null) return;

        movesUI.Setup(playerMon, this);
        movesUI.Refresh();
        movesUI.SetInteractable(true);
    }

    private IEnumerator BattleTurnCoroutine(MoveData playerMove)
    {
        state = BattleState.Busy;
        movesUI?.SetInteractable(false);

        bool playerFirst = MonLevelSystem.GetSpeed(playerMon.instance) >= MonLevelSystem.GetSpeed(currentWild.instance);

        if (playerFirst)
        {
            yield return PlayerAttack(playerMove);
            if (IsWildDead()) { yield return WinWildAndExit(); yield break; }

            yield return EnemyAttack();
            if (IsPlayerDead()) { yield return LoseAndExit(); yield break; }
        }
        else
        {
            yield return EnemyAttack();
            if (IsPlayerDead()) { yield return LoseAndExit(); yield break; }

            yield return PlayerAttack(playerMove);
            if (IsWildDead()) { yield return WinWildAndExit(); yield break; }
        }

        state = BattleState.PlayerTurn;
        battleUI?.SetText($"¿Qué hará {playerMon.instance.species.monName}?");
        movesUI?.SetInteractable(true);
    }

    private IEnumerator PlayerAttack(MoveData move)
    {
        var attacker = playerMon.instance;
        var defender = currentWild.instance;

        battleUI?.SetText($"{attacker.species.monName} usó {move.moveName}!");

        float mult = TypeChart.GetMultiplier(move.type, defender.species.type);

        int dmg = ComputeDamage(attacker, defender, move);
        enemyHealth?.Hurt(dmg);

        yield return new WaitForSecondsRealtime(1f);

        string effectText = TypeChart.GetEffectText(mult);
        if (!string.IsNullOrEmpty(effectText))
        {
            battleUI?.SetText(effectText);
            yield return new WaitForSecondsRealtime(1f);
        }
    }

    private IEnumerator EnemyAttack()
    {
        var attacker = currentWild.instance;
        var defender = playerMon.instance;

        MoveData enemyMove = GetRandomEnemyMove();
        string name = enemyMove != null ? enemyMove.moveName : "Punch";

        battleUI?.SetText($"{attacker.species.monName} usó {name}!");

        float mult = 1f;
        int dmg;

        if (enemyMove != null)
        {
            mult = TypeChart.GetMultiplier(enemyMove.type, defender.species.type);
            dmg = ComputeDamage(attacker, defender, enemyMove);
        }
        else
        {
            dmg = 5;
        }

        playerHealth?.Hurt(dmg);

        yield return new WaitForSecondsRealtime(1f);

        string effectText = TypeChart.GetEffectText(mult);
        if (!string.IsNullOrEmpty(effectText))
        {
            battleUI?.SetText(effectText);
            yield return new WaitForSecondsRealtime(1f);
        }
    }

    private static int ComputeDamage(MonInstance attacker, MonInstance defender, MoveData move)
    {
        int atk = MonLevelSystem.GetAttack(attacker);
        int def = MonLevelSystem.GetDefense(defender);

        int baseDmg = move.power + (atk / 4) - (def / 6);
        baseDmg = Mathf.Max(1, baseDmg);

        float mult = TypeChart.GetMultiplier(move.type, defender.species.type);

        int finalDmg = Mathf.RoundToInt(baseDmg * mult);
        return Mathf.Max(1, finalDmg);
    }

    private bool IsWildDead() => currentWild == null || currentWild.instance.currentHP <= 0;
    private bool IsPlayerDead() => playerMon == null || playerMon.instance.currentHP <= 0;

    private IEnumerator WinWildAndExit()
    {
        battleUI?.SetText($"¡{currentWild.instance.species.monName} se debilitó!");
        yield return new WaitForSecondsRealtime(1f);

        int phase = GetCurrentPhase();
        bool leveledUp = MonLevelSystem.AddExperience(playerMon.instance, MonLevelSystem.ExpSource.Wild, phase);

        battleUI?.ShowPlayerMon(playerMon);
        battleUI?.SetPlayerExp(playerMon.instance);

        if (leveledUp)
        {
            movesUI?.Refresh();
            battleUI?.SetText($"¡{playerMon.instance.species.monName} subió a Nv. {playerMon.instance.level}!");
            yield return new WaitForSecondsRealtime(1f);
        }

        DespawnCurrentWild();
        EndBattle();
    }

    private IEnumerator LoseAndExit()
    {
        battleUI?.SetText($"¡{playerMon.instance.species.monName} se debilitó!");
        yield return new WaitForSecondsRealtime(1f);
        EndBattle();
    }

    private MoveData GetRandomEnemyMove()
    {
        var moves = currentWild.instance.moves;
        if (moves == null || moves.Count == 0) return null;

        for (int i = 0; i < 10; i++)
        {
            var m = moves[Random.Range(0, moves.Count)];
            if (m != null) return m;
        }
        return null;
    }

    private void DespawnCurrentWild()
    {
        if (currentWild != null) currentWild.gameObject.SetActive(false);
    }
    private int GetCurrentPhase()
    {
        return 3;
    }
}