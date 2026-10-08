using UnityEditor;
using UnityEngine;
using SandJamTest.Scene3D;
namespace SandJamTest.Editor
{
    public static partial class BuildVideoScene
    {
        static void ConfigureVideo201(Transform root,VideoScreen screen,SandJamSceneController controller)
        {
            screen.ReferenceBoosterLabels=true;
            controller.GameCamera.backgroundColor=Hex("32333E");
            // The original's gray-purple stage is much less saturated than the earlier prototype.
            var plum=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/StagePlum.mat");plum.color=Hex("6B627E");EditorUtility.SetDirty(plum);
            var rails=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/RailTop.mat");rails.color=Hex("575365");EditorUtility.SetDirty(rails);
            foreach(var actor in controller.Characters)
            {
                actor.Visual.localScale=Vector3.one*.86f;
                actor.AmmoLabel.characterSize*=.90f;
                foreach(var text in actor.AmmoLabel.GetComponentsInChildren<TextMesh>())if(text!=actor.AmmoLabel)text.characterSize*=.90f;
                actor.ClickCollider.size=new Vector3(.65f,.78f,1.5f);
            }
            foreach(var label in root.GetComponentsInChildren<TextMesh>(true))
            {
                if(label.text=="Level 147")label.text="Level 201";
                else if(label.text=="147")label.text="201";
                else if(label.text=="2040")label.text="5445";
            }
            controller.AmmoPerShot=20; // One displayed unit per tick (export divider=20).
            root.gameObject.AddComponent<Video201Smoke>();
        }
        [MenuItem("Sand Jam/Video UI/Create Level 201 from video")]
        public static void CreateVideo201(){CharacterVesselMeshBuilder.Generate();video201=true;try{Create();}finally{video201=false;}}
        [MenuItem("Sand Jam/Video UI/Build Level 201 from video")]
        public static void CreateVideo201AndBuild(){Video201Checks.Run();video201=true;try{CreateAndBuild();}finally{video201=false;}}
    }
}
