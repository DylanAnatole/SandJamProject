using UnityEngine;

namespace SandJamTest.Scene3D
{
    // Chained pair, drawn with the original chain meshes while both cubes wait in their lanes. As in the original
    // Character prefab each chain is two copies, one around the blocks' top and one lower down, so the pair reads
    // as a cage:
    // - one behind the other (same lane): "ChainBack" (NurbsPath), a loop around each block crossing in an X;
    // - side by side (two lanes): "ChainRight" (pCube3) squashed flat into a ring around both blocks.
    // Each half is in its block's colour.
    // Without the original meshes each cube gets a procedural tube loop pointing at its partner.
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
        }
        // Side-by-side cage (pCube3): x -0.55 .. 2.41, two halves (submesh 1 at -x, submesh 0 at +x) centred
        // 0.74 either side of x 0.93, each 1.48 wide; y centred on -0.04. The original squashes it to 4% of its
        // height (0.604 x 0.040 x 0.715) so it lies flat; rings around the blocks' top and lower body.
        static Mesh cageMesh; static bool cageLoaded;
        static Mesh CageMesh() { if (!cageLoaded) { cageLoaded = true; cageMesh = Resources.Load<Mesh>("Mechanics/ChainLoop"); } return cageMesh; }
        static readonly Vector3 CageCentre = new Vector3(.93f, -.04f, 0);
        const float CageHalfCentre = .74f, CageHalfWidth = 1.48f, CageMeshDepth = 1.134f, CageFlat = .040f / .604f;
        // Top copy height (fraction of the block height) and how far below it the second copy sits, in mesh units
        // of each chain (original: ChainRight (1) 13.78 x 0.040 / 0.604, ChainBack (1) 0.859).
        const float ChainTop = .95f, CageLowerDrop = 13.78f * .0404f / .604f, BackLowerDrop = .8588f;

        public void SetInQueue(bool inQueue)
        {
            IsInQueue = inQueue;
            // Hide at once on selection (LateUpdate would leave one frame with the loops still drawn).
            if (!inQueue && pair) pair.enabled = false;
            if (!inQueue) foreach (var loop in new[] { firstLoop, secondLoop }) if (loop != null && loop.Renderer) loop.Renderer.enabled = false;
        }

        // Original chain (Character prefab "ChainBack", mesh NurbsPath): ONE piece for the pair - a rounded square
        // loop around each block's top (submesh 0 at -x, submesh 1 at +x, each in its block's colour) and two strands
        // crossing in an X between them. Loop centres sit ~1.36 mesh units either side of the middle; 1.3 wide.
        static Mesh pairMesh; static bool pairLoaded;
        static Mesh PairMesh() { if (!pairLoaded) { pairLoaded = true; pairMesh = Resources.Load<Mesh>("Mechanics/ChainBackLoop"); } return pairMesh; }
        const float PairLoopCentre = 1.36f, PairLoopWidth = 1.3f;
        MeshRenderer pair, lowerRing; MeshFilter pairFilter; Material pairFirst, pairSecond; Mesh pairBent, bentSource; float bentFor = -1;
        bool UpdatePair(bool visible)
        {
            int order = Second.SourceOrder - First.SourceOrder; // > 0: First is the back cube (higher on screen)
            var cage = order == 0 ? CageMesh() : null; if (cage && !cage.isReadable) cage = null;
            var mesh = cage ? cage : PairMesh(); if (!mesh || !mesh.isReadable) return false;
            if (!pair)
            {
                var go = new GameObject("Chain (original)"); go.transform.SetParent(First.transform.parent, false);
                pairFilter = go.AddComponent<MeshFilter>();
                pair = go.AddComponent<MeshRenderer>(); pair.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                pairFirst = new Material(Shader.Find("Standard")) { name = "Chain first" }; pairFirst.SetFloat("_Glossiness", .7f);
                pairSecond = new Material(pairFirst) { name = "Chain second" };
                pair.sharedMaterials = new[] { pairFirst, pairSecond };
            }
            pair.enabled = visible;
            if (lowerRing) lowerRing.enabled = visible;
            if (!visible) return true;
            Paint(pairFirst, ColorOf(First)); Paint(pairSecond, ColorOf(Second));
            // Submesh 0 is the -x half of the back chain but the +x half of the cage; First always sits at -x.
            var materials = cage ? new[] { pairSecond, pairFirst } : new[] { pairFirst, pairSecond };
            if (pair.sharedMaterials[0] != materials[0]) pair.sharedMaterials = materials;
            pairFirst.SetColor("_EmissionColor", pairFirst.color * .55f); pairSecond.SetColor("_EmissionColor", pairSecond.color * .55f);
            // Both blocks wear their loops at the same height (the original chains lie level, tilted with the blocks).
            var frame = Matrix4x4.TRS(new Vector3(0, .235f, 0), Quaternion.Euler(CharacterSandVessel.BlockPitch, 0, 0), Vector3.one);
            float top = CharacterSandVessel.BlockSize.y * ChainTop;
            var a = First.Visual.TransformPoint(frame.MultiplyPoint3x4(new Vector3(0, top, 0))); var b = Second.Visual.TransformPoint(frame.MultiplyPoint3x4(new Vector3(0, top, 0)));
            var blockUp = First.Visual.TransformDirection(frame.MultiplyVector(Vector3.up)).normalized;
            var along = b - a; float distance = along.magnitude; if (distance < .01f) return true;
            along /= distance;
            var up = (blockUp - along * Vector3.Dot(blockUp, along)).normalized;
            float worldScale = First.Visual.lossyScale.x;
            // Round tubes: one uniform scale (the cage only squashed flat, as the original). The back chain's loops hug
            // the block; the cage ring stands a little off it.
            float scale = (CharacterSandVessel.BlockSize.x + (cage ? .10f : .12f)) * worldScale / (cage ? CageHalfWidth : PairLoopWidth);
            float half = distance * .5f / scale; // loop centre in mesh units
            if (Mathf.Abs(half - bentFor) > .01f || mesh != bentSource)
            {
                // The loops keep their shape; only the part between them stretches across the gap.
                if (cage) Bend(mesh, half, CageCentre, CageHalfCentre, .35f, 0);
                else Bend(mesh, half, Vector3.zero, PairLoopCentre, .9f, TubeInflate);
            }
            var t = pair.transform;
            t.position = (a + b) * .5f;
            t.rotation = Quaternion.LookRotation(Vector3.Cross(along, up), up); // local +x = along (First at -x)
            var ps = t.parent ? t.parent.lossyScale : Vector3.one;
            // The cage ring clears the block on every side (front rails stay in front of the tilted block).
            float depth = (CharacterSandVessel.BlockSize.z + .16f) * worldScale / CageMeshDepth;
            t.localScale = cage ? new Vector3(scale / ps.x, scale * CageFlat / ps.y, depth / ps.z) : new Vector3(scale / ps.x, scale / ps.y, scale / ps.z);
            if (!lowerRing)
            {
                var go = new GameObject("Chain (original, lower copy)"); go.transform.SetParent(t.parent, false);
                go.AddComponent<MeshFilter>();
                lowerRing = go.AddComponent<MeshRenderer>(); lowerRing.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            lowerRing.GetComponent<MeshFilter>().sharedMesh = pairFilter.sharedMesh;
            if (lowerRing.sharedMaterials.Length == 0 || lowerRing.sharedMaterials[0] != materials[0]) lowerRing.sharedMaterials = materials;
            lowerRing.transform.SetPositionAndRotation(t.position - up * ((cage ? CageLowerDrop : BackLowerDrop) * scale), t.rotation);
            lowerRing.transform.localScale = t.localScale;
            return true;
        }
        // The original back-chain tubes read about twice as thick as the mesh gives at our block size: every vertex is
        // pushed out along its normal by this much (mesh units), which keeps the tubes round. (The cage mesh has hard,
        // split edges that this would tear apart; its rails are already thick enough.)
        const float TubeInflate = .03f;
        // Copy of a chain mesh, re-centred on centre, with its two block loops moved to +-half (shape kept) and the
        // part between them (|x| < inner) stretched or shortened to fill the gap; tubes thickened by inflate.
        void Bend(Mesh source, float half, Vector3 centre, float loopCentre, float inner, float inflate)
        {
            bentFor = half;
            if (source != bentSource) { if (pairBent) Destroy(pairBent); pairBent = null; bentSource = source; }
            var v = source.vertices; var n = source.normals;
            float span = Mathf.Max(.2f, half - (loopCentre - inner));
            for (int i = 0; i < v.Length; i++)
            {
                v[i] -= centre;
                float x = v[i].x, ax = Mathf.Abs(x), sign = Mathf.Sign(x);
                v[i].x = ax >= inner ? sign * (half + ax - loopCentre) : x / inner * span;
                if (inflate > 0 && n.Length == v.Length) v[i] += n[i] * inflate;
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
            return loop;
        }

        // Same bright tone as the cube's sand (palette colour brightened like CharacterSandVessel).
        static Color ColorOf(SceneActorView actor)
        {
            var color = actor.Shooter != null ? ColorPalette.Of(actor.Shooter.Color) : Color.white;
            color *= 1.15f; color.a = 1;
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
            loop.Renderer.enabled = visible;
            if (!visible) return;
            // Block frame: pivot at the bottom of the sand block, pitched like the block itself.
            var frame = Matrix4x4.TRS(new Vector3(0, .235f, 0), Quaternion.Euler(CharacterSandVessel.BlockPitch, 0, 0), Vector3.one);
            // Partner's top-face centre expressed in this block's frame.
            var top = new Vector3(0, CharacterSandVessel.BlockSize.y * RingHeight, 0);
            var partnerTop = loop.Partner.Visual.TransformPoint(frame.MultiplyPoint3x4(top));
            var partnerLocal = frame.inverse.MultiplyPoint3x4(loop.Owner.Visual.InverseTransformPoint(partnerTop));
            if ((partnerLocal - loop.BuiltFor).sqrMagnitude < .0004f) return;
            loop.BuiltFor = partnerLocal;
            Paint(loop.Material, ColorOf(loop.Owner));
            Build(loop, frame, partnerLocal);
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
            if (lowerRing) Destroy(lowerRing.gameObject);
            foreach (var loop in new[] { firstLoop, secondLoop })
            {
                if (loop == null) continue;
                if (loop.Renderer) Destroy(loop.Renderer.gameObject);
                if (loop.Mesh) Destroy(loop.Mesh);
                if (loop.Material) Destroy(loop.Material);
            }
        }
    }
}
