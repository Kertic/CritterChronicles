using System;
using System.Collections.Generic;
using AutobattlerSample.Core;
using AutobattlerSample.Data;
using AutobattlerSample.Map;
using UnityEngine;
using UnityEngine.UI;

namespace AutobattlerSample.UI
{
    public class MapScreen
    {
        private GameObject _root;
        private RectTransform _content;
        private Action<MapNode> _onNodeSelected;
        private Action _onManageTeam;

        public static MapScreen Create(Transform parent, Action<MapNode> onNodeSelected, Action onManageTeam = null)
        {
            var screen = new MapScreen();
            screen._onNodeSelected = onNodeSelected;
            screen._onManageTeam = onManageTeam;

            var canvas = UIFactory.CreateRootCanvas(parent);
            screen._root = UIFactory.CreatePanel("MapScreen", canvas.transform, Vector2.zero, Vector2.one);
            screen._content = screen._root.GetComponent<RectTransform>();
            screen._root.SetActive(false);
            return screen;
        }

        public void Show(RunState state)
        {
            _root.SetActive(true);
            Clear();

            int totalFloors = state.Map.Floors.Count;

            var nodePositions = new Dictionary<MapNode, Vector2>();
            float yStart = -350f;
            float yEnd = 320f;
            float ySpacing = totalFloors > 1 ? (yEnd - yStart) / (totalFloors - 1) : 0f;
            float xSpacing = 220f;

            for (int floor = 0; floor < totalFloors; floor++)
            {
                var row = state.Map.Floors[floor];
                float y = yStart + floor * ySpacing;
                float totalWidth = (row.Count - 1) * xSpacing;

                for (int i = 0; i < row.Count; i++)
                {
                    float x = (i * xSpacing) - totalWidth * 0.5f;
                    nodePositions[row[i]] = new Vector2(x, y);
                }
            }

            // Draw connection lines
            for (int floor = 0; floor < totalFloors; floor++)
            {
                foreach (var node in state.Map.Floors[floor])
                {
                    foreach (var child in node.Children)
                    {
                        Color lineColor = new Color(0.35f, 0.35f, 0.4f, 0.6f);
                        if (node.Visited && state.Map.IsNodeSelectable(child))
                            lineColor = new Color(0.6f, 0.7f, 0.9f, 0.8f);
                        UIFactory.CreateLine(_content, nodePositions[node], nodePositions[child], lineColor, 3f);
                    }
                }
            }

            // Draw node buttons
            for (int floor = 0; floor < totalFloors; floor++)
            {
                var row = state.Map.Floors[floor];
                for (int i = 0; i < row.Count; i++)
                {
                    var node = row[i];
                    Vector2 pos = nodePositions[node];

                    string label = node.Label;
                    if (node.Encounter != null && node.Type != MapNodeType.Rest && node.Type != MapNodeType.Boss && node.Type != MapNodeType.Shop)
                    {
                        int enemyCount = node.Encounter.Enemies.Count;
                        label += $"\n({enemyCount} foes)";
                    }

                    var button = UIFactory.CreateButton($"Node_{floor}_{i}", _content, label);
                    var rt = button.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = new Vector2(200f, 70f);
                    rt.anchoredPosition = pos;

                    var labelText = button.GetComponentInChildren<Text>();
                    if (labelText != null) labelText.fontSize = 18;

                    bool selectable = state.Map.IsNodeSelectable(node);
                    var capturedNode = node;
                    button.onClick.AddListener(() => _onNodeSelected?.Invoke(capturedNode));

                    var colors = button.colors;
                    Color nodeColor = GetNodeColor(node, selectable);
                    colors.normalColor = nodeColor;
                    colors.highlightedColor = nodeColor * 1.3f;
                    colors.pressedColor = nodeColor * 0.7f;
                    colors.disabledColor = node.Visited
                        ? new Color(0.08f, 0.12f, 0.08f)
                        : new Color(0.12f, 0.12f, 0.12f);
                    button.colors = colors;
                    button.interactable = selectable;
                }
            }

            // Title
            var title = UIFactory.CreateText("Title", _content, "Choose Your Path", 38);
            title.fontStyle = FontStyle.Bold;
            var titleRt = title.rectTransform;
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.offsetMin = new Vector2(20f, -55f);
            titleRt.offsetMax = new Vector2(-20f, -10f);

            // Team info at bottom
            string teamInfo = "Team: ";
            foreach (var u in state.Team)
            {
                if (!u.IsAlive) continue;
                string rankStr = u.Rank > 1 ? $"R{u.Rank} " : "";
                string shieldStr = u.Shield > 0 ? $" Sh:{u.Shield}" : "";
                teamInfo += $"  {u.DisplayName} ({rankStr}HP:{u.CurrentHP}/{u.EffectiveMaxHP} ATK:{u.EffectiveAttackDamage} CD:{u.EffectiveCooldown}{shieldStr})";
            }
            var teamText = UIFactory.CreateText("Team", _content, teamInfo, 16);
            teamText.color = new Color(0.7f, 0.8f, 1f);
            var teamRt = teamText.rectTransform;
            teamRt.anchorMin = new Vector2(0f, 0f);
            teamRt.anchorMax = new Vector2(1f, 0f);
            teamRt.offsetMin = new Vector2(20f, 10f);
            teamRt.offsetMax = new Vector2(-20f, 50f);

            // Manage Team button
            if (_onManageTeam != null)
            {
                var manageBtn = UIFactory.CreateButton("ManageTeam", _content, "Manage Team");
                var manageBtnRt = manageBtn.GetComponent<RectTransform>();
                manageBtnRt.anchorMin = new Vector2(1f, 0f);
                manageBtnRt.anchorMax = new Vector2(1f, 0f);
                manageBtnRt.pivot = new Vector2(1f, 0f);
                manageBtnRt.sizeDelta = new Vector2(180f, 45f);
                manageBtnRt.anchoredPosition = new Vector2(-20f, 55f);
                var manageBtnLabel = manageBtn.GetComponentInChildren<Text>();
                if (manageBtnLabel != null) manageBtnLabel.fontSize = 18;
                var btnColors = manageBtn.colors;
                btnColors.normalColor = new Color(0.2f, 0.3f, 0.45f);
                btnColors.highlightedColor = new Color(0.25f, 0.4f, 0.55f);
                manageBtn.colors = btnColors;
                manageBtn.onClick.AddListener(() => _onManageTeam?.Invoke());
            }

            // Floor labels on the side
            for (int floor = 0; floor < totalFloors; floor++)
            {
                float y = yStart + floor * ySpacing;
                string floorLabel = floor == totalFloors - 1 ? "BOSS" : $"Floor {floor + 1}";
                var fText = UIFactory.CreateText($"Floor_{floor}", _content, floorLabel, 14);
                fText.color = new Color(0.5f, 0.5f, 0.6f);
                var fRt = fText.rectTransform;
                fRt.anchorMin = new Vector2(0.5f, 0.5f);
                fRt.anchorMax = new Vector2(0.5f, 0.5f);
                fRt.sizeDelta = new Vector2(100f, 30f);
                fRt.anchoredPosition = new Vector2(-500f, y);
            }
        }

        public void Hide() => _root.SetActive(false);

        private Color GetNodeColor(MapNode node, bool selectable)
        {
            if (!selectable && !node.Visited)
                return new Color(0.15f, 0.15f, 0.15f);

            Color baseColor;
            switch (node.Type)
            {
                case MapNodeType.Battle:
                    baseColor = new Color(0.25f, 0.25f, 0.4f);
                    break;
                case MapNodeType.Elite:
                    baseColor = new Color(0.5f, 0.35f, 0.1f);
                    break;
                case MapNodeType.Rest:
                    baseColor = new Color(0.15f, 0.4f, 0.15f);
                    break;
                case MapNodeType.Boss:
                    baseColor = new Color(0.55f, 0.1f, 0.1f);
                    break;
                case MapNodeType.Shop:
                    baseColor = new Color(0.15f, 0.35f, 0.45f);
                    break;
                default:
                    baseColor = new Color(0.3f, 0.3f, 0.3f);
                    break;
            }

            if (node.Reinforced)
                baseColor = Color.Lerp(baseColor, new Color(0.8f, 0.15f, 0.1f), 0.4f);

            return baseColor;
        }

        private void Clear()
        {
            for (int i = _content.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(_content.GetChild(i).gameObject);
        }
    }
}
