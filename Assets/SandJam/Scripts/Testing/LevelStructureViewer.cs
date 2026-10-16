using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SandJamTest.Scene3D
{
    // Level structure scene (Scenes/LevelStructure): shows how one level is put together.
    // Edit mode: a labelled diagram, rebuilt whenever the inspector changes and listed in the Hierarchy -
    //   the board (one pixel per grid cell, coloured by region), every region (name, colour, sand amount, flags,
    //   prerequisites), the lanes of cubes in queue order (colour, ammo, chain / freeze / secret / key / half)
    //   and the five stash slots, plus a summary (size, divider, sand vs ammo per colour).
    // Play mode: the diagram hides and the real level is built from the gameplay template, straight into play,
    //   so the runtime objects (regions, characters, board texture, effects) can be inspected in the Hierarchy.
    // The diagram objects are never saved (HideFlags.DontSave); only the settings on this component are.
    [ExecuteAlways]
    public sealed class LevelStructureViewer : MonoBehaviour
    {
        [Tooltip("Campaign level number (1-based), used when no JSON is assigned.")]
        [Min(1)] public int LevelNumber = 1;
        [Tooltip("Optional: any level JSON (Resources/Levels/Data/...). Overrides the level number.")]
        public TextAsset LevelJson;
        [Tooltip("Gameplay template catalog (Resources/LevelPack/Catalog), used in Play mode.")]
        public LevelCatalog Catalog;
        public Camera PreviewCamera;

        const float Cell = .06f, CubeSize = .5f, LaneGap = .7f;
        const string PreviewName = "Level structure (preview, not saved)";
        Transform preview;
        Font font;
        bool dirty = true;
        readonly List<Object> generated = new List<Object>();

        public TextAsset Source
        {
            get
            {
                if (LevelJson) return LevelJson;
                var levels = Campaign.Levels;
                return Resources.Load<TextAsset>("Levels/Data/" + levels[(LevelNumber - 1) % levels.Length]);
            }
        }

        void OnEnable() { dirty = true; }
        void OnValidate() { dirty = true; }
        void OnDisable() { Clear(); }

        void Update()
        {
            if (Application.isPlaying) return;
            if (dirty) { dirty = false; Rebuild(); }
        }

        // ---------- Play mode: the real level ----------
        void Start()
        {
            if (!Application.isPlaying) return;
            Clear();
            if (PreviewCamera) PreviewCamera.gameObject.SetActive(false);
            var asset = Source;
            if (!asset || !Catalog) { Debug.LogError("LevelStructureViewer: missing level JSON or catalog"); return; }
            var prefab = Resources.Load<GameObject>(Catalog.Get(0).PrefabResource);
            GameServices.Ensure();
            SandJamSceneController.PendingLevel = asset;
            var level = Instantiate(prefab, transform);
            level.name = "Level " + asset.name + " (runtime)";
            StartCoroutine(EnterGameplay(level));
        }

        System.Collections.IEnumerator EnterGameplay(GameObject level)
        {
            yield return null; yield return null;
            var screen = level.GetComponentInChildren<VideoScreen>(true);
            if (screen) { var popups = screen.Popups; while (popups && popups.Open) popups.Close(); screen.Play(); while (popups && popups.Open) popups.Close(); }
        }

        // ---------- Edit mode: the diagram ----------
        void Clear()
        {
            if (preview) DestroyImmediate(preview.gameObject);
            var stale = transform.Find(PreviewName);
            if (stale) DestroyImmediate(stale.gameObject);
            foreach (var o in generated) if (o) DestroyImmediate(o);
            generated.Clear();
            preview = null;
        }

        public void Rebuild()
        {
            Clear();
            var asset = Source;
            if (!asset) return;
            LevelData data;
            try { data = LevelDataManager.Load(asset); }
            catch (System.Exception e) { Debug.LogWarning("LevelStructureViewer: " + e.Message); return; }
            if (!font) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            preview = new GameObject(PreviewName).transform;
            preview.gameObject.hideFlags = HideFlags.DontSave;
            preview.SetParent(transform, false);

            float boardW = data.columnCount * Cell, boardH = data.rowCount * Cell;
            var board = Node(preview, "Board " + data.columnCount + "x" + data.rowCount + " (1 pixel = 1 grid cell)", new Vector3(-boardW / 2, 0, 0));
            BuildBoard(board, data);
            var regions = Node(preview, "Regions (" + data.parts.Length + ")", Vector3.zero);
            BuildRegions(regions, board, data);

            float top = -.6f;
            var stash = Node(preview, "Stash - 5 slots", new Vector3(0, top, 0));
            for (int i = 0; i < 5; i++)
            {
                var slot = Quad(stash, "Slot " + (i + 1), new Vector3((i - 2) * (CubeSize + .15f), 0, 0), new Vector2(CubeSize, CubeSize * .55f), new Color(.75f, .8f, .9f));
                Text(slot, (i + 1).ToString(), Vector3.zero, .1f, new Color(.3f, .35f, .45f));
            }
            var lanes = Node(preview, "Lanes (" + data.laneData.Length + ", front cube first)", new Vector3(0, top - .8f, 0));
            float laneStep = CubeSize + LaneGap;
            for (int l = 0; l < data.laneData.Length; l++)
            {
                float x = (l - (data.laneData.Length - 1) / 2f) * laneStep;
                var cubes = data.laneData[l].ColorAmmoDatas;
                var lane = Node(lanes, "Lane " + (l + 1) + " (" + cubes.Length + " cubes)", new Vector3(x, 0, 0));
                Text(lane, "Lane " + (l + 1), new Vector3(0, .42f, 0), .11f, Color.white);
                for (int o = 0; o < cubes.Length; o++)
                {
                    var c = cubes[o];
                    var cube = Quad(lane, CubeName(o, c), new Vector3(0, -o * (CubeSize + .12f), 0), new Vector2(CubeSize, CubeSize),
                        c.IsSecret ? new Color(.23f, .25f, .32f) : ColorPalette.Of(c.ColorType));
                    Text(cube, c.AmmoCount.ToString(), new Vector3(0, .04f, -.01f), .16f, Color.white);
                    var flags = Flags(c);
                    if (flags.Length > 0) Text(cube, flags, new Vector3(0, -.16f, -.01f), .09f, new Color(1, .95f, .55f));
                }
            }

            int cubeCount = data.laneData.Sum(l => l.ColorAmmoDatas.Length);
            float laneBottom = top - .8f - data.laneData.Max(l => l.ColorAmmoDatas.Length) * (CubeSize + .12f);
            var summary = Summary(asset.name, data, cubeCount);
            float summaryTop = boardH + .2f + summary.Split('\n').Length * .15f;
            var info = Node(preview, "Summary", new Vector3(-boardW / 2, summaryTop, 0));
            Text(info, summary, Vector3.zero, .12f, Color.white, TextAnchor.UpperLeft);

            if (PreviewCamera)
            {
                PreviewCamera.orthographic = true;
                float minX = Mathf.Min(-boardW / 2, -(data.laneData.Length / 2f) * laneStep) - .4f, maxX = -minX;
                float maxY = summaryTop + .2f, minY = laneBottom - .2f;
                PreviewCamera.transform.position = transform.position + new Vector3(0, (maxY + minY) / 2, -10);
                float aspect = PreviewCamera.aspect > 0 ? PreviewCamera.aspect : .5f;
                PreviewCamera.orthographicSize = Mathf.Max((maxY - minY) / 2, (maxX - minX) / 2 / aspect) + .1f;
            }
        }

        void BuildBoard(Transform board, LevelData data)
        {
            var tex = new Texture2D(data.columnCount, data.rowCount, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            var px = Enumerable.Repeat(new Color32(40, 52, 66, 255), data.columnCount * data.rowCount).ToArray();
            foreach (var p in data.parts)
            {
                Color c = ColorPalette.Of(p.ColorType);
                if (!p.isOpenedAtStart) c = Color.Lerp(c, Color.black, .35f);
                if (p.IsUnlockerPart) c = new Color(.95f, .78f, .2f);
                for (int i = 0; i < p.rows.Length; i++)
                {
                    int x = p.cols[i], y = p.rows[i];
                    if (x < 0 || y < 0 || x >= data.columnCount || y >= data.rowCount) continue;
                    // Faint checker shows the cells.
                    px[y * data.columnCount + x] = Color.Lerp(c, Color.white, ((x + y) & 1) == 0 ? 0 : .08f);
                }
            }
            tex.SetPixels32(px); tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero, 1f / Cell);
            sprite.hideFlags = HideFlags.DontSave;
            generated.Add(tex); generated.Add(sprite);
            var r = board.gameObject.AddComponent<SpriteRenderer>(); r.sprite = sprite;
        }

        void BuildRegions(Transform parent, Transform board, LevelData data)
        {
            for (int i = 0; i < data.parts.Length; i++)
            {
                var p = data.parts[i];
                var cell = Centre(p);
                var pos = board.localPosition + new Vector3((cell.x + .5f) * Cell, (cell.y + .5f) * Cell, -.02f);
                var node = Node(parent, RegionName(i, p), pos);
                Text(node, p.amount.ToString(), Vector3.zero, .12f, Color.white);
            }
        }

        static readonly string[] ColourNames = { "none", "red", "green", "blue", "yellow", "orange", "pink", "purple", "black", "white", "brown" };
        static string Colour(int c) { return c >= 0 && c < ColourNames.Length ? c + " " + ColourNames[c] : c.ToString(); }

        static Vector2 Centre(PartData p)
        {
            if (p.rows.Length == 0) return Vector2.zero;
            float cx = (float)p.cols.Average(), cy = (float)p.rows.Average();
            int best = 0; float bestD = float.MaxValue;
            for (int i = 0; i < p.rows.Length; i++) { float d = (p.cols[i] - cx) * (p.cols[i] - cx) + (p.rows[i] - cy) * (p.rows[i] - cy); if (d < bestD) { bestD = d; best = i; } }
            return new Vector2(p.cols[best], p.rows[best]);
        }

        static string RegionName(int i, PartData p)
        {
            var parts = new List<string> { "Region " + i + " \"" + p.name + "\"", "colour " + Colour(p.ColorType), p.amount + " sand", p.rows.Length + " cells" };
            parts.Add(p.isOpenedAtStart ? "open" : "closed");
            if (p.IsUnlockerPart) parts.Add("padlock -> " + p.unlockerPartName);
            if (p.IsUnlockedPart) parts.Add("locked");
            if (p.prerequests != null && p.prerequests.Length > 0) parts.Add("after " + string.Join(",", p.prerequests));
            return string.Join(" | ", parts);
        }

        static string CubeName(int order, CharacterData c)
        {
            var name = "Cube " + (order + 1) + " | colour " + Colour(c.ColorType) + " | " + c.AmmoCount + " ammo";
            var flags = Flags(c);
            return flags.Length > 0 ? name + " | " + flags : name;
        }

        static string Flags(CharacterData c)
        {
            var f = new List<string>();
            if (c.IsChain) f.Add(c.IsChainSameLane ? "chain(lane)" : "chain");
            if (c.IsFreeze) f.Add("ice " + c.FreezeCount);
            if (c.IsSecret) f.Add("secret");
            if (c.IsUnlocker) f.Add("key");
            if (c.IsHalf) f.Add("half");
            return string.Join(" ", f);
        }

        static string Summary(string id, LevelData data, int cubes)
        {
            var lines = new List<string>
            {
                id,
                "Grid " + data.columnCount + "x" + data.rowCount + " | divider " + data.uiDivider + " | regions " + data.parts.Length + " | lanes " + data.laneData.Length + " | cubes " + cubes,
                "Board: bright = open at start, dark = opens later, gold = padlock. Numbers = sand per region.",
                "Cubes: number = ammo, yellow tags = mechanics (chain, ice N, secret, key, half).",
                "Sand / ammo per colour:"
            };
            var colours = data.parts.Select(p => p.ColorType).Concat(data.laneData.SelectMany(l => l.ColorAmmoDatas).Select(c => c.ColorType)).Distinct().OrderBy(c => c);
            foreach (var c in colours)
            {
                int sand = data.parts.Where(p => p.ColorType == c && !p.IsUnlockerPart).Sum(p => p.amount);
                int ammo = data.laneData.SelectMany(l => l.ColorAmmoDatas).Where(a => a.ColorType == c).Sum(a => a.AmmoCount);
                lines.Add("  colour " + Colour(c) + ": sand " + sand + " / ammo " + ammo + (sand == ammo ? "" : "  (differs)"));
            }
            return string.Join("\n", lines);
        }

        // ---------- helpers ----------
        static Transform Node(Transform parent, string name, Vector3 local)
        {
            var go = new GameObject(name) { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(parent, false); go.transform.localPosition = local;
            return go.transform;
        }

        Transform Quad(Transform parent, string name, Vector3 local, Vector2 size, Color color)
        {
            var node = Node(parent, name, local);
            var r = node.gameObject.AddComponent<SpriteRenderer>();
            r.sprite = WhiteSprite(); r.color = color; r.drawMode = SpriteDrawMode.Simple;
            node.localScale = new Vector3(size.x, size.y, 1);
            return node;
        }

        static Sprite WhiteSprite() { return SpriteFactory.White; } // shared: never added to `generated`

        void Text(Transform parent, string text, Vector3 local, float height, Color color, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            var node = Node(parent, "Label", Vector3.zero);
            // Labels keep their size whatever the parent's scale.
            var s = parent.lossyScale;
            node.localScale = new Vector3(1 / Mathf.Max(.0001f, s.x), 1 / Mathf.Max(.0001f, s.y), 1);
            node.localPosition = new Vector3(local.x / Mathf.Max(.0001f, s.x), local.y / Mathf.Max(.0001f, s.y), local.z - .01f);
            var t = node.gameObject.AddComponent<TextMesh>();
            t.font = font; t.text = text; t.anchor = anchor; t.alignment = anchor == TextAnchor.UpperLeft ? TextAlignment.Left : TextAlignment.Center;
            t.fontSize = 48; t.characterSize = height / 4.8f; t.color = color;
            node.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        }
    }
}
