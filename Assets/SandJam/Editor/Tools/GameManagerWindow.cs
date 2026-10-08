using System.IO;
using UnityEditor;
using UnityEngine;

namespace SandJamTest.Editor
{
    // Development dashboard for the campaign: inspect and edit the save, jump to a level,
    // grant coins/boosters, reset intros and read lifetime stats. Works in and out of Play mode.
    public sealed class GameManagerWindow : EditorWindow
    {
        int jumpLevel = 1, coinAmount = 500;
        Vector2 scroll;

        [MenuItem("Sand Jam/Game Manager")]
        static void Open() { GetWindow<GameManagerWindow>("Sand Jam Manager").minSize = new Vector2(320, 420); }

        void OnInspectorUpdate() { Repaint(); }

        void OnGUI()
        {
            var d = SaveManager.Data;
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("Save", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(SaveManager.FilePath, EditorStyles.miniLabel, GUILayout.Height(16));
            EditorGUILayout.LabelField("Level", d.level + "  (" + Campaign.CurrentId + ")");
            EditorGUILayout.LabelField("Campaign entries", Campaign.Levels.Length.ToString());
            EditorGUILayout.LabelField("Coins", d.coins.ToString());
            EditorGUILayout.LabelField("Boosters", "Flare " + d.flares + " · Swap " + d.swaps + " · Select " + d.selects);
            EditorGUILayout.LabelField("Sound", d.soundOn ? "On" : "Off");
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Lives", LivesManager.Lives + "/" + LivesManager.Max + (LivesManager.IsFull ? "" : "  (next in " + LivesManager.Countdown(LivesManager.UntilNext) + ")"));
                if (GUILayout.Button("-1", GUILayout.Width(32))) LivesManager.LoseLife();
                if (GUILayout.Button("Refill", GUILayout.Width(52))) LivesManager.Refill();
            }
            EditorGUILayout.LabelField("Intros seen", d.introsSeen.Count == 0 ? "-" : string.Join(", ", d.introsSeen));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Progress", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                jumpLevel = Mathf.Max(1, EditorGUILayout.IntField("Go to level", jumpLevel));
                if (GUILayout.Button("Set", GUILayout.Width(50))) SetLevel(jumpLevel);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("◀ Previous")) SetLevel(d.level - 1);
                if (GUILayout.Button("Next ▶")) SetLevel(d.level + 1);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Feature L15")) SetLevel(15);
                if (GUILayout.Button("Secret L45")) SetLevel(45);
                if (GUILayout.Button("Hard L20")) SetLevel(20);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Economy", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                coinAmount = EditorGUILayout.IntField("Coins", coinAmount);
                if (GUILayout.Button("Add", GUILayout.Width(50))) EconomyManager.Add(coinAmount, "debug");
                if (GUILayout.Button("Set", GUILayout.Width(50))) EconomyManager.SetCoins(coinAmount, "debug");
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("+1 Flare")) EconomyManager.AddBooster("rocket", 1, "debug");
                if (GUILayout.Button("+1 Swap")) EconomyManager.AddBooster("swap", 1, "debug");
                if (GUILayout.Button("+1 Select")) EconomyManager.AddBooster("select", 1, "debug");
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Stats", EditorStyles.boldLabel);
            var s = d.stats;
            EditorGUILayout.LabelField("Started / Won / Lost", s.levelsStarted + " / " + s.levelsWon + " / " + s.levelsLost);
            EditorGUILayout.LabelField("Highest level", s.highestLevel.ToString());
            EditorGUILayout.LabelField("Coins earned / spent", s.coinsEarned + " / " + s.coinsSpent);
            EditorGUILayout.LabelField("Boosters used", s.boostersUsed.ToString());
            EditorGUILayout.LabelField("Time played", (s.secondsPlayed / 60f).ToString("F1") + " min");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Maintenance", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Reset intros")) { d.introsSeen.Clear(); SaveManager.Save(); }
                if (GUILayout.Button("Reload from disk")) SaveManager.Load();
                if (GUILayout.Button("Reveal file")) EditorUtility.RevealInFinder(File.Exists(SaveManager.FilePath) ? SaveManager.FilePath : Application.persistentDataPath);
            }
            if (GUILayout.Button("Reset save (new player)") && EditorUtility.DisplayDialog("Reset save", "Delete all progress, coins, boosters and stats?", "Reset", "Cancel"))
                SaveManager.ResetAll();
            if (Application.isPlaying && GUILayout.Button("Reload level now"))
            {
                var manager = FindObjectOfType<Scene3D.LevelManager>();
                if (manager) manager.Reload(true);
            }
            EditorGUILayout.EndScrollView();
        }

        static void SetLevel(int level) { Campaign.LevelNumber = Mathf.Max(1, level); Campaign.NotifyChanged(); }
    }
}
