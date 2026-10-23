using System.Collections;
using System.Linq;
using UnityEngine;

namespace SandJamTest.Scene3D
{
    [DefaultExecutionOrder(-100)]
    public sealed class VideoScreen : MonoBehaviour
    {
        // UI units per pixel of the 1920 px tall recordings the screens are measured against (ortho size 5.375).
        public const float Px=10.75f/1920;
        public SandJamSceneController Controller;
        public LevelManager Levels;
        public Camera UiCamera;
        public TextMesh Status;
        public SpriteRenderer EditorPreview;
        public ReferenceUiPlaceholder[] Placeholders;
        public GameObject Loading,Home,Gameplay,Celebration,Result;
        public Transform LoadingFill,Mascot;
        public GameObject FifthSlotLock;
        public TextMesh FifthSlotRemaining;
        public BoosterController Boosters { get; private set; }
        public GameplayFeedbackController Feedback { get; private set; }
        public CampaignUi Ui { get; private set; }
        public PopupScreens Popups { get; private set; }
        public enum Page { Loading,Home,Gameplay,Celebration,Result }
        public Page Current { get; private set; }
        float elapsed, winPresentationTime, loadingTime=1.6f; Vector3 fillScale,mascotPosition;
        int transitionFrame,viewWidth,viewHeight;
        void Awake()
        {
            if(EditorPreview)EditorPreview.enabled=false;
            SandBoardTextureView.ObstacleTint=SandBoardTextureView.GameOutline;
            fillScale=LoadingFill.localScale;mascotPosition=Mascot.localPosition;
            // Original "Superb!" (recording 2, 228 s): the finished picture only faintly shows through a deep purple layer
            // (#1E1936 at ~94%, solved from the picture and background colours under it); the gear and level pill vanish.
            var superbBackdrop=Celebration.transform.Find("Dark backdrop");
            if(superbBackdrop){var r=superbBackdrop.GetComponent<SpriteRenderer>();if(r){r.color=PopupScreens.DeepDim;r.sortingOrder=14;}} // above the gameplay HUD (booster badges), below the Superb! effects
            var superbTitle=Celebration.GetComponentsInChildren<TextMesh>(true).FirstOrDefault(t=>t.text=="Superb!" && t.name!="Text outline");
            if(superbTitle){var fx=Celebration.AddComponent<SuperbFx>();fx.Font=UiFont.Font?UiFont.Font:Controller.InterfaceFont;fx.Build(superbTitle);}
            StyleHome();
            FitGameplayHud();
            var devLabel=Loading.transform.Find("UI reconstruction");if(devLabel)devLabel.gameObject.SetActive(false);
            Result.AddComponent<ResultFx>().Build();
            gameObject.AddComponent<TextOutliner>();
            if(Status)Status.gameObject.SetActive(false); // the original shows no status text under the board
            Show(Page.Loading);
            Boosters = gameObject.AddComponent<BoosterController>(); Boosters.Initialize(this);
            Feedback=gameObject.AddComponent<GameplayFeedbackController>();Feedback.Initialize(this);
            if(!Levels)Debug.LogError("VideoScreen needs its LevelManager (Levels).",this);
            Ui=gameObject.AddComponent<CampaignUi>();Ui.Initialize(this);Popups=gameObject.AddComponent<PopupScreens>();Popups.Initialize(this);
            UiFont.ApplyAll();
        }

        // Home closer to the original recording (level 267): brighter level sign and stage, and a soft spotlight
        // falling from the walkway onto the PLAY button.
        // Walkway crossing (local units of the Home page), edge to edge. The original crosses the visible screen in
        // ~2.1 s (~2.25 units/s) in a quick trot: a 0.46 s step cycle (the walk clip lasts 0.83 s). The whole
        // character turns toward where it walks (MascotYaw, degrees from facing the camera), so the walk clip plays
        // in its natural profile; the cube reads the same from the side.
        const float MascotEdgeX=3.3f, MascotWalk=3f, MascotLap=3.7f, MascotEnter=1f, MascotYaw=70f, MascotAnimSpeed=1.8f;
        int mascotLap=-1,mascotPasses; float mascotDirection=1;
        static readonly Color[] MascotColors={new Color(0f,.64f,1f),new Color(1f,.8f,0f),new Color(.75f,.09f,.09f),new Color(.83f,0f,.78f),
            new Color(.11f,.79f,0f),new Color(1f,.53f,0f),new Color(.44f,0f,.93f)};
        // New pass: random direction (+1 = from the right edge) and a colour different from the last one.
        int mascotColor=-1;
        void StartMascotPass()
        {
            var mascotAnimator=Mascot.GetComponentInChildren<Animator>();if(mascotAnimator)mascotAnimator.speed=MascotAnimSpeed;
            // Passes alternate: left to right, then right to left, and so on (direction 1 walks right to left).
            mascotDirection=mascotPasses++%2==0?-1:1;
            int next=UnityEngine.Random.Range(0,MascotColors.Length-1);if(next>=mascotColor)next++;
            if(mascotColor<0)next=0; // the first one is blue, as in the recording
            mascotColor=next;
            foreach(var r in Mascot.GetComponentsInChildren<Renderer>(true))
                foreach(var m in r.materials)if(m.HasProperty("_Color"))m.color=MascotColors[next];
        }
        // Original splash between levels (recording, 259 -> 260): only the big SAND JAM logo high on the purple
        // background and the loading bar near the bottom.
        void QuickSplash()
        {
            foreach(var name in new[]{"image-1-splashScene","Voodoo","Entertain the world","logo","Loading hint"}){var t=Loading.transform.Find(name);if(t)t.gameObject.SetActive(false);}
            var header=Loading.transform.Find("header-splashScene");
            if(header){header.localScale*=1.34f;header.localPosition=new Vector3(-.32f,2.38f,header.localPosition.z);}
            var label=Loading.transform.Find("Loading...");if(label)label.localScale*=1.5f;
            foreach(var name in new[]{"bg-fillbar-splashScene","Loading fill","Loading..."}){var t=Loading.transform.Find(name);if(t)t.localPosition+=new Vector3(0,-1.0f,0);}
            fillScale=LoadingFill.localScale;
        }
        void StyleHome()
        {
            // Colours measured on the original Home (recording, level 267). The sign's blue panel and inset are tinted
            // below, once they use plain sprites.
            Tint(Home.transform.Find("Lower stage"),new Color32(34,82,118,255));
            Tint(Home.transform.Find("Level sign rim"),new Color32(158,206,242,255));
            Tint(Home.transform.Find("Level sign shadow"),new Color32(13,71,116,255));
            Tint(Home.transform.Find("Top bar"),new Color32(54,112,157,255));
            Tint(Home.transform.Find("Walkway"),new Color32(120,182,228,255));
            Tint(Home.transform.Find("Join label"),new Color32(200,146,36,255));
            foreach(Transform child in Home.transform)
            {
                if(child.name.StartsWith("Hanger")){Tint(child,new Color32(122,186,235,255));var hs=child.localScale;child.localScale=new Vector3(hs.x*1.5f,hs.y,hs.z);}
                if(child.name=="Paving joint")Tint(child,new Color32(92,140,182,255));
            }
            // The sign panels go through a shader that darkens them ~30%; plain sprite shading gives the true colours.
            // The panel sprite is shaded (not white), which dulled the measured colours; the sign uses a plain white
            // rounded panel instead so the tints land on the original's colours.
            var plain=SpriteFactory.PlainSprite;
            foreach(var name in new[]{"Level sign blue","Sign inset","Level sign rim","Level sign shadow","Join label"})
            {
                var t=Home.transform.Find(name);var r=t?t.GetComponent<SpriteRenderer>():null;if(!r)continue;
                var rb=r.bounds;r.sharedMaterial=plain;r.sprite=SpriteFactory.Rounded(64,22,130);r.drawMode=SpriteDrawMode.Sliced;
                var ps=t.parent.lossyScale;t.localScale=Vector3.one;r.size=new Vector2(rb.size.x/ps.x,rb.size.y/ps.y);
            }
            Tint(Home.transform.Find("Level sign blue"),new Color32(71,141,214,255));
            Tint(Home.transform.Find("Sign inset"),new Color32(93,169,228,255));
            // The original sign is about 12% larger; it grows around its own centre.
            var signParts=new[]{"Level sign shadow","Level sign rim","Level sign blue","Sign inset","147","LEVEL"};
            var rim=Home.transform.Find("Level sign rim");
            if(rim)
            {
                var centre=rim.position;
                foreach(var name in signParts)
                {
                    var t=Home.transform.Find(name);if(!t)continue;
                    t.position=centre+(t.position-centre)*1.12f;t.localScale*=1.12f;
                }
            }
            // Full-width bands are square-cornered and reach past both screen edges (the rounded panels left dark
            // corners and side strips on phones); the top bar is as thin as the original's.
            var square=SpriteFactory.White;
            foreach(var name in new[]{"Top bar","Walkway","Lower stage","Walkway edge"})
            {
                var r=Home.transform.Find(name)?Home.transform.Find(name).GetComponent<SpriteRenderer>():null;if(!r)continue;
                var rb=r.bounds;r.sprite=square;r.drawMode=SpriteDrawMode.Simple;
                var rps=r.transform.parent.lossyScale;r.transform.localScale=new Vector3(6.4f/rps.x,rb.size.y/rps.y,1);
                r.transform.position=new Vector3(0,rb.center.y,r.transform.position.z);
            }
            var bar=Home.transform.Find("Top bar")?Home.transform.Find("Top bar").GetComponent<SpriteRenderer>():null;
            // Reaches well past the top edge: tall phones (19.5:9) see above the design box.
            if(bar){float barBottom=4.2f,barTop=8f;var bps=bar.transform.parent.lossyScale;bar.transform.localScale=new Vector3(bar.transform.localScale.x,(barTop-barBottom)/bps.y,1);bar.transform.position=new Vector3(0,(barTop+barBottom)*.5f,bar.transform.position.z);
                // Original (measured, 1920 px tall): a darker band over the phone's status area (top 107 px, #2C6791)
                // and a thin dark line along the bar's lower edge (~4 px, #2C648F). Shop / Gallery copy both.
                BarStrip(bar,"Top bar status band",4.78f,barTop,new Color32(44,103,145,255));
                BarStrip(bar,"Top bar edge",barBottom,barBottom+.025f,new Color32(44,100,143,255));
                // The settings button and the lives / coin counters sit higher on the original's bar (~0.11 units).
                foreach(Transform child in Home.transform)
                    if(!child.name.StartsWith("Top bar") && System.Array.IndexOf(PopupScreens.TopBarParts,child.name)>=0)child.position+=Vector3.up*.11f;}
            foreach(var name in new[]{"generalBackground-main","bg-navbar"}){var t=Home.transform.Find(name);if(t){var ls=t.localScale;t.localScale=new Vector3(ls.x*1.12f,ls.y,ls.z);}}
            // Hangers run from the top bar down to the sign.
            var rimNow=Home.transform.Find("Level sign rim");
            if(rimNow && bar)FitHangers(bar,rimNow.GetComponent<Renderer>().bounds.max.y-.02f,false);
            ReplaceLevelSign(bar);
            ReplaceJoinPlaque();
            // Sand Quest chest and plaque (recording 1, 1334.6 s at level 267): 165 px wide at x 680-845, y 670-847;
            // ours was 5% smaller and 11 px left, 10 px low. The plaque's JOIN text moves with it.
            NavPlace("Chest_Enable",ChestGrow,ChestShiftPx.x,ChestShiftPx.y-5);NavPlace("Join label",ChestGrow,ChestShiftPx.x,ChestShiftPx.y);
            foreach(var t in Home.GetComponentsInChildren<TextMesh>(true))if(t.text=="JOIN")Place(t.transform,ChestGrow*JoinTextScale,ChestShiftPx.x,ChestShiftPx.y);
            // Navbar, measured on the original Home (recording 2, 888x1920): the bar sits 19 px lower, the selected tab
            // rises 12 px higher above it, the icons are lower (shop and home bigger, gallery smaller) and so are the captions.
            NavPlace("bg-navbar",1,0,19);NavPlace("bg-navBar-seleckted",1,0,-12);
            NavPlace("icon-shop",1.15f*1.1f,-6.5f,17.5f);NavPlace("icon-home",1.15f*1.12f,0,16);NavPlace("icon-cards",1.15f*.87f,10,19);
            foreach(var name in new[]{"SHOP","HOME","GALLERY"})NavPlace(name,1.22f,0,9);
            // Walkway as a 3D ledge (original Home, measured): a shaded back edge, two rows of tiles split by a joint,
            // joints leaning outward in perspective and a front lip falling into the shadow of the stage below. It is
            // drawn as one texture, replacing the flat band, its thin edge line and the separate joint sprites.
            var walkway=Home.transform.Find("Walkway");var lowerStage=Home.transform.Find("Lower stage");
            var walkwayEdge=Home.transform.Find("Walkway edge");
            float floorTop=lowerStage?lowerStage.GetComponent<Renderer>().bounds.max.y:-.98f,floorBottom=-1.8f;
            if(walkway)
            {
                var wr=walkway.GetComponent<SpriteRenderer>();
                if(walkwayEdge)floorBottom=walkwayEdge.GetComponent<Renderer>().bounds.min.y;
                // Recording 2 (244 s): the walkway's back edge is 32 px lower and its front lip 14 px lower; the stage
                // below loses the same top strip so the sky shows down to the walkway.
                floorTop-=WalkwayTopDropPx*Px;floorBottom-=WalkwayBottomDropPx*Px;
                // The cube walking on it is smaller and lower too (recording 2, 244 s: ~95 px wide, feet at ~1245 px).
                if(Mascot){Mascot.localScale*=MascotScale;Mascot.position+=Vector3.down*MascotDropPx*Px;mascotPosition=Mascot.localPosition;}
                if(lowerStage){var lr=lowerStage.GetComponent<Renderer>();var lb=lr.bounds;float k=(floorTop-lb.min.y)/lb.size.y;
                    var ls=lowerStage.localScale;lowerStage.localScale=new Vector3(ls.x,ls.y*k,ls.z);lowerStage.position+=Vector3.up*(lb.min.y-lr.bounds.min.y);}
                if(wr)
                {
                    wr.sprite=WalkwaySprite();wr.color=Color.white;
                    var wps=walkway.parent.lossyScale;var wsz=wr.sprite.bounds.size;
                    walkway.localScale=new Vector3(6.4f/wsz.x/wps.x,(floorTop-floorBottom)/wsz.y/wps.y,1);
                    walkway.position=new Vector3(0,(floorTop+floorBottom)*.5f,walkway.position.z);
                }
                foreach(Transform child in Home.transform)
                    if(child.name=="Paving joint"){var jr=child.GetComponent<Renderer>();if(jr)jr.enabled=false;}
                if(walkwayEdge){var er=walkwayEdge.GetComponent<Renderer>();if(er)er.enabled=false;}
            }
            var stage=Home.transform.Find("Lower stage");var walk=Home.transform.Find("Walkway edge");
            // Original PLAY caption is much larger on the button.
            var play=Home.transform.Find("PLAY");
            if(play)foreach(var label in play.GetComponentsInChildren<TextMesh>(true))if(label.name!="Text outline"){label.transform.localScale*=1.3f*PlayLabelGrow;break;}
            // The button itself is ~5% larger and a few px lower (recording 2, 244 s).
            Place(play,PlayButtonScale,0,PlayButtonDropPx);
            if(!stage || !walk)return;
            var stageArt=stage.GetComponent<Renderer>();
            if(stageArt)AddStagePattern(stageArt,floorBottom,(walkway?walkway.position.z:stage.position.z)-.01f);
            var cone=new GameObject("Stage spotlight"){layer=Home.layer}.AddComponent<SpriteRenderer>();
            cone.transform.SetParent(Home.transform,true);
            cone.sprite=SpotlightSprite();
            // The original's light falls on the walkway too (the floor under it is ~40% brighter), then fades below.
            float top=floorTop,bottom=floorBottom-2.5f;
            cone.transform.position=new Vector3(0,(top+bottom)/2,(walkway?walkway.position.z:stage.position.z)-.02f);
            var size=cone.sprite.bounds.size;var parent=cone.transform.parent.lossyScale;
            cone.transform.localScale=new Vector3(SpotlightWidth/size.x/parent.x,(top-bottom)/size.y/parent.y,1);
        }
        // Scales a part about its own centre and moves it by (dx, dy) screen pixels of the 1920 px tall recordings
        // (dy downward).
        internal static void Place(Transform t,float scale,float dxPx,float dyPx)
        {
            if(!t)return;
            var r=t.GetComponentInChildren<Renderer>();var before=r?r.bounds.center:t.position;
            t.localScale*=scale;
            t.position+=before-(r?r.bounds.center:t.position)+new Vector3(dxPx,-dyPx,0)*Px;
        }
        void NavPlace(string name,float scale,float dxPx,float dyPx){Place(Home.transform.Find(name),scale,dxPx,dyPx);}
        // A flat strip of the top bar between two heights, just in front of it.
        void BarStrip(SpriteRenderer bar,string name,float bottom,float top,Color32 color)
        {
            var r=new GameObject(name){layer=Home.layer}.AddComponent<SpriteRenderer>();
            r.transform.SetParent(Home.transform,false);
            r.sprite=SpriteFactory.White;r.color=color;r.sortingOrder=bar.sortingOrder;r.sharedMaterial=bar.sharedMaterial;
            r.transform.position=new Vector3(0,(top+bottom)*.5f,bar.transform.position.z-.001f);
            r.FitWorldSize(6.4f,top-bottom);
        }
        // Hangers run from just under the top bar down to the sign (bottom). rods: straight square-ended dark rods
        // (~0.09 wide, with the sign art) instead of the stretched template pills.
        void FitHangers(SpriteRenderer bar,float bottom,bool rods)
        {
            float top=bar.bounds.min.y+.02f;
            foreach(Transform child in Home.transform)
            {
                if(!child.name.StartsWith("Hanger"))continue;
                var hr=child.GetComponent<SpriteRenderer>();if(!hr)continue;
                if(rods){hr.sprite=SpriteFactory.White;hr.drawMode=SpriteDrawMode.Simple;hr.color=new Color32(84,124,172,255);child.localScale=Vector3.one;}
                var hb=hr.bounds;var ls=child.localScale;
                float sx=rods?.09f/Mathf.Max(.0001f,hb.size.x):ls.x;
                child.localScale=new Vector3(sx,ls.y*(top-bottom)/Mathf.Max(.0001f,hb.size.y),1);
                child.position=new Vector3(child.position.x,(top+bottom)*.5f,child.position.z);
            }
        }
        // Original Home level sign: the brushed blue panel art (Group 10295), ~86% as wide as the drawn sign, hung
        // from two thin dark rods; the number and LEVEL in white with only a soft blue edge.
        void ReplaceLevelSign(SpriteRenderer bar)
        {
            var tex=Resources.Load<Texture2D>("VideoUI/home-level-sign");var rim=Home.transform.Find("Level sign rim");
            if(!tex || !rim)return;
            var rr=rim.GetComponent<Renderer>();var rb=rr.bounds;
            var go=new GameObject("Level sign art"){layer=Home.layer};go.transform.SetParent(Home.transform,false);
            var art=go.AddComponent<SpriteRenderer>();
            art.sprite=Sprite.Create(tex,new Rect(0,0,tex.width,tex.height),new Vector2(.5f,.5f),100);
            art.sharedMaterial=SpriteFactory.PlainSprite;art.sortingOrder=rr.sortingOrder;
            // Original (measured on the 888 px recording): the blue panel is 32% of the screen wide, top where the drawn
            // sign's top was; the art (with its rim) is drawn at 37% of a 5-unit screen so the panels match.
            float width=5.0f*.37f,height=width*753f/606f; // art is 606x753 (the imported texture may be resized square)
            art.FitWorldSize(width,height);
            go.transform.position=new Vector3(rb.center.x,rb.max.y-height*.5f,rim.position.z);
            foreach(var name in new[]{"Level sign blue","Sign inset","Level sign rim","Level sign shadow"}){var t=Home.transform.Find(name);if(t){var r=t.GetComponent<Renderer>();if(r)r.enabled=false;}}
            // Number and LEVEL centred on the new panel (upper two thirds / lower band).
            var number=Home.transform.Find("147");var level=Home.transform.Find("LEVEL");var ab=art.bounds;
            if(number){number.position=new Vector3(ab.center.x,ab.min.y+ab.size.y*.6f,number.position.z);number.localScale*=.88f;}
            if(level)level.position=new Vector3(ab.center.x,ab.min.y+ab.size.y*.22f,level.position.z);
            // Thin dark rods from the top bar to the sign.
            if(bar)FitHangers(bar,ab.max.y-.03f,true);
            StartCoroutine(SignText());
        }
        // Original "JOIN" plaque under the Sand Quest chest: the gold label art (Chest_Label_Enable, 262x111), same
        // width as the drawn pill it replaces. Kept when the art is absent.
        // JOIN text: 72 x 22 px with a thin dark-gold edge (139,106,4), ours was 85 x 28 with the black ink.
        const float ChestGrow=1.05f, JoinTextScale=.82f; internal static readonly Color JoinInk=new Color32(139,106,4,255); static readonly Vector2 ChestShiftPx=new Vector2(11,-8);
        void ReplaceJoinPlaque()
        {
            var tex=Resources.Load<Texture2D>("VideoUI/chest-label");var label=Home.transform.Find("Join label");
            var r=label?label.GetComponent<SpriteRenderer>():null;if(!tex || !r)return;
            var b=r.bounds;
            r.sprite=Sprite.Create(tex,new Rect(0,0,tex.width,tex.height),new Vector2(.5f,.5f),100);
            r.drawMode=SpriteDrawMode.Simple;r.color=Color.white;r.sharedMaterial=SpriteFactory.PlainSprite;
            float width=b.size.x*1.05f,height=width*111f/262f; // true art aspect (the import may resize it)
            r.FitWorldSize(width,height);
            label.position=new Vector3(b.center.x,b.center.y,label.position.z);
        }
        // Recording 2 (244 s): the number and LEVEL are white with a thin near-black edge (the shared ink), the number
        // 6% smaller and 15 px higher, LEVEL 12% smaller and 14 px higher than first built.
        IEnumerator SignText()
        {
            Place(Home.transform.Find("147"),.94f,0,-15);Place(Home.transform.Find("LEVEL"),.88f,0,-14);
            // The outline component is added by TextOutliner a moment after the page is built.
            for(int i=0;i<30;i++)
            {
                bool done=true;
                foreach(var name in new[]{"147","LEVEL"})
                {
                    var t=Home.transform.Find(name);var o=t?t.GetComponent<OutlinedText>():null;
                    if(o)o.SetThickness(SignOutline);else if(t)done=false;
                }
                if(done)yield break;
                yield return new WaitForSecondsRealtime(.2f);
            }
        }
        const float SignOutline=.03f;
        static void Tint(Transform t,Color color)
        {
            if(!t)return;var r=t.GetComponent<Renderer>();if(!r)return;
            var sr=r as SpriteRenderer;if(sr){sr.color=color;return;}
            if(r.sharedMaterial && r.sharedMaterial.HasProperty("_Color"))r.material.color=color;
        }
        // Original lower stage: faint pixel diamonds (little checkered sparkles, a shade darker than the stage) in a
        // staggered grid below the walkway, under the spotlight. Measured on the recording: ~0.21 units across,
        // 1.08 apart in a row, rows 0.6 apart, every other row shifted half a step.
        void AddStagePattern(Renderer stageArt,float top,float z)
        {
            const float ppu=48f,stepX=1.08f,stepY=.6f,motif=.21f;
            float left=-3.2f,width=6.4f,bottom=stageArt.bounds.min.y,height=top-bottom;
            if(height<=.3f)return;
            int w=Mathf.CeilToInt(width*ppu),h=Mathf.CeilToInt(height*ppu);
            var t=new Texture2D(w,h,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp,name="Stage pattern"};
            var px=new Color32[w*h];var ink=new Color32(14,38,70,50);float cell=motif/5f;int c=Mathf.Max(1,Mathf.RoundToInt(cell*ppu));
            for(int row=0;(row+.4f)*stepY<height;row++)
                for(int col=-1;col*stepX<width+stepX;col++)
                {
                    float cx=(col+(row%2)*.5f)*stepX+.3f,cy=height-(row+.4f)*stepY;
                    for(int dy=-2;dy<=2;dy++)for(int dx=-2;dx<=2;dx++)
                    {
                        if(Mathf.Abs(dx)+Mathf.Abs(dy)>2 || ((dx+dy)&1)!=0)continue; // checkered diamond: centre, corners, tips
                        int x0=Mathf.RoundToInt((cx+dx*cell)*ppu)-c/2,y0=Mathf.RoundToInt((cy+dy*cell)*ppu)-c/2;
                        for(int y=y0;y<y0+c;y++)for(int x=x0;x<x0+c;x++)if(x>=0&&y>=0&&x<w&&y<h)px[y*w+x]=ink;
                    }
                }
            t.SetPixels32(px);t.Apply(false,true);
            var r=new GameObject("Stage pattern"){layer=Home.layer}.AddComponent<SpriteRenderer>();
            r.transform.SetParent(Home.transform,true);
            r.sprite=Sprite.Create(t,new Rect(0,0,w,h),new Vector2(.5f,.5f),ppu);
            r.sharedMaterial=SpriteFactory.PlainSprite;r.sortingOrder=stageArt.sortingOrder;
            r.transform.position=new Vector3(left+width*.5f,bottom+height*.5f,z);
            var ps=r.transform.parent.lossyScale;r.transform.localScale=new Vector3(1/ps.x,1/ps.y,1);
        }
        // Trapezoid of light: narrower under the walkway, widening and fading out towards the bottom.
        // Measured on the original (888 px wide): 41% of the screen wide on the walkway, ~50% mid-stage, ~68% near
        // the bottom, i.e. about 2.2 units at the top widening to 3.9.
        const float SpotlightWidth=4.4f;
        static Sprite SpotlightSprite()
        {
            // Covers the walkway (top quarter, even light) and the stage below (fading out); SpotlightWidth wide, with
            // a soft edge.
            const int w=128,h=128;var t=new Texture2D(w,h,TextureFormat.RGBA32,false){wrapMode=TextureWrapMode.Clamp};
            const float lit=.25f;
            for(int y=0;y<h;y++)
            {
                float v=y/(float)(h-1);                        // 0 bottom, 1 top
                float down=1-v;                                // 0 top, 1 bottom
                float half=.25f+.2f*down;                      // half width, fraction of SpotlightWidth: 1.1 units -> 2
                float fade=down<lit?1:Mathf.Pow(Mathf.Clamp01(1-(down-lit)/(1-lit)),1.3f);
                for(int x=0;x<w;x++)
                {
                    float u=Mathf.Abs((x+.5f)/w-.5f);
                    float edge=Mathf.SmoothStep(0,1,Mathf.Clamp01((half-u)/.08f));
                    t.SetPixel(x,y,new Color(.9f,.92f,.97f,.42f*fade*edge));
                }
            }
            t.Apply();
            return Sprite.Create(t,new Rect(0,0,w,h),new Vector2(.5f,.5f),w);
        }
        // Walkway ledge texture, top to bottom (fractions of its height, colours measured on the original):
        // shaded back edge, back tile row, row joint, front tile row (taller, nearer), front lip into shadow.
        // Three tile joints (centre and about 1.55 units each side) lean outward towards the viewer.
        static Sprite walkwaySprite;
        static Sprite WalkwaySprite()
        {
            if(walkwaySprite)return walkwaySprite;
            const int w=1024,h=160;const float width=6.4f,height=.8f;
            var px=new Color32[w*h];
            Color tile=new Color32(116,178,226,255),joint=new Color32(76,134,181,255);
            Color[] back={new Color32(112,174,222,255),new Color32(67,127,171,255),new Color32(35,82,120,255),new Color32(26,64,96,255)};
            Color[] lip={new Color32(118,182,230,255),new Color32(71,131,175,255),new Color32(36,83,123,255),new Color32(32,77,114,255)};
            Color Ramp(Color[] c,float f){f=Mathf.Clamp01(f)*(c.Length-1);int i=Mathf.Min((int)f,c.Length-2);return Color.Lerp(c[i],c[i+1],f-i);}
            const float backEnd=.111f,rowJoint=.46f,frontEnd=.889f,jointHalf=.012f;
            for(int y=0;y<h;y++)
            {
                float v=1-(y+.5f)/h;                           // 0 top, 1 bottom
                for(int x=0;x<w;x++)
                {
                    Color c;
                    if(v<backEnd)c=Ramp(back,v/backEnd);
                    else if(v>frontEnd)c=Ramp(lip,(v-frontEnd)/(1-frontEnd));
                    else
                    {
                        float floor=(v-backEnd)/(frontEnd-backEnd);
                        float wx=((x+.5f)/w-.5f)*width;
                        float side=1.53f+.12f*floor;              // outer joints spread towards the front
                        float d=Mathf.Min(Mathf.Abs(wx),Mathf.Min(Mathf.Abs(wx-side),Mathf.Abs(wx+side)));
                        float rowD=Mathf.Abs(v-rowJoint)*height;  // distance to the row joint, in units
                        float k=Mathf.Clamp01((jointHalf*1.6f-Mathf.Min(d,rowD))/(jointHalf*1.2f));
                        // Slight depth shading: the back row a touch darker than the front row.
                        c=Color.Lerp(tile*(v<rowJoint?.97f:1f),joint,k);c.a=1;
                    }
                    px[y*w+x]=c;
                }
            }
            var t=new Texture2D(w,h,TextureFormat.RGBA32,false){wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
            t.SetPixels32(px);t.Apply();
            walkwaySprite=Sprite.Create(t,new Rect(0,0,w,h),new Vector2(.5f,.5f),w/width);
            return walkwaySprite;
        }
        // Gameplay top bar measured on recording 2 (153 s): the gear, level pill and coin wallet sit ~20 px higher; the
        // gear a little smaller and further left, the pill a little wider, the wallet further right.
        const float PlayButtonScale=1.05f,PlayButtonDropPx=6,PlayLabelGrow=1.08f;
        const float WalkwayTopDropPx=32,WalkwayBottomDropPx=14,MascotScale=.8f,MascotDropPx=24f;
        void FitGameplayHud()
        {
            var hud=Gameplay.transform.Find("HUD - visual controls only");if(!hud)return;
            Place(hud.Find("Settings - UI only"),.95f,-6,-19);
            foreach(Transform t in hud)if(t.name=="Level badge"||t.name.StartsWith("Level ")){t.localScale=Vector3.Scale(t.localScale,new Vector3(1.05f,1,1));t.position+=Vector3.up*23*Px;}
            Place(hud.Find("Coin wallet - UI only"),.95f,10,-21);
        }
        // Original Superb! screen: the coin counter stays bright above the dark veil (the rest of the HUD dims).
        Renderer[] coinWallet;
        void LiftCoinWallet(bool lift)
        {
            if(coinWallet==null){var w=Gameplay.transform.FindDeep("Coin wallet - UI only");
                coinWallet=w?w.GetComponentsInChildren<Renderer>(true):new Renderer[0];}
            foreach(var r in coinWallet)if(r)r.sortingOrder=lift?16:0;
        }
        void OnDestroy(){SandBoardTextureView.ObstacleTint=SandBoardTextureView.DefaultObstacleTint;}
        public void Show(Page page)
        {
            if (Boosters && page != Page.Gameplay) Boosters.Cancel();
            if (page == Page.Result && Current != Page.Result && AudioManager.Ready) AudioManager.Instance.Play(Sfx.Victory);
            // Pages switch with a hard cut, as in the original (recording 2: Home -> level, Level Complete -> CONGRATS
            // -> Home); Superb! brings its own flash.
            Current=page;elapsed=0;
            if(page==Page.Loading)ShowLoadingHint();
            if(page==Page.Home && Popups)Popups.ShowSandQuestOnHome();
            Loading.SetActive(page==Page.Loading);Home.SetActive(page==Page.Home);
            // The finished picture stays visible under the Superb! screen's dark veil (original).
            Gameplay.SetActive(page==Page.Gameplay || page==Page.Celebration);
            LiftCoinWallet(page==Page.Celebration);
            Celebration.SetActive(page==Page.Celebration);Result.SetActive(page==Page.Result);
            // A Play click must not also select the character under the new screen.
            transitionFrame=Time.frameCount;Controller.enabled=false;
        }
        // Original loading screen: a random tip under the progress bar (MainMenu_Basic hint list).
        TextMesh[] loadingHint;
        void ShowLoadingHint()
        {
            if(loadingHint==null)
            {
                var label=Loading.GetComponentsInChildren<TextMesh>(true).FirstOrDefault(t=>t.text=="Loading..." && t.name!="Text outline");
                if(!label)return;
                var hint=Instantiate(label.gameObject,label.transform.parent);hint.name="Loading hint";
                float barY=LoadingFill.position.y,labelY=label.transform.position.y;
                hint.transform.position=new Vector3(label.transform.position.x,labelY+(barY-labelY)*1.85f,label.transform.position.z);
                hint.transform.localScale=label.transform.localScale*.62f;
                loadingHint=hint.GetComponentsInChildren<TextMesh>(true);
                foreach(var text in loadingHint){text.anchor=TextAnchor.UpperCenter;text.alignment=TextAlignment.Center;}
            }
            var tip=LoadingHints.Wrap(LoadingHints.Random(),44);
            foreach(var text in loadingHint)if(text)text.text=tip;
        }
        public void Play()
        {
            winPresentationTime=0;
            if (Boosters) Boosters.Cancel();
            Controller.Restart();Controller.Advance(0);Show(Page.Gameplay);
            int level=Levels.PlayedNumber>0?Levels.PlayedNumber:Campaign.LevelNumber;
            GameEvents.RaiseLevelStarted(level);
            Popups.ShowIntrosFor(level);
        }
        void Update()
        {
            if(viewWidth!=Screen.width || viewHeight!=Screen.height)
            {
                viewWidth=Screen.width;viewHeight=Screen.height;
                SandJamSceneController.FitView(Controller.GameCamera,(float)viewWidth/Mathf.Max(1,viewHeight),Controller.ViewAspect);
            }
            if(Current==Page.Gameplay && Time.frameCount>transitionFrame)Controller.enabled=true;
            elapsed+=Time.unscaledDeltaTime;
            if(Current==Page.Loading)
            {
                float progress=Mathf.Clamp01(elapsed/loadingTime);LoadingFill.localScale=new Vector3(fillScale.x*progress,fillScale.y,fillScale.z);
                var p=LoadingFill.localPosition;p.x=-1.625f*(1-progress);LoadingFill.localPosition=p;
                if(elapsed>loadingTime+.2f)Show(Page.Home);
            }
            if(Current==Page.Home)
            {
                // Original Home (recordings): a sand cube walks across the whole walkway in ~3 s, then the next one
                // comes - each pass a new colour (blue, yellow, red, pink...), alternating left-to-right and right-to-left.
                float lapTime=elapsed+MascotLap-MascotEnter;
                int lapIndex=Mathf.FloorToInt(lapTime/MascotLap);
                float lap=lapTime-lapIndex*MascotLap;
                bool walking=lap<MascotWalk;
                // Kept active between passes (parked off screen): re-enabling restarted the walk animation in a
                // side-on pose for a frame.
                if(lapIndex!=mascotLap){mascotLap=lapIndex;StartMascotPass();}
                float x=walking?Mathf.Lerp(MascotEdgeX*mascotDirection,-MascotEdgeX*mascotDirection,lap/MascotWalk):-6f*mascotDirection;
                Mascot.localPosition=new Vector3(x,mascotPosition.y,mascotPosition.z);
                Mascot.localRotation=Quaternion.Euler(0,-MascotYaw*mascotDirection,0);
            }
            if(Current==Page.Celebration && elapsed>SuperbFx.Duration)Show(Page.Result);
            if(Current==Page.Gameplay && Controller.Game!=null)
            {
                if(Controller.Game.State==GameState.Won)
                {
                    Levels.RecordCompletion();
                    winPresentationTime+=Time.unscaledDeltaTime;
                    if(winPresentationTime>=1.65f)Show(Page.Celebration);
                }
                else winPresentationTime=0;
            }
            if(Input.GetKeyDown(KeyCode.Escape) && !Popups.Open)Show(Page.Home);
            if(Input.GetMouseButtonDown(0) && !Boosters.PickerOpen)
            {
                RaycastHit hit;
                if(!Physics.Raycast((UiCamera?UiCamera:Controller.GameCamera).ScreenPointToRay(Input.mousePosition),out hit,100,1<<9))return;
                var button=hit.collider.GetComponent<VideoUiButton>();
                if(button){button.Press();if(!button.Action.StartsWith("booster:"))GameEvents.RaiseButtonClicked(button.Action);}
                // While a popup is open only its own buttons respond.
                if(Popups.Open || Feedback.FailureVisible){if(Popups.Owns(button))Popups.Handle(button.Action);return;}
                if(button)HandleButton(button.Action);
            }
        }
        void NextAfterResult(){if(!Popups.ShowCongrats())Levels.LoadNext();}
        void HandleButton(string action)
        {
            if(action.StartsWith("booster:")){Boosters.Activate(action.Substring(8));return;}
            switch(action)
            {
                case "play":
                    if(!LivesManager.CanPlay)Popups.ShowNoLives();
                    // After a win the campaign has advanced; load that level instead of replaying the old one.
                    else if(Levels.PlayedNumber!=Campaign.LevelNumber)Levels.Reload(true);
                    else Play();
                    break;
                case "home":Show(Page.Home);break;
                case "sand-quest":Popups.OpenSandQuest();break;
                case "shop":Popups.OpenShop();break;
                case "gallery":Popups.OpenGallery();break;
                case "daily-rewards":Popups.OpenDailyRewards();break;
                case "daily-tasks":Popups.OpenTasks(TaskBoard.Kind.Daily);break;
                case "weekly-tasks":Popups.OpenTasks(TaskBoard.Kind.Weekly);break;
                // Original: the collection card (CONGRATS · category · % completed) comes before the next level.
                case "next-level":
                    var fx=Result.GetComponent<ResultFx>();
                    if(!fx || !fx.Collect(NextAfterResult))NextAfterResult();
                    break;
                case "double":Levels.DoubleReward();break;
                case "settings":Ui.OpenSettings();break;
            }
        }
        void Start()
        {
            // "Next" / "Retry" reload the scene straight into gameplay.
            if(Campaign.SkipIntro){Campaign.SkipIntro=false;Play();}
            // After a win or Try Again the SAND JAM splash shows for under a second, then Home.
            else if(Campaign.OpenHome){Campaign.OpenHome=false;loadingTime=.6f;QuickSplash();}
        }
    }
}

