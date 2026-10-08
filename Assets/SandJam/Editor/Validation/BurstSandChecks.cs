using System;
using System.Linq;
using System.Diagnostics;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using SandJamTest.Scene3D;
namespace SandJamTest.Editor
{
    public static class BurstSandChecks
    {
        static void CheckPaint()
        {
            using(var filled=new NativeArray<byte>(256,Allocator.TempJob))
            using(var colors=new NativeArray<Color32>(256,Allocator.TempJob))
            using(var output=new NativeArray<Color32>(256,Allocator.TempJob))
            {
                var fillWrite=filled;var colorWrite=colors;
                for(int i=0;i<256;i++){fillWrite[i]=(byte)(i%3==0?0:1);colorWrite[i]=new Color32((byte)i,(byte)(255-i),(byte)(i/2),255);}
                Color32 empty=new Color32(182,180,204,255);
                foreach(float glow in new[]{0f,.03f,.06f,.12f})
                {
                    new SceneRegionView.PaintRegionJob{Filled=filled,Colors=colors,Output=output,Empty=empty,Glow=glow}.Run();
                    for(int i=0;i<256;i++)
                    {
                        Color32 expected=filled[i]==0?empty:(Color32)Color.Lerp(colors[i],Color.white,glow);
                        var actual=output[i];
                        // Burst/native float rounding can differ by one 8-bit colour step.
                        if(Math.Abs(actual.r-expected.r)>1 || Math.Abs(actual.g-expected.g)>1 || Math.Abs(actual.b-expected.b)>1 || actual.a!=expected.a)
                            throw new Exception("Burst pixel colour differs at "+i);
                    }
                }
            }
            UnityEngine.Debug.Log("PASS Burst region colours: locked, black/white, partial and completed glow");
        }
        public static void Run()
        {
            CheckPaint();
            ShaderMaterialChecks.Run();
            foreach(string resource in new[]{"LevelPack/Level001","LevelPack/Level002","LevelPack/Level003","VideoUI/Level201Original"})
            foreach(var part in LevelDataManager.Load(Resources.Load<TextAsset>(resource)).parts)
            using(var fast=new BurstSandSimulation(part.rows,part.cols))
            using(var old=new SandPourSimulation(part.rows,part.cols))
            {
                if(!fast.Inside(fast.InletX,fast.InletY) || part.rows.Where((y,i)=>part.cols[i]==fast.InletX).Max()!=fast.InletY)throw new Exception("Inlet must be at the top of the central column");
                float midpoint=(part.cols.Min()+part.cols.Max())*.5f;
                if(part.cols.Any(x=>Math.Abs(x-midpoint)<Math.Abs(fast.InletX-midpoint)))throw new Exception("Inlet is not centered in region");
                for(int phase=1;phase<=4;phase++)
                {
                    int count=part.rows.Length*phase/4;old.Request(count);fast.Request(count);
                    int steps=0;
                    do{
                        for(int i=0;i<16;i++)old.Step();fast.ScheduleSteps(16);fast.CompleteSteps();steps+=16;
                        if(old.Settled!=fast.Settled || old.Moving.Count!=fast.MovingCount)throw new Exception("Burst differs from managed simulation");
                        for(int i=0;i<old.Moving.Count;i++){var a=old.Moving[i];var b=fast.Moving[i];if(a.Index!=b.Index || a.X!=b.X || a.Y!=b.Y)throw new Exception("Burst grain path differs");}
                        for(int i=0;i<old.Filled.Length;i++)if(old.Filled[i]!=(fast.Filled[i]!=0))throw new Exception("Burst fill differs");
                        fast.Validate();if(steps>20000)throw new Exception("Burst flow stalled");
                    }while(!old.Complete);
                }
            }
            var rows=Enumerable.Range(0,4800).Select(i=>i/60).ToArray();var cols=Enumerable.Range(0,4800).Select(i=>i%60).ToArray();
            using(var warm=new BurstSandSimulation(rows,cols)){warm.Request(rows.Length);warm.ScheduleSteps(8);warm.CompleteSteps();}
            double managed;var watch=Stopwatch.StartNew();
            using(var baseline=new SandPourSimulation(rows,cols)){baseline.Request(rows.Length);watch.Restart();for(int i=0;i<5000;i++)baseline.Step();watch.Stop();managed=watch.Elapsed.TotalMilliseconds;}
            using(var fast=new BurstSandSimulation(rows,cols))
            {
                fast.Request(rows.Length);watch.Restart();for(int i=0;i<625;i++){fast.ScheduleSteps(8);fast.CompleteSteps();}watch.Stop();
                fast.Validate();if(!fast.UsedBurst)throw new Exception("Job ran without Burst compilation");
                System.IO.Directory.CreateDirectory("TestResults/Burst");
                System.IO.File.WriteAllText("TestResults/Burst/benchmark.txt","PASS: Identical managed/Burst falling-sand automaton across all shipped levels and four request phases; every grain stays in its region; top-center inlet\nPASS: Burst compiler executed job\n4800 cells, 5000 steps, 8 steps/job; editor CPU-only benchmark (not mobile FPS)\nManaged ms: "+managed.ToString("F2")+"\nBurst Jobs ms: "+watch.Elapsed.TotalMilliseconds.ToString("F2")+"\n");
            }
            UnityEngine.Debug.Log("PASS Burst equivalence, conservation, disposal and benchmark");
        }
    }
}
