using UnityEngine;

namespace SandJamTest.Scene3D
{
    // SAND SHOP page rebuilt from the original shop mockup (Texture2D/Shop.png, 342 px wide) and shop art, with
    // the contents of the original remote-config defaults (RC_Shop, reward_bundle, reward_coin_preset):
    // bundles (coins + boosters), coin packs, boosters for coins (x3 Funflare 1000 / Swap 1300 / Select 2000)
    // and the booster bundle (x5 each, 6000). Real-money items are shown but the clone has no payments.
    public sealed class ShopPopup : PopupModule
    {
        Popup shop;
        Transform shopContent;
        readonly DragScroller scroll = new DragScroller();
        const float ShopUnit = 5.2f / 342f;      // mockup pixels → popup units
        const float ShopTop = 2.75f;             // where mockup row 185 (first card) sits when not scrolled (under the header)
        static readonly string[] BoosterIcons = { "Shop/icon-flare", "Shop/icon-swap", "Shop/icon-select" };
        static readonly string[] BoosterActions = { "rocket", "swap", "select" };

        static float ShopY(float mockupY) { return ShopTop - (mockupY - 185) * ShopUnit; }
        static float ShopX(float mockupX) { return (mockupX - 168) * ShopUnit; }

        public override bool IsPage(GameObject root) { return shop != null && root == shop.Root; }
        public override void Build()
        {
            shop = Blank("Shop page");
            shop.RightButton.transform.parent.gameObject.SetActive(false);
            var backdrop = shop.Root.transform.Find("Dark backdrop").GetComponent<SpriteRenderer>();
            backdrop.color = new Color(.122f, .294f, .443f, 1f); // the shop's navy page colour (reads #1B4162, recording 2)
            shopContent = new GameObject("Shop content") { layer = shop.Root.layer }.transform;
            shopContent.SetParent(shop.Root.transform, false);

            // Bundles: coins + one row of boosters, green price button.
            // Store prices as shown in the Vietnamese store (recording 2026-10-08); the starter price was not visible.
            // Recording 2026-10-08: the list starts with REMOVE ADS (249000) and the No Ads Pack (499000).
            RemoveAdsCard(250.5f);
            NoAdsPack(419.5f);
            const float D = 339.5f; // everything below moves down by the two no-ads cards
            Bundle(277 + D, "Shop/bg-starterPack-shop", "Shop/bg-slot-starterPack-shop", "Starter Pack", 2500, 1, "299000 VND");
            Bundle(474 + D, "Shop/bg-smallBundle-shop", "Shop/bg-slot-smallBundle-shop", "Small Bundle", 5000, 2, "599000 VND");
            Bundle(671 + D, "Shop/bg-mediumBundle-shop", "Shop/bg-slot-smallBundle-shop", "Medium Bundle", 8000, 4, "999000 VND");
            Bundle(868 + D, "Shop/bg-largeBundle-shop", "Shop/bg-slot-smallBundle-shop", "Large Bundle", 12000, 6, "1199000 VND");

            // Coin packs (reward_coin_preset).
            SectionHeader(1013 + D, "Shop/bg-coinSectionHeader-shop", "COINS");
            int[] coins = { 500, 2500, 5000, 12500, 25000, 50000 };
            string[] prices = { "25000 VND", "75000 VND", "125000 VND", "249000 VND", "499000 VND", "999000 VND" }; // estimates
            for (int i = 0; i < coins.Length; i++)
                CoinCell(new[] { 63f, 167f, 270f }[i % 3], i < 3 ? 1130 + D : 1298 + D, "Shop/icon-coin" + (i + 1) + "-shop", coins[i], prices[i]);

            // Boosters for coins and the booster bundle.
            // Recording: Flare x3 400, Swap x3 1300, Select x3 2000, all three x5 for 6000.
            SectionHeader(1425 + D, "Shop/bg-boosterSectionHeader-shop", "BOOSTERS");
            int[] boosterPrices = { 400, 1300, 2000 };
            for (int i = 0; i < 3; i++) BoosterCell(new[] { 63f, 167f, 270f }[i], 1542 + D, i, 3, boosterPrices[i]);
            BigBoosterBundle(1720 + D, 5, 6000);
            // The list ends well above the navbar: opened at its end, the BOOSTERS header sits right under the title
            // (recording 2, 12 s: header top at 460 px).
            scroll.Max = Mathf.Max(0, (1820 + D - 154) * ShopUnit - (ShopTop + 3.9f));

            // Fixed header drawn over the scrolling list.
            // Fixed SAND SHOP header under the Home top bar, drawn over the scrolling list.
            // Original: the band spans ~80% of the screen, the title tab ~45%.
            // Recording 2 (12 s): the title tab 332 x 174 px centred at 353 px, the band behind it 100 px tall, the
            // title 49 px high (were 385 x 130 at 307, 74 and 43).
            const float HeaderY = 3.44f;
            var header = ShopArt("Shop/bg-header1-shop", 168, 0, 4.2f, -7.4f, null); header.transform.localPosition = new Vector3(0, HeaderY, -7.4f);
            header.transform.localScale = Vector3.Scale(header.transform.localScale, new Vector3(.94f, 1.34f, 1));
            // The band ends at the Home top bar's lower edge (y 4.2): it used to reach up to 4.9 and cover the bottom
            // half of the copied top bar (settings, lives, coins), which the original keeps whole.
            // Page colour from the top bar down to the header band's lower edge (400 px), so the list never shows
            // above it.
            const float HeaderBottom = 3.135f;
            var bar = Child<SpriteRenderer>(shop, "Header bar", new Vector3(0, (HeaderBottom + 4.2f) * .5f, -7.3f));
            bar.sprite = SpriteFactory.White; bar.color = backdrop.color; bar.transform.localScale = new Vector3(8, 4.2f - HeaderBottom, 1);
            SetText(OutlinedLabel(shop, "Shop title", new Vector3(0, HeaderY, -7.6f), .055f), "SAND SHOP");
            // Bottom strip behind the navbar so scrolled cards never show under it.
            var foot = Child<SpriteRenderer>(shop, "Footer bar", new Vector3(0, -5.1f, -7.3f)); foot.sprite = bar.sprite;
            foot.color = bar.color; foot.transform.localScale = new Vector3(8, 1.8f, 1);
            AddPageChrome(shop, "icon-shop");
            PopupScreens.HideCoinBar(shop);
        }

        SpriteRenderer ShopArt(string sprite, float x, float y, float width, float z, Transform parent)
        {
            var r = Art(shop, sprite, new Vector3(ShopX(x), ShopY(y), z), width);
            if (parent) r.transform.SetParent(parent, true);
            return r;
        }
        // Art stretched to width x height (canvas pixels), e.g. 9-sliced-looking panels and buttons.
        SpriteRenderer ShopPanel(string sprite, float x, float y, float width, float height, float z)
        {
            var r = ShopArt(sprite, x, y, width * ShopUnit, z, shopContent);
            r.SetLocalSize(width * ShopUnit, height * ShopUnit);
            return r;
        }
        // The three booster icons in a row with an "xN" count at each icon's lower right.
        void BoosterRow(float x0, float dx, float y, float iconSize, string count, float countDx, float countDy, float countSize)
        {
            for (int i = 0; i < 3; i++)
            {
                float x = x0 + i * dx;
                ShopArt(BoosterIcons[i], x, y, iconSize * ShopUnit, -6.5f, shopContent);
                ShopText(count, x + countDx, y + countDy, countSize, -6.6f, true, Color.white);
            }
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
            // Original price buttons: chunky (about a third as tall as wide) with a big caption.
            ShopPanel("Shop/button-green", x, y, w, 45, -6.7f);
            if (coinIcon) ShopArt("Shop/icon-coin_1", x - w * .3f, y, 26 * ShopUnit, -6.8f, shopContent);
            ShopText(text, x + (coinIcon ? 10 : 0), y - 1, text.Length > 9 ? .042f : .046f, -6.9f, true, Color.white);
            ShopHit(x, y, w, 46, action);
        }

        void RemoveAdsCard(float y)
        {
            ShopArt("Shop/bg-NoAds-shop", 168, y, 300 * ShopUnit, -6.3f, shopContent);
            ShopArt("Shop/icon-noAds", 55, y - 12, 62 * ShopUnit, -6.5f, shopContent);
            ShopText("REMOVE ADS", 190, y - 40, .04f, -6.6f, true, Color.white);
            ShopText("Have more fun without advertisements", 190, y - 17, .022f, -6.6f, true, Color.white);
            PriceButton(190, y + 30, 135, "249000 VND", "shop-iap", false);
        }
        void NoAdsPack(float y)
        {
            ShopArt("Shop/bg-NoAdsPack-shop", 168, y, 300 * ShopUnit, -6.3f, shopContent);
            ShopText("No Ads Pack", 168, y - 74, .04f, -6.6f, true, Color.white);
            ShopArt("Shop/icon-noAds", 55, y - 20, 62 * ShopUnit, -6.5f, shopContent);
            ShopArt("Shop/bg-slot-noAdsPack-shop_0", 203, y - 21, 197 * ShopUnit, -6.4f, shopContent);
            ShopArt("Shop/icon-coin_1", 128, y - 27, 30 * ShopUnit, -6.5f, shopContent);
            ShopText("1000", 128, y - 4, .026f, -6.6f, true, Color.white);
            BoosterRow(173, 44, y - 27, 32, "x3", 4, 21, .027f);
            PriceButton(167, y + 53, 135, "499000 VND", "shop-iap", false);
        }
        void Bundle(float y, string card, string slot, string title, int coins, int each, string price)
        {
            ShopArt(card, 168, y, 300 * ShopUnit, -6.3f, shopContent);
            ShopText(title, 168, y - 74, .04f, -6.6f, true, Color.white);
            ShopArt("Shop/icon-coin2-shop", 60, y - 22, 52 * ShopUnit, -6.5f, shopContent);
            ShopText(coins.ToString(), 60, y + 10, .034f, -6.7f, true, Color.white);
            ShopArt(slot, 203, y - 21, 197 * ShopUnit, -6.4f, shopContent);
            BoosterRow(145, 57, y - 27, 36, "x" + each, 4, 21, .03f);
            PriceButton(167, y + 53, 135, price, "shop-iap", false);
        }
        void SectionHeader(float y, string sprite, string title)
        {
            ShopPanel(sprite, 168, y, 300, 43, -6.3f);
            ShopText(title, 168, y - 1, .042f, -6.5f, true, Color.white);
        }
        // One small shop cell (coins or boosters): card, inner slot and a touch area for the whole cell.
        void SectionCell(string section, float x, float y, string action)
        {
            ShopPanel("Shop/bg-" + section + "-shop", x, y, 92, 140, -6.3f);
            ShopPanel("Shop/bg-slot-" + section + "-shop", x, y - 15, 80, 95, -6.4f);
            ShopHit(x, y, 92, 140, action);
        }
        void CoinCell(float x, float y, string icon, int coins, string price)
        {
            SectionCell("coinSection", x, y, "shop-iap");
            ShopArt(icon, x, y - 28, 52 * ShopUnit, -6.5f, shopContent);
            ShopText(coins.ToString(), x, y + 12, .032f, -6.6f, true, Color.white);
            ShopText(price, x, y + 50, .032f, -6.6f, true, Color.white);
        }
        void BoosterCell(float x, float y, int booster, int count, int price)
        {
            SectionCell("boosterSection", x, y, "shop-buy:" + BoosterActions[booster] + ":" + count + ":" + price);
            // Recording 2 (12 s): a larger icon with its count over its lower part, a larger price.
            ShopArt(BoosterIcons[booster], x, y - 26, 62 * ShopUnit, -6.5f, shopContent);
            ShopText("x" + count, x - 4, y - 4, .04f, -6.6f, true, Color.white);
            ShopArt("Shop/icon-coin_1", x - 30, y + 48, 22 * ShopUnit, -6.6f, shopContent);
            ShopText(price.ToString(), x + 8, y + 48, .042f, -6.6f, true, Color.white);
        }
        void BigBoosterBundle(float y, int each, int price)
        {
            ShopPanel("Shop/bg-boosterSection-big-shop", 168, y, 300, 160, -6.3f);
            ShopPanel("Shop/bg-slot-boosterSection-big-shop", 168, y - 30, 270, 70, -6.4f);
            BoosterRow(85, 83, y - 34, 56, "x" + each, 0, 16, .04f);
            PriceButton(167, y + 42, 135, price.ToString(), "shop-buy:all:" + each + ":" + price, true);
        }

        public void OpenShop()
        {
            if (Open) return;
            // The original opens at the top of the list (recording 2, 3.45 s: SAND SHOP, Remove Ads); the player then
            // scrolled down to BOOSTERS (6.2 s), the frames once taken for the opening view.
            scroll.Reset(); shopContent.localPosition = Vector3.zero;
            Show(shop); RefreshPageLives(shop);
        }
        void RefreshShopCoins() { RefreshPopupCoins(); }

        // Shop buttons fire on release without a drag, so scrolling never buys anything by accident.
        public override bool Handles(string action) { return action.StartsWith("shop-"); }
        public override bool Handle(string action) { scroll.Hold(action); return true; }
        void RunShopAction(string action)
        {
            // No messages on screen (original): real-money packs do nothing here, a booster bought shows in the counters.
            if (action == "shop-iap") return;
            var parts = action.Split(':'); // shop-buy:<booster|all>:<count>:<price>
            int count = int.Parse(parts[2]), price = int.Parse(parts[3]);
            if (!EconomyManager.TrySpend(price, "shop:" + parts[1])) return;
            foreach (var booster in parts[1] == "all" ? BoosterActions : new[] { parts[1] }) EconomyManager.AddBooster(booster, count, "shop");
            RefreshShopCoins();
        }

        public override void Tick()
        {
            if (Open != shop.Root) return;
            scroll.Tick(screen.UiCamera, RunShopAction);
            shopContent.localPosition = new Vector3(0, scroll.Value, 0);
        }
    }
}
