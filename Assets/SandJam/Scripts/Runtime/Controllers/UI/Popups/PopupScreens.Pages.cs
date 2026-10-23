using System.Linq;
using UnityEngine;

namespace SandJamTest.Scene3D
{
    // SHOP and GALLERY are full pages in the original, not dialogs: the Home top bar (settings, lives, coins) and
    // the SHOP / HOME / GALLERY navbar stay on screen, with the current tab highlighted. The Home chrome is copied
    // into the page in front of its content; the navbar switches pages.
    public sealed partial class PopupScreens
    {
        internal static readonly string[] TopBarParts = { "Top bar", "Top bar status band", "Top bar edge", "icon-settings-v2", "bg-currency", "button-plus_0", "icon-coin", "2040", "Full", "icon-heart-big", "5" };
        static readonly string[] NavbarParts = { "bg-navbar", "bg-navBar-seleckted", "icon-shop", "icon-home", "icon-cards", "SHOP", "HOME", "GALLERY" };

        internal void AddPageChrome(Popup page, string activeIcon)
        {
            var home = screen.Home.transform;
            foreach (Transform child in home)
            {
                if (System.Array.IndexOf(TopBarParts, child.name) < 0 && System.Array.IndexOf(NavbarParts, child.name) < 0) continue;
                var copy = Instantiate(child.gameObject, page.Root.transform, true);
                copy.name = child.name; // refreshes find the counters by name (no "(Clone)")
                foreach (var hit in copy.GetComponentsInChildren<VideoUiButton>(true)) Destroy(hit.gameObject);
                // In front of the page content (page art sits around z -11).
                var p = copy.transform.position; copy.transform.position = new Vector3(p.x, p.y, p.z - 6.5f);
                copy.SetActive(true);
                if (child.name == "bg-navBar-seleckted")
                {
                    var icon = home.Find(activeIcon);
                    if (icon) copy.transform.position = new Vector3(icon.position.x, copy.transform.position.y, copy.transform.position.z);
                }
            }
            // Navbar buttons: same spots as the Home icons.
            foreach (var pair in new[] { new[] { "icon-shop", "nav-shop" }, new[] { "icon-home", "nav-home" }, new[] { "icon-cards", "nav-gallery" } })
            {
                var icon = home.Find(pair[0]); if (!icon) continue;
                var hit = Hit(page, Vector3.zero, new Vector2(1.6f, 1.2f), pair[1]);
                hit.transform.position = new Vector3(icon.position.x, -4.6f, icon.position.z - 9f);
            }
        }

        // Lives count / timer on a page's copied top bar.
        internal void RefreshPageLives(Popup page)
        {
            foreach (var label in page.Root.GetComponentsInChildren<TextMesh>(true))
            {
                if (label.transform.parent != page.Root.transform) continue;
                if (label.name == "5") label.text = LivesManager.Lives.ToString();
                else if (label.name == "Full") label.text = LivesManager.IsFull ? "Full" : LivesManager.Countdown(LivesManager.UntilNext);
            }
        }

        // Recording 2 (3.2-3.45 s, Home -> Shop): a page slides in from its tab's side (Shop from the left, Gallery from
        // the right) over what was on screen, a whole screen width in ~0.25 s, slowing steadily to a stop (last frames
        // -150, -95, -60, -34, -15, -3, 0 px). The top bar and the navbar stay put.
        const float PageSlideSeconds = .25f;
        int pageTab = 1; // 0 Shop, 1 Home, 2 Gallery
        readonly System.Collections.Generic.List<Transform> sliding = new System.Collections.Generic.List<Transform>();
        readonly System.Collections.Generic.List<float> slidingX = new System.Collections.Generic.List<float>();
        float pageSlideFrom, pageSlideAt = -1;
        int TabOf(GameObject root) { return shop.IsPage(root) ? 0 : gallery.IsPage(root) ? 2 : -1; }
        void StartPageSlide(GameObject root)
        {
            EndPageSlide();
            int tab = TabOf(root);
            if (tab < 0) return;
            if (tab != pageTab)
            {
                pageSlideFrom = Mathf.Sign(tab - pageTab) * 888 * VideoScreen.Px; pageSlideAt = Time.unscaledTime;
                foreach (Transform child in root.transform)
                {
                    if (!child.gameObject.activeSelf || System.Array.IndexOf(TopBarParts, child.name) >= 0 || System.Array.IndexOf(NavbarParts, child.name) >= 0 || child.name.StartsWith("Hit area - nav-")) continue;
                    sliding.Add(child); slidingX.Add(child.localPosition.x);
                }
                TickPageSlide();
            }
            pageTab = tab;
        }
        void TickPageSlide()
        {
            if (pageSlideAt < 0) return;
            float t = Mathf.Clamp01((Time.unscaledTime - pageSlideAt) / PageSlideSeconds), offset = pageSlideFrom * (1 - t) * (1 - t);
            for (int i = 0; i < sliding.Count; i++)
                if (sliding[i]) { var p = sliding[i].localPosition; sliding[i].localPosition = new Vector3(slidingX[i] + offset, p.y, p.z); }
            if (t >= 1) { pageSlideAt = -1; sliding.Clear(); slidingX.Clear(); }
        }
        void EndPageSlide() { if (pageSlideAt >= 0) { pageSlideAt = Time.unscaledTime - PageSlideSeconds; TickPageSlide(); } }
        // A page closing returns to Home's tab.
        void LeavePage(GameObject root) { if (TabOf(root) >= 0) { EndPageSlide(); pageTab = 1; } }

        bool HandleNav(string action)
        {
            Close();
            if (action == "nav-shop") OpenShop();
            else if (action == "nav-gallery") OpenGallery();
            return true;
        }
    }
}
