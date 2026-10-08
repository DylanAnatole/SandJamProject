using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace SandJamTest.Scene3D
{
    // Sandbox scene for one or more mechanics: loads original levels through the normal runtime builder,
    // without campaign progress, coins or saves. On-screen panel: level switcher, restart, autoplay and
    // live mechanic state (frozen counts, sleeping halves, padlock counters, chain pairs).
    public sealed class MechanicTestBootstrap : MonoBehaviour
    {
        [Serializable] public sealed class Entry { public string Title; public string LevelId; [TextArea] public string Rule; }
        public string SceneTitle = "Mechanic test";
        public Entry[] Levels = new Entry[0];
        int index;
        GameObject holder;
        bool autoPlay;
        GUIStyle box, title, body, button;

        void Start() { if (Levels.Length > 0) Load(0); }

        public void Load(int i)
        {
            index = (i % Levels.Length + Levels.Length) % Levels.Length;
            StartCoroutine(LoadRoutine());
        }

        IEnumerator LoadRoutine()
        {
            if (holder) { Destroy(holder); yield return null; }
            var prefab = Resources.Load<GameObject>("LevelPack/Prefabs/Level001");
            var level = Resources.Load<TextAsset>("Levels/Data/" + Levels[index].LevelId);
            if (!prefab || !level) { Debug.LogError("Mechanic test: missing template or level " + Levels[index].LevelId); yield break; }
            // Build inactive so the campaign LevelManager can be removed before any Awake runs.
            holder = new GameObject("Level - " + Levels[index].LevelId);
            holder.SetActive(false);
            var instance = Instantiate(prefab, holder.transform);
            foreach (var manager in instance.GetComponentsInChildren<LevelManager>(true)) DestroyImmediate(manager);
            var screen = instance.GetComponentInChildren<VideoScreen>(true);
            if (screen) screen.Levels = null;
            SandJamSceneController.PendingLevel = level;
            Campaign.SkipIntro = true;
            holder.SetActive(true);
            if (autoPlay) SetAutoPlay(true);
        }

        SandJamSceneController Controller { get { return holder ? holder.GetComponentInChildren<SandJamSceneController>() : null; } }

        // "Tự giải": replays the solver's winning move list (falls back to the hint if no solution is found).
        void SetAutoPlay(bool on)
        {
            autoPlay = on;
            var existing = holder ? holder.GetComponent<AutoPlayer>() : null;
            if (!on) { if (existing) Destroy(existing); return; }
            if (existing || !holder) return;
            var player = holder.AddComponent<AutoPlayer>(); player.Interval = .3f; player.WaitForReplay = true;
            StartCoroutine(Solve(player));
        }
        IEnumerator Solve(AutoPlayer player)
        {
            SandJamSceneController c = null;
            while (player && ((c = Controller) == null || c.Game == null)) yield return null;
            if (!player) yield break;
            try { player.Replay = LevelReplaySolver.Solve(c.Game.Data); }
            catch (Exception e) { Debug.LogWarning("No solver replay, using hints: " + e.Message); player.WaitForReplay = false; }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.PageDown)) Load(index + 1);
            if (Input.GetKeyDown(KeyCode.PageUp)) Load(index - 1);
        }

        string Status()
        {
            var c = Controller;
            if (c == null || c.Game == null) return "Đang tải…";
            var g = c.Game;
            var all = g.Lanes.SelectMany(l => l).Concat(g.Slots.Where(s => s != null)).ToArray();
            int frozen = all.Count(s => s.IsFrozen), halves = g.Slots.Count(s => s != null && s.Half), keys = all.Count(s => s.Key && s.Ammo > 0);
            int chained = all.Count(s => s.Partner != null) / 2;
            var locks = g.Regions.Where(r => r.Data.IsUnlockerPart).Select(r => DisplayAmount.Units(r.Remaining, g.Data.uiDivider)).ToArray();
            string state = g.State == GameState.Won ? "THẮNG" : g.State == GameState.Lost ? "THUA (hết chỗ)" : "Đang chơi";
            return state + " · lượt " + g.Moves + " · còn " + DisplayAmount.Units(g.Remaining, g.Data.uiDivider) +
                   "\nBăng: " + frozen + (frozen > 0 ? " (" + string.Join(",", all.Where(s => s.IsFrozen).Select(s => s.FreezeRemaining.ToString()).ToArray()) + ")" : "") +
                   " · Nửa đang ngủ: " + halves + " · Cặp nối: " + chained +
                   "\nKhối chìa khóa: " + keys + " · Ổ khóa: " + (locks.Length == 0 ? "-" : string.Join(", ", locks));
        }

        void OnGUI()
        {
            if (Levels.Length == 0) return;
            if (box == null)
            {
                box = new GUIStyle(GUI.skin.box) { normal = { background = Texture2D.whiteTexture } };
                title = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold, wordWrap = true };
                body = new GUIStyle(GUI.skin.label) { fontSize = 12, wordWrap = true };
                button = new GUIStyle(GUI.skin.button) { fontSize = 13 };
                title.normal.textColor = body.normal.textColor = Color.white;
            }
            var entry = Levels[index];
            float w = Mathf.Min(360, Screen.width - 16);
            var area = new Rect(8, Screen.height - 232, w, 224);
            var old = GUI.color; GUI.color = new Color(.08f, .07f, .14f, .86f); GUI.Box(area, GUIContent.none, box); GUI.color = old;
            GUILayout.BeginArea(new Rect(area.x + 10, area.y + 8, area.width - 20, area.height - 16));
            GUILayout.Label(SceneTitle + "  ·  " + (index + 1) + "/" + Levels.Length + "  ·  " + entry.Title, title);
            GUILayout.Label(entry.LevelId, body);
            GUILayout.Label(entry.Rule, body);
            GUILayout.Label(Status(), body);
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("◀ Màn trước", button)) Load(index - 1);
            if (GUILayout.Button("Chơi lại", button)) Load(index);
            if (GUILayout.Button("Màn sau ▶", button)) Load(index + 1);
            if (GUILayout.Button(autoPlay ? "Tự giải: BẬT" : "Tự giải: TẮT", button)) SetAutoPlay(!autoPlay);
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
    }
}
