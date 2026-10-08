using System;
using System.Collections.Generic;
namespace SandJamTest.Scene3D
{
    // Managed reference used by editor regression checks: runs the same step job without Burst.
    public sealed class SandPourSimulation : IDisposable
    {
        public struct Grain { public int Index, X, Y, Speed, Dir; }
        public readonly int[] Rows, Cols;
        public readonly int Width, InletX, InletY;
        public readonly List<Grain> Moving=new List<Grain>();
        public readonly bool[] Filled;
        public int Emitted {get{return core.Emitted;}}
        public int Settled {get{return core.Settled;}}
        public int Requested {get{return core.Requested;}}
        public bool Complete {get{return core.Complete;}}
        readonly BurstSandSimulation core;
        public SandPourSimulation(int[] rows,int[] cols){
            core=new BurstSandSimulation(rows,cols,true);Rows=rows;Cols=cols;Width=core.Width;InletX=core.InletX;InletY=core.InletY;
            Filled=new bool[rows.Length];Moving.Capacity=rows.Length;
        }
        public bool Inside(int x,int y){return core.Inside(x,y);}
        public void Request(int count){core.Request(count);}
        public void Step(){
            core.ScheduleSteps(1);
            Moving.Clear();for(int i=0;i<core.MovingCount;i++)Moving.Add(core.Moving[i]);
            for(int i=0;i<Filled.Length;i++)Filled[i]=core.Filled[i]!=0;
        }
        public void Validate(){core.Validate();}
        public void Dispose(){core.Dispose();}
    }
}
