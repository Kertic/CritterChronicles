using AutobattlerSample.Battle;
using AutobattlerSample.Data;
using UnityEngine;
using UnityEngine.UI;

namespace AutobattlerSample.UI
{
    public class CombatLog
    {
        private readonly Text _logText;
        private readonly ScrollRect _scrollRect;
        private readonly Text _copyButtonLabel;
        private readonly GameObject _scrollBody;
        private readonly Text _toggleLabel;
        private readonly System.Text.StringBuilder _richSb = new();
        private readonly System.Text.StringBuilder _plainSb = new();
        private int _entryNumber;
        private bool _minimized;

        public static CombatLog Create(RectTransform parent)
        {
            // Background panel
            var panel = new GameObject("CombatLog", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            var panelImg = panel.GetComponent<Image>();
            panelImg.color = new Color(0.08f, 0.08f, 0.08f, 0.85f);

            var panelRt = panel.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.02f, 0.02f);
            panelRt.anchorMax = new Vector2(0.98f, 0.32f);
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;

            // Header bar
            var header = UIFactory.CreateText("LogHeader", panel.transform, "COMBAT LOG", 18, TextAnchor.MiddleLeft);
            header.color = new Color(1f, 0.85f, 0.4f);
            header.fontStyle = FontStyle.Bold;
            var headerRt = header.rectTransform;
            headerRt.anchorMin = new Vector2(0f, 1f);
            headerRt.anchorMax = new Vector2(0.6f, 1f);
            headerRt.offsetMin = new Vector2(10f, -28f);
            headerRt.offsetMax = new Vector2(0f, -2f);

            // Minimize/expand toggle button
            var toggleBtn = UIFactory.CreateButton("ToggleBtn", panel.transform, "");
            var toggleBtnRt = toggleBtn.GetComponent<RectTransform>();
            toggleBtnRt.anchorMin = new Vector2(1f, 1f);
            toggleBtnRt.anchorMax = new Vector2(1f, 1f);
            toggleBtnRt.pivot = new Vector2(1f, 1f);
            toggleBtnRt.sizeDelta = new Vector2(30f, 24f);
            toggleBtnRt.anchoredPosition = new Vector2(-125f, -3f);
            toggleBtn.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.25f, 1f);
            var toggleLabel = toggleBtn.GetComponentInChildren<Text>();
            toggleLabel.fontSize = 16;
            toggleLabel.text = "\u25BC"; // down arrow = minimized
            toggleLabel.color = new Color(0.8f, 0.8f, 0.8f);

            // Copy button
            var copyBtn = UIFactory.CreateButton("CopyBtn", panel.transform, "");
            var copyBtnRt = copyBtn.GetComponent<RectTransform>();
            copyBtnRt.anchorMin = new Vector2(1f, 1f);
            copyBtnRt.anchorMax = new Vector2(1f, 1f);
            copyBtnRt.pivot = new Vector2(1f, 1f);
            copyBtnRt.sizeDelta = new Vector2(110f, 24f);
            copyBtnRt.anchoredPosition = new Vector2(-8f, -3f);
            copyBtn.GetComponent<Image>().color = new Color(0.18f, 0.18f, 0.18f, 1f);

            var existingLabel = copyBtn.GetComponentInChildren<Text>();
            existingLabel.fontSize = 14;
            existingLabel.text = "\u2398  Copy Log";
            existingLabel.color = new Color(0.8f, 0.8f, 0.8f);
            var copyBtnLabelRef = existingLabel;

            // Scroll view
            var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect));
            scrollGo.transform.SetParent(panel.transform, false);
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = new Vector2(8f, 4f);
            scrollRt.offsetMax = new Vector2(-20f, -30f);

            // Viewport (mask)
            var maskGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            maskGo.transform.SetParent(scrollGo.transform, false);
            maskGo.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f);
            maskGo.GetComponent<Mask>().showMaskGraphic = false;
            var maskRt = maskGo.GetComponent<RectTransform>();
            maskRt.anchorMin = Vector2.zero;
            maskRt.anchorMax = Vector2.one;
            maskRt.offsetMin = Vector2.zero;
            maskRt.offsetMax = Vector2.zero;

            // Content container
            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            contentGo.transform.SetParent(maskGo.transform, false);
            var contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0f, 1f);
            contentRt.sizeDelta = new Vector2(0f, 0f);
            var layout = contentGo.GetComponent<VerticalLayoutGroup>();
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            var fitter = contentGo.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Log text
            var logText = UIFactory.CreateText("LogBody", contentGo.transform, "", 15, TextAnchor.UpperLeft);
            logText.color = new Color(0.85f, 0.85f, 0.85f);
            logText.horizontalOverflow = HorizontalWrapMode.Wrap;
            logText.verticalOverflow = VerticalWrapMode.Overflow;

            // Scrollbar
            var scrollbarGo = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            scrollbarGo.transform.SetParent(panel.transform, false);
            scrollbarGo.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 0.6f);
            var scrollbarRt = scrollbarGo.GetComponent<RectTransform>();
            scrollbarRt.anchorMin = new Vector2(1f, 0f);
            scrollbarRt.anchorMax = new Vector2(1f, 1f);
            scrollbarRt.pivot = new Vector2(1f, 0.5f);
            scrollbarRt.offsetMin = new Vector2(-14f, 4f);
            scrollbarRt.offsetMax = new Vector2(-2f, -30f);

            var slideArea = new GameObject("SlidingArea", typeof(RectTransform));
            slideArea.transform.SetParent(scrollbarGo.transform, false);
            var slideRt = slideArea.GetComponent<RectTransform>();
            slideRt.anchorMin = Vector2.zero;
            slideRt.anchorMax = Vector2.one;
            slideRt.offsetMin = Vector2.zero;
            slideRt.offsetMax = Vector2.zero;

            var handleGo = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handleGo.transform.SetParent(slideArea.transform, false);
            handleGo.GetComponent<Image>().color = new Color(0.45f, 0.45f, 0.45f, 0.8f);
            var handleRt = handleGo.GetComponent<RectTransform>();
            handleRt.anchorMin = Vector2.zero;
            handleRt.anchorMax = Vector2.one;
            handleRt.offsetMin = Vector2.zero;
            handleRt.offsetMax = Vector2.zero;

            var scrollbar = scrollbarGo.GetComponent<Scrollbar>();
            scrollbar.handleRect = handleRt;
            scrollbar.targetGraphic = handleGo.GetComponent<Image>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = maskRt;
            scroll.content = contentRt;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;

            var combatLog = new CombatLog(logText, scroll, copyBtnLabelRef, scrollGo, toggleLabel);

            // Hook up buttons
            copyBtn.onClick.AddListener(() => combatLog.CopyToClipboard());
            toggleBtn.onClick.AddListener(() => combatLog.ToggleMinimize());

            return combatLog;
        }

        private CombatLog(Text logText, ScrollRect scrollRect, Text copyButtonLabel,
            GameObject scrollBody, Text toggleLabel)
        {
            _logText = logText;
            _scrollRect = scrollRect;
            _copyButtonLabel = copyButtonLabel;
            _scrollBody = scrollBody;
            _toggleLabel = toggleLabel;
            _entryNumber = 0;
            _minimized = false;
        }

        public void ToggleMinimize()
        {
            _minimized = !_minimized;
            if (_scrollBody != null) _scrollBody.SetActive(!_minimized);
            if (_toggleLabel != null) _toggleLabel.text = _minimized ? "\u25B2" : "\u25BC";
        }

        public void AddEntry(TurnAction action)
        {
            _entryNumber++;

            string attackerName = action.Attacker.DisplayName;
            string turnTag = action.TurnNumber > 0 ? $"R{action.TurnNumber}" : $"#{_entryNumber}";

            // Cooldown skip
            if (action.WasOnCooldown)
            {
                string skipTag = action.Attacker.IsAlly
                    ? $"<color=#4499FF>{attackerName}</color>"
                    : $"<color=#FF4444>{attackerName}</color>";
                string rankTag = action.AttackerRank > 1 ? $" <color=#FFD700>[R{action.AttackerRank}]</color>" : "";
                _richSb.AppendLine($"<color=#FFDD66>{turnTag}</color>  {skipTag}{rankTag} is on cooldown (<color=#FF8888>{action.AttackerCooldownAfter} turns</color>)");
                _richSb.AppendLine();
                _plainSb.AppendLine($"{turnTag}  {attackerName} is on cooldown ({action.AttackerCooldownAfter} turns)");
                _plainSb.AppendLine();
                UpdateDisplay();
                return;
            }

            string attackerTag = action.Attacker.IsAlly
                ? $"<color=#4499FF>{attackerName}</color>"
                : $"<color=#FF4444>{attackerName}</color>";
            string rankInfo = action.AttackerRank > 1 ? $" <color=#FFD700>[R{action.AttackerRank}]</color>" : "";

            switch (action.UsedActionType)
            {
                case ActionType.Attack:
                {
                    string targetName = action.Target?.DisplayName ?? "?";
                    string targetTag = action.Target != null && action.Target.IsAlly
                        ? $"<color=#4499FF>{targetName}</color>"
                        : $"<color=#FF4444>{targetName}</color>";
                    _richSb.AppendLine($"<color=#FFDD66>{turnTag}</color>  {attackerTag}{rankInfo} attacks {targetTag}");
                    _richSb.Append($"    <color=#FFAA33>{action.DamageDealt}</color> dmg");
                    if (action.ShieldAbsorbed > 0)
                        _richSb.Append($"  (<color=#6699FF>{action.ShieldAbsorbed} absorbed</color>)");
                    _richSb.AppendLine();
                    _richSb.Append($"    HP  <color=#88FF88>{action.TargetHPBefore}</color> \u2192 <color=#88FF88>{action.TargetHPAfter}</color>");
                    if (action.KilledTarget)
                        _richSb.Append("  <color=#FF5555>\u2726 KILLED</color>");
                    _richSb.AppendLine();
                    if (action.LifestealHealed > 0)
                        _richSb.AppendLine($"    <color=#DD88FF>\u2665 Lifesteal: +{action.LifestealHealed} HP</color>");
                    _plainSb.AppendLine($"{turnTag}  {attackerName} attacks {targetName} for {action.DamageDealt} dmg");
                    break;
                }
                case ActionType.ShieldSelf:
                    _richSb.AppendLine($"<color=#FFDD66>{turnTag}</color>  {attackerTag}{rankInfo} uses <color=#6699FF>{action.ActionName}</color>");
                    _richSb.AppendLine($"    <color=#6699FF>+{action.ShieldGained} Shield</color>");
                    _plainSb.AppendLine($"{turnTag}  {attackerName} shields self (+{action.ShieldGained})");
                    break;
                case ActionType.HealSelf:
                    _richSb.AppendLine($"<color=#FFDD66>{turnTag}</color>  {attackerTag}{rankInfo} uses <color=#88FF88>{action.ActionName}</color>");
                    _richSb.AppendLine($"    <color=#88FF88>+{action.HealAmount} HP</color> (HP {action.TargetHPBefore} \u2192 {action.TargetHPAfter})");
                    _plainSb.AppendLine($"{turnTag}  {attackerName} heals self (+{action.HealAmount} HP)");
                    break;
                case ActionType.HealFront:
                {
                    string healTargetName = action.Target?.DisplayName ?? "?";
                    string healTargetTag = action.Target != null && action.Target.IsAlly
                        ? $"<color=#4499FF>{healTargetName}</color>"
                        : $"<color=#FF4444>{healTargetName}</color>";
                    _richSb.AppendLine($"<color=#FFDD66>{turnTag}</color>  {attackerTag}{rankInfo} uses <color=#88FF88>{action.ActionName}</color> on {healTargetTag}");
                    _richSb.AppendLine($"    <color=#88FF88>+{action.HealAmount} HP</color> (HP {action.TargetHPBefore} \u2192 {action.TargetHPAfter})");
                    _plainSb.AppendLine($"{turnTag}  {attackerName} heals {healTargetName} (+{action.HealAmount} HP)");
                    break;
                }
                case ActionType.HealAll:
                {
                    _richSb.AppendLine($"<color=#FFDD66>{turnTag}</color>  {attackerTag}{rankInfo} uses <color=#88FF88>{action.ActionName}</color> on all allies");
                    _richSb.AppendLine($"    <color=#88FF88>+{action.HealAmount} HP total</color>");
                    if (action.HealAllResults != null)
                    {
                        foreach (var (unit, healed) in action.HealAllResults)
                        {
                            string uTag = unit.IsAlly
                                ? $"<color=#4499FF>{unit.DisplayName}</color>"
                                : $"<color=#FF4444>{unit.DisplayName}</color>";
                            _richSb.AppendLine($"      {uTag} +{healed} HP");
                        }
                    }
                    _plainSb.AppendLine($"{turnTag}  {attackerName} heals all allies (+{action.HealAmount} HP total)");
                    break;
                }
            }

            _richSb.Append($"    CD: <color=#FF8888>{action.AttackerCooldownAfter}</color>");
            _richSb.AppendLine();

            // Log passive triggers
            if (action.LifestealTriggered)
            {
                _richSb.AppendLine($"    <color=#DD88FF>\u2665 Passive: Lifesteal healed +{action.LifestealHealed} HP</color>");
                _plainSb.AppendLine($"    Passive: Lifesteal healed +{action.LifestealHealed} HP");
            }
            if (action.HasteTriggered)
            {
                _richSb.AppendLine($"    <color=#FFBB44>\u26A1 Passive: HasteOnHeal on {action.HasteUnitName} — " +
                                   $"{action.HasteActionName} CD {action.HasteCooldownBefore} \u2192 {action.HasteCooldownAfter}</color>");
                _plainSb.AppendLine($"    Passive: HasteOnHeal on {action.HasteUnitName} — " +
                                    $"{action.HasteActionName} CD {action.HasteCooldownBefore} -> {action.HasteCooldownAfter}");
            }

            _richSb.AppendLine();
            _plainSb.AppendLine();

            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            _logText.text = _richSb.ToString();
            Canvas.ForceUpdateCanvases();
            _scrollRect.verticalNormalizedPosition = 0f;
        }

        public void CopyToClipboard()
        {
            string text = _plainSb.ToString();
            if (string.IsNullOrEmpty(text))
                text = "(Combat log is empty)";

            GUIUtility.systemCopyBuffer = text;

            if (_copyButtonLabel != null)
            {
                _copyButtonLabel.text = "\u2714  Copied!";
                _scrollRect.StartCoroutine(ResetCopyLabel());
            }
        }

        private System.Collections.IEnumerator ResetCopyLabel()
        {
            yield return new WaitForSeconds(1.5f);
            if (_copyButtonLabel != null)
                _copyButtonLabel.text = "\u2398  Copy Log";
        }

        public void Clear()
        {
            _richSb.Clear();
            _plainSb.Clear();
            _logText.text = "";
            _entryNumber = 0;
        }
    }
}

