using System;
using System.Collections.Generic;

namespace DungeonFit.Core.Models;

public sealed class RoomRun
{
	private readonly List<CompletionResult> _setResults = new();
	private readonly List<CombatSetResult> _combatResults = new();
	private readonly PlayerCombatStats _playerStats;
	private readonly EnemyDefinition _enemy;
	private readonly int _enemyLevel;

	private int _activeSetNumber;
	private bool _activeSetIsBoss;
	private int _activeEnemyMaxHp;
	private int _activeEnemyHp;
	private int _activeRepsResolved;
	private int _activeDamageDealt;
	private int _activeDamageTaken;
	private bool _activeEnemyDefeated;

	public RoomRun(TaskTemplate task, PlayerCombatStats playerStats, EnemyDefinition enemy, int initialPlayerHp)
	{
		Task = task;
		_playerStats = playerStats;
		_enemy = enemy;
		_enemyLevel = Math.Max(1, task.DungeonLevel);
		CurrentPlayerHp = Math.Clamp(initialPlayerHp, -playerStats.MaxHp, playerStats.MaxHp);
	}

	public TaskTemplate Task { get; }

	public int CurrentPlayerHp { get; private set; }

	public bool IsComplete => _setResults.Count >= Task.TotalSets;

	public bool IsSkipped { get; private set; }

	public IReadOnlyList<CompletionResult> SetResults => _setResults;

	public IReadOnlyList<CombatSetResult> CombatResults => _combatResults;

	public ActiveSetCombatState ActiveCombatState => _activeSetNumber == Progress.CurrentSet
		? BuildActiveState()
		: ActiveSetCombatState.Empty(CurrentPlayerHp, _playerStats.MaxHp);

	public RoomProgress Progress => new(
		IsComplete || IsSkipped ? _setResults.Count : _setResults.Count + 1,
		Task.TotalSets,
		!IsComplete && !IsSkipped && _setResults.Count + 1 == Task.TotalSets,
		IsComplete,
		IsSkipped);

	public ActiveSetCombatState BeginActiveSet()
	{
		if (IsComplete || IsSkipped)
		{
			return ActiveCombatState;
		}

		var progress = Progress;
		if (_activeSetNumber == progress.CurrentSet)
		{
			return BuildActiveState();
		}

		_activeSetNumber = progress.CurrentSet;
		_activeSetIsBoss = progress.IsBossWave;
		_activeEnemyMaxHp = _activeSetIsBoss ? _enemy.GetBossMaxHp(_enemyLevel) : _enemy.GetNormalMaxHp(_enemyLevel);
		_activeEnemyHp = _activeEnemyMaxHp;
		_activeRepsResolved = 0;
		_activeDamageDealt = 0;
		_activeDamageTaken = 0;
		_activeEnemyDefeated = false;
		return BuildActiveState();
	}

	public CombatRepResult? ResolveRepHit()
	{
		if (IsComplete || IsSkipped)
		{
			return null;
		}

		var state = BeginActiveSet();
		if (state.SetNumber == 0)
		{
			return null;
		}

		_activeRepsResolved++;
		var hpBefore = CurrentPlayerHp;
		var enemyHpBefore = _activeEnemyHp;
		var wasEvading = hpBefore <= 0;
		var movingAfterKill = _activeEnemyDefeated;
		var damageDealt = 0;
		var damageTaken = 0;
		var enemyAttacked = false;

		if (!_activeEnemyDefeated)
		{
			damageDealt = wasEvading ? 0 : Math.Max(1, _playerStats.Attack + (_playerStats.EquipmentScore / 6));
			_activeEnemyHp = Math.Max(0, _activeEnemyHp - damageDealt);
			_activeDamageDealt += damageDealt;
			_activeEnemyDefeated = _activeEnemyHp <= 0;

			if (!_activeEnemyDefeated && ShouldEnemyAttack(_activeSetIsBoss, _activeSetNumber, _activeRepsResolved))
			{
				enemyAttacked = true;
				damageTaken = wasEvading ? 1 : (_activeSetIsBoss ? _enemy.GetBossAttack(_enemyLevel) : _enemy.GetNormalAttack(_enemyLevel));
				CurrentPlayerHp = Math.Max(-_playerStats.MaxHp, CurrentPlayerHp - damageTaken);
				_activeDamageTaken += damageTaken;
			}
		}

		return new CombatRepResult(
			_activeSetNumber,
			_activeRepsResolved,
			_activeSetIsBoss,
			hpBefore,
			CurrentPlayerHp,
			_playerStats.MaxHp,
			enemyHpBefore,
			_activeEnemyHp,
			_activeEnemyMaxHp,
			damageDealt,
			damageTaken,
			enemyAttacked,
			_activeEnemyDefeated,
			wasEvading,
			movingAfterKill);
	}

	public CombatSetResult? ReportSet()
	{
		if (IsComplete || IsSkipped)
		{
			return null;
		}

		var state = BeginActiveSet();
		if (state.SetNumber == 0)
		{
			return null;
		}

		var result = _activeEnemyDefeated ? CompletionResult.Completed : CompletionResult.Partial;
		var rewardKind = _activeEnemyDefeated ? BankedRewardKind.Chest : BankedRewardKind.GoldOnly;
		var gold = _activeEnemyDefeated
			? _activeSetIsBoss ? 20 : 10
			: _activeSetIsBoss ? 8 : 3;
		gold = ApplyGoldBonus(gold);
		var chestTier = _activeSetIsBoss ? "Boss" : "Normal";
		var combatResult = new CombatSetResult(
			_activeSetNumber,
			_activeSetIsBoss,
			result,
			rewardKind,
			chestTier,
			gold,
			state.PlayerHp,
			CurrentPlayerHp,
			_activeEnemyMaxHp,
			_activeEnemyHp,
			_activeSetIsBoss ? _enemy.GetBossAttack(_enemyLevel) : _enemy.GetNormalAttack(_enemyLevel),
			_activeDamageDealt,
			_activeDamageTaken,
			_activeEnemyDefeated,
			state.PlayerHp <= 0);

		_setResults.Add(result);
		_combatResults.Add(combatResult);
		ClearActiveSet();
		return combatResult;
	}

	public void Skip()
	{
		if (IsComplete)
		{
			return;
		}

		IsSkipped = true;
	}

	public int HealPlayer(int amount)
	{
		if (amount <= 0)
		{
			return 0;
		}

		var before = CurrentPlayerHp;
		CurrentPlayerHp = Math.Min(_playerStats.MaxHp, CurrentPlayerHp + amount);
		return CurrentPlayerHp - before;
	}

	private ActiveSetCombatState BuildActiveState()
	{
		return new ActiveSetCombatState(
			_activeSetNumber,
			_activeSetIsBoss,
			CurrentPlayerHp,
			_playerStats.MaxHp,
			_activeEnemyHp,
			_activeEnemyMaxHp,
			_activeRepsResolved,
			_activeDamageDealt,
			_activeDamageTaken,
			_activeEnemyDefeated,
			CurrentPlayerHp <= 0);
	}

	private void ClearActiveSet()
	{
		_activeSetNumber = 0;
		_activeSetIsBoss = false;
		_activeEnemyMaxHp = 0;
		_activeEnemyHp = 0;
		_activeRepsResolved = 0;
		_activeDamageDealt = 0;
		_activeDamageTaken = 0;
		_activeEnemyDefeated = false;
	}

	private string AttackSeed(int setNumber, int repNumber)
	{
		return $"{Task.Id}:{Task.DungeonTypeId}:set:{setNumber}:rep:{repNumber}";
	}

	private bool ShouldEnemyAttack(bool isBoss, int setNumber, int repNumber)
	{
		var threshold = isBoss ? 45 : 25;
		return StableRoll(AttackSeed(setNumber, repNumber), 100) < threshold;
	}

	private int ApplyGoldBonus(int gold)
	{
		if (_playerStats.DungeonGoldBonusPercent <= 0)
		{
			return gold;
		}

		return (int)Math.Ceiling(gold * (1 + (_playerStats.DungeonGoldBonusPercent / 100.0)));
	}

	private static int StableRoll(string seed, int maxExclusive)
	{
		var hash = 17;
		foreach (var character in seed)
		{
			hash = (hash * 31) + character;
		}

		return Math.Abs(hash) % maxExclusive;
	}
}
