using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AutobattlerSample.Data;
using UnityEngine;

namespace AutobattlerSample.Battle
{
    public class BattleCombatManager : MonoBehaviour
    {
        private List<BattleUnit> _allies;
        private List<BattleUnit> _enemies;
        private Action<TurnAction> _onTurnAction;
        private Action<BattleResult> _onBattleEnd;
        private Coroutine _battleCoroutine;

        public void StartBattle(List<BattleUnit> allies, List<BattleUnit> enemies,
            Action<TurnAction> onTurnAction, Action<BattleResult> onBattleEnd)
        {
            _allies = allies;
            _enemies = enemies;
            _onTurnAction = onTurnAction;
            _onBattleEnd = onBattleEnd;

            // Reset all cooldowns at the start of each battle
            foreach (var u in _allies) u.CurrentCooldown = 0;
            foreach (var u in _enemies) u.CurrentCooldown = 0;

            if (_battleCoroutine != null)
                StopCoroutine(_battleCoroutine);
            _battleCoroutine = StartCoroutine(RunBattle());
        }

        private IEnumerator RunBattle()
        {
            yield return new WaitForSeconds(0.5f);

            var result = new BattleResult();
            int maxRounds = 100;
            int round = 0;

            while (round < maxRounds && _allies.Any(u => u.IsAlive) && _enemies.Any(u => u.IsAlive))
            {
                round++;

                // Start of turn: tick all cooldowns down by 1
                foreach (var u in _allies.Where(u => u.IsAlive)) u.TickCooldown();
                foreach (var u in _enemies.Where(u => u.IsAlive)) u.TickCooldown();

                // Build turn order: allies in team order, then enemies in list order
                var turnOrder = new List<BattleUnit>();
                turnOrder.AddRange(_allies.Where(u => u.IsAlive));
                turnOrder.AddRange(_enemies.Where(u => u.IsAlive));

                foreach (var unit in turnOrder)
                {
                    if (!unit.IsAlive) continue;
                    if (!_allies.Any(u => u.IsAlive) || !_enemies.Any(u => u.IsAlive)) break;

                    if (!unit.IsReady)
                    {
                        // Unit is on cooldown — skip turn
                        var skipAction = new TurnAction
                        {
                            Attacker = unit,
                            WasOnCooldown = true,
                            AttackerCooldownAfter = unit.CurrentCooldown,
                            AttackerRank = unit.Rank
                        };
                        result.TurnLog.Add(skipAction);
                        _onTurnAction?.Invoke(skipAction);
                        yield return new WaitForSeconds(0.25f);
                        continue;
                    }

                    // Pick a random living target from the opposing team
                    var targets = unit.IsAlly
                        ? _enemies.Where(u => u.IsAlive).ToList()
                        : _allies.Where(u => u.IsAlive).ToList();

                    if (targets.Count == 0) break;

                    var target = targets[UnityEngine.Random.Range(0, targets.Count)];
                    int hpBefore = target.CurrentHP;
                    int shieldBefore = target.Shield;
                    int rawDamage = unit.AttackDamage;
                    var (_, shieldAbsorbed) = target.TakeDamage(rawDamage);

                    // Start cooldown after attacking
                    unit.StartCooldown();

                    // Handle Lifesteal passive
                    int lifestealHealed = 0;
                    if (unit.Passive == PassiveType.Lifesteal)
                    {
                        int hpDamage = rawDamage - shieldAbsorbed;
                        if (hpDamage > 0)
                            lifestealHealed = unit.Heal(hpDamage);
                    }

                    var action = new TurnAction
                    {
                        Attacker = unit,
                        Target = target,
                        RawDamage = rawDamage,
                        DamageDealt = rawDamage,
                        ShieldAbsorbed = shieldAbsorbed,
                        TargetHPBefore = hpBefore,
                        TargetHPAfter = target.CurrentHP,
                        TargetShieldBefore = shieldBefore,
                        TargetShieldAfter = target.Shield,
                        KilledTarget = !target.IsAlive,
                        WasOnCooldown = false,
                        AttackerCooldownAfter = unit.CurrentCooldown,
                        AttackerRank = unit.Rank,
                        LifestealHealed = lifestealHealed
                    };

                    result.TurnLog.Add(action);
                    _onTurnAction?.Invoke(action);

                    yield return new WaitForSeconds(0.7f);
                }
            }

            // Write back HP/Shield to instances and full-heal surviving allies
            foreach (var ally in _allies)
            {
                ally.WriteBackHP();
                if (ally.IsAlive)
                    ally.Instance.FullHeal();
            }
            foreach (var enemy in _enemies)
                enemy.WriteBackHP();

            result.PlayerWon = _allies.Any(u => u.IsAlive);

            if (!result.PlayerWon)
            {
                foreach (var enemy in _enemies.Where(u => u.IsAlive))
                    result.SurvivingEnemies.Add(enemy.Instance);
            }

            yield return new WaitForSeconds(0.3f);
            _onBattleEnd?.Invoke(result);
        }
    }
}
