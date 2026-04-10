using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AutobattlerSample.Battle;
using AutobattlerSample.Data;
using UnityEngine;
using UnityEngine.UI;

namespace AutobattlerSample.UI
{
    public class UnitVisual : MonoBehaviour
    {
        public BattleUnit Unit { get; private set; }
        private Text _statText;
        private Image _shapeImage;
        private Image _hpBarFill;
        private RectTransform _rt;
        private Vector2 _originalPos;
        private Color _originalColor;
        private readonly List<ActionVisual> _actionVisuals = new();

        public void Init(BattleUnit unit, Image shape, Text statLabel, Image hpBarFill = null)
        {
            Unit = unit;
            _shapeImage = shape;
            _statText = statLabel;
            _hpBarFill = hpBarFill;
            _rt = GetComponent<RectTransform>();
            _originalPos = _rt.anchoredPosition;
            _originalColor = shape.color;

            // Create action visuals below the unit
            CreateActionIcons();
            UpdateStats();
        }

        private void CreateActionIcons()
        {
            if (Unit == null || Unit.Actions.Count == 0) return;

            var containerGo = new GameObject("Actions", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            containerGo.transform.SetParent(transform, false);
            var containerRt = containerGo.GetComponent<RectTransform>();
            containerRt.anchorMin = new Vector2(0.5f, 0.5f);
            containerRt.anchorMax = new Vector2(0.5f, 0.5f);
            containerRt.pivot = new Vector2(0.5f, 1f);
            containerRt.anchoredPosition = new Vector2(0f, -50f);
            float totalWidth = Unit.Actions.Count * 44f;
            containerRt.sizeDelta = new Vector2(totalWidth, 40f);

            var layout = containerGo.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 4f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = false;
            layout.childControlHeight = false;

            foreach (var action in Unit.Actions.OrderBy(a => a.Priority))
            {
                var actionVisual = ActionVisual.Create(containerGo.transform, action, 36f);
                _actionVisuals.Add(actionVisual);
            }
        }

        public void UpdateStats()
        {
            if (Unit == null) return;
            if (!Unit.IsAlive)
            {
                _statText.text = $"{Unit.DisplayName}\n<color=red>DEAD</color>";
                _shapeImage.color = new Color(_originalColor.r * 0.3f, _originalColor.g * 0.3f, _originalColor.b * 0.3f, 0.35f);
                SetHPBarWidth(0f);
                UpdateActionVisuals();
                return;
            }

            string rankStr = Unit.Rank > 1 ? $" <color=#FFD700>R{Unit.Rank}</color>" : "";
            string shieldStr = Unit.Shield > 0 ? $"  <color=#6699FF>Sh:{Unit.Shield}</color>" : "";
            string passiveStr = Unit.Passive != PassiveType.None
                ? $"\n<color=#DD88FF>{Unit.Passive}</color>"
                : "";

            _statText.text = $"{Unit.DisplayName}{rankStr}\n" +
                             $"HP:{Unit.CurrentHP}/{Unit.MaxHP}{shieldStr}{passiveStr}";

            if (_hpBarFill != null)
            {
                float ratio = Unit.MaxHP > 0 ? (float)Unit.CurrentHP / Unit.MaxHP : 0f;
                SetHPBarWidth(ratio);
                _hpBarFill.color = new Color(0.2f, 0.85f, 0.25f);
            }

            UpdateActionVisuals();
        }

        private void UpdateActionVisuals()
        {
            for (int i = 0; i < _actionVisuals.Count; i++)
            {
                if (i < Unit.Actions.Count)
                    _actionVisuals[i].UpdateCooldown();
            }
        }

        private void SetHPBarWidth(float ratio)
        {
            if (_hpBarFill == null) return;
            var fillRt = _hpBarFill.rectTransform;
            var bgRt = fillRt.parent as RectTransform;
            if (bgRt == null) return;
            float innerWidth = Mathf.Max(0f, bgRt.rect.width - 2f);
            fillRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, innerWidth * Mathf.Clamp01(ratio));
        }

        public IEnumerator PlayAttackWiggle(Vector2 direction)
        {
            _rt.anchoredPosition = _originalPos;
            Vector2 offset = direction.normalized * 30f;
            float elapsed = 0f;
            float duration = 0.2f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                _rt.anchoredPosition = _originalPos + offset * Mathf.Sin(t * Mathf.PI);
                yield return null;
            }
            _rt.anchoredPosition = _originalPos;
        }

        public IEnumerator PlayHitWiggle()
        {
            _rt.anchoredPosition = _originalPos;
            float elapsed = 0f;
            float duration = 0.25f;
            float magnitude = 10f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float decay = 1f - elapsed / duration;
                float x = Random.Range(-magnitude, magnitude) * decay;
                float y = Random.Range(-magnitude, magnitude) * decay;
                _rt.anchoredPosition = _originalPos + new Vector2(x, y);
                yield return null;
            }
            _rt.anchoredPosition = _originalPos;
        }
    }
}
