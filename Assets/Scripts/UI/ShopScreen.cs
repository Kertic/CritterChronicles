using System;
using System.Collections.Generic;
using AutobattlerSample.Core;
using AutobattlerSample.Data;
using UnityEngine;
using UnityEngine.UI;

namespace AutobattlerSample.UI
{
    public class ShopScreen
    {
        private GameObject _root;
        private RectTransform _content;
        private Action _onComplete;
        private RunState _runState;

        public static ShopScreen Create(Transform parent, Action onComplete)
        {
            var screen = new ShopScreen();
            screen._onComplete = onComplete;

            var canvas = UIFactory.CreateRootCanvas(parent);
            screen._root = UIFactory.CreatePanel("ShopScreen", canvas.transform, Vector2.zero, Vector2.one);
            screen._content = screen._root.GetComponent<RectTransform>();
            screen._root.SetActive(false);
            return screen;
        }

        public void Show(RunState state, List<UnitData> unitOfferings, List<ItemData> itemOfferings)
        {
            _root.SetActive(true);
            _runState = state;
            Clear();

            // Title
            var title = UIFactory.CreateText("Title", _content, "Shop — Pick One!", 38);
            title.fontStyle = FontStyle.Bold;
            title.color = new Color(1f, 0.85f, 0.3f);
            SetRect(title.rectTransform, new Vector2(0f, 0.88f), new Vector2(1f, 0.97f));

            int totalOfferings = unitOfferings.Count + itemOfferings.Count;
            float cardWidth = 0.8f / Mathf.Max(totalOfferings, 1);
            int idx = 0;

            // Unit offerings
            foreach (var unitData in unitOfferings)
            {
                float xMin = 0.1f + idx * cardWidth;
                float xMax = xMin + cardWidth - 0.02f;

                // Check if player already owns this unit
                bool alreadyOwned = false;
                int currentRank = 0;
                foreach (var u in state.Team)
                {
                    if (u.BaseData != null && u.BaseData.UnitId == unitData.UnitId)
                    {
                        alreadyOwned = true;
                        currentRank = u.Rank;
                        break;
                    }
                }

                string actionLabel = alreadyOwned
                    ? $"UPGRADE\n{unitData.DisplayName}\nRank {currentRank} → {currentRank + 1}\n+{unitData.RankUpBonusHP} Max HP"
                    : $"RECRUIT\n{unitData.DisplayName}\n{unitData.Type} / {unitData.Size}\nHP:{unitData.MaxHP} ATK:{unitData.BaseAttackDamage} CD:{unitData.AttackCooldown}";

                var btn = UIFactory.CreateButton($"ShopUnit_{idx}", _content, actionLabel);
                SetRect(btn.GetComponent<RectTransform>(), new Vector2(xMin, 0.3f), new Vector2(xMax, 0.82f));

                var labelText = btn.GetComponentInChildren<Text>();
                if (labelText != null) labelText.fontSize = 20;

                btn.GetComponent<Image>().color = alreadyOwned
                    ? new Color(0.2f, 0.35f, 0.2f)
                    : new Color(0.15f, 0.25f, 0.4f);

                var capturedUnit = unitData;
                btn.onClick.AddListener(() =>
                {
                    bool upgraded = _runState.UpgradeOrAddUnit(capturedUnit);
                    string msg = upgraded
                        ? $"Upgraded {capturedUnit.DisplayName}!"
                        : $"Recruited {capturedUnit.DisplayName}!";
                    Debug.Log($"[Shop] {msg}");
                    _onComplete?.Invoke();
                });

                idx++;
            }

            // Item offerings
            foreach (var item in itemOfferings)
            {
                float xMin = 0.1f + idx * cardWidth;
                float xMax = xMin + cardWidth - 0.02f;

                string sign = item.Type == ItemType.CooldownReduction ? "-" : "+";
                string label = $"ITEM\n{item.Name}\n{sign}{item.Amount} {item.TypeName}";

                var btn = UIFactory.CreateButton($"ShopItem_{idx}", _content, label);
                SetRect(btn.GetComponent<RectTransform>(), new Vector2(xMin, 0.3f), new Vector2(xMax, 0.82f));

                var labelText = btn.GetComponentInChildren<Text>();
                if (labelText != null) labelText.fontSize = 20;

                Color bgColor;
                switch (item.Type)
                {
                    case ItemType.MaxHP: bgColor = new Color(0.15f, 0.3f, 0.15f); break;
                    case ItemType.CooldownReduction: bgColor = new Color(0.3f, 0.2f, 0.35f); break;
                    case ItemType.Shield: bgColor = new Color(0.15f, 0.2f, 0.35f); break;
                    default: bgColor = new Color(0.2f, 0.2f, 0.2f); break;
                }
                btn.GetComponent<Image>().color = bgColor;

                var capturedItem = item;
                btn.onClick.AddListener(() =>
                {
                    ShowItemTargetPhase(capturedItem);
                });

                idx++;
            }

            // Skip button
            var skip = UIFactory.CreateButton("Skip", _content, "Skip");
            SetRect(skip.GetComponent<RectTransform>(), new Vector2(0.38f, 0.1f), new Vector2(0.62f, 0.2f));
            skip.onClick.AddListener(() => _onComplete?.Invoke());
        }

        private void ShowItemTargetPhase(ItemData item)
        {
            Clear();

            string sign = item.Type == ItemType.CooldownReduction ? "-" : "+";
            var title = UIFactory.CreateText("Title", _content,
                $"Give \"{item.Name}\" ({sign}{item.Amount} {item.TypeName}) to which unit?", 28);
            title.color = new Color(1f, 0.85f, 0.3f);
            SetRect(title.rectTransform, new Vector2(0f, 0.85f), new Vector2(1f, 0.95f));

            int livingCount = 0;
            foreach (var u in _runState.Team) { if (u.IsAlive) livingCount++; }

            int idx = 0;
            foreach (var unit in _runState.Team)
            {
                if (!unit.IsAlive) continue;
                float xMin = 0.1f + idx * (0.8f / livingCount);
                float xMax = xMin + (0.75f / livingCount);

                string info = $"{unit.DisplayName} (R{unit.Rank})\nHP:{unit.CurrentHP}/{unit.EffectiveMaxHP}" +
                              $"\nATK:{unit.EffectiveAttackDamage} CD:{unit.EffectiveCooldown}" +
                              (unit.Shield > 0 ? $"\nShield:{unit.Shield}" : "");

                var btn = UIFactory.CreateButton($"Unit_{idx}", _content, info);
                SetRect(btn.GetComponent<RectTransform>(), new Vector2(xMin, 0.35f), new Vector2(xMax, 0.75f));
                var labelText = btn.GetComponentInChildren<Text>();
                if (labelText != null) labelText.fontSize = 18;

                var capturedUnit = unit;
                btn.onClick.AddListener(() =>
                {
                    item.ApplyTo(capturedUnit);
                    _runState.CollectedItems.Add(item);
                    Debug.Log($"[Shop] Applied {item.Name} to {capturedUnit.DisplayName}");
                    _onComplete?.Invoke();
                });
                idx++;
            }

            var desc = UIFactory.CreateText("Desc", _content, "Click a unit to apply the item", 22);
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

