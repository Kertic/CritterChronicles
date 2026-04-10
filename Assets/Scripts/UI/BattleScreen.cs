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
        private bool _lastBattleWon;

        private readonly Dictionary<BattleUnit, UnitVisual> _unitVisuals = new();
        private readonly Dictionary<BattleUnit, Vector2> _unitPositions = new();
        private CombatLog _combatLog;

        private static readonly Color[] allyColors =
        {
            new Color(0.2f, 0.55f, 0.9f),
            new Color(0.3f, 0.75f, 0.35f),
            new Color(0.6f, 0.4f, 0.85f),
            new Color(0.9f, 0.7f, 0.2f)
        };

        private static readonly Color[] enemyColors =
        {
            new Color(0.9f, 0.25f, 0.2f),
            new Color(0.9f, 0.5f, 0.15f),
            new Color(0.75f, 0.2f, 0.45f),
            new Color(0.7f, 0.35f, 0.3f)
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

            // Turn indicator text
            _turnText = UIFactory.CreateText("TurnText", _content, "Battle starting...", 24);
            _turnText.color = new Color(1f, 0.9f, 0.5f);
            var turnRt = _turnText.rectTransform;
            turnRt.anchorMin = new Vector2(0.3f, 0.5f);
            turnRt.anchorMax = new Vector2(0.7f, 0.58f);
            turnRt.offsetMin = Vector2.zero;
            turnRt.offsetMax = Vector2.zero;

            // "VS" text
            var vsText = UIFactory.CreateText("VS", _content, "VS", 52);
            vsText.color = new Color(1f, 0.8f, 0.3f, 0.5f);
            var vsRt = vsText.rectTransform;
            vsRt.anchorMin = new Vector2(0.45f, 0.38f);
            vsRt.anchorMax = new Vector2(0.55f, 0.5f);
            vsRt.offsetMin = Vector2.zero;
            vsRt.offsetMax = Vector2.zero;

            // Side labels
            var allyLabel = UIFactory.CreateText("AllyLabel", _content, "YOUR TEAM", 22);
            allyLabel.color = new Color(0.4f, 0.7f, 1f);
            SetAnchoredRect(allyLabel.rectTransform, new Vector2(0.05f, 0.85f), new Vector2(0.4f, 0.92f));

            var enemyLabel = UIFactory.CreateText("EnemyLabel", _content, "ENEMIES", 22);
            enemyLabel.color = new Color(1f, 0.4f, 0.4f);
            SetAnchoredRect(enemyLabel.rectTransform, new Vector2(0.6f, 0.85f), new Vector2(0.95f, 0.92f));

            // Create ally BattleUnits and visuals (left side, squares)
            var allyBattleUnits = new List<BattleUnit>();
            var livingAllies = allyInstances.Where(u => u.IsAlive).ToList();
            for (int i = 0; i < livingAllies.Count; i++)
            {
                var bu = new BattleUnit(livingAllies[i], true);
                allyBattleUnits.Add(bu);
                Vector2 pos = GetUnitPosition(i, livingAllies.Count, true);
                CreateUnitVisual(bu, pos, true, i);
            }

            // Create enemy BattleUnits and visuals (right side, circles)
            var enemyBattleUnits = new List<BattleUnit>();
            for (int i = 0; i < encounter.Enemies.Count; i++)
            {
                var bu = new BattleUnit(encounter.Enemies[i], false);
                enemyBattleUnits.Add(bu);
                Vector2 pos = GetUnitPosition(i, encounter.Enemies.Count, false);
                CreateUnitVisual(bu, pos, false, i);
            }

            // Create combat log panel
            _combatLog = CombatLog.Create(_content);

            return (allyBattleUnits, enemyBattleUnits);
        }

        public void OnTurnAction(TurnAction action)
        {
            // Update turn text
            if (_turnText != null)
            {
                string arrow = action.Attacker.IsAlly ? " >> " : " << ";
                _turnText.text = $"{action.Attacker.DisplayName}{arrow}{action.Target.DisplayName}  (-{action.DamageDealt})";
            }

            // Play attacker wiggle
            if (_unitVisuals.TryGetValue(action.Attacker, out var attackerVisual))
            {
                Vector2 dir = action.Attacker.IsAlly ? Vector2.right : Vector2.left;
                attackerVisual.StartCoroutine(attackerVisual.PlayAttackWiggle(dir));
            }

            // Play target hit wiggle
            if (_unitVisuals.TryGetValue(action.Target, out var targetVisual))
            {
                targetVisual.StartCoroutine(targetVisual.PlayHitWiggle());
                targetVisual.UpdateStats();
            }

            // Spawn floating damage number at target position
            if (_unitPositions.TryGetValue(action.Target, out var targetPos))
            {
                Color dmgColor = action.Attacker.IsAlly ? new Color(1f, 1f, 0.3f) : new Color(1f, 0.3f, 0.3f);
                DamageNumber.Spawn(_content, targetPos + new Vector2(0, 55f), action.DamageDealt, dmgColor);
            }

            // Append to combat log
            _combatLog?.AddEntry(action);
        }

        public void ShowResult(bool playerWon)
        {
            _lastBattleWon = playerWon;

            // Update turn text
            if (_turnText != null)
                _turnText.text = playerWon ? "All enemies defeated!" : "Your team has fallen...";

            // Footer
            _footer = UIFactory.CreateText("Footer", _content, playerWon ? "VICTORY!" : "DEFEAT...", 40);
            _footer.fontStyle = FontStyle.Bold;
            _footer.color = playerWon ? new Color(0.3f, 1f, 0.3f) : new Color(1f, 0.3f, 0.3f);
            SetAnchoredRect(_footer.rectTransform, new Vector2(0.3f, 0.08f), new Vector2(0.7f, 0.18f));

            // Continue button
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
            // Container
            var container = new GameObject($"Unit_{unit.DisplayName}", typeof(RectTransform));
            container.transform.SetParent(_content, false);
            var containerRt = container.GetComponent<RectTransform>();
            containerRt.anchorMin = new Vector2(0.5f, 0.5f);
            containerRt.anchorMax = new Vector2(0.5f, 0.5f);
            containerRt.anchoredPosition = position;
            containerRt.sizeDelta = Vector2.zero;

            // Shape: square for allies, circle for enemies
            Color color = isAlly ? allyColors[index % allyColors.Length] : enemyColors[index % enemyColors.Length];
            Image shape;
            if (isAlly)
                shape = UIFactory.CreateSquare(container.transform, color, 80f);
            else
                shape = UIFactory.CreateCircle(container.transform, color, 80f);

            // Stat label above shape
            var statsText = UIFactory.CreateText("Stats", container.transform, "", 16, TextAnchor.LowerCenter);
            var statsRt = statsText.rectTransform;
            statsRt.anchorMin = new Vector2(0.5f, 0.5f);
            statsRt.anchorMax = new Vector2(0.5f, 0.5f);
            statsRt.sizeDelta = new Vector2(200f, 70f);
            statsRt.anchoredPosition = new Vector2(0f, 75f);

            // Add UnitVisual component
            var visual = container.AddComponent<UnitVisual>();
            visual.Init(unit, shape, statsText);

            _unitVisuals[unit] = visual;
            _unitPositions[unit] = position;
        }

        private Vector2 GetUnitPosition(int index, int total, bool isAlly)
        {
            float x = isAlly ? -350f : 350f;
            float yBase = -30f;
            float spacing = 150f;
            float totalH = (total - 1) * spacing;
            float y = yBase + totalH / 2f - index * spacing;
            return new Vector2(x, y);
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
