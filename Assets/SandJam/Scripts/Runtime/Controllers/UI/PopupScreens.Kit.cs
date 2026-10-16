using UnityEngine;

namespace SandJamTest.Scene3D
{
    // Building blocks shared by every popup module: a blank card, the green button, the red close button and a
    // touch area. Popup modules reach them through PopupModule.
    public sealed partial class PopupScreens
    {
        internal Popup Blank(string name)
        {
            var p = Build(name);
            var keep = new[] { "Dark backdrop", "bg-currency", "button-plus_0", "icon-coin", "2040", "Next" };
            foreach (Transform child in p.Root.transform)
                if (System.Array.IndexOf(keep, child.name) < 0) child.gameObject.SetActive(false);
            return p;
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
            foreach (var label in Open.GetComponentsInChildren<TextMesh>(true)) if (label.name == "2040") label.text = Campaign.Coins.ToString();
        }
    }
}
