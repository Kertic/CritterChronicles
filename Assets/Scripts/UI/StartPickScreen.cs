using System;
using System.Collections.Generic;
using AutobattlerSample.Core;
using AutobattlerSample.Data;
using UnityEngine;
using UnityEngine.UI;

namespace AutobattlerSample.UI
{
    public class StartPickScreen
    {
        private GameObject _root;
        private RectTransform _content;
        private Action<List<UnitData>> _onDone;
        private Action _onHelp;
        private List<UnitData> _picks;
        private readonly List<UnitData> _selected = new();
        private int _maxPicks;

        public static StartPickScreen Create(Transform parent, Action<List<UnitData>> onDone, Action onHelp = null)
        {
            var screen = new StartPickScreen();
            screen._onDone = onDone;
            screen._onHelp = onHelp;

            var canvas = UIFactory.CreateRootCanvas(parent);
            screen._root = UIFactory.CreatePanel("StartPickScreen", canvas.transform, Vector2.zero, Vector2.one);
            screen._content = screen._root.GetComponent<RectTransform>();
            screen._root.SetActive(false);
            return screen;
        }

        public void Show(List<UnitData> picks, int maxPicks = 3)
        {
            _picks = picks;
            _maxPicks = maxPicks;
            _selected.Clear();
            _root.SetActive(true);
            Rebuild();
        }

        private void Rebuild()
        {
            Clear();

            var title = UIFactory.CreateText("Title", _content,
                $"Choose Your Starting Critters ({_selected.Count}/{_maxPicks})", 36);
            title.fontStyle = FontStyle.Bold;
            title.color = new Color(1f, 0.85f, 0.3f);
            SetRect(title.rectTransform, new Vector2(0f, 0.88f), new Vector2(1f, 0.97f));

            var desc = UIFactory.CreateText("Desc", _content,
                "Click critters to select them for your team. Large creatures cost 2 slots.", 20);
            desc.color = new Color(0.7f, 0.7f, 0.8f);
            SetRect(desc.rectTransform, new Vector2(0.1f, 0.82f), new Vector2(0.9f, 0.88f));

            if (_onHelp != null)
            {
                var helpBtn = UIFactory.CreateButton("Help", _content, "?");
                var helpBtnRt = helpBtn.GetComponent<RectTransform>();
                helpBtnRt.anchorMin = new Vector2(1f, 1f);
                helpBtnRt.anchorMax = new Vector2(1f, 1f);
                helpBtnRt.pivot = new Vector2(1f, 1f);
                helpBtnRt.sizeDelta = new Vector2(45f, 45f);
                helpBtnRt.anchoredPosition = new Vector2(-20f, -10f);
                var helpLabel = helpBtn.GetComponentInChildren<Text>();
                if (helpLabel != null)
                {
                    helpLabel.fontSize = 26;
                    helpLabel.fontStyle = FontStyle.Bold;
                }
                var helpColors = helpBtn.colors;
                helpColors.normalColor = new Color(0.35f, 0.3f, 0.45f);
                helpColors.highlightedColor = new Color(0.45f, 0.4f, 0.55f);
                helpBtn.colors = helpColors;
                helpBtn.onClick.AddListener(() => _onHelp?.Invoke());
            }

            float cardWidth = 0.8f / Mathf.Max(_picks.Count, 1);
            for (int i = 0; i < _picks.Count; i++)
            {
                var unit = _picks[i];
                float xMin = 0.1f + i * cardWidth;
                float xMax = xMin + cardWidth - 0.02f;

                bool isSelected = _selected.Contains(unit);
                string sizeLabel = unit.Size == CreatureSize.Large ? " (2 slots)" : " (1 slot)";
                string selectedTag = isSelected ? "\n<color=#FFD700>SELECTED</color>" : "";
                string passiveStr = unit.Passive != PassiveType.None ? $"\n[{unit.Passive}]" : "";
                string label = $"{unit.DisplayName}\n{unit.Type} / {unit.Size}{sizeLabel}\n" +
                               $"HP:{unit.MaxHP}  ATK:{unit.BaseAttackDamage}  CD:{unit.AttackCooldown}{passiveStr}{selectedTag}";

                var btn = UIFactory.CreateButton($"Pick_{i}", _content, label);
                SetRect(btn.GetComponent<RectTransform>(), new Vector2(xMin, 0.3f), new Vector2(xMax, 0.8f));

                var labelText = btn.GetComponentInChildren<Text>();
                if (labelText != null) labelText.fontSize = 18;

                btn.GetComponent<Image>().color = isSelected
                    ? new Color(0.2f, 0.4f, 0.2f)
                    : new Color(0.15f, 0.2f, 0.3f);

                var capturedUnit = unit;
                btn.onClick.AddListener(() =>
                {
                    if (_selected.Contains(capturedUnit))
                        _selected.Remove(capturedUnit);
                    else if (_selected.Count < _maxPicks)
                        _selected.Add(capturedUnit);
                    Rebuild();
                });
            }

            // Confirm button
            bool canConfirm = _selected.Count > 0;
            var confirmBtn = UIFactory.CreateButton("Confirm", _content,
                canConfirm ? $"Start Run ({_selected.Count} critters)" : "Select at least 1 critter");
            SetRect(confirmBtn.GetComponent<RectTransform>(), new Vector2(0.3f, 0.1f), new Vector2(0.7f, 0.2f));
            confirmBtn.interactable = canConfirm;
            if (canConfirm)
            {
                confirmBtn.onClick.AddListener(() =>
                {
                    _root.SetActive(false);
                    _onDone?.Invoke(new List<UnitData>(_selected));
                });
            }
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

