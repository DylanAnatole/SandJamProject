using System.Linq;
using UnityEngine;

namespace SandJamTest.Scene3D
{
    // Runtime restyle toward the original's bright 3D look (colours sampled from gameplay video):
    // lavender gradient backdrop, filled letterbox bars and waiting pads lying on the floor.
    // Applied at runtime so every baked level prefab picks it up without regeneration.
    public sealed class OriginalLookStyler : MonoBehaviour
    {
        public static readonly Color BackdropTop = Hex(0xECEBF8), BackdropMiddle = Hex(0xE7E7F4), BackdropBottom = Hex(0xCBC8F2);
        // Campaign levels use the original stage theme of their 15-level block (OriginalThemes): blue, lavender
        // (the tuned colours above) or dark — backdrop, picture frame and lane banks are recoloured.
        static readonly string[] FrameParts = { "Solid board frame", "Raised board frame", "Tray top", "Tray bevel", "Extruded plinth" };
        static readonly string[] SideParts = { "Left lane bank", "Right booster bank", "Left solid rail", "Right solid rail", "Left rail top", "Right rail top", "Left lane rail", "Right booster rail" };
        public float PadTilt = 55f, PadThickness = 1f, PadDepth = 1.55f;
        public int ThemeIndex { get; private set; }
        Color top, middle, bottom;
        Camera filler;
        Mesh gradientMesh;
        Material gradientMaterial;

        // Measured on recording 2 (level 267, 888 px wide): at the same camera the original picture is ~3.5% larger,
        // and its frame border is ~6.7% of the picture's width at the sides and top. The bottom border is the same and
        // disappears into the stand's slot (the stand's top meets the picture's lower edge).
        const float PictureScale = 1.035f, FrameSide = .067f;
        public void Apply(SandJamSceneController controller)
        {
            var picture = controller.Board ? controller.Board.transform.parent : null;
            if (picture)
            {
                // Grown about its own centre (board, regions and counters move with it); the frame follows below.
                var c = controller.Board.Renderer.bounds.center; var p = picture.position;
                picture.localScale *= PictureScale;
                picture.position = new Vector3(c.x + (p.x - c.x) * PictureScale, c.y + (p.y - c.y) * PictureScale, p.z);
            }
            var levels = FindObjectOfType<LevelManager>();
            ThemeIndex = levels ? OriginalThemes.IndexForLevel(levels.PlayedNumber > 0 ? levels.PlayedNumber : Campaign.LevelNumber) : OriginalThemes.Lavender;
            top = BackdropTop; middle = BackdropMiddle; bottom = BackdropBottom;
            if (ThemeIndex != OriginalThemes.Lavender) ApplyTheme(controller, OriginalThemes.All[ThemeIndex]);
            // The light "Inset rim" plate behind the picture is a bit larger than the picture and showed as a pale
            // seam along the frame. Paint it with the frame's own shadow tone so the picture meets the frame cleanly.
            foreach (var r in controller.GetComponentsInChildren<SpriteRenderer>(true))
                if (r.name == "Inset rim")
                {
                    var frame = controller.GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault(f => f.name == "Raised board frame");
                    r.color = Color.Lerp(frame ? frame.color : new Color(.37f, .29f, .5f), Color.black, .35f);
                }
            BuildBoardBacking(controller);
            var cam = controller.GameCamera;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = middle;
            // Letterbox bars outside the 0.6 play aspect use the same lavender instead of black/grey.
            filler = new GameObject("Backdrop letterbox camera").AddComponent<Camera>();
            filler.transform.SetParent(transform, false);
            filler.depth = cam.depth - 10; filler.cullingMask = 0;
            filler.clearFlags = CameraClearFlags.SolidColor; filler.backgroundColor = bottom;
            BuildGradient(cam);
            var stash = controller.StashSlots.Length > 0 ? controller.StashSlots[0].parent : null;
            if (stash)
                foreach (Transform child in stash)
                    if (child.name.StartsWith("Solid waiting pad"))
                    {
                        // Original pads are tiles lying nearly flat in front of the stand, deep enough to read as
                        // ~0.66 of their width on screen (recording 2: 112 x 75 px), skewed by the perspective.
                        child.localRotation = Quaternion.Euler(PadTilt, 0, 0);
                        var ls = child.localScale; child.localScale = new Vector3(ls.x, ls.y * PadDepth, PadThickness);
                    }
            // A cube in the stash stands in the middle of its pad (original), not in front of it.
            for (int i = 0; stash && i < controller.StashSlots.Length; i++)
            {
                var pad = stash.Find("Solid waiting pad " + i); var pr = pad ? pad.GetComponent<Renderer>() : null; if (!pr) continue;
                var slot = controller.StashSlots[i]; var pc = pr.bounds.center;
                slot.position = new Vector3(slot.position.x, pc.y, pc.z);
            }
            EvenFrameBorder(controller);
            RoundFrame(controller);
            WhiteRailEdges(controller);
            TiltLaneWalls(controller);
            BuildFrameFeet(controller);
        }

        // The frame mesh leaves a thinner border above the picture than at its sides. Stretch the frame (and its
        // shadow) upward, bottom fixed, so the top border matches the side borders as in the original.
        // Original frame: generously rounded outer corners and a rounded opening (the picture's corners tucked under
        // the frame). The modelled frame was square; it is rebuilt as a rounded ring with outer and inner walls in the
        // same material, its face just in front of the picture.
        const int CornerSteps = 10;
        void RoundFrame(SandJamSceneController controller)
        {
            var board = controller.Board ? controller.Board.Renderer : null;
            var frame = controller.GetComponentsInChildren<MeshRenderer>(true).FirstOrDefault(f => f.name == "Solid board frame");
            if (!board || !frame) return;
            var o = frame.bounds; var b = board.bounds;
            float front = Mathf.Min(o.min.z, b.min.z - .02f), back = o.max.z;
            // Border from the picture, in the original's proportions (the modelled frame was thicker at the sides).
            float side = b.size.x * FrameSide;
            var outer = RoundedLoop(b.min.x - side, b.max.x + side, b.min.y - side, b.max.y + side, .34f);
            var inner = RoundedLoop(b.min.x, b.max.x, b.min.y, b.max.y, .16f);
            var v = new System.Collections.Generic.List<Vector3>(); var n = new System.Collections.Generic.List<Vector3>(); var t = new System.Collections.Generic.List<int>();
            int count = outer.Count;
            // Face.
            for (int i = 0; i < count; i++) { v.Add(new Vector3(outer[i].x, outer[i].y, front)); n.Add(Vector3.back); v.Add(new Vector3(inner[i].x, inner[i].y, front)); n.Add(Vector3.back); }
            for (int i = 0; i < count; i++) { int j = (i + 1) % count, a = i * 2, c = j * 2; t.AddRange(new[] { a, a + 1, c, a + 1, c + 1, c }); } // clockwise as seen by the camera
            // Walls (outer facing out, inner facing in).
            Wall(outer, front, back, true, v, n, t); Wall(inner, front, back, false, v, n, t);
            var mesh = new Mesh { name = "Rounded board frame" };
            mesh.SetVertices(v); mesh.SetNormals(n); mesh.SetTriangles(t, 0); mesh.RecalculateBounds();
            var go = new GameObject("Rounded board frame") { layer = frame.gameObject.layer };
            go.transform.SetParent(frame.transform.parent, false);
            go.transform.position = Vector3.zero; go.transform.rotation = Quaternion.identity;
            var ps = go.transform.parent ? go.transform.parent.lossyScale : Vector3.one;
            go.transform.localScale = new Vector3(1 / ps.x, 1 / ps.y, 1 / ps.z);
            go.transform.position = Vector3.zero;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterials = frame.sharedMaterials.Length > 0 ? new[] { frame.sharedMaterials[0] } : frame.sharedMaterials;
            r.shadowCastingMode = frame.shadowCastingMode; r.receiveShadows = frame.receiveShadows;
            // Measured face colour of the original frame (86,114,163): a little deeper and bluer than the old frame.
            if (r.sharedMaterial && r.sharedMaterial.HasProperty("_Color")) { var m = r.material; m.color = m.color * new Color(.87f, .87f, 1.02f, 1); }
            frame.enabled = false;
        }
        static System.Collections.Generic.List<Vector2> RoundedLoop(float x0, float x1, float y0, float y1, float radius)
        {
            radius = Mathf.Min(radius, (x1 - x0) * .5f, (y1 - y0) * .5f);
            var centres = new[] { new Vector2(x1 - radius, y1 - radius), new Vector2(x0 + radius, y1 - radius), new Vector2(x0 + radius, y0 + radius), new Vector2(x1 - radius, y0 + radius) };
            var list = new System.Collections.Generic.List<Vector2>();
            for (int c = 0; c < 4; c++)
                for (int k = 0; k <= CornerSteps; k++)
                {
                    float a = (c * 90 + k * 90f / CornerSteps) * Mathf.Deg2Rad;
                    list.Add(centres[c] + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius);
                }
            return list;
        }
        static void Wall(System.Collections.Generic.List<Vector2> loop, float front, float back, bool outward, System.Collections.Generic.List<Vector3> v, System.Collections.Generic.List<Vector3> n, System.Collections.Generic.List<int> t)
        {
            int count = loop.Count, start = v.Count;
            Vector2 centre = Vector2.zero; foreach (var p in loop) centre += p; centre /= count;
            for (int i = 0; i < count; i++)
            {
                var dir = (loop[i] - centre).normalized; var normal = new Vector3(dir.x, dir.y, 0) * (outward ? 1 : -1);
                v.Add(new Vector3(loop[i].x, loop[i].y, front)); n.Add(normal); v.Add(new Vector3(loop[i].x, loop[i].y, back)); n.Add(normal);
            }
            for (int i = 0; i < count; i++)
            {
                int j = (i + 1) % count, a = start + i * 2, c = start + j * 2;
                if (outward) t.AddRange(new[] { a, a + 1, c, c, a + 1, c + 1 }); else t.AddRange(new[] { a, c, a + 1, a + 1, c, c + 1 });
            }
        }
        void EvenFrameBorder(SandJamSceneController controller)
        {
            var board = controller.Board ? controller.Board.Renderer : null;
            var frame = controller.GetComponentsInChildren<Renderer>(true).FirstOrDefault(f => f.name == "Solid board frame");
            if (!board || !frame) return;
            var b = board.bounds; var f0 = frame.bounds;
            float side = ((b.min.x - f0.min.x) + (f0.max.x - b.max.x)) * .5f;
            float extra = (b.max.y + side) - f0.max.y;
            if (extra <= .005f) return;
            // Only the top border grows: vertices above the picture's top edge move up, so the frame's opening
            // still fits the picture exactly (stretching the whole frame left a gap above the picture).
            var filter = frame.GetComponent<MeshFilter>();
            if (filter && filter.sharedMesh)
            {
                var t = frame.transform; var mesh = Instantiate(filter.sharedMesh); mesh.name = filter.sharedMesh.name + " (even border)";
                var vertices = mesh.vertices;
                // The mesh's opening is also a little taller than the picture: its inner top edge is pulled down onto
                // the picture's top edge, the outer edge up by `extra`, everything in between scaled.
                float innerTop = float.MaxValue, outerTop = f0.max.y;
                for (int i = 0; i < vertices.Length; i++)
                {
                    var w = t.TransformPoint(vertices[i]);
                    if (w.y > b.max.y + .005f && Mathf.Abs(w.x - b.center.x) < b.extents.x * .8f) innerTop = Mathf.Min(innerTop, w.y);
                }
                float gap = innerTop < outerTop ? innerTop - b.max.y : 0;
                for (int i = 0; i < vertices.Length; i++)
                {
                    var w = t.TransformPoint(vertices[i]);
                    if (w.y <= b.max.y + .005f) continue;
                    float k = Mathf.Clamp01((w.y - innerTop) / Mathf.Max(.0001f, outerTop - innerTop));
                    w.y += Mathf.Lerp(-gap, extra, k); vertices[i] = t.InverseTransformPoint(w);
                }
                mesh.vertices = vertices; mesh.RecalculateBounds();
                filter.sharedMesh = mesh;
            }
            // The flat shadow behind the frame simply follows the new outline.
            foreach (var r in controller.GetComponentsInChildren<Renderer>(true))
            {
                if (r.name != "Frame shadow" && r.name != "Raised board frame") continue;
                var rt = r.transform; var rb = r.bounds;
                if (rb.size.y <= 0) continue;
                float k = (rb.size.y + extra) / rb.size.y;
                var ls = rt.localScale; rt.localScale = new Vector3(ls.x, ls.y * k, ls.z);
                var nb = r.bounds; rt.position += new Vector3(0, rb.min.y - nb.min.y, 0);
            }
        }

        // The lane walls follow the same floor perspective as the cubes (SceneActorView.DepthOffset): their lower
        // ends lean toward the camera, so the walls fan out with the lanes.
        static readonly string[] LaneWalls = { "Left lane rail", "Left lane bank", "Right booster rail", "Right booster bank",
            "Left solid rail", "Left rail top", "Right solid rail", "Right rail top" };
        // Original lanes: the walls' lane-facing edge shows as a bright white strip beside the dark wall top (both
        // walls, every level). The solid rail under each wall top is that strip; it shared the dark top colour.
        void WhiteRailEdges(SandJamSceneController controller)
        {
            foreach (var r in controller.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (r.name != "Left solid rail" && r.name != "Right solid rail") continue;
                var m = r.material; // per-level instance, freed with the scene
                if (m.HasProperty("_Color")) m.color = new Color(.96f, .95f, .97f);
                if (m.HasProperty("_ShadeStrength")) m.SetFloat("_ShadeStrength", .35f);
                if (m.HasProperty("_GrainStrength")) m.SetFloat("_GrainStrength", 0);
            }
        }
        const float LaneWallOut = .17f, LaneWallDown = .3f;
        void TiltLaneWalls(SandJamSceneController controller)
        {
            float angle = Mathf.Atan(SceneActorView.DepthPerUnit) * Mathf.Rad2Deg;
            foreach (var r in controller.GetComponentsInChildren<Renderer>(true))
            {
                if (System.Array.IndexOf(LaneWalls, r.name) < 0) continue;
                var t = r.transform;
                t.RotateAround(new Vector3(t.position.x, SceneActorView.DepthOriginY, t.position.z), Vector3.right, angle);
                // Keep the on-screen height: the tilt alone would shorten the wall.
                var ls = t.localScale; t.localScale = new Vector3(ls.x, ls.y / Mathf.Cos(angle * Mathf.Deg2Rad), ls.z);
                // Original (recording 2, 888 px wide): the banks start ~54 px lower and stand ~30 px further out from
                // the cubes than the modelled ones.
                float side = r.name.StartsWith("Left") ? -1 : 1;
                t.position += new Vector3(side * LaneWallOut, -LaneWallDown, 0);
            }
        }
        // Original stand: a solid slab with a slot cut into its top; the picture frame is plugged into the slot,
        // like two blocks fitted together. Built as real geometry in the frame's own material, tipped a little toward
        // the camera so the slab top and the slot mouth beside each post are visible.
        const float StandTilt = -6f;
        void BuildFrameFeet(SandJamSceneController controller)
        {
            var renderers = controller.GetComponentsInChildren<Renderer>(true);
            var plinth = renderers.FirstOrDefault(r => r.name == "Extruded plinth");
            var frame = renderers.FirstOrDefault(r => r.name == "Solid board frame");
            if (!plinth || !frame || plinth.transform.parent.Find("Frame stand")) return;
            var fb = frame.bounds; var parent = plinth.transform.parent;
            plinth.enabled = false;
            // The rounded frame (RoundFrame) reaches from just in front of the picture to the old frame's back: the slot
            // spans that depth.
            var board = controller.Board ? controller.Board.Renderer.bounds : fb;
            float frontZ = Mathf.Min(fb.min.z, board.min.z - .02f), centreZ = (frontZ + fb.max.z) * .5f;
            // Measured (recording 2, level 268): the frame stands at the slab's front, behind a thin lip; most of the slab
            // top lies behind it (seen beside the posts), and the tall front face rounds off into its lower edge.
            float width = fb.size.x * 1.11f, height = .32f, slotDepth = .28f, frontLip = .05f, backLip = 1f;
            float slotHalfX = fb.extents.x + .07f, slotHalfZ = (fb.max.z - frontZ) * .5f + .02f;
            var mesh = new Mesh { name = "Frame stand" };
            var vertices = new System.Collections.Generic.List<Vector3>(); var normals = new System.Collections.Generic.List<Vector3>();
            var body = new System.Collections.Generic.List<int>(); var floor = new System.Collections.Generic.List<int>();
            float hx = width * .5f, front = -slotHalfZ - frontLip, back = slotHalfZ + backLip;
            AddBox(vertices, normals, body, new Vector3(-hx, -height, front), new Vector3(hx, 0, -slotHalfZ), .03f, .14f); // front lip, rounded edges
            AddBox(vertices, normals, body, new Vector3(-hx, -height, -slotHalfZ), new Vector3(hx, -slotDepth, back));    // under the slot
            AddBox(vertices, normals, body, new Vector3(-hx, -slotDepth, slotHalfZ), new Vector3(hx, 0, back));         // back lip
            AddBox(vertices, normals, body, new Vector3(-hx, -slotDepth, -slotHalfZ), new Vector3(-slotHalfX, 0, slotHalfZ)); // left end
            AddBox(vertices, normals, body, new Vector3(slotHalfX, -slotDepth, -slotHalfZ), new Vector3(hx, 0, slotHalfZ));   // right end
            // Dark slot floor.
            int f = vertices.Count; float fy = -slotDepth + .002f;
            vertices.AddRange(new[] { new Vector3(-slotHalfX, fy, -slotHalfZ), new Vector3(-slotHalfX, fy, slotHalfZ), new Vector3(slotHalfX, fy, slotHalfZ), new Vector3(slotHalfX, fy, -slotHalfZ) });
            for (int k = 0; k < 4; k++) normals.Add(Vector3.up);
            floor.AddRange(new[] { f, f + 1, f + 2, f, f + 2, f + 3 });
            mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.subMeshCount = 2;
            mesh.SetTriangles(body, 0); mesh.SetTriangles(floor, 1); mesh.RecalculateBounds();
            var stand = new GameObject("Frame stand") { layer = frame.gameObject.layer };
            stand.transform.SetParent(parent, false);
            // The slab top sits high enough to hide the frame's rounded bottom corners inside the slot.
            stand.transform.position = new Vector3(fb.center.x, fb.min.y + .27f, centreZ);
            stand.transform.rotation = Quaternion.Euler(StandTilt, 0, 0);
            var scale = parent.lossyScale; stand.transform.localScale = new Vector3(1 / scale.x, 1 / scale.y, 1 / scale.z);
            stand.AddComponent<MeshFilter>().sharedMesh = mesh;
            var bodyMaterial = new Material(frame.sharedMaterial);
            // Its front faces the light differently from the frame: rendered #5F7F9D, the original's reads #5576A6.
            if (bodyMaterial.HasProperty("_Color")) { var fc = frame.sharedMaterial.color; bodyMaterial.color = new Color(fc.r * .895f, fc.g * .93f, Mathf.Min(1, fc.b * 1.06f), fc.a); }
            var slotMaterial = new Material(frame.sharedMaterial);
            if (slotMaterial.HasProperty("_Color")) slotMaterial.color = Color.Lerp(frame.sharedMaterial.color, Color.black, .55f);
            stand.AddComponent<MeshRenderer>().sharedMaterials = new[] { bodyMaterial, slotMaterial };
            var shadow = renderers.FirstOrDefault(r => r.name == "Plinth contact shadow");
            if (shadow)
            {
                var sb = shadow.bounds; var sl = shadow.transform.localScale;
                shadow.transform.localScale = new Vector3(sl.x * width * 1.04f / Mathf.Max(.0001f, sb.size.x), sl.y, sl.z);
                shadow.transform.position += new Vector3(fb.center.x - shadow.bounds.center.x, 0, 0);
            }
        }
        // Box; `bevel` cuts a 45° chamfer along its top-front edge, `lowBevel` a chamfer that high (no deeper than the box)
        // along its bottom-front edge: the slab's rounded edges.
        static void AddBox(System.Collections.Generic.List<Vector3> v, System.Collections.Generic.List<Vector3> n, System.Collections.Generic.List<int> t, Vector3 a, Vector3 b, float bevel = 0, float lowBevel = 0)
        {
            void Face(Vector3 normal, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
            {
                int i0 = v.Count; v.Add(p0); v.Add(p1); v.Add(p2); v.Add(p3);
                for (int k = 0; k < 4; k++) n.Add(normal);
                t.AddRange(new[] { i0, i0 + 1, i0 + 2, i0, i0 + 2, i0 + 3 });
            }
            float tz = a.z + bevel, fy = b.y - bevel, bz = a.z + Mathf.Min(lowBevel, b.z - a.z), by = a.y + lowBevel;
            Face(Vector3.up, new Vector3(a.x, b.y, tz), new Vector3(a.x, b.y, b.z), new Vector3(b.x, b.y, b.z), new Vector3(b.x, b.y, tz));
            Face(Vector3.down, new Vector3(a.x, a.y, bz), new Vector3(b.x, a.y, bz), new Vector3(b.x, a.y, b.z), new Vector3(a.x, a.y, b.z));
            Face(Vector3.back, new Vector3(a.x, by, a.z), new Vector3(a.x, fy, a.z), new Vector3(b.x, fy, a.z), new Vector3(b.x, by, a.z));
            if (bevel > 0) Face(new Vector3(0, 1, -1).normalized, new Vector3(a.x, fy, a.z), new Vector3(a.x, b.y, tz), new Vector3(b.x, b.y, tz), new Vector3(b.x, fy, a.z));
            if (lowBevel > 0) Face(new Vector3(0, -.3f, -1).normalized, // shaded as the rounded edge, not a dark underside
                new Vector3(a.x, a.y, bz), new Vector3(a.x, by, a.z), new Vector3(b.x, by, a.z), new Vector3(b.x, a.y, bz));
            Face(Vector3.forward, new Vector3(b.x, a.y, b.z), new Vector3(b.x, b.y, b.z), new Vector3(a.x, b.y, b.z), new Vector3(a.x, a.y, b.z));
            Face(Vector3.left, new Vector3(a.x, a.y, b.z), new Vector3(a.x, b.y, b.z), new Vector3(a.x, b.y, a.z), new Vector3(a.x, a.y, a.z));
            Face(Vector3.right, new Vector3(b.x, a.y, a.z), new Vector3(b.x, b.y, a.z), new Vector3(b.x, b.y, b.z), new Vector3(b.x, a.y, b.z));
        }

        // Dark plate right behind the picture, slightly larger than it: the frame opening is a little wider than the
        // picture on some sides, and the light backdrop used to show through as a pale seam along the edge.
        void BuildBoardBacking(SandJamSceneController controller)
        {
            var board = controller.Board ? controller.Board.Renderer : null;
            if (!board) return;
            var frame = controller.GetComponentsInChildren<Renderer>(true).FirstOrDefault(f => f.name == "Solid board frame" || f.name == "Raised board frame");
            Color tone = new Color(.37f, .29f, .5f);
            if (frame is SpriteRenderer) tone = ((SpriteRenderer)frame).color;
            else if (frame && frame.sharedMaterial && frame.sharedMaterial.HasProperty("_Color")) tone = frame.sharedMaterial.color;
            // The frame mesh is modelled slightly off-centre (about 0.025 to the left); centre it on the picture so the
            // opening is even on both sides.
            if (frame)
            {
                float dx = board.bounds.center.x - frame.bounds.center.x;
                foreach (var part in controller.GetComponentsInChildren<Renderer>(true))
                    if (part.name == "Solid board frame" || part.name == "Raised board frame" || part.name == "Frame shadow")
                        part.transform.position += new Vector3(dx, 0, 0);
            }
            var go = new GameObject("Board backing") { layer = board.gameObject.layer };
            go.transform.SetParent(board.transform.parent, true);
            var b = board.bounds;
            // Behind the frame, so it only fills gaps in the opening instead of covering the frame's inner lip.
            float behind = frame ? frame.bounds.max.z + .02f : b.max.z + .04f;
            go.transform.position = new Vector3(b.center.x, b.center.y, behind);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = SpriteFactory.White;
            r.color = Color.Lerp(tone, Color.black, .12f); // close to the frame colour: the picture sits flush in the frame
            r.sortingOrder = board.sortingOrder - 1;
            go.transform.localScale = Vector3.one;
            // It sits behind the frame (perspective shifts it), so it is made generously larger than the opening.
            var world = new Vector3(b.size.x + .6f, b.size.y + .6f, 1);
            var parentScale = go.transform.parent ? go.transform.parent.lossyScale : Vector3.one;
            go.transform.localScale = new Vector3(world.x / parentScale.x, world.y / parentScale.y, 1);
            go.transform.rotation = board.transform.rotation;
        }

        void ApplyTheme(SandJamSceneController controller, StageTheme theme)
        {
            top = theme.DisplayTop; middle = theme.DisplayMiddle; bottom = theme.DisplayMiddle;
            foreach (var renderer in controller.GetComponentsInChildren<Renderer>(true))
            {
                bool frame = System.Array.IndexOf(FrameParts, renderer.name) >= 0, side = System.Array.IndexOf(SideParts, renderer.name) >= 0;
                bool shadow = renderer.name == "Frame shadow";
                if (!frame && !side && !shadow) continue;
                var color = shadow ? Color.Lerp(theme.DisplayFrame, Color.black, .35f) : frame ? theme.DisplayFrame : theme.DisplaySide;
                var sprite = renderer as SpriteRenderer;
                if (sprite) sprite.color = color;
                else if (renderer.material.HasProperty("_Color")) renderer.material.color = color; // per-level instance, freed with the scene
            }
        }

        void BuildGradient(Camera cam)
        {
            var obj = new GameObject("Backdrop gradient");
            obj.transform.SetParent(transform, false);
            obj.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 8f);
            const float w = 20f, h = 18f;
            gradientMesh = new Mesh { name = "Backdrop gradient" };
            gradientMesh.vertices = new[]
            {
                new Vector3(-w, -h, 0), new Vector3(w, -h, 0), new Vector3(-w, 0, 0), new Vector3(w, 0, 0), new Vector3(-w, h, 0), new Vector3(w, h, 0)
            };
            gradientMesh.colors = new[] { bottom, bottom, middle, middle, top, top };
            gradientMesh.uv = new[] { Vector2.zero, Vector2.right, new Vector2(0, .5f), new Vector2(1, .5f), Vector2.up, Vector2.one };
            gradientMesh.triangles = new[] { 0, 2, 1, 1, 2, 3, 2, 4, 3, 3, 4, 5 };
            obj.AddComponent<MeshFilter>().sharedMesh = gradientMesh;
            var renderer = obj.AddComponent<MeshRenderer>();
            gradientMaterial = new Material(Shader.Find("Sprites/Default"));
            renderer.sharedMaterial = gradientMaterial;
            renderer.sortingOrder = -100;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false;
        }

        static Color Hex(int rgb) { return new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f); }

        void OnDestroy()
        {
            if (gradientMesh) Destroy(gradientMesh);
            if (gradientMaterial) Destroy(gradientMaterial);
        }
    }
}
