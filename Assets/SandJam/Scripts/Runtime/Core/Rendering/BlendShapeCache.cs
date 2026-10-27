using System.Collections.Generic;
using UnityEngine;

namespace SandJamTest
{
    // A mesh's first blend shape baked at a few fixed amounts (original mechanic meshes: the ice cube melting away, the
    // padlock's shackle swinging open). Each step is baked once and shared by every renderer; swapping sharedMesh costs
    // nothing per frame, where a SkinnedMeshRenderer re-skins every frame on mobile.
    public static class BlendShapeCache
    {
        public const int Steps = 20;
        static readonly Dictionary<Mesh, Mesh[]> baked = new Dictionary<Mesh, Mesh[]>();
        static SkinnedMeshRenderer baker;

        // amount 0..1 of the blend shape (0 = the mesh itself).
        public static Mesh At(Mesh source, float amount)
        {
            if (!source || source.blendShapeCount == 0 || !source.isReadable) return source; // baking needs Read/Write
            int step = Mathf.Clamp(Mathf.RoundToInt(amount * Steps), 0, Steps);
            if (step == 0) return source;
            Mesh[] steps;
            if (!baked.TryGetValue(source, out steps)) baked.Add(source, steps = new Mesh[Steps + 1]);
            if (!steps[step]) steps[step] = Bake(source, step * 100f / Steps);
            return steps[step];
        }

        // Bakes every step now (while a level builds), so none is baked mid-play when the ice melts or the lock opens.
        public static void Prewarm(Mesh source)
        {
            for (int step = 1; step <= Steps; step++) At(source, (float)step / Steps);
        }

        static Mesh Bake(Mesh source, float weight)
        {
            if (!baker)
            {
                var obj = new GameObject("Blend shape baker") { hideFlags = HideFlags.HideAndDontSave };
                baker = obj.AddComponent<SkinnedMeshRenderer>(); baker.enabled = false;
            }
            baker.sharedMesh = source;
            baker.SetBlendShapeWeight(0, weight);
            var mesh = new Mesh { name = source.name + " " + Mathf.RoundToInt(weight), hideFlags = HideFlags.DontSave };
            baker.BakeMesh(mesh);
            mesh.RecalculateBounds();
            mesh.UploadMeshData(true); // only drawn: drops the CPU copy, about half of the ~15 MB the ice and padlock steps take
            return mesh;
        }
    }
}
