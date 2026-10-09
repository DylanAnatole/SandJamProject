using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
namespace SandJamTest.Scene3D
{
    [DefaultExecutionOrder(-100)]
    public sealed class ReferenceScreen : MonoBehaviour
    {
        public SandJamSceneController Controller;
        public TextMesh Status;
        public SpriteRenderer EditorPreview;
        public ReferenceUiPlaceholder[] Placeholders;
        public int PreviewLevelNumber=111;
        public int PreviewCoins=210;
        void Awake(){if(EditorPreview)EditorPreview.enabled=false;SandBoardTextureView.ObstacleTint=new Color(.51f,.50f,.68f);}
        void OnDestroy(){SandBoardTextureView.ObstacleTint=new Color(.16f,.27f,.34f);}
        void Update()
        {
            if(Controller.Game==null)return;
            Status.text=Controller.Game.State==GameState.Won?"Complete!  ·  R: retry":Controller.Game.State==GameState.Lost?"Out of Space  ·  R: retry":"";
        }
        void Start()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--reference-smoke");
            if(i>=0 && i+1<args.Length){Controller.enabled=false;StartCoroutine(Smoke(args[i+1]));}
        }
        IEnumerator Capture(string folder,string name)
        {
            yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(folder,name+".png"),t.EncodeToPNG());Destroy(t);
        }
        IEnumerator Smoke(string folder)
        {
            Directory.CreateDirectory(folder);yield return null;Controller.Advance(0);
            if(Controller.Game==null || Controller.Game.Slots.Length!=5 || Controller.Regions.Length!=39 || Placeholders.Length!=5 || Placeholders.Any(p=>p.FunctionImplemented))
            {File.WriteAllText(Path.Combine(folder,"FAILED.txt"),"Reference scene configuration invalid");Application.Quit(1);yield break;}
            yield return Capture(folder,"01-reference-start");
            int initial=Controller.Game.Remaining;
            Controller.SelectLane(2);
            for(int i=0;i<120;i++){Controller.Advance(.03f);yield return null;}
            if(Controller.Game.Remaining>=initial){File.WriteAllText(Path.Combine(folder,"FAILED.txt"),"Sand does not flow");Application.Quit(1);yield break;}
            yield return Capture(folder,"02-reference-flow");
            Controller.Restart();Controller.Advance(0);
            if(Controller.Game.Remaining!=initial){File.WriteAllText(Path.Combine(folder,"FAILED.txt"),"Restart failed");Application.Quit(1);yield break;}
            File.WriteAllText(Path.Combine(folder,"checks.txt"),"PASS: Deer level has 39 regions\nPASS: Exactly five stash slots\nPASS: Five UI-only controls without actions\nPASS: Selecting yellow pours sand\nPASS: Restart restores level\nPASS: Screenshots captured\n");Application.Quit(0);
        }
    }
}

