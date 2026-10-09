using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
namespace SandJamTest.Scene3D
{
    // Falling-sand cellular automaton for one region. Row 0 is the bottom of the board.
    // Grains enter at the top-center mouth as a thin stream, accelerate downward, roll diagonally
    // off slopes, spread along the surface so it stays level (as in the original), and settle when blocked. Cells no falling grain
    // can reach (pockets above the mouth) are settled from the bottom once the mouth is buried.
    public sealed class BurstSandSimulation : IDisposable
    {
        public const int MaxFallSpeed = 2;   // cells per step (gentle fall)
        public const int SeepPerStep = 2;    // pocket cells settled per step once the mouth is buried
        public readonly int[] Rows,Cols;
        public readonly int Width,InletX,InletY;
        public NativeArray<byte> Filled;
        public NativeArray<SandPourSimulation.Grain> Moving;
        NativeArray<int> rows,cols,cellAt,spawns,seep,state,topCell;
        readonly int minX,maxX;
        // Simulation step at which each cell settled (-1 = empty); drives the fresh-sand highlight.
        public NativeArray<int> SettleStep;
        NativeArray<byte> occupied;
        readonly int height;
        readonly bool managed;
        JobHandle handle;bool pending,disposed;
        public int Emitted {get{return state[0];}}
        public int Settled {get{return state[1];}}
        public int Requested {get{return state[2];}}
        public int MovingCount {get{return state[3];}}
        public bool Complete {get{return Settled==Requested;}}
        public bool UsedBurst {get{return state[4]==1;}}
        public int StepCount {get{return state[8];}}
        // Column and row the stream currently falls from (the mouth sweeps left and right across the top).
        public int MouthX {get{return state[9];}}
        public int MouthY {get{int c=topCell[state[9]];return c<0?InletY:Rows[c];}}
        public BurstSandSimulation(int[] sourceRows,int[] sourceCols):this(sourceRows,sourceCols,false){}
        internal BurstSandSimulation(int[] sourceRows,int[] sourceCols,bool runManaged)
        {
            var layout=new SandRegionFlowLayout(sourceRows,sourceCols);
            managed=runManaged;
            Rows=sourceRows;Cols=sourceCols;Width=layout.Width;height=layout.Height;InletX=layout.InletX;InletY=layout.InletY;
            rows=new NativeArray<int>(Rows,Allocator.Persistent);cols=new NativeArray<int>(Cols,Allocator.Persistent);
            cellAt=new NativeArray<int>(layout.CellAt,Allocator.Persistent);
            spawns=new NativeArray<int>(layout.Spawns,Allocator.Persistent);
            seep=new NativeArray<int>(layout.SeepOrder,Allocator.Persistent);
            state=new NativeArray<int>(10,Allocator.Persistent);
            topCell=new NativeArray<int>(layout.TopCell,Allocator.Persistent);minX=layout.MinX;maxX=layout.MaxX;
            state[9]=InletX;
            // Deterministic seed so replays and editor checks are reproducible.
            state[5]=unchecked((int)(0x9E3779B9u^(uint)(Rows.Length*7919+InletX*131+InletY)));if(state[5]==0)state[5]=1;
            Filled=new NativeArray<byte>(Rows.Length,Allocator.Persistent);
            occupied=new NativeArray<byte>(Rows.Length,Allocator.Persistent);
            Moving=new NativeArray<SandPourSimulation.Grain>(Rows.Length,Allocator.Persistent);
            SettleStep=new NativeArray<int>(Rows.Length,Allocator.Persistent);
            for(int i=0;i<Rows.Length;i++)SettleStep[i]=-1;
        }
        public bool Inside(int x,int y){return x>=0 && x<Width && y>=0 && y<height && cellAt[y*Width+x]>=0;}
        public int CellIndex(int x,int y){return x>=0 && x<Width && y>=0 && y<height?cellAt[y*Width+x]:-1;}
        // Row where the pour stream lands: first cell below the mouth that is settled sand or outside the region.
        public int LandingRow()
        {
            CompleteSteps();
            int mx=state[9];
            for(int y=MouthY;y>=0;y--)
            {
                int c=cellAt[y*Width+mx];
                if(c<0 || Filled[c]!=0) return y+1;
            }
            return 0;
        }
        public void Request(int count){CompleteSteps();if(count<Requested || count>Rows.Length)throw new ArgumentOutOfRangeException("count");state[2]=count;}
        public void ScheduleSteps(int steps)
        {
            CompleteSteps();if(steps<=0 || Complete)return;
            var job=new StepJob{TopCell=topCell,MinX=minX,MaxX=maxX,Rows=rows,Cols=cols,CellAt=cellAt,Spawns=spawns,Seep=seep,State=state,Filled=Filled,Moving=Moving,Occupied=occupied,SettleStep=SettleStep,Width=Width,Height=height,Steps=steps};
            if(managed){job.Execute();return;}
            handle=job.Schedule();pending=true;
        }
        public void CompleteSteps(){if(!pending)return;handle.Complete();pending=false;}
        public void Validate()
        {
            CompleteSteps();int filled=0,occupiedCount=0;var cells=new System.Collections.Generic.HashSet<int>();
            for(int i=0;i<Filled.Length;i++)if(Filled[i]!=0){filled++;cells.Add(i);}
            for(int i=0;i<MovingCount;i++)
            {
                var g=Moving[i];
                if(!Inside(g.X,g.Y) || !cells.Add(cellAt[g.Y*Width+g.X]))throw new InvalidOperationException("Sand grains overlap or left their region");
            }
            for(int i=0;i<occupied.Length;i++)occupiedCount+=occupied[i];
            if(filled!=Settled || Settled+MovingCount!=Emitted || occupiedCount!=Emitted || Emitted>Requested)
                throw new InvalidOperationException("Sand conservation failed");
        }
        public void Dispose()
        {
            if(disposed)return;CompleteSteps();disposed=true;
            SettleStep.Dispose();topCell.Dispose();rows.Dispose();cols.Dispose();cellAt.Dispose();spawns.Dispose();seep.Dispose();state.Dispose();Filled.Dispose();Moving.Dispose();occupied.Dispose();
        }
        // State: 0 emitted, 1 settled, 2 requested, 3 moving count, 4 burst flag, 5 rng, 6 seep cursor, 7 next grain id, 8 step count, 9 mouth column
        [BurstCompile(CompileSynchronously=true)]
        public struct StepJob : IJob
        {
            [ReadOnly] public NativeArray<int> Rows,Cols,CellAt,Spawns,Seep,TopCell;
            public int MinX,MaxX;
            public NativeArray<int> State;
            public NativeArray<byte> Filled,Occupied;
            public NativeArray<int> SettleStep;
            public NativeArray<SandPourSimulation.Grain> Moving;
            public int Width,Height,Steps;
            [BurstDiscard] static void MarkManaged(ref int enabled){enabled=0;}
            int Cell(int x,int y){return x<0 || x>=Width || y<0 || y>=Height?-1:CellAt[y*Width+x];}
            bool Free(int x,int y){int c=Cell(x,y);return c>=0 && Occupied[c]==0;}
            int NextRandom()
            {
                uint s=(uint)State[5];s^=s<<13;s^=s>>17;s^=s<<5;State[5]=(int)s;return (int)(s>>1);
            }
            void Move(ref SandPourSimulation.Grain g,int x,int y)
            {
                Occupied[CellAt[g.Y*Width+g.X]]=0;g.X=x;g.Y=y;Occupied[CellAt[y*Width+x]]=1;
            }
            public void Execute()
            {
                int burst=1;MarkManaged(ref burst);State[4]=burst;
                for(int step=0;step<Steps && State[1]<State[2];step++)
                {
                    State[8]++;
                    int write=0;
                    for(int n=0;n<State[3];n++)
                    {
                        var g=Moving[n];
                        // 1. Fall straight down with simple acceleration.
                        int fallen=0;
                        while(fallen<g.Speed && Free(g.X,g.Y-1)){Move(ref g,g.X,g.Y-1);fallen++;}
                        if(fallen>0)
                        {
                            if(fallen==g.Speed && g.Speed<MaxFallSpeed)g.Speed++;
                            Moving[write++]=g;continue;
                        }
                        g.Speed=1;
                        // Blocked by a grain that is still falling: wait instead of settling mid-air.
                        int below=Cell(g.X,g.Y-1);
                        if(below>=0 && Filled[below]==0){Moving[write++]=g;continue;}
                        // 2. Roll off a slope; keep rolling the same way once committed.
                        int side=g.Dir!=0?g.Dir:((NextRandom()&1)==0?-1:1);
                        if(Free(g.X+side,g.Y-1)){Move(ref g,g.X+side,g.Y-1);g.Dir=side;Moving[write++]=g;continue;}
                        if(Free(g.X-side,g.Y-1)){Move(ref g,g.X-side,g.Y-1);g.Dir=-side;Moving[write++]=g;continue;}
                        // 3. Spread along the surface (original sand levels out flat), never reversing, so motion terminates.
                        if(Free(g.X+side,g.Y)){Move(ref g,g.X+side,g.Y);g.Dir=side;Moving[write++]=g;continue;}
                        if(g.Dir==0 && Free(g.X-side,g.Y)){Move(ref g,g.X-side,g.Y);g.Dir=-side;Moving[write++]=g;continue;}
                        // 4. Settle.
                        int settledCell=CellAt[g.Y*Width+g.X];Filled[settledCell]=1;SettleStep[settledCell]=State[8];State[1]++;
                    }
                    State[3]=write;
                    // Pour new grains through every free mouth cell.
                    // The mouth sweeps slowly left and right along the top of the region (triangle wave), so the
                    // stream visibly rains across the area instead of drilling one spot.
                    int range=MaxX-MinX;
                    int period=range<4?1:range*28;
                    int phase=(State[8]+period/2)%(2*period); // start in the middle of the region
                    // Eased sweep: slows down and turns smoothly at both ends instead of bouncing.
                    float tri=.5f-.5f*math.cos(math.PI*phase/period);
                    int target=MinX+(int)(tri*range+.5f);
                    bool mouthBuried=true;int chosen=-1;
                    for(int d=0;d<=range && chosen<0;d++)
                        for(int sgn=-1;sgn<=1 && chosen<0;sgn+=2)
                        {
                            int x=target+d*sgn;if(x<MinX || x>MaxX)continue;
                            int cell=TopCell[x];if(cell<0 || Filled[cell]!=0)continue;
                            mouthBuried=false;
                            if(Occupied[cell]==0)chosen=cell;
                        }
                    if(chosen<0)for(int x=MinX;x<=MaxX && mouthBuried;x++){int cell=TopCell[x];if(cell>=0 && Filled[cell]==0)mouthBuried=false;}
                    if(chosen>=0){State[9]=Cols[chosen];
                        if(State[0]<State[2]){Occupied[chosen]=1;State[0]++;
                            Moving[State[3]++]=new SandPourSimulation.Grain{Index=State[7]++,X=Cols[chosen],Y=Rows[chosen],Speed=1,Dir=0};}}
                    // Mouth is buried: remaining grains settle into unreachable pockets, lowest first.
                    if(mouthBuried)
                        for(int k=0;k<SeepPerStep && State[0]<State[2];k++)
                        {
                            while(State[6]<Seep.Length && Occupied[Seep[State[6]]]!=0)State[6]++;
                            if(State[6]>=Seep.Length)break;
                            int cell=Seep[State[6]];
                            Occupied[cell]=1;Filled[cell]=1;SettleStep[cell]=State[8];State[0]++;State[1]++;State[7]++;
                        }
                }
            }
        }
    }
}
