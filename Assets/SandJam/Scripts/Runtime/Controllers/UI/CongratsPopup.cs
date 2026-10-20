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
        public override void Build()
        {
            congrats = Blank("Congrats popup");
            congrats.Root.transform.Find("Next").gameObject.SetActive(false); // the whole screen continues instead
            // The original card sits on an even, opaque dark navy (#141225): nothing of the board shows.
            congrats.Root.transform.Find("Dark backdrop").GetComponent<SpriteRenderer>().color = new Color32(20, 18, 37, 255);
            congratsRays = Art(congrats, "Congrats/light-congrats", new Vector3(0, 3.1f, -6.1f), 4.2f).transform;
            Art(congrats, "Congrats/bg-popUp-congrats-v2", new Vector3(0, .06f, -6.2f), 3.86f);
            cardBw = Child<SpriteRenderer>(congrats, "Cover grey", new Vector3(0, CoverY, -6.4f));
            cardColor = Child<SpriteRenderer>(congrats, "Cover colour", new Vector3(0, CoverY, -6.45f));
            Art(congrats, "Congrats/ribbon-yellow-v2", new Vector3(0, 2.74f, -6.6f), 4.95f);
            Art(congrats, "Congrats/text-congrats-v2", new Vector3(0, 2.8f, -6.7f), 3.3f);
            cardName = OutlinedLabel(congrats, "Collection name", new Vector3(0, 2.04f, -6.8f), .042f);
            cardPercent = OutlinedLabel(congrats, "Collection percent", new Vector3(0, -1.63f, -6.8f), .038f);
            SetText(OutlinedLabel(congrats, "Tap to continue", new Vector3(0, -3.48f, -6.8f), .042f), "Tap To Continue");
            // The whole screen continues, in front of everything else in the popup.
            var hit = Child<BoxCollider>(congrats, "Hit area - congrats-continue", new Vector3(0, 0, -9.5f));
            hit.gameObject.layer = 9; hit.size = new Vector3(30, 40, .2f);
            hit.gameObject.AddComponent<VideoUiButton>().Action = "congrats-continue";
        }
        public override bool Handles(string action) { return action == "congrats-continue"; }
        public override bool Handle(string action) { Close(); screen.Levels.LoadNext(); return true; }

        public bool ShowCongrats()
        {
            int level = screen.Levels.PlayedNumber;
            var info = GalleryCollections.ForLevel(level);
            if (info == null) return false;
            Sprite colour, grey;
            if (info.Cover != null) { colour = Resources.Load<Sprite>(info.Cover); grey = Resources.Load<Sprite>(info.Cover + "-bw"); }
            else { colour = Resources.Load<Sprite>("LevelRenders/" + info.RenderId); grey = Greyscale(colour); }
            if (!colour || !grey) return false;
            if (info.Cover != null) { colour = CoverCrop(colour); grey = CoverCrop(grey); }
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

        // Copy of a sprite's area with transparent rounded corners (radius ~10% of its width, the frame rim's inner
        // rounding). Rendered through the GPU, so the source texture need not be readable.
        readonly Dictionary<Sprite, Sprite> roundedCopies = new Dictionary<Sprite, Sprite>();
        Sprite RoundedCopy(Sprite source)
        {
            if (!source) return source;
            if (roundedCopies.TryGetValue(source, out var done)) return done;
            var r = source.textureRect; var tex = source.texture;
            int w = Mathf.RoundToInt(r.width), h = Mathf.RoundToInt(r.height);
            var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Graphics.Blit(tex, rt, new Vector2(r.width / tex.width, r.height / tex.height), new Vector2(r.x / tex.width, r.y / tex.height));
            var copy = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = tex.filterMode, wrapMode = TextureWrapMode.Clamp, name = source.name + " rounded" };
            var active = RenderTexture.active; RenderTexture.active = rt;
            copy.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            RenderTexture.active = active; RenderTexture.ReleaseTemporary(rt);
            var px = copy.GetPixels32(); float radius = w * .1f;
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                // Only the four corner squares are touched: alpha falls off past the radius from the corner centre.
                bool left = x + .5f < radius, right = x + .5f > w - radius, bottom = y + .5f < radius, top = y + .5f > h - radius;
                if (!((left || right) && (bottom || top))) continue;
                float cx = left ? radius : w - radius, cy = bottom ? radius : h - radius;
                float d = Mathf.Sqrt((x + .5f - cx) * (x + .5f - cx) + (y + .5f - cy) * (y + .5f - cy));
                float a = Mathf.Clamp01(radius + .5f - d);
                var c = px[y * w + x]; c.a = (byte)(c.a * a); px[y * w + x] = c;
            }
            copy.SetPixels32(px); copy.Apply(false, true);
            var sprite = Sprite.Create(copy, new Rect(0, 0, w, h), new Vector2(.5f, .5f), source.pixelsPerUnit);
            sprite.name = source.name + " (rounded)";
            roundedCopies[source] = sprite; runtimeArt.Add(copy); runtimeArt.Add(sprite);
            return sprite;
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
            if (!IsOpen(congrats)) return;
            if (congratsRays) congratsRays.localRotation = Quaternion.Euler(0, 0, Time.unscaledTime * 12f);
            if (!coverMask) return;
            float t = Mathf.Clamp01((Time.unscaledTime - ShownAt - .3f) / .8f);
            t = t * t * (3 - 2 * t);
            SetCoverFill(Mathf.Lerp(fillFrom, fillTo, t), Mathf.RoundToInt(Mathf.Lerp(percentFrom, percentTo, t)));
        }
    }
}
