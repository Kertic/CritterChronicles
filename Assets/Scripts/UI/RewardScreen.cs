using System;
using System.Collections.Generic;
using AutobattlerSample.Data;
using UnityEngine;
using UnityEngine.UI;

namespace AutobattlerSample.UI
{
    public class RewardScreen
    {
        private GameObject _root;
        private RectTransform _content;
        private Action<ItemData, UnitInstance> _onComplete;
        private ItemData _selectedItem;
        private List<UnitInstance> _team;

        public static RewardScreen Create(Transform parent, Action<ItemData, UnitInstance> onComplete)
        {
            var screen = new RewardScreen();
            screen._onComplete = onComplete;

            var canvas = UIFactory.CreateRootCanvas(parent);
            screen._root = UIFactory.CreatePanel("RewardScreen", canvas.transform, Vector2.zero, Vector2.one);
            screen._content = screen._root.GetComponent<RectTransform>();
            screen._root.SetActive(false);
            return screen;
        }

        public void Show(List<ItemData> items, List<UnitInstance> team)
        {
            _root.SetActive(true);
            _team = team;
            _selectedItem = null;
            ShowItemPhase(items);
        }

        private void ShowItemPhase(List<ItemData> items)
        {
            Clear();

            var title = UIFactory.CreateText("Title", _content, "Choose a Reward Item", 38);
            title.fontStyle = FontStyle.Bold;
            title.color = new Color(1f, 0.85f, 0.3f);
            SetRect(title.rectTransform, new Vector2(0f, 0.85f), new Vector2(1f, 0.95f));

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                string statColor = item.Stat == StatType.HP ? "#66ff66" :
                                   item.Stat == StatType.Armor ? "#6699ff" : "#ff6666";

                var button = UIFactory.CreateButton($"Item_{i}", _content,
                    $"{item.Name}\n<color={statColor}>+{item.Amount} {item.StatName}</color>");
                var rt = button.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.1f + i * 0.28f, 0.4f);
                rt.anchorMax = new Vector2(0.3f + i * 0.28f, 0.7f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                var labelText = button.GetComponentInChildren<Text>();
                if (labelText != null) labelText.fontSize = 24;

                // Color based on stat type
                var img = button.GetComponent<Image>();
                switch (item.Stat)
                {
                    case StatType.HP:
                        img.color = new Color(0.15f, 0.3f, 0.15f);
                        break;
                    case StatType.Armor:
                        img.color = new Color(0.15f, 0.2f, 0.35f);
                        break;
                    case StatType.AttackDamage:
                        img.color = new Color(0.35f, 0.15f, 0.15f);
                        break;
                }

                var capturedItem = item;
                button.onClick.AddListener(() =>
                {
                    _selectedItem = capturedItem;
                    ShowUnitPhase();
                });
            }

            // Skip button
            var skip = UIFactory.CreateButton("Skip", _content, "Skip Reward");
            SetRect(skip.GetComponent<RectTransform>(), new Vector2(0.38f, 0.2f), new Vector2(0.62f, 0.3f));
            skip.onClick.AddListener(() => _onComplete?.Invoke(null, null));
        }

        private void ShowUnitPhase()
        {
            Clear();

            var title = UIFactory.CreateText("Title", _content,
                $"Give \"{_selectedItem.Name}\" (+{_selectedItem.Amount} {_selectedItem.StatName}) to which unit?", 30);
            title.color = new Color(1f, 0.85f, 0.3f);
            SetRect(title.rectTransform, new Vector2(0f, 0.82f), new Vector2(1f, 0.95f));

            int livingCount = 0;
            foreach (var u in _team) { if (u.IsAlive) livingCount++; }

            int idx = 0;
            foreach (var unit in _team)
            {
                if (!unit.IsAlive) continue;

                float xMin = 0.1f + idx * (0.8f / livingCount);
                float xMax = xMin + (0.75f / livingCount);

                string info = $"{unit.DisplayName}\nHP:{unit.CurrentHP}/{unit.EffectiveMaxHP}\n" +
                              $"ARM:{unit.EffectiveArmor}  ATK:{unit.EffectiveAttack}";
                var button = UIFactory.CreateButton($"Unit_{idx}", _content, info);
                SetRect(button.GetComponent<RectTransform>(), new Vector2(xMin, 0.35f), new Vector2(xMax, 0.7f));

                var labelText = button.GetComponentInChildren<Text>();
                if (labelText != null) labelText.fontSize = 20;

                var capturedUnit = unit;
                button.onClick.AddListener(() =>
                {
                    _selectedItem.ApplyTo(capturedUnit);
                    _onComplete?.Invoke(_selectedItem, capturedUnit);
                });
                idx++;
            }

            var desc = UIFactory.CreateText("Desc", _content,
                "Click a unit to apply the item", 22);
            desc.color = new Color(0.7f, 0.7f, 0.8f);
            SetRect(desc.rectTransform, new Vector2(0.2f, 0.25f), new Vector2(0.8f, 0.33f));
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
