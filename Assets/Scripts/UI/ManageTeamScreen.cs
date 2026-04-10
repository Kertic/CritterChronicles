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
        private UnitInstance _selectedUnit;

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
            _root.SetActive(true);
            Rebuild();
        }

        private void Rebuild()
        {
            Clear();

            var title = UIFactory.CreateText("Title", _content, "Manage Team - Formation & Actions", 32);
            title.fontStyle = FontStyle.Bold;
            title.color = new Color(0.6f, 0.85f, 1f);
            SetRect(title.rectTransform, new Vector2(0f, 0.92f), new Vector2(1f, 0.98f));

            var desc = UIFactory.CreateText("Desc", _content,
                $"Slots: {_runState.UsedSlots}/{RunState.MaxSlots}  |  Drag active critters to reorder. Drag camp critters into the team. Drag camp items onto a critter to equip them.",
                16);
            desc.color = new Color(0.7f, 0.7f, 0.8f);
            SetRect(desc.rectTransform, new Vector2(0.02f, 0.87f), new Vector2(0.98f, 0.92f));

            BuildActiveTeamSection();
            BuildSelectedUnitSection();
            BuildCampSection();
            BuildCampItemsSection();

            var doneBtn = UIFactory.CreateButton("Done", _content, "Done");
            SetRect(doneBtn.GetComponent<RectTransform>(), new Vector2(0.38f, 0.01f), new Vector2(0.62f, 0.07f));
            doneBtn.onClick.AddListener(() => _onDone?.Invoke());
        }

        private void BuildActiveTeamSection()
        {
            var teamLabel = UIFactory.CreateText("TeamLabel", _content, "ACTIVE TEAM", 20);
            teamLabel.color = new Color(0.4f, 0.8f, 0.4f);
            teamLabel.fontStyle = FontStyle.Bold;
            SetRect(teamLabel.rectTransform, new Vector2(0.02f, 0.81f), new Vector2(0.5f, 0.86f));

            var teamArea = UIFactory.CreatePanel("TeamArea", _content, new Vector2(0.02f, 0.36f), new Vector2(0.98f, 0.81f));
            teamArea.GetComponent<Image>().color = new Color(0.12f, 0.17f, 0.13f, 0.85f);
            var teamAreaDrop = teamArea.AddComponent<UIDropZone>();
            teamAreaDrop.Init(
                payload => payload.Type == DragPayloadType.CampUnit,
                payload =>
                {
                    var unit = (UnitInstance)payload.Value;
                    _runState.ActivateFromCamp(unit);
                    Rebuild();
                });

            var team = _runState.Team;
            float rowHeight = Mathf.Min(0.12f, 0.40f / Mathf.Max(team.Count, 1));
            float yTop = 0.96f;

            for (int i = 0; i < team.Count; i++)
            {
                var unit = team[i];
                float yMax = yTop - i * (rowHeight + 0.02f);
                float yMin = yMax - rowHeight;

                string posLabel = GetOrdinal(unit.Position);
                var numText = UIFactory.CreateText($"Num_{i}", teamArea.transform, posLabel, 16);
                numText.color = new Color(1f, 0.9f, 0.5f);
                numText.fontStyle = FontStyle.Bold;
                SetRect(numText.rectTransform, new Vector2(0.02f, yMin), new Vector2(0.12f, yMax));

                string sizeStr = unit.SlotCost > 1 ? $" [{unit.SlotCost} slots]" : "";
                string passiveStr = unit.Passive != PassiveType.None ? $" [{unit.Passive}]" : "";
                string actionsStr = string.Join(", ", unit.Actions
                    .OrderBy(a => a.Priority)
                    .Select(a => $"{GetActionOrderLabel(a.Priority)}: {a.Data.ShortLabel}"));
                string itemsStr = unit.EquippedItems.Count > 0
                    ? "  Items: " + string.Join(", ", unit.EquippedItems.Select(it => it.Name))
                    : "";

                string info = $"{unit.DisplayName} R{unit.Rank}{sizeStr} - HP:{unit.CurrentHP}/{unit.EffectiveMaxHP}{passiveStr}{itemsStr}\n{actionsStr}";

                bool isSelected = _selectedUnit == unit;
                var infoPanel = UIFactory.CreatePanel($"Info_{i}", teamArea.transform, new Vector2(0.13f, yMin), new Vector2(0.84f, yMax));
                infoPanel.GetComponent<Image>().color = isSelected
                    ? new Color(0.2f, 0.25f, 0.35f, 0.95f)
                    : new Color(0.15f, 0.18f, 0.22f, 0.9f);

                var infoText = UIFactory.CreateText($"InfoText_{i}", infoPanel.transform, info, 14, TextAnchor.MiddleLeft);
                infoText.color = new Color(0.9f, 0.9f, 0.95f);
                var infoTextRt = infoText.rectTransform;
                infoTextRt.anchorMin = Vector2.zero;
                infoTextRt.anchorMax = Vector2.one;
                infoTextRt.offsetMin = new Vector2(8f, 0f);
                infoTextRt.offsetMax = new Vector2(-8f, 0f);

                var capturedUnit = unit;
                var selectBtn = infoPanel.AddComponent<Button>();
                selectBtn.onClick.AddListener(() =>
                {
                    _selectedUnit = _selectedUnit == capturedUnit ? null : capturedUnit;
                    Rebuild();
                });

                var draggable = infoPanel.AddComponent<UIDraggable>();
                draggable.Init(new UIDragPayload { Type = DragPayloadType.TeamUnit, Value = unit }, _content);

                var infoDrop = infoPanel.AddComponent<UIDropZone>();
                int capturedIndex = i;
                infoDrop.Init(
                    payload => CanDropOnTeamUnit(payload, unit),
                    payload =>
                    {
                        if (payload.Type == DragPayloadType.TeamUnit)
                        {
                            var dragged = (UnitInstance)payload.Value;
                            if (dragged != unit)
                                _runState.MoveUnitToIndex(dragged, capturedIndex);
                        }
                        else if (payload.Type == DragPayloadType.CampUnit)
                        {
                            _runState.ActivateFromCampAtIndex((UnitInstance)payload.Value, capturedIndex);
                        }
                        else if (payload.Type == DragPayloadType.CampItem)
                        {
                            var item = (ItemData)payload.Value;
                            item.ApplyTo(unit);
                            _runState.CampItems.Remove(item);
                        }
                        else if (payload.Type == DragPayloadType.EquippedItem)
                        {
                            var (eqItem, owner) = ((ItemData, UnitInstance))payload.Value;
                            eqItem.UnapplyFrom(owner);
                            eqItem.ApplyTo(unit);
                        }
                        Rebuild();
                    });

                var campBtn = UIFactory.CreateButton($"Camp_{i}", teamArea.transform, "Send To Camp");
                SetRect(campBtn.GetComponent<RectTransform>(), new Vector2(0.85f, yMin), new Vector2(0.98f, yMax));
                campBtn.GetComponentInChildren<Text>().fontSize = 14;
                campBtn.GetComponent<Image>().color = new Color(0.4f, 0.25f, 0.15f);
                campBtn.onClick.AddListener(() =>
                {
                    _runState.SendToCamp(unit);
                    if (_selectedUnit == unit) _selectedUnit = null;
                    Rebuild();
                });
            }
        }

        private void BuildSelectedUnitSection()
        {
            if (_selectedUnit == null)
                return;

            bool hasActions = _selectedUnit.Actions.Count > 0;
            bool hasItems = _selectedUnit.EquippedItems.Count > 0;

            if (!hasActions && !hasItems)
                return;

            var section = UIFactory.CreatePanel("SelectedUnitSection", _content, new Vector2(0.02f, 0.16f), new Vector2(0.98f, 0.34f));
            section.GetComponent<Image>().color = new Color(0.14f, 0.14f, 0.18f, 0.9f);

            // Drop zone: accept camp items directly onto this section to equip
            var sectionDrop = section.AddComponent<UIDropZone>();
            sectionDrop.Init(
                payload => payload.Type == DragPayloadType.CampItem && payload.Value is ItemData,
                payload =>
                {
                    var item = (ItemData)payload.Value;
                    item.ApplyTo(_selectedUnit);
                    _runState.CampItems.Remove(item);
                    Rebuild();
                });

            var sectionTitle = UIFactory.CreateText("SectionTitle", section.transform,
                $"Details for {_selectedUnit.DisplayName}", 16);
            sectionTitle.color = new Color(0.9f, 0.8f, 0.4f);
            sectionTitle.fontStyle = FontStyle.Bold;
            SetRect(sectionTitle.rectTransform, new Vector2(0.02f, 0.88f), new Vector2(0.98f, 0.98f));

            // Layout: actions in upper portion, equipped items in lower portion
            float actionsTop, actionsBottom;
            float itemsLabelY, itemsCardTop, itemsCardBottom;

            if (hasActions && hasItems)
            {
                actionsTop = 0.86f;
                actionsBottom = 0.36f;
                itemsLabelY = 0.30f;
                itemsCardTop = 0.28f;
                itemsCardBottom = 0.02f;
            }
            else if (hasActions)
            {
                actionsTop = 0.86f;
                actionsBottom = 0.02f;
                itemsLabelY = 0f;
                itemsCardTop = 0f;
                itemsCardBottom = 0f;
            }
            else
            {
                actionsTop = 0f;
                actionsBottom = 0f;
                itemsLabelY = 0.82f;
                itemsCardTop = 0.78f;
                itemsCardBottom = 0.02f;
            }

            // --- Action Priority ---
            if (hasActions)
            {
                var actLabel = UIFactory.CreateText("ActLabel", section.transform, "ACTION PRIORITY", 13);
                actLabel.color = new Color(0.7f, 0.8f, 0.9f);
                actLabel.fontStyle = FontStyle.Bold;
                SetRect(actLabel.rectTransform, new Vector2(0.02f, actionsTop), new Vector2(0.5f, actionsTop + 0.10f));

                var sortedActions = _selectedUnit.Actions.OrderBy(a => a.Priority).ToList();
                float areaHeight = actionsTop - actionsBottom;
                float rowHeight = Mathf.Min(0.16f, areaHeight / Mathf.Max(sortedActions.Count, 1));

                for (int j = 0; j < sortedActions.Count; j++)
                {
                    var action = sortedActions[j];
                    float yMax = actionsTop - j * rowHeight;
                    float yMin = yMax - rowHeight + 0.01f;

                    string aLabel = $"{GetActionOrderLabel(j)} choice: {action.DisplayName} ({action.Type}, {action.Amount}, CD:{action.MaxCooldown})";
                    var aText = UIFactory.CreateText($"A_{j}", section.transform, aLabel, 13, TextAnchor.MiddleLeft);
                    aText.color = Color.white;
                    SetRect(aText.rectTransform, new Vector2(0.03f, yMin), new Vector2(0.68f, yMax));

                    if (j > 0)
                    {
                        var aUpBtn = UIFactory.CreateButton($"AUp_{j}", section.transform, "Earlier");
                        SetRect(aUpBtn.GetComponent<RectTransform>(), new Vector2(0.72f, yMin), new Vector2(0.84f, yMax));
                        aUpBtn.GetComponentInChildren<Text>().fontSize = 12;
                        var prev = sortedActions[j - 1];
                        aUpBtn.onClick.AddListener(() =>
                        {
                            (action.Priority, prev.Priority) = (prev.Priority, action.Priority);
                            Rebuild();
                        });
                    }

                    if (j < sortedActions.Count - 1)
                    {
                        var nextAction = sortedActions[j + 1];
                        var aDownBtn = UIFactory.CreateButton($"ADown_{j}", section.transform, "Later");
                        SetRect(aDownBtn.GetComponent<RectTransform>(), new Vector2(0.85f, yMin), new Vector2(0.97f, yMax));
                        aDownBtn.GetComponentInChildren<Text>().fontSize = 12;
                        aDownBtn.onClick.AddListener(() =>
                        {
                            (action.Priority, nextAction.Priority) = (nextAction.Priority, action.Priority);
                            Rebuild();
                        });
                    }
                }
            }

            // --- Equipped Items ---
            if (hasItems)
            {
                var itemLabel = UIFactory.CreateText("EqItemLabel", section.transform,
                    "EQUIPPED ITEMS  (drag to Camp Items to unequip)", 12);
                itemLabel.color = new Color(0.6f, 0.85f, 0.6f);
                itemLabel.fontStyle = FontStyle.Bold;
                SetRect(itemLabel.rectTransform, new Vector2(0.02f, itemsLabelY), new Vector2(0.98f, itemsLabelY + 0.08f));

                float itemW = 0.92f / Mathf.Max(_selectedUnit.EquippedItems.Count, 1);
                for (int i = 0; i < _selectedUnit.EquippedItems.Count; i++)
                {
                    var item = _selectedUnit.EquippedItems[i];
                    float xMin = 0.03f + i * itemW;
                    float xMax = Mathf.Min(0.97f, xMin + itemW - 0.01f);

                    var itemPanel = UIFactory.CreatePanel($"EqItem_{i}", section.transform,
                        new Vector2(xMin, itemsCardBottom), new Vector2(xMax, itemsCardTop));
                    itemPanel.GetComponent<Image>().color = new Color(0.2f, 0.28f, 0.2f, 0.95f);

                    string itemDesc;
                    if (item.Type == ItemType.ActionGrant)
                        itemDesc = $"{item.Name}\n{item.GrantedActionType}:{item.GrantedActionAmount}";
                    else
                        itemDesc = $"{item.Name}\n{item.TypeName}";

                    var itemText = UIFactory.CreateText($"EqItemText_{i}", itemPanel.transform, itemDesc, 11, TextAnchor.MiddleCenter);
                    itemText.color = new Color(0.85f, 0.95f, 0.85f);
                    var tRt = itemText.rectTransform;
                    tRt.anchorMin = new Vector2(0f, 0.28f);
                    tRt.anchorMax = Vector2.one;
                    tRt.offsetMin = new Vector2(2f, 0f);
                    tRt.offsetMax = new Vector2(-2f, 0f);

                    var capturedItem = item;
                    var capturedUnit = _selectedUnit;

                    // Unequip button at bottom of card
                    var unequipBtn = UIFactory.CreateButton($"Unequip_{i}", itemPanel.transform, "Unequip");
                    var ubRt = unequipBtn.GetComponent<RectTransform>();
                    ubRt.anchorMin = new Vector2(0.05f, 0.02f);
                    ubRt.anchorMax = new Vector2(0.95f, 0.26f);
                    ubRt.offsetMin = Vector2.zero;
                    ubRt.offsetMax = Vector2.zero;
                    unequipBtn.GetComponentInChildren<Text>().fontSize = 10;
                    unequipBtn.GetComponent<Image>().color = new Color(0.4f, 0.2f, 0.15f);
                    unequipBtn.onClick.AddListener(() =>
                    {
                        capturedItem.UnapplyFrom(capturedUnit);
                        _runState.CampItems.Add(capturedItem);
                        Rebuild();
                    });

                    // Draggable for unequipping via drag to camp items area
                    var draggable = itemPanel.AddComponent<UIDraggable>();
                    draggable.Init(new UIDragPayload
                    {
                        Type = DragPayloadType.EquippedItem,
                        Value = (capturedItem, capturedUnit)
                    }, _content);
                }
            }
        }

        private void BuildCampSection()
        {
            var campLabel = UIFactory.CreateText("CampLabel", _content, "CAMP CRITTERS", 18);
            campLabel.color = new Color(0.8f, 0.6f, 0.3f);
            campLabel.fontStyle = FontStyle.Bold;
            SetRect(campLabel.rectTransform, new Vector2(0.02f, 0.10f), new Vector2(0.48f, 0.15f));

            var campArea = UIFactory.CreatePanel("CampArea", _content, new Vector2(0.02f, 0.02f), new Vector2(0.48f, 0.10f));
            campArea.GetComponent<Image>().color = new Color(0.2f, 0.16f, 0.12f, 0.9f);
            var campDrop = campArea.AddComponent<UIDropZone>();
            campDrop.Init(
                payload => payload.Type == DragPayloadType.TeamUnit,
                payload =>
                {
                    var unit = (UnitInstance)payload.Value;
                    _runState.SendToCamp(unit);
                    if (_selectedUnit == unit) _selectedUnit = null;
                    Rebuild();
                });

            if (_runState.CampRoster.Count == 0)
            {
                var emptyText = UIFactory.CreateText("CampEmpty", campArea.transform, "Drag active critters here to send them to camp.", 13);
                emptyText.color = new Color(0.8f, 0.75f, 0.7f);
                SetRect(emptyText.rectTransform, new Vector2(0.02f, 0.1f), new Vector2(0.98f, 0.9f));
                return;
            }

            float width = 0.94f / Mathf.Max(_runState.CampRoster.Count, 1);
            for (int i = 0; i < _runState.CampRoster.Count; i++)
            {
                var campUnit = _runState.CampRoster[i];
                float xMin = 0.02f + i * width;
                float xMax = Mathf.Min(0.98f, xMin + width - 0.01f);
                string campInfo = $"{campUnit.DisplayName}\n{campUnit.BaseData?.Type}/{campUnit.BaseData?.Size}\nHP:{campUnit.CurrentHP}/{campUnit.EffectiveMaxHP}";

                var card = UIFactory.CreateButton($"CampUnit_{i}", campArea.transform, campInfo);
                SetRect(card.GetComponent<RectTransform>(), new Vector2(xMin, 0.08f), new Vector2(xMax, 0.92f));
                card.GetComponentInChildren<Text>().fontSize = 12;
                card.GetComponent<Image>().color = new Color(0.2f, 0.3f, 0.2f);

                var draggable = card.gameObject.AddComponent<UIDraggable>();
                draggable.Init(new UIDragPayload { Type = DragPayloadType.CampUnit, Value = campUnit }, _content);

                bool canActivate = _runState.UsedSlots + campUnit.SlotCost <= RunState.MaxSlots;
                card.interactable = canActivate;
                var capturedCamp = campUnit;
                card.onClick.AddListener(() =>
                {
                    if (_runState.ActivateFromCamp(capturedCamp))
                        Rebuild();
                });
            }
        }

        private void BuildCampItemsSection()
        {
            var campItemLabel = UIFactory.CreateText("CampItemLabel", _content, "CAMP ITEMS", 18);
            campItemLabel.color = new Color(0.6f, 0.85f, 0.6f);
            campItemLabel.fontStyle = FontStyle.Bold;
            SetRect(campItemLabel.rectTransform, new Vector2(0.52f, 0.10f), new Vector2(0.98f, 0.15f));

            var itemArea = UIFactory.CreatePanel("CampItemArea", _content, new Vector2(0.52f, 0.02f), new Vector2(0.98f, 0.10f));
            itemArea.GetComponent<Image>().color = new Color(0.15f, 0.18f, 0.12f, 0.9f);

            // Drop zone: accept equipped items dragged here to unequip them back to camp
            var itemAreaDrop = itemArea.AddComponent<UIDropZone>();
            itemAreaDrop.Init(
                payload => payload.Type == DragPayloadType.EquippedItem,
                payload =>
                {
                    var (item, owner) = ((ItemData, UnitInstance))payload.Value;
                    item.UnapplyFrom(owner);
                    _runState.CampItems.Add(item);
                    Rebuild();
                });

            if (_runState.CampItems.Count == 0)
            {
                var emptyText = UIFactory.CreateText("ItemEmpty", itemArea.transform,
                    "Drag items onto a critter to equip, or drag equipped items here to unequip.", 12);
                emptyText.color = new Color(0.75f, 0.85f, 0.75f);
                SetRect(emptyText.rectTransform, new Vector2(0.02f, 0.1f), new Vector2(0.98f, 0.9f));
                return;
            }

            float width = 0.94f / Mathf.Max(_runState.CampItems.Count, 1);
            for (int i = 0; i < _runState.CampItems.Count; i++)
            {
                var campItem = _runState.CampItems[i];
                float xMin = 0.02f + i * width;
                float xMax = Mathf.Min(0.98f, xMin + width - 0.01f);

                string itemLabel;
                if (campItem.Type == ItemType.ActionGrant)
                    itemLabel = $"{campItem.Name}\n{campItem.GrantedActionType}:{campItem.GrantedActionAmount}";
                else
                {
                    string sign = campItem.Type == ItemType.CooldownReduction ? "-" : "+";
                    itemLabel = $"{campItem.Name}\n{sign}{campItem.Amount} {campItem.TypeName}";
                }

                var itemBtn = UIFactory.CreateButton($"CampItem_{i}", itemArea.transform, itemLabel);
                SetRect(itemBtn.GetComponent<RectTransform>(), new Vector2(xMin, 0.08f), new Vector2(xMax, 0.92f));
                itemBtn.GetComponentInChildren<Text>().fontSize = 11;
                itemBtn.GetComponent<Image>().color = new Color(0.25f, 0.3f, 0.2f);

                var draggable = itemBtn.gameObject.AddComponent<UIDraggable>();
                draggable.Init(new UIDragPayload { Type = DragPayloadType.CampItem, Value = campItem }, _content);
            }
        }

        public void Hide() => _root.SetActive(false);

        private bool CanDropOnTeamUnit(UIDragPayload payload, UnitInstance targetUnit)
        {
            if (payload == null)
                return false;

            switch (payload.Type)
            {
                case DragPayloadType.TeamUnit:
                    return payload.Value is UnitInstance dragged && dragged != targetUnit;
                case DragPayloadType.CampUnit:
                    return payload.Value is UnitInstance campUnit &&
                           _runState.UsedSlots + campUnit.SlotCost <= RunState.MaxSlots;
                case DragPayloadType.CampItem:
                    return payload.Value is ItemData;
                case DragPayloadType.EquippedItem:
                    // Allow transferring equipped items to a different unit
                    if (payload.Value is (ItemData, UnitInstance owner))
                        return owner != targetUnit;
                    return false;
                default:
                    return false;
            }
        }

        private static string GetOrdinal(int index)
        {
            return index switch
            {
                0 => "First",
                1 => "Second",
                2 => "Third",
                3 => "Fourth",
                4 => "Fifth",
                5 => "Sixth",
                _ => $"{index + 1}th"
            };
        }

        private static string GetActionOrderLabel(int index)
        {
            return GetOrdinal(index);
        }

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
