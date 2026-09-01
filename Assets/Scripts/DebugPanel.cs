using UnityEngine;
using UnityEngine.UI;

namespace TotalDeck
{
    /// <summary>
    /// Debug mode overlay. Toggle with F1. When enabled, a panel is docked on
    /// the right edge of the screen showing live AI (Enemy) economy + hand.
    /// Everything is built in code at runtime — no scene wiring needed.
    /// </summary>
    public class DebugPanel : MonoBehaviour
    {
        public static bool Enabled { get; private set; }

        GameObject panel;
        Text bodyText;
        Font font;
        CanvasGroup cg;

        void Start()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Build();
            SetVisible(false); // starts hidden
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                Enabled = !Enabled;
                SetVisible(Enabled);
            }
            if (Enabled && bodyText != null)
                Refresh();
        }

        void Build()
        {
            // Canvas
            var canvasGo = new GameObject("DebugPanelCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500; // above game HUD
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            cg = canvasGo.AddComponent<CanvasGroup>();

            // Panel (right dock)
            panel = new GameObject("DebugPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvasGo.transform, false);
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0.2f);
            rt.anchorMax = new Vector2(1f, 0.9f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(300f, 0f);
            panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);

            // Title
            var titleGo = new GameObject("Title", typeof(RectTransform), typeof(Text));
            titleGo.transform.SetParent(panel.transform, false);
            var trt = titleGo.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 1f); trt.anchorMax = new Vector2(1f, 1f);
            trt.pivot = new Vector2(0.5f, 1f);
            trt.anchoredPosition = new Vector2(0f, -8f);
            trt.sizeDelta = new Vector2(0f, 28f);
            var title = titleGo.GetComponent<Text>();
            title.font = font; title.fontSize = 18; title.alignment = TextAnchor.MiddleCenter;
            title.color = new Color(0f, 1f, 0.67f);
            title.text = "DEBUG  [F1]";

            // Body
            var bodyGo = new GameObject("Body", typeof(RectTransform), typeof(Text));
            bodyGo.transform.SetParent(panel.transform, false);
            var brt = bodyGo.GetComponent<RectTransform>();
            brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
            brt.offsetMin = new Vector2(12f, 8f);
            brt.offsetMax = new Vector2(-8f, -40f);
            bodyText = bodyGo.GetComponent<Text>();
            bodyText.font = font; bodyText.fontSize = 15; bodyText.alignment = TextAnchor.UpperLeft;
            bodyText.color = Color.white;
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyText.verticalOverflow = VerticalWrapMode.Overflow;
        }

        void SetVisible(bool on)
        {
            if (cg == null) return;
            cg.alpha = on ? 1f : 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }

        void Refresh()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Enemy == null)
            {
                bodyText.text = "(no GameManager)";
                return;
            }
            var e = gm.Enemy;
            int upkeep = gm.EnemyRegiments.Count * GameConfig.UpkeepPerRegiment;
            int net = GameConfig.BaseIncome + e.PendingBounty - upkeep;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("== AI / Enemy ==");
            sb.AppendLine($"Treasury:  ${e.Treasury}");
            sb.AppendLine($"Income:    +${GameConfig.BaseIncome}");
            sb.AppendLine($"Bounty:    +${e.PendingBounty}");
            sb.AppendLine($"Upkeep:    -${upkeep}");
            sb.AppendLine($"Net:       {(net >= 0 ? "+" : "")}${net}");
            sb.AppendLine($"DrawCost:  ${e.CurrentDrawCost}");
            sb.AppendLine($"Regiments: {gm.EnemyRegiments.Count}");
            sb.AppendLine();
            sb.AppendLine($"Hand ({e.Hand.Count}):");
            if (e.Hand.Count == 0)
                sb.AppendLine("  (empty)");
            else
                foreach (var c in e.Hand)
                    sb.AppendLine($"  • {(c != null ? c.cardName : "?")}  ${c?.playCost}");

            bodyText.text = sb.ToString();
        }
    }
}
