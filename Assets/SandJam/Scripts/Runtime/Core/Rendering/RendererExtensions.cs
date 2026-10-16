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
    }
}
