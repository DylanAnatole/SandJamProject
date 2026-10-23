using System.Collections.Generic;
using UnityEngine;

namespace SandJamTest.Scene3D
{
    // Original CONGRATS card (current build), shown after a win before the next level loads: yellow ribbon with
    // light rays, gold-framed glass card, the gallery collection name, its cover revealed in colour from the bottom
    // up as levels are won, "N% Completed" and "Tap To Continue" anywhere on screen.
    public sealed class CongratsPopup : PopupModule
    {
        Popup congrats;
        SpriteRenderer cardBw, cardColor; Transform congratsRays; TextMesh[] cardName, cardPercent;
        const float CoverWidth = 2.5f, CoverHeight = 3.15f, CoverY = .17f;
        const float CardShortenPx = 116;
        static readonly Color RaysColor = Color.white; // the art at full strength (its own yellow)
        // Recording 1 (774.75-774.87 s): the CONGRATS card snaps open in ~0.12 s, easing out.
        public bool FastPopIn { get { return IsOpen(congrats); } }
        public override void Build()
        {
            BuildReward();
            congrats = Blank("Congrats popup");
            congrats.Root.transform.Find("Next").gameObject.SetActive(false); // the whole screen continues instead
            // The original card sits on an even, opaque dark navy (#141225): nothing of the board shows.
            congrats.Root.transform.Find("Dark backdrop").GetComponent<SpriteRenderer>().color = new Color32(20, 18, 37, 255);
            PopupScreens.DimCoinBar(congrats, .38f); // greyed like the rest of the HUD under it (recording 2, 145 s)
            // Recording 2 (145 s): bright yellow rays (median #DDC52D), reaching higher above the ribbon.
            var rays = Art(congrats, "Congrats/light-congrats", new Vector3(0, 2.8f, -6.1f), 4.4f); // centred on the ribbon, rays reaching high above it
            rays.color = RaysColor; congratsRays = rays.transform;
            // The original card is shorter: its bottom edge ends ~116 px higher, the top staying under the ribbon.
            var card = Art(congrats, "Congrats/bg-popUp-congrats-v2", new Vector3(0, .06f, -6.2f), 3.86f);
            float cut = CardShortenPx * VideoScreen.Px, cardHeight = card.bounds.size.y;
            card.transform.localScale = Vector3.Scale(card.transform.localScale, new Vector3(1, (cardHeight - cut) / cardHeight, 1));
            card.transform.position += Vector3.up * cut * .5f;
            cardBw = Child<SpriteRenderer>(congrats, "Cover grey", new Vector3(0, CoverY, -6.4f));
            cardColor = Child<SpriteRenderer>(congrats, "Cover colour", new Vector3(0, CoverY, -6.45f));
            // Ribbon, title and captions placed as measured on recording 2 (145 s).
            Ribbon(congrats, 2.59f);
            cardName = OutlinedLabel(congrats, "Collection name", new Vector3(0, 1.98f, -6.8f), .042f);
            cardPercent = OutlinedLabel(congrats, "Collection percent", new Vector3(0, -1.574f, -6.8f), .037f);
            SetText(OutlinedLabel(congrats, "Tap to continue", new Vector3(0, -3.32f, -6.8f), .044f), "Tap To Continue");
            // The whole screen continues, in front of everything else in the popup.
            var hit = Child<BoxCollider>(congrats, "Hit area - congrats-continue", new Vector3(0, 0, -9.5f));
            hit.gameObject.layer = 9; hit.size = new Vector3(30, 40, .2f);
            hit.gameObject.AddComponent<VideoUiButton>().Action = "congrats-continue";
        }
        void Ribbon(Popup p, float y)
        {
            Art(p, "Congrats/ribbon-yellow-v2", new Vector3(0, y, -6.6f), 4.74f);
            Art(p, "Congrats/text-congrats-v2", new Vector3(0, y + .06f, -6.7f), 3.17f);
        }

        // Gallery milestone reward (recording 3, 58.1-62 s): over the dimmed Gallery, the CONGRATS ribbon, a glowing coin
        // pile (or the gift) with its amount, "Get Rewards" and a coin x2 button. Measured at 59.3 s: ribbon at y 490,
        // pile ~400 px wide at y 960, amount at y 1080, buttons centred at x 242 / 655, y 1355.
        Popup reward; SpriteRenderer rewardCoins; TextMesh[] rewardAmount; int rewardCoinsShown; bool rewardIsGift;
        GameObject[] rewardButtons;
        static Vector3 Spot(float xPx, float yPx, float z) { return new Vector3((xPx - 444) * VideoScreen.Px, (960 - yPx) * VideoScreen.Px, z); }
        void BuildReward()
        {
            reward = Blank("Gallery reward popup");
            reward.Root.transform.Find("Next").gameObject.SetActive(false);
            reward.Root.transform.Find("Dark backdrop").GetComponent<SpriteRenderer>().color = new Color(0, 0, 0, .66f);
            PopupScreens.DimCoinBar(reward, .38f);
            SpinningLight.Add(reward.Root.transform, reward.Root.transform.TransformPoint(Spot(444, 930, -6.1f)), 4.2f);
            Ribbon(reward, (960 - 490) * VideoScreen.Px);
            rewardCoins = Art(reward, "VideoUI/icon-coin-reward", Spot(440, 955, -6.4f), 2.24f);
            rewardAmount = OutlinedLabel(reward, "Reward amount", Spot(444, 1080, -6.8f), .08f);
            BuildGift();
            int before = reward.Root.transform.childCount;
            Art(reward, "VideoUI/button-green-v2", Spot(242, 1355, -6.5f), 2.18f);
            SetText(OutlinedLabel(reward, "Get rewards label", Spot(242, 1352, -6.8f), .042f), "Get Rewards");
            Hit(reward, Spot(242, 1355, -8.5f), new Vector2(2.18f, .62f), "reward-collect");
            Art(reward, "VideoUI/button-yellow-v2", Spot(655, 1360, -6.5f), 1.68f);
            Art(reward, "VideoUI/icon-coin", Spot(600, 1360, -6.6f), .36f);
            SetText(OutlinedLabel(reward, "Double label", Spot(690, 1357, -6.8f), .05f), "X2");
            Hit(reward, Spot(655, 1360, -8.5f), new Vector2(1.68f, .62f), "reward-double");
            rewardButtons = new GameObject[reward.Root.transform.childCount - before];
            for (int i = 0; i < rewardButtons.Length; i++) rewardButtons[i] = reward.Root.transform.GetChild(before + i).gameObject;
        }

        // Last milestone: the original's gift (popup_gallery_reward_gift, clip gift_open, recording 3 at 63.6-65.5 s).
        // The box squashes to 48% and shakes, springs up to 135%, the lid flies off to the left turning 64 degrees, a
        // coin with the amount rises out of it, and the buttons come last. Box and lid are the prefab's art; positions
        // are its canvas units (box 291 wide, here 280 px: bottom of the box at y 1200, x 440).
        Transform giftRoot, giftLid, giftCoin;
        const float GiftUnitPx = 280f / 291f, GiftButtonsAt = 1.9f;
        static readonly float[] LidTimes = { 0, .833f, .917f, 1.033f, 1.117f }, LidX = { 7, 7, -91, -194, -241 }, LidY = { 316, 316, 417, 258, 46 };
        Vector3 GiftUnits(float x, float y, float z) { return new Vector3(x * GiftUnitPx * VideoScreen.Px, y * GiftUnitPx * VideoScreen.Px, z); }
        void BuildGift()
        {
            giftRoot = new GameObject("Gift").transform;
            giftRoot.SetParent(reward.Root.transform, false);
            giftRoot.localPosition = Spot(440, 1200, -6.4f);
            Transform Part(string sprite, float width, Vector3 at)
            {
                var r = Art(reward, sprite, Vector3.zero, width * GiftUnitPx * VideoScreen.Px);
                r.transform.SetParent(giftRoot, false); r.transform.localPosition = at;
                return r.transform;
            }
            Part("GalleryPage/gift-box", 291, GiftUnits(0, 165, 0));
            giftLid = Part("GalleryPage/gift-lid", 280, GiftUnits(7, 316, -.05f));
            giftCoin = new GameObject("Gift coin").transform;
            giftCoin.SetParent(reward.Root.transform, false);
            var coin = Art(reward, "VideoUI/icon-coin", Spot(444, 712, -6.6f), .55f);
            coin.transform.SetParent(giftCoin, true);
            foreach (var t in OutlinedLabel(reward, "Gift amount", Spot(444, 760, -6.8f), .04f)) t.transform.SetParent(giftCoin, true);
        }
        void TickGift(float t)
        {
            // Squash (chest_origin scale y) about the box bottom, a shake while squashed, then the spring.
            float sy = t < .167f ? Mathf.Lerp(1, .67f, t / .167f) : t < .317f ? Mathf.Lerp(.67f, .482f, (t - .167f) / .15f)
                : t < .733f ? .482f : t < .833f ? Mathf.Lerp(.482f, 1.35f, (t - .733f) / .1f) : Mathf.Lerp(1.35f, 1, Mathf.Clamp01((t - .833f) / .184f));
            float shake = t > .317f && t < .733f ? 1.6f + 2.8f * Mathf.Sin((t - .317f) * Mathf.PI * 20) : t >= .733f && t < .833f ? 4.2f * (.833f - t) / .1f : 0;
            giftRoot.localScale = new Vector3(1, sy, 1);
            giftRoot.localRotation = Quaternion.Euler(0, 0, shake);
            int k = 1; while (k < LidTimes.Length - 1 && t > LidTimes[k]) k++;
            float f = Mathf.Clamp01((t - LidTimes[k - 1]) / (LidTimes[k] - LidTimes[k - 1]));
            giftLid.localPosition = GiftUnits(Mathf.Lerp(LidX[k - 1], LidX[k], f), Mathf.Lerp(LidY[k - 1], LidY[k], f), -.05f);
            giftLid.localRotation = Quaternion.Euler(0, 0, 64.3f * Mathf.SmoothStep(0, 1, (t - .833f) / .284f));
            // The coin rises from the box mouth (y 920) to y 712 between 1.3 and 1.65 s.
            float c = Mathf.Clamp01((t - 1.3f) / .35f);
            giftCoin.gameObject.SetActive(c > 0);
            giftCoin.localPosition = new Vector3(0, -(920 - 712) * VideoScreen.Px * (1 - c) * (1 - c), 0);
            foreach (var b in rewardButtons) if (b.activeSelf != t >= GiftButtonsAt) b.SetActive(t >= GiftButtonsAt);
        }
        public void ShowGalleryReward(int coins, bool gift)
        {
            rewardCoinsShown = coins;
            rewardIsGift = gift;
            rewardCoins.gameObject.SetActive(!gift); giftRoot.gameObject.SetActive(gift);
            SetText(rewardAmount, gift ? "" : coins.ToString());
            SetText(giftCoin.GetComponentsInChildren<TextMesh>(true), coins.ToString());
            foreach (var b in rewardButtons) b.SetActive(true);
            giftCoin.gameObject.SetActive(false);
            Show(reward);
            if (gift) TickGift(0);
        }

        public override bool Handles(string action) { return action == "congrats-continue" || action.StartsWith("reward-"); }
        public override bool Handle(string action)
        {
            if (action == "congrats-continue") { Close(); screen.Levels.LoadNext(); return true; }
            // The original's x2 is a rewarded ad; here it simply doubles the coins, as on Level Complete.
            if (action == "reward-double") EconomyManager.Add(rewardCoinsShown, "double");
            Close(); Host.ResumeGallery(); return true;
        }

        public bool ShowCongrats()
        {
            int level = screen.Levels.PlayedNumber;
            var info = GalleryCollections.ForLevel(level);
            if (info == null) return false;
            Sprite colour, grey; CoverPair(info.Cover, info.RenderId, out colour, out grey);
            if (!colour || !grey) return false;
            colour = RoundedCopy(colour); grey = RoundedCopy(grey); // corners follow the rim; the glass shows around them
            SetText(cardName, info.Name);
            var size = grey.bounds.size;
            // The picture fills the rounded slot completely (original): it is fitted to the slot box on both axes
            // (the 84x112 renders are within a few percent of the slot's shape) and runs under the rim.
            float kx = CoverWidth / size.x, ky = CoverHeight / size.y;
            cardBw.sprite = grey; cardBw.transform.localScale = new Vector3(kx, ky, 1);
            // Cover sprites do not all pivot on their centre (the picture drifted right and down out of the slot):
            // each one is centred on the slot through its own bounds.
            cardBw.transform.localPosition = Centred(grey, kx, ky, cardBw.transform.localPosition.z);
            // Original: the colour cover sits exactly on the grey one and a hard horizontal edge rises from the
            // previous completion to the new one (about 0.8 s, right after the card pops in).
            cardColor.sprite = colour;
            float cx = CoverWidth / colour.bounds.size.x, cy = CoverHeight / colour.bounds.size.y;
            cardColor.transform.localScale = new Vector3(cx, cy, 1);
            cardColor.transform.localPosition = Centred(colour, cx, cy, cardColor.transform.localPosition.z);
            cardColor.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            cardColor.gameObject.SetActive(true);
            // Original: the picture sits in a rounded slot with a soft lavender rim.
            if (!coverFrame)
            {
                coverFrame = Child<SpriteRenderer>(congrats, "Cover frame", new Vector3(0, CoverY, -6.5f));
                coverFrame.sprite = RoundedFrame(); runtimeArt.Add(coverFrame.sprite); runtimeArt.Add(coverFrame.sprite.texture);
            }
            const float rim = .06f;
            coverFrame.SetLocalSize(CoverWidth + rim, CoverHeight + rim);
            if (!coverMask)
            {
                coverMask = Child<SpriteMask>(congrats, "Cover fill mask", new Vector3(0, 0, -6.45f));
                coverMask.sprite = SpriteFactory.WhiteBottom;
            }
            coverBottom = CoverY - CoverHeight / 2; coverWidth = CoverWidth; coverHeight = CoverHeight;
            int completed = info.Completed(level);
            fillTo = (float)completed / info.Count;
            fillFrom = (float)Mathf.Max(0, completed - 1) / info.Count;
            percentTo = info.Percent(level); percentFrom = info.Count == 0 ? 0 : 100 * Mathf.Max(0, completed - 1) / info.Count;
            SetCoverFill(fillFrom, percentFrom);
            // The card replaces the Level Complete page (same depth); the next level loads right after it.
            screen.Result.SetActive(false);
            Show(congrats);
            return true;
        }

        static Vector3 Centred(Sprite sprite, float kx, float ky, float z)
        {
            var c = sprite.bounds.center;
            return new Vector3(-c.x * kx, CoverY - c.y * ky, z);
        }
        SpriteRenderer coverFrame;
        // Rim texture for a 3:4 picture: transparent inside, lavender rim, card-coloured corners outside the rounding.
        static Sprite RoundedFrame()
        {
            const int w = 180, h = 236, border = 6; const float radius = 22;
            var card = new Color(.89f, .93f, .99f, 1); var rimColor = new Color(.62f, .62f, .93f, 1); var edge = new Color(.45f, .44f, .8f, 1);
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                float d = SpriteFactory.RoundedRectDistance(x, y, w, h, radius); // the outer edge is the texture border
                Color c;
                if (d > 0) c = Color.clear;                            // outside the rounding: the glass (the picture has rounded corners)
                else if (d > -1.5f) c = edge;                          // thin darker outline
                else if (d > -border) c = rimColor;                    // lavender rim
                else c = Color.clear;                                  // the picture shows through
                pixels[y * w + x] = c;
            }
            texture.SetPixels(pixels); texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(.5f, .5f), 100);
        }
        SpriteMask coverMask; float coverBottom, coverWidth, coverHeight, fillFrom, fillTo; int percentFrom, percentTo, shownPercent = -1;
        void SetCoverFill(float fraction, int percent)
        {
            coverMask.transform.localPosition = new Vector3(0, coverBottom, coverMask.transform.localPosition.z);
            coverMask.transform.localScale = new Vector3(coverWidth * 1.02f, Mathf.Max(.0001f, coverHeight * fraction), 1);
            if (percent != shownPercent) { shownPercent = percent; SetText(cardPercent, percent + "% Completed"); }
        }
        public override void Tick()
        {
            if (IsOpen(reward) && rewardIsGift) TickGift(Time.unscaledTime - ShownAt);
            if (!IsOpen(congrats)) return;
            if (congratsRays) congratsRays.localRotation = Quaternion.Euler(0, 0, Time.unscaledTime * 12f);
            if (!coverMask) return;
            float t = Mathf.Clamp01((Time.unscaledTime - ShownAt - .3f) / .8f);
            t = t * t * (3 - 2 * t);
            SetCoverFill(Mathf.Lerp(fillFrom, fillTo, t), Mathf.RoundToInt(Mathf.Lerp(percentFrom, percentTo, t)));
        }
    }
}
