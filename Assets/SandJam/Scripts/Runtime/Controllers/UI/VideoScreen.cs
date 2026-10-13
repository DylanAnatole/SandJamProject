using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SandJamTest.Scene3D
{
    [DefaultExecutionOrder(-100)]
    public sealed class VideoScreen : MonoBehaviour
    {
        public SandJamSceneController Controller;
        public bool ReferenceBoosterLabels;
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
        float elapsed, winPresentationTime, loadingTime=1.6f; Vector3 fillScale,mascotPosition; bool smoke;
        int transitionFrame,viewWidth,viewHeight;
        void Awake()
        {
            if(EditorPreview)EditorPreview.enabled=false;
            // Region outlines stay lavender in every stage theme (recordings: blue and dark themes alike).
            SandBoardTextureView.ObstacleTint=new Color(.51f,.50f,.68f);
            fillScale=LoadingFill.localScale;mascotPosition=Mascot.localPosition;
            // Original "Superb!": the finished, zoomed-in picture stays visible through a translucent dark layer.
            var superbBackdrop=Celebration.transform.Find("Dark backdrop");
            if(superbBackdrop){var r=superbBackdrop.GetComponent<SpriteRenderer>();if(r){var c=r.color;c.a=.8f;r.color=c;}}
            var superbTitle=Celebration.GetComponentsInChildren<TextMesh>(true).FirstOrDefault(t=>t.text=="Superb!" && t.name!="Text outline");
            if(superbTitle){var fx=Celebration.AddComponent<SuperbFx>();fx.Font=UiFont.Font?UiFont.Font:Controller.InterfaceFont;fx.Build(superbTitle);}
            StyleHome();
            gameObject.AddComponent<TextOutliner>();
            Show(Page.Loading);
            Boosters = gameObject.AddComponent<BoosterController>(); Boosters.Initialize(this);
            Feedback=gameObject.AddComponent<GameplayFeedbackController>();Feedback.Initialize(this);
            if(Levels){Ui=gameObject.AddComponent<CampaignUi>();Ui.Initialize(this);Popups=gameObject.AddComponent<PopupScreens>();Popups.Initialize(this);}
            UiFont.ApplyAll();
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--booster-smoke") >= 0) gameObject.AddComponent<BoosterSmoke>();
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--feeling-smoke") >= 0) gameObject.AddComponent<FeelingSmoke>();
        }

        // Home closer to the original recording (level 267): brighter level sign and stage, and a soft spotlight
        // falling from the walkway onto the PLAY button.
        void StyleHome()
        {
            Tint(Home.transform.Find("Level sign blue"),new Color32(80,160,241,255));
            Tint(Home.transform.Find("Sign inset"),new Color32(104,180,236,255));
            Tint(Home.transform.Find("Level sign rim"),new Color32(178,232,250,255));
            Tint(Home.transform.Find("Lower stage"),new Color32(38,91,132,255));
            // Colours measured on the original Home (recording, level 267).
            // (sprites render ~12% darker here, so the sign colours are pre-brightened to land on the measured ones)
            Tint(Home.transform.Find("Level sign blue"),new Color32(106,192,255,255));
            Tint(Home.transform.Find("Sign inset"),new Color32(124,206,255,255));
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
            var plain=new Material(Shader.Find("Sprites/Default"));
            foreach(var name in new[]{"Level sign blue","Sign inset","Level sign rim","Level sign shadow","Join label"})
            {
                var t=Home.transform.Find(name);var r=t?t.GetComponent<SpriteRenderer>():null;if(!r)continue;
                var rb=r.bounds;r.sharedMaterial=plain;r.sprite=WhiteRounded();r.drawMode=SpriteDrawMode.Sliced;
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
            var square=SquareSprite();
            foreach(var name in new[]{"Top bar","Walkway","Lower stage","Walkway edge"})
            {
                var r=Home.transform.Find(name)?Home.transform.Find(name).GetComponent<SpriteRenderer>():null;if(!r)continue;
                var rb=r.bounds;r.sprite=square;r.drawMode=SpriteDrawMode.Simple;
                var rps=r.transform.parent.lossyScale;r.transform.localScale=new Vector3(6.4f/rps.x,rb.size.y/rps.y,1);
                r.transform.position=new Vector3(0,rb.center.y,r.transform.position.z);
            }
            var bar=Home.transform.Find("Top bar")?Home.transform.Find("Top bar").GetComponent<SpriteRenderer>():null;
            if(bar){var bb=bar.bounds;float barBottom=4.2f;var bps=bar.transform.parent.lossyScale;bar.transform.localScale=new Vector3(bar.transform.localScale.x,(bb.max.y-barBottom)/bps.y,1);bar.transform.position=new Vector3(0,(bb.max.y+barBottom)*.5f,bar.transform.position.z);}
            foreach(var name in new[]{"generalBackground-main","bg-navbar"}){var t=Home.transform.Find(name);if(t){var ls=t.localScale;t.localScale=new Vector3(ls.x*1.12f,ls.y,ls.z);}}
            // Hangers run from the top bar down to the sign.
            var rimNow=Home.transform.Find("Level sign rim");
            if(rimNow && bar)foreach(Transform child in Home.transform)
                if(child.name.StartsWith("Hanger"))
                {
                    var hr=child.GetComponent<SpriteRenderer>();if(!hr)continue;
                    float hTop=bar.bounds.min.y+.02f,hBottom=rimNow.GetComponent<Renderer>().bounds.max.y-.02f;var hb=hr.bounds;
                    child.localScale=new Vector3(child.localScale.x,child.localScale.y*(hTop-hBottom)/Mathf.Max(.001f,hb.size.y),1);
                    child.position=new Vector3(child.position.x,(hTop+hBottom)*.5f,child.position.z);
                }
            // Navbar: the original icons and captions are noticeably bigger.
            foreach(var name in new[]{"icon-shop","icon-home","icon-cards"}){var t=Home.transform.Find(name);if(t)t.localScale*=1.15f;}
            foreach(var name in new[]{"SHOP","HOME","GALLERY"}){var t=Home.transform.Find(name);if(t)t.localScale*=1.22f;}
            // Walkway seen in perspective: the tile joints lean outward like the original's floor.
            foreach(Transform child in Home.transform)
                if(child.name=="Paving joint")child.localRotation=Quaternion.Euler(0,0,-child.position.x*7f);
            var stage=Home.transform.Find("Lower stage");var walk=Home.transform.Find("Walkway edge");
            // Original PLAY caption is much larger on the button.
            var play=Home.transform.Find("PLAY");
            if(play)foreach(var label in play.GetComponentsInChildren<TextMesh>(true))if(label.name!="Text outline"){label.transform.localScale*=1.3f;break;}
            if(!stage || !walk)return;
            var cone=new GameObject("Stage spotlight"){layer=Home.layer}.AddComponent<SpriteRenderer>();
            cone.transform.SetParent(Home.transform,true);
            cone.sprite=SpotlightSprite();
            float top=walk.GetComponent<Renderer>().bounds.min.y,bottom=top-2.5f;
            cone.transform.position=new Vector3(0,(top+bottom)/2,stage.position.z-.03f);
            var size=cone.sprite.bounds.size;var parent=cone.transform.parent.lossyScale;
            cone.transform.localScale=new Vector3(3.2f/size.x/parent.x,(top-bottom)/size.y/parent.y,1);
        }
        static Sprite whiteRounded;
        static Sprite WhiteRounded()
        {
            if(whiteRounded)return whiteRounded;
            const int n=64;const float radius=22f;var t=new Texture2D(n,n,TextureFormat.RGBA32,false){wrapMode=TextureWrapMode.Clamp};
            for(int y=0;y<n;y++)for(int x=0;x<n;x++)
            {
                float px=Mathf.Abs(x+.5f-n/2f)-(n/2f-radius),py=Mathf.Abs(y+.5f-n/2f)-(n/2f-radius);
                float d=new Vector2(Mathf.Max(px,0),Mathf.Max(py,0)).magnitude+Mathf.Min(Mathf.Max(px,py),0)-radius;
                t.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(.5f-d)));
            }
            t.Apply();
            whiteRounded=Sprite.Create(t,new Rect(0,0,n,n),new Vector2(.5f,.5f),130f,0,SpriteMeshType.FullRect,new Vector4(23,23,23,23));
            return whiteRounded;
        }
        static Sprite squareSprite;
        static Sprite SquareSprite(){if(squareSprite)return squareSprite;var w=Texture2D.whiteTexture;squareSprite=Sprite.Create(w,new Rect(0,0,w.width,w.height),new Vector2(.5f,.5f),w.width);return squareSprite;}
        static void Tint(Transform t,Color color)
        {
            if(!t)return;var r=t.GetComponent<Renderer>();if(!r)return;
            var sr=r as SpriteRenderer;if(sr){sr.color=color;return;}
            if(r.sharedMaterial && r.sharedMaterial.HasProperty("_Color"))r.material.color=color;
        }
        // Trapezoid of light: narrower under the walkway, widening and fading out towards the bottom.
        static Sprite SpotlightSprite()
        {
            const int w=128,h=128;var t=new Texture2D(w,h,TextureFormat.RGBA32,false){wrapMode=TextureWrapMode.Clamp};
            for(int y=0;y<h;y++)
            {
                float v=y/(float)(h-1);                        // 0 bottom, 1 top
                float half=.3f+.2f*(1-v);                      // widens downwards
                for(int x=0;x<w;x++)
                {
                    float u=Mathf.Abs((x+.5f)/w-.5f);
                    float edge=Mathf.Clamp01((half-u)/.06f);
                    float a=.34f*v*v*edge;
                    t.SetPixel(x,y,new Color(.75f,.88f,1f,a));
                }
            }
            t.Apply();
            return Sprite.Create(t,new Rect(0,0,w,h),new Vector2(.5f,.5f),w);
        }
        void OnDestroy(){SandBoardTextureView.ObstacleTint=new Color(.16f,.27f,.34f);}
        public void Show(Page page)
        {
            if (Boosters && page != Page.Gameplay) Boosters.Cancel();
            if(page!=Current && Current!=Page.Loading && !smoke) StartFade(page==Page.Celebration?.18f:.3f);
            Current=page;elapsed=0;
            if(page==Page.Loading)ShowLoadingHint();
            if(page==Page.Home && Popups)Popups.ShowSandQuestOnHome();
            Loading.SetActive(page==Page.Loading);Home.SetActive(page==Page.Home);Gameplay.SetActive(page==Page.Gameplay);
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
            if(Levels)GameEvents.RaiseLevelStarted(Levels.PlayedNumber>0?Levels.PlayedNumber:Campaign.LevelNumber);
            if(Popups && Levels)Popups.ShowIntrosFor(Levels.PlayedNumber>0?Levels.PlayedNumber:Campaign.LevelNumber);
        }
        // Page transition: the new page appears out of a quick dark veil instead of a hard cut.
        SpriteRenderer fade; float fadeTime, fadeDuration;
        void StartFade(float seconds)
        {
            if(!fade)
            {
                var go=new GameObject("Page fade"){layer=Result.layer};
                go.transform.SetParent(Result.transform.parent,false);
                var origin=Result.transform.parent?Result.transform.parent.position:Vector3.zero;
                go.transform.position=new Vector3(origin.x,origin.y,-10.6f); // above pages (z -5..-8), below popups (z -11..-14)
                fade=go.AddComponent<SpriteRenderer>();
                var t=Texture2D.whiteTexture;
                fade.sprite=Sprite.Create(t,new Rect(0,0,t.width,t.height),new Vector2(.5f,.5f),t.width);
                fade.sortingOrder=0; // same order as page and popup art, so depth decides
                var ps=go.transform.parent?go.transform.parent.lossyScale:Vector3.one;
                go.transform.localScale=new Vector3(30/Mathf.Max(.0001f,ps.x),40/Mathf.Max(.0001f,ps.y),1); // covers the whole screen
            }
            fadeDuration=seconds;fadeTime=0;fade.gameObject.SetActive(true);
            fade.color=new Color(.08f,.06f,.16f,.85f);
        }
        void UpdateFade()
        {
            if(!fade || !fade.gameObject.activeSelf)return;
            fadeTime+=Time.unscaledDeltaTime;
            float t=Mathf.Clamp01(fadeTime/fadeDuration);
            fade.color=new Color(.08f,.06f,.16f,.85f*(1-t)*(1-t));
            if(t>=1)fade.gameObject.SetActive(false);
        }
        void Update()
        {
            UpdateFade();
            if(viewWidth!=Screen.width || viewHeight!=Screen.height)
            {
                viewWidth=Screen.width;viewHeight=Screen.height;
                SandJamSceneController.FitView(Controller.GameCamera,(float)viewWidth/Mathf.Max(1,viewHeight),Controller.ViewAspect);
            }
            if(Current==Page.Gameplay && !smoke && Time.frameCount>transitionFrame)Controller.enabled=true;
            elapsed+=Time.unscaledDeltaTime;
            if(Current==Page.Loading)
            {
                float progress=Mathf.Clamp01(elapsed/loadingTime);LoadingFill.localScale=new Vector3(fillScale.x*progress,fillScale.y,fillScale.z);
                var p=LoadingFill.localPosition;p.x=-1.625f*(1-progress);LoadingFill.localPosition=p;
                if(elapsed>loadingTime+.2f && !smoke)Show(Page.Home);
            }
            if(Current==Page.Home)
            {
                Mascot.localPosition=mascotPosition+new Vector3(Mathf.PingPong(elapsed*.4f,3),0,0);
                Mascot.localRotation=Quaternion.Euler(0,(elapsed*.4f)%6<3?-70:70,0);
            }
            if(Current==Page.Celebration && elapsed>SuperbFx.Duration && !smoke)Show(Page.Result);
            if(Current==Page.Gameplay && Controller.Game!=null)
            {
                Status.text=Controller.Game.State==GameState.Lost?(Popups?"":"Out of Space · R: retry"):Boosters && !string.IsNullOrEmpty(Boosters.Message)?Boosters.Message:Controller.SelectionFeedback;
                if(Controller.Game.State==GameState.Won && !smoke)
                {
                    if(Levels)Levels.RecordCompletion();
                    winPresentationTime+=Time.unscaledDeltaTime;
                    if(winPresentationTime>=1.65f)Show(Page.Celebration);
                }
                else winPresentationTime=0;
            }
            if(smoke)return;
            if(Input.GetKeyDown(KeyCode.F1))Show(Page.Loading);
            if((Input.GetKeyDown(KeyCode.F2) || Input.GetKeyDown(KeyCode.Escape)) && !(Popups && Popups.Open))Show(Page.Home);
            if(Input.GetKeyDown(KeyCode.F3))Play();
            if(Input.GetKeyDown(KeyCode.F4))Show(Page.Celebration);
            if(Input.GetKeyDown(KeyCode.F5))Show(Page.Result);
            if(Input.GetMouseButtonDown(0) && !(Boosters && Boosters.PickerOpen) && !(Feedback && Feedback.FailureVisible && !Popups))
            {
                RaycastHit hit;
                if(Physics.Raycast((UiCamera?UiCamera:Controller.GameCamera).ScreenPointToRay(Input.mousePosition),out hit,100,1<<9))
                {
                    var button=hit.collider.GetComponent<VideoUiButton>();
                    if(button)button.Press();
                    if(button && !button.Action.StartsWith("booster:"))GameEvents.RaiseButtonClicked(button.Action);
                    // While a popup is open only its own buttons respond.
                    if(Popups && (Popups.Open || (Feedback && Feedback.FailureVisible))){if(Popups.Owns(button))Popups.Handle(button.Action);return;}
                    // After a win the campaign has advanced; load that level instead of replaying the old one.
                    if(button && button.Action=="play")
                    {
                        if(Levels && !LivesManager.CanPlay){if(Popups)Popups.ShowNoLives();}
                        else if(Levels && Levels.PlayedNumber!=Campaign.LevelNumber)Levels.Reload(true);
                        else Play();
                    }
                    else if(button && button.Action=="home")Show(Page.Home);
                    else if(button && button.Action=="sand-quest" && Popups)Popups.OpenSandQuest();
                    else if(button && button.Action=="shop" && Popups)Popups.OpenShop();
                    else if(button && button.Action=="gallery" && Popups)Popups.OpenGallery();
                    else if(button && button.Action=="daily-rewards" && Popups)Popups.OpenDailyRewards();
                    else if(button && button.Action=="daily-tasks" && Popups)Popups.OpenTasks(TaskBoard.Kind.Daily);
                    else if(button && button.Action=="weekly-tasks" && Popups)Popups.OpenTasks(TaskBoard.Kind.Weekly);
                    // Original: the collection card (CONGRATS · category · % completed) comes before the next level.
                    else if(button && button.Action=="next-level" && Levels){if(!(Popups && Popups.ShowCongrats()))Levels.LoadNext();}
                    else if(button && button.Action=="double" && Levels)Levels.DoubleReward();
                    else if(button && button.Action=="settings" && Ui)Ui.OpenSettings();
                    else if(button && button.Action.StartsWith("level:") && Levels){int index;if(int.TryParse(button.Action.Substring(6),out index))Levels.Select(index);}
                    else if(button && button.Action.StartsWith("booster:") && Boosters) Boosters.Activate(button.Action.Substring(8));
                }
            }
        }
        void Start()
        {
            string[] args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"--video-smoke");
            if(index>=0 && index+1<args.Length){smoke=true;Controller.enabled=false;StartCoroutine(Guard(Smoke(args[index+1]),args[index+1]));}
            // "Next" / "Retry" reload the scene straight into gameplay.
            else if(Campaign.SkipIntro){Campaign.SkipIntro=false;Play();}
            // Original: after a win the SAND JAM splash shows for under a second, then Home.
            else if(Campaign.OpenHome){Campaign.OpenHome=false;loadingTime=.6f;}
        }
        IEnumerator Guard(IEnumerator routine,string folder)
        {
            Directory.CreateDirectory(folder);
            while(true)
            {
                object current;
                try {if(!routine.MoveNext())break;current=routine.Current;}
                catch(Exception e){File.WriteAllText(Path.Combine(folder,"FAILED.txt"),e.ToString());Debug.LogException(e);Application.Quit(1);yield break;}
                yield return current;
            }
        }
        IEnumerator Capture(string folder,string name)
        {
            yield return null;yield return new WaitForEndOfFrame();
            var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(folder,name+".png"),t.EncodeToPNG());Destroy(t);
        }
        static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
        IEnumerator Smoke(string folder)
        {
            yield return null;
            Require(Controller.Game!=null,"Game model did not initialize");
            Require(!Controller.GameCamera.orthographic && UiCamera && UiCamera.orthographic,"Perspective stage or overlay camera missing");
            Require(Controller.Regions.Length==18 && Controller.Game.Slots.Length==5,"Expected Pop Art level and five slots");
            Require(Controller.Game.Data.uiDivider==10,"Incorrect UI divider");
            Require(Controller.Game.SlotRemaining(4)==300,"Fifth slot should start locked at 300 units");
            Require(Controller.Game.Regions.Where(r=>r.Open).Select(r=>r.Remaining/10).OrderBy(n=>n).SequenceEqual(new[]{42,45}),"Initial regions differ from video");
            Require(Placeholders.Where(p=>p.ActionId=="rocket" || p.ActionId=="swap" || p.ActionId=="select").All(p=>p.FunctionImplemented),"Booster actions are not connected");
            Show(Page.Loading);yield return Capture(folder,"01-loading");
            Show(Page.Home);yield return Capture(folder,"02-home");
            Play();yield return Capture(folder,"03-gameplay");
            var animated=Controller.Characters.Single(c=>c.SourceLane==0 && c.SourceOrder==0).Motion;
            Require(animated && animated.Animator,"Recovered animator is missing");
            var diagnosticSkin=animated.GetComponentInChildren<SkinnedMeshRenderer>();var diagnosticMesh=new Mesh();diagnosticSkin.BakeMesh(diagnosticMesh);
            Require(diagnosticSkin.sharedMaterials.Length==diagnosticSkin.sharedMesh.subMeshCount,"A character submesh has no material");
            Require(diagnosticSkin.sharedMaterial.shader.name=="SandJamTest/SandToonDepth" && diagnosticSkin.sharedMaterial.shader.isSupported,"Toon shader unavailable");
            Require(Quaternion.Angle(animated.transform.localRotation,Quaternion.identity)>20,"Character pitch was lost during Bind");
            File.WriteAllText(Path.Combine(folder,"rig-diagnostics.txt"),"Skin bounds: "+diagnosticSkin.bounds+"\nBaked: "+diagnosticMesh.bounds+"\nScale: "+diagnosticSkin.transform.lossyScale+"\nAnimator: "+animated.Animator.transform.position+"\nMaterial: "+diagnosticSkin.sharedMaterial.name+"\nVertices: "+diagnosticMesh.vertexCount);Destroy(diagnosticMesh);
            var leg=animated.Animator.GetComponentsInChildren<Transform>().Single(t=>t.name=="mixamorig:LeftUpLeg");
            animated.SetWalking(true);animated.Animator.Play("Walk",0,.1f);animated.Animator.Update(0);var poseA=leg.localRotation;
            animated.Animator.Play("Walk",0,.55f);animated.Animator.Update(0);var poseB=leg.localRotation;
            Require(Quaternion.Angle(poseA,poseB)>1,"Recovered Walk clip did not animate the leg bone");
            animated.ResetPose();
            int initial=Controller.Game.Remaining;Require(Controller.SelectLane(0),"Cannot select purple");
            for(int i=0;i<120;i++){Controller.Advance(.03f);yield return null;if(i==7)yield return Capture(folder,"03b-walking-to-slot");}
            Require(Controller.Game.Remaining<initial,"Selected character did not pour");
            Require(Controller.Game.SlotRemaining(4)<300,"Slot unlock counter did not decrease");
            yield return Capture(folder,"04-sand-flow");
            Play();
            // A deterministic replay found from this level's exported queues and dependencies.
            int[] replay={0,1,0,0,1,1,0,0,0,0,0,1,1,1,2,1,1,2,2,2,2,2,2,2};
            foreach(int lane in replay)
            {
                Require(Controller.SelectLane(lane),"Replay selection failed in lane "+lane);
                int steps=0;
                do
                {
                    for(int i=0;i<20;i++){Controller.Advance(.03f);steps++;}
                    yield return null;
                    Require(steps<20000,"Replay stalled while waiting for sand");
                }
                while(Controller.Game.Slots.Any(s=>Controller.Game.Target(s)>=0) ||
                      Controller.Regions.Any(r=>r.FlyingCount>0) ||
                      Controller.Game.Regions.Select((r,i)=>r.Remaining==0 && !Controller.Regions[i].IsSettled).Any(p=>p));
                Require(Controller.Game.State!=GameState.Lost,"Replay lost unexpectedly");
            }
            Controller.Advance(.1f);
            Require(Controller.Game.State==GameState.Won,"Replay did not complete the level");
            Require(Controller.Regions.All(r=>r.IsSettled),"Some regions are not completely covered");
            Require(Controller.Game.SlotRemaining(4)==0,"Fifth slot did not unlock");
            yield return Capture(folder,"05-completed-board");
            Show(Page.Celebration);yield return Capture(folder,"05-celebration");
            Show(Page.Result);yield return Capture(folder,"06-result");
            Play();Require(Controller.Game.Remaining==initial,"Replay did not reset level");
            File.WriteAllText(Path.Combine(folder,"checks.txt"),"PASS: All five screens captured\nPASS: Perspective stage and orthographic HUD\nPASS: Toon shader supported and all character submeshes have materials\nPASS: Inclined pose survives restart\nPASS: Recovered Walk clip changes the leg bone pose\nPASS: Walking-to-slot screenshot captured\nPASS: Level 147 reference has 18 regions, amounts 45/42, divider 10\nPASS: Exactly five slots; fifth unlocks after 300 units\nPASS: Three booster actions connected\nPASS: Purple selection pours sand\nPASS: Full 24-move replay wins with all 18 regions settled\nPASS: Replay restores level\n");
            Application.Quit(0);
        }
    }
}

