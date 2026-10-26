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

    // The one text outline used by the game: eight copies of the label behind it, in the Ink colour, kept in sync
    // with the label's text, font, size and fade. Add it with OutlinedText.Add(label, ink) or let TextOutliner give
    // it to white labels on the UI screens.
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
        MeshRenderer mainRenderer;
        readonly TextMesh[] copies = new TextMesh[Directions.Length];
        readonly MeshRenderer[] copyRenderers = new MeshRenderer[Directions.Length];
        string text; float size = -1, alpha = -1; Font font;
        Vector3 lastBounds; int phase;

        void Awake()
        {
            main = GetComponent<TextMesh>(); mainRenderer = GetComponent<MeshRenderer>(); phase = GetInstanceID() & 7;
            // Older, thinner outlines (scene builder / popup helpers) are replaced by this one.
            foreach (Transform child in transform) if (child.name == "Text outline") child.gameObject.SetActive(false);
            for (int i = 0; i < copies.Length; i++)
            {
                var go = new GameObject("Text outline") { layer = gameObject.layer };
                go.transform.SetParent(transform, false);
                copies[i] = go.AddComponent<TextMesh>();
                var r = copyRenderers[i] = go.GetComponent<MeshRenderer>();
                r.sortingLayerID = mainRenderer.sortingLayerID; r.sortingOrder = mainRenderer.sortingOrder; // same order: depth puts it just behind the letters
                r.NoShadows();
            }
            Recolour(); // right away: code reading the label's colours this frame must not see default white copies
        }
        void Recolour()
        {
            if (!main) return;
            alpha = main.color.a; var c = Ink; c.a = alpha;
            foreach (var t in copies) if (t) t.color = c;
        }

        // Outline colour change after creation: the copies are recoloured on the next frame.
        public void SetInk(Color ink) { Ink = ink; Recolour(); }
        // Outline radius change after creation: the copies are re-laid out on the next frame.
        public void SetThickness(float thickness) { if (Thickness == thickness) return; Thickness = thickness; size = -1; }

        // Busy labels (cube numbers, board counters) set their text here: the outline follows at once, and the label
        // is no longer polled (reading TextMesh.text makes a new string every time).
        bool driven;
        public static void SetText(TextMesh label, string value)
        {
            label.text = value;
            var o = label.GetComponent<OutlinedText>();
            if (o) { o.driven = true; if (o.main) o.Sync(); }
        }

        void LateUpdate()
        {
            // Reading main.text allocates: only when the label's size changed, or every 8th frame for same-width edits.
            var now = mainRenderer ? mainRenderer.bounds.size : Vector3.zero;
            if (now != lastBounds || (!driven && ((Time.frameCount + phase) & 7) == 0))
            {
                lastBounds = now;
                if (main.text != text || main.characterSize != size || main.font != font) Sync();
            }
            if (main.color.a != alpha) Recolour();
        }

        void Sync()
        {
            text = main.text; size = main.characterSize; font = main.font;
            for (int i = 0; i < copies.Length; i++)
            {
                var t = copies[i];
                t.font = main.font; t.fontSize = main.fontSize; t.fontStyle = main.fontStyle; t.characterSize = main.characterSize;
                t.anchor = main.anchor; t.alignment = main.alignment; t.lineSpacing = main.lineSpacing; t.richText = main.richText;
                t.text = text;
                copyRenderers[i].sharedMaterial = mainRenderer.sharedMaterial;
            }
            // Outline thickness follows the line height, measured from the rendered label.
            int lines = string.IsNullOrEmpty(text) ? 1 : text.Split('\n').Length;
            float scale = Mathf.Max(.0001f, Mathf.Abs(transform.lossyScale.y));
            float line = mainRenderer.bounds.size.y / scale / lines;
            if (line <= 0) { size = -1; return; } // not laid out yet; try again next frame
            float r = line * Thickness;
            for (int i = 0; i < copies.Length; i++)
                copies[i].transform.localPosition = new Vector3(Directions[i].x * r, Directions[i].y * r - r * .45f, .01f);
        }
    }
}
