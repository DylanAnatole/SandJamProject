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
        public SceneRegionView Unlocks; // the part this padlock opens (its count waits until the padlock has gone)
        bool lockDone; int lockCells; GameObject lockIcon; PadlockView padlock;
        // Original padlock (UnlockerGif, PaintablePart "Lock"): the gold 3D lock (mesh Lock, texture Lock_Color) on the
        // locked part, its count in a white bubble at the lock's top right with dark digits. Sizes as fractions of the
        // board's width, measured on the gif: lock ~11% tall, bubble ~5.5% across, ~6% right of and ~5% above its centre.
        const float LockHeight = .13f, BubbleSize = .085f, LockTilt = -8f;
        static readonly Vector2 BubbleOffset = new Vector2(.075f, .06f);
        static readonly Color BubbleInk = new Color(.24f, .17f, .14f);
        static Material lockMaterial;
        public int SettledCount { get { return LockMode?(lockDone?lockCells:0):sand==null?0:sand.Settled; } }
        public Transform Padlock { get { return lockIcon ? lockIcon.transform : null; } }
        public bool IsSettled { get { return LockMode?lockDone && (!padlock || padlock.Burst):sand!=null && sand.Complete; } }
        public SandBoardTextureView Board { get; private set; }
        public Texture2D FlowMask { get; private set; }
        BurstSandSimulation sand;
        int beforeStep;bool advancing;
        Color32 solid, darkGrain, empty;
        NativeArray<Color32> settledColors, paintedColors;
        readonly List<Vector2Int> previousMoving=new List<Vector2Int>(256);
        int lastRemaining=-1;
        bool lastOpen, lastVisible;
        float stepTime;
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
        static readonly System.Collections.Generic.List<MeshRenderer> counterParts=new System.Collections.Generic.List<MeshRenderer>(); // reused: no array per change
        void SortCounter()
        {
            Counter.GetComponentsInChildren(true,counterParts);
            foreach (var renderer in counterParts) renderer.sortingOrder=10;
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
            advancing=false; idleSteps=0; revealGlow=0; seenHidden=false; lastSettleStep=0; freshPainted=false; ClearMoving(); ReleaseColors(); if(sand!=null)sand.Dispose(); sand=null; lastRemaining=-1; stepTime=0; lockDone=false; if(padlock){padlock.ResetClosed();lockIcon.SetActive(LockMode);}
        }
        void ApplyLock(Region region)
        {
            lockCells=region.Data.rows.Length;
            bool done=region.Remaining==0;
            if(done==lockDone && region.Remaining==lastRemaining) return;
            bool justDone=done && !lockDone, received=lastRemaining>=0 && region.Remaining<lastRemaining;
            lockDone=done; lastRemaining=region.Remaining;
            Target.position=Counter.transform.position;
            if(!lockIcon) BuildPadlock();
            // The padlock opens and hides itself and its count (PadlockView); a key cube received pops it.
            if(justDone){ if(padlock) padlock.Open(Unlocks && Unlocks.Counter ? Unlocks.Counter.transform : null); else Counter.gameObject.SetActive(false); }
            else if(!done){ Counter.gameObject.SetActive(true); if(received && padlock) padlock.Pop(); }
            var backing=Backing(); if(backing) backing.gameObject.SetActive(false);
            int shown=DisplayAmount.Units(region.Remaining,Divider);
            if(shown!=shownLock){shownLock=shown;OutlinedText.SetText(Counter,shown<0?"":DisplayAmount.Text(shown));}
            if(done || !Board) return;
            // Diagonal stripes in the region colour, like the locked areas in the reference gif.
            Color baseColor=ColorPalette.Of(region.Data.ColorType);
            var original=OriginalConfig.Color(region.Data.ColorType);
            Color pale=original!=null && original.HasInit?original.init:Color.Lerp(EmptyTint,baseColor,.4f);
            Color32 dark=(Color32)Color.Lerp(baseColor,Color.white,.15f), light=(Color32)pale;
            if(!justDone) for(int i=0;i<region.Data.rows.Length;i++){int x=region.Data.cols[i],y=region.Data.rows[i];Board.SetBase(x,y,((x+y)/3)%2==0?dark:light);}
        }
        void BuildPadlock()
        {
            var mesh=Resources.Load<Mesh>("Mechanics/Lock");
            float width=Board && Board.Renderer ? Board.Renderer.bounds.size.x : 6f;
            var centre=Counter.transform.position;
            lockIcon=new GameObject("Padlock"){layer=Counter.gameObject.layer};
            lockIcon.transform.SetParent(Counter.transform.parent,true);
            lockIcon.transform.position=centre; // it shrinks away about its centre
            padlock=lockIcon.AddComponent<PadlockView>();
            if(mesh)
            {
                if(!lockMaterial)
                {
                    lockMaterial=new Material(Shader.Find("SandJamTest/SandToonDepth")){name="Padlock gold"};
                    lockMaterial.SetTexture("_MainTex",Resources.Load<Texture2D>("Mechanics/LockColor"));
                    lockMaterial.SetFloat("_Albedo",1);lockMaterial.SetFloat("_GrainStrength",0);lockMaterial.SetFloat("_Gloss",.5f);
                    lockMaterial.SetFloat("_ShadeStrength",.75f);lockMaterial.SetFloat("_OutlineWidth",.012f);lockMaterial.SetColor("_Color",new Color(1.15f,1.1f,.95f)); // the texture reads a little dark under the toon light
                }
                var body=new GameObject("Padlock mesh"){layer=lockIcon.layer};
                body.transform.SetParent(lockIcon.transform,false);
                body.AddComponent<MeshFilter>().sharedMesh=mesh;
                var bodyRenderer=body.AddComponent<MeshRenderer>(); bodyRenderer.sharedMaterial=lockMaterial; bodyRenderer.NoShadows();
                padlock.Body=bodyRenderer; padlock.Closed=mesh; BlendShapeCache.Prewarm(mesh); padlock.Gold=lockMaterial.GetColor("_Color");
                float k=width*LockHeight/Mathf.Max(.01f,mesh.bounds.size.y);
                body.transform.rotation=Quaternion.Euler(0,0,LockTilt);
                body.transform.localScale=Vector3.one*k/Mathf.Max(.0001f,lockIcon.transform.lossyScale.y);
                body.transform.position=centre-body.transform.rotation*(mesh.bounds.center*k)+new Vector3(0,0,-.6f);
            }
            // Count bubble: a white disc behind the counter, which turns dark with a white edge.
            var bubble=new GameObject("Padlock count bubble"){layer=lockIcon.layer}.AddComponent<SpriteRenderer>();
            bubble.transform.SetParent(lockIcon.transform,true);
            bubble.sprite=SpriteFactory.Disc; bubble.sortingOrder=10;
            var at=centre+new Vector3(BubbleOffset.x*width,BubbleOffset.y*width,-.7f);
            bubble.transform.position=at;
            float d=width*BubbleSize/Mathf.Max(.0001f,bubble.sprite.bounds.size.x);
            bubble.transform.localScale=Vector3.one*d/Mathf.Max(.0001f,bubble.transform.parent.lossyScale.x);
            Counter.transform.position=new Vector3(at.x,at.y,at.z-.05f);

            Counter.color=BubbleInk;
            var outline=Counter.GetComponent<SandJamTest.Scene3D.OutlinedText>(); if(outline) outline.SetInk(Color.white);
            padlock.Counter=Counter.transform; padlock.Init();
        }
        // The counter's backing plate, found once (a name lookup every frame for every locked region).
        Transform backingPlate; bool backingSearched; int shownLock=-2;
        Transform Backing() { if(!backingSearched){backingSearched=true;backingPlate=transform.Find("Amount backing");} return backingPlate; }
        public void Apply(Region region)
        {
            if(LockMode){ApplyLock(region);return;}
            // A locked part leaves its pixels to its padlock's stripes until the padlock opens it.
            if(region.Data.IsUnlockedPart && !region.Open)
            {
                Counter.gameObject.SetActive(false);
                var hidden=Backing(); if(hidden) hidden.gameObject.SetActive(false);
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
            var backing=Backing(); if(backing) backing.gameObject.SetActive(region.InformationVisible);
            OutlinedText.SetText(Counter,!region.InformationVisible || region.Remaining==0?"":DisplayAmount.Text(DisplayAmount.Units(region.Remaining,Divider)));
            PaintBase(); PaintMoving();
        }
        Color32 RevealedEmpty() { return (Color32)Color.Lerp((Color)openEmpty,RevealWash,revealGlow*RevealStrength); }
        void PaintBase()
        {
            // Run avoids dispatch/wait overhead for these small regions while using Burst native code.
            // The Burst job also writes the colours straight into the board texture (no per-cell C# loop).
            if(!boardIndex.IsCreated){boardIndex=new NativeArray<int>(sand.Filled.Length,Allocator.Persistent);for(int i=0;i<boardIndex.Length;i++)boardIndex[i]=sand.Rows[i]*Board.Width+sand.Cols[i];}
            new PaintRegionJob{Filled=sand.Filled,Colors=settledColors,Output=paintedColors,Empty=empty,
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
            // TryGetComponent: a missing component costs nothing (GetComponent built a null-error message every frame in
            // the editor, ~0.6 KB per counter).
            if(!counterOutline && Counter.TryGetComponent(out counterOutline))counterOutline.SetThickness(CounterOutlineThickness);
        }
        NativeArray<int> boardIndex;
        void ReleaseColors(){if(settledColors.IsCreated)settledColors.Dispose();if(paintedColors.IsCreated)paintedColors.Dispose();if(boardIndex.IsCreated)boardIndex.Dispose();}
        void OnDestroy(){if(sand!=null)sand.Dispose();ReleaseColors();if(FlowMask)Destroy(FlowMask);}
        // Compiled before first use in the editor (like the sand step job): asynchronously it kept running the previous
        // build after a field change, reading the new fields at the old offsets.
        [BurstCompile(CompileSynchronously=true)]
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
            public float FreshLift;
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
                        c=(Color32)Color.Lerp((Color)Colors[i],Color.white,fresh);
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
                // A finished region keeps its plain colour (recording 2, 55.5-62 s: grey stays 134); only its count goes.
                if(lastRemaining==0 && Counter.gameObject.activeSelf)
                {
                    Counter.gameObject.SetActive(false);
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
            if(IsSettled && lastRemaining==0 && Counter.gameObject.activeSelf) { Counter.text="";Counter.gameObject.SetActive(false); }
        }
    }
}

