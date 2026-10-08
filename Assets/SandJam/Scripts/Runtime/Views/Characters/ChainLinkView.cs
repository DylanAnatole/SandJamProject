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
        const float TubeRadius = .045f, RingHeight = .9f, RingMargin = .04f, TipReach = .72f;
        const int TubeSides = 8;
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

        public void SetInQueue(bool inQueue)
        {
            IsInQueue = inQueue;
            // Hide at once on selection (LateUpdate would leave one frame with the loops still drawn).
            if (!inQueue) foreach (var loop in new[] { firstLoop, secondLoop }) if (loop != null && loop.Renderer) loop.Renderer.enabled = false;
        }
        // True when both loops are drawn (used by the chain smoke test).
        public bool LoopsVisible { get { return firstLoop != null && firstLoop.Renderer.enabled && secondLoop.Renderer.enabled; } }

        void LateUpdate()
        {
            if (!First || !Second) return;
            if (firstLoop == null) { firstLoop = Create(First, Second, 0); secondLoop = Create(Second, First, .045f); }
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
            color = Color.Lerp(color * 1.2f, Color.white, .1f); color.a = 1;
            return color;
        }
        static void Paint(Material material, Color color)
        {
            material.color = color;
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * .35f); // keeps the loop as vivid as the cube in shade
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
