using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SandJamTest.Editor
{
    // Builds Resources/Levels/Campaign.json from the original level order (Default_LevelOrderConfig),
    // keeping every entry so level N matches the shipped game; the solver verdicts go to the report.
    // Runs incrementally so the editor stays responsive.
    public static class CampaignBuilder
    {
        const string OrderPath = "Assets/SandJam/Resources/Levels/LevelOrder.json";
        const string OutputPath = "Assets/SandJam/Resources/Levels/Campaign.json";
        const string ReportPath = "TestResults/Campaign/report.txt";
        [Serializable] sealed class Order { public string[] levelOrder; }
        [Serializable] sealed class CampaignFile { public string[] levels; }

        static string[] order;
        static List<string> unique;
        static Dictionary<string, string> verdicts;
        static int cursor;
        static Stopwatch watch;
        public static bool Running { get; private set; }
        public static string Progress { get { return unique == null ? "idle" : cursor + "/" + unique.Count; } }

        [MenuItem("Sand Jam/Levels/Build campaign from original order")]
        public static void Start()
        {
            if (Running) return;
            order = JsonUtility.FromJson<Order>(File.ReadAllText(OrderPath)).levelOrder;
            unique = order.Distinct().ToList();
            verdicts = new Dictionary<string, string>();
            cursor = 0; watch = Stopwatch.StartNew(); Running = true;
            EditorApplication.update += Step;
        }

        static void Step()
        {
            var frame = Stopwatch.StartNew();
            while (cursor < unique.Count && frame.ElapsedMilliseconds < 200)
            {
                string id = unique[cursor++];
                verdicts[id] = Check(id);
            }
            EditorUtility.DisplayProgressBar("Sand Jam campaign", "Checking " + Progress, (float)cursor / unique.Count);
            if (cursor < unique.Count) return;
            EditorApplication.update -= Step; EditorUtility.ClearProgressBar(); Running = false;
            Write();
        }

        public static string Check(string id)
        {
            var asset = Resources.Load<TextAsset>("Levels/Data/" + id);
            if (!asset) return "missing file";
            try
            {
                var data = LevelDataManager.Load(asset);
                LevelReplaySolver.Solve(data);
                return "ok";
            }
            catch (Exception e) { return e.Message.Replace("\n", " "); }
            finally { Resources.UnloadAsset(asset); }
        }

        static void Write()
        {
            // Unsupported/unsolved levels stay in place (LevelBootstrap skips files that fail to load).
            var playable = order.ToArray();
            File.WriteAllText(OutputPath, JsonUtility.ToJson(new CampaignFile { levels = playable }, true));
            AssetDatabase.ImportAsset(OutputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            var lines = new List<string>
            {
                "Original order entries: " + order.Length + " (" + unique.Count + " unique levels)",
                "Campaign entries: " + playable.Length + " (" + playable.Distinct().Count() + " unique levels)",
                "Seconds: " + watch.Elapsed.TotalSeconds.ToString("F1"), ""
            };
            lines.AddRange(verdicts.GroupBy(v => v.Value.StartsWith("ok") ? "ok" : v.Value.Split(':')[0]).Select(g => g.Key + ": " + g.Count()));
            lines.Add(""); lines.AddRange(verdicts.Where(v => v.Value != "ok").Select(v => v.Key + " -> " + v.Value));
            File.WriteAllLines(ReportPath, lines);
            UnityEngine.Debug.Log("Campaign built: " + playable.Length + " entries. Report: " + ReportPath);
        }
    }
}
