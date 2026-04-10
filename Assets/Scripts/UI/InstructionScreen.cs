using System;
using UnityEngine;
using UnityEngine.UI;

namespace AutobattlerSample.UI
{
    public class InstructionScreen
    {
        private GameObject _root;
        private RectTransform _content;
        private Action _onClose;

        public static InstructionScreen Create(Transform parent, Action onClose)
        {
            var screen = new InstructionScreen();
            screen._onClose = onClose;

            var canvas = UIFactory.CreateRootCanvas(parent);
            canvas.sortingOrder = 100; // Render on top of everything
            screen._root = UIFactory.CreatePanel("InstructionScreen", canvas.transform, Vector2.zero, Vector2.one);
            screen._content = screen._root.GetComponent<RectTransform>();
            screen._root.GetComponent<Image>().color = new Color(0.06f, 0.06f, 0.08f, 0.97f);
            screen._root.SetActive(false);
            return screen;
        }

        public void Show()
        {
            _root.SetActive(true);
            Clear();
            Build();
        }

        public void Hide() => _root.SetActive(false);
        public bool IsVisible => _root != null && _root.activeSelf;

        private void Build()
        {
            // Title
            var title = UIFactory.CreateText("Title", _content, "How to Play — Critter Chronicles", 34);
            title.fontStyle = FontStyle.Bold;
            title.color = new Color(1f, 0.85f, 0.3f);
            SetRect(title.rectTransform, new Vector2(0f, 0.92f), new Vector2(1f, 0.98f));

            // Scrollable body
            var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect));
            scrollGo.transform.SetParent(_content, false);
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0.05f, 0.08f);
            scrollRt.anchorMax = new Vector2(0.95f, 0.91f);
            scrollRt.offsetMin = Vector2.zero;
            scrollRt.offsetMax = Vector2.zero;

            var maskGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            maskGo.transform.SetParent(scrollGo.transform, false);
            maskGo.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f);
            maskGo.GetComponent<Mask>().showMaskGraphic = false;
            var maskRt = maskGo.GetComponent<RectTransform>();
            maskRt.anchorMin = Vector2.zero;
            maskRt.anchorMax = Vector2.one;
            maskRt.offsetMin = Vector2.zero;
            maskRt.offsetMax = Vector2.zero;

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
            layout.padding = new RectOffset(10, 10, 5, 5);
            layout.spacing = 4f;
            var fitter = contentGo.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = maskRt;
            scroll.content = contentRt;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;

            // Body text
            string body =
                "<color=#FFD700><b>== OVERVIEW ==</b></color>\n" +
                "Critter Chronicles is an autobattler roguelike. Build a team of critters, navigate a dungeon map, " +
                "fight enemies in automatic turn-based battles, and collect items to power up your team.\n\n" +

                "<color=#FFD700><b>== MAP & NAVIGATION ==</b></color>\n" +
                "• Choose a path through the dungeon floor by floor.\n" +
                "• <color=#6688CC>Battle</color> nodes pit your team against enemies.\n" +
                "• <color=#CC8833>Elite</color> nodes have tougher foes with bonus HP.\n" +
                "• <color=#CC3333>Boss</color> nodes are the final encounter — defeat the boss to win!\n" +
                "• <color=#33AA33>Rest</color> nodes fully heal your entire team and grant one free item.\n" +
                "• <color=#3388AA>Shop</color> nodes let you recruit new critters, upgrade existing ones, or buy items.\n" +
                "• Enemies that survive a lost battle reinforce nearby future nodes (marked with !!)\n\n" +

                "<color=#FFD700><b>== BATTLES ==</b></color>\n" +
                "• Battles are fully automatic and turn-based by rounds.\n" +
                "• At the start of each round, the UI shows the full turn order and who acts first.\n" +
                "• Each unit has <color=#FFAA33>Actions</color> (attack, heal, shield, etc.) with cooldowns.\n" +
                "• Actions are used in <color=#FFAA33>priority order</color> (lowest number = used first). " +
                "You can reorder priorities in the Manage Team screen.\n" +
                "• When all actions are on cooldown, the unit skips its turn.\n" +
                "• Front units (<color=#FFAA33>First</color> position) are targeted first by enemy attacks.\n" +
                "• Combat advances one round at a time with <color=#FFAA33>Next Round</color>, or continuously if <color=#FFAA33>Auto</color> is enabled.\n\n" +

                "<color=#FFD700><b>== ACTIONS & COOLDOWNS ==</b></color>\n" +
                "• <color=#CC4444>ATK</color> — Attack the closest enemy for X damage.\n" +
                "• <color=#4488CC>SHL</color> — Shield Self: grants a shield that absorbs damage.\n" +
                "• <color=#44AA55>HEL</color> — Heal Self: restores HP to this unit.\n" +
                "• <color=#55BB88>HFR</color> — Heal Front: heals the frontmost allied unit.\n" +
                "• <color=#66CC99>HAL</color> — Heal All: heals every living ally.\n" +
                "• Each action has a cooldown (CD) shown as a number. After use, the action " +
                "goes on cooldown for that many rounds. The radial overlay shows remaining CD visually.\n\n" +

                "<color=#FFD700><b>== PASSIVES ==</b></color>\n" +
                "• <color=#DD88FF>Lifesteal</color> — When this unit deals damage through HP (not shield), it heals for that amount.\n" +
                "• <color=#DD88FF>HasteOnHeal</color> — Whenever this unit is healed, its first Attack action's cooldown " +
                "is reduced by 1. This means heal-focused teams speed up the Bear's big attacks!\n\n" +

                "<color=#FFD700><b>== TEAM MANAGEMENT ==</b></color>\n" +
                "• Your team has a max of <color=#FFAA33>6 slots</color>. Large critters take 2 slots.\n" +
                "• Use the <color=#4488CC>Manage Team</color> button on the map to drag critters into formation order, " +
                "drag camp critters into the party, drag active critters back to camp, and drag camp items onto a critter to equip them.\n" +
                "• Camp units don't fight but can be swapped in at any time.\n" +
                "• Items can be unequipped and stored in camp, then given to a different unit.\n\n" +

                "<color=#FFD700><b>== ITEMS ==</b></color>\n" +
                "• <color=#66FF66>Max HP</color> items permanently increase a unit's health.\n" +
                "• <color=#CC88FF>CD Reduction</color> items reduce attack cooldowns.\n" +
                "• <color=#6699FF>Shield</color> items grant a Shield Self action.\n" +
                "• <color=#FFAA66>Action Grant</color> items give a unit a brand-new action (attack, heal, etc.).\n" +
                "• Items are awarded after winning battles or purchased from shops.\n\n" +

                "<color=#FFD700><b>== RECRUITING & RANKING UP ==</b></color>\n" +
                "• If you acquire a critter you already own, it <color=#FFD700>ranks up</color> instead!\n" +
                "• Ranking up increases Max HP and scales attack damage.\n\n" +

                "<color=#FFD700><b>== TIPS ==</b></color>\n" +
                "• Put tanky units in front (First, Second) to absorb damage.\n" +
                "• Healers behind the front line keep your tank alive and trigger HasteOnHeal passives.\n" +
                "• Balance your team between damage dealers and support.\n" +
                "• Longer runs now have many more floors, so pace your team and item usage.\n" +
                "• Check the combat log for detailed breakdowns of what happened each round.";

            var bodyText = UIFactory.CreateText("Body", contentGo.transform, body, 18, TextAnchor.UpperLeft);
            bodyText.color = new Color(0.9f, 0.9f, 0.92f);
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyText.verticalOverflow = VerticalWrapMode.Overflow;

            // Close button
            var closeBtn = UIFactory.CreateButton("Close", _content, "Close");
            SetRect(closeBtn.GetComponent<RectTransform>(), new Vector2(0.38f, 0.01f), new Vector2(0.62f, 0.07f));
            closeBtn.GetComponent<Image>().color = new Color(0.3f, 0.2f, 0.15f);
            closeBtn.onClick.AddListener(() =>
            {
                Hide();
                _onClose?.Invoke();
            });
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

