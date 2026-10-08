using System;
using System.Linq;

namespace SandJamTest.Scene3D
{
    // Static geometry for one region: cell lookup, top-center inlet, pour mouth and the
    // order used to settle pockets that falling grains can never reach (original PlaceSand(isLast)).
    public sealed class SandRegionFlowLayout
    {
        public const int MouthHalfWidth = 0; // original pours a one-pixel stream
        public readonly int[] Rows, Cols, CellAt, Spawns, SeepOrder;
        public readonly int InletIndex, Width, Height, InletX, InletY;
        public SandRegionFlowLayout(int[] rows, int[] cols)
        {
            if(rows==null || cols==null || rows.Length==0 || rows.Length!=cols.Length)
                throw new ArgumentException("Invalid sand region");
            Rows=rows;Cols=cols;Width=cols.Max()+1;Height=rows.Max()+1;
            if(rows.Min()<0 || cols.Min()<0) throw new ArgumentException("Negative sand cell");
            CellAt=Enumerable.Repeat(-1,Width*Height).ToArray();
            for(int i=0;i<rows.Length;i++)
            {
                int key=rows[i]*Width+cols[i];
                if(CellAt[key]>=0) throw new ArgumentException("Duplicate sand cell");
                CellAt[key]=i;
            }
            float middle=(cols.Min()+cols.Max())*.5f;
            InletX=cols.Distinct().OrderBy(x=>Math.Abs(x-middle)).ThenBy(x=>x).First();
            InletY=rows.Where((y,i)=>cols[i]==InletX).Max();
            InletIndex=CellAt[InletY*Width+InletX];
            // Mouth = inlet plus MouthHalfWidth cells either side on the inlet row.
            Spawns=Enumerable.Range(-MouthHalfWidth,MouthHalfWidth*2+1).OrderBy(Math.Abs)
                .Select(d=>InletX+d).Where(x=>x>=0 && x<Width)
                .Select(x=>CellAt[InletY*Width+x]).Where(i=>i>=0).ToArray();
            int inletX=InletX;
            SeepOrder=Enumerable.Range(0,rows.Length)
                .OrderBy(i=>rows[i]).ThenBy(i=>Math.Abs(cols[i]-inletX)).ThenBy(i=>cols[i]).ToArray();
        }
    }
}
