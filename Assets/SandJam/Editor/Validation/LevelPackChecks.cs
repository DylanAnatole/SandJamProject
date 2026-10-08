using System;
using UnityEngine;
namespace SandJamTest.Editor
{
    public static class LevelPackChecks
    {
        static void Check(bool b,string m){if(!b)throw new Exception("Level catalog: "+m);}
        public static void Run(LevelCatalog catalog)
        {
            catalog.Validate();Check(catalog.Levels.Length>0,"Expected at least one level");
            foreach(var e in catalog.Levels)
            {
                var a=LevelDataManager.Load(e.Json);var b=LevelDataManager.Load(e.Json);
                Check(!ReferenceEquals(a,b) && !ReferenceEquals(a.parts,b.parts),"Loads share mutable definitions");
                Check(a.sceneName==e.SourceId,"Wrong source mapping");
                var replay=JsonUtility.FromJson<LevelReplay>(e.TestReplay.text);var game=new SandGame(a,5,null,true);
                foreach(int lane in replay.lanes){Check(game.SelectLane(lane),"Invalid winning replay");for(int i=0;i<50000 && game.State==GameState.Playing;i++)if(game.Tick(20).Count==0)break;}
                Check(game.State==GameState.Won,"Replay does not win");
            }
            bool rejected=false;var broken=new TextAsset("{}");try{LevelDataManager.Load(broken);}catch(ArgumentException){rejected=true;}UnityEngine.Object.DestroyImmediate(broken);Check(rejected,"Malformed definition accepted");
            var clone=UnityEngine.Object.Instantiate(catalog);clone.Levels=new[]{clone.Levels[0],clone.Levels[0]};rejected=false;try{clone.Validate();}catch(ArgumentException){rejected=true;}UnityEngine.Object.DestroyImmediate(clone);Check(rejected,"Duplicate numbers accepted");
            Debug.Log("PASS catalog isolation, validation and all catalog winning replays");
        }
    }
}
