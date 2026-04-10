using System;
using AutobattlerSample.Core;
using AutobattlerSample.Data;
using UnityEngine;
using UnityEngine.UI;

namespace AutobattlerSample.UI
{
    public class RestScreen
    {
        private GameObject _root;
        private RectTransform _content;
        private Action _onContinue;

        public static RestScreen Create(Transform parent, Action onContinue)
        {
            var screen = new RestScreen();
            screen._onContinue = onContinue;

            var canvas = UIFactory.CreateRootCanvas(parent);
            screen._root = UIFactory.CreatePanel("RestScreen", canvas.transform, Vector2.zero, Vector2.one);
            screen._content = screen._root.GetComponent<RectTransform>();
            screen._root.SetActive(false);
            return screen;
        }

        public void Show(RunState state, ItemData rewardItem)
        {
            _root.SetActive(true);
            Clear();

            var title = UIFactory.CreateText("Title", _content, "Campfire Rest", 42);
            title.color = new Color(1f, 0.7f, 0.3f);
            title.fontStyle = FontStyle.Bold;
            SetRect(title.rectTransform, new Vector2(0f, 0.8f), new Vector2(1f, 0.92f));

            var desc = UIFactory.CreateText("Desc", _content,
                "Your team rests by the fire.\nAll allies heal to full HP and you find a campsite reward.", 26);
            SetRect(desc.rectTransform, new Vector2(0.1f, 0.62f), new Vector2(0.9f, 0.80f));

            string info = "";
            foreach (var unit in state.Team)
            {
                string healInfo = unit.CurrentHP < unit.EffectiveMaxHP
                    ? $"HP {unit.CurrentHP} -> {unit.EffectiveMaxHP}"
                    : "Full HP";
                info += $"{unit.DisplayName}: {healInfo}\n";
            }

            var infoText = UIFactory.CreateText("HealInfo", _content, info, 24);
            SetRect(infoText.rectTransform, new Vector2(0.2f, 0.34f), new Vector2(0.8f, 0.60f));

            if (rewardItem != null)
            {
                var rewardText = UIFactory.CreateText("RewardInfo", _content,
                    $"Campsite Reward:\n{rewardItem.Name}\n{rewardItem.TypeName}", 24);
                rewardText.color = new Color(0.75f, 1f, 0.75f);
                SetRect(rewardText.rectTransform, new Vector2(0.25f, 0.20f), new Vector2(0.75f, 0.34f));
            }

            var button = UIFactory.CreateButton("Rest", _content, "Rest & Continue");
            SetRect(button.GetComponent<RectTransform>(), new Vector2(0.35f, 0.08f), new Vector2(0.65f, 0.18f));
            button.onClick.AddListener(() => _onContinue?.Invoke());
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
