using System.Collections.Generic;
using UnityEngine;

namespace SandJamTest
{
    public static class RendererExtensions
    {
        // Scales the renderer's transform so its drawn bounds measure width x height in world units (z untouched).
        // Measures the unscaled bounds first, so it works whatever the sprite size, pixels per unit or parent scale.
        public static void FitWorldSize(this Renderer renderer, float width, float height)
        {
            var t = renderer.transform; var z = t.localScale.z;
            t.localScale = new Vector3(1, 1, z);
            var size = renderer.bounds.size;
            t.localScale = new Vector3(width / Mathf.Max(.0001f, size.x), height / Mathf.Max(.0001f, size.y), z);
        }

        // World bounds around all the renderers (empty bounds at the origin when there are none).
        public static Bounds CombinedBounds(this IList<Renderer> renderers)
        {
            if (renderers.Count == 0) return new Bounds();
            var b = renderers[0].bounds;
            for (int i = 1; i < renderers.Count; i++) b.Encapsulate(renderers[i].bounds);
            return b;
        }

        // First descendant (inactive included, the root itself too) with this exact name; null when none.
        public static Transform FindDeep(this Transform root, string name)
        {
            var direct = root.Find(name); if (direct) return direct;
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }
    }
}
