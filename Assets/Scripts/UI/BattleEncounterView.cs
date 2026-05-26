using DungeonFit.Core.Models;
using Godot;
using System;

namespace DungeonFit.UI;

public sealed class BattleEncounterView
{
    private readonly BattleActorView _player;
    private readonly BattleActorView _enemy;
    private readonly Control _stage;
    private readonly Label _enemyName;
    private readonly ProgressBar _bossHealth;
    private Tween? _attackTween;
    private EnemyDefinition _enemyDefinition = new("training_dummy", "\u8a13\u7df4\u5047\u4eba", "\u8a13\u7df4\u6559\u5b98", 24, 2, 54, 4);
    private int _enemyLevel = 1;

    public BattleEncounterView(
        PanelContainer playerToken,
        Label playerLabel,
        PanelContainer enemyToken,
        Label enemyLabel,
        Label enemyName,
        ProgressBar bossHealth)
    {
        _stage = playerToken.GetParent<Control>();
        _player = new BattleActorView(
            playerToken,
            playerLabel,
            "\u5192\u96aa\u8005",
            BattleActorAnimationSet.PlayerKnight,
            flipHorizontal: false);
        _enemy = new BattleActorView(
            enemyToken,
            enemyLabel,
            _enemyDefinition.DisplayName,
            BattleActorAnimationSet.EnemySkeleton,
            flipHorizontal: true);
        _enemyName = enemyName;
        _bossHealth = bossHealth;
        _stage.Resized += PositionActors;
        PositionActors();
    }

    public void SetEnemy(EnemyDefinition enemyDefinition, int enemyLevel)
    {
        _enemyDefinition = enemyDefinition;
        _enemyLevel = Math.Max(1, enemyLevel);
        _enemy.SetDisplayName(enemyDefinition.DisplayName);
    }

    public void ShowActiveWave(RoomProgress progress, ActiveSetCombatState state)
    {
        PositionActors();
        var enemyLabel = progress.IsBossWave
            ? $"{_enemyDefinition.BossName} Lv.{_enemyLevel}"
            : $"{_enemyDefinition.DisplayName} Lv.{_enemyLevel}";
        _enemy.SetDisplayName(enemyLabel);
        _enemyName.Text = progress.IsBossWave
            ? $"\u6700\u7d42 Wave  {enemyLabel}"
            : $"Wave {progress.CurrentSet}  {enemyLabel}";
        _player.SetState(BattleActorState.Idle);
        _enemy.SetState(progress.IsBossWave ? BattleActorState.Active : BattleActorState.Idle);
        RefreshHealth(progress, state);
    }

    public void ShowWaveAttackWindup(RoomProgress progress, ActiveSetCombatState state)
    {
        PositionActors();
        _attackTween?.Kill();
        if (state.EnemyDefeated)
        {
            _player.SetState(BattleActorState.Moving);
            _enemy.SetState(BattleActorState.Defeated);
            return;
        }

        _player.SetState(state.IsEvading ? BattleActorState.Evading : BattleActorState.Active);
        _enemy.SetState(BattleActorState.Idle);
    }

    public void ShowWavePeakHit(RoomProgress progress, CombatRepResult? result, ActiveSetCombatState state)
    {
        PositionActors();
        _attackTween?.Kill();
        if (result is null)
        {
            RefreshHealth(progress, state);
            return;
        }

        _player.SetState(GetPlayerPeakState(result));
        _enemy.SetState(GetEnemyPeakState(result));
        RefreshHealth(progress, state);
        _enemyName.Text = result.IsMovingAfterKill
            ? "\u6575\u4eba\u5df2\u5012\u4e0b\uff0c\u524d\u9032\u4e2d"
            : result.EnemyAttacked
                ? string.Format("\u6575\u4eba\u53cd\u64ca -{0}", result.DamageTaken)
                : result.DamageDealt > 0
                    ? string.Format("\u547d\u4e2d -{0}", result.DamageDealt)
                    : "\u8eb2\u907f\u4e2d";

        _attackTween = _stage.CreateTween();
        _attackTween.TweenInterval(0.3);
        _attackTween.TweenCallback(Callable.From(() =>
        {
            if (progress.IsComplete || progress.IsSkipped)
            {
                return;
            }

            _player.SetState(result.EnemyDefeated
                ? BattleActorState.Moving
                : result.PlayerHpAfter <= 0
                    ? BattleActorState.Evading
                    : BattleActorState.Idle);
            _enemy.SetState(result.EnemyDefeated ? BattleActorState.Defeated : BattleActorState.Idle);
        }));
    }

    public void ShowRest(RoomProgress progress, ActiveSetCombatState state)
    {
        PositionActors();
        _attackTween?.Kill();
        _enemyName.Text = progress.IsBossWave
            ? $"\u4f11\u606f\u4e2d  {_enemyDefinition.BossName} Lv.{_enemyLevel}"
            : $"\u4f11\u606f\u4e2d  Wave {progress.CurrentSet}";
        _player.SetState(state.IsEvading ? BattleActorState.Evading : BattleActorState.Rest);
        _enemy.SetState(state.EnemyDefeated ? BattleActorState.Defeated : BattleActorState.Rest);
        RefreshHealth(progress, state);
    }

    public void ShowSetReported(RoomProgress progress, CombatSetResult result)
    {
        PositionActors();
        _attackTween?.Kill();
        _player.SetState(result.WasEvading
            ? BattleActorState.Evading
            : result.EnemyDefeated
                ? BattleActorState.Active
                : BattleActorState.Rest);
        _enemy.SetState(result.EnemyDefeated ? BattleActorState.Hit : BattleActorState.Idle);
        _enemyName.Text = result.EnemyDefeated
            ? "\u6575\u4eba\u64ca\u7834\uff0c\u5bf6\u7bb1\u5df2\u5c01\u5b58"
            : "\u6575\u4eba\u672a\u5012\u4e0b\uff0c\u50c5\u5b58\u5165\u91d1\u5e63";
        _bossHealth.Visible = progress.IsBossWave;
        _bossHealth.Value = result.EnemyDefeated
            ? 0
            : (result.EnemyHpAfter / (float)result.EnemyMaxHp) * 100f;
    }

    public void RefreshActiveHealth(RoomProgress progress, ActiveSetCombatState state)
    {
        RefreshHealth(progress, state);
    }

    public void ShowResult(RoomProgress progress, CombatSetResult? finalResult)
    {
        PositionActors();
        _attackTween?.Kill();
        if (progress.IsSkipped)
        {
            _enemyName.Text = "\u623f\u9593\u64a4\u9000";
            _player.SetState(BattleActorState.Rest);
            _enemy.SetState(BattleActorState.Idle);
            _bossHealth.Visible = false;
            return;
        }

        var bossDefeated = finalResult?.EnemyDefeated == true;
        _enemyName.Text = bossDefeated ? "Boss \u5df2\u64ca\u7834" : "\u623f\u9593\u5b8c\u6210\uff0cBoss \u672a\u64ca\u7834";
        _player.SetState(bossDefeated ? BattleActorState.Victory : BattleActorState.Rest);
        _enemy.SetDisplayName($"{_enemyDefinition.BossName} Lv.{_enemyLevel}");
        _enemy.SetState(bossDefeated ? BattleActorState.Defeated : BattleActorState.Idle);
        _bossHealth.Visible = true;
        _bossHealth.Value = bossDefeated || finalResult is null
            ? 0
            : (finalResult.EnemyHpAfter / (float)finalResult.EnemyMaxHp) * 100f;
    }

    private int _playerHpSnapshot = 1;
    private int _playerMaxHpSnapshot = 1;

    private static BattleActorState GetPlayerPeakState(CombatRepResult result)
    {
        if (result.IsMovingAfterKill)
        {
            return BattleActorState.Moving;
        }

        if (result.EnemyAttacked && result.DamageTaken > 0)
        {
            return BattleActorState.Hit;
        }

        if (result.WasEvading)
        {
            return BattleActorState.Evading;
        }

        return result.DamageDealt > 0 ? BattleActorState.Active : BattleActorState.Idle;
    }

    private static BattleActorState GetEnemyPeakState(CombatRepResult result)
    {
        if (result.EnemyDefeated)
        {
            return BattleActorState.Hit;
        }

        if (result.IsMovingAfterKill)
        {
            return BattleActorState.Defeated;
        }

        if (result.EnemyAttacked)
        {
            return BattleActorState.Active;
        }

        return result.DamageDealt > 0 ? BattleActorState.Hit : BattleActorState.Idle;
    }

    private void RefreshHealth(RoomProgress progress, ActiveSetCombatState state)
    {
        _playerHpSnapshot = state.PlayerHp;
        _playerMaxHpSnapshot = state.PlayerMaxHp;
        _player.ShowHp(state.PlayerHp, state.PlayerMaxHp, isPlayer: true, isEvading: state.IsEvading);

        if (progress.IsBossWave)
        {
            _enemy.HideHp();
            _bossHealth.Visible = true;
            _bossHealth.MaxValue = Mathf.Max(1, state.EnemyMaxHp);
            _bossHealth.Value = Mathf.Clamp(state.EnemyHp, 0, Mathf.Max(1, state.EnemyMaxHp));
            return;
        }

        _bossHealth.Visible = false;
        _enemy.ShowHp(state.EnemyHp, state.EnemyMaxHp, isPlayer: false, isEvading: false);
    }

    private void PositionActors()
    {
        if (_stage.Size == Vector2.Zero)
        {
            return;
        }

        var playerPosition = new Vector2(
            _stage.Size.X * -0.03f,
            _stage.Size.Y * 0.36f);
        var enemyPosition = new Vector2(
            _stage.Size.X - _enemy.TokenSize.X + (_stage.Size.X * 0.04f),
            _stage.Size.Y * 0.31f);

        _player.SetTokenPosition(playerPosition);
        _enemy.SetTokenPosition(enemyPosition);
    }
}
