using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SandJamTest.Editor
{
    // New level from a pixel-art picture (84 x 112 PNG, one pixel per board cell). Each 4-connected patch of one colour
    // becomes a region of the nearest game colour; black, outline-lavender or transparent pixels are walls. The cubes
    // are cut from the regions in the order they open (bottom regions first, then their neighbours), 400 sand each,
    // and dealt across the three lanes. LevelAuthoring checks the result (sand totals, solver) and adds it to the
    // campaign. Pictures live in Assets/SandJam/Levels/Pictures; see Documentation/THIET_KE_LEVEL_VI.md.
    public static class LevelFromPicture
    {
        const int Columns = 84, Rows = 112, Lanes = 3, Divider = 20, CubeSand = 400;
        // Patches smaller than this read as stray pixels and become walls.
        const int MinRegionCells = Divider;
        const float WallLuma = .08f, WallTintDistance = .12f, GridSlotScale = .082f;

        [MenuItem("Sand Jam/Levels/New level from selected picture")]
        static void FromSelection()
        {
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (!path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) { Debug.LogError("[Levels] Select an 84 x 112 PNG in the Project window first."); return; }
            LevelAuthoring.Publish(Build(path));
        }
        [MenuItem("Sand Jam/Levels/New level from selected picture", true)]
        static bool CanBuild() { return Selection.activeObject is Texture2D; }

        public static LevelData Build(string pngPath)
        {
            var texture = new Texture2D(2, 2);
            texture.LoadImage(File.ReadAllBytes(pngPath)); // raw file: no import settings needed
            if (texture.width != Columns || texture.height != Rows)
                throw new ArgumentException("Picture must be " + Columns + " x " + Rows + " px, got " + texture.width + " x " + texture.height);
            // Texture rows start at the bottom, like board rows.
            var colour = new int[Rows, Columns];
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Columns; c++) colour[r, c] = ColourOf(texture.GetPixel(c, r));
            UnityEngine.Object.DestroyImmediate(texture);

            var regionOf = new int[Rows, Columns];
            var cells = new List<List<Vector2Int>>();
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Columns; c++)
                {
                    if (colour[r, c] == 0 || regionOf[r, c] != 0) continue;
                    var patch = Flood(colour, regionOf, r, c, cells.Count + 1);
                    cells.Add(patch);
                }
            // Too small patches become walls.
            var walls = new List<Vector2Int>();
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Columns; c++)
                    if (colour[r, c] == 0 || cells[regionOf[r, c] - 1].Count < MinRegionCells) { walls.Add(new Vector2Int(c, r)); regionOf[r, c] = 0; }
            var kept = Enumerable.Range(0, cells.Count).Where(i => cells[i].Count >= MinRegionCells).ToList();
            if (kept.Count == 0) throw new ArgumentException("No colour region of at least " + MinRegionCells + " cells.");

            var neighbours = kept.ToDictionary(i => i, i => new HashSet<int>());
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Columns; c++)
                {
                    int a = regionOf[r, c] - 1; if (a < 0) continue;
                    // Regions separated by a wall line (up to 2 cells) still touch.
                    for (int dr = -2; dr <= 2; dr++)
                        for (int dc = -2; dc <= 2; dc++)
                        {
                            int rr = r + dr, cc = c + dc;
                            if (rr < 0 || rr >= Rows || cc < 0 || cc >= Columns) continue;
                            int b = regionOf[rr, cc] - 1;
                            if (b >= 0 && b != a) neighbours[a].Add(b);
                        }
                }
            // Regions on the bottom row are open from the start (the lowest one if none reaches it); the rest open
            // when a neighbour fills, so the picture fills from the bottom up.
            var open = new HashSet<int>(kept.Where(i => cells[i].Any(p => p.y == 0)));
            if (open.Count == 0) open.Add(kept.OrderBy(i => cells[i].Min(p => p.y)).First());
            var order = new List<int>(open); var queued = new HashSet<int>(open);
            for (int k = 0; k < order.Count; k++)
                foreach (int n in neighbours[order[k]].OrderBy(n => cells[n].Min(p => p.y)))
                    if (queued.Add(n)) order.Add(n);
            order.AddRange(kept.Where(i => !queued.Contains(i)));

            string Name(int i) { return "Region_" + (kept.IndexOf(i) + 1); }
            var parts = kept.Select(i => new PartData
            {
                name = Name(i), ColorType = colour[cells[i][0].y, cells[i][0].x],
                amount = cells[i].Count / Divider * Divider, isOpenedAtStart = open.Contains(i),
                prerequests = open.Contains(i) ? new string[0] : neighbours[i].Select(Name).ToArray(),
                rows = cells[i].Select(p => p.y).ToArray(), cols = cells[i].Select(p => p.x).ToArray(),
            }).ToArray();

            // Cubes in opening order, dealt across the lanes.
            var lanes = Enumerable.Range(0, Lanes).Select(_ => new List<CharacterData>()).ToArray();
            int dealt = 0;
            foreach (int i in order)
            {
                var part = parts[kept.IndexOf(i)];
                for (int left = part.amount; left > 0; left -= CubeSand)
                    lanes[dealt++ % Lanes].Add(new CharacterData { ColorType = part.ColorType, AmmoCount = Mathf.Min(CubeSand, left) });
            }
            return new LevelData
            {
                sceneName = Path.GetFileNameWithoutExtension(pngPath), rowCount = Rows, columnCount = Columns,
                uiDivider = Divider, characterAmmo = CubeSand, gridSlotScale = GridSlotScale,
                gridSlotNeedAmmoCount = new int[5], parts = parts,
                opr = walls.Select(p => p.y).ToArray(), opc = walls.Select(p => p.x).ToArray(),
                laneData = lanes.Select(l => new LaneData { ColorAmmoDatas = l.ToArray() }).ToArray(),
            };
        }

        static List<Vector2Int> Flood(int[,] colour, int[,] regionOf, int r0, int c0, int id)
        {
            var patch = new List<Vector2Int>(); var stack = new Stack<Vector2Int>();
            int want = colour[r0, c0]; regionOf[r0, c0] = id; stack.Push(new Vector2Int(c0, r0));
            while (stack.Count > 0)
            {
                var p = stack.Pop(); patch.Add(p);
                foreach (var d in new[] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down })
                {
                    var q = p + d;
                    if (q.x < 0 || q.x >= Columns || q.y < 0 || q.y >= Rows || regionOf[q.y, q.x] != 0 || colour[q.y, q.x] != want) continue;
                    regionOf[q.y, q.x] = id; stack.Push(q);
                }
            }
            return patch;
        }

        // 0 = wall; otherwise the nearest game colour (ColorPalette, types 1-16).
        static int ColourOf(Color pixel)
        {
            if (pixel.a < .5f || pixel.grayscale < WallLuma || Distance(pixel, Scene3D.SandBoardTextureView.GameOutline) < WallTintDistance) return 0;
            int best = 1; float bestDistance = float.MaxValue;
            for (int type = 1; type <= 16; type++)
            {
                float d = Distance(pixel, ColorPalette.Of(type));
                if (d < bestDistance) { bestDistance = d; best = type; }
            }
            return best;
        }
        static float Distance(Color a, Color b) { return Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b); }
    }
}
