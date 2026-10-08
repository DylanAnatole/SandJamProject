using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
namespace SandJamTest.Editor
{
    [InitializeOnLoad]
    public sealed class ShaderMaterialChecks : IPreprocessBuildWithReport
    {
        public int callbackOrder {get{return 0;}}
        static ShaderMaterialChecks(){EditorApplication.delayCall+=RepairKnownBindings;}
        // Material names preserve the intended shader even when an imported GUID was stale.
        [MenuItem("Sand Jam/Validation/Repair and validate shaders")]
        public static void Run(){RepairKnownBindings();Validate();}
        static void RepairKnownBindings(){
            if(EditorApplication.isCompiling || EditorApplication.isUpdating){EditorApplication.delayCall+=RepairKnownBindings;return;}
            const string refreshKey="SandJam.ShaderBindings.Refresh.20261006";
            if(!SessionState.GetBool(refreshKey,false)){
                SessionState.SetBool(refreshKey,true);
                foreach(string id in AssetDatabase.FindAssets("t:Shader",new[]{"Assets/SandJam/Resources"}))
                    AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(id),ImportAssetOptions.ForceUpdate);
            }
            bool changed=false;
            foreach(string id in AssetDatabase.FindAssets("t:Material",new[]{"Assets/SandJam/Generated"})){
                var material=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(id));
                if(!material || !material.name.StartsWith("SandJamTest/",StringComparison.Ordinal))continue;
                var expected=Shader.Find(material.name);
                if(expected && material.shader!=expected){material.shader=expected;EditorUtility.SetDirty(material);changed=true;}
            }
            if(changed)AssetDatabase.SaveAssets();
            // Also repair already-instantiated materials when the Editor retained an old binding.
            foreach(var material in Resources.FindObjectsOfTypeAll<Material>()){
                string intended=material.name.Replace(" (Instance)","");
                if(!intended.StartsWith("SandJamTest/",StringComparison.Ordinal))continue;
                var expected=Shader.Find(intended);if(expected && material.shader!=expected)material.shader=expected;
            }
        }
        public static void Validate(){
            int checkedCount=0;
            foreach(string id in AssetDatabase.FindAssets("t:Material",new[]{"Assets"})){
                string path=AssetDatabase.GUIDToAssetPath(id);var material=AssetDatabase.LoadAssetAtPath<Material>(path);
                Check(material,path);checkedCount++;
            }
            foreach(string id in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/SandJam/Resources/LevelPack/Prefabs"})){
                string path=AssetDatabase.GUIDToAssetPath(id);var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                foreach(var renderer in prefab.GetComponentsInChildren<Renderer>(true)){
                    // Geometry-only disabled handles intentionally have no draw material.
                    if(!renderer.enabled && renderer.name=="Editor grid reference")continue;
                    foreach(var material in renderer.sharedMaterials)Check(material,path+" / "+renderer.name);
                }
            }
            foreach(string id in AssetDatabase.FindAssets("t:Shader",new[]{"Assets/SandJam/Resources"})){
                var shader=AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath(id));
                if(!shader.isSupported || ShaderUtil.ShaderHasError(shader))throw new BuildFailedException("Shader unsupported or failed: "+shader.name);
            }
            Debug.Log("PASS: Shader audit, "+checkedCount+" materials and all SandJamGame level renderers");
        }
        static void Check(Material material,string context){
            if(!material || !material.shader || material.shader.name=="Hidden/InternalErrorShader" || !material.shader.isSupported || ShaderUtil.ShaderHasError(material.shader))
                throw new BuildFailedException("Invalid material/shader: "+context);
        }
        public void OnPreprocessBuild(BuildReport report){Run();}
    }
}
