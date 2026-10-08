using System.Collections.Generic;
using UnityEngine;

namespace SandJamTest.Scene3D
{
    // GALLERY page rebuilt from the original canvas_gallery (MainMenu scene, 1292x2796 canvas): category header,
    // the collection cover in a framed card with left/right arrows, the collection progress bar with three
    // milestone rewards, and the grid of picture cards (finished levels show their picture, the rest a "?").
    public sealed partial class PopupScreens
    {
        Popup gallery;
        readonly List<GameObject> galleryDynamic = new List<GameObject>();
        List<GalleryCollections.Info> galleryList;
        int galleryIndex;

        static Vector3 G(float canvasX, float canvasY, float z) { return new Vector3(canvasX * HomeUnit, canvasY * HomeUnit, z); }
        SpriteRenderer GalleryArt(string sprite, float x, float y, float w, float h, float z, bool dynamic = true)
        {
            var r = Art(gallery, sprite.Contains("/") ? sprite : "GalleryPage/" + sprite, G(x, y, z), w * HomeUnit);
            if (r.sprite) r.transform.localScale = new Vector3(w * HomeUnit / r.sprite.bounds.size.x, h * HomeUnit / r.sprite.bounds.size.y, 1);
            if (dynamic) galleryDynamic.Add(r.gameObject);
            return r;
        }
        TextMesh[] GalleryText(string text, float x, float y, float size, float z)
        {
            var labels = OutlinedLabel(gallery, "Gallery text", G(x, y, z), size);
            SetText(labels, text); galleryDynamic.Add(labels[0].gameObject);
            return labels;
        }
        void GalleryHit(float x, float y, float w, float h, string action)
        {
            galleryDynamic.Add(Hit(gallery, G(x, y, -8.5f), new Vector2(w * HomeUnit, h * HomeUnit), action));
        }

        void BuildGallery()
        {
            gallery = Blank("Gallery page");
            gallery.RightButton.transform.parent.gameObject.SetActive(false);
            var backdrop = gallery.Root.transform.Find("Dark backdrop").GetComponent<SpriteRenderer>();
            backdrop.color = new Color(.05f, .2f, .4f, 1);
            GalleryArt("bg-topTexture-cardCollection", 0, 700, 1506, 1506, -6.05f, false);
            GalleryArt("bg-bottom-cardCollection", 0, -700, 1506, 1500, -6.1f, false);
            CloseX(gallery, new Vector3(2.15f, 4.95f, -8.6f), "close");
            foreach (var name in new[] { "bg-currency", "button-plus_0", "icon-coin", "2040" })
            { var t = gallery.Root.transform.Find(name); if (t) t.gameObject.SetActive(false); }
        }

        public void OpenGallery()
        {
            if (Open) return;
            galleryList = GalleryCollections.Unlocked();
            galleryIndex = Mathf.Max(0, galleryList.Count - 1);
            Show(gallery);
            RefreshGallery();
        }

        void RefreshGallery()
        {
            foreach (var o in galleryDynamic) if (o) Destroy(o);
            galleryDynamic.Clear();
            if (galleryList.Count == 0)
            {
                GalleryText("Qua màn 15 để mở\nbộ sưu tập đầu tiên!", 0, 300, .05f, -6.6f);
                return;
            }
            var info = galleryList[galleryIndex];
            int won = Campaign.LevelNumber - 1;
            // Header and the collection cover card (colour revealed bottom-up, like the CONGRATS card).
            GalleryArt("header-top-cardCollection", 0, 1028, 829, 126, -6.3f);
            GalleryText(info.Name, 0, 1040, .045f, -6.5f);
            GalleryArt("bg-cards-top-carCollection", 0, 709, 506, 626, -6.3f);
            Sprite colour, grey;
            CollectionCover(info, out colour, out grey);
            if (grey)
            {
                var size = grey.bounds.size; float k = Mathf.Min(450 * HomeUnit / size.x, 570 * HomeUnit / size.y);
                var g = Art(gallery, "", G(0, 709, -6.4f), 1); g.sprite = grey; g.transform.localScale = new Vector3(k, k, 1); galleryDynamic.Add(g.gameObject);
                float fraction = (float)info.Completed(won) / info.Count;
                if (fraction > 0 && colour)
                {
                    var rect = colour.textureRect;
                    var slice = Sprite.Create(colour.texture, new Rect(rect.x, rect.y, rect.width, Mathf.Max(1, Mathf.Round(rect.height * fraction))), new Vector2(.5f, 0), colour.pixelsPerUnit);
                    runtimeArt.Add(slice);
                    var c = Art(gallery, "", G(0, 0, -6.45f), 1); c.sprite = slice;
                    c.transform.localScale = new Vector3(k * colour.bounds.size.x / size.x, k * colour.bounds.size.y / size.y, 1);
                    c.transform.localPosition = new Vector3(0, 709 * HomeUnit - k * size.y / 2, -6.45f);
                    galleryDynamic.Add(c.gameObject);
                }
            }
            // Arrows to browse unlocked collections.
            if (galleryIndex > 0) GalleryArrow(-1);
            if (galleryIndex < galleryList.Count - 1) GalleryArrow(1);
            // Progress bar with the three milestone rewards.
            GalleryArt("bg-header-middle-cardCollection", 0, 239, 1320, 210, -6.3f);
            GalleryArt("bg-fillbar-middle-cardCollection", -25, 247, 1000, 90, -6.35f);
            float fill = (float)info.Completed(won) / info.Count;
            if (fill > 0) GalleryArt("fillbar-middle-cardCollection", -525 + 500 * fill, 247, 1000 * fill, 78, -6.4f);
            GalleryText(info.Completed(won) + "/" + info.Count, -25, 245, .04f, -6.5f);
            for (int m = 0; m < 3; m++)
            {
                float x = -525 + 1000 * (m + 1) / 3f;
                if (m == 2) x -= 30;
                GalleryArt("bg-reward", x, 345, 113, 136, -6.45f);
                bool claimed = GalleryCollections.MilestoneClaimed(info, m), reached = GalleryCollections.MilestoneReached(info, m);
                if (claimed) GalleryArt("Tasks/icon-done", x, 352, 80, 68, -6.5f);
                else if (m == 2) GalleryArt("icon-gift", x, 352, 70, 88, -6.5f);
                else { GalleryArt("Shop/icon-coin_1", x, 365, 56, 56, -6.5f); GalleryText(GalleryCollections.MilestoneCoins(info, m).ToString(), x, 320, .026f, -6.55f); }
                if (reached && !claimed) GalleryHit(x, 345, 150, 170, "gallery-claim:" + m);
                if (reached && !claimed) { var glow = GalleryArt("Lives/light", x, 352, 210, 210, -6.42f); glow.color = new Color(1, .9f, .4f, .8f); }
            }
            // Card grid: 5 columns; finished levels show their picture.
            const int cols = 5; const float cw = 228, ch = 316, gx = 245, gy = 330;
            for (int i = 0; i < info.Count; i++)
            {
                int level = info.Start + i;
                float x = (i % cols - (cols - 1) / 2f) * gx, y = -60 - ch / 2 - (i / cols) * gy;
                bool done = level <= won;
                GalleryArt(done ? "bg-cardSlot-cardCollection" : "bg-cardSlot-cover-cardCollection", x, y, cw, ch, -6.3f);
                if (!done) continue;
                string id = System.Text.RegularExpressions.Regex.Replace(Campaign.Levels[(level - 1) % Campaign.Levels.Length], "_Dupe$", "");
                var picture = Resources.Load<Sprite>("LevelRenders/" + id);
                if (!picture) continue;
                var p = Art(gallery, "", G(x, y, -6.4f), 1); p.sprite = picture;
                float k = Mathf.Min((cw - 30) * HomeUnit / picture.bounds.size.x, (ch - 30) * HomeUnit / picture.bounds.size.y);
                p.transform.localScale = new Vector3(k, k, 1); galleryDynamic.Add(p.gameObject);
            }
        }

        void GalleryArrow(int direction)
        {
            float x = 302.8f * direction;
            GalleryArt("bg-arrow-cardCollection", x, 668, 120, 120, -6.5f);
            var arrow = GalleryArt("icon-arrow-left-cardCollection", x - 7 * direction, 671, 62, 94, -6.55f);
            if (direction > 0) arrow.flipX = true;
            GalleryHit(x, 668, 170, 170, direction < 0 ? "gallery-prev" : "gallery-next");
        }

        void CollectionCover(GalleryCollections.Info info, out Sprite colour, out Sprite grey)
        {
            if (info.Cover != null) { colour = Resources.Load<Sprite>(info.Cover); grey = Resources.Load<Sprite>(info.Cover + "-bw"); }
            else { colour = Resources.Load<Sprite>("LevelRenders/" + info.RenderId); grey = Greyscale(colour); }
        }

        bool HandleGallery(string action)
        {
            if (action == "gallery-prev") galleryIndex = Mathf.Max(0, galleryIndex - 1);
            else if (action == "gallery-next") galleryIndex = Mathf.Min(galleryList.Count - 1, galleryIndex + 1);
            else if (action.StartsWith("gallery-claim:"))
            {
                GalleryCollections.ClaimMilestone(galleryList[galleryIndex], int.Parse(action.Substring(14)));
                RefreshPopupCoins();
            }
            RefreshGallery();
            return true;
        }
    }
}
