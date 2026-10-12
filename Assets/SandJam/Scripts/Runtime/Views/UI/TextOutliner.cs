using System.Collections.Generic;
using UnityEngine;

namespace SandJamTest.Scene3D
{
    // Original UI text (Lilita One "WithOutline"): white letters with a thick dark purple outline that sits a little
    // lower than the letters. This scans the screens for white labels that have no outline yet and gives each one
    // eight dark copies behind it, kept in sync with the label's text, size and fade.
    public sealed class TextOutliner : MonoBehaviour
    {
        public static readonly Color Ink = new Color(.17f, .12f, .3f);
        const float Rescan = .4f;
        float nextScan;
        readonly HashSet<TextMesh> seen = new HashSet<TextMesh>();

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
            foreach (var label in GetComponentsInChildren<TextMesh>(false))
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

    public sealed class OutlinedText : MonoBehaviour
    {
        static readonly Vector2[] Directions =
        {
            new Vector2(1, 0), new Vector2(-1, 0), new Vector2(0, 1), new Vector2(0, -1),
            new Vector2(.71f, .71f), new Vector2(-.71f, .71f), new Vector2(.71f, -.71f), new Vector2(-.71f, -.71f),
        };
        TextMesh main;
        MeshRenderer mainRenderer;
        readonly TextMesh[] copies = new TextMesh[Directions.Length];
        string text; float size = -1, alpha = -1; Font font;

        void Awake()
        {
            main = GetComponent<TextMesh>(); mainRenderer = GetComponent<MeshRenderer>();
            // Older, thinner outlines (scene builder / popup helpers) are replaced by this one.
            foreach (Transform child in transform) if (child.name == "Text outline") child.gameObject.SetActive(false);
            for (int i = 0; i < copies.Length; i++)
            {
                var go = new GameObject("Text outline") { layer = gameObject.layer };
                go.transform.SetParent(transform, false);
                copies[i] = go.AddComponent<TextMesh>();
                var r = go.GetComponent<MeshRenderer>();
                r.sortingLayerID = mainRenderer.sortingLayerID; r.sortingOrder = mainRenderer.sortingOrder; // same order: depth puts it just behind the letters
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            }
        }

        void LateUpdate()
        {
            if (main.text != text || main.characterSize != size || main.font != font) Sync();
            if (main.color.a != alpha)
            {
                alpha = main.color.a;
                var c = TextOutliner.Ink; c.a = alpha;
                foreach (var t in copies) t.color = c;
            }
        }

        void Sync()
        {
            text = main.text; size = main.characterSize; font = main.font;
            foreach (var t in copies)
            {
                t.font = main.font; t.fontSize = main.fontSize; t.fontStyle = main.fontStyle; t.characterSize = main.characterSize;
                t.anchor = main.anchor; t.alignment = main.alignment; t.lineSpacing = main.lineSpacing; t.richText = main.richText;
                t.text = text;
                t.GetComponent<MeshRenderer>().sharedMaterial = mainRenderer.sharedMaterial;
            }
            // Outline thickness follows the line height, measured from the rendered label.
            int lines = string.IsNullOrEmpty(text) ? 1 : text.Split('\n').Length;
            float scale = Mathf.Max(.0001f, Mathf.Abs(transform.lossyScale.y));
            float line = mainRenderer.bounds.size.y / scale / lines;
            if (line <= 0) { size = -1; return; } // not laid out yet; try again next frame
            float r = line * .08f;
            for (int i = 0; i < copies.Length; i++)
                copies[i].transform.localPosition = new Vector3(Directions[i].x * r, Directions[i].y * r - r * .45f, .01f);
        }
    }
}
