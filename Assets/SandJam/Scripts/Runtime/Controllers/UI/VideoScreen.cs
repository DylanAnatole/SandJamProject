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
        float elapsed, winPresentationTime; Vector3 fillScale,mascotPosition; bool smoke;
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
            if(superbTitle){var fx=Celebration.AddComponent<SuperbFx>();fx.Font=Controller.InterfaceFont;fx.Build(superbTitle);}
            Show(Page.Loading);
            Boosters = gameObject.AddComponent<BoosterController>(); Boosters.Initialize(this);
            Feedback=gameObject.AddComponent<GameplayFeedbackController>();Feedback.Initialize(this);
            if(Levels){Ui=gameObject.AddComponent<CampaignUi>();Ui.Initialize(this);Popups=gameObject.AddComponent<PopupScreens>();Popups.Initialize(this);}
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--booster-smoke") >= 0) gameObject.AddComponent<BoosterSmoke>();
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--feeling-smoke") >= 0) gameObject.AddComponent<FeelingSmoke>();
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--october-smoke") >= 0) gameObject.AddComponent<OctoberVideoSmoke>();
        }
        void OnDestroy(){SandBoardTextureView.ObstacleTint=new Color(.16f,.27f,.34f);}
        public void Show(Page page)
        {
            if (Boosters && page != Page.Gameplay) Boosters.Cancel();
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
        void Update()
        {
            if(viewWidth!=Screen.width || viewHeight!=Screen.height)
            {
                viewWidth=Screen.width;viewHeight=Screen.height;
                float aspect=(float)viewWidth/Mathf.Max(1,viewHeight),target=Controller.ViewAspect;
                Controller.GameCamera.rect=aspect>target?new Rect((1-target/aspect)*.5f,0,target/aspect,1):new Rect(0,(1-aspect/target)*.5f,1,aspect/target);
                Controller.GameCamera.aspect=target;
            }
            if(Current==Page.Gameplay && !smoke && Time.frameCount>transitionFrame)Controller.enabled=true;
            elapsed+=Time.unscaledDeltaTime;
            if(Current==Page.Loading)
            {
                float progress=Mathf.Clamp01(elapsed/1.6f);LoadingFill.localScale=new Vector3(fillScale.x*progress,fillScale.y,fillScale.z);
                var p=LoadingFill.localPosition;p.x=-1.625f*(1-progress);LoadingFill.localPosition=p;
                if(elapsed>1.8f && !smoke)Show(Page.Home);
            }
            if(Current==Page.Home)
            {
                Mascot.localPosition=mascotPosition+new Vector3(Mathf.PingPong(elapsed*.4f,3),0,0);
                Mascot.localRotation=Quaternion.Euler(0,(elapsed*.4f)%6<3?-70:70,0);
            }
            if(Current==Page.Celebration && elapsed>2.2f && !smoke)Show(Page.Result);
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

