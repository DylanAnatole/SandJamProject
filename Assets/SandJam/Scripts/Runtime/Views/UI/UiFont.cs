using UnityEngine;

namespace SandJamTest.Scene3D
{
    // The original UI font is Lilita One (Google Fonts, SIL Open Font License; see Assets/SandJam/Fonts).
    // Every TextMesh in the game is switched to it; the font is already heavy, so synthetic bold is dropped.
    public static class UiFont
    {
        static Font font;
        public static Font Font { get { if (!font) font = Resources.Load<Font>("LilitaOne-Regular"); return font; } }

        // Lilita One draws smaller than Inter at the same size; UI labels (laid out for Inter) grow to match.
        const float UiGrowth = 1.15f;

        public static void Apply(TextMesh label)
        {
            var f = Font;
            if (!f || !label || label.font == f) return;
            label.font = f; label.fontStyle = FontStyle.Normal;
            if (label.name != "Text outline" && label.GetComponentInParent<VideoScreen>()) label.transform.localScale *= UiGrowth;
            var r = label.GetComponent<MeshRenderer>(); if (r) r.sharedMaterial = f.material;
        }

        public static void ApplyAll() { foreach (var label in Object.FindObjectsOfType<TextMesh>(true)) Apply(label); }
    }
}
