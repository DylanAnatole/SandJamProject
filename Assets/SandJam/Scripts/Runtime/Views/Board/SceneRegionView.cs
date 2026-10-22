using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace SandJamTest.Scene3D
{
    public static class DisplayAmount
    {
        public static int Units(int raw,int divider) { return raw<=0?0:1+(raw-1)/Math.Max(1,divider); }
        // Counters change many times a second while sand pours: the digits come from a cached table instead of a
        // new string per change (zero GC in the gameplay loop).
        static readonly string[] texts=new string[1001];
        public static string Text(int value)
        {
            if(value<0 || value>=texts.Length) return value.ToString();
            return texts[value] ?? (texts[value]=value.ToString());
        }
    }
    public sealed class SceneRegionView : MonoBehaviour
    {
        public int PartIndex;
        public Color EmptyTint = new Color(.88f,.93f,.95f);
        public float OpenTintStrength = .32f;
        public bool OverrideSandColor;
        public Color SandColor = Color.white;
        public MeshFilter Geometry; // Serialized geometry retained as an editor reference only.
        public TextMesh Counter;
        public Transform Target;
        public int Divider=40;
        // Padlock (unlocker part): receives key-cube sand as a counter only. While locked it draws its
        // colour as diagonal stripes with a lock icon (original UnlockerGif); it never pours visible sand.
        public bool LockMode;
        bool lockDone; int lockCells; SpriteRenderer lockIcon;
        public int SettledCount { get { return LockMode?(lockDone?lockCells:0):sand==null?0:sand.Settled; } }
        public bool IsSettled { get { return LockMode?lockDone:sand!=null && sand.Complete; } }
        public SandBoardTextureView Board { get; private set; }
        public Texture2D FlowMask { get; private set; }
        BurstSandSimulation sand;
        int beforeStep;bool advancing;
        Color32 solid, darkGrain, empty;
        NativeArray<Color32> settledColors, paintedColors;
        readonly List<Vector2Int> previousMoving=new List<Vector2Int>(256);
        int lastRemaining=-1;
        bool lastOpen, lastVisible;
        float stepTime, completionGlow;
        // One grain enters per step; the original pours roughly 550-600 cells per second.
        const float StepSeconds=1f/320f; // calm, continuous trickle (one grain enters per step)
        // Fresh-sand sheen: newly settled grains start this much lighter and fade over ~0.4 s. The original has
        // none (recording 2, 58 s: grains land in their final colour), so it stays a faint glint on the surface.
        const int FreshSteps=120; const float FreshLift=.1f;
        int idleSteps, lastSettleStep; bool freshPainted;
        // Region opening mid-level (recording 2, 153.1-153.5 s): it lights up white-cyan (~0.3-0.4 of the way to white)
        // and fades into its colour.
        const float RevealSeconds=.4f, RevealStrength=.4f; static readonly Color RevealWash=new Color(.88f,.97f,1f);
        float revealGlow; bool seenHidden; Color32 openEmpty;
        public void AttachBoard(SandBoardTextureView board)
        {
            Board=board; Geometry.GetComponent<MeshRenderer>().enabled=false;
            SortCounter();
        }
        // Counters sort above the shared transparent board sprite, the digits one order above their dark outline
        // copies: on the tilted board, depth alone let the copies of counters near the top draw over the white
        // digits (they read as black numbers). Re-applied when a counter first shows, since its outline copies are
        // created then.
        void SortCounter()
        {
            foreach (var renderer in Counter.GetComponentsInChildren<MeshRenderer>(true)) renderer.sortingOrder=10;
            Counter.GetComponent<MeshRenderer>().sortingOrder=11;
        }
        public Color32 SolidColor { get { return solid; } }
        void ClearMoving()
        {
            foreach(var p in previousMoving) Board.ClearGrain(p.x,p.y);
            previousMoving.Clear();
        }
        public void ResetFlow()
        {
            advancing=false; idleSteps=0; revealGlow=0; seenHidden=false; lastSettleStep=0; freshPainted=false; ClearMoving(); ReleaseColors(); if(sand!=null)sand.Dispose(); sand=null; lastRemaining=-1; stepTime=0;completionGlow=0; lockDone=false;
        }
        void ApplyLock(Region region)
        {
            lockCells=region.Data.rows.Length;
            bool done=region.Remaining==0;
            if(done==lockDone && region.Remaining==lastRemaining) return;
            bool justDone=done && !lockDone;
            lockDone=done; lastRemaining=region.Remaining;
            Target.position=Counter.transform.position;
            if(!lockIcon)
            {
                var obj=new GameObject("Padlock icon"){layer=Counter.gameObject.layer};
                obj.transform.SetParent(Counter.transform.parent,false);
                obj.transform.position=Counter.transform.position+new Vector3(-.32f,.02f,-.01f);
                lockIcon=obj.AddComponent<SpriteRenderer>(); lockIcon.sprite=Resources.Load<Sprite>("VideoUI/icon-lock"); lockIcon.sortingOrder=11;
                if(lockIcon.sprite){float s=.42f/lockIcon.sprite.bounds.size.y;obj.transform.localScale=Vector3.one*s/Mathf.Max(.0001f,obj.transform.parent.lossyScale.y);}
            }
            lockIcon.gameObject.SetActive(!done);
            Counter.gameObject.SetActive(!done);
            var backing=transform.Find("Amount backing"); if(backing) backing.gameObject.SetActive(false);
            Counter.text=done?"":DisplayAmount.Text(DisplayAmount.Units(region.Remaining,Divider));
            if(done || !Board) return;
            // Diagonal stripes in the region colour, like the locked areas in the reference gif.
            Color baseColor=ColorPalette.Of(region.Data.ColorType);
            var original=OriginalConfig.Color(region.Data.ColorType);
            Color pale=original!=null && original.HasInit?original.init:Color.Lerp(EmptyTint,baseColor,.4f);
            Color32 dark=(Color32)Color.Lerp(baseColor,Color.white,.15f), light=(Color32)pale;
            if(!justDone) for(int i=0;i<region.Data.rows.Length;i++){int x=region.Data.cols[i],y=region.Data.rows[i];Board.SetBase(x,y,((x+y)/3)%2==0?dark:light);}
        }
        public void Apply(Region region)
        {
            if(LockMode){ApplyLock(region);return;}
            // A locked part leaves its pixels to its padlock's stripes until the padlock opens it.
            if(region.Data.IsUnlockedPart && !region.Open)
            {
                Counter.gameObject.SetActive(false);
                var hidden=transform.Find("Amount backing"); if(hidden) hidden.gameObject.SetActive(false);
                lastOpen=false; lastRemaining=-1; return;
            }
            if(sand==null)
            {
                sand=new BurstSandSimulation(region.Data.rows,region.Data.cols);
                Target.position=Board.transform.TransformPoint(new Vector3((sand.InletX+.5f)*SandBoardTextureView.CellSize,(sand.InletY+.5f)*SandBoardTextureView.CellSize,0));
                if(!FlowMask){
                    FlowMask=new Texture2D(Board.Width,Board.Height,TextureFormat.RGBA32,false){name="Region flow mask "+PartIndex,filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
                    var maskPixels=new Color32[Board.Width*Board.Height];
                    for(int i=0;i<sand.Rows.Length;i++)maskPixels[sand.Rows[i]*Board.Width+sand.Cols[i]]=new Color32(255,255,255,255);
                    FlowMask.SetPixels32(maskPixels);FlowMask.Apply(false,true);
                }
                solid=OverrideSandColor?SandColor:ColorPalette.Of(region.Data.ColorType);
                darkGrain=DarkGrain(solid,OverrideSandColor?null:OriginalConfig.Color(region.Data.ColorType));
                settledColors=new NativeArray<Color32>(sand.Filled.Length,Allocator.Persistent);
                paintedColors=new NativeArray<Color32>(sand.Filled.Length,Allocator.Persistent);
                for(int i=0;i<settledColors.Length;i++) settledColors[i]=SandShade(solid,darkGrain,sand.Cols[i],sand.Rows[i]);
            }
            if(region.Remaining==lastRemaining && region.Open==lastOpen && region.InformationVisible==lastVisible) return;
            lastRemaining=region.Remaining; lastOpen=region.Open; lastVisible=region.InformationVisible;
            sand.Request((int)((long)(region.Data.amount-region.Remaining)*region.Data.rows.Length/region.Data.amount));
            // Open regions use the original pale InitColor; locked ones stay neutral.
            var original=OverrideSandColor?null:OriginalConfig.Color(region.Data.ColorType);
            openEmpty=region.InformationVisible && original!=null && original.HasInit?(Color32)original.init:(Color32)Color.Lerp(EmptyTint,(Color)solid,region.InformationVisible?OpenTintStrength:0f);
            if(!region.InformationVisible) seenHidden=true;
            else if(seenHidden){ seenHidden=false; revealGlow=1; }
            empty=RevealedEmpty();
            Counter.gameObject.SetActive(region.InformationVisible);
            if(region.InformationVisible) SortCounter();
            var backing=transform.Find("Amount backing"); if(backing) backing.gameObject.SetActive(region.InformationVisible);
            Counter.text=!region.InformationVisible || region.Remaining==0?"":DisplayAmount.Text(DisplayAmount.Units(region.Remaining,Divider));
            PaintBase(); PaintMoving();
        }
        Color32 RevealedEmpty() { return (Color32)Color.Lerp((Color)openEmpty,RevealWash,revealGlow*RevealStrength); }
        void PaintBase()
        {
            // Run avoids dispatch/wait overhead for these small regions while using Burst native code.
            // The Burst job also writes the colours straight into the board texture (no per-cell C# loop).
            if(!boardIndex.IsCreated){boardIndex=new NativeArray<int>(sand.Filled.Length,Allocator.Persistent);for(int i=0;i<boardIndex.Length;i++)boardIndex[i]=sand.Rows[i]*Board.Width+sand.Cols[i];}
            new PaintRegionJob{Filled=sand.Filled,Colors=settledColors,Output=paintedColors,Empty=empty,Glow=.12f*completionGlow,
                SettleStep=sand.SettleStep,Now=sand.StepCount+idleSteps,FreshSteps=FreshSteps,FreshLift=FreshLift,
                BoardIndex=boardIndex,BoardBackground=Board.BackgroundPixels,BoardPixels=Board.Pixels}.Run();
            Board.MarkDirty();
            freshPainted=sand.StepCount+idleSteps-lastSettleStep<FreshSteps;
        }
        void PaintMoving()
        {
            // Restore previous pixel positions before drawing the current frame.
            ClearMoving();
            // While the region is being fed, draw one unbroken thread from the mouth down to the pile, so supply
            // pauses between shots never read as a dotted, broken stream.
            if(sand.MovingCount>0 || sand.Requested>sand.Emitted)
            {
                int mx=sand.MouthX,land=sand.LandingRow();
                // Original thread (recording 2, 58.4 s): a 1-px line in the plain sand colour.
                for(int y=sand.MouthY;y>=land;y--)
                {
                    if(!sand.Inside(mx,y) || IsFilledCell(mx,y)) break;
                    Board.SetGrain(mx,y,solid);
                    previousMoving.Add(new Vector2Int(mx,y));
                }
            }
            for(int i=0;i<sand.MovingCount;i++){var g=sand.Moving[i];
                if(!sand.Inside(g.X,g.Y)) continue;
                var c=SandShade(solid,darkGrain,g.X,g.Y+g.Index);
                Board.SetGrain(g.X,g.Y,c); previousMoving.Add(new Vector2Int(g.X,g.Y));
                // Motion trail: fill the cells a fast grain skipped this step, so the stream reads as one
                // unbroken thread instead of a dotted line.
                for(int k=1;k<g.Speed+1;k++){int y=g.Y+k; if(!sand.Inside(g.X,y) || IsFilledCell(g.X,y)) break;
                    Board.SetGrain(g.X,y,c); previousMoving.Add(new Vector2Int(g.X,y));}}
        }
        bool IsFilledCell(int x,int y){int i=sand.CellIndex(x,y);return i>=0 && sand.Filled[i]!=0;}
        // Settled sand in the original (recording 2, 58-61 s) is the base colour with about one grain in five in the
        // colour's own dark shade (original config "dark": yellow 214,172,0, purple 78,0,166, grey 125 - measured
        // 208,176,8 / 72,0,160 / 120); no lighter grains. Colours without one use x.85.
        const int DarkGrainPercent=20; const float DarkGrainShade=.85f;
        static Color32 DarkGrain(Color32 baseColor,OriginalConfig.ColorEntry original)
        {
            Color c=original!=null && original.dark.maxColorComponent>0?original.dark:(Color)baseColor*DarkGrainShade;c.a=1;return c;
        }
        public static Color32 SandShade(Color32 baseColor,Color32 dark,int x,int y)
        {
            uint h=(uint)(x*73856093)^(uint)(y*19349663);h^=h>>13;h*=0x5bd1e995;h^=h>>15;
            return (int)(h%100)<DarkGrainPercent?dark:baseColor;
        }
        // Board counters in the original (recording 2, 158 s, zoomed): digits ~25 px tall with a thin dark edge. They
        // count down without popping (152.7-153.0 s: same height every frame).
        const float CounterScale=.77f, CounterOutlineThickness=.045f;
        bool counterScaleKnown; OutlinedText counterOutline;
        void LateUpdate()
        {
            if(!Counter) return;
            if(!counterScaleKnown){Counter.transform.localScale*=CounterScale;counterScaleKnown=true;}
            if(!counterOutline){counterOutline=Counter.GetComponent<OutlinedText>();if(counterOutline)counterOutline.SetThickness(CounterOutlineThickness);}
        }
        NativeArray<int> boardIndex;
        void ReleaseColors(){if(settledColors.IsCreated)settledColors.Dispose();if(paintedColors.IsCreated)paintedColors.Dispose();if(boardIndex.IsCreated)boardIndex.Dispose();}
        void OnDestroy(){if(sand!=null)sand.Dispose();ReleaseColors();if(FlowMask)Destroy(FlowMask);}
        [BurstCompile]
        public struct PaintRegionJob : IJob
        {
            [ReadOnly] public NativeArray<byte> Filled;
            [ReadOnly] public NativeArray<Color32> Colors;
            [ReadOnly] public NativeArray<int> SettleStep;
            [WriteOnly] public NativeArray<Color32> Output;
            // Optional: cell -> board pixel index and the board's arrays (zero-length = skip).
            [ReadOnly] public NativeArray<int> BoardIndex;
            [NativeDisableContainerSafetyRestriction] public NativeArray<Color32> BoardBackground, BoardPixels;
            public Color32 Empty;
            public float Glow, FreshLift;
            public int Now, FreshSteps;
            public void Execute()
            {
                bool board=BoardIndex.Length==Output.Length;
                for(int i=0;i<Output.Length;i++)
                {
                    Color32 c;
                    if(Filled[i]==0)c=Empty;
                    else
                    {
                        // Freshly landed grains catch the light, then fade into the pile.
                        int age=Now-SettleStep[i];
                        float fresh=age>=0 && age<FreshSteps?FreshLift*(1f-(float)age/FreshSteps):0f;
                        c=(Color32)Color.Lerp((Color)Colors[i],Color.white,Glow+fresh);
                    }
                    Output[i]=c;
                    if(board){int p=BoardIndex[i];BoardBackground[p]=c;BoardPixels[p]=c;}
                }
            }
        }
        public void Advance(float delta){BeginAdvance(delta);FinishAdvance();}
        public void BeginAdvance(float delta)
        {
            if(sand==null || delta<=0) return;
            if(revealGlow>0){ revealGlow=Mathf.Max(0,revealGlow-delta/RevealSeconds); empty=RevealedEmpty(); PaintBase(); }
            if(IsSettled)
            {
                if(freshPainted){idleSteps+=Mathf.Max(1,Mathf.RoundToInt(delta/StepSeconds));PaintBase();}
                if(lastRemaining==0 && completionGlow<1)
                {
                    completionGlow=Mathf.MoveTowards(completionGlow,1,delta/.35f);
                    PaintBase(); Counter.gameObject.SetActive(false);
                    var backing=transform.Find("Amount backing");if(backing)backing.gameObject.SetActive(false);
                }
                return;
            }
            beforeStep=sand.Settled;
            stepTime+=delta;
            int steps=0;while(stepTime>=StepSeconds){stepTime-=StepSeconds;steps++;}
            // Only a gentle catch-up when many grains queue at the mouth: the stream should stay one calm, unbroken
            // trickle rather than surging.
            int backlog=sand.Requested-sand.Emitted;
            // One grain enters per step, so several cubes pouring into the same region can request sand faster
            // than it falls. Speed the simulation up with the waiting sand so the picture keeps pace with the cubes
            // (~0.3 s behind at most) and the last grain lands about when the last cube walks off.
            if(backlog>40) steps=Mathf.CeilToInt(steps*Mathf.Min(5f,1f+(backlog-40)/160f));
            sand.ScheduleSteps(steps);advancing=true;
        }
        public void FinishAdvance()
        {
            if(!advancing)return;advancing=false;sand.CompleteSteps();
            if(beforeStep!=sand.Settled) lastSettleStep=sand.StepCount+idleSteps;
            // Pellets aim at the current pour column.
            if(sand.MovingCount>0) Target.position=Board.transform.TransformPoint(new Vector3((sand.MouthX+.5f)*SandBoardTextureView.CellSize,(sand.MouthY+.5f)*SandBoardTextureView.CellSize,0));
            if(beforeStep!=sand.Settled || freshPainted) PaintBase();
            PaintMoving();
            if(IsSettled && lastRemaining==0) { Counter.text="";Counter.gameObject.SetActive(false); }
        }
    }
}

