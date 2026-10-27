using UnityEngine;

namespace SandJamTest.Scene3D
{
    // Building blocks shared by every popup module: a blank card, the green button, the red close button and a
    // touch area. Popup modules reach them through PopupModule.
    public sealed partial class PopupScreens
    {
        // Deep purple layer of the original's Superb! screen and Hard / Bonus level cards (#1E1936 at ~94%,
        // recording 2 228 s; hard card of level 260: the board reads #201E3D under it).
        internal static readonly Color32 DeepDim = new Color32(30, 25, 54, 241);
        // The coin bar copied from the result page into every card (with its gold rim).
        internal static readonly string[] CoinBar = { "bg-currency", "button-plus_0", "icon-coin", "2040", "Coin bar gold rim" };
        // The result card with only the backdrop, coin bar (without the rim) and button group left (optionally its
        // touch areas too).
        static readonly string[] BlankKeep = { "Dark backdrop", "bg-currency", "button-plus_0", "icon-coin", "2040", "Next" };
        internal Popup Blank(string name, bool keepHitAreas = false)
        {
            var p = Build(name);
            foreach (Transform child in p.Root.transform)
                if (System.Array.IndexOf(BlankKeep, child.name) < 0 && !(keepHitAreas && child.name.StartsWith("Hit area", System.StringComparison.Ordinal))) child.gameObject.SetActive(false);
            return p;
        }
        // Scales the card (everything but the backdrop and coin bar) about the popup centre and moves it up by dyPx
        // pixels of the recordings.
        internal static void FitCard(Popup p, float scale, float dyPx)
        {
            foreach (Transform child in p.Root.transform)
            {
                if (child.name == "Dark backdrop" || System.Array.IndexOf(CoinBar, child.name) >= 0) continue;
                child.localPosition = Vector3.Scale(child.localPosition, new Vector3(scale, scale, 1));
                child.localScale = Vector3.Scale(child.localScale, new Vector3(scale, scale, 1));
                child.position += Vector3.up * dyPx * VideoScreen.Px;
            }
        }
        // The coin bar left visible but dimmed, as under the original's opaque CONGRATS screen.
        internal static void DimCoinBar(Popup p, float brightness)
        {
            foreach (var name in CoinBar)
            {
                var t = p.Root.transform.Find(name); if (!t) continue;
                foreach (var r in t.GetComponentsInChildren<SpriteRenderer>(true)) r.color = new Color(r.color.r * brightness, r.color.g * brightness, r.color.b * brightness, r.color.a);
                foreach (var m in t.GetComponentsInChildren<TextMesh>(true)) m.color = new Color(m.color.r * brightness, m.color.g * brightness, m.color.b * brightness, m.color.a);
            }
        }
        internal static void HideCoinBar(Popup p)
        {
            foreach (var name in CoinBar) { var t = p.Root.transform.Find(name); if (t) t.gameObject.SetActive(false); }
        }
        internal void GreenButton(Popup p, float y, string action, string text, float scale = 1.05f)
        {
            var group = p.RightButton.transform.parent; group.localPosition = new Vector3(0, 0, group.localPosition.z);
            foreach (Transform child in p.RightButton.transform.parent)
                child.localPosition = new Vector3(0, y, child.localPosition.z);
            p.RightButton.transform.parent.localScale = new Vector3(scale, scale, 1);
            Configure(p.RightButton, p.Right, action, text);
        }
        internal void CloseX(Popup p, Vector3 local, string action)
        {
            var panel = Child<SpriteRenderer>(p, "Close button", local);
            // Original glossy red button with a white rim and cross (button-exit); drawn fallback otherwise.
            var art = Resources.Load<Sprite>("VideoUI/button-exit-v2");
            if (art) { panel.sprite = art; float k = .62f / art.bounds.size.y; panel.transform.localScale = new Vector3(k, k, 1); }
            else
            {
                panel.sprite = p.Ribbon.sprite; panel.color = new Color(.92f, .16f, .2f);
                float s = .5f / Mathf.Max(.01f, panel.sprite.bounds.size.y);
                panel.transform.localScale = new Vector3(s * panel.sprite.bounds.size.y / Mathf.Max(.01f, panel.sprite.bounds.size.x), s, 1);
                Label(p, "Close X", local + new Vector3(0, 0, -.2f), "X", .06f, Color.white);
            }
            var hit = Child<BoxCollider>(p, "Hit area - " + action, local + new Vector3(0, 0, -.5f));
            hit.gameObject.layer = 9; hit.size = new Vector3(.6f, .6f, .2f);
            hit.gameObject.AddComponent<VideoUiButton>().Action = action;
            // Under the button art so a press bounces the button, not the whole popup.
            var scale = panel.transform.localScale;
            hit.transform.SetParent(panel.transform, true);
            hit.size = new Vector3(.6f / scale.x, .6f / scale.y, .2f);
        }
        internal GameObject Hit(Popup p, Vector3 local, Vector2 size, string action)
        {
            var hit = Child<BoxCollider>(p, "Hit area - " + action, local);
            hit.gameObject.layer = 9; hit.size = new Vector3(size.x, size.y, .2f);
            hit.gameObject.AddComponent<VideoUiButton>().Action = action;
            return hit.gameObject;
        }
        // Coin counters on every open popup / page show the wallet.
        internal void RefreshPopupCoins()
        {
            if (!Open) return;
            foreach (var label in Open.GetComponentsInChildren<TextMesh>(true)) if (label.name == "2040") label.text = EconomyManager.Coins.ToString();
        }
    }
}
