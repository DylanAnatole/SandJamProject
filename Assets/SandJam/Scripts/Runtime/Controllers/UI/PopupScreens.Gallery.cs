using System.Collections.Generic;
using UnityEngine;

namespace SandJamTest.Scene3D
{
    // GALLERY page as in the current build (gameplay recording 2026-10-08) on the original canvas_gallery art:
    // category tab, a carousel of collection covers (selected one in the middle, neighbours peeking at the
    // sides, arrows), the collection bar with three milestone rewards (ticked once claimed), "Claim All Cards",
    // and a scrolling 3-column grid of the collection's pictures. Home top bar and navbar stay visible.
    public sealed partial class PopupScreens
    {
        Popup gallery;
        readonly List<GameObject> galleryDynamic = new List<GameObject>();
        Transform galleryGrid;
        List<GalleryCollections.Info> galleryList;
        int galleryIndex;
        float galleryScroll, galleryScrollMax, galleryPressY, galleryPressScroll;
        bool galleryPressing, galleryDragged;
        string galleryPending;
        const float GridTop = .02f, GridBottom = -4.2f;

        SpriteRenderer GalleryArt(string sprite, float x, float y, float w, float h, float z, Transform parent = null)
        {
            var r = Art(gallery, sprite.Contains("/") ? sprite : "GalleryPage/" + sprite, new Vector3(x, y, z), w);
            if (r.sprite) r.transform.localScale = new Vector3(w / r.sprite.bounds.size.x, h / r.sprite.bounds.size.y, 1);
            if (parent) r.transform.SetParent(parent, true);
            galleryDynamic.Add(r.gameObject);
            return r;
        }
        SpriteRenderer GalleryPicture(Sprite sprite, float x, float y, float maxW, float maxH, float z, Transform parent = null)
        {
            var r = Child<SpriteRenderer>(gallery, "Picture", new Vector3(x, y, z)); r.sprite = sprite;
            if (sprite) { float k = Mathf.Min(maxW / sprite.bounds.size.x, maxH / sprite.bounds.size.y); r.transform.localScale = new Vector3(k, k, 1); }
            if (parent) r.transform.SetParent(parent, true);
            galleryDynamic.Add(r.gameObject);
            return r;
        }
        TextMesh[] GalleryText(string text, float x, float y, float size, float z)
        {
            var labels = OutlinedLabel(gallery, "Gallery text", new Vector3(x, y, z), size);
            SetText(labels, text); galleryDynamic.Add(labels[0].gameObject);
            return labels;
        }
        void GalleryHit(float x, float y, float w, float h, string action, Transform parent = null)
        {
            var hit = Hit(gallery, new Vector3(x, y, -8.5f), new Vector2(w, h), action);
            if (parent) hit.transform.SetParent(parent, true);
            galleryDynamic.Add(hit);
        }

        void BuildGallery()
        {
            gallery = Blank("Gallery page");
            gallery.RightButton.transform.parent.gameObject.SetActive(false);
            foreach (var name in new[] { "bg-currency", "button-plus_0", "icon-coin", "2040" })
            { var t = gallery.Root.transform.Find(name); if (t) t.gameObject.SetActive(false); }
            var backdrop = gallery.Root.transform.Find("Dark backdrop").GetComponent<SpriteRenderer>();
            backdrop.color = new Color(.1f, .3f, .55f, 1);
            var top = Art(gallery, "GalleryPage/bg-topTexture-cardCollection", new Vector3(0, 3.2f, -6.05f), 5.8f);
            var bottom = Art(gallery, "GalleryPage/bg-bottom-cardCollection", new Vector3(0, -2.4f, -6.08f), 5.8f);
            if (bottom.sprite) bottom.transform.localScale = new Vector3(5.8f / bottom.sprite.bounds.size.x, 5.6f / bottom.sprite.bounds.size.y, 1);
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
            foreach (var o in galleryDynamic) if (o) Destroy(o);
            galleryDynamic.Clear();
            galleryScroll = 0;
            if (galleryList.Count == 0) { GalleryText("Beat level 15 to unlock\nyour first collection!", 0, 1.5f, .05f, -6.6f); return; }
            var info = galleryList[galleryIndex];
            int won = Campaign.LevelNumber - 1;
            // Category tab.
            GalleryArt("header-top-cardCollection", 0, 3.78f, 2.95f, .53f, -6.3f);
            GalleryText(info.StyleName.ToUpperInvariant(), 0, 3.81f, .042f, -6.5f);
            // Carousel: neighbours first so the selected card draws on top.
            for (int side = -1; side <= 1; side += 2)
            {
                int n = galleryIndex + side; if (n < 0 || n >= galleryList.Count) continue;
                GalleryArt("bg-cards-top-carCollection", side * 2.25f, 2.57f, 1.45f, 1.8f, -6.25f);
                Sprite c, g; StyleCover(galleryList[n], out c, out g);
                GalleryPicture(c, side * 2.25f, 2.57f, 1.3f, 1.62f, -6.27f);
            }
            GalleryArt("bg-cards-top-carCollection", 0, 2.57f, 1.72f, 2.13f, -6.3f);
            Sprite colour, grey; StyleCover(info, out colour, out grey);
            float fraction = (float)info.Completed(won) / info.Count;
            if (fraction >= 1 || !grey) GalleryPicture(colour, 0, 2.57f, 1.53f, 1.93f, -6.35f);
            else
            {
                var g = GalleryPicture(grey, 0, 2.57f, 1.53f, 1.93f, -6.35f);
                if (fraction > 0 && colour)
                {
                    var rect = colour.textureRect;
                    var slice = Sprite.Create(colour.texture, new Rect(rect.x, rect.y, rect.width, Mathf.Max(1, Mathf.Round(rect.height * fraction))), new Vector2(.5f, 0), colour.pixelsPerUnit);
                    runtimeArt.Add(slice);
                    var c = GalleryPicture(slice, 0, 0, 1, 1, -6.4f);
                    c.transform.localScale = g.transform.localScale;
                    c.transform.localPosition = new Vector3(0, 2.57f - g.bounds.size.y / 2, -6.4f);
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
            GalleryArt("Group 10298", 0, .34f, 1.9f, .48f, -6.3f);
            GalleryText("Claim All Cards", 0, .35f, .036f, -6.4f);
            GalleryHit(0, .34f, 1.9f, .5f, "gallery-claim-all");
            // Picture grid (3 columns), scrolled inside the band between the button and the navbar.
            galleryGrid = new GameObject("Gallery grid") { layer = gallery.Root.layer }.transform;
            galleryGrid.SetParent(gallery.Root.transform, false);
            galleryDynamic.Add(galleryGrid.gameObject);
            const float cw = 1.17f, ch = 1.46f, gx = 1.6f, gy = 1.7f;
            for (int i = 0; i < info.Count; i++)
            {
                int level = info.Start + i; bool done = level <= won;
                float x = (i % 3 - 1) * gx, y = -.55f - (i / 3) * gy;
                GalleryArt(done ? "bg-cardSlot-cardCollection" : "bg-cardSlot-cover-cardCollection", x, y, cw, ch, -6.3f, galleryGrid);
                if (!done) continue;
                string id = System.Text.RegularExpressions.Regex.Replace(Campaign.Levels[(level - 1) % Campaign.Levels.Length], "_Dupe$", "");
                GalleryPicture(Resources.Load<Sprite>("LevelRenders/" + id), x, y, cw - .12f, ch - .12f, -6.35f, galleryGrid);
            }
            int rows = (info.Count + 2) / 3;
            galleryScrollMax = Mathf.Max(0, .55f + rows * gy - (GridTop - GridBottom));
            ClipGrid();
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
            if (info.StyleCover != null) { colour = Resources.Load<Sprite>(info.StyleCover); grey = Resources.Load<Sprite>(info.StyleCover + "-bw"); }
            else { colour = Resources.Load<Sprite>("LevelRenders/" + info.StyleRenderId); grey = Greyscale(colour); }
        }

        // Cards scrolled out of the grid band are hidden (the original uses a mask).
        void ClipGrid()
        {
            if (!galleryGrid) return;
            galleryGrid.localPosition = new Vector3(0, galleryScroll, 0);
            // A card (1.46 tall) shows while its centre keeps it mostly inside the band.
            foreach (Transform card in galleryGrid)
            {
                float y = card.position.y;
                card.gameObject.SetActive(y < GridTop - .45f && y > GridBottom + .45f);
            }
        }

        bool HandleGallery(string action)
        {
            galleryPending = action;
            return true;
        }
        void RunGalleryAction(string action)
        {
            var info = galleryList[galleryIndex];
            if (action == "gallery-prev") galleryIndex = Mathf.Max(0, galleryIndex - 1);
            else if (action == "gallery-next") galleryIndex = Mathf.Min(galleryList.Count - 1, galleryIndex + 1);
            else if (action == "gallery-claim-all") for (int m = 0; m < 3; m++) GalleryCollections.ClaimMilestone(info, m);
            else if (action.StartsWith("gallery-claim:")) GalleryCollections.ClaimMilestone(info, int.Parse(action.Substring(14)));
            RefreshPopupCoins();
            RefreshGallery();
        }

        // Drag / wheel scrolling of the grid; buttons fire on release without a drag (like the shop).
        void UpdateGallery()
        {
            if (Open != gallery.Root) return;
            var cam = screen.UiCamera; if (!cam) return;
            var pointer = Input.mousePosition;
            if (float.IsInfinity(pointer.x) || float.IsNaN(pointer.x) || float.IsInfinity(pointer.y) || float.IsNaN(pointer.y)) return;
            float y = cam.ScreenToWorldPoint(pointer).y;
            if (Input.GetMouseButtonDown(0)) { galleryPressing = true; galleryDragged = false; galleryPressY = y; galleryPressScroll = galleryScroll; }
            if (galleryPressing && Input.GetMouseButton(0) && y < GridTop)
            {
                float delta = y - galleryPressY;
                if (Mathf.Abs(delta) > .12f) galleryDragged = true;
                if (galleryDragged) { galleryScroll = Mathf.Clamp(galleryPressScroll + delta, 0, galleryScrollMax); ClipGrid(); }
            }
            if (galleryPressing && Input.GetMouseButtonUp(0))
            {
                galleryPressing = false;
                if (!galleryDragged && galleryPending != null) RunGalleryAction(galleryPending);
                galleryPending = null;
            }
            float wheel = Input.mouseScrollDelta.y;
            if (Mathf.Abs(wheel) > .01f) { galleryScroll = Mathf.Clamp(galleryScroll - wheel * .6f, 0, galleryScrollMax); ClipGrid(); }
        }
    }
}
