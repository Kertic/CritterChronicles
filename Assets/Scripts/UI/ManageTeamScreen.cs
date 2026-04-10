using System;
using System.Collections.Generic;
using AutobattlerSample.Core;
using AutobattlerSample.Data;
using UnityEngine;
using UnityEngine.UI;

namespace AutobattlerSample.UI
{
    public class ManageTeamScreen
    {
        private GameObject _root;
        private RectTransform _content;
        private Action _onDone;
        private RunState _runState;

        public static ManageTeamScreen Create(Transform parent, Action onDone)
        {
            var screen = new ManageTeamScreen();
            screen._onDone = onDone;

            var canvas = UIFactory.CreateRootCanvas(parent);
            screen._root = UIFactory.CreatePanel("ManageTeamScreen", canvas.transform, Vector2.zero, Vector2.one);
            screen._content = screen._root.GetComponent<RectTransform>();
            screen._root.SetActive(false);
            return screen;
        }

        public void Show(RunState state)
        {
            _runState = state;
            _root.SetActive(true);
            Rebuild();
        }

        private void Rebuild()
        {
            Clear();

            // Title
            var title = UIFactory.CreateText("Title", _content, "Manage Team — Set Turn Order", 36);
            title.fontStyle = FontStyle.Bold;
            title.color = new Color(0.6f, 0.85f, 1f);
            SetRect(title.rectTransform, new Vector2(0f, 0.88f), new Vector2(1f, 0.97f));

            var desc = UIFactory.CreateText("Desc", _content,
                "Units act in this order during battle. Use arrows to reorder.", 20);
            desc.color = new Color(0.7f, 0.7f, 0.8f);
            SetRect(desc.rectTransform, new Vector2(0.1f, 0.82f), new Vector2(0.9f, 0.88f));

            var team = _runState.Team;
            int livingCount = 0;
            foreach (var u in team) { if (u.IsAlive) livingCount++; }

            float rowHeight = Mathf.Min(0.12f, 0.7f / Mathf.Max(livingCount, 1));
            float yTop = 0.78f;
            int displayIdx = 0;

            for (int i = 0; i < team.Count; i++)
            {
                var unit = team[i];
                if (!unit.IsAlive) continue;

                float yMax = yTop - displayIdx * (rowHeight + 0.01f);
                float yMin = yMax - rowHeight;

                // Order number
                var numText = UIFactory.CreateText($"Num_{i}", _content, $"#{displayIdx + 1}", 24);
                numText.color = new Color(1f, 0.9f, 0.5f);
                numText.fontStyle = FontStyle.Bold;
                SetRect(numText.rectTransform, new Vector2(0.05f, yMin), new Vector2(0.1f, yMax));

                // Unit info
                string passiveStr = unit.Passive != PassiveType.None ? $" [{unit.Passive}]" : "";
                string shieldStr = unit.Shield > 0 ? $" Shield:{unit.Shield}" : "";
                string attrStr = unit.BaseData != null && unit.BaseData.Attributes.Count > 0
                    ? $" ({string.Join(", ", unit.BaseData.Attributes)})"
                    : "";
                string info = $"{unit.DisplayName} R{unit.Rank} — {unit.BaseData?.Type} / {unit.BaseData?.Size}{attrStr}\n" +
                              $"HP:{unit.CurrentHP}/{unit.EffectiveMaxHP}  ATK:{unit.EffectiveAttackDamage}  CD:{unit.EffectiveCooldown}{shieldStr}{passiveStr}";

                var infoPanel = UIFactory.CreatePanel($"Info_{i}", _content, new Vector2(0.12f, yMin), new Vector2(0.75f, yMax));
                infoPanel.GetComponent<Image>().color = new Color(0.15f, 0.18f, 0.22f, 0.9f);
                var infoText = UIFactory.CreateText($"InfoText_{i}", infoPanel.transform, info, 17, TextAnchor.MiddleLeft);
                infoText.color = new Color(0.9f, 0.9f, 0.95f);
                var infoTextRt = infoText.rectTransform;
                infoTextRt.anchorMin = Vector2.zero;
                infoTextRt.anchorMax = Vector2.one;
                infoTextRt.offsetMin = new Vector2(10f, 0f);
                infoTextRt.offsetMax = new Vector2(-5f, 0f);

                // Up button
                int capturedI = i;
                if (displayIdx > 0)
                {
                    var upBtn = UIFactory.CreateButton($"Up_{i}", _content, "▲");
                    SetRect(upBtn.GetComponent<RectTransform>(), new Vector2(0.78f, yMin), new Vector2(0.86f, yMax));
                    var upLabel = upBtn.GetComponentInChildren<Text>();
                    if (upLabel != null) upLabel.fontSize = 22;
                    upBtn.onClick.AddListener(() =>
                    {
                        _runState.MoveUnitUp(capturedI);
                        Rebuild();
                    });
                }

                // Down button
                if (displayIdx < livingCount - 1)
                {
                    var downBtn = UIFactory.CreateButton($"Down_{i}", _content, "▼");
                    SetRect(downBtn.GetComponent<RectTransform>(), new Vector2(0.87f, yMin), new Vector2(0.95f, yMax));
                    var downLabel = downBtn.GetComponentInChildren<Text>();
                    if (downLabel != null) downLabel.fontSize = 22;
                    downBtn.onClick.AddListener(() =>
                    {
                        _runState.MoveUnitDown(capturedI);
                        Rebuild();
                    });
                }

                displayIdx++;
            }

            // Done button
            var doneBtn = UIFactory.CreateButton("Done", _content, "Done");
            SetRect(doneBtn.GetComponent<RectTransform>(), new Vector2(0.38f, 0.03f), new Vector2(0.62f, 0.1f));
            doneBtn.onClick.AddListener(() => _onDone?.Invoke());
        }

        public void Hide() => _root.SetActive(false);

        private void Clear()
        {
            for (int i = _content.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(_content.GetChild(i).gameObject);
        }

        private static void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}

