using System.Collections;
using AutobattlerSample.Battle;
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

        public void Init(BattleUnit unit, Image shape, Text statLabel, Image hpBarFill = null)
        {
            Unit = unit;
            _shapeImage = shape;
            _statText = statLabel;
            _hpBarFill = hpBarFill;
            _rt = GetComponent<RectTransform>();
            _originalPos = _rt.anchoredPosition;
            _originalColor = shape.color;
            UpdateStats();
        }

        public void UpdateStats()
        {
            if (Unit == null) return;
            if (!Unit.IsAlive)
            {
                _statText.text = $"{Unit.DisplayName}\n<color=red>DEAD</color>";
                _shapeImage.color = new Color(_originalColor.r * 0.3f, _originalColor.g * 0.3f, _originalColor.b * 0.3f, 0.35f);
                if (_hpBarFill != null) _hpBarFill.fillAmount = 0f;
                return;
            }
            _statText.text = $"{Unit.DisplayName}\nHP:{Unit.CurrentHP}/{Unit.MaxHP}\nARM:{Unit.Armor}  ATK:{Unit.Attack}";
            if (_hpBarFill != null)
            {
                float ratio = Unit.MaxHP > 0 ? (float)Unit.CurrentHP / Unit.MaxHP : 0f;
                _hpBarFill.fillAmount = ratio;
                // Colour shifts green → yellow → red as HP drops
                _hpBarFill.color = Color.Lerp(new Color(0.9f, 0.2f, 0.2f), new Color(0.2f, 0.85f, 0.25f), ratio);
            }
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

