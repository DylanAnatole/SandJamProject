using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
namespace SandJamTest.Scene3D
{
    public sealed class FreezeMechanicSmoke : MonoBehaviour
    {
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"--freeze-smoke");if(at<0 || at+1>=args.Length)yield break;
            string folder=args[at+1];Directory.CreateDirectory(folder);var run=Run(folder);
            while(true){object current;try{if(!run.MoveNext())break;current=run.Current;}catch(Exception e){File.WriteAllText(Path.Combine(folder,"FAILED.txt"),e.ToString());Debug.LogException(e);Application.Quit(1);yield break;}yield return current;}
        }
        static void Check(bool b,string m){if(!b)throw new Exception(m);}
        IEnumerator Capture(string folder,string name){yield return null;yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(folder,name+".png"),t.EncodeToPNG());Destroy(t);}
        IEnumerator Run(string folder)
        {
            yield return null;var screen=GetComponent<VideoScreen>();screen.enabled=false;screen.Play();var c=screen.Controller;c.enabled=false;yield return null;
            var frozen=c.Characters.Single(a=>a.SourceLane==1 && a.SourceOrder==0);var second=c.Characters.Single(a=>a.SourceLane==2 && a.SourceOrder==0);
            var view=frozen.GetComponent<CharacterFreezeView>();
            Check(view.IceVisible && view.Counter.text=="BĂNG 1" && !frozen.AmmoLabel.gameObject.activeSelf,"Ice presentation missing");
            Check(view.Shell.sharedMaterial.shader.isSupported,"Ice shader unsupported");
            Check(!c.SelectActor(frozen) && frozen.Shooter.FreezeRemaining==1,"Frozen actor accepted click");
            yield return Capture(folder,"01-frozen");
            Check(c.SelectLane(0),"First ordinary actor failed");Check(!frozen.Shooter.IsFrozen && second.Shooter.FreezeRemaining==1,"Incorrect thaw counters");
            for(int i=0;i<60;i++){c.Advance(.03f);yield return null;}
            yield return new WaitForSeconds(.35f);yield return null;
            Check(!view.IceVisible && frozen.AmmoLabel.gameObject.activeSelf,"Ice did not dissolve");
            yield return Capture(folder,"02-first-thawed");
            foreach(int lane in new[]{1,2,0,1,2})
            {
                Check(c.SelectLane(lane),"Thawed selection failed: "+lane);
                for(int i=0;i<600;i++){c.Advance(.03f);if(i%20==0)yield return null;}
            }
            for(int i=0;i<10000 && c.Game.State==GameState.Playing;i++){c.Advance(.03f);if(i%20==0)yield return null;}
            Check(c.Game.State==GameState.Won && c.Regions.All(r=>r.IsSettled),"Freeze level did not finish");
            yield return Capture(folder,"03-complete");
            c.Restart();c.Advance(0);yield return null;yield return new WaitForEndOfFrame();
            Check(frozen.Shooter.FreezeRemaining==1 && second.Shooter.FreezeRemaining==2 && view.IceVisible,"Restart did not restore ice");
            yield return Capture(folder,"04-restarted");
            File.WriteAllText(Path.Combine(folder,"checks.txt"),"PASS: Frozen clicks blocked\nPASS: Ice shader and counters visible\nPASS: Successful admission thaws one layer\nPASS: Shell dissolves and original colour/amount return\nPASS: Six-box replay wins all regions\nPASS: Restart restores ice\n");Application.Quit(0);
        }
    }
}
