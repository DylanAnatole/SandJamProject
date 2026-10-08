using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using SandJamTest.Scene3D;
namespace SandJamTest.Editor
{
    public static partial class BuildVideoScene
    {
        static LevelCatalog packCatalog;
        static void PrepareCatalog()
        {
            AssetDatabase.Refresh();
            const string path="Assets/SandJam/Resources/LevelPack/Catalog.asset";
            packCatalog=AssetDatabase.LoadAssetAtPath<LevelCatalog>(path);
            if(!packCatalog){packCatalog=ScriptableObject.CreateInstance<LevelCatalog>();AssetDatabase.CreateAsset(packCatalog,path);}
            // Seed once. Later builds preserve entries edited in the catalog inspector.
            if(packCatalog.Levels==null || packCatalog.Levels.Length==0)
                packCatalog.Levels=Enumerable.Range(1,3).Select(number=>new LevelEntry{Number=number,SceneName="SandJamLevel"+number.ToString("D3"),Json=Resources.Load<TextAsset>("LevelPack/Level"+number.ToString("D3"))}).ToArray();
            foreach(var entry in packCatalog.Levels)
            {
                var data=LevelDataManager.Load(entry.Json);entry.SourceId=data.sceneName;
                var replay=LevelReplaySolver.Solve(data);string replayPath="Assets/SandJam/Resources/LevelPack/Replay"+entry.Number.ToString("D3")+".json";
                File.WriteAllText(replayPath,JsonUtility.ToJson(new LevelReplay{lanes=replay},true));AssetDatabase.ImportAsset(replayPath);
                entry.TestReplay=AssetDatabase.LoadAssetAtPath<TextAsset>(replayPath);
                Debug.Log("PASS Level "+entry.Number+": "+data.sceneName+", replay "+replay.Length+" moves");
            }
            packCatalog.Validate();EditorUtility.SetDirty(packCatalog);AssetDatabase.SaveAssets();
            LevelPackChecks.Run(packCatalog);
        }
        static void ConfigureLevelPack(Transform root,VideoScreen screen,SandJamSceneController controller)
        {
            controller.GameCamera.backgroundColor=Hex("32333E");controller.AmmoPerShot=packEntry.Json?LevelDataManager.Load(packEntry.Json).uiDivider:20;
            foreach(var actor in controller.Characters)actor.Visual.localScale=Vector3.one*.86f;
            foreach(var label in root.GetComponentsInChildren<TextMesh>(true))
            {
                if(label.text=="Level 147")label.text="Level "+packEntry.Number;
                else if(label.text=="147")label.text=packEntry.Number.ToString();
            }
            var manager=root.gameObject.AddComponent<LevelManager>();manager.Catalog=packCatalog;manager.Index=Array.IndexOf(packCatalog.Levels,packEntry);manager.Controller=controller;manager.Screen=screen;screen.Levels=manager;
            foreach(var button in screen.Result.GetComponentsInChildren<VideoUiButton>(true))if(button.Action=="home")button.Action="next-level";
            for(int i=0;i<Mathf.Min(3,packCatalog.Levels.Length);i++)Button(screen.Home.transform,"LV "+packCatalog.Levels[i].Number,"level:"+i,85+i*156,909,118,44);
            SetOverlayLayer(screen.Home.transform);
        }
        [MenuItem("Sand Jam/Levels/Create scenes from catalog")]
        public static void CreateLevelPack()
        {
            PrepareCatalog();CharacterVesselMeshBuilder.Generate();
            Directory.CreateDirectory("Assets/SandJam/Resources/LevelPack/Prefabs");AssetDatabase.Refresh();
            var temporaryScenes=new System.Collections.Generic.List<string>();
            try
            {
                foreach(var entry in packCatalog.Levels)
                {
                    packEntry=entry;Create();temporaryScenes.Add(Scene);
                    entry.PrefabResource="LevelPack/Prefabs/Level"+entry.Number.ToString("D3");
                    var root=UnityEngine.Object.FindObjectOfType<VideoScreen>();
                    PrefabUtility.SaveAsPrefabAsset(root.gameObject,"Assets/SandJam/Resources/"+entry.PrefabResource+".prefab");
                }
            }
            finally{packEntry=null;}
            EditorUtility.SetDirty(packCatalog);AssetDatabase.SaveAssets();
            var scene=UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single);
            var bootstrap=new GameObject("Level bootstrap").AddComponent<LevelBootstrap>();bootstrap.Catalog=packCatalog;
            const string gameScene="Assets/SandJam/Scenes/SandJamGame.unity";
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,gameScene);
            foreach(var path in temporaryScenes)AssetDatabase.DeleteAsset(path);
            var existing=EditorBuildSettings.scenes.Where(s=>File.Exists(s.path) && s.path!=gameScene).ToList();
            existing.Insert(0,new EditorBuildSettingsScene(gameScene,true));EditorBuildSettings.scenes=existing.ToArray();
        }
        [MenuItem("Sand Jam/Levels/Build catalog levels")]
        public static void CreateLevelPackAndBuild()
        {
            BurstSandChecks.Run();CreateLevelPack();Directory.CreateDirectory("BuildLevelPack");
            PlayerSettings.defaultScreenWidth=483;PlayerSettings.defaultScreenHeight=1075;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.runInBackground=true;QualitySettings.antiAliasing=4;
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/SandJam/Scenes/SandJamGame.unity"},locationPathName="BuildLevelPack/SandJam-LevelPack.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Level pack build failed");
        }
    }
}
