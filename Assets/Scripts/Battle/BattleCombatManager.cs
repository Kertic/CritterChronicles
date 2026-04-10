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

            if (_battleCoroutine != null)
                StopCoroutine(_battleCoroutine);
            _battleCoroutine = StartCoroutine(RunBattle());
        }

        private IEnumerator RunBattle()
        {
            // Short pause before battle starts
            yield return new WaitForSeconds(0.5f);

            var result = new BattleResult();
            int maxRounds = 100;
            int round = 0;

            while (round < maxRounds && _allies.Any(u => u.IsAlive) && _enemies.Any(u => u.IsAlive))
            {
                round++;

                // Gather all living units and shuffle for random turn order
                var turnOrder = new List<BattleUnit>();
                turnOrder.AddRange(_allies.Where(u => u.IsAlive));
                turnOrder.AddRange(_enemies.Where(u => u.IsAlive));
                Shuffle(turnOrder);

                foreach (var unit in turnOrder)
                {
                    if (!unit.IsAlive) continue;
                    if (!_allies.Any(u => u.IsAlive) || !_enemies.Any(u => u.IsAlive)) break;

                    // Pick a random living target from the opposing team
                    var targets = unit.IsAlly
                        ? _enemies.Where(u => u.IsAlive).ToList()
                        : _allies.Where(u => u.IsAlive).ToList();

                    if (targets.Count == 0) break;

                    var target = targets[UnityEngine.Random.Range(0, targets.Count)];
                    int damage = target.TakeDamage(unit.Attack);

                    var action = new TurnAction
                    {
                        Attacker = unit,
                        Target = target,
                        DamageDealt = damage,
                        KilledTarget = !target.IsAlive
                    };

                    result.TurnLog.Add(action);
                    _onTurnAction?.Invoke(action);

                    yield return new WaitForSeconds(0.7f);
                }
            }

            // Write back HP to instances
            foreach (var ally in _allies)
                ally.WriteBackHP();
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

        private static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}

