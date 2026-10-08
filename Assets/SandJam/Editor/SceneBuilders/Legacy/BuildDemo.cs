using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SandJamTest.Editor
{
    public static class BuildDemo
    {
        static readonly List<string> results = new List<string>();
        static void Check(bool pass, string name)
        {
            if (!pass) throw new Exception("FAILED: " + name);
            results.Add("PASS: " + name);
        }
        static void Settle(SandGame game)
        {
            int guard = 0;
            while (game.State == GameState.Playing && game.Slots.Any(s => game.Target(s) >= 0) && guard++ < 2000)
            {
                game.Tick();
                CheckConservation(game);
            }
            if (guard >= 2000) throw new Exception("Simulation failed to settle.");
        }
        static void CheckConservation(SandGame game)
        {
            if (game.Supply + game.SpentAmmo != game.InitialSupply || game.Remaining + game.SpentAmmo != game.TotalRequired || game.Regions.Any(r => r.Remaining < 0) || game.Slots.Any(s => s != null && s.Ammo <= 0))
                throw new Exception("Ammo conservation or slot release failed.");
        }

        [MenuItem("Sand Jam/Run gameplay checks")]
        public static void Tests()
        {
            results.Clear();
            string json = Resources.Load<TextAsset>("Tutorial").text;
            var data = JsonUtility.FromJson<LevelData>(json);
            var game = new SandGame(data);
            Check(game.Remaining == 9000 && game.Supply == 9000 && game.Lanes.Sum(l => l.Count) == 13, "Tutorial loads: 9000 ammo, 13 characters");
            Check(game.Regions.Count(r => r.Open) == 1, "Only red is open at start");
            Check(!game.SelectLane(-1) && !game.SelectLane(3), "Invalid input rejected");
            game.SelectLane(2); game.Tick(1000);
            Check(game.SpentAmmo == 0 && game.Slots[0].Ammo == 800, "Closed green does not consume ammo");

            game = new SandGame(data);
            game.SelectLane(0); game.SelectLane(1);
            game.Tick(10000);
            Check(game.SpentAmmo == 1600 && game.Slots.All(s => s == null), "Oversized tick clamps ammo and releases slots");
            Check(game.Regions[0].Remaining == 760, "Region amount stays independent of pixel count");
            game.SelectLane(0); // Purple must wait; red is behind it.
            game.SelectLane(0); Settle(game);
            Check(game.Regions[0].Remaining == 0 && game.Regions[1].Open && game.Regions[3].Open && !game.Regions[2].Open, "Completing red opens linked green and yellow only");
            Check(game.Slots.Any(s => s != null && s.Color == 7 && s.Ammo == 800), "Waiting character keeps its ammo");

            game = new SandGame(data);
            int moves = 0;
            while (game.State == GameState.Playing && moves++ < 30)
            {
                Settle(game);
                if (game.State != GameState.Playing) break;
                int lane = game.HintLane();
                if (lane < 0 || !game.SelectLane(lane)) throw new Exception("Tutorial solver became stuck.");
            }
            Check(game.State == GameState.Won && game.SpentAmmo == 9000 && game.Remaining == 0, "Full tutorial has a valid winning route");
            Check(!game.SelectLane(0) && game.Tick().Count == 0, "Win is terminal until restart");
            Check(JsonUtility.ToJson(data) == JsonUtility.ToJson(JsonUtility.FromJson<LevelData>(json)), "Playing does not mutate source DTO");

            game = new SandGame(data);
            foreach (int lane in new[] { 0, 1, 0, 1, 2, 1, 1, 1 }) { game.SelectLane(lane); Settle(game); }
            Check(game.State == GameState.Lost && game.Slots.All(s => s != null), "Wrong order produces full blocked stash and loss");
            Check(!game.SelectLane(0), "Blocked slots cannot be overfilled");

            game = new SandGame(data, 1);
            game.SelectLane(0);
            Check(game.State == GameState.Playing && !game.SelectLane(1), "Full but actively shooting stash does not lose");
            Settle(game);
            Check(game.Slots[0] == null && game.State == GameState.Playing, "Firing frees space before loss evaluation");

            var changed = JsonUtility.FromJson<LevelData>(json);
            changed.parts[0].rows[0] = -1;
            bool rejected = false;
            try { new SandGame(changed); } catch (ArgumentException) { rejected = true; }
            Check(rejected, "Out-of-bounds level rejected");
            var fresh = new SandGame(data);
            Check(fresh.Moves == 0 && fresh.SpentAmmo == 0 && fresh.Remaining == 9000 && fresh.Slots.All(s => s == null), "Restart creates clean state");
            Directory.CreateDirectory("TestResults");
            File.WriteAllLines("TestResults/model-tests.txt", results);
            Debug.Log(string.Join("\n", results));
        }

        [MenuItem("Sand Jam/Open test scene")]
        public static void OpenScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene("Assets/SandJam/Scenes/CoreGameplay.unity");
        }

        [MenuItem("Sand Jam/Build Windows test")]
        public static void Build()
        {
            Tests();
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SandJam/Resources/Original/CharacterVisual.prefab");
            if (!source) throw new Exception("Original character prefab missing.");
            var skin = source.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (!skin || !skin.sharedMesh || skin.bones.Length != 41 || skin.bones.Any(b => b == null)) throw new Exception("Original character rig incomplete.");
            if (source.GetComponentsInChildren<MonoBehaviour>(true).Length != 0) throw new Exception("Unexpected original scripts in visual prefab.");
            File.WriteAllText("TestResults/asset-checks.txt", "PASS: Original mesh and 41 bones resolved\nPASS: Visual prefab has no original scripts\n");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Sand Jam - Core Gameplay").AddComponent<SandJamDemo>();
            EditorSceneManager.SaveScene(scene, "Assets/SandJam/Scenes/CoreGameplay.unity");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/SandJam/Scenes/CoreGameplay.unity", true) };
            PlayerSettings.companyName = "Gameplay Lab";
            PlayerSettings.productName = "Sand Jam - Core Test";
            PlayerSettings.defaultScreenWidth = 660;
            PlayerSettings.defaultScreenHeight = 1056;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.colorSpace = ColorSpace.Gamma;
            QualitySettings.vSyncCount = 0;
            Directory.CreateDirectory("BuildAssets");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { "Assets/SandJam/Scenes/CoreGameplay.unity" },
                locationPathName = "BuildAssets/SandJam-AssetsTest.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });
            File.WriteAllText("TestResults/build-result.txt", report.summary.result + "\nErrors: " + report.summary.totalErrors + "\nSize: " + report.summary.totalSize);
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Player build failed.");
            AssetDatabase.SaveAssets();
        }
    }
}
