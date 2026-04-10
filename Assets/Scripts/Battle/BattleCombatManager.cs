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
        private Action<int> _onNewRound;
        private Action<IReadOnlyList<BattleUnit>> _onTurnOrderReady;
        private Coroutine _battleCoroutine;
        private bool _autoAdvance;
        private bool _waitingForNextRound;

        public bool AutoAdvance => _autoAdvance;
        public bool WaitingForNextRound => _waitingForNextRound;

        public void SetAutoAdvance(bool enabled)
        {
            _autoAdvance = enabled;
        }

        public void AdvanceToNextRound()
        {
            _waitingForNextRound = false;
        }

        public void StartBattle(List<BattleUnit> allies, List<BattleUnit> enemies,
            Action<TurnAction> onTurnAction, Action<BattleResult> onBattleEnd,
            Action<int> onNewRound = null, Action<IReadOnlyList<BattleUnit>> onTurnOrderReady = null)
        {
            _allies = allies;
            _enemies = enemies;
            _onTurnAction = onTurnAction;
            _onBattleEnd = onBattleEnd;
            _onNewRound = onNewRound;
            _onTurnOrderReady = onTurnOrderReady;

            // Reset all cooldowns
            foreach (var u in _allies)
                foreach (var a in u.Actions) a.CurrentCooldown = 0;
            foreach (var u in _enemies)
                foreach (var a in u.Actions) a.CurrentCooldown = 0;

            if (_battleCoroutine != null)
                StopCoroutine(_battleCoroutine);
            _battleCoroutine = StartCoroutine(RunBattle());
        }

        private IEnumerator RunBattle()
        {
            _autoAdvance = false;
            _waitingForNextRound = false;
            yield return new WaitForSeconds(0.5f);

            var result = new BattleResult();
            int maxRounds = 100;
            int round = 0;

            while (round < maxRounds && _allies.Any(u => u.IsAlive) && _enemies.Any(u => u.IsAlive))
            {
                round++;
                result.TotalTurns = round;
                _onNewRound?.Invoke(round);

                // Tick all cooldowns
                foreach (var u in _allies.Where(u => u.IsAlive)) u.TickAllCooldowns();
                foreach (var u in _enemies.Where(u => u.IsAlive)) u.TickAllCooldowns();

                // Build turn order: shuffle all living units randomly
                var turnOrder = new List<BattleUnit>();
                turnOrder.AddRange(_allies.Where(u => u.IsAlive));
                turnOrder.AddRange(_enemies.Where(u => u.IsAlive));
                ShuffleList(turnOrder);
                _onTurnOrderReady?.Invoke(turnOrder);

                _waitingForNextRound = !_autoAdvance;
                yield return new WaitUntil(() => _autoAdvance || !_waitingForNextRound);

                foreach (var unit in turnOrder)
                {
                    if (!unit.IsAlive) continue;
                    if (!_allies.Any(u => u.IsAlive) || !_enemies.Any(u => u.IsAlive)) break;

                    var readyAction = unit.GetNextReadyAction();

                    if (readyAction == null)
                    {
                        // All actions on cooldown
                        int minCd = unit.Actions.Count > 0 ? unit.Actions.Min(a => a.CurrentCooldown) : 0;
                        var skipAction = new TurnAction
                        {
                            Attacker = unit,
                            WasOnCooldown = true,
                            AttackerCooldownAfter = minCd,
                            AttackerRank = unit.Rank,
                            TurnNumber = round
                        };
                        result.TurnLog.Add(skipAction);
                        _onTurnAction?.Invoke(skipAction);
                        yield return new WaitForSeconds(0.25f);
                        continue;
                    }

                    TurnAction action = null;

                    switch (readyAction.Type)
                    {
                        case ActionType.Attack:
                            action = ExecuteAttack(unit, readyAction, round);
                            break;
                        case ActionType.ShieldSelf:
                            action = ExecuteShieldSelf(unit, readyAction, round);
                            break;
                        case ActionType.HealSelf:
                            action = ExecuteHealSelf(unit, readyAction, round);
                            break;
                        case ActionType.HealFront:
                            action = ExecuteHealFront(unit, readyAction, round);
                            break;
                        case ActionType.HealAll:
                            action = ExecuteHealAll(unit, readyAction, round);
                            break;
                    }

                    if (action != null)
                    {
                        result.TurnLog.Add(action);
                        _onTurnAction?.Invoke(action);
                        yield return new WaitForSeconds(0.7f);
                    }
                }
            }

            // Write back HP/Shield and revive dead allies at full HP
            foreach (var ally in _allies)
            {
                ally.WriteBackHP();
                ally.Instance.FullHeal(); // Revive dead + full heal living
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

        private TurnAction ExecuteAttack(BattleUnit unit, ActionInstance readyAction, int round)
        {
            // Target closest enemy (lowest Position)
            var targets = unit.IsAlly
                ? _enemies.Where(u => u.IsAlive).OrderBy(u => u.Position).ToList()
                : _allies.Where(u => u.IsAlive).OrderBy(u => u.Position).ToList();

            if (targets.Count == 0) return null;

            // Pick the closest (lowest position), break ties randomly
            int minPos = targets[0].Position;
            var closest = targets.Where(u => u.Position == minPos).ToList();
            var target = closest[UnityEngine.Random.Range(0, closest.Count)];

            int hpBefore = target.CurrentHP;
            int shieldBefore = target.Shield;
            int rawDamage = readyAction.Amount;
            var (_, shieldAbsorbed) = target.TakeDamage(rawDamage);

            readyAction.StartCooldown();

            // Lifesteal
            int lifestealHealed = 0;
            bool lifestealTriggered = false;
            bool hasteTriggered = false;
            string hasteUnitName = null;
            string hasteActionName = null;
            int hasteCdBefore = 0, hasteCdAfter = 0;

            if (unit.Passive == PassiveType.Lifesteal)
            {
                int hpDamage = rawDamage - shieldAbsorbed;
                if (hpDamage > 0)
                {
                    lifestealHealed = unit.Heal(hpDamage);
                    lifestealTriggered = lifestealHealed > 0;
                    if (lifestealHealed > 0)
                    {
                        var haste = unit.TriggerHasteOnHeal(1);
                        if (haste.triggered)
                        {
                            hasteTriggered = true;
                            hasteUnitName = unit.DisplayName;
                            hasteActionName = haste.actionName;
                            hasteCdBefore = haste.cdBefore;
                            hasteCdAfter = haste.cdAfter;
                        }
                    }
                }
            }

            return new TurnAction
            {
                Attacker = unit,
                Target = target,
                UsedActionType = ActionType.Attack,
                ActionName = readyAction.DisplayName,
                RawDamage = rawDamage,
                DamageDealt = rawDamage,
                ShieldAbsorbed = shieldAbsorbed,
                TargetHPBefore = hpBefore,
                TargetHPAfter = target.CurrentHP,
                TargetShieldBefore = shieldBefore,
                TargetShieldAfter = target.Shield,
                KilledTarget = !target.IsAlive,
                WasOnCooldown = false,
                AttackerCooldownAfter = readyAction.CurrentCooldown,
                AttackerRank = unit.Rank,
                LifestealHealed = lifestealHealed,
                LifestealTriggered = lifestealTriggered,
                HasteTriggered = hasteTriggered,
                HasteUnitName = hasteUnitName,
                HasteActionName = hasteActionName,
                HasteCooldownBefore = hasteCdBefore,
                HasteCooldownAfter = hasteCdAfter,
                TurnNumber = round
            };
        }

        private TurnAction ExecuteShieldSelf(BattleUnit unit, ActionInstance readyAction, int round)
        {
            int amount = readyAction.Amount;
            unit.AddShield(amount);
            readyAction.StartCooldown();

            return new TurnAction
            {
                Attacker = unit,
                Target = unit,
                UsedActionType = ActionType.ShieldSelf,
                ActionName = readyAction.DisplayName,
                ShieldGained = amount,
                WasOnCooldown = false,
                AttackerCooldownAfter = readyAction.CurrentCooldown,
                AttackerRank = unit.Rank,
                TargetShieldBefore = unit.Shield - amount,
                TargetShieldAfter = unit.Shield,
                TurnNumber = round
            };
        }

        private TurnAction ExecuteHealSelf(BattleUnit unit, ActionInstance readyAction, int round)
        {
            int hpBefore = unit.CurrentHP;
            int healed = unit.Heal(readyAction.Amount);
            readyAction.StartCooldown();

            bool hasteTriggered = false;
            string hasteUnitName = null, hasteActionName = null;
            int hasteCdBefore = 0, hasteCdAfter = 0;

            if (healed > 0)
            {
                var haste = unit.TriggerHasteOnHeal(1);
                if (haste.triggered)
                {
                    hasteTriggered = true;
                    hasteUnitName = unit.DisplayName;
                    hasteActionName = haste.actionName;
                    hasteCdBefore = haste.cdBefore;
                    hasteCdAfter = haste.cdAfter;
                }
            }

            return new TurnAction
            {
                Attacker = unit,
                Target = unit,
                HealTarget = unit,
                UsedActionType = ActionType.HealSelf,
                ActionName = readyAction.DisplayName,
                HealAmount = healed,
                WasOnCooldown = false,
                AttackerCooldownAfter = readyAction.CurrentCooldown,
                AttackerRank = unit.Rank,
                TargetHPBefore = hpBefore,
                TargetHPAfter = unit.CurrentHP,
                HasteTriggered = hasteTriggered,
                HasteUnitName = hasteUnitName,
                HasteActionName = hasteActionName,
                HasteCooldownBefore = hasteCdBefore,
                HasteCooldownAfter = hasteCdAfter,
                TurnNumber = round
            };
        }

        private TurnAction ExecuteHealFront(BattleUnit unit, ActionInstance readyAction, int round)
        {
            // Find ally with lowest position that is alive
            var allies = unit.IsAlly ? _allies : _enemies;
            BattleUnit target = null;
            foreach (var a in allies.Where(u => u.IsAlive).OrderBy(u => u.Position))
            {
                target = a;
                break;
            }
            if (target == null) target = unit; // fallback to self

            int hpBefore = target.CurrentHP;
            int healed = target.Heal(readyAction.Amount);
            readyAction.StartCooldown();

            bool hasteTriggered = false;
            string hasteUnitName = null, hasteActionName = null;
            int hasteCdBefore = 0, hasteCdAfter = 0;

            if (healed > 0)
            {
                var haste = target.TriggerHasteOnHeal(1);
                if (haste.triggered)
                {
                    hasteTriggered = true;
                    hasteUnitName = target.DisplayName;
                    hasteActionName = haste.actionName;
                    hasteCdBefore = haste.cdBefore;
                    hasteCdAfter = haste.cdAfter;
                }
            }

            return new TurnAction
            {
                Attacker = unit,
                Target = target,
                HealTarget = target,
                UsedActionType = ActionType.HealFront,
                ActionName = readyAction.DisplayName,
                HealAmount = healed,
                WasOnCooldown = false,
                AttackerCooldownAfter = readyAction.CurrentCooldown,
                AttackerRank = unit.Rank,
                TargetHPBefore = hpBefore,
                TargetHPAfter = target.CurrentHP,
                HasteTriggered = hasteTriggered,
                HasteUnitName = hasteUnitName,
                HasteActionName = hasteActionName,
                HasteCooldownBefore = hasteCdBefore,
                HasteCooldownAfter = hasteCdAfter,
                TurnNumber = round
            };
        }

        private TurnAction ExecuteHealAll(BattleUnit unit, ActionInstance readyAction, int round)
        {
            var allies = unit.IsAlly ? _allies : _enemies;
            int totalHealed = 0;
            var results = new List<(BattleUnit unit, int healed)>();

            bool hasteTriggered = false;
            string hasteUnitName = null, hasteActionName = null;
            int hasteCdBefore = 0, hasteCdAfter = 0;

            foreach (var ally in allies)
            {
                if (!ally.IsAlive) continue;
                int healed = ally.Heal(readyAction.Amount);
                totalHealed += healed;
                results.Add((ally, healed));
                if (healed > 0)
                {
                    var haste = ally.TriggerHasteOnHeal(1);
                    if (haste.triggered && !hasteTriggered)
                    {
                        hasteTriggered = true;
                        hasteUnitName = ally.DisplayName;
                        hasteActionName = haste.actionName;
                        hasteCdBefore = haste.cdBefore;
                        hasteCdAfter = haste.cdAfter;
                    }
                }
            }

            readyAction.StartCooldown();

            return new TurnAction
            {
                Attacker = unit,
                Target = unit,
                HealTarget = unit,
                UsedActionType = ActionType.HealAll,
                ActionName = readyAction.DisplayName,
                HealAmount = totalHealed,
                HealAllResults = results,
                WasOnCooldown = false,
                AttackerCooldownAfter = readyAction.CurrentCooldown,
                AttackerRank = unit.Rank,
                TargetHPBefore = 0,
                TargetHPAfter = 0,
                HasteTriggered = hasteTriggered,
                HasteUnitName = hasteUnitName,
                HasteActionName = hasteActionName,
                HasteCooldownBefore = hasteCdBefore,
                HasteCooldownAfter = hasteCdAfter,
                TurnNumber = round
            };
        }

        private static void ShuffleList<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
