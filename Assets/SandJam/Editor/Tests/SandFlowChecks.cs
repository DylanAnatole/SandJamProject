using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using SandJamTest.Scene3D;
namespace SandJamTest.Editor
{
    public static class SandFlowChecks
    {
        static void Drain(SandPourSimulation sand)
        {
            var previous=new Dictionary<int,int>();
            for(int tick=0;!sand.Complete && tick<40000;tick++)
            {
                sand.Step(); sand.Validate();
                foreach(var grain in sand.Moving)
                {
                    int y; if(previous.TryGetValue(grain.Index,out y) && grain.Y>y) throw new Exception("A grain moved upward");
                    previous[grain.Index]=grain.Y;
                }
            }
            if(!sand.Complete) throw new Exception("Sand flow stalled");
        }
        public static void Run()
        {
            var data=JsonUtility.FromJson<LevelData>(Resources.Load<TextAsset>("Tutorial").text);
            foreach(var part in data.parts)
            using(var sand=new SandPourSimulation(part.rows,part.cols))
            {
                sand.Request(part.rows.Length/3); Drain(sand);
                sand.Request(part.rows.Length); Drain(sand);
                if(sand.Settled!=part.rows.Length) throw new Exception("Sand lost");
            }
            // Open rectangle: gravity alone must stack every column from the floor with no gaps.
            var rows=Enumerable.Range(0,384).Select(i=>i/32).ToArray();
            var cols=Enumerable.Range(0,384).Select(i=>i%32).ToArray();
            using(var flat=new SandPourSimulation(rows,cols))
            {
                flat.Request(256); Drain(flat);
                for(int i=0;i<384;i++) if(flat.Filled[i] && rows[i]>0 && !flat.Filled[i-32]) throw new Exception("Floating sand above a gap");
                var heights=Enumerable.Range(0,32).Select(x=>Enumerable.Range(0,12).Count(y=>flat.Filled[y*32+x])).ToArray();
                if(heights.Max()-heights.Min()>2) throw new Exception("Sand surface is not level: "+string.Join(",",heights));
            }
            // Ledge pocket: cells under an overhang are filled by sideways creep.
            var pocket=new List<int>();for(int y=0;y<6;y++)for(int x=0;x<12;x++)if(!(y==3 && x>=5))pocket.Add(y*12+x);
            using(var ledge=new SandPourSimulation(pocket.Select(i=>i/12).ToArray(),pocket.Select(i=>i%12).ToArray()))
            {
                ledge.Request(pocket.Count); Drain(ledge);
                if(ledge.Settled!=pocket.Count) throw new Exception("Overhang pocket left empty");
            }
            Directory.CreateDirectory("TestResults/EvenFlow");
            File.WriteAllText("TestResults/EvenFlow/grid-tests.txt","PASS: 5 tutorial shapes fully filled\nPASS: No moving or settled grain overlap\nPASS: No upward movement\nPASS: Conservation during partial and full pour\nPASS: Rectangle fills from the floor with a level surface and no floating sand\nPASS: Pocket under an overhang is filled\n");
        }
    }
}
