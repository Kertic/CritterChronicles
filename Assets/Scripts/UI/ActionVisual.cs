using AutobattlerSample.Data;
using UnityEngine;
using UnityEngine.UI;

namespace AutobattlerSample.UI
{
    /// <summary>
    /// Visual representation of a single action with a WoW-style radial cooldown overlay.
    /// </summary>
    public class ActionVisual : MonoBehaviour
    {
        private Image _background;
        private Image _cooldownOverlay;
        private Text _cooldownText;
        private Text _labelText;
        private ActionInstance _action;

        public static ActionVisual Create(Transform parent, ActionInstance action, float size = 40f)
        {
            var go = new GameObject($"Action_{action.DisplayName}", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(size, size);

            // Background
            var bgGo = new GameObject("BG", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(go.transform, false);
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            var bgImg = bgGo.GetComponent<Image>();
            bgImg.color = GetActionColor(action.Type);
            bgImg.raycastTarget = false;

            // Radial cooldown overlay
            var overlayGo = new GameObject("CDOverlay", typeof(RectTransform), typeof(Image));
            overlayGo.transform.SetParent(go.transform, false);
            var overlayRt = overlayGo.GetComponent<RectTransform>();
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.offsetMin = Vector2.zero;
            overlayRt.offsetMax = Vector2.zero;
            var overlayImg = overlayGo.GetComponent<Image>();
            overlayImg.color = new Color(0, 0, 0, 0.65f);
            overlayImg.type = Image.Type.Filled;
            overlayImg.fillMethod = Image.FillMethod.Radial360;
            overlayImg.fillOrigin = (int)Image.Origin360.Top;
            overlayImg.fillClockwise = true;
            overlayImg.fillAmount = 0f;
            overlayImg.raycastTarget = false;

            // Cooldown number text
            var cdTextGo = new GameObject("CDText", typeof(RectTransform), typeof(Text));
            cdTextGo.transform.SetParent(go.transform, false);
            var cdTextRt = cdTextGo.GetComponent<RectTransform>();
            cdTextRt.anchorMin = Vector2.zero;
            cdTextRt.anchorMax = Vector2.one;
            cdTextRt.offsetMin = Vector2.zero;
            cdTextRt.offsetMax = Vector2.zero;
            var cdText = cdTextGo.GetComponent<Text>();
            cdText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            cdText.fontSize = (int)(size * 0.45f);
            cdText.alignment = TextAnchor.MiddleCenter;
            cdText.color = Color.white;
            cdText.fontStyle = FontStyle.Bold;
            cdText.raycastTarget = false;

            // Label under the icon
            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(go.transform, false);
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0f, 0f);
            labelRt.anchorMax = new Vector2(1f, 0f);
            labelRt.pivot = new Vector2(0.5f, 1f);
            labelRt.sizeDelta = new Vector2(size + 10f, 14f);
            labelRt.anchoredPosition = new Vector2(0f, -2f);
            var labelText = labelGo.GetComponent<Text>();
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 10;
            labelText.alignment = TextAnchor.UpperCenter;
            labelText.color = new Color(0.8f, 0.8f, 0.8f);
            labelText.text = GetActionAbbreviation(action.Type);
            labelText.raycastTarget = false;

            var visual = go.AddComponent<ActionVisual>();
            visual._background = bgImg;
            visual._cooldownOverlay = overlayImg;
            visual._cooldownText = cdText;
            visual._labelText = labelText;
            visual._action = action;
            visual.UpdateCooldown();

            return visual;
        }

        public void SetAction(ActionInstance action)
        {
            _action = action;
            if (_background != null)
                _background.color = GetActionColor(action.Type);
            if (_labelText != null)
                _labelText.text = GetActionAbbreviation(action.Type);
            UpdateCooldown();
        }

        public void UpdateCooldown()
        {
            if (_action == null) return;

            int current = _action.CurrentCooldown;
            int max = _action.MaxCooldown;

            if (current <= 0)
            {
                _cooldownOverlay.fillAmount = 0f;
                _cooldownText.text = "";
            }
            else
            {
                _cooldownOverlay.fillAmount = max > 0 ? (float)current / max : 1f;
                _cooldownText.text = current.ToString();
            }
        }

        private static Color GetActionColor(ActionType type)
        {
            switch (type)
            {
                case ActionType.Attack: return new Color(0.7f, 0.25f, 0.2f);
                case ActionType.ShieldSelf: return new Color(0.2f, 0.4f, 0.7f);
                case ActionType.HealSelf: return new Color(0.2f, 0.65f, 0.3f);
                case ActionType.HealFront: return new Color(0.3f, 0.7f, 0.5f);
                case ActionType.HealAll: return new Color(0.4f, 0.8f, 0.6f);
                default: return new Color(0.3f, 0.3f, 0.3f);
            }
        }

        private static string GetActionAbbreviation(ActionType type)
        {
            switch (type)
            {
                case ActionType.Attack: return "ATK";
                case ActionType.ShieldSelf: return "SHL";
                case ActionType.HealSelf: return "HEL";
                case ActionType.HealFront: return "HFR";
                case ActionType.HealAll: return "HAL";
                default: return "???";
            }
        }
    }
}

