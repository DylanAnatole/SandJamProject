using UnityEngine;

namespace SandJamTest.Scene3D
{
    // Chained pair, as drawn in the current build (gameplay recordings): each cube wears a glossy tube loop in its
    // own colour, shaped like a little house — a rounded square hugging the top of the block plus a point aimed
    // at its partner. The two points overlap in the gap and cross like interlocked chain links.
    // Shown only while both cubes wait in their lanes.
    public sealed class ChainLinkView : MonoBehaviour
    {
        public SceneActorView First, Second;
        public bool IsInQueue { get; private set; } = true;
        const float TubeRadius = .062f, RingHeight = .9f, RingMargin = .04f, TipReach = .72f;
        const int TubeSides = 12;
        Loop firstLoop, secondLoop;

        sealed class Loop
        {
            public SceneActorView Owner, Partner;
            public MeshRenderer Renderer;
            public Mesh Mesh;
            public Material Material;
            public Vector3 BuiltFor = new Vector3(float.NaN, 0, 0);
            public float Lift; // keeps the two crossing points from z-fighting
            public SkinnedMeshRenderer Original; // the original chain loop mesh, when available
        }
        // Original chain (Character prefab "ChainRight"): the pCube3 loop mesh, a long flat rounded loop lying on the
        // block's top and reaching toward the partner; its blend shape breathes 0 -> 15 -> 0 every 0.83 s.
        static Mesh originalLoop; static bool originalLoaded;
        static Mesh OriginalLoop() { if (!originalLoaded) { originalLoaded = true; originalLoop = Resources.Load<Mesh>("Mechanics/ChainLoop"); } return originalLoop; }
        // Mesh extents along the loop (x: -0.55 .. 2.41) and across it (z: +-0.57).
        const float LoopMinX = -.547f, LoopLength = 2.961f, LoopHalfWidth = .567f;

        public void SetInQueue(bool inQueue)
        {
            IsInQueue = inQueue;
            // Hide at once on selection (LateUpdate would leave one frame with the loops still drawn).
            if (!inQueue && pair) pair.enabled = false;
            if (!inQueue) foreach (var loop in new[] { firstLoop, secondLoop }) if (loop != null) { if (loop.Renderer) loop.Renderer.enabled = false; if (loop.Original) loop.Original.enabled = false; }
        }

        // Original chain (Character prefab "ChainBack", mesh NurbsPath): ONE piece for the pair - a rounded square
        // loop around each block's top (submesh 0 at -x, submesh 1 at +x, each in its block's colour) and two strands
        // crossing in an X between them. Loop centres sit ~1.36 mesh units either side of the middle; 1.3 wide.
        static Mesh pairMesh; static bool pairLoaded;
        static Mesh PairMesh() { if (!pairLoaded) { pairLoaded = true; pairMesh = Resources.Load<Mesh>("Mechanics/ChainBackLoop"); } return pairMesh; }
        const float PairLoopCentre = 1.36f, PairLoopWidth = 1.3f;
        MeshRenderer pair; MeshFilter pairFilter; Material pairFirst, pairSecond; Mesh pairBent; float bentFor = -1;
        bool UpdatePair(bool visible)
        {
            var mesh = PairMesh(); if (!mesh || !mesh.isReadable) return false;
            if (!pair)
            {
                var go = new GameObject("Chain (original)"); go.transform.SetParent(First.transform.parent, false);
                pairFilter = go.AddComponent<MeshFilter>();
                pair = go.AddComponent<MeshRenderer>(); pair.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                pairFirst = new Material(Shader.Find("Standard")) { name = "Chain first" }; pairFirst.SetFloat("_Glossiness", .8f);
                pairSecond = new Material(pairFirst) { name = "Chain second" };
                pair.sharedMaterials = new[] { pairFirst, pairSecond };
            }
            pair.enabled = visible;
            if (!visible) return true;
            Paint(pairFirst, ColorOf(First)); Paint(pairSecond, ColorOf(Second));
            pairFirst.SetColor("_EmissionColor", pairFirst.color * .6f); pairSecond.SetColor("_EmissionColor", pairSecond.color * .6f);
            // Each loop wraps its block a little above mid height (original), as a round tube: the mesh keeps one
            // uniform scale, so the strands are never squashed into flat ribbons.
            var frame = Matrix4x4.TRS(new Vector3(0, .235f, 0), Quaternion.Euler(CharacterSandVessel.BlockPitch, 0, 0), Vector3.one);
            // Original: the cube further back wears its loop high, around its top; the front cube wears it low around its
            // body, so the strands slope down across the gap (side by side pairs: both at the same height).
            float h = CharacterSandVessel.BlockSize.y;
            int order = Second.SourceOrder - First.SourceOrder; // > 0: First is the back cube (higher on screen)
            float ha = order > 0 ? h * .86f : order < 0 ? h * .5f : h * .86f, hb = order > 0 ? h * .5f : order < 0 ? h * .86f : h * .86f; // side by side: both around the top, clear of the counts
            var a = First.Visual.TransformPoint(frame.MultiplyPoint3x4(new Vector3(0, ha, 0))); var b = Second.Visual.TransformPoint(frame.MultiplyPoint3x4(new Vector3(0, hb, 0)));
            var blockUp = First.Visual.TransformDirection(frame.MultiplyVector(Vector3.up)).normalized;
            var along = b - a; float distance = along.magnitude; if (distance < .01f) return true;
            along /= distance;
            // The pair's plane tilts with the slope; its up stays as close to the blocks' up as possible.
            var up = (blockUp - along * Vector3.Dot(blockUp, along)).normalized;
            float worldScale = First.Visual.lossyScale.x;
            float scale = (CharacterSandVessel.BlockSize.x + .16f) * worldScale / PairLoopWidth; // loop just wider than the block
            float half = distance * .5f / scale; // loop centre in mesh units
            if (Mathf.Abs(half - bentFor) > .01f) Bend(mesh, half);
            var t = pair.transform;
            t.position = (a + b) * .5f;
            t.rotation = Quaternion.LookRotation(Vector3.Cross(along, up), up); // local +x = along (First at -x)
            var ps = t.parent ? t.parent.lossyScale : Vector3.one;
            t.localScale = new Vector3(scale / ps.x, scale / ps.y, scale / ps.z);
            return true;
        }
        // Copy of the chain mesh with its two loops moved to +-half (their shape untouched) and the crossing strands
        // stretched or shortened to fill the gap.
        void Bend(Mesh source, float half)
        {
            bentFor = half;
            const float inner = .9f; // loops occupy |x| >= 0.9, centred on 1.36
            var v = source.vertices;
            // The loops are lengthened along the pair (the block is deeper than the loop is long), so the front strand
            // of each loop passes in front of its block like a basket, as in the original.
            const float stretch = 1.45f;
            float span = Mathf.Max(.2f, half - (PairLoopCentre - inner) * stretch);
            for (int i = 0; i < v.Length; i++)
            {
                float x = v[i].x, ax = Mathf.Abs(x), sign = Mathf.Sign(x);
                v[i].x = ax >= inner ? sign * (half + (ax - PairLoopCentre) * stretch) : x / inner * span;
            }
            if (!pairBent) { pairBent = Instantiate(source); pairBent.name = "Chain (fitted)"; pairBent.ClearBlendShapes(); }
            pairBent.vertices = v; pairBent.RecalculateBounds(); pairBent.RecalculateNormals();
            pairFilter.sharedMesh = pairBent;
        }

        void LateUpdate()
        {
            if (!First || !Second) return;
            if (UpdatePair(IsInQueue && First.gameObject.activeInHierarchy && Second.gameObject.activeInHierarchy)) return;
            if (firstLoop == null) { firstLoop = Create(First, Second, 0); secondLoop = Create(Second, First, .07f); }
            bool visible = IsInQueue && First.gameObject.activeInHierarchy && Second.gameObject.activeInHierarchy;
            Refresh(firstLoop, visible); Refresh(secondLoop, visible);
        }

        Loop Create(SceneActorView owner, SceneActorView partner, float lift)
        {
            var obj = new GameObject("Chain loop");
            obj.transform.SetParent(owner.Visual, false);
            var loop = new Loop { Owner = owner, Partner = partner, Lift = lift };
            loop.Mesh = new Mesh { name = "Chain loop" };
            obj.AddComponent<MeshFilter>().sharedMesh = loop.Mesh;
            loop.Renderer = obj.AddComponent<MeshRenderer>();
            loop.Material = new Material(Shader.Find("Standard")) { name = "Chain loop" };
            loop.Material.SetFloat("_Glossiness", .78f);
            Paint(loop.Material, ColorOf(owner));
            loop.Renderer.sharedMaterial = loop.Material;
            loop.Renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var mesh = OriginalLoop();
            if (mesh)
            {
                var go = new GameObject("Chain loop (original)"); go.transform.SetParent(owner.Visual, false);
                loop.Original = go.AddComponent<SkinnedMeshRenderer>(); loop.Original.sharedMesh = mesh;
                loop.Original.sharedMaterials = new[] { loop.Material, loop.Material };
                loop.Original.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; loop.Original.updateWhenOffscreen = true;
                loop.Renderer.enabled = false; loop.Renderer.gameObject.SetActive(false);
            }
            return loop;
        }

        // Same bright tone as the cube's sand (palette colour brightened like CharacterSandVessel).
        static Color ColorOf(SceneActorView actor)
        {
            var color = actor.Shooter != null ? ColorPalette.Of(actor.Shooter.Color) : Color.white;
            color = Color.Lerp(color * 1.2f, Color.white, .1f); color.a = 1;
            return color;
        }
        static void Paint(Material material, Color color)
        {
            material.color = color;
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * .5f); // keeps the loop as vivid as the cube in shade
        }

        void Refresh(Loop loop, bool visible)
        {
            if (loop.Original)
            {
                loop.Original.enabled = visible;
                if (!visible) return;
                // Breathing, offset per loop so the pair does not pulse in sync.
                float t = Mathf.Repeat(Time.time + loop.Lift * 3, .8333f) / .8333f;
                loop.Original.SetBlendShapeWeight(0, 15f * Mathf.Sin(t * Mathf.PI));
            }
            else
            {
                loop.Renderer.enabled = visible;
                if (!visible) return;
            }
            // Block frame: pivot at the bottom of the sand block, pitched like the block itself.
            var frame = Matrix4x4.TRS(new Vector3(0, .235f, 0), Quaternion.Euler(CharacterSandVessel.BlockPitch, 0, 0), Vector3.one);
            // Partner's top-face centre expressed in this block's frame.
            var top = new Vector3(0, CharacterSandVessel.BlockSize.y * RingHeight, 0);
            var partnerTop = loop.Partner.Visual.TransformPoint(frame.MultiplyPoint3x4(top));
            var partnerLocal = frame.inverse.MultiplyPoint3x4(loop.Owner.Visual.InverseTransformPoint(partnerTop));
            if ((partnerLocal - loop.BuiltFor).sqrMagnitude < .0004f) return;
            loop.BuiltFor = partnerLocal;
            Paint(loop.Material, ColorOf(loop.Owner));
            if (loop.Original) Place(loop, frame, partnerLocal); else Build(loop, frame, partnerLocal);
        }

        // The original loop lies flat on the block's top: its near end just behind the block's far edge, its far end
        // past the middle of the gap so it hooks through the partner's loop; as wide as the block's top.
        void Place(Loop loop, Matrix4x4 frame, Vector3 partnerTop)
        {
            var size = CharacterSandVessel.BlockSize;
            var centre = new Vector3(0, size.y * RingHeight + loop.Lift * .5f, 0);
            var toward = partnerTop - centre; toward.y = 0;
            float distance = toward.magnitude; if (distance < .01f) return;
            var dir = toward / distance;
            float half = (Mathf.Abs(dir.x) > Mathf.Abs(dir.z) ? size.x : size.z) * .5f + RingMargin;
            float near = -half, far = distance * TipReach;
            float sx = (far - near) / LoopLength, sz = ((Mathf.Abs(dir.x) > Mathf.Abs(dir.z) ? size.z : size.x) * .5f + RingMargin) / LoopHalfWidth;
            // Loop axis (mesh +x) along the partner direction, lying in the block's top plane.
            var along = Quaternion.FromToRotation(Vector3.right, dir);
            var start = centre + dir * near;
            var t = loop.Original.transform;
            t.localRotation = frame.rotation * along;
            t.localPosition = frame.MultiplyPoint3x4(start - dir * (LoopMinX * sx));
            t.localScale = new Vector3(sx, .045f / 1.0f, sz);
        }

        void Build(Loop loop, Matrix4x4 frame, Vector3 partnerTop)
        {
            var size = CharacterSandVessel.BlockSize;
            float y = size.y * RingHeight;
            var centre = new Vector3(0, y, 0);
            // Facing edge of the top face: the dominant top-plane axis towards the partner.
            var toward = partnerTop - centre;
            bool alongX = Mathf.Abs(toward.x) > Mathf.Abs(toward.z);
            var d = alongX ? new Vector3(Mathf.Sign(toward.x), 0, 0) : new Vector3(0, 0, Mathf.Sign(toward.z));
            var s = alongX ? new Vector3(0, 0, 1) : new Vector3(1, 0, 0);
            float hd = (alongX ? size.x : size.z) * .5f + RingMargin, hs = (alongX ? size.z : size.x) * .5f + RingMargin;
            // The point goes past the middle of the gap so it hooks through the partner's point (an X).
            var tip = centre + toward * TipReach + Vector3.up * loop.Lift;
            var corners = new[]
            {
                centre - d * hd - s * hs, centre - d * hd + s * hs,
                centre + d * hd + s * hs, tip, centre + d * hd - s * hs,
            };
            var radii = new[] { .09f, .09f, .07f, .06f, .07f };
            var path = Round(corners, radii, 5);
            for (int i = 0; i < path.Length; i++) path[i] = frame.MultiplyPoint3x4(path[i]);
            Tube(loop.Mesh, path, frame.MultiplyVector(Vector3.up));
        }

        // Closed polygon with each corner replaced by a short quadratic arc of the given radius.
        static Vector3[] Round(Vector3[] corners, float[] radii, int steps)
        {
            var list = new System.Collections.Generic.List<Vector3>();
            int n = corners.Length;
            for (int i = 0; i < n; i++)
            {
                Vector3 prev = corners[(i - 1 + n) % n], v = corners[i], next = corners[(i + 1) % n];
                float r = radii[i];
                var a = v + (prev - v).normalized * Mathf.Min(r, Vector3.Distance(prev, v) * .45f);
                var b = v + (next - v).normalized * Mathf.Min(r, Vector3.Distance(next, v) * .45f);
                for (int k = 0; k <= steps; k++)
                {
                    float t = k / (float)steps;
                    list.Add((1 - t) * (1 - t) * a + 2 * (1 - t) * t * v + t * t * b);
                }
            }
            return list.ToArray();
        }
        // Closed tube along the path (round cross-section, smooth normals).
        static void Tube(Mesh mesh, Vector3[] path, Vector3 up)
        {
            int n = path.Length;
            var vertices = new Vector3[n * TubeSides]; var normals = new Vector3[n * TubeSides]; var triangles = new int[n * TubeSides * 6];
            for (int i = 0; i < n; i++)
            {
                var tangent = (path[(i + 1) % n] - path[(i - 1 + n) % n]).normalized;
                var side = Vector3.Cross(tangent, up); if (side.sqrMagnitude < 1e-6f) side = Vector3.Cross(tangent, Vector3.right);
                side.Normalize();
                var normalUp = Vector3.Cross(side, tangent).normalized;
                for (int k = 0; k < TubeSides; k++)
                {
                    float angle = k * Mathf.PI * 2 / TubeSides;
                    var normal = side * Mathf.Cos(angle) + normalUp * Mathf.Sin(angle);
                    vertices[i * TubeSides + k] = path[i] + normal * TubeRadius; normals[i * TubeSides + k] = normal;
                }
            }
            int t = 0;
            for (int i = 0; i < n; i++)
                for (int k = 0; k < TubeSides; k++)
                {
                    int a = i * TubeSides + k, b = i * TubeSides + (k + 1) % TubeSides;
                    int c = ((i + 1) % n) * TubeSides + k, e = ((i + 1) % n) * TubeSides + (k + 1) % TubeSides;
                    // Unity front faces: cross(b - a, c - a) points outwards.
                    triangles[t++] = a; triangles[t++] = c; triangles[t++] = b;
                    triangles[t++] = b; triangles[t++] = c; triangles[t++] = e;
                }
            mesh.Clear(); mesh.vertices = vertices; mesh.normals = normals; mesh.triangles = triangles; mesh.RecalculateBounds();
        }

        void OnDestroy()
        {
            if (pair) Destroy(pair.gameObject); if (pairFirst) Destroy(pairFirst); if (pairSecond) Destroy(pairSecond); if (pairBent) Destroy(pairBent);
            foreach (var loop in new[] { firstLoop, secondLoop })
            {
                if (loop == null) continue;
                if (loop.Renderer) Destroy(loop.Renderer.gameObject);
                if (loop.Original) Destroy(loop.Original.gameObject);
                if (loop.Mesh) Destroy(loop.Mesh);
                if (loop.Material) Destroy(loop.Material);
            }
        }
    }
}
