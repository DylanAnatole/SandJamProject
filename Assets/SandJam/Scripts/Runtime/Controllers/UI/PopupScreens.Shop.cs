using UnityEngine;

namespace SandJamTest.Scene3D
{
    // SAND SHOP page rebuilt from the original shop mockup (Texture2D/Shop.png, 342 px wide) and shop art, with
    // the contents of the original remote-config defaults (RC_Shop, reward_bundle, reward_coin_preset):
    // bundles (coins + boosters), coin packs, boosters for coins (x3 Funflare 1000 / Swap 1300 / Select 2000)
    // and the booster bundle (x5 each, 6000). Real-money items are shown but the clone has no payments.
    public sealed partial class PopupScreens
    {
        Popup shop;
        Transform shopContent;
        TextMesh[] shopToast;
        float shopScroll, shopScrollMax, shopToastUntil, shopPressY, shopPressScroll;
        bool shopPressing, shopDragged;
        string shopPendingAction;
        const float ShopUnit = 5.2f / 342f;      // mockup pixels → popup units
        const float ShopTop = 2.95f;             // where mockup row 185 (first card) sits when not scrolled (under the header)
        static readonly string[] BoosterIcons = { "Shop/icon-flare", "Shop/icon-swap", "Shop/icon-select" };
        static readonly string[] BoosterActions = { "rocket", "swap", "select" };

        static float ShopY(float mockupY) { return ShopTop - (mockupY - 185) * ShopUnit; }
        static float ShopX(float mockupX) { return (mockupX - 168) * ShopUnit; }

        void BuildShop()
        {
            shop = Blank("Shop page");
            shop.RightButton.transform.parent.gameObject.SetActive(false);
            var backdrop = shop.Root.transform.Find("Dark backdrop").GetComponent<SpriteRenderer>();
            backdrop.color = new Color(.17f, .35f, .55f, 1f); // the shop's navy page colour
            shopContent = new GameObject("Shop content") { layer = shop.Root.layer }.transform;
            shopContent.SetParent(shop.Root.transform, false);

            // Bundles: coins + one row of boosters, green price button.
            // Store prices as shown in the Vietnamese store (recording 2026-10-08); the starter price was not visible.
            Bundle(277, "Shop/bg-starterPack-shop", "Shop/bg-slot-starterPack-shop", "Starter Pack", 2500, 1, "299000 VND");
            Bundle(474, "Shop/bg-smallBundle-shop", "Shop/bg-slot-smallBundle-shop", "Small Bundle", 5000, 2, "599000 VND");
            Bundle(671, "Shop/bg-mediumBundle-shop", "Shop/bg-slot-smallBundle-shop", "Medium Bundle", 8000, 4, "999000 VND");
            Bundle(868, "Shop/bg-largeBundle-shop", "Shop/bg-slot-smallBundle-shop", "Large Bundle", 12000, 6, "1199000 VND");

            // Coin packs (reward_coin_preset).
            SectionHeader(1013, "Shop/bg-coinSectionHeader-shop", "COINS");
            int[] coins = { 500, 2500, 5000, 12500, 25000, 50000 };
            string[] prices = { "25000 VND", "75000 VND", "125000 VND", "249000 VND", "499000 VND", "999000 VND" }; // estimates
            for (int i = 0; i < coins.Length; i++)
                CoinCell(new[] { 63f, 167f, 270f }[i % 3], i < 3 ? 1130 : 1298, "Shop/icon-coin" + (i + 1) + "-shop", coins[i], prices[i]);

            // Boosters for coins and the booster bundle.
            // Recording: Flare x3 400, Swap x3 1300, Select x3 2000, all three x5 for 6000.
            SectionHeader(1425, "Shop/bg-boosterSectionHeader-shop", "BOOSTERS");
            int[] boosterPrices = { 400, 1300, 2000 };
            for (int i = 0; i < 3; i++) BoosterCell(new[] { 63f, 167f, 270f }[i], 1542, i, 3, boosterPrices[i]);
            BigBoosterBundle(1720, 5, 6000);
            shopScrollMax = Mathf.Max(0, (1820 - 185) * ShopUnit - (ShopTop + 3.9f)); // list ends above the navbar

            // Fixed header drawn over the scrolling list.
            // Fixed SAND SHOP header under the Home top bar, drawn over the scrolling list.
            var header = ShopArt("Shop/bg-header1-shop", 168, 0, 5.4f, -7.4f, null); header.transform.localPosition = new Vector3(0, 3.7f, -7.4f);
            var bar = Child<SpriteRenderer>(shop, "Header bar", new Vector3(0, 4.1f, -7.3f));
            var white = Texture2D.whiteTexture; bar.sprite = Sprite.Create(white, new Rect(0, 0, white.width, white.height), new Vector2(.5f, .5f), white.width);
            runtimeArt.Add(bar.sprite); bar.color = new Color(.15f, .3f, .48f); bar.transform.localScale = new Vector3(8, 1.6f, 1);
            SetText(OutlinedLabel(shop, "Shop title", new Vector3(0, 3.7f, -7.6f), .055f), "SAND SHOP");
            // Bottom strip behind the navbar so scrolled cards never show under it.
            var foot = Child<SpriteRenderer>(shop, "Footer bar", new Vector3(0, -5.1f, -7.3f)); foot.sprite = bar.sprite;
            foot.color = bar.color; foot.transform.localScale = new Vector3(8, 1.8f, 1);
            AddPageChrome(shop, "icon-shop");
            shopToast = OutlinedLabel(shop, "Shop toast", new Vector3(0, -3.75f, -8.2f), .034f);
            foreach (var name in new[] { "bg-currency", "button-plus_0", "icon-coin", "2040" })
            { var t = shop.Root.transform.Find(name); if (t) t.gameObject.SetActive(false); }
        }

        SpriteRenderer ShopArt(string sprite, float x, float y, float width, float z, Transform parent)
        {
            var r = Art(shop, sprite, new Vector3(ShopX(x), ShopY(y), z), width);
            if (parent) r.transform.SetParent(parent, true);
            return r;
        }
        TextMesh[] ShopText(string text, float x, float y, float size, float z, bool outlined, Color color)
        {
            var labels = outlined ? OutlinedLabel(shop, "Shop text", new Vector3(ShopX(x), ShopY(y), z), size)
                                  : new[] { Label(shop, "Shop text", new Vector3(ShopX(x), ShopY(y), z), text, size, color) };
            SetText(labels, text);
            labels[0].transform.SetParent(shopContent, true);
            return labels;
        }
        void ShopHit(float x, float y, float w, float h, string action)
        {
            var hit = Hit(shop, new Vector3(ShopX(x), ShopY(y), -7.2f), new Vector2(w * ShopUnit, h * ShopUnit), action);
            hit.transform.SetParent(shopContent, true);
        }
        void PriceButton(float x, float y, float w, string text, string action, bool coinIcon)
        {
            var b = ShopArt("Shop/button-green", x, y, w * ShopUnit, -6.7f, shopContent);
            if (b.sprite) b.transform.localScale = new Vector3(w * ShopUnit / b.sprite.bounds.size.x, 38 * ShopUnit / b.sprite.bounds.size.y, 1);
            if (coinIcon) ShopArt("Shop/icon-coin_1", x - w * .28f, y, 22 * ShopUnit, -6.8f, shopContent);
            ShopText(text, x + (coinIcon ? 10 : 0), y - 1, .038f, -6.9f, true, Color.white);
            ShopHit(x, y, w, 40, action);
        }

        void Bundle(float y, string card, string slot, string title, int coins, int each, string price)
        {
            ShopArt(card, 168, y, 300 * ShopUnit, -6.3f, shopContent);
            ShopText(title, 168, y - 74, .04f, -6.6f, true, Color.white);
            ShopArt("Shop/icon-coin2-shop", 60, y - 22, 52 * ShopUnit, -6.5f, shopContent);
            ShopText(coins.ToString(), 60, y + 10, .034f, -6.7f, true, Color.white);
            ShopArt(slot, 203, y - 21, 197 * ShopUnit, -6.4f, shopContent);
            for (int i = 0; i < 3; i++)
            {
                float x = 145 + i * 57;
                ShopArt(BoosterIcons[i], x, y - 27, 36 * ShopUnit, -6.5f, shopContent);
                ShopText("x" + each, x + 4, y - 6, .03f, -6.6f, true, Color.white);
            }
            PriceButton(167, y + 53, 135, price, "shop-iap", false);
        }
        void SectionHeader(float y, string sprite, string title)
        {
            var r = ShopArt(sprite, 168, y, 300 * ShopUnit, -6.3f, shopContent);
            if (r.sprite) r.transform.localScale = new Vector3(300 * ShopUnit / r.sprite.bounds.size.x, 43 * ShopUnit / r.sprite.bounds.size.y, 1);
            ShopText(title, 168, y - 1, .042f, -6.5f, true, Color.white);
        }
        void Cell(string card, float x, float y)
        {
            var r = ShopArt(card, x, y, 92 * ShopUnit, -6.3f, shopContent);
            if (r.sprite) r.transform.localScale = new Vector3(92 * ShopUnit / r.sprite.bounds.size.x, 140 * ShopUnit / r.sprite.bounds.size.y, 1);
        }
        void CoinCell(float x, float y, string icon, int coins, string price)
        {
            Cell("Shop/bg-coinSection-shop", x, y);
            var slot = ShopArt("Shop/bg-slot-coinSection-shop", x, y - 15, 80 * ShopUnit, -6.4f, shopContent);
            if (slot.sprite) slot.transform.localScale = new Vector3(80 * ShopUnit / slot.sprite.bounds.size.x, 95 * ShopUnit / slot.sprite.bounds.size.y, 1);
            ShopArt(icon, x, y - 28, 52 * ShopUnit, -6.5f, shopContent);
            ShopText(coins.ToString(), x, y + 12, .032f, -6.6f, true, Color.white);
            ShopText(price, x, y + 50, .032f, -6.6f, true, Color.white);
            ShopHit(x, y, 92, 140, "shop-iap");
        }
        void BoosterCell(float x, float y, int booster, int count, int price)
        {
            Cell("Shop/bg-boosterSection-shop", x, y);
            var slot = ShopArt("Shop/bg-slot-boosterSection-shop", x, y - 15, 80 * ShopUnit, -6.4f, shopContent);
            if (slot.sprite) slot.transform.localScale = new Vector3(80 * ShopUnit / slot.sprite.bounds.size.x, 95 * ShopUnit / slot.sprite.bounds.size.y, 1);
            ShopArt(BoosterIcons[booster], x, y - 32, 48 * ShopUnit, -6.5f, shopContent);
            ShopText("x" + count, x, y + 10, .036f, -6.6f, true, Color.white);
            ShopArt("Shop/icon-coin_1", x - 26, y + 50, 18 * ShopUnit, -6.6f, shopContent);
            ShopText(price.ToString(), x + 6, y + 50, .032f, -6.6f, true, Color.white);
            ShopHit(x, y, 92, 140, "shop-buy:" + BoosterActions[booster] + ":" + count + ":" + price);
        }
        void BigBoosterBundle(float y, int each, int price)
        {
            var r = ShopArt("Shop/bg-boosterSection-big-shop", 168, y, 300 * ShopUnit, -6.3f, shopContent);
            if (r.sprite) r.transform.localScale = new Vector3(300 * ShopUnit / r.sprite.bounds.size.x, 160 * ShopUnit / r.sprite.bounds.size.y, 1);
            var slot = ShopArt("Shop/bg-slot-boosterSection-big-shop", 168, y - 30, 270 * ShopUnit, -6.4f, shopContent);
            if (slot.sprite) slot.transform.localScale = new Vector3(270 * ShopUnit / slot.sprite.bounds.size.x, 70 * ShopUnit / slot.sprite.bounds.size.y, 1);
            for (int i = 0; i < 3; i++)
            {
                float x = 85 + i * 83;
                ShopArt(BoosterIcons[i], x, y - 34, 44 * ShopUnit, -6.5f, shopContent);
                ShopText("x" + each, x + 22, y - 18, .036f, -6.6f, true, Color.white);
            }
            PriceButton(167, y + 42, 135, price.ToString(), "shop-buy:all:" + each + ":" + price, true);
        }

        public void OpenShop()
        {
            if (Open) return;
            shopScroll = 0; shopContent.localPosition = Vector3.zero;
            SetText(shopToast, "");
            Show(shop); RefreshPageLives(shop);
        }
        void RefreshShopCoins() { RefreshPopupCoins(); }
        void ShopToast(string text) { SetText(shopToast, text); shopToastUntil = Time.unscaledTime + 2.2f; }

        // Shop buttons fire on release without a drag, so scrolling never buys anything by accident.
        bool HandleShop(string action)
        {

            shopPendingAction = action;
            return true;
        }
        void RunShopAction(string action)
        {
            if (action == "shop-iap") { ShopToast("Purchases are not available"); return; }
            var parts = action.Split(':'); // shop-buy:<booster|all>:<count>:<price>
            int count = int.Parse(parts[2]), price = int.Parse(parts[3]);
            if (!EconomyManager.TrySpend(price, "shop:" + parts[1])) { ShopToast("Not enough coins · need " + price); return; }
            foreach (var booster in parts[1] == "all" ? BoosterActions : new[] { parts[1] }) EconomyManager.AddBooster(booster, count, "shop");
            ShopToast(parts[1] == "all" ? "+" + count + " of each booster" : "+" + count + " booster");
            RefreshShopCoins();
        }

        void UpdateShop()
        {
            if (Open != shop.Root) return;
            if (shopToastUntil > 0 && Time.unscaledTime > shopToastUntil) { SetText(shopToast, ""); shopToastUntil = 0; }
            var cam = screen.UiCamera; if (!cam) return;
            var pointer = Input.mousePosition;
            if (float.IsInfinity(pointer.x) || float.IsNaN(pointer.x) || float.IsInfinity(pointer.y) || float.IsNaN(pointer.y)) return; // no pointer (unfocused editor)
            float y = cam.ScreenToWorldPoint(pointer).y;
            if (Input.GetMouseButtonDown(0)) { shopPressing = true; shopDragged = false; shopPressY = y; shopPressScroll = shopScroll; }
            if (shopPressing && Input.GetMouseButton(0))
            {
                float delta = y - shopPressY;
                if (Mathf.Abs(delta) > .12f) shopDragged = true;
                if (shopDragged) shopScroll = Mathf.Clamp(shopPressScroll + delta, 0, shopScrollMax);
            }
            if (shopPressing && Input.GetMouseButtonUp(0))
            {
                shopPressing = false;
                if (!shopDragged && shopPendingAction != null) RunShopAction(shopPendingAction);
                shopPendingAction = null;
            }
            float wheel = Input.mouseScrollDelta.y;
            if (Mathf.Abs(wheel) > .01f) shopScroll = Mathf.Clamp(shopScroll - wheel * .6f, 0, shopScrollMax);
            shopContent.localPosition = new Vector3(0, shopScroll, 0);
        }
    }
}
