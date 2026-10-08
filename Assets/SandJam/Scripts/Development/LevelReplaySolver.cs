using System;
using System.Linq;
using System.Collections.Generic;
namespace SandJamTest
{
    // Solvability probe used by the campaign builder and the mechanic test scenes ("Tự giải").
    // Gameplay itself never depends on this search.
    public sealed class LevelReplaySolver
    {
        readonly LevelData data;
        readonly int capacity;
        readonly HashSet<string> visited=new HashSet<string>();
        int[] solution;
        LevelReplaySolver(LevelData definition,int slots){data=definition;capacity=slots;}
        // slots = 5 normally; 6 = after one "Play on" (extra waiting slot).
        public static int[] Solve(LevelData data,int slots=5)
        {
            var solver=new LevelReplaySolver(data,slots);
            if(!solver.Search(new List<int>()))throw new InvalidOperationException("No winning replay: "+data.sceneName);
            return solver.solution;
        }
        SandGame Replay(List<int> path)
        {
            var game=new SandGame(data,capacity,null,true);
            foreach(int lane in path)
            {
                if(!game.SelectLane(lane))throw new InvalidOperationException("Invalid search prefix");
                int count=0;while(game.State==GameState.Playing && game.Tick(20).Count>0)if(++count>50000)throw new InvalidOperationException("Replay drain budget exceeded");
            }
            return game;
        }
        bool Search(List<int> path)
        {
            var game=Replay(path);
            if(game.State==GameState.Won){solution=path.ToArray();return true;}
            if(game.State==GameState.Lost)return false;
            string key=string.Join(",",game.Lanes.Select(l=>l.Count))+"|"+string.Join(",",game.Regions.Select(r=>r.Remaining+":"+r.Open))+"|"+string.Join(",",game.Slots.Select(s=>s==null?"-":s.Color+":"+s.Ammo+":"+(s.Partner==null?-1:Array.IndexOf(game.Slots,s.Partner))));
            if(!visited.Add(key))return false;
            if(visited.Count>100000)throw new InvalidOperationException("Search budget exceeded for "+data.sceneName);
            foreach(int lane in Enumerable.Range(0,game.Lanes.Length).Where(game.CanSelectLane).OrderByDescending(i=>game.Target(game.Lanes[i].Peek())>=0))
            {path.Add(lane);if(Search(path))return true;path.RemoveAt(path.Count-1);}
            return false;
        }
    }
}
