using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SandJamTest.Editor
{
    // Building new levels. A level is one JSON file (LevelData) in Resources/CustomLevels plus its id in
    // Resources/Levels/Campaign.json; the gameplay prefab builds the picture, regions and cube lanes from it at
    // runtime. Templates live in Assets/SandJam/Levels/Templates; the format and workflow are described in
    // Documentation/THIET_KE_LEVEL_VI.md.
    public static class LevelAuthoring
    {
        const string TemplateFolder = "Assets/SandJam/Levels/Templates";
        const string DataFolder = "Assets/SandJam/Resources/" + Campaign.CustomFolder; // in the repository (see .gitignore)
        const string CampaignPath = "Assets/SandJam/Resources/Levels/Campaign.json";
        [Serializable] sealed class CampaignFile { public string[] levels; }

        [MenuItem("Sand Jam/Levels/New level from template")]
        public static void NewLevelFromMenu()
        {
            string template = Directory.GetFiles(TemplateFolder, "*.json").OrderBy(f => f).FirstOrDefault();
            if (template == null) { Debug.LogError("No level template in " + TemplateFolder); return; }
            CreateFromTemplate(template);
        }

        // Copies a template to a new custom level, appends it to the campaign and makes it the level to play next.
        public static string CreateFromTemplate(string templatePath)
        {
            return Publish(JsonUtility.FromJson<LevelData>(File.ReadAllText(templatePath)));
        }

        // Saves a level as the next free Custom_NNN after checking it, appends it to the campaign and makes it the
        // level to play next.
        public static string Publish(LevelData data)
        {
            int n = 1; string id;
            do id = "Custom_" + n++.ToString("D3"); while (File.Exists(Path.Combine(DataFolder, id + ".json")));
            data.sceneName = id;
            Check(data);
            Directory.CreateDirectory(DataFolder);
            File.WriteAllText(Path.Combine(DataFolder, id + ".json"), JsonUtility.ToJson(data));
            int number = AppendToCampaign(id);
            AssetDatabase.Refresh();
            Campaign.LevelNumber = number;
            Debug.Log("Created level " + id + " as level " + number + ". Press Play to try it; edit " + DataFolder + "/" + id + ".json to change it.");
            return id;
        }

        // Scene that shows how one level is built (diagram in edit mode, the real level in Play mode).
        const string StructureScene = "Assets/SandJam/Scenes/LevelStructure.unity";
        [MenuItem("Sand Jam/Levels/Open level structure scene")]
        public static void OpenStructureScene()
        {
            if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (File.Exists(StructureScene)) { UnityEditor.SceneManagement.EditorSceneManager.OpenScene(StructureScene); return; }
            CreateStructureScene();
        }

        public static void CreateStructureScene()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Single);
            var camera = new GameObject("Preview camera").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.11f, .13f, .18f);
            camera.orthographic = true; camera.transform.position = new Vector3(0, 0, -10);
            var viewer = new GameObject("Level Structure").AddComponent<Scene3D.LevelStructureViewer>();
            viewer.PreviewCamera = camera;
            viewer.Catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>("Assets/SandJam/Resources/LevelPack/Catalog.asset");
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, StructureScene);
            Selection.activeGameObject = viewer.gameObject;
        }

        [MenuItem("Sand Jam/Levels/Check custom levels")]
        public static void CheckCustomLevels()
        {
            var files = Directory.GetFiles(DataFolder, "Custom_*.json").Concat(Directory.GetFiles(TemplateFolder, "*.json")).ToArray();
            foreach (var file in files)
            {
                try { Check(JsonUtility.FromJson<LevelData>(File.ReadAllText(file))); Debug.Log("[Levels] OK " + Path.GetFileName(file)); }
                catch (Exception e) { Debug.LogError("[Levels] " + Path.GetFileName(file) + ": " + e.Message); }
            }
            if (files.Length == 0) Debug.Log("[Levels] No custom levels yet.");
        }

        // Rules the game relies on: valid data, every colour's cube sand equals its regions' sand, and the level
        // can be won with the normal five waiting slots.
        public static void Check(LevelData data)
        {
            SandGame.Validate(data);
            var need = data.parts.Where(p => !p.IsUnlockerPart).GroupBy(p => p.ColorType).ToDictionary(g => g.Key, g => g.Sum(p => p.amount));
            var have = data.laneData.SelectMany(l => l.ColorAmmoDatas).GroupBy(c => c.ColorType).ToDictionary(g => g.Key, g => g.Sum(c => c.AmmoCount));
            foreach (var color in need.Keys.Union(have.Keys))
            {
                int a, b; need.TryGetValue(color, out a); have.TryGetValue(color, out b);
                if (a != b) throw new InvalidOperationException("Colour " + color + ": regions need " + a + " sand but the cubes carry " + b);
            }
            LevelReplaySolver.Solve(data);
        }

        static int AppendToCampaign(string id)
        {
            var campaign = File.Exists(CampaignPath) ? JsonUtility.FromJson<CampaignFile>(File.ReadAllText(CampaignPath)) : new CampaignFile();
            var levels = (campaign.levels ?? new string[0]).ToList();
            if (!levels.Contains(id)) levels.Add(id);
            campaign.levels = levels.ToArray();
            File.WriteAllText(CampaignPath, JsonUtility.ToJson(campaign, true));
            return levels.IndexOf(id) + 1;
        }
    }
}
