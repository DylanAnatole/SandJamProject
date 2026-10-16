using System.Collections.Generic;
using UnityEngine;

namespace SandJamTest
{
    // Shared procedural sprites and materials (one cached instance each, rebuilt if Unity unloads it). Use these
    // instead of creating a white / rounded / round sprite or a plain "Sprites/Default" material in each view:
    // the shared objects must never be destroyed by their users.
    public static class SpriteFactory
    {
        static Sprite white, whiteBottom;
        // 1 x 1 unit white square, pivot in the centre (tint and scale it).
        public static Sprite White { get { return white ? white : (white = FromWhite(new Vector2(.5f, .5f))); } }
        // Same, pivot on the bottom edge (grows upward when scaled in y).
        public static Sprite WhiteBottom { get { return whiteBottom ? whiteBottom : (whiteBottom = FromWhite(new Vector2(.5f, 0))); } }
        static Sprite FromWhite(Vector2 pivot)
        {
            var t = Texture2D.whiteTexture;
            return Sprite.Create(t, new Rect(0, 0, t.width, t.height), pivot, t.width);
        }

        // White rounded rectangle for 9-slicing (Full Rect mesh, so sliced/tiled drawing works without warnings).
        // size: texture size in pixels; radius: corner radius in pixels; pixelsPerUnit sets the corner size in units.
        static readonly Dictionary<Vector3, Sprite> rounded = new Dictionary<Vector3, Sprite>();
        public static Sprite Rounded(int size, float radius, float pixelsPerUnit)
        {
            var key = new Vector3(size, radius, pixelsPerUnit);
            if (rounded.TryGetValue(key, out var cached) && cached) return cached;
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "Rounded " + size + "/" + radius };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
                px[y * size + x] = new Color32(255, 255, 255, (byte)(RoundedRectAlpha(x, y, size, size, radius) * 255));
            t.SetPixels32(px); t.Apply(false, true);
            float border = radius + 1;
            var sprite = Sprite.Create(t, new Rect(0, 0, size, size), new Vector2(.5f, .5f), pixelsPerUnit, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
            rounded[key] = sprite;
            return sprite;
        }

        // Anti-aliased coverage of pixel (x, y) inside a w x h rounded rectangle with the given corner radius.
        public static float RoundedRectAlpha(int x, int y, int w, int h, float radius)
        {
            float px = Mathf.Abs(x + .5f - w / 2f) - (w / 2f - radius), py = Mathf.Abs(y + .5f - h / 2f) - (h / 2f - radius);
            float d = new Vector2(Mathf.Max(px, 0), Mathf.Max(py, 0)).magnitude + Mathf.Min(Mathf.Max(px, py), 0) - radius;
            return Mathf.Clamp01(.5f - d);
        }

        static Sprite disc, softDot;
        // Hard-edged white disc, 1 unit across.
        public static Sprite Disc { get { return disc ? disc : (disc = Radial("Disc", 64, d => Mathf.Clamp01((1 - d) * 32f))); } }
        // White dot fading out quadratically from the centre, 1 unit across (glows, light pools).
        public static Sprite SoftDot { get { return softDot ? softDot : (softDot = Radial("Soft dot", 64, d => (1 - d) * (1 - d))); } }
        // Radial sprite: alpha from the normalised distance to the centre (0 centre, 1 edge, clamped).
        public static Sprite Radial(string name, int size, System.Func<float, float> alpha, Color32? rgb = null)
        {
            var c = rgb ?? new Color32(255, 255, 255, 255);
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = name };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float d = Mathf.Clamp01(Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(size / 2f, size / 2f)) / (size / 2f));
                px[y * size + x] = new Color32(c.r, c.g, c.b, (byte)(Mathf.Clamp01(alpha(d)) * 255));
            }
            t.SetPixels32(px); t.Apply(false, true);
            return Sprite.Create(t, new Rect(0, 0, size, size), new Vector2(.5f, .5f), size);
        }

        static Material plain;
        // Shared "Sprites/Default" material: unlit, honours sprite masks, no darkening (the project's pixel sprite
        // shader shades sprites ~12% and ignores masks). Never destroy or modify it; copy it to customise.
        public static Material PlainSprite { get { return plain ? plain : (plain = new Material(Shader.Find("Sprites/Default")) { name = "Plain sprite (shared)" }); } }

        // First enabled camera that renders the given layer (the UI overlay for popups, the main camera for the game).
        public static Camera CameraForLayer(int layer)
        {
            foreach (var c in Camera.allCameras) if ((c.cullingMask & (1 << layer)) != 0) return c;
            return null;
        }
    }
}
