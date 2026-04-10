using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace AutobattlerSample.UI
{
    public class DamageNumber : MonoBehaviour
    {
        public static void Spawn(Transform parent, Vector2 position, int damage, Color color)
        {
            var go = new GameObject("DmgNum", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = new Vector2(120f, 40f);

            var text = go.GetComponent<Text>();
            text.text = damage > 0 ? $"-{damage}" : "0";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 30;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.raycastTarget = false;

            var dn = go.AddComponent<DamageNumber>();
            dn.StartCoroutine(dn.FloatAndFade(rt, text));
        }

        private IEnumerator FloatAndFade(RectTransform rt, Text text)
        {
            Vector2 startPos = rt.anchoredPosition;
            float duration = 0.9f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                rt.anchoredPosition = startPos + new Vector2(0f, 60f * t);
                var c = text.color;
                c.a = 1f - t * t;
                text.color = c;
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}

