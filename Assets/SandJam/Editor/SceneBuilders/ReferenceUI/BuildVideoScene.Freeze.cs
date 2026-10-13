using System.Linq;
using UnityEditor;
using UnityEngine;
using SandJamTest.Scene3D;
namespace SandJamTest.Editor
{
    public static partial class BuildVideoScene
    {
        internal static void ConfigureFreezeDemo(LevelData data)
        {
            data.sceneName="Freeze mechanic test";data.gridSlotNeedAmmoCount=new int[5];
            int[] colors={7,5,6};
            for(int i=0;i<data.parts.Length;i++){data.parts[i].ColorType=colors[i%3];data.parts[i].amount=30;data.parts[i].isOpenedAtStart=true;}
            data.laneData=new[]{
                new LaneData{ColorAmmoDatas=new[]{Frozen(7,0),Frozen(5,0)}},
                new LaneData{ColorAmmoDatas=new[]{Frozen(5,1),Frozen(6,3)}},
                new LaneData{ColorAmmoDatas=new[]{Frozen(6,2),Frozen(7,0)}}};
        }
        static CharacterData Frozen(int color,int count){return new CharacterData{ColorType=color,AmmoCount=90,IsFreeze=count>0,FreezeCount=count};}
        static void AddFreezeView(SceneActorView actor)
        {
            var material=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/IceShell.mat");
            if(!material){material=new Material(Shader.Find("SandJamTest/IceShell"));material.SetTexture("_MainTex",Resources.Load<Texture2D>("Mechanics/Ice"));material=Save(material,"IceShell.mat");}
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/IceShell.asset");
            if(!mesh)mesh=BevelBox("IceShell",.80f,.68f,.64f,.08f,.025f);
            var shell=DepthMesh("Frozen shell",actor.Visual,mesh,material,Vector3.zero,Vector3.zero);shell.localPosition=new Vector3(0,.53f,0);
            var label=Text("Freeze remaining","",actor.transform,0,0,14,Color.white,-.9f,true);label.transform.localPosition=new Vector3(0,.24f,-.85f);
            var view=actor.gameObject.AddComponent<CharacterFreezeView>();view.Actor=actor;view.Shell=shell.GetComponent<Renderer>();view.Counter=label;
        }
    }
}
