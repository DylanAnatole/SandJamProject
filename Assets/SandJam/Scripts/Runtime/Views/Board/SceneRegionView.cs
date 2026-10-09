using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace SandJamTest.Scene3D
{
    public static class DisplayAmount
    {
        public static int Units(int raw,int divider) { return raw<=0?0:1+(raw-1)/Math.Max(1,divider); }
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
        public int FlyingCount { get { return sand==null?0:sand.MovingCount; } }
        public bool IsSettled { get { return LockMode?lockDone:sand!=null && sand.Complete; } }
        public SandBoardTextureView Board { get; private set; }
        public Texture2D FlowMask { get; private set; }
        BurstSandSimulation sand;
        int beforeStep;bool advancing;
        Color32 solid, empty;
        NativeArray<Color32> settledColors, paintedColors;
        readonly List<Vector2Int> previousMoving=new List<Vector2Int>(256);
        int lastRemaining=-1;
        bool lastOpen, lastVisible;
        float stepTime, completionGlow;
        // One grain enters per step; the original pours roughly 550-600 cells per second.
        const float StepSeconds=1f/600f;
        // Fresh-sand highlight: newly settled grains start this much lighter and fade over ~0.3 s.
        const int FreshSteps=180; const float FreshLift=.32f;
        int idleSteps, lastSettleStep; bool freshPainted;
        float counterPunch;
        public void AttachBoard(SandBoardTextureView board)
        {
            Board=board; Geometry.GetComponent<MeshRenderer>().enabled=false;
            // Labels revealed after startup must sort above the shared transparent board sprite.
            foreach (var renderer in Counter.GetComponentsInChildren<MeshRenderer>(true)) renderer.sortingOrder=10;
        }
        public void ValidateFlow() { if(sand!=null) sand.Validate(); }
        public Color32 SolidColor { get { return solid; } }
        // World point where the falling stream currently hits the sand surface.
        public bool TryGetPourImpact(out Vector3 world)
        {
            world=Vector3.zero;
            if(sand==null || sand.MovingCount==0 || !Board) return false;
            int y=sand.LandingRow();
            world=Board.transform.TransformPoint(new Vector3((sand.InletX+.5f)*SandBoardTextureView.CellSize,y*SandBoardTextureView.CellSize,0));
            return true;
        }
        void ClearMoving()
        {
            foreach(var p in previousMoving) Board.ClearGrain(p.x,p.y);
            previousMoving.Clear();
        }
        public void ResetFlow()
        {
            advancing=false; idleSteps=0; lastSettleStep=0; freshPainted=false; counterPunch=0; ClearMoving(); ReleaseColors(); if(sand!=null)sand.Dispose(); sand=null; lastRemaining=-1; stepTime=0;completionGlow=0; lockDone=false;
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
            Counter.text=done?"":DisplayAmount.Units(region.Remaining,Divider).ToString();
            if(done || !Board) return;
            // Diagonal stripes in the region colour, like the locked areas in the reference gif.
            Color baseColor=ColorPalette.Of(region.Data.ColorType);
            var original=OriginalConfig.Color(region.Data.ColorType);
            Color pale=original!=null && original.init.a>0?original.init:Color.Lerp(EmptyTint,baseColor,.4f);
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
                settledColors=new NativeArray<Color32>(sand.Filled.Length,Allocator.Persistent);
                paintedColors=new NativeArray<Color32>(sand.Filled.Length,Allocator.Persistent);
                for(int i=0;i<settledColors.Length;i++) settledColors[i]=SandShade(solid,sand.Cols[i],sand.Rows[i]);
            }
            if(region.Remaining==lastRemaining && region.Open==lastOpen && region.InformationVisible==lastVisible) return;
            // The amount label pops each time it counts down (original counter feedback).
            if(lastRemaining>0 && region.Remaining<lastRemaining && region.Remaining>0) counterPunch=1f;
            lastRemaining=region.Remaining; lastOpen=region.Open; lastVisible=region.InformationVisible;
            sand.Request((int)((long)(region.Data.amount-region.Remaining)*region.Data.rows.Length/region.Data.amount));
            // Open regions use the original pale InitColor; locked ones stay neutral.
            var original=OverrideSandColor?null:OriginalConfig.Color(region.Data.ColorType);
            empty=region.InformationVisible && original!=null && original.init.a>0?(Color32)original.init:(Color32)Color.Lerp(EmptyTint,(Color)solid,region.InformationVisible?OpenTintStrength:0f);
            Counter.gameObject.SetActive(region.InformationVisible);
            var backing=transform.Find("Amount backing"); if(backing) backing.gameObject.SetActive(region.InformationVisible);
            Counter.text=!region.InformationVisible || region.Remaining==0?"":DisplayAmount.Units(region.Remaining,Divider).ToString();
            PaintBase(); PaintMoving();
        }
        void PaintBase()
        {
            // Run avoids dispatch/wait overhead for these small regions while using Burst native code.
            new PaintRegionJob{Filled=sand.Filled,Colors=settledColors,Output=paintedColors,Empty=empty,Glow=.12f*completionGlow,
                SettleStep=sand.SettleStep,Now=sand.StepCount+idleSteps,FreshSteps=FreshSteps,FreshLift=FreshLift}.Run();
            freshPainted=sand.StepCount+idleSteps-lastSettleStep<FreshSteps;
            for(int i=0;i<sand.Filled.Length;i++) Board.SetBase(sand.Cols[i],sand.Rows[i],paintedColors[i]);
        }
        void PaintMoving()
        {
            // Restore previous pixel positions before drawing the current frame.
            ClearMoving();
            for(int i=0;i<sand.MovingCount;i++){var g=sand.Moving[i];
                if(sand.Inside(g.X,g.Y)) { Board.SetGrain(g.X,g.Y,(Color32)Color.Lerp((Color)SandShade(solid,g.X,g.Y+g.Index),Color.white,.12f)); previousMoving.Add(new Vector2Int(g.X,g.Y)); }}
        }
        // Settled sand in the original is speckled: mostly base colour with darker and lighter grains.
        public static Color32 SandShade(Color32 baseColor,int x,int y)
        {
            uint h=(uint)(x*73856093)^(uint)(y*19349663);h^=h>>13;h*=0x5bd1e995;h^=h>>15;
            int roll=(int)(h%100);
            float k=roll<22?.78f:roll<38?.89f:roll<88?1f:1.14f;
            Color c=(Color)baseColor*k;c.a=1;return c;
        }
        Vector3 counterScale; bool counterScaleKnown;
        void LateUpdate()
        {
            if(!Counter) return;
            if(!counterScaleKnown){counterScale=Counter.transform.localScale;counterScaleKnown=true;}
            if(counterPunch<=0) return;
            counterPunch=Mathf.Max(0,counterPunch-Time.deltaTime/.18f);
            Counter.transform.localScale=counterScale*(1f+.28f*Mathf.Sin(counterPunch*Mathf.PI));
        }
        void ReleaseColors(){if(settledColors.IsCreated)settledColors.Dispose();if(paintedColors.IsCreated)paintedColors.Dispose();}
        void OnDestroy(){if(sand!=null)sand.Dispose();ReleaseColors();if(FlowMask)Destroy(FlowMask);}
        [BurstCompile]
        public struct PaintRegionJob : IJob
        {
            [ReadOnly] public NativeArray<byte> Filled;
            [ReadOnly] public NativeArray<Color32> Colors;
            [ReadOnly] public NativeArray<int> SettleStep;
            [WriteOnly] public NativeArray<Color32> Output;
            public Color32 Empty;
            public float Glow, FreshLift;
            public int Now, FreshSteps;
            public void Execute()
            {
                for(int i=0;i<Output.Length;i++)
                {
                    if(Filled[i]==0){Output[i]=Empty;continue;}
                    Color32 c=Colors[i];
                    // Freshly landed grains catch the light, then fade into the pile.
                    int age=Now-SettleStep[i];
                    float fresh=age>=0 && age<FreshSteps?FreshLift*(1f-(float)age/FreshSteps):0f;
                    Output[i]=(Color32)Color.Lerp((Color)c,Color.white,Glow+fresh);
                }
            }
        }
        public void Advance(float delta){BeginAdvance(delta);FinishAdvance();}
        public void BeginAdvance(float delta)
        {
            if(sand==null || delta<=0) return;
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
            // Catch up when grains are queued behind the mouth, so the picture keeps pace with the pouring cube
            // instead of trickling on long after it emptied.
            int backlog=sand.Requested-sand.Emitted;
            if(backlog>60) steps=Mathf.RoundToInt(steps*Mathf.Min(3f,1f+(backlog-60)/240f));
            sand.ScheduleSteps(steps);advancing=true;
        }
        public void FinishAdvance()
        {
            if(!advancing)return;advancing=false;sand.CompleteSteps();
            if(beforeStep!=sand.Settled) lastSettleStep=sand.StepCount+idleSteps;
            if(beforeStep!=sand.Settled || freshPainted) PaintBase();
            PaintMoving();
            if(IsSettled && lastRemaining==0) { Counter.text="";Counter.gameObject.SetActive(false); }
        }
    }
}

