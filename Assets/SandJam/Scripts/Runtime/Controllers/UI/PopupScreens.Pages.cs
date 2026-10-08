using System.Linq;
using UnityEngine;

namespace SandJamTest.Scene3D
{
    // SHOP and GALLERY are full pages in the original, not dialogs: the Home top bar (settings, lives, coins) and
    // the SHOP / HOME / GALLERY navbar stay on screen, with the current tab highlighted. The Home chrome is copied
    // into the page in front of its content; the navbar switches pages.
    public sealed partial class PopupScreens
    {
        static readonly string[] TopBarParts = { "Top bar", "icon-settings-v2", "bg-currency", "button-plus_0", "icon-coin", "2040", "Full", "icon-heart-big", "5" };
        static readonly string[] NavbarParts = { "bg-navbar", "bg-navBar-seleckted", "icon-shop", "icon-home", "icon-cards", "SHOP", "HOME", "GALLERY" };

        void AddPageChrome(Popup page, string activeIcon)
        {
            var home = screen.Home.transform;
            foreach (Transform child in home)
            {
                if (System.Array.IndexOf(TopBarParts, child.name) < 0 && System.Array.IndexOf(NavbarParts, child.name) < 0) continue;
                var copy = Instantiate(child.gameObject, page.Root.transform, true);
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
        void RefreshPageLives(Popup page)
        {
            foreach (var label in page.Root.GetComponentsInChildren<TextMesh>(true))
            {
                if (label.transform.parent != page.Root.transform) continue;
                if (label.name == "5") label.text = LivesManager.Lives.ToString();
                else if (label.name == "Full") label.text = LivesManager.IsFull ? "Full" : LivesManager.Countdown(LivesManager.UntilNext);
            }
        }

        bool HandleNav(string action)
        {
            Close();
            if (action == "nav-shop") OpenShop();
            else if (action == "nav-gallery") OpenGallery();
            return true;
        }
    }
}
