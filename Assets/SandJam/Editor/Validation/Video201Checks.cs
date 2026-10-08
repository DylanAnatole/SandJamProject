using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace SandJamTest.Editor
{
    public static class Video201Checks
    {
        [Serializable] public class Replay { public int[] lanes; }
        static LevelData data;static HashSet<string> visited;static int[] solution;
        static SandGame ReplayGame(List<int> path)
        {
            var game=new SandGame(data,5,null,true);
            foreach(int lane in path){if(!game.SelectLane(lane))throw new Exception("Invalid replay prefix");for(int i=0;i<20000 && game.State==GameState.Playing;i++)if(game.Tick(20).Count==0)break;}
            return game;
        }
        static bool Search(List<int> path)
        {
            var game=ReplayGame(path);if(game.State==GameState.Won){solution=path.ToArray();return true;}
            if(game.State==GameState.Lost)return false;
            string key=string.Join(",",game.Lanes.Select(l=>l.Count))+"|"+string.Join(",",game.Regions.Select(r=>r.Remaining))+"|"+string.Join(",",game.Slots.Select(s=>s==null?"-":s.Color+":"+s.Ammo));
            if(!visited.Add(key))return false;if(visited.Count>100000)throw new Exception("Level 201 search budget exceeded");
            foreach(int lane in Enumerable.Range(0,3).Where(game.CanSelectLane).OrderByDescending(i=>game.Target(game.Lanes[i].Peek())>=0))
            {path.Add(lane);if(Search(path))return true;path.RemoveAt(path.Count-1);}
            return false;
        }
        public static void Run()
        {
            data=JsonUtility.FromJson<LevelData>(Resources.Load<TextAsset>("VideoUI/Level201Original").text);SandGame.Validate(data);
            if(data.uiDivider!=20 || !data.gridSlotNeedAmmoCount.SequenceEqual(new[]{0,0,0,150,250}) || !data.parts.Where(p=>p.isOpenedAtStart).Select(p=>p.amount/20).OrderBy(x=>x).SequenceEqual(new[]{6,9}))throw new Exception("Level 201 differs from reference");
            visited=new HashSet<string>();if(!Search(new List<int>()))throw new Exception("No winning replay for Level 201");
            const string path="Assets/SandJam/Resources/VideoUI/Level201Replay.json";
            System.IO.File.WriteAllText(path,JsonUtility.ToJson(new Replay{lanes=solution},true));AssetDatabase.ImportAsset(path);
            Debug.Log("PASS Level 201 matching data and winning replay: "+string.Join(",",solution));
        }
    }
}
