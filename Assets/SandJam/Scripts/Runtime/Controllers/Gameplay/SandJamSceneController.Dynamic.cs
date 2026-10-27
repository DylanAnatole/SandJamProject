using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SandJamTest.Scene3D
{
    // Builds the region and character views for any original level JSON at runtime,
    // using the scene's first region and character as templates (no per-level prefab).
    public sealed partial class SandJamSceneController
    {
        // Set by LevelBootstrap before the level prefab is instantiated; consumed in Awake.
        public static TextAsset PendingLevel;
        readonly Dictionary<int, Material> characterMaterials = new Dictionary<int, Material>();
        readonly List<Object> runtimeAssets = new List<Object>();
        static Mesh coverMesh;

        void BuildLevelViews(LevelData data)
        {
            BuildRegions(data);
            BuildCharacters(data);
            var colors = data.parts.Select(p => p.ColorType).Concat(data.laneData.SelectMany(l => l.ColorAmmoDatas).Select(c => c.ColorType)).Distinct().ToArray();
            ProjectileMaterials = colors.Select(c => Track(new Material(Shader.Find("Unlit/Color")) { color = ColorPalette.Of(c) })).ToArray();
            MaterialColorIds = colors;
        }

        T Track<T>(T asset) where T : Object { runtimeAssets.Add(asset); return asset; }

        void BuildRegions(LevelData data)
        {
            var template = Regions[0];
            var parent = template.transform.parent;
            var views = new SceneRegionView[data.parts.Length];
            for (int i = 0; i < views.Length; i++)
                views[i] = i < Regions.Length ? Regions[i] : Instantiate(template, parent);
            for (int i = views.Length; i < Regions.Length; i++) { Regions[i].gameObject.SetActive(false); Destroy(Regions[i].gameObject); }
            for (int i = 0; i < views.Length; i++)
            {
                var part = data.parts[i];
                views[i].name = part.name; views[i].PartIndex = i;
                // The template prefab hard-codes per-region sand colours for its own level; dynamic levels
                // must always colour by the JSON ColorType.
                views[i].OverrideSandColor = false;
                views[i].LockMode = part.IsUnlockerPart;
                views[i].Unlocks = null;
                if (part.IsUnlockerPart) { int target = System.Array.FindIndex(data.parts, o => o.IsUnlockedPart && o.name == part.unlockerPartName); if (target >= 0 && target < views.Length) views[i].Unlocks = views[target]; }
                var cell = LabelCell(part.rows, part.cols);
                // Board texture sits at (-3.15, 0.4) in the board root with one cell per CellSize.
                float gx = cell.x + .5f, gy = cell.y + .5f;
                // Original: most counters are placed by hand in the level (e.g. on top of a ring, not inside it).
                if (part.isManualPosition && data.gridSlotScale > 0)
                {
                    float mx = part.cnvPos.x / data.gridSlotScale + data.columnCount * .5f, my = part.cnvPos.y / data.gridSlotScale;
                    if (mx >= 0 && mx <= data.columnCount && my >= 0 && my <= data.rowCount) { gx = mx; gy = my; }
                }
                views[i].Counter.transform.localPosition = new Vector3(-3.15f + gx * SandBoardTextureView.CellSize, .4f + gy * SandBoardTextureView.CellSize, -.09f);
            }
            Regions = views;
        }

        // Deepest interior cell (farthest from the region edge), so counters sit inside concave shapes.
        static Vector2Int LabelCell(int[] rows, int[] cols)
        {
            var index = new Dictionary<int, int>();
            for (int i = 0; i < rows.Length; i++) index[rows[i] * 1024 + cols[i]] = i;
            var depth = Enumerable.Repeat(-1, rows.Length).ToArray();
            var queue = new Queue<int>();
            int[] dx = { 1, -1, 0, 0 }, dy = { 0, 0, 1, -1 };
            for (int i = 0; i < rows.Length; i++)
                for (int d = 0; d < 4; d++)
                    if (!index.ContainsKey((rows[i] + dy[d]) * 1024 + cols[i] + dx[d])) { depth[i] = 0; queue.Enqueue(i); break; }
            while (queue.Count > 0)
            {
                int i = queue.Dequeue();
                for (int d = 0; d < 4; d++)
                {
                    int n;
                    if (index.TryGetValue((rows[i] + dy[d]) * 1024 + cols[i] + dx[d], out n) && depth[n] < 0) { depth[n] = depth[i] + 1; queue.Enqueue(n); }
                }
            }
            float cx = (float)cols.Average(), cy = (float)rows.Average();
            int best = 0;
            for (int i = 1; i < rows.Length; i++)
            {
                if (depth[i] > depth[best]) best = i;
                else if (depth[i] == depth[best] && (cols[i] - cx) * (cols[i] - cx) + (rows[i] - cy) * (rows[i] - cy) < (cols[best] - cx) * (cols[best] - cx) + (rows[best] - cy) * (rows[best] - cy)) best = i;
            }
            return new Vector2Int(cols[best], rows[best]);
        }

        void BuildCharacters(LevelData data)
        {
            var template = Characters[0];
            var parent = template.transform.parent;
            var specs = new List<KeyValuePair<Vector2Int, CharacterData>>();
            for (int lane = 0; lane < data.laneData.Length; lane++)
                for (int order = 0; order < data.laneData[lane].ColorAmmoDatas.Length; order++)
                    specs.Add(new KeyValuePair<Vector2Int, CharacterData>(new Vector2Int(lane, order), data.laneData[lane].ColorAmmoDatas[order]));
            var actors = new SceneActorView[specs.Count];
            for (int i = 0; i < actors.Length; i++)
                actors[i] = i < Characters.Length ? Characters[i] : Instantiate(template, parent);
            for (int i = actors.Length; i < Characters.Length; i++) { Characters[i].gameObject.SetActive(false); Destroy(Characters[i].gameObject); }
            for (int i = 0; i < actors.Length; i++)
            {
                var actor = actors[i]; var spec = specs[i];
                actor.SourceLane = spec.Key.x; actor.SourceOrder = spec.Key.y; actor.ColorId = spec.Value.ColorType;
                actor.name = "Character " + spec.Key.x + "-" + spec.Key.y;
                var style = actor.GetComponent<CharacterDepthStyle>();
                if (style && style.Skin)
                {
                    var material = CharacterMaterial(style.Skin.sharedMaterial, spec.Value.ColorType);
                    style.Skin.sharedMaterials = Enumerable.Repeat(material, style.Skin.sharedMaterials.Length).ToArray();
                }
                if (!actor.GetComponent<CubeContactShadow>()) actor.gameObject.AddComponent<CubeContactShadow>().Actor = actor;
                if (spec.Value.IsFreeze && !actor.GetComponent<CharacterFreezeView>()) AddFreezeShell(actor);
                if (spec.Value.IsUnlocker) AddKey(actor);
                var cover = actor.GetComponent<ReferenceQueueCover>();
                if (spec.Value.IsSecret && !cover) AddSecretCover(actor, style ? style.Skin.sharedMaterial : null);
                else if (!spec.Value.IsSecret && cover) { if (cover.Cover) Destroy(cover.Cover); Destroy(cover); }
            }
            Characters = actors;
        }

        Material CharacterMaterial(Material source, int color)
        {
            Material material;
            if (characterMaterials.TryGetValue(color, out material)) return material;
            material = Track(new Material(source) { name = "Character colour " + color });
            if (material.HasProperty("_Color")) material.SetColor("_Color", ColorPalette.Of(color));
            characterMaterials[color] = material;
            return material;
        }

        // Slate "?" block shown over secret characters until they reach the front of their lane.
        // Original look (question.png, walkthrough videos): rounded dark-slate cube, one big "?" on the
        // front face and a scattered "???" cluster on the top face.
        static Sprite secretMark, secretCluster;
        Material secretMaterial;
        void AddSecretCover(SceneActorView actor, Material source)
        {
            var size = CharacterSandVessel.BlockSize + new Vector3(.02f, .02f, .02f);
            if (!coverMesh) coverMesh = CharacterSandVessel.RoundedBlock(size, .08f);
            if (!secretMark) secretMark = SecretSprite("Secret/secret-mark");
            if (!secretCluster) secretCluster = SecretSprite("Secret/secret-cluster");
            if (!secretMaterial)
            {
                secretMaterial = source ? Track(new Material(source) { name = "Secret cover" }) : Track(new Material(Shader.Find("Standard")));
                if (secretMaterial.HasProperty("_MainTex")) secretMaterial.SetTexture("_MainTex", null);
                if (secretMaterial.HasProperty("_Color")) secretMaterial.SetColor("_Color", new Color(.23f, .25f, .32f));
            }
            var root = new GameObject("Secret cover");
            root.transform.SetParent(actor.Visual, false);
            // Same pitch as the sand block; pivot at its bottom (0.235 above the rig origin, minus the margin).
            var pitch = Quaternion.Euler(CharacterSandVessel.BlockPitch, 0, 0);
            root.transform.localPosition = new Vector3(0, .225f, 0);
            root.transform.localRotation = pitch;
            root.AddComponent<MeshFilter>().sharedMesh = coverMesh;
            var renderer = root.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = secretMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Glyph(root.transform, secretMark, new Vector3(0, size.y * .5f, -size.z * .5f - .006f), Quaternion.identity, size.x * .62f);
            Glyph(root.transform, secretCluster, new Vector3(0, size.y + .006f, 0), Quaternion.Euler(90, 0, 0), size.x * .78f);
            var queueCover = actor.gameObject.AddComponent<ReferenceQueueCover>();
            queueCover.Actor = actor; queueCover.Controller = this; queueCover.Cover = root;
            queueCover.MaskedRenderers = new Renderer[0];
        }

        static Sprite SecretSprite(string path)
        {
            var texture = Resources.Load<Texture2D>(path);
            if (!texture) return null;
            texture.wrapMode = TextureWrapMode.Clamp;
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), texture.width);
        }

        static void Glyph(Transform parent, Sprite sprite, Vector3 position, Quaternion rotation, float width)
        {
            if (!sprite) return;
            var glyph = new GameObject(sprite.texture.name);
            glyph.transform.SetParent(parent, false);
            glyph.transform.localPosition = position; glyph.transform.localRotation = rotation;
            glyph.transform.localScale = Vector3.one * width; // the sprite is 1 unit wide
            var renderer = glyph.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        static Mesh iceMesh, iceMelting;
        static Sprite iceSplash;
        const float SplashSize = 1.25f;
        Material iceMaterial, keyMaterial;

        // Ice shell over a frozen cube (original Freeze gif): slightly larger than the block, with a count.
        void AddFreezeShell(SceneActorView actor)
        {
            var size = CharacterSandVessel.BlockSize + new Vector3(.08f, .08f, .08f);
            // The original ice cube (it melts step by step); without it a plain rounded shell.
            if (!iceMelting) iceMelting = Resources.Load<Mesh>("Mechanics/IceCube");
            if (iceMelting && !iceMelting.isReadable) iceMelting = null; // it melts through baked copies (Read/Write)
            if (!iceMesh && !iceMelting) iceMesh = CharacterSandVessel.RoundedBlock(size, .09f);
            if (!iceMaterial)
            {
                iceMaterial = Track(new Material(Shader.Find("SandJamTest/IceShell")) { name = "Ice shell" });
                iceMaterial.SetTexture("_MainTex", Resources.Load<Texture2D>("Mechanics/Ice"));
            }
            var pitch = Quaternion.Euler(CharacterSandVessel.BlockPitch, 0, 0);
            var shell = new GameObject("Frozen shell");
            shell.transform.SetParent(actor.Visual, false);
            shell.transform.localPosition = new Vector3(0, .195f, 0); shell.transform.localRotation = pitch;
            shell.AddComponent<MeshFilter>().sharedMesh = iceMelting ? iceMelting : iceMesh;
            if (iceMelting)
            {
                // mesh.bounds also spans the melted shape; fit the whole cube itself.
                var b = GeometryUtility.CalculateBounds(iceMelting.vertices, Matrix4x4.identity);
                shell.transform.localScale = new Vector3(size.x / b.size.x, size.y / b.size.y, size.z / b.size.z);
                // Its bottom on the pivot, like the rounded shell (built standing on its origin).
                shell.transform.localPosition -= pitch * Vector3.Scale(b.center - new Vector3(0, b.extents.y, 0), shell.transform.localScale);
            }
            var renderer = shell.AddComponent<MeshRenderer>(); renderer.sharedMaterial = iceMaterial;
            renderer.NoShadows();
            var label = Instantiate(actor.AmmoLabel, actor.AmmoLabel.transform.parent);
            // Same spot as the ammo count once CharacterSandVessel lifts it onto the front face.
            label.name = "Freeze remaining"; label.transform.localPosition = actor.AmmoLabel.transform.localPosition + new Vector3(0, .2f, -.05f);
            label.transform.localScale = actor.AmmoLabel.transform.localScale * 1.22f;
            // Original (Freeze gif): white digits edged in deep ice blue.
            // The copy may still carry the legacy outline hand-over, which keeps only light inks: the blue edge is set
            // here instead.
            label.color = Color.white;
            var legacy = label.GetComponent<ReferenceTextOutline>(); if (legacy) DestroyImmediate(legacy);
            foreach (Transform child in label.transform) if (child.name == "Text outline") Destroy(child.gameObject);
            OutlinedText.Add(label, new Color(.1f, .36f, .78f));
            var view = actor.gameObject.AddComponent<CharacterFreezeView>();
            view.Actor = actor; view.Shell = renderer; view.Counter = label; view.Melting = iceMelting;
            view.Splash = AddIceSplash(actor.transform);
            if (actor.SourceLane >= 0 && actor.SourceLane < LaneStarts.Length) view.Front = LaneStarts[actor.SourceLane];
        }

        // Frost splashed on the ground under a frozen cube (original Freeze gif; Splash texture, tint of "Splash 1").
        static SpriteRenderer AddIceSplash(Transform actor)
        {
            if (!iceSplash)
            {
                var texture = Resources.Load<Texture2D>("Mechanics/Splash");
                if (!texture) return null;
                iceSplash = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), texture.width / SplashSize);
            }
            var splash = new GameObject("Ice splash").AddComponent<SpriteRenderer>();
            splash.transform.SetParent(actor, false);
            splash.transform.localPosition = new Vector3(0, .01f, 0);
            splash.transform.localRotation = Quaternion.Euler(90, 0, Random.Range(0f, 360f));
            splash.sprite = iceSplash; splash.sharedMaterial = SpriteFactory.PlainSprite; splash.color = new Color(.89f, .97f, 1f, .85f);
            return splash;
        }

        // Golden key lying on top of a key cube (original UnlockerGif).
        void AddKey(SceneActorView actor)
        {
            var mesh = Resources.Load<Mesh>("Mechanics/Key");
            if (!mesh) return;
            if (!keyMaterial)
            {
                var source = Characters[0].GetComponent<CharacterDepthStyle>().Skin.sharedMaterial;
                keyMaterial = Track(new Material(source) { name = "Key gold" });
                if (keyMaterial.HasProperty("_Color")) keyMaterial.SetColor("_Color", new Color(1f, .78f, .12f));
                if (keyMaterial.HasProperty("_GrainStrength")) keyMaterial.SetFloat("_GrainStrength", 0);
                if (keyMaterial.HasProperty("_OutlineWidth")) keyMaterial.SetFloat("_OutlineWidth", .008f);
            }
            var pitch = Quaternion.Euler(CharacterSandVessel.BlockPitch, 0, 0);
            var key = new GameObject("Key");
            key.transform.SetParent(actor.Visual, false);
            key.transform.localPosition = new Vector3(0, .235f, 0) + pitch * new Vector3(0, CharacterSandVessel.BlockSize.y + .05f, 0);
            key.transform.localRotation = pitch * Quaternion.Euler(0, 40, 0);
            key.transform.localScale = Vector3.one * (.62f / Mathf.Max(.01f, mesh.bounds.size.z));
            key.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = key.AddComponent<MeshRenderer>(); renderer.sharedMaterial = keyMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        void ReleaseRuntimeAssets() { foreach (var asset in runtimeAssets) if (asset) Destroy(asset); runtimeAssets.Clear(); }
    }
}
