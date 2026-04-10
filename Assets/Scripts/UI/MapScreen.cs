using System;
using System.Collections.Generic;
using System.Linq;
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
        private Action _onHelp;

        public static MapScreen Create(Transform parent, Action<MapNode> onNodeSelected, Action onManageTeam = null, Action onHelp = null)
        {
            var screen = new MapScreen();
            screen._onNodeSelected = onNodeSelected;
            screen._onManageTeam = onManageTeam;
            screen._onHelp = onHelp;

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

            // Create a scroll view to handle large maps
            var scrollGo = new GameObject("MapScroll", typeof(RectTransform), typeof(ScrollRect));
            scrollGo.transform.SetParent(_content, false);
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0f, 0.06f);
            scrollRt.anchorMax = new Vector2(1f, 0.88f);
            scrollRt.offsetMin = Vector2.zero;
            scrollRt.offsetMax = Vector2.zero;

            var maskGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            maskGo.transform.SetParent(scrollGo.transform, false);
            maskGo.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f);
            maskGo.GetComponent<Mask>().showMaskGraphic = false;
            var maskRt = maskGo.GetComponent<RectTransform>();
            maskRt.anchorMin = Vector2.zero;
            maskRt.anchorMax = Vector2.one;
            maskRt.offsetMin = Vector2.zero;
            maskRt.offsetMax = Vector2.zero;

            var mapContent = new GameObject("MapContent", typeof(RectTransform));
            mapContent.transform.SetParent(maskGo.transform, false);
            var mapContentRt = mapContent.GetComponent<RectTransform>();

            float ySpacing = 120f;
            float totalHeight = (totalFloors - 1) * ySpacing + 100f;
            mapContentRt.anchorMin = new Vector2(0.5f, 0f);
            mapContentRt.anchorMax = new Vector2(0.5f, 0f);
            mapContentRt.pivot = new Vector2(0.5f, 0f);
            mapContentRt.sizeDelta = new Vector2(1200f, totalHeight);
            mapContentRt.anchoredPosition = Vector2.zero;

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = maskRt;
            scroll.content = mapContentRt;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;

            var nodePositions = new Dictionary<MapNode, Vector2>();
            float xSpacing = 200f;

            for (int floor = 0; floor < totalFloors; floor++)
            {
                var row = state.Map.Floors[floor];
                float y = floor * ySpacing + 50f;
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
                        var lineImg = UIFactory.CreateLine(mapContentRt, nodePositions[node], nodePositions[child], lineColor, 3f);
                        // Reanchor line to bottom-left of map content
                        var lineRt = lineImg.rectTransform;
                        lineRt.anchorMin = new Vector2(0.5f, 0f);
                        lineRt.anchorMax = new Vector2(0.5f, 0f);
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

                    var button = UIFactory.CreateButton($"Node_{floor}_{i}", mapContentRt, label);
                    var rt = button.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.5f, 0f);
                    rt.anchorMax = new Vector2(0.5f, 0f);
                    rt.sizeDelta = new Vector2(180f, 60f);
                    rt.anchoredPosition = pos;

                    var labelText = button.GetComponentInChildren<Text>();
                    if (labelText != null) labelText.fontSize = 16;

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

            // Floor labels
            for (int floor = 0; floor < totalFloors; floor++)
            {
                float y = floor * ySpacing + 50f;
                string floorLabel = floor == totalFloors - 1 ? "BOSS" : $"F{floor + 1}";
                var fText = UIFactory.CreateText($"Floor_{floor}", mapContentRt, floorLabel, 13);
                fText.color = new Color(0.5f, 0.5f, 0.6f);
                var fRt = fText.rectTransform;
                fRt.anchorMin = new Vector2(0.5f, 0f);
                fRt.anchorMax = new Vector2(0.5f, 0f);
                fRt.sizeDelta = new Vector2(80f, 25f);
                fRt.anchoredPosition = new Vector2(-530f, y);
            }

            // Scroll to current floor
            if (state.Map.CurrentNode != null)
            {
                float targetY = state.Map.CurrentNode.Floor * ySpacing + 50f;
                float normalizedY = totalHeight > 0 ? Mathf.Clamp01(targetY / totalHeight) : 0f;
                scroll.verticalNormalizedPosition = normalizedY;
            }
            else
            {
                scroll.verticalNormalizedPosition = 0f;
            }

            // Title
            var titleText = UIFactory.CreateText("Title", _content, "Choose Your Path", 36);
            titleText.fontStyle = FontStyle.Bold;
            var titleRt = titleText.rectTransform;
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.offsetMin = new Vector2(20f, -50f);
            titleRt.offsetMax = new Vector2(-20f, -10f);
            string teamInfo = $"Team ({state.UsedSlots}/{RunState.MaxSlots} slots): ";
            foreach (var u in state.Team)
            {
                string rankStr = u.Rank > 1 ? $"R{u.Rank} " : "";
                string pos = $"P{u.Position}";
                teamInfo += $"  {u.DisplayName}({rankStr}{pos} HP:{u.CurrentHP}/{u.EffectiveMaxHP})";
            }
            if (state.CampRoster.Count > 0)
                teamInfo += $"  | Camp: {state.CampRoster.Count} units";

            var teamText = UIFactory.CreateText("Team", _content, teamInfo, 14);
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

            // Help button
            {
                var helpBtn = UIFactory.CreateButton("Help", _content, "?");
                var helpBtnRt = helpBtn.GetComponent<RectTransform>();
                helpBtnRt.anchorMin = new Vector2(1f, 1f);
                helpBtnRt.anchorMax = new Vector2(1f, 1f);
                helpBtnRt.pivot = new Vector2(1f, 1f);
                helpBtnRt.sizeDelta = new Vector2(45f, 45f);
                helpBtnRt.anchoredPosition = new Vector2(-20f, -10f);
                var helpLabel = helpBtn.GetComponentInChildren<Text>();
                if (helpLabel != null) { helpLabel.fontSize = 26; helpLabel.fontStyle = FontStyle.Bold; }
                var hColors = helpBtn.colors;
                hColors.normalColor = new Color(0.35f, 0.3f, 0.45f);
                hColors.highlightedColor = new Color(0.45f, 0.4f, 0.55f);
                helpBtn.colors = hColors;
                helpBtn.onClick.AddListener(() => _onHelp?.Invoke());
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
