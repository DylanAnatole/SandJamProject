using System.Collections.Generic;
using UnityEngine;

namespace SandJamTest.Scene3D
{
    // GALLERY page as in the current build (gameplay recording 2026-10-08) on the original canvas_gallery art:
    // category tab, a carousel of collection covers (selected one in the middle, neighbours peeking at the
    // sides, arrows), the collection bar with three milestone rewards (ticked once claimed), "Claim All Cards",
    // and a scrolling 3-column grid of the collection's pictures. Home top bar and navbar stay visible.
    public sealed class GalleryPopup : PopupModule
    {
        Popup gallery;
        readonly List<GameObject> galleryDynamic = new List<GameObject>();
        Transform galleryGrid;
        List<GalleryCollections.Info> galleryList;
        int galleryIndex;
        readonly DragScroller scroll = new DragScroller();
        const float GridTop = .02f, GridBottom = -4.2f;

        // Moves a built piece under parent (when given) and records it for the next refresh to destroy.
        T Track<T>(T piece, Transform parent) where T : Component
        {
            if (parent) piece.transform.SetParent(parent, true);
            galleryDynamic.Add(piece.gameObject);
            return piece;
        }
        SpriteRenderer GalleryArt(string sprite, float x, float y, float w, float h, float z, Transform parent = null)
        {
            var r = Art(gallery, sprite.Contains("/") ? sprite : "GalleryPage/" + sprite, new Vector3(x, y, z), w);
            r.SetLocalSize(w, h);
            return Track(r, parent);
        }
        // Picture size inside a card frame as actually drawn: the frame's measured size minus its rim (the nominal
        // sizes are kept as the fallback and as the rim proportion).
        Vector2 FrameInner(SpriteRenderer frame, float w, float h)
        {
            if (!frame || !frame.sprite) return new Vector2(w, h);
            var size = frame.bounds.size; var ls = gallery.Root.transform.lossyScale;
            float fw = size.x / ls.x, fh = size.y / ls.y, rim = Mathf.Min(fw, fh) * .055f;
            return new Vector2(fw - 2 * rim, fh - 2 * rim);
        }
        float FrameCentreY(SpriteRenderer frame)
        {
            return frame && frame.sprite ? gallery.Root.transform.InverseTransformPoint(frame.bounds.center).y : 2.57f;
        }
        SpriteRenderer GalleryPicture(Sprite sprite, float x, float y, float maxW, float maxH, float z, Transform parent = null)
        {
            var r = Child<SpriteRenderer>(gallery, "Picture", new Vector3(x, y, z)); r.sprite = sprite;
            if (sprite) { float k = Mathf.Min(maxW / sprite.bounds.size.x, maxH / sprite.bounds.size.y); r.transform.localScale = new Vector3(k, k, 1); }
            return Track(r, parent);
        }
        // Original cards: the picture fills the card's opening and is clipped to its rounded shape by the card
        // mask, leaving the blue slot frame around it.
        SpriteRenderer MaskedPicture(Sprite sprite, float x, float y, float w, float h, float z, Transform parent = null)
        {
            var maskSprite = Resources.Load<Sprite>("GalleryPage/bg-cardSlot-cardCollection_mask");
            // Everything is centred on (x, y) by its drawn bounds, whatever the sprites' pivots (the slot and render
            // pivots are not centred; trusting them left the mask ~0.1 above the picture, cutting its bottom off).
            var target = gallery.Root.transform.TransformPoint(new Vector3(x, y, z));
            var r = Child<SpriteRenderer>(gallery, "Picture", new Vector3(x, y, z)); r.sprite = sprite;
            if (sprite)
            {
                float k = Mathf.Max(w / sprite.bounds.size.x, h / sprite.bounds.size.y); r.transform.localScale = new Vector3(k, k, 1);
                var c = r.bounds.center; r.transform.position += new Vector3(target.x - c.x, target.y - c.y, 0);
            }
            if (maskSprite)
            {
                r.sharedMaterial = SpriteFactory.PlainSprite;
                r.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                var m = Child<SpriteMask>(gallery, "Picture mask", new Vector3(x, y, z)); m.sprite = maskSprite;
                // The mask art's opaque card covers columns 12-311 and rows 12-383 (top down) of its 324x450 texture
                // (a shadow strip below): scale it so the card spans w x h and put that card's centre on (x, y).
                float ppu = maskSprite.pixelsPerUnit;
                m.transform.localScale = new Vector3(w / (300f / ppu), h / (372f / ppu), 1);
                var mb = m.bounds;
                var card = new Vector2(mb.min.x + mb.size.x * (12f + 312f) / 2f / 324f, mb.max.y - mb.size.y * (12f + 384f) / 2f / 450f);
                m.transform.position += new Vector3(target.x - card.x, target.y - card.y, 0);
                Track(m, parent);
            }
            return Track(r, parent);
        }
        TextMesh[] GalleryText(string text, float x, float y, float size, float z, Transform parent = null)
        {
            var labels = OutlinedLabel(gallery, "Gallery text", new Vector3(x, y, z), size);
            SetText(labels, text); Track(labels[0], parent);
            return labels;
        }
        void GalleryHit(float x, float y, float w, float h, string action, Transform parent = null)
        {
            var hit = Hit(gallery, new Vector3(x, y, -8.5f), new Vector2(w, h), action);
            if (parent) hit.transform.SetParent(parent, true);
            galleryDynamic.Add(hit);
        }

        public override bool IsPage(GameObject root) { return gallery != null && root == gallery.Root; }
        public override void Build()
        {
            gallery = Blank("Gallery page");
            gallery.RightButton.transform.parent.gameObject.SetActive(false);
            PopupScreens.HideCoinBar(gallery);
            var backdrop = gallery.Root.transform.Find("Dark backdrop").GetComponent<SpriteRenderer>();
            backdrop.color = new Color(.1f, .3f, .55f, 1);
            var top = Art(gallery, "GalleryPage/bg-topTexture-cardCollection", new Vector3(0, 3.2f, -6.05f), 5.8f);
            var bottom = Art(gallery, "GalleryPage/bg-bottom-cardCollection", new Vector3(0, -2.4f, -6.08f), 5.8f);
            bottom.SetLocalSize(5.8f, 5.6f);
            AddPageChrome(gallery, "icon-cards");
        }

        // Cover carousel measured on recording 2 (30 s): the selected card's blue frame 340 x 384 px centred at 500 px
        // (ours read 284 x 374 at 523), the neighbours further out.
        const float CardGrowX = 1.2f, CardGrowY = 1.03f, CarouselY = 2.7f, SideX = 2.72f;
        public void OpenGallery()
        {
            if (Open) return;
            galleryList = GalleryCollections.Unlocked();
            galleryIndex = Mathf.Max(0, galleryList.Count - 1);
            Show(gallery); RefreshPageLives(gallery);
            scroll.Value = 0; RefreshGallery();
        }

        // Back from the milestone reward: same collection, same scroll.
        public void ResumeGallery() { Show(gallery); RefreshPageLives(gallery); RefreshGallery(); }

        void RefreshGallery()
        {
            DestroyAll(galleryDynamic);
            cardPieces.Clear();
            if (galleryList.Count == 0) { GalleryText("Beat level 15 to unlock\nyour first collection!", 0, 1.5f, .05f, -6.6f); return; }
            var info = galleryList[galleryIndex];
            int won = Campaign.LevelNumber - 1;
            // Category tab: hangs from the top bar with a smaller caption (original gallery, recording 2026-10-08).
            GalleryArt("header-top-cardCollection", 0, 4.0f, 3.2f, .44f, -6.3f);
            GalleryText(info.StyleName.ToUpperInvariant(), 0, 4.02f, .032f, -6.5f);
            // Carousel: neighbours first so the selected card draws on top.
            int carouselFrom = galleryDynamic.Count;
            for (int side = -1; side <= 1; side += 2)
            {
                int n = galleryIndex + side; if (n < 0 || n >= galleryList.Count) continue;
                var sideFrame = GalleryArt("bg-cards-top-carCollection", side * SideX, CarouselY, 1.45f * CardGrowX, 1.8f * CardGrowY, -6.25f);
                float sy = FrameCentreY(sideFrame); var ss = FrameInner(sideFrame, 1.33f * CardGrowX, 1.67f * CardGrowY);
                Sprite c, g; StyleCover(galleryList[n], out c, out g);
                MaskedPicture(c, side * SideX, sy, ss.x, ss.y, -6.27f);
            }
            // The card art does not pivot on its centre: pictures are centred on the frame as drawn (they used to sit
            // high, overflowing the top edge with a gap at the bottom).
            var mainFrame = GalleryArt("bg-cards-top-carCollection", 0, CarouselY, 1.72f * CardGrowX, 2.13f * CardGrowY, -6.3f);
            float cy = FrameCentreY(mainFrame); var cs = FrameInner(mainFrame, 1.58f * CardGrowX, 1.98f * CardGrowY);
            Sprite colour, grey; StyleCover(info, out colour, out grey);
            float fraction = (float)info.Completed(won) / info.Count;
            if (fraction >= 1 || !grey) MaskedPicture(colour, 0, cy, cs.x, cs.y, -6.35f);
            else
            {
                // Grey cover with the coloured part rising from the bottom, both clipped to the rounded card.
                var g = MaskedPicture(grey, 0, cy, cs.x, cs.y, -6.35f);
                if (fraction > 0 && colour)
                {
                    var rect = colour.textureRect;
                    var slice = Sprite.Create(colour.texture, new Rect(rect.x, rect.y, rect.width, Mathf.Max(1, Mathf.Round(rect.height * fraction))), new Vector2(.5f, 0), colour.pixelsPerUnit);
                    runtimeArt.Add(slice);
                    var c = GalleryPicture(slice, 0, 0, 1, 1, -6.4f);
                    c.sharedMaterial = g.sharedMaterial; c.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                    c.transform.localScale = g.transform.localScale;
                    c.transform.localPosition = new Vector3(0, cy - g.bounds.size.y / 2, -6.4f);
                }
            }
            carousel.Clear(); carouselX.Clear();
            for (int i = carouselFrom; i < galleryDynamic.Count; i++) { carousel.Add(galleryDynamic[i].transform); carouselX.Add(galleryDynamic[i].transform.localPosition.x); }
            if (galleryIndex > 0) GalleryArrow(-1);
            if (galleryIndex < galleryList.Count - 1) GalleryArrow(1);
            // Collection bar with milestone rewards.
            GalleryArt("bg-header-middle-cardCollection", 0, 1.0f, 5.4f, .86f, -6.3f);
            GalleryArt("bg-fillbar-middle-cardCollection", 0, .97f, 4.5f, .32f, -6.35f);
            if (fraction > 0) GalleryArt("fillbar-middle-cardCollection", -2.25f + 2.25f * fraction, .97f, 4.45f * fraction, .27f, -6.4f);
            float[] mx = { -.88f, .65f, 1.99f };
            for (int m = 0; m < 3; m++)
            {
                bool claimed = GalleryCollections.MilestoneClaimed(info, m), reached = GalleryCollections.MilestoneReached(info, m);
                GalleryArt("bg-reward", mx[m], 1.24f, .42f, .5f, -6.45f);
                if (claimed) GalleryArt("Tasks/icon-done", mx[m], 1.27f, .32f, .27f, -6.5f);
                else if (m == 2) GalleryArt("icon-gift", mx[m], 1.27f, .26f, .32f, -6.5f);
                else { GalleryArt("Shop/icon-coin_1", mx[m], 1.32f, .2f, .2f, -6.5f); GalleryText(GalleryCollections.MilestoneCoins(info, m).ToString(), mx[m], 1.17f, .024f, -6.55f); }
                if (reached && !claimed)
                {
                    var glow = GalleryArt("Lives/light", mx[m], 1.27f, .75f, .75f, -6.42f); glow.color = new Color(1, .9f, .4f, .8f);
                    GalleryHit(mx[m], 1.24f, .55f, .62f, "gallery-claim:" + m);
                }
            }
            GalleryArt("Group 10298", 0, .34f, 1.9f, .48f, -6.3f).color = new Color(.61f, .63f, .65f); // original: a steel-blue pill (#39639F, recording 2, 30 s)
            GalleryText("Claim All Cards", 0, .35f, .036f, -6.4f);
            GalleryHit(0, .34f, 1.9f, .5f, "gallery-claim-all");
            // Picture grid (3 columns), scrolled inside the band between the button and the navbar.
            galleryGrid = new GameObject("Gallery grid") { layer = gallery.Root.layer }.transform;
            galleryGrid.SetParent(gallery.Root.transform, false);
            galleryDynamic.Add(galleryGrid.gameObject);
            // Measured on recording 2 (30 s): cards 223 x 272 px, columns 288 px and rows 305 px apart, the first row's
            // top at 970 px.
            const float cw = 1.247f, ch = 1.70f, gx = 1.6f, gy = 1.708f, firstRow = -.81f;
            for (int i = 0; i < info.Count; i++)
            {
                int level = info.Start + i; bool done = level <= won;
                int firstPiece = galleryGrid.childCount;
                float x = (i % 3 - 1) * gx, y = firstRow - (i / 3) * gy;
                bool waiting = done && GalleryCollections.CardWaiting(level);
                // Inside the slot frame's blue rim: rows 12-383 and columns 12-311 of the 324x450 art (measured top down;
                // the art has a transparent strip at the bottom, so that area sits above the sprite's centre).
                float vw = cw * 300f / 324f, vh = ch * 372f / 450f, vy = y + ch * (.5f - (12f + 384f) / 2f / 450f), rim = cw * .025f;
                if (waiting) { ClaimCard(level, x, y, vy, vw, vh, cw, ch); KeepCard(level, x, firstPiece); continue; }
                var slot = GalleryArt(done ? "bg-cardSlot-cardCollection" : "bg-cardSlot-cover-cardCollection", x, y, cw, ch, -6.3f, galleryGrid);
                if (!done) continue;
                vy = InnerCardY(slot);
                string id = System.Text.RegularExpressions.Regex.Replace(Campaign.Levels[(level - 1) % Campaign.Levels.Length], "_Dupe$", "");
                MaskedPicture(Resources.Load<Sprite>("LevelRenders/" + id), x, vy, vw - 2 * rim, vh - 2 * rim, -6.35f, galleryGrid);
                KeepCard(level, x, firstPiece);
            }
            int rows = (info.Count + 2) / 3;
            scroll.Max = Mathf.Max(0, -firstRow + rows * gy - (GridTop - GridBottom));
            ClipGrid();
        }

        // Centre of a card slot's inner card (rows 12-383 of the 450 px art) as drawn: the slot art's pivot is not
        // centred, so this is taken from its bounds, in gallery-root space.
        float InnerCardY(SpriteRenderer slot)
        {
            var b = slot.bounds;
            return gallery.Root.transform.InverseTransformPoint(new Vector3(0, b.max.y - b.size.y * (12f + 384f) / 2f / 450f, 0)).y;
        }

        // A won level's card still face down (original): the "?" card in a gold frame with a "Claim" strip.
        void ClaimCard(int level, float x, float y, float cardY, float cardW, float cardH, float cw, float ch)
        {
            cardY = InnerCardY(GalleryArt("bg-cardSlot-cover-cardCollection", x, y, cw, ch, -6.3f, galleryGrid));
            var gold = Track(Child<SpriteRenderer>(gallery, "Claim frame", new Vector3(x, cardY, -6.29f)), galleryGrid);
            gold.sprite = SpriteFactory.Rounded(64, 22, 260); gold.drawMode = SpriteDrawMode.Sliced;
            gold.size = new Vector2(cardW + .14f, cardH + .14f); gold.color = new Color(1f, .78f, .22f);
            GalleryArt("GalleryPage/Group 10298", x, cardY - cardH * .36f, cardW * .9f, .3f, -6.36f, galleryGrid);
            GalleryText("Claim", x, cardY - cardH * .36f, .032f, -6.4f, galleryGrid);
            GalleryHit(x, y, cw, ch, "gallery-card:" + level, galleryGrid);
        }

        void GalleryArrow(int direction)
        {
            float x = 1.28f * direction; // just clear of the larger card (recording 2)
            GalleryArt("bg-arrow-cardCollection", x, CarouselY - .1f, .46f, .46f, -6.5f);
            var arrow = GalleryArt("icon-arrow-left-cardCollection", x - .02f * direction, CarouselY - .09f, .22f, .33f, -6.55f);
            if (direction > 0) arrow.flipX = true;
            GalleryHit(x, CarouselY - .1f, .65f, .65f, direction < 0 ? "gallery-prev" : "gallery-next");
        }

        void StyleCover(GalleryCollections.Info info, out Sprite colour, out Sprite grey) { CoverPair(info.StyleCover, info.StyleRenderId, out colour, out grey); }

        // Cards scrolled out of the grid band are hidden (the original uses a mask).
        void ClipGrid()
        {
            if (!galleryGrid) return;
            galleryGrid.localPosition = new Vector3(0, scroll.Value, 0);
            // A card (1.46 tall) shows while its centre keeps it mostly inside the band.
            foreach (Transform card in galleryGrid)
            {
                float y = card.position.y;
                card.gameObject.SetActive(y < GridTop - .55f && y > GridBottom + .55f);
            }
        }

        public override bool Handles(string action) { return action.StartsWith("gallery-"); }
        public override bool Handle(string action) { scroll.Hold(action); return true; }
        void RunGalleryAction(string action)
        {
            var info = galleryList[galleryIndex];
            if (flipPhase == 0 && StartFlip(action, info)) return;
            int before = galleryIndex;
            if (action == "gallery-prev") galleryIndex = Mathf.Max(0, galleryIndex - 1);
            else if (action == "gallery-next") galleryIndex = Mathf.Min(galleryList.Count - 1, galleryIndex + 1);
            else if (action == "gallery-claim-all") GalleryCollections.ClaimAllCards(info);
            else if (action.StartsWith("gallery-card:")) GalleryCollections.ClaimCard(int.Parse(action.Substring(13)));
            else if (action.StartsWith("gallery-claim:"))
            {
                int m = int.Parse(action.Substring(14));
                if (GalleryCollections.ClaimMilestone(info, m)) { Host.ShowGalleryReward(GalleryCollections.MilestoneCoins(info, m), m == 2); return; }
            }
            RefreshPopupCoins();
            RefreshGallery();
            // The new page is built in place, then slid in from where the old one stood.
            if (galleryIndex != before) scroll.Value = 0;
            if (galleryIndex != before) { slideFrom = (galleryIndex - before) * SideX + heldOffset; slideTime = 0; Slide(); }
            heldOffset = 0;
        }

        // Recording 2 (26.3-26.5 s, 36.9-37.2 s): a page turn slides the covers one card across in ~0.25 s, fastest at
        // once and slowing steadily to a stop; the category name changes at once.
        const float SlideSeconds = .25f;
        readonly List<Transform> carousel = new List<Transform>(); readonly List<float> carouselX = new List<float>();
        float slideFrom, slideTime = 1;
        void Slide() { float t = Mathf.Clamp01(slideTime); MoveCarousel(slideFrom * (1 - t) * (1 - t)); }
        void MoveCarousel(float offset)
        {
            for (int i = 0; i < carousel.Count; i++)
                if (carousel[i]) { var p = carousel[i].localPosition; carousel[i].localPosition = new Vector3(carouselX[i] + offset, p.y, p.z); }
        }

        // Claiming cards (recording 3, 53.45-54.1 s, Claim All): the "?" cards fold shut across (~0.4 s), then the
        // pictures unfold (~0.15 s). One card claimed alone does the same.
        const float FoldSeconds = .4f, UnfoldSeconds = .15f;
        sealed class CardPiece { public Transform T; public float X, ScaleX, CentreX; }
        readonly Dictionary<int, List<CardPiece>> cardPieces = new Dictionary<int, List<CardPiece>>();
        readonly List<int> flipping = new List<int>();
        int flipPhase; float flipTime; string flipAction; // phase 1 folding, 2 unfolding
        void KeepCard(int level, float centreX, int firstPiece)
        {
            var list = new List<CardPiece>();
            for (int c = firstPiece; c < galleryGrid.childCount; c++)
            {
                var t = galleryGrid.GetChild(c);
                list.Add(new CardPiece { T = t, X = t.localPosition.x, ScaleX = t.localScale.x, CentreX = centreX });
            }
            cardPieces[level] = list;
        }
        void FoldCards(float k)
        {
            k = Mathf.Max(.001f, k);
            foreach (int level in flipping)
            {
                List<CardPiece> list;
                if (!cardPieces.TryGetValue(level, out list)) continue;
                foreach (var p in list)
                {
                    if (!p.T) continue;
                    var pos = p.T.localPosition; p.T.localPosition = new Vector3(p.CentreX + (p.X - p.CentreX) * k, pos.y, pos.z);
                    var sc = p.T.localScale; p.T.localScale = new Vector3(p.ScaleX * k, sc.y, sc.z);
                }
            }
        }
        bool StartFlip(string action, GalleryCollections.Info info)
        {
            flipping.Clear();
            if (action.StartsWith("gallery-card:")) flipping.Add(int.Parse(action.Substring(13)));
            else if (action == "gallery-claim-all")
                for (int level = info.Start; level < info.Start + info.Count; level++) if (cardPieces.ContainsKey(level) && GalleryCollections.CardWaiting(level)) flipping.Add(level);
            if (flipping.Count == 0) return false;
            flipAction = action; flipPhase = 1; flipTime = 0;
            return true;
        }
        void TickFlip()
        {
            if (flipPhase == 0) return;
            flipTime += Time.unscaledDeltaTime;
            if (flipPhase == 1)
            {
                float t = Mathf.Clamp01(flipTime / FoldSeconds);
                FoldCards(1 - t * t);
                if (t < 1) return;
                flipPhase = 2; flipTime = 0;
                RunGalleryAction(flipAction); // claims and rebuilds the grid (pictures now)
                FoldCards(0);
                return;
            }
            float u = Mathf.Clamp01(flipTime / UnfoldSeconds);
            FoldCards(1 - (1 - u) * (1 - u));
            if (u >= 1) flipPhase = 0;
        }

        // Swiping the carousel (as in the original): the covers follow the finger (half as far where there is no next
        // cover), and on release turn a page when dragged a third of a card or flicked, otherwise slide back.
        const float SwipeBottom = CarouselY - 1.3f, TurnDistance = SideX / 3, TurnSpeed = 4f;
        float heldOffset;
        void Swipe()
        {
            float x = scroll.SwipeX;
            if (x != 0)
            {
                bool more = x < 0 ? galleryIndex < galleryList.Count - 1 : galleryIndex > 0;
                heldOffset = more ? x : x * .5f; slideTime = 1; MoveCarousel(heldOffset);
                return;
            }
            if (!scroll.SwipeReleased || heldOffset == 0) return;
            float flick = scroll.SwipeSpeed;
            int dir = heldOffset < -TurnDistance || flick < -TurnSpeed ? 1 : heldOffset > TurnDistance || flick > TurnSpeed ? -1 : 0;
            int target = Mathf.Clamp(galleryIndex + dir, 0, galleryList.Count - 1);
            if (dir != 0 && target != galleryIndex) RunGalleryAction(dir > 0 ? "gallery-next" : "gallery-prev");
            else { slideFrom = heldOffset; slideTime = 0; heldOffset = 0; }
        }

        // Drag / wheel scrolling of the grid (only below the grid's top); buttons fire on release without a drag.
        public override void Tick()
        {
            if (Open != gallery.Root) return;
            if (scroll.Tick(screen.UiCamera, flipPhase == 0 ? RunGalleryAction : (System.Action<string>)(a => { }), GridTop)) ClipGrid();
            TickFlip();
            if (scroll.SwipeStartY > SwipeBottom) Swipe();
            if (slideTime < 1) { slideTime += Time.unscaledDeltaTime / SlideSeconds; Slide(); }
        }
    }
}
