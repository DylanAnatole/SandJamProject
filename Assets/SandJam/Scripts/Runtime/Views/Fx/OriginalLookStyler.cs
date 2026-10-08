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
        public float PadTilt = 58f;
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
                        child.localRotation = Quaternion.Euler(PadTilt, 0, 0);
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
