using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SandJamTest.Scene3D
{
    public sealed class OctoberVideoSmoke : MonoBehaviour
    {
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"--october-smoke");if(at<0)yield break;
            string folder=args[at+1];Directory.CreateDirectory(folder);var run=Run(folder);
            while(true)
            {
                object current;try{if(!run.MoveNext())break;current=run.Current;}
                catch(Exception e){File.WriteAllText(Path.Combine(folder,"FAILED.txt"),e.ToString());Debug.LogException(e);Application.Quit(1);yield break;}
                yield return current;
            }
        }
        static void Check(bool value,string message){if(!value)throw new Exception(message);}
        IEnumerator Capture(string folder,string name)
        {
            yield return null;yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToPNG());Destroy(image);
        }
        IEnumerator Run(string folder)
        {
            yield return null;var screen=GetComponent<VideoScreen>();screen.enabled=false;screen.Play();var c=screen.Controller;c.enabled=false;
            Check(c.SelectLane(0),"Cannot select test shooter");
            for(int i=0;i<26;i++){c.Advance(.03f);yield return null;}
            var streams=c.ProjectileRoot.GetComponentsInChildren<LineRenderer>(true);
            Check(streams.Count(s=>s.enabled && s.name.StartsWith("Sand shot"))==1 && streams[0].positionCount==3,"Expected one continuous stream per shooter");
            yield return Capture(folder,"01-continuous-stream");
            screen.Play();c.enabled=false;
            Check(streams.All(s=>!s.enabled),"Restart left a stream visible");
            var thresholds=c.Game.Data.gridSlotNeedAmmoCount;
            c.Game.Data.gridSlotNeedAmmoCount=new[]{0,0,0,150,250};
            screen.Feedback.Refresh(0);
            Check(!screen.Feedback.IsSlotLockedVisible(2) && screen.Feedback.IsSlotLockedVisible(3) && screen.Feedback.IsSlotLockedVisible(4),"Independent locks did not display");
            Check(c.Game.Slots.Length==5,"Slot count changed");
            yield return Capture(folder,"02-independent-locks");
            c.Game.Data.gridSlotNeedAmmoCount=thresholds;
            // Isolated blocked-stash fixture; do not advance actor views for synthetic shooters.
            for(int i=0;i<4;i++)c.Game.Slots[i]=new Shooter(new CharacterData{ColorType=99,AmmoCount=20});
            c.Game.Tick(20,s=>false);Check(c.Game.State==GameState.Lost,"Fixture did not reach failure");
            screen.Feedback.Refresh(.8f);Check(screen.Feedback.FailureVisible,"Failure popup missing");
            yield return Capture(folder,"03-no-space");
            screen.Feedback.Retry();c.enabled=false;screen.Feedback.Refresh(0);
            Check(!screen.Feedback.FailureVisible && c.Game.State==GameState.Playing && c.Game.Slots.All(s=>s==null),"Retry did not restore game");
            screen.Feedback.GoHome();Check(screen.Current==VideoScreen.Page.Home,"Home button failed");
            File.WriteAllText(Path.Combine(folder,"checks.txt"),"PASS: One continuous reusable stream per shooter\nPASS: Restart clears streams\nPASS: Independent fourth and fifth slot locks\nPASS: Exactly five slots\nPASS: Failure popup, retry and home\n");Application.Quit(0);
        }
    }
}
