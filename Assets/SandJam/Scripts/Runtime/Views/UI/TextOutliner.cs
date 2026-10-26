using System.Collections.Generic;
using UnityEngine;

namespace SandJamTest.Scene3D
{
    // Original UI text (Lilita One "WithOutline"): white letters with a thick dark purple outline that sits a little
    // lower than the letters. This scans the screens for white labels that have no outline yet and gives each one
    // eight dark copies behind it, kept in sync with the label's text, size and fade.
    public sealed class TextOutliner : MonoBehaviour
    {
        // Measured on recordings 1 and 2 (Home, Shop, popups, CONGRATS, Level Complete): an almost black outline,
        // #0B080C to #0D0A0D, not purple.
        public static readonly Color Ink = new Color32(12, 9, 13, 255);
        const float Rescan = .4f;
        float nextScan;
        readonly HashSet<TextMesh> seen = new HashSet<TextMesh>();
        readonly List<TextMesh> scan = new List<TextMesh>(); // reused: GetComponentsInChildren would allocate an array

        // Board counters and cube numbers live outside the UI screens; they are switched to the UI font when a
        // level starts (and once at startup).
        bool fontPass = true;
        void OnEnable() { GameEvents.LevelStarted += OnLevel; }
        void OnDisable() { GameEvents.LevelStarted -= OnLevel; }
        void OnLevel(int level) { fontPass = true; }

        void LateUpdate()
        {
            if (fontPass) { fontPass = false; UiFont.ApplyAll(); }
            if (Time.unscaledTime < nextScan) return;
            nextScan = Time.unscaledTime + Rescan;
            GetComponentsInChildren(false, scan);
            foreach (var label in scan)
            {
                if (!seen.Add(label)) continue;
                UiFont.Apply(label);
                if (!Eligible(label)) continue;
                label.gameObject.AddComponent<OutlinedText>();
            }
        }

        static bool Eligible(TextMesh label)
        {
            if (label.name == "Text outline" || label.GetComponent<OutlinedText>()) return false;
            var parent = label.transform.parent;
            if (parent && (parent.name == "Text outline" || parent.GetComponent<OutlinedText>())) return false;
            if (label.GetComponentInParent<SuperbFx>() || UiFont.OnGeometry(label)) return false;
            var c = label.color;
            return c.r > .85f && c.g > .85f && c.b > .85f;
        }
    }

    // The one text outline used by the game: eight offset copies of the label's glyphs in the Ink colour, kept in sync
    // with the label's text, font, size and fade. The copies are ONE mesh behind the label (they were eight TextMesh
    // objects: eight renderers per label, ~130 extra renderers on a level). Add it with OutlinedText.Add(label, ink)
    // or let TextOutliner give it to white labels on the UI screens.
    public sealed class OutlinedText : MonoBehaviour
    {
        // Outline colour; dark captions use a light outline instead (original Hard/Bonus popups).
        public Color Ink = TextOutliner.Ink;
        // Outline radius as a fraction of the line height.
        public float Thickness = .08f;

        public static OutlinedText Add(TextMesh label, Color ink, float thickness = .08f)
        {
            var o = label.GetComponent<OutlinedText>();
            if (!o) { o = label.gameObject.AddComponent<OutlinedText>(); }
            o.Thickness = thickness; o.SetInk(ink);
            return o;
        }
        static readonly Vector2[] Directions =
        {
            new Vector2(1, 0), new Vector2(-1, 0), new Vector2(0, 1), new Vector2(0, -1),
            new Vector2(.71f, .71f), new Vector2(-.71f, .71f), new Vector2(.71f, -.71f), new Vector2(-.71f, -.71f),
        };
        TextMesh main;
        MeshRenderer mainRenderer, edgeRenderer;
        Mesh edge;
        string text; float size = -1, alpha = -1; Font font;
        Vector3 lastBounds; int phase;
        Color shown; bool shownSet; // colour drawn now (Ink unless ShowInk overrides it)

        void Awake()
        {
            main = GetComponent<TextMesh>(); mainRenderer = GetComponent<MeshRenderer>(); phase = GetInstanceID() & 7;
            // Older, thinner outlines (scene builder / popup helpers) are replaced by this one.
            foreach (Transform child in transform) if (child.name == "Text outline") child.gameObject.SetActive(false);
            var go = new GameObject("Text outline mesh") { layer = gameObject.layer };
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0, 0, .01f); // just behind the letters
            edge = new Mesh { name = "Text outline" }; edge.MarkDynamic();
            go.AddComponent<MeshFilter>().sharedMesh = edge;
            edgeRenderer = go.AddComponent<MeshRenderer>();
            edgeRenderer.sortingLayerID = mainRenderer.sortingLayerID; edgeRenderer.sortingOrder = mainRenderer.sortingOrder; // same order: depth puts it just behind
            edgeRenderer.NoShadows();
            edgeRenderer.sharedMaterial = mainRenderer.sharedMaterial;
            Font.textureRebuilt += OnAtlasRebuilt;
        }
        void OnDestroy() { Font.textureRebuilt -= OnAtlasRebuilt; if (edge) Destroy(edge); }
        // The font atlas was repacked: the glyph UVs moved, rebuild next frame.
        void OnAtlasRebuilt(Font rebuilt) { if (rebuilt == font) size = -1; }

        Color32 CurrentInk() { var c = shownSet ? shown : Ink; c.a *= main ? main.color.a : 1; return c; }
        void Recolour()
        {
            if (!main) return;
            alpha = main.color.a;
            if (colors.Count == 0) return;
            var c32 = CurrentInk();
            for (int i = 0; i < colors.Count; i++) colors[i] = c32;
            edge.SetColors(colors);
        }

        // Outline colour change after creation.
        public void SetInk(Color ink) { Ink = ink; shownSet = false; Recolour(); }
        // Draws the outline in another colour for now, keeping Ink (back-row cube numbers use a shade of their block).
        public void ShowInk(Color ink) { shown = ink; shownSet = true; Recolour(); }
        // Outline radius change after creation: re-laid out on the next frame.
        public void SetThickness(float thickness) { if (Thickness == thickness) return; Thickness = thickness; size = -1; }

        // Busy labels (cube numbers, board counters) set their text here: the outline follows at once, and the label
        // is no longer polled (reading TextMesh.text makes a new string every time).
        bool driven;
        public static void SetText(TextMesh label, string value)
        {
            label.text = value;
            var o = label.GetComponent<OutlinedText>();
            if (o) { o.driven = true; o.size = -1; }
        }

        void LateUpdate()
        {
            // Reading main.text allocates: only when the label's size changed, or every 8th frame for same-width edits.
            var now = mainRenderer ? mainRenderer.localBounds.size : Vector3.zero;
            if (now != lastBounds || size < 0 || (!driven && ((Time.frameCount + phase) & 7) == 0))
            {
                if (now != lastBounds || size < 0 || main.text != text || main.characterSize != size || main.font != font) Sync();
                lastBounds = now;
            }
            if (main.color.a != alpha) Recolour();
            if (edgeRenderer.sharedMaterial != mainRenderer.sharedMaterial) edgeRenderer.sharedMaterial = mainRenderer.sharedMaterial;
        }

        // The label's glyph quads, laid out by the font the way TextMesh lays them out (same font, size, anchor and
        // alignment), copied eight times around it.
        static TextGenerator generator;
        readonly List<Vector3> vertices = new List<Vector3>();
        readonly List<Vector2> uvs = new List<Vector2>();
        readonly List<Color32> colors = new List<Color32>();
        readonly List<int> triangles = new List<int>();
        void Sync()
        {
            text = main.text; size = main.characterSize; font = main.font;
            vertices.Clear(); uvs.Clear(); colors.Clear(); triangles.Clear();
            var local = mainRenderer.localBounds;
            if (string.IsNullOrEmpty(text) || !font || local.size.x <= 0 || local.size.y <= 0) { edge.Clear(); return; }
            if (generator == null) generator = new TextGenerator();
            var settings = new TextGenerationSettings
            {
                font = font, fontSize = main.fontSize > 0 ? main.fontSize : font.fontSize, fontStyle = main.fontStyle,
                lineSpacing = main.lineSpacing, richText = main.richText, scaleFactor = 1, textAnchor = main.anchor,
                alignByGeometry = false, generateOutOfBounds = true, horizontalOverflow = HorizontalWrapMode.Overflow,
                verticalOverflow = VerticalWrapMode.Overflow, pivot = new Vector2(.5f, .5f), color = Color.white,
                updateBounds = false,
            };
            generator.Populate(text, settings);
            var glyphs = generator.verts;
            if (glyphs.Count == 0) { edge.Clear(); return; }
            // TextMesh draws one font pixel as characterSize / 10 units, about the same anchor point as the generator.
            float unit = main.characterSize * .1f;
            // Outline thickness follows the line height.
            int lines = 1; for (int i = 0; i < text.Length; i++) if (text[i] == '\n') lines++;
            float r = local.size.y / lines * Thickness;
            var ink = CurrentInk();
            for (int d = 0; d < Directions.Length; d++)
            {
                var offset = new Vector3(Directions[d].x * r, Directions[d].y * r - r * .45f, 0);
                for (int i = 0; i + 3 < glyphs.Count; i += 4)
                {
                    if (glyphs[i].position == glyphs[i + 2].position) continue;
                    int start = vertices.Count;
                    for (int k = 0; k < 4; k++)
                    {
                        var g = glyphs[i + k];
                        vertices.Add(new Vector3(g.position.x * unit, g.position.y * unit, 0) + offset);
                        uvs.Add(g.uv0); colors.Add(ink);
                    }
                    triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
                    triangles.Add(start + 2); triangles.Add(start + 3); triangles.Add(start);
                }
            }
            edge.Clear();
            edge.SetVertices(vertices); edge.SetUVs(0, uvs); edge.SetColors(colors); edge.SetTriangles(triangles, 0);
            alpha = main.color.a;
        }
    }
}
