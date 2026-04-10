using System;
using System.Collections.Generic;
using System.Linq;
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
        private UnitInstance _selectedUnit; // for action priority editing
        private ItemData _equipItem; // item being equipped from camp

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
            _selectedUnit = null;
            _equipItem = null;
            _root.SetActive(true);
            Rebuild();
        }

        private void Rebuild()
        {
            Clear();

            // Title
            var title = UIFactory.CreateText("Title", _content, "Manage Team — Formation & Actions", 32);
            title.fontStyle = FontStyle.Bold;
            title.color = new Color(0.6f, 0.85f, 1f);
            SetRect(title.rectTransform, new Vector2(0f, 0.91f), new Vector2(1f, 0.98f));

            var desc = UIFactory.CreateText("Desc", _content,
                $"Slots: {_runState.UsedSlots}/{RunState.MaxSlots}  |  Front (pos 0) = fights first. Use arrows to set formation. Click a unit to edit action priority.", 16);
            desc.color = new Color(0.7f, 0.7f, 0.8f);
            SetRect(desc.rectTransform, new Vector2(0.02f, 0.86f), new Vector2(0.98f, 0.91f));

            // Active team section
            var teamLabel = UIFactory.CreateText("TeamLabel", _content, "ACTIVE TEAM", 20);
            teamLabel.color = new Color(0.4f, 0.8f, 0.4f);
            teamLabel.fontStyle = FontStyle.Bold;
            SetRect(teamLabel.rectTransform, new Vector2(0.02f, 0.81f), new Vector2(0.5f, 0.86f));

            var team = _runState.Team;
            float rowHeight = Mathf.Min(0.09f, 0.5f / Mathf.Max(team.Count, 1));
            float yTop = 0.80f;

            for (int i = 0; i < team.Count; i++)
            {
                var unit = team[i];
                float yMax = yTop - i * (rowHeight + 0.005f);
                float yMin = yMax - rowHeight;

                // Position number
                string posLabel = $"Pos {unit.Position}";
                var numText = UIFactory.CreateText($"Num_{i}", _content, posLabel, 16);
                numText.color = new Color(1f, 0.9f, 0.5f);
                numText.fontStyle = FontStyle.Bold;
                SetRect(numText.rectTransform, new Vector2(0.02f, yMin), new Vector2(0.08f, yMax));

                // Unit info
                string sizeStr = unit.SlotCost > 1 ? $" [Large={unit.SlotCost}slots]" : "";
                string passiveStr = unit.Passive != PassiveType.None ? $" [{unit.Passive}]" : "";
                string actionsStr = "";
                foreach (var a in unit.Actions.OrderBy(a => a.Priority))
                    actionsStr += $" {a.Data.ShortLabel}(CD:{a.MaxCooldown})";

                string info = $"{unit.DisplayName} R{unit.Rank}{sizeStr} — HP:{unit.CurrentHP}/{unit.EffectiveMaxHP}{passiveStr}\n" +
                              $"Actions:{actionsStr}";

                bool isSelected = _selectedUnit == unit;
                var infoPanel = UIFactory.CreatePanel($"Info_{i}", _content, new Vector2(0.09f, yMin), new Vector2(0.72f, yMax));
                infoPanel.GetComponent<Image>().color = isSelected
                    ? new Color(0.2f, 0.25f, 0.35f, 0.95f)
                    : new Color(0.15f, 0.18f, 0.22f, 0.9f);
                var infoText = UIFactory.CreateText($"InfoText_{i}", infoPanel.transform, info, 14, TextAnchor.MiddleLeft);
                infoText.color = new Color(0.9f, 0.9f, 0.95f);
                var infoTextRt = infoText.rectTransform;
                infoTextRt.anchorMin = Vector2.zero;
                infoTextRt.anchorMax = Vector2.one;
                infoTextRt.offsetMin = new Vector2(8f, 0f);
                infoTextRt.offsetMax = new Vector2(-5f, 0f);

                // Click to select for action editing
                var capturedUnit = unit;
                var selectBtn = infoPanel.AddComponent<Button>();
                selectBtn.onClick.AddListener(() =>
                {
                    _selectedUnit = _selectedUnit == capturedUnit ? null : capturedUnit;
                    Rebuild();
                });

                // Up/Down arrows
                int capturedI = i;
                if (i > 0)
                {
                    var upBtn = UIFactory.CreateButton($"Up_{i}", _content, "\u25B2");
                    SetRect(upBtn.GetComponent<RectTransform>(), new Vector2(0.73f, yMin), new Vector2(0.78f, yMax));
                    var upLabel = upBtn.GetComponentInChildren<Text>();
                    if (upLabel != null) upLabel.fontSize = 18;
                    upBtn.onClick.AddListener(() =>
                    {
                        _runState.MoveUnitUp(capturedI);
                        Rebuild();
                    });
                }

                if (i < team.Count - 1)
                {
                    var downBtn = UIFactory.CreateButton($"Down_{i}", _content, "\u25BC");
                    SetRect(downBtn.GetComponent<RectTransform>(), new Vector2(0.79f, yMin), new Vector2(0.84f, yMax));
                    var downLabel = downBtn.GetComponentInChildren<Text>();
                    if (downLabel != null) downLabel.fontSize = 18;
                    downBtn.onClick.AddListener(() =>
                    {
                        _runState.MoveUnitDown(capturedI);
                        Rebuild();
                    });
                }

                // Send to camp button
                var campBtn = UIFactory.CreateButton($"Camp_{i}", _content, "Camp");
                SetRect(campBtn.GetComponent<RectTransform>(), new Vector2(0.85f, yMin), new Vector2(0.98f, yMax));
                campBtn.GetComponentInChildren<Text>().fontSize = 14;
                campBtn.GetComponent<Image>().color = new Color(0.4f, 0.25f, 0.15f);
                var campCaptured = unit;
                campBtn.onClick.AddListener(() =>
                {
                    _runState.SendToCamp(campCaptured);
                    if (_selectedUnit == campCaptured) _selectedUnit = null;
                    Rebuild();
                });
            }

            // Action priority editor for selected unit
            if (_selectedUnit != null && _selectedUnit.Actions.Count > 1)
            {
                float actionY = yTop - team.Count * (rowHeight + 0.005f) - 0.02f;

                var actionTitle = UIFactory.CreateText("ActionTitle", _content,
                    $"Action Priority for {_selectedUnit.DisplayName} (lower = used first)", 16);
                actionTitle.color = new Color(0.9f, 0.8f, 0.4f);
                SetRect(actionTitle.rectTransform, new Vector2(0.02f, actionY - 0.03f), new Vector2(0.98f, actionY));

                var sortedActions = _selectedUnit.Actions.OrderBy(a => a.Priority).ToList();
                for (int j = 0; j < sortedActions.Count; j++)
                {
                    var action = sortedActions[j];
                    float ay = actionY - 0.04f - j * 0.035f;
                    string aLabel = $"P{action.Priority}: {action.DisplayName} ({action.Type}, {action.Amount}, CD:{action.MaxCooldown})";
                    var aText = UIFactory.CreateText($"A_{j}", _content, aLabel, 14, TextAnchor.MiddleLeft);
                    aText.color = Color.white;
                    SetRect(aText.rectTransform, new Vector2(0.1f, ay - 0.03f), new Vector2(0.7f, ay));

                    int capturedJ = j;
                    if (j > 0)
                    {
                        var aUpBtn = UIFactory.CreateButton($"AUp_{j}", _content, "\u25B2");
                        SetRect(aUpBtn.GetComponent<RectTransform>(), new Vector2(0.72f, ay - 0.03f), new Vector2(0.78f, ay));
                        aUpBtn.GetComponentInChildren<Text>().fontSize = 14;
                        aUpBtn.onClick.AddListener(() =>
                        {
                            // Swap priorities
                            var prev = sortedActions[capturedJ - 1];
                            (action.Priority, prev.Priority) = (prev.Priority, action.Priority);
                            Rebuild();
                        });
                    }
                    if (j < sortedActions.Count - 1)
                    {
                        var aDownBtn = UIFactory.CreateButton($"ADown_{j}", _content, "\u25BC");
                        SetRect(aDownBtn.GetComponent<RectTransform>(), new Vector2(0.79f, ay - 0.03f), new Vector2(0.85f, ay));
                        aDownBtn.GetComponentInChildren<Text>().fontSize = 14;
                        var nextAction = sortedActions[j + 1];
                        aDownBtn.onClick.AddListener(() =>
                        {
                            (action.Priority, nextAction.Priority) = (nextAction.Priority, action.Priority);
                            Rebuild();
                        });
                    }
                }

                // Equipped items for selected unit
                if (_selectedUnit.EquippedItems.Count > 0)
                {
                    float itemY = actionY - 0.04f - sortedActions.Count * 0.035f - 0.02f;
                    var itemTitle = UIFactory.CreateText("ItemTitle", _content,
                        $"Equipped Items on {_selectedUnit.DisplayName}", 16);
                    itemTitle.color = new Color(0.6f, 0.85f, 0.6f);
                    SetRect(itemTitle.rectTransform, new Vector2(0.02f, itemY - 0.03f), new Vector2(0.98f, itemY));

                    for (int k = 0; k < _selectedUnit.EquippedItems.Count; k++)
                    {
                        var item = _selectedUnit.EquippedItems[k];
                        float iy = itemY - 0.04f - k * 0.035f;
                        string itemLabel = $"{item.Name} ({item.TypeName})";
                        var iText = UIFactory.CreateText($"Item_{k}", _content, itemLabel, 14, TextAnchor.MiddleLeft);
                        iText.color = new Color(0.8f, 0.9f, 0.8f);
                        SetRect(iText.rectTransform, new Vector2(0.1f, iy - 0.03f), new Vector2(0.7f, iy));

                        var capturedItem = item;
                        var capturedUnit2 = _selectedUnit;
                        var unequipBtn = UIFactory.CreateButton($"Unequip_{k}", _content, "Unequip");
                        SetRect(unequipBtn.GetComponent<RectTransform>(), new Vector2(0.72f, iy - 0.03f), new Vector2(0.85f, iy));
                        unequipBtn.GetComponentInChildren<Text>().fontSize = 12;
                        unequipBtn.GetComponent<Image>().color = new Color(0.5f, 0.3f, 0.2f);
                        unequipBtn.onClick.AddListener(() =>
                        {
                            capturedItem.UnapplyFrom(capturedUnit2);
                            _runState.CampItems.Add(capturedItem);
                            Rebuild();
                        });
                    }
                }
            }

            // Camp roster section
            if (_runState.CampRoster.Count > 0)
            {
                var campLabel = UIFactory.CreateText("CampLabel", _content, "AT CAMP", 18);
                campLabel.color = new Color(0.8f, 0.6f, 0.3f);
                campLabel.fontStyle = FontStyle.Bold;
                SetRect(campLabel.rectTransform, new Vector2(0.02f, 0.12f), new Vector2(0.5f, 0.17f));

                for (int c = 0; c < _runState.CampRoster.Count; c++)
                {
                    var campUnit = _runState.CampRoster[c];
                    float cx = 0.05f + c * 0.2f;
                    string campInfo = $"{campUnit.DisplayName}\n{campUnit.BaseData?.Type}/{campUnit.BaseData?.Size}\nHP:{campUnit.CurrentHP}/{campUnit.EffectiveMaxHP}";

                    var activateBtn = UIFactory.CreateButton($"Activate_{c}", _content, $"Activate\n{campInfo}");
                    SetRect(activateBtn.GetComponent<RectTransform>(), new Vector2(cx, 0.03f), new Vector2(cx + 0.18f, 0.12f));
                    activateBtn.GetComponentInChildren<Text>().fontSize = 12;
                    activateBtn.GetComponent<Image>().color = new Color(0.2f, 0.3f, 0.2f);

                    bool canActivate = _runState.UsedSlots + campUnit.SlotCost <= RunState.MaxSlots;
                    activateBtn.interactable = canActivate;

                    var capturedCamp = campUnit;
                    activateBtn.onClick.AddListener(() =>
                    {
                        _runState.ActivateFromCamp(capturedCamp);
                        Rebuild();
                    });
                }
            }

            // Camp Items section
            if (_runState.CampItems.Count > 0)
            {
                var campItemLabel = UIFactory.CreateText("CampItemLabel", _content, "CAMP ITEMS (click to equip)", 16);
                campItemLabel.color = new Color(0.6f, 0.85f, 0.6f);
                campItemLabel.fontStyle = FontStyle.Bold;
                SetRect(campItemLabel.rectTransform, new Vector2(0.52f, 0.12f), new Vector2(0.98f, 0.17f));

                for (int ci = 0; ci < _runState.CampItems.Count; ci++)
                {
                    var campItem = _runState.CampItems[ci];
                    float cx = 0.52f + ci * 0.12f;
                    if (cx + 0.11f > 0.98f) break; // limit display

                    bool isEquipping = _equipItem == campItem;
                    string ciLabel = isEquipping
                        ? $"EQUIP\n{campItem.Name}"
                        : campItem.Name;

                    var ciBtn = UIFactory.CreateButton($"CampItem_{ci}", _content, ciLabel);
                    SetRect(ciBtn.GetComponent<RectTransform>(), new Vector2(cx, 0.03f), new Vector2(cx + 0.11f, 0.12f));
                    ciBtn.GetComponentInChildren<Text>().fontSize = 11;
                    ciBtn.GetComponent<Image>().color = isEquipping
                        ? new Color(0.2f, 0.4f, 0.2f)
                        : new Color(0.25f, 0.3f, 0.2f);

                    var capturedCampItem = campItem;
                    ciBtn.onClick.AddListener(() =>
                    {
                        _equipItem = _equipItem == capturedCampItem ? null : capturedCampItem;
                        Rebuild();
                    });
                }
            }

            // Equip target picker
            if (_equipItem != null)
            {
                var equipTitle = UIFactory.CreateText("EquipTitle", _content,
                    $"Select a unit to equip \"{_equipItem.Name}\"", 16);
                equipTitle.color = new Color(1f, 0.9f, 0.5f);
                SetRect(equipTitle.rectTransform, new Vector2(0.02f, 0.23f), new Vector2(0.98f, 0.27f));

                var allUnits = _runState.Team.Concat(_runState.CampRoster).Where(u => u.IsAlive).ToList();
                for (int eu = 0; eu < allUnits.Count; eu++)
                {
                    var eqUnit = allUnits[eu];
                    float ex = 0.05f + eu * (0.88f / allUnits.Count);
                    float ew = 0.85f / allUnits.Count;

                    var eqBtn = UIFactory.CreateButton($"EquipTo_{eu}", _content, eqUnit.DisplayName);
                    SetRect(eqBtn.GetComponent<RectTransform>(), new Vector2(ex, 0.27f), new Vector2(ex + ew, 0.33f));
                    eqBtn.GetComponentInChildren<Text>().fontSize = 13;
                    eqBtn.GetComponent<Image>().color = new Color(0.2f, 0.3f, 0.2f);

                    var capturedEqUnit = eqUnit;
                    eqBtn.onClick.AddListener(() =>
                    {
                        _equipItem.ApplyTo(capturedEqUnit);
                        _runState.CampItems.Remove(_equipItem);
                        _equipItem = null;
                        Rebuild();
                    });
                }
            }

            // Done button
            var doneBtn = UIFactory.CreateButton("Done", _content, "Done");
            SetRect(doneBtn.GetComponent<RectTransform>(), new Vector2(0.38f, 0.17f), new Vector2(0.62f, 0.23f));
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

