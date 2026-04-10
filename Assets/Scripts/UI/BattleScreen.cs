using System;
using System.Collections.Generic;
using System.Linq;
using AutobattlerSample.Battle;
using AutobattlerSample.Data;
using AutobattlerSample.Map;
using UnityEngine;
using UnityEngine.UI;

namespace AutobattlerSample.UI
{
    public class BattleScreen
    {
        private GameObject _root;
        private RectTransform _content;
        private Action<bool> _onContinue;
        private Text _footer;
        private Text _turnText;
        private Text _roundText;
        private Text _turnOrderText;
        private bool _lastBattleWon;

        private readonly Dictionary<BattleUnit, UnitVisual> _unitVisuals = new();
        private readonly Dictionary<BattleUnit, Vector2> _unitPositions = new();
        private CombatLog _combatLog;
        private BattleCombatManager _combatManager;
        private Button _nextRoundBtn;
        private Button _autoBtn;
        private Text _autoBtnLabel;

        private static readonly Color[] allyColors =
        {
            new Color(0.2f, 0.55f, 0.9f),
            new Color(0.3f, 0.75f, 0.35f),
            new Color(0.6f, 0.4f, 0.85f),
            new Color(0.9f, 0.7f, 0.2f),
            new Color(0.4f, 0.8f, 0.8f),
            new Color(0.8f, 0.5f, 0.7f)
        };

        private static readonly Color[] enemyColors =
        {
            new Color(0.9f, 0.25f, 0.2f),
            new Color(0.9f, 0.5f, 0.15f),
            new Color(0.75f, 0.2f, 0.45f),
            new Color(0.7f, 0.35f, 0.3f),
            new Color(0.85f, 0.4f, 0.5f),
            new Color(0.8f, 0.6f, 0.2f)
        };

        public static BattleScreen Create(Transform parent, Action<bool> onContinue)
        {
            var screen = new BattleScreen
            {
                _onContinue = onContinue
            };

            var canvas = UIFactory.CreateRootCanvas(parent);
            screen._root = UIFactory.CreatePanel("BattleScreen", canvas.transform, Vector2.zero, Vector2.one);
            screen._content = screen._root.GetComponent<RectTransform>();
            screen._root.SetActive(false);
            return screen;
        }

        public void SetCombatManager(BattleCombatManager manager)
        {
            _combatManager = manager;
        }

        public (List<BattleUnit> allies, List<BattleUnit> enemies) ShowBattle(
            MapNode node, List<UnitInstance> allyInstances, EncounterData encounter)
        {
            _root.SetActive(true);
            Clear();
            _unitVisuals.Clear();
            _unitPositions.Clear();

            var title = UIFactory.CreateText("Title", _content, $"Battle: {node.Label}", 36);
            var titleRt = title.rectTransform;
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.offsetMin = new Vector2(20f, -60f);
            titleRt.offsetMax = new Vector2(-20f, -10f);

            _roundText = UIFactory.CreateText("RoundText", _content, "Round 0", 22);
            _roundText.color = new Color(1f, 0.7f, 0.3f);
            _roundText.fontStyle = FontStyle.Bold;
            SetAnchoredRect(_roundText.rectTransform, new Vector2(0.42f, 0.90f), new Vector2(0.58f, 0.96f));

            _turnOrderText = UIFactory.CreateText("TurnOrder", _content, "Turn order: waiting...", 18, TextAnchor.UpperLeft);
            _turnOrderText.color = new Color(0.9f, 0.92f, 1f);
            SetAnchoredRect(_turnOrderText.rectTransform, new Vector2(0.30f, 0.80f), new Vector2(0.70f, 0.89f));

            _turnText = UIFactory.CreateText("TurnText", _content, "Battle starting...", 20);
            _turnText.color = new Color(1f, 0.9f, 0.5f);
            SetAnchoredRect(_turnText.rectTransform, new Vector2(0.15f, 0.47f), new Vector2(0.85f, 0.53f));

            var vsText = UIFactory.CreateText("VS", _content, "VS", 52);
            vsText.color = new Color(1f, 0.8f, 0.3f, 0.5f);
            SetAnchoredRect(vsText.rectTransform, new Vector2(0.47f, 0.55f), new Vector2(0.53f, 0.65f));

            var allyLabel = UIFactory.CreateText("AllyLabel", _content, "YOUR TEAM", 22);
            allyLabel.color = new Color(0.4f, 0.7f, 1f);
            SetAnchoredRect(allyLabel.rectTransform, new Vector2(0.02f, 0.85f), new Vector2(0.3f, 0.92f));

            var enemyLabel = UIFactory.CreateText("EnemyLabel", _content, "ENEMIES", 22);
            enemyLabel.color = new Color(1f, 0.4f, 0.4f);
            SetAnchoredRect(enemyLabel.rectTransform, new Vector2(0.7f, 0.85f), new Vector2(0.98f, 0.92f));

            var allyBattleUnits = new List<BattleUnit>();
            var activeAllies = allyInstances.Where(u => u.IsAlive && u.IsActive).OrderBy(u => u.Position).ToList();
            for (int i = 0; i < activeAllies.Count; i++)
            {
                var bu = new BattleUnit(activeAllies[i], true);
                allyBattleUnits.Add(bu);
                CreateUnitVisual(bu, GetUnitPositionHorizontal(i, activeAllies.Count, true), true, i);
            }

            var enemyBattleUnits = new List<BattleUnit>();
            for (int i = 0; i < encounter.Enemies.Count; i++)
                encounter.Enemies[i].Position = i;

            var sortedEnemies = encounter.Enemies.OrderBy(e => e.Position).ToList();
            for (int i = 0; i < sortedEnemies.Count; i++)
            {
                var bu = new BattleUnit(sortedEnemies[i], false);
                enemyBattleUnits.Add(bu);
                CreateUnitVisual(bu, GetUnitPositionHorizontal(i, sortedEnemies.Count, false), false, i);
            }

            _combatLog = CombatLog.Create(_content);
            CreateRoundControls();
            UpdateRoundControls();

            return (allyBattleUnits, enemyBattleUnits);
        }

        private void CreateRoundControls()
        {
            _nextRoundBtn = UIFactory.CreateButton("NextRoundBtn", _content, "Next Round");
            var nextRt = _nextRoundBtn.GetComponent<RectTransform>();
            nextRt.anchorMin = new Vector2(1f, 1f);
            nextRt.anchorMax = new Vector2(1f, 1f);
            nextRt.pivot = new Vector2(1f, 1f);
            nextRt.sizeDelta = new Vector2(150f, 40f);
            nextRt.anchoredPosition = new Vector2(-20f, -15f);
            _nextRoundBtn.GetComponent<Image>().color = new Color(0.2f, 0.38f, 0.22f);
            var nextLabel = _nextRoundBtn.GetComponentInChildren<Text>();
            if (nextLabel != null)
            {
                nextLabel.fontSize = 18;
                nextLabel.fontStyle = FontStyle.Bold;
            }
            _nextRoundBtn.onClick.AddListener(() =>
            {
                _combatManager?.AdvanceToNextRound();
                UpdateRoundControls();
            });

            _autoBtn = UIFactory.CreateButton("AutoBtn", _content, "Auto: Off");
            var autoRt = _autoBtn.GetComponent<RectTransform>();
            autoRt.anchorMin = new Vector2(1f, 1f);
            autoRt.anchorMax = new Vector2(1f, 1f);
            autoRt.pivot = new Vector2(1f, 1f);
            autoRt.sizeDelta = new Vector2(130f, 40f);
            autoRt.anchoredPosition = new Vector2(-180f, -15f);
            _autoBtnLabel = _autoBtn.GetComponentInChildren<Text>();
            if (_autoBtnLabel != null)
            {
                _autoBtnLabel.fontSize = 18;
                _autoBtnLabel.fontStyle = FontStyle.Bold;
            }
            _autoBtn.onClick.AddListener(() =>
            {
                if (_combatManager == null)
                    return;

                _combatManager.SetAutoAdvance(!_combatManager.AutoAdvance);
                if (_combatManager.AutoAdvance && _combatManager.WaitingForNextRound)
                    _combatManager.AdvanceToNextRound();
                UpdateRoundControls();
            });
        }

        private void UpdateRoundControls()
        {
            if (_combatManager == null)
                return;

            if (_autoBtnLabel != null)
                _autoBtnLabel.text = _combatManager.AutoAdvance ? "Auto: On" : "Auto: Off";

            if (_autoBtn != null)
                _autoBtn.GetComponent<Image>().color = _combatManager.AutoAdvance
                    ? new Color(0.2f, 0.45f, 0.2f)
                    : new Color(0.3f, 0.25f, 0.4f);

            if (_nextRoundBtn != null)
                _nextRoundBtn.interactable = !_combatManager.AutoAdvance;
        }

        public void OnNewRound(int round)
        {
            if (_roundText != null)
                _roundText.text = $"Round {round}";
            UpdateRoundControls();
        }

        public void OnTurnOrderReady(IReadOnlyList<BattleUnit> turnOrder)
        {
            if (_turnOrderText == null)
                return;

            if (turnOrder == null || turnOrder.Count == 0)
            {
                _turnOrderText.text = "Turn order: none";
                return;
            }

            var order = turnOrder.Select((unit, index) =>
            {
                string marker = index == 0 ? ">> " : "";
                string side = unit.IsAlly ? "A" : "E";
                return $"{marker}{side}:{unit.DisplayName}";
            });
            _turnOrderText.text = "Turn order: " + string.Join("  |  ", order);
            UpdateRoundControls();
        }

        public void OnTurnAction(TurnAction action)
        {
            if (action.WasOnCooldown)
            {
                if (_turnText != null)
                    _turnText.text = $"{action.Attacker.DisplayName} is on cooldown ({action.AttackerCooldownAfter} turns)";

                if (_unitVisuals.TryGetValue(action.Attacker, out var skipVisual))
                    skipVisual.UpdateStats();

                _combatLog?.AddEntry(action);
                return;
            }

            if (_turnText != null)
            {
                switch (action.UsedActionType)
                {
                    case ActionType.Attack:
                    {
                        string arrow = action.Attacker.IsAlly ? " >> " : " << ";
                        string extra = "";
                        if (action.ShieldAbsorbed > 0)
                            extra += $" [{action.ShieldAbsorbed} shielded]";
                        if (action.LifestealHealed > 0)
                            extra += $" [+{action.LifestealHealed} heal]";
                        _turnText.text = $"{action.Attacker.DisplayName}{arrow}{action.Target.DisplayName}  (-{action.DamageDealt}){extra}";
                        break;
                    }
                    case ActionType.ShieldSelf:
                        _turnText.text = $"{action.Attacker.DisplayName} shields self (+{action.ShieldGained} shield)";
                        break;
                    case ActionType.HealSelf:
                        _turnText.text = $"{action.Attacker.DisplayName} heals self (+{action.HealAmount} HP)";
                        break;
                    case ActionType.HealFront:
                        _turnText.text = $"{action.Attacker.DisplayName} heals {action.Target.DisplayName} (+{action.HealAmount} HP)";
                        break;
                    case ActionType.HealAll:
                        _turnText.text = $"{action.Attacker.DisplayName} heals all allies (+{action.HealAmount} HP total)";
                        break;
                }
            }

            if (_unitVisuals.TryGetValue(action.Attacker, out var attackerVisual))
            {
                if (action.UsedActionType == ActionType.Attack)
                {
                    Vector2 dir = action.Attacker.IsAlly ? Vector2.right : Vector2.left;
                    attackerVisual.StartCoroutine(attackerVisual.PlayAttackWiggle(dir));
                }
                attackerVisual.UpdateStats();
            }

            if (action.Target != null && action.Target != action.Attacker &&
                _unitVisuals.TryGetValue(action.Target, out var targetVisual))
            {
                if (action.UsedActionType == ActionType.Attack)
                    targetVisual.StartCoroutine(targetVisual.PlayHitWiggle());
                targetVisual.UpdateStats();
            }

            if (action.UsedActionType == ActionType.Attack && _unitPositions.TryGetValue(action.Target, out var targetPos))
            {
                Color dmgColor = action.Attacker.IsAlly ? new Color(1f, 1f, 0.3f) : new Color(1f, 0.3f, 0.3f);
                DamageNumber.Spawn(_content, targetPos + new Vector2(0, 55f), action.DamageDealt, dmgColor);
            }

            if (action.LifestealHealed > 0 && _unitPositions.TryGetValue(action.Attacker, out var atkPos))
                DamageNumber.SpawnHeal(_content, atkPos + new Vector2(0, 55f), action.LifestealHealed);

            if ((action.UsedActionType == ActionType.HealSelf || action.UsedActionType == ActionType.HealFront)
                && action.HealAmount > 0 && action.Target != null && _unitPositions.TryGetValue(action.Target, out var healPos))
            {
                DamageNumber.SpawnHeal(_content, healPos + new Vector2(0, 55f), action.HealAmount);
            }

            if (action.UsedActionType == ActionType.HealAll && action.HealAllResults != null)
            {
                foreach (var (healedUnit, healed) in action.HealAllResults)
                {
                    if (healed > 0 && _unitPositions.TryGetValue(healedUnit, out var haPos))
                        DamageNumber.SpawnHeal(_content, haPos + new Vector2(0, 55f), healed);
                    if (_unitVisuals.TryGetValue(healedUnit, out var healedVisual))
                        healedVisual.UpdateStats();
                }
            }

            if (action.UsedActionType == ActionType.ShieldSelf && action.ShieldGained > 0
                && _unitPositions.TryGetValue(action.Attacker, out var shieldPos))
            {
                DamageNumber.SpawnShield(_content, shieldPos + new Vector2(0, 55f), action.ShieldGained);
            }

            _combatLog?.AddEntry(action);
        }

        public void ShowResult(bool playerWon)
        {
            _lastBattleWon = playerWon;

            if (_turnText != null)
                _turnText.text = playerWon ? "All enemies defeated!" : "Your team has fallen...";

            _footer = UIFactory.CreateText("Footer", _content, playerWon ? "VICTORY!" : "DEFEAT...", 40);
            _footer.fontStyle = FontStyle.Bold;
            _footer.color = playerWon ? new Color(0.3f, 1f, 0.3f) : new Color(1f, 0.3f, 0.3f);
            SetAnchoredRect(_footer.rectTransform, new Vector2(0.3f, 0.08f), new Vector2(0.7f, 0.18f));

            var button = UIFactory.CreateButton("Continue", _content, "Continue");
            SetAnchoredRect(button.GetComponent<RectTransform>(), new Vector2(0.4f, 0.01f), new Vector2(0.6f, 0.07f));
            button.onClick.AddListener(() => _onContinue?.Invoke(_lastBattleWon));
        }

        public void SetFooter(string text)
        {
            if (_footer != null) _footer.text = text;
        }

        public void Hide() => _root.SetActive(false);

        private void CreateUnitVisual(BattleUnit unit, Vector2 position, bool isAlly, int index)
        {
            var container = new GameObject($"Unit_{unit.DisplayName}", typeof(RectTransform));
            container.transform.SetParent(_content, false);
            var containerRt = container.GetComponent<RectTransform>();
            containerRt.anchorMin = new Vector2(0.5f, 0.5f);
            containerRt.anchorMax = new Vector2(0.5f, 0.5f);
            containerRt.anchoredPosition = position;
            containerRt.sizeDelta = Vector2.zero;

            Color color = isAlly ? allyColors[index % allyColors.Length] : enemyColors[index % enemyColors.Length];
            Image shape = isAlly
                ? UIFactory.CreateSquare(container.transform, color, 80f)
                : UIFactory.CreateCircle(container.transform, color, 80f);

            var statsText = UIFactory.CreateText("Stats", container.transform, "", 14, TextAnchor.LowerCenter);
            var statsRt = statsText.rectTransform;
            statsRt.anchorMin = new Vector2(0.5f, 0.5f);
            statsRt.anchorMax = new Vector2(0.5f, 0.5f);
            statsRt.sizeDelta = new Vector2(200f, 80f);
            statsRt.anchoredPosition = new Vector2(0f, 100f);

            var (hpBg, hpFill) = UIFactory.CreateHPBar(container.transform, 100f, 10f);
            hpBg.rectTransform.anchoredPosition = new Vector2(0f, 50f);

            var visual = container.AddComponent<UnitVisual>();
            visual.Init(unit, shape, statsText, hpFill);

            _unitVisuals[unit] = visual;
            _unitPositions[unit] = position;
        }

        private Vector2 GetUnitPositionHorizontal(int index, int total, bool isAlly)
        {
            float y = 30f;
            float spacing = 120f;

            if (isAlly)
                return new Vector2(-200f - index * spacing, y);

            return new Vector2(200f + index * spacing, y);
        }

        private void Clear()
        {
            for (int i = _content.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(_content.GetChild(i).gameObject);
        }

        private static void SetAnchoredRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
