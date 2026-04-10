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
        private bool _lastBattleWon;

        private readonly Dictionary<BattleUnit, UnitVisual> _unitVisuals = new();
        private readonly Dictionary<BattleUnit, Vector2> _unitPositions = new();
        private CombatLog _combatLog;
        private BattleCombatManager _combatManager;
        private Button _pauseBtn;
        private Text _pauseBtnLabel;

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

            // Title
            var title = UIFactory.CreateText("Title", _content, $"Battle: {node.Label}", 36);
            var titleRt = title.rectTransform;
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.offsetMin = new Vector2(20f, -60f);
            titleRt.offsetMax = new Vector2(-20f, -10f);

            // Round counter
            _roundText = UIFactory.CreateText("RoundText", _content, "Round 0", 22);
            _roundText.color = new Color(1f, 0.7f, 0.3f);
            _roundText.fontStyle = FontStyle.Bold;
            var roundRt = _roundText.rectTransform;
            roundRt.anchorMin = new Vector2(0.42f, 0.90f);
            roundRt.anchorMax = new Vector2(0.58f, 0.96f);
            roundRt.offsetMin = Vector2.zero;
            roundRt.offsetMax = Vector2.zero;

            // Turn indicator text
            _turnText = UIFactory.CreateText("TurnText", _content, "Battle starting...", 20);
            _turnText.color = new Color(1f, 0.9f, 0.5f);
            var turnRt = _turnText.rectTransform;
            turnRt.anchorMin = new Vector2(0.15f, 0.47f);
            turnRt.anchorMax = new Vector2(0.85f, 0.53f);
            turnRt.offsetMin = Vector2.zero;
            turnRt.offsetMax = Vector2.zero;

            // "VS" text
            var vsText = UIFactory.CreateText("VS", _content, "VS", 52);
            vsText.color = new Color(1f, 0.8f, 0.3f, 0.5f);
            var vsRt = vsText.rectTransform;
            vsRt.anchorMin = new Vector2(0.47f, 0.55f);
            vsRt.anchorMax = new Vector2(0.53f, 0.65f);
            vsRt.offsetMin = Vector2.zero;
            vsRt.offsetMax = Vector2.zero;

            // Side labels
            var allyLabel = UIFactory.CreateText("AllyLabel", _content, "YOUR TEAM", 22);
            allyLabel.color = new Color(0.4f, 0.7f, 1f);
            SetAnchoredRect(allyLabel.rectTransform, new Vector2(0.02f, 0.85f), new Vector2(0.3f, 0.92f));

            var enemyLabel = UIFactory.CreateText("EnemyLabel", _content, "ENEMIES", 22);
            enemyLabel.color = new Color(1f, 0.4f, 0.4f);
            SetAnchoredRect(enemyLabel.rectTransform, new Vector2(0.7f, 0.85f), new Vector2(0.98f, 0.92f));

            // Create ally BattleUnits — sorted by Position (front first)
            var allyBattleUnits = new List<BattleUnit>();
            var activeAllies = allyInstances.Where(u => u.IsAlive && u.IsActive).OrderBy(u => u.Position).ToList();
            for (int i = 0; i < activeAllies.Count; i++)
            {
                var bu = new BattleUnit(activeAllies[i], true);
                allyBattleUnits.Add(bu);
                Vector2 pos = GetUnitPositionHorizontal(i, activeAllies.Count, true);
                CreateUnitVisual(bu, pos, true, i);
            }

            // Create enemy BattleUnits — sorted by Position (front first)
            var enemyBattleUnits = new List<BattleUnit>();
            // Assign positions to enemies
            for (int i = 0; i < encounter.Enemies.Count; i++)
                encounter.Enemies[i].Position = i;
            var sortedEnemies = encounter.Enemies.OrderBy(e => e.Position).ToList();
            for (int i = 0; i < sortedEnemies.Count; i++)
            {
                var bu = new BattleUnit(sortedEnemies[i], false);
                enemyBattleUnits.Add(bu);
                Vector2 pos = GetUnitPositionHorizontal(i, sortedEnemies.Count, false);
                CreateUnitVisual(bu, pos, false, i);
            }

            // Create combat log panel
            _combatLog = CombatLog.Create(_content);

            // Pause button
            _pauseBtn = UIFactory.CreateButton("PauseBtn", _content, "\u23F8 Pause");
            var pauseRt = _pauseBtn.GetComponent<RectTransform>();
            pauseRt.anchorMin = new Vector2(1f, 1f);
            pauseRt.anchorMax = new Vector2(1f, 1f);
            pauseRt.pivot = new Vector2(1f, 1f);
            pauseRt.sizeDelta = new Vector2(120f, 40f);
            pauseRt.anchoredPosition = new Vector2(-20f, -15f);
            _pauseBtn.GetComponent<Image>().color = new Color(0.3f, 0.25f, 0.4f);
            _pauseBtnLabel = _pauseBtn.GetComponentInChildren<Text>();
            if (_pauseBtnLabel != null) { _pauseBtnLabel.fontSize = 18; _pauseBtnLabel.fontStyle = FontStyle.Bold; }
            _pauseBtn.onClick.AddListener(() =>
            {
                if (_combatManager != null)
                {
                    _combatManager.TogglePause();
                    _pauseBtnLabel.text = _combatManager.IsPaused ? "\u25B6 Resume" : "\u23F8 Pause";
                    _pauseBtn.GetComponent<Image>().color = _combatManager.IsPaused
                        ? new Color(0.2f, 0.45f, 0.2f)
                        : new Color(0.3f, 0.25f, 0.4f);
                }
            });

            return (allyBattleUnits, enemyBattleUnits);
        }

        public void OnNewRound(int round)
        {
            if (_roundText != null)
                _roundText.text = $"Round {round}";
        }

        public void OnTurnAction(TurnAction action)
        {
            // Handle cooldown skip
            if (action.WasOnCooldown)
            {
                if (_turnText != null)
                    _turnText.text = $"{action.Attacker.DisplayName} is on cooldown ({action.AttackerCooldownAfter} turns)";

                if (_unitVisuals.TryGetValue(action.Attacker, out var skipVisual))
                    skipVisual.UpdateStats();

                _combatLog?.AddEntry(action);
                return;
            }

            // Update turn text based on action type
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

            // Attacker visual effects
            if (_unitVisuals.TryGetValue(action.Attacker, out var attackerVisual))
            {
                if (action.UsedActionType == ActionType.Attack)
                {
                    Vector2 dir = action.Attacker.IsAlly ? Vector2.right : Vector2.left;
                    attackerVisual.StartCoroutine(attackerVisual.PlayAttackWiggle(dir));
                }
                attackerVisual.UpdateStats();
            }

            // Target visual effects
            if (action.Target != null && action.Target != action.Attacker &&
                _unitVisuals.TryGetValue(action.Target, out var targetVisual))
            {
                if (action.UsedActionType == ActionType.Attack)
                    targetVisual.StartCoroutine(targetVisual.PlayHitWiggle());
                targetVisual.UpdateStats();
            }

            // Floating numbers
            if (action.UsedActionType == ActionType.Attack && _unitPositions.TryGetValue(action.Target, out var targetPos))
            {
                Color dmgColor = action.Attacker.IsAlly ? new Color(1f, 1f, 0.3f) : new Color(1f, 0.3f, 0.3f);
                DamageNumber.Spawn(_content, targetPos + new Vector2(0, 55f), action.DamageDealt, dmgColor);
            }

            if (action.LifestealHealed > 0 && _unitPositions.TryGetValue(action.Attacker, out var atkPos))
            {
                DamageNumber.SpawnHeal(_content, atkPos + new Vector2(0, 55f), action.LifestealHealed);
            }

            if ((action.UsedActionType == ActionType.HealSelf || action.UsedActionType == ActionType.HealFront)
                && action.HealAmount > 0 && action.Target != null && _unitPositions.TryGetValue(action.Target, out var healPos))
            {
                DamageNumber.SpawnHeal(_content, healPos + new Vector2(0, 55f), action.HealAmount);
            }

            // HealAll: show floating heal numbers on each healed unit and update their visuals
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
            Image shape;
            if (isAlly)
                shape = UIFactory.CreateSquare(container.transform, color, 80f);
            else
                shape = UIFactory.CreateCircle(container.transform, color, 80f);

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

        /// <summary>
        /// Horizontal layout: allies on left, enemies on right.
        /// Units within a team are spread horizontally (side by side).
        /// Front units are closest to center, back units further out.
        /// </summary>
        private Vector2 GetUnitPositionHorizontal(int index, int total, bool isAlly)
        {
            // Y position: fixed baseline
            float y = 30f;

            // X position: spread horizontally within each side
            float spacing = 120f;

            if (isAlly)
            {
                // Allies on the left; front (index 0) = closest to center (rightmost)
                float x = -200f - index * spacing;
                return new Vector2(x, y);
            }
            else
            {
                // Enemies on the right; front (index 0) = closest to center (leftmost)
                float x = 200f + index * spacing;
                return new Vector2(x, y);
            }
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
