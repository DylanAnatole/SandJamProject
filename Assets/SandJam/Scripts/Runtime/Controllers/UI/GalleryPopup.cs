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
            var r = Child<SpriteRenderer>(gallery, "Picture", new Vector3(x, y, z)); r.sprite = sprite;
            if (sprite) { float k = Mathf.Max(w / sprite.bounds.size.x, h / sprite.bounds.size.y); r.transform.localScale = new Vector3(k, k, 1); }
            if (maskSprite)
            {
                r.sharedMaterial = SpriteFactory.PlainSprite;
                r.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                var m = Child<SpriteMask>(gallery, "Picture mask", new Vector3(x, y, z)); m.sprite = maskSprite;
                // The mask art's opaque card covers x 12-311, y 66-437 of its 324x450 texture (a transparent strip
                // at the bottom): scale and shift it so the card itself spans w x h centred on (x, y). Fitting the
                // whole texture left the picture short and high in its frame.
                const float fx = 300f / 324f, fy = 372f / 450f, offsetY = (66f + 438f) / 2f / 450f - .5f;
                var ms = maskSprite.bounds.size; float sx = w / (ms.x * fx), sy = h / (ms.y * fy);
                m.transform.localScale = new Vector3(sx, sy, 1);
                m.transform.localPosition = new Vector3(x, y - offsetY * ms.y * sy, z);
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
            foreach (var name in new[] { "bg-currency", "button-plus_0", "icon-coin", "2040" })
            { var t = gallery.Root.transform.Find(name); if (t) t.gameObject.SetActive(false); }
            var backdrop = gallery.Root.transform.Find("Dark backdrop").GetComponent<SpriteRenderer>();
            backdrop.color = new Color(.1f, .3f, .55f, 1);
            var top = Art(gallery, "GalleryPage/bg-topTexture-cardCollection", new Vector3(0, 3.2f, -6.05f), 5.8f);
            var bottom = Art(gallery, "GalleryPage/bg-bottom-cardCollection", new Vector3(0, -2.4f, -6.08f), 5.8f);
            bottom.SetLocalSize(5.8f, 5.6f);
            AddPageChrome(gallery, "icon-cards");
        }

        public void OpenGallery()
        {
            if (Open) return;
            galleryList = GalleryCollections.Unlocked();
            galleryIndex = Mathf.Max(0, galleryList.Count - 1);
            Show(gallery); RefreshPageLives(gallery);
            RefreshGallery();
        }

        void RefreshGallery()
        {
            DestroyAll(galleryDynamic);
            scroll.Value = 0;
            if (galleryList.Count == 0) { GalleryText("Beat level 15 to unlock\nyour first collection!", 0, 1.5f, .05f, -6.6f); return; }
            var info = galleryList[galleryIndex];
            int won = Campaign.LevelNumber - 1;
            // Category tab: hangs from the top bar with a smaller caption (original gallery, recording 2026-10-08).
            GalleryArt("header-top-cardCollection", 0, 4.0f, 3.2f, .44f, -6.3f);
            GalleryText(info.StyleName.ToUpperInvariant(), 0, 4.02f, .032f, -6.5f);
            // Carousel: neighbours first so the selected card draws on top.
            for (int side = -1; side <= 1; side += 2)
            {
                int n = galleryIndex + side; if (n < 0 || n >= galleryList.Count) continue;
                var sideFrame = GalleryArt("bg-cards-top-carCollection", side * 2.25f, 2.57f, 1.45f, 1.8f, -6.25f);
                float sy = FrameCentreY(sideFrame); var ss = FrameInner(sideFrame, 1.33f, 1.67f);
                Sprite c, g; StyleCover(galleryList[n], out c, out g);
                MaskedPicture(c, side * 2.25f, sy, ss.x, ss.y, -6.27f);
            }
            // The card art does not pivot on its centre: pictures are centred on the frame as drawn (they used to sit
            // high, overflowing the top edge with a gap at the bottom).
            var mainFrame = GalleryArt("bg-cards-top-carCollection", 0, 2.57f, 1.72f, 2.13f, -6.3f);
            float cy = FrameCentreY(mainFrame); var cs = FrameInner(mainFrame, 1.58f, 1.98f);
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
            GalleryArt("Group 10298", 0, .34f, 1.9f, .48f, -6.3f).color = new Color(.42f, .5f, .72f); // original: a dark navy pill
            GalleryText("Claim All Cards", 0, .35f, .036f, -6.4f);
            GalleryHit(0, .34f, 1.9f, .5f, "gallery-claim-all");
            // Picture grid (3 columns), scrolled inside the band between the button and the navbar.
            galleryGrid = new GameObject("Gallery grid") { layer = gallery.Root.layer }.transform;
            galleryGrid.SetParent(gallery.Root.transform, false);
            galleryDynamic.Add(galleryGrid.gameObject);
            const float cw = 1.42f, ch = 1.78f, gx = 1.6f, gy = 1.95f; // original cards nearly fill a third of the width
            for (int i = 0; i < info.Count; i++)
            {
                int level = info.Start + i; bool done = level <= won;
                float x = (i % 3 - 1) * gx, y = -.85f - (i / 3) * gy;
                bool waiting = done && GalleryCollections.CardWaiting(level);
                // Inside the slot frame's blue rim: rows 12-383 and columns 12-311 of the 324x450 art (measured top down;
                // the art has a transparent strip at the bottom, so that area sits above the sprite's centre).
                float vw = cw * 300f / 324f, vh = ch * 372f / 450f, vy = y + ch * (.5f - (12f + 384f) / 2f / 450f), rim = cw * .025f;
                if (waiting) { ClaimCard(level, x, y, vy, vw, vh, cw, ch); continue; }
                GalleryArt(done ? "bg-cardSlot-cardCollection" : "bg-cardSlot-cover-cardCollection", x, y, cw, ch, -6.3f, galleryGrid);
                if (!done) continue;
                string id = System.Text.RegularExpressions.Regex.Replace(Campaign.Levels[(level - 1) % Campaign.Levels.Length], "_Dupe$", "");
                MaskedPicture(Resources.Load<Sprite>("LevelRenders/" + id), x, vy, vw - 2 * rim, vh - 2 * rim, -6.35f, galleryGrid);
            }
            int rows = (info.Count + 2) / 3;
            scroll.Max = Mathf.Max(0, .85f + rows * gy - (GridTop - GridBottom));
            ClipGrid();
        }

        // A won level's card still face down (original): the "?" card in a gold frame with a "Claim" strip.
        void ClaimCard(int level, float x, float y, float cardY, float cardW, float cardH, float cw, float ch)
        {
            var gold = Track(Child<SpriteRenderer>(gallery, "Claim frame", new Vector3(x, cardY, -6.29f)), galleryGrid);
            gold.sprite = SpriteFactory.Rounded(64, 22, 260); gold.drawMode = SpriteDrawMode.Sliced;
            gold.size = new Vector2(cardW + .14f, cardH + .14f); gold.color = new Color(1f, .78f, .22f);
            GalleryArt("bg-cardSlot-cover-cardCollection", x, y, cw, ch, -6.3f, galleryGrid);
            GalleryArt("GalleryPage/Group 10298", x, cardY - cardH * .36f, cardW * .9f, .3f, -6.36f, galleryGrid);
            GalleryText("Claim", x, cardY - cardH * .36f, .032f, -6.4f, galleryGrid);
            GalleryHit(x, y, cw, ch, "gallery-card:" + level, galleryGrid);
        }

        void GalleryArrow(int direction)
        {
            float x = 1.18f * direction;
            GalleryArt("bg-arrow-cardCollection", x, 2.57f, .46f, .46f, -6.5f);
            var arrow = GalleryArt("icon-arrow-left-cardCollection", x - .02f * direction, 2.58f, .22f, .33f, -6.55f);
            if (direction > 0) arrow.flipX = true;
            GalleryHit(x, 2.57f, .65f, .65f, direction < 0 ? "gallery-prev" : "gallery-next");
        }

        void StyleCover(GalleryCollections.Info info, out Sprite colour, out Sprite grey)
        {
            // Gallery covers carry a transparent margin and an off-centre pivot: the card uses them trimmed and centred
            // (the same crop as the CONGRATS card), so the picture fills the rounded slot.
            if (info.StyleCover != null)
            {
                colour = Resources.Load<Sprite>(info.StyleCover); grey = Resources.Load<Sprite>(info.StyleCover + "-bw");
                if (colour) colour = CoverCrop(colour); if (grey) grey = CoverCrop(grey);
            }
            else { colour = Resources.Load<Sprite>("LevelRenders/" + info.StyleRenderId); grey = Greyscale(colour); }
        }

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
            if (action == "gallery-prev") galleryIndex = Mathf.Max(0, galleryIndex - 1);
            else if (action == "gallery-next") galleryIndex = Mathf.Min(galleryList.Count - 1, galleryIndex + 1);
            else if (action == "gallery-claim-all") GalleryCollections.ClaimAllCards(info);
            else if (action.StartsWith("gallery-card:")) GalleryCollections.ClaimCard(int.Parse(action.Substring(13)));
            else if (action.StartsWith("gallery-claim:")) GalleryCollections.ClaimMilestone(info, int.Parse(action.Substring(14)));
            RefreshPopupCoins();
            RefreshGallery();
        }

        // Drag / wheel scrolling of the grid (only below the grid's top); buttons fire on release without a drag.
        public override void Tick()
        {
            if (Open == gallery.Root && scroll.Tick(screen.UiCamera, RunGalleryAction, GridTop)) ClipGrid();
        }
    }
}
