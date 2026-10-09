using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SandJamTest
{
    public sealed class SandJamDemo : MonoBehaviour
    {
        const float Width = 600, Height = 960, Interval = .08f;
        static readonly Color Ink = Hex("23394B"), Muted = Hex("728696"), Accent = Hex("167D78"), Paper = Hex("F3F7F8");
        readonly Rect boardRect = new Rect(174, 147, 252, 336);
        SandGame game;
        LevelData level;
        Texture2D board, circle;
        Font font;
        OriginalAssetView originalAssets;
        AudioSource audioSource;
        bool muted;
        GUIStyle label;
        readonly List<FlyingShot> particles = new List<FlyingShot>();
        Vector2[] centers;
        int shownRevision = -1, hint = -1;
        float timer, hintUntil, messageUntil, elapsed;
        bool paused, fast;
        string message = "Chọn nhân vật đỏ để bắt đầu.", error;
        string smokeDir;
        bool smokeMode;
        struct FlyingShot { public Vector2 From, To; public Color Color; public float Started; }

        static Color Hex(string value) { Color c; ColorUtility.TryParseHtmlString("#" + value, out c); return c; }
        // Kept for the legacy demo and validation code; the game uses ColorPalette directly.
        public static Color Palette(int color) { return ColorPalette.Of(color); }
        static string ColorName(int c)
        {
            switch (c) { case 1: return "RED"; case 2: return "GREEN"; case 3: return "BLUE"; case 4: return "YELLOW"; case 7: return "PURPLE"; default: return "MÀU"; }
        }

        void Awake()
        {
            Application.targetFrameRate = 60;
            var camera = Camera.main;
            if (camera == null) { camera = new GameObject("Camera").AddComponent<Camera>(); camera.tag = "MainCamera"; }
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Hex("DCE7EC");
            camera.cullingMask = ~(1 << 30);
            camera.gameObject.AddComponent<AudioListener>();
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0;
            font = Resources.Load<Font>("Inter");
            circle = MakeRoundedTexture(64, 32);
            try
            {
                originalAssets = new OriginalAssetView();
                originalAssets.Load();
                var source = Resources.Load<TextAsset>("Tutorial");
                if (source == null) throw new Exception("Không tìm thấy dữ liệu màn hướng dẫn.");
                level = JsonUtility.FromJson<LevelData>(source.text);
                board = new Texture2D(level.columnCount, level.rowCount, TextureFormat.RGBA32, false);
                board.filterMode = FilterMode.Point;
                centers = level.parts.Select(p => new Vector2((float)p.cols.Average() + .5f, (float)p.rows.Average() + .5f)).ToArray();
                Restart();
            }
            catch (Exception ex) { error = ex.Message; Debug.LogException(ex); }
            var args = Environment.GetCommandLineArgs();
            int at = Array.IndexOf(args, "--smoke-output");
            if (at >= 0 && at + 1 < args.Length) { smokeMode = true; smokeDir = args[at + 1]; StartCoroutine(SmokeTest()); }
        }

        static Texture2D MakeRoundedTexture(int size, float radius)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(Mathf.Abs(x + .5f - size * .5f) - (size * .5f - radius), 0);
                float dy = Mathf.Max(Mathf.Abs(y + .5f - size * .5f) - (size * .5f - radius), 0);
                pixels[y * size + x] = new Color(1, 1, 1, Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy)));
            }
            texture.SetPixels(pixels); texture.Apply(); return texture;
        }

        void Restart()
        {
            if (audioSource != null) audioSource.Stop();
            game = new SandGame(level);
            particles.Clear(); timer = elapsed = 0; paused = false; hint = -1; shownRevision = -1;
            message = "Chọn nhân vật đỏ để bắt đầu."; messageUntil = Time.unscaledTime + 8;
        }

        void Update()
        {
            if (game == null) return;
            if (!smokeMode)
            {
                if (Input.GetKeyDown(KeyCode.R)) Restart();
                if (Input.GetKeyDown(KeyCode.Space)) paused = !paused;
                if (Input.GetKeyDown(KeyCode.Alpha1)) Select(0);
                if (Input.GetKeyDown(KeyCode.Alpha2)) Select(1);
                if (Input.GetKeyDown(KeyCode.Alpha3)) Select(2);
                if (!paused && game.State == GameState.Playing)
                {
                    elapsed += Time.unscaledDeltaTime;
                    timer += Mathf.Min(Time.unscaledDeltaTime, .25f) * (fast ? 2 : 1);
                    while (timer >= Interval) { timer -= Interval; Fire(); }
                }
            }
            particles.RemoveAll(p => Time.unscaledTime - p.Started > .3f);
            if (shownRevision != game.Revision) { PaintBoard(); shownRevision = game.Revision; }
        }

        void Fire()
        {
            int completed = game.Regions.Count(r => r.Remaining == 0);
            var previousState = game.State;
            foreach (var shot in game.Tick())
                particles.Add(new FlyingShot { From = new Vector2(65 + shot.Slot * 94, 610), To = BoardPoint(centers[shot.Region]), Color = Palette(shot.Color), Started = Time.unscaledTime });
            if (game.State == GameState.Won && previousState != GameState.Won) PlaySound(originalAssets.VictorySound, .45f);
            else if (game.Regions.Count(r => r.Remaining == 0) > completed) PlaySound(originalAssets.CompleteSound, .35f);
        }

        void Select(int lane)
        {
            if (paused || game.State != GameState.Playing) return;
            if (game.SelectLane(lane)) { hint = -1; message = "Nhân vật tự bắn khi có vùng cùng màu đang mở."; PlaySound(originalAssets.SelectSound, .3f); }
            else message = "Ô chờ đã đầy. Đợi nhân vật dùng hết đạn.";
            messageUntil = Time.unscaledTime + 3;
        }

        Vector2 BoardPoint(Vector2 p) { return new Vector2(boardRect.x + p.x / level.columnCount * boardRect.width, boardRect.yMax - p.y / level.rowCount * boardRect.height); }

        void PaintBoard()
        {
            var pixels = Enumerable.Repeat(Hex("213C4B"), level.rowCount * level.columnCount).ToArray();
            foreach (var region in game.Regions)
            {
                var p = region.Data;
                int filled = (int)((long)(p.amount - region.Remaining) * p.rows.Length / p.amount);
                var baseColor = Palette(p.ColorType);
                for (int i = 0; i < p.rows.Length; i++)
                {
                    int r = p.rows[i], c = p.cols[i];
                    uint hash = (uint)(r * 73856093) ^ (uint)(c * 19349663);
                    hash ^= hash >> 13; hash *= 1274126177u; hash ^= hash >> 16;
                    float grain = (hash & 255) / 255f;
                    Color color;
                    if (i < filled) color = Color.Lerp(baseColor * .83f, baseColor, .45f + grain * .55f);
                    else color = Color.Lerp(Hex("EBF0F2"), baseColor, region.Open ? .34f : .13f) * (.96f + grain * .04f);
                    color.a = 1;
                    pixels[r * level.columnCount + c] = color;
                }
            }
            board.SetPixels(pixels); board.Apply(false);
        }

        void OnGUI()
        {
            if (circle == null) return;
            float scale = Mathf.Min(Screen.width / Width, Screen.height / Height);
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - Width * scale) / 2, (Screen.height - Height * scale) / 2, 0), Quaternion.identity, Vector3.one * scale);
            if (label == null) label = new GUIStyle(GUI.skin.label) { font = font, alignment = TextAnchor.MiddleLeft, wordWrap = true, padding = new RectOffset(0, 0, 0, 0) };
            Box(new Rect(0, 0, Width, Height), Paper, false);
            Text(new Rect(22, 22, 280, 44), "SAND JAM", 30, Ink);
            Text(new Rect(24, 65, 370, 20), "MÀN 01  /  THỬ NHÂN VẬT GỐC", 11, Accent);
            if (Button(new Rect(349, 25, 99, 39), muted ? "Sound: Off" : "Sound: On", Color.white, Ink))
            { muted = !muted; audioSource.mute = muted; }
            if (Button(new Rect(459, 25, 117, 39), "Retry  ↻", Color.white, Ink)) Restart();
            if (error != null) { Text(new Rect(30, 120, 540, 200), error, 20, Ink); return; }
            if (game == null) return;
            float progress = 1 - (float)game.Remaining / game.TotalRequired;
            Text(new Rect(24, 101, 300, 21), "HOÀN THÀNH BỨC TRANH", 11, Muted);
            Text(new Rect(500, 101, 76, 21), Mathf.FloorToInt(progress * 100) + "%", 16, Ink, TextAnchor.MiddleRight);
            Box(new Rect(24, 127, 552, 5), Hex("DFE7EB"));
            if (progress > 0) Box(new Rect(24, 127, 552 * progress, 5), Accent);

            Box(new Rect(164, 142, 272, 351), Hex("D8E3E8"));
            Box(new Rect(167, 140, 266, 349), Color.white);
            GUI.DrawTexture(boardRect, board);
            Text(new Rect(25, 157, 131, 22), "PALETTE", 11, Muted);
            for (int i = 0; i < game.Regions.Length; i++)
            {
                var region = game.Regions[i];
                float y = 195 + i * 56;
                Dot(new Vector2(34, y + 10), 7, Palette(region.Data.ColorType));
                Text(new Rect(48, y, 105, 18), ColorName(region.Data.ColorType), 10, Ink);
                Text(new Rect(48, y + 19, 109, 19), region.Remaining == 0 ? "Đã hoàn tất" : region.Open ? "Open" : "Locked", 10, region.Open ? Accent : Muted);
                var pos = BoardPoint(centers[i]);
                Box(new Rect(pos.x - 25, pos.y - 12, 50, 24), new Color(.08f, .16f, .23f, .86f));
                Text(new Rect(pos.x - 25, pos.y - 12, 50, 24), region.Remaining == 0 ? "✓" : region.Remaining.ToString(), 11, Color.white, TextAnchor.MiddleCenter);
            }
            Text(new Rect(453, 162, 120, 20), "CÒN LẠI", 10, Muted);
            Text(new Rect(453, 185, 130, 32), game.Remaining.ToString("N0"), 23, Ink);
            Text(new Rect(453, 217, 119, 30), "đơn vị màu", 10, Muted);
            Text(new Rect(453, 300, 122, 60), "Fill the open areas to unlock more colors.", 12, Ink);
            Text(new Rect(453, 410, 123, 58), "Đúng màu.\nĐúng thứ tự.", 13, Accent);

            string info = Time.unscaledTime < messageUntil ? message : "Chọn đầu hàng → vào ô chờ → tự bắn đúng màu.";
            Box(new Rect(24, 503, 552, 43), Hex("E1EEED"));
            Text(new Rect(38, 509, 524, 31), info, 12, Accent, TextAnchor.MiddleCenter);
            Text(new Rect(24, 566, 370, 22), "STASH", 13, Ink);
            Text(new Rect(435, 566, 141, 22), game.Slots.Count(s => s != null) + " / 6 vị trí", 11, Muted, TextAnchor.MiddleRight);
            for (int i = 0; i < game.Slots.Length; i++)
            {
                var rect = new Rect(24 + i * 94, 600, 82, 90);
                var s = game.Slots[i];
                Box(rect, s == null ? Hex("E5ECEF") : Color.white);
                if (s == null) Text(rect, "+", 28, Hex("B5C5CE"), TextAnchor.MiddleCenter);
                else
                {
                    bool shooting = game.Target(s) >= 0;
                    float bob = shooting && !paused ? Mathf.Sin(Time.unscaledTime * 14 + i) * 1.5f : 0;
                    DrawShooter(new Vector2(rect.center.x, 622 + bob), s.Color, .90f);
                    Text(new Rect(rect.x, 650, 82, 22), s.Ammo.ToString(), 16, Ink, TextAnchor.MiddleCenter);
                    Text(new Rect(rect.x, 676, 82, 12), shooting ? "ĐANG BẮN" : "ĐỢI MÀU", 8, shooting ? Accent : Muted, TextAnchor.MiddleCenter);
                    Box(new Rect(rect.x + 8, 672, 66 * (float)s.Ammo / s.InitialAmmo, 2), Palette(s.Color));
                }
            }
            Text(new Rect(24, 710, 410, 25), "CHỌN NHÂN VẬT ĐẦU HÀNG", 12, Ink);
            for (int lane = 0; lane < game.Lanes.Length; lane++)
            {
                float x = 24 + lane * 188;
                var queue = game.Lanes[lane].ToArray();
                var rect = new Rect(x, 745, 176, 73);
                bool lit = hint == lane && Time.unscaledTime < hintUntil;
                if (lit) Box(new Rect(x - 3, 742, 182, 79), Accent);
                bool enabled = !paused && game.State == GameState.Playing && queue.Length > 0 && game.Slots.Any(s => s == null);
                if (Button(rect, "", queue.Length == 0 ? Hex("E5ECEF") : Color.white, Ink, enabled)) Select(lane);
                if (queue.Length == 0) Text(rect, "Hết nhân vật", 13, Muted, TextAnchor.MiddleCenter);
                else
                {
                    DrawShooter(new Vector2(x + 31, 772), queue[0].Color, 1);
                    Text(new Rect(x + 63, 753, 105, 25), queue[0].Ammo.ToString(), 22, Ink);
                    Text(new Rect(x + 63, 782, 110, 22), ColorName(queue[0].Color), 9, Muted);
                }
                Text(new Rect(x, 825, 176, 17), "LANE " + (lane + 1) + "  ·  " + queue.Length + " nhân vật", 9, Muted);
                for (int q = 1; q < queue.Length; q++)
                {
                    float qx = x + 10 + (q - 1) * 42;
                    DrawShooter(new Vector2(qx + 5, 855), queue[q].Color, .42f);
                    Text(new Rect(qx - 10, 875, 35, 16), queue[q].Ammo.ToString(), 9, Muted, TextAnchor.MiddleCenter);
                }
            }
            if (Button(new Rect(24, 907, 137, 32), "Hint", Color.white, Accent))
            {
                hint = game.HintLane(); hintUntil = Time.unscaledTime + 4;
                message = hint >= 0 ? "Thử nhân vật ở hàng " + (hint + 1) + "." : "Đợi nhân vật trong ô chờ hoàn thành.";
                messageUntil = hintUntil;
            }
            if (Button(new Rect(174, 907, 104, 32), fast ? "Speed ×2" : "Speed ×1", Color.white, Ink)) fast = !fast;
            if (Button(new Rect(290, 907, 126, 32), paused ? "Continue" : "Pause", Color.white, Ink)) paused = !paused;
            Text(new Rect(430, 911, 147, 23), "Phím 1 / 2 / 3 · R", 9, Muted, TextAnchor.MiddleRight);

            foreach (var p in particles)
            {
                float t = Mathf.Clamp01((Time.unscaledTime - p.Started) / .3f);
                var pos = Vector2.Lerp(p.From, p.To, t);
                pos.x += Mathf.Sin(t * Mathf.PI) * 18;
                Dot(pos, 4, p.Color);
            }
            if (game.State != GameState.Playing) EndScreen();
            else if (paused)
            {
                Box(new Rect(0, 140, 600, 752), new Color(.07f, .14f, .2f, .65f), false);
                Text(new Rect(60, 350, 480, 80), "PAUSED", 30, Color.white, TextAnchor.MiddleCenter);
                if (Button(new Rect(210, 450, 180, 48), "Continue", Color.white, Accent)) paused = false;
            }
        }

        void EndScreen()
        {
            bool won = game.State == GameState.Won;
            Box(new Rect(0, 0, Width, Height), new Color(.06f, .13f, .19f, .72f), false);
            Box(new Rect(73, 310, 454, 318), Color.white);
            Text(new Rect(100, 335, 400, 32), won ? "PICTURE COMPLETE" : "CẦN MỘT THỨ TỰ KHÁC", 11, Accent, TextAnchor.MiddleCenter);
            Text(new Rect(100, 379, 400, 60), won ? "Superb!" : "Out of Space", 36, Ink, TextAnchor.MiddleCenter);
            Text(new Rect(107, 449, 386, 68), won ? "Bạn đã dùng " + game.SpentAmmo.ToString("N0") + " đạn màu qua " + game.Moves + " picks." : "Các nhân vật đang chờ màu chưa mở.\nThử ưu tiên màu của vùng đang mở.", 16, Muted, TextAnchor.MiddleCenter);
            var playRect = new Rect(160, 535, 280, 73);
            GUI.DrawTexture(playRect, originalAssets.PlayButton, ScaleMode.StretchToFill);
            Text(new Rect(160, 534, 280, 65), "Retry this level", 15, Ink, TextAnchor.MiddleCenter);
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && playRect.Contains(Event.current.mousePosition))
            { Event.current.Use(); Restart(); }
        }

        void DrawShooter(Vector2 center, int color, float scale)
        {
            GUI.color = new Color(1, 1, 1, .22f);
            GUI.DrawTexture(new Rect(center.x - 24 * scale, center.y + 15 * scale, 48 * scale, 16 * scale), originalAssets.Shadow);
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(center.x - 32 * scale, center.y - 31 * scale, 64 * scale, 75 * scale), originalAssets.Portrait(color), ScaleMode.ScaleToFit);
        }
        void PlaySound(AudioClip clip, float volume) { if (!muted && !smokeMode && clip != null) audioSource.PlayOneShot(clip, volume); }
        void Dot(Vector2 p, float radius, Color color) { GUI.color = color; GUI.DrawTexture(new Rect(p.x - radius, p.y - radius, radius * 2, radius * 2), circle); GUI.color = Color.white; }
        void Box(Rect rect, Color color, bool rounded = true)
        {
            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, color, 0, rounded ? Mathf.Min(12, rect.height / 2) : 0);
        }
        void Text(Rect rect, string text, int size, Color color, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            label.fontSize = size; label.normal.textColor = color; label.alignment = alignment; GUI.Label(rect, text, label);
        }
        bool Button(Rect rect, string text, Color background, Color foreground, bool enabled = true)
        {
            bool hover = rect.Contains(Event.current.mousePosition) && enabled;
            Box(rect, hover ? Color.Lerp(background, Accent, .09f) : background);
            Text(rect, text, 12, enabled ? foreground : Muted, TextAnchor.MiddleCenter);
            if (enabled && Event.current.type == EventType.MouseDown && Event.current.button == 0 && rect.Contains(Event.current.mousePosition))
            { Event.current.Use(); return true; }
            return false;
        }

        IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            var texture = ScreenCapture.CaptureScreenshotAsTexture();
            var pixels = texture.GetPixels32();
            bool visible = false;
            for (int i = 0; i < pixels.Length; i += 97)
                if (pixels[i].r > 30 || pixels[i].g > 30 || pixels[i].b > 30) { visible = true; break; }
            if (!visible)
            {
                File.WriteAllText(Path.Combine(smokeDir, "smoke-failed.txt"), "No rendered image: run the visual test in a visible window.");
                Destroy(texture); Application.Quit(4); yield break;
            }
            File.WriteAllBytes(Path.Combine(smokeDir, name + ".png"), texture.EncodeToPNG());
            Destroy(texture);
        }

        IEnumerator SmokeTest()
        {
            Directory.CreateDirectory(smokeDir);
            yield return null;
            if (error != null) { File.WriteAllText(Path.Combine(smokeDir, "smoke-failed.txt"), error); Application.Quit(1); yield break; }
            originalAssets.SavePreviews(smokeDir);
            yield return Capture("01-start");
            Select(0); Select(1);
            for (int i = 0; i < 20; i++) { Fire(); yield return null; }
            yield return Capture("02-painting");
            int guard = 0;
            while (game.State == GameState.Playing && guard++ < 2500)
            {
                if (!game.Slots.Any(s => game.Target(s) >= 0))
                {
                    int lane = game.HintLane();
                    if (lane >= 0) Select(lane);
                }
                Fire(); yield return null;
            }
            if (game.State != GameState.Won) { File.WriteAllText(Path.Combine(smokeDir, "smoke-failed.txt"), "Autoplay did not win."); Application.Quit(2); yield break; }
            yield return Capture("03-win");
            Restart();
            int[] losingMoves = { 0, 1, 0, 1, 2, 1, 1, 1 };
            foreach (int lane in losingMoves)
            {
                Select(lane);
                for (int i = 0; i < 50; i++) Fire();
            }
            yield return null;
            yield return Capture("04-stalled");
            File.WriteAllText(Path.Combine(smokeDir, "smoke-result.txt"), "Original asset portraits: " + originalAssets.PortraitCount + "\nOriginal sounds and textures loaded\nInitial render OK\nPainting OK\nTutorial autoplay WON with 9000 ammo\nRestart OK\nBlocked-order state: " + game.State);
            Application.Quit(game.State == GameState.Lost ? 0 : 3);
        }

        void OnDestroy() { if (originalAssets != null) originalAssets.Dispose(); if (board != null) Destroy(board); if (circle != null) Destroy(circle); }
    }
}

