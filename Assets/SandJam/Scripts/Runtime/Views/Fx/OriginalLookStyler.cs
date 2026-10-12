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
        public float PadTilt = 55f, PadThickness = 1.8f;
        public int ThemeIndex { get; private set; }
        Color top, middle, bottom;
        Camera filler;
        Mesh gradientMesh;
        Material gradientMaterial;

        public void Apply(SandJamSceneController controller)
        {
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
                        // Original pads are chunky tiles: thicker, and tilted a little less so the front edge shows.
                        child.localRotation = Quaternion.Euler(PadTilt, 0, 0);
                        var ls = child.localScale; child.localScale = new Vector3(ls.x, ls.y, PadThickness);
                    }
            EvenFrameBorder(controller);
            TiltLaneWalls(controller);
            BuildFrameFeet(controller);
        }

        // The frame mesh leaves a thinner border above the picture than at its sides. Stretch the frame (and its
        // shadow) upward, bottom fixed, so the top border matches the side borders as in the original.
        static readonly string[] FrameOutline = { "Solid board frame", "Raised board frame", "Frame shadow" };
        void EvenFrameBorder(SandJamSceneController controller)
        {
            var board = controller.Board ? controller.Board.Renderer : null;
            var frame = controller.GetComponentsInChildren<Renderer>(true).FirstOrDefault(f => f.name == "Solid board frame");
            if (!board || !frame) return;
            var b = board.bounds; var f0 = frame.bounds;
            float side = ((b.min.x - f0.min.x) + (f0.max.x - b.max.x)) * .5f;
            float extra = (b.max.y + side) - f0.max.y;
            if (extra <= .005f) return;
            foreach (var r in controller.GetComponentsInChildren<Renderer>(true))
            {
                if (System.Array.IndexOf(FrameOutline, r.name) < 0) continue;
                var t = r.transform; var rb = r.bounds;
                if (rb.size.y <= 0) continue;
                float k = (rb.size.y + extra) / rb.size.y;
                // Scale along the world-vertical axis of the part, keeping its bottom edge in place.
                var ls = t.localScale; t.localScale = new Vector3(ls.x, ls.y * k, ls.z);
                var nb = r.bounds; t.position += new Vector3(0, rb.min.y - nb.min.y, 0);
            }
        }

        // The lane walls follow the same floor perspective as the cubes (SceneActorView.DepthOffset): their lower
        // ends lean toward the camera, so the walls fan out with the lanes.
        static readonly string[] LaneWalls = { "Left lane rail", "Left lane bank", "Right booster rail", "Right booster bank",
            "Left solid rail", "Left rail top", "Right solid rail", "Right rail top" };
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
            }
        }
        // Original frame stand: raised end blocks ("feet") at both ends of the plinth.
        void BuildFrameFeet(SandJamSceneController controller)
        {
            var plinth = controller.GetComponentsInChildren<Renderer>(true).FirstOrDefault(r => r.name == "Extruded plinth");
            if (!plinth || plinth.transform.parent.Find("Frame foot")) return;
            var b = plinth.bounds;
            for (int side = -1; side <= 1; side += 2)
            {
                var foot = Instantiate(plinth.gameObject, plinth.transform.parent, true);
                foot.name = "Frame foot";
                var t = foot.transform; var ls = t.localScale;
                float width = b.size.x * .06f;
                t.localScale = new Vector3(ls.x * width / b.size.x, ls.y * 1.25f, ls.z * 1.04f);
                t.position = new Vector3(b.center.x + side * (b.extents.x - width * .5f), b.center.y + b.size.y * .12f, plinth.transform.position.z - .02f);
                var fr = foot.GetComponent<Renderer>();
                if (fr && fr.sharedMaterial && fr.sharedMaterial.HasProperty("_Color")) fr.material.color = Color.Lerp(fr.sharedMaterial.color, Color.white, .12f);
            }
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
            var white = Texture2D.whiteTexture;
            r.sprite = Sprite.Create(white, new Rect(0, 0, white.width, white.height), new Vector2(.5f, .5f), white.width);
            r.color = Color.Lerp(tone, Color.black, .45f);
            r.sortingOrder = board.sortingOrder - 1;
            go.transform.localScale = Vector3.one;
            var world = new Vector3(b.size.x + .14f, b.size.y + .14f, 1);
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
