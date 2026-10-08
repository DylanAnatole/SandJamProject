using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
namespace SandJamTest.Scene3D
{
    public sealed class Video201Smoke : MonoBehaviour
    {
        [Serializable] class Replay { public int[] lanes; }
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"--video201-smoke");if(at<0 || at+1>=args.Length)yield break;
            string folder=args[at+1];Directory.CreateDirectory(folder);var run=Run(folder);
            while(true){object current;try{if(!run.MoveNext())break;current=run.Current;}catch(Exception e){File.WriteAllText(Path.Combine(folder,"FAILED.txt"),e.ToString());Debug.LogException(e);Application.Quit(1);yield break;}yield return current;}
        }
        static void Check(bool b,string m){if(!b)throw new Exception(m);}
        IEnumerator Capture(string folder,string name){yield return null;yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(folder,name+".png"),t.EncodeToPNG());Destroy(t);}
        IEnumerator Run(string folder)
        {
            yield return null;var screen=GetComponent<VideoScreen>();screen.enabled=false;screen.Show(VideoScreen.Page.Home);
            yield return Capture(folder,"01-home");screen.Play();var c=screen.Controller;c.enabled=false;yield return null;
            Check(c.Game.Regions.Where(r=>r.Open).Select(r=>r.Remaining/20).OrderBy(x=>x).SequenceEqual(new[]{6,9}),"Initial board differs from video");
            Check(c.Game.SlotRemaining(3)==150 && c.Game.SlotRemaining(4)==250 && c.StashSlots.Length==5,"Incorrect waiting pads");
            Check(FindObjectsOfType<ChainLinkView>().Length==2,"Recovered linked pairs missing");
            yield return Capture(folder,"02-start");
            Check(c.SelectLane(1) && c.SelectLane(1),"Purple then white selection failed");
            for(int i=0;i<40;i++){c.Advance(.03f);yield return null;}
            Check(c.Game.SpentAmmo>0,"White actor did not pour");
            yield return Capture(folder,"03-pouring");
            c.Restart();c.Advance(0);yield return null;
            var replay=JsonUtility.FromJson<Replay>(Resources.Load<TextAsset>("VideoUI/Level201Replay").text);
            foreach(var lane in replay.lanes)
            {
                Check(c.SelectLane(lane),"Replay selection failed: "+lane);
                int steps=0;
                do{
                    for(int i=0;i<20;i++){c.Advance(.03f);steps++;}
                    yield return null;Check(steps<20000,"Sand did not settle");
                }while(c.Game.Slots.Any(s=>c.Game.Target(s)>=0) || c.Regions.Any(r=>r.FlyingCount>0) || c.Game.Regions.Select((r,i)=>r.Remaining==0 && !c.Regions[i].IsSettled).Any(x=>x));
                Check(c.Game.State!=GameState.Lost,"Replay lost");
            }
            c.Advance(.1f);Check(c.Game.State==GameState.Won && c.Regions.All(r=>r.IsSettled),"Board not completed");
            Check(c.Game.SlotRemaining(3)==0 && c.Game.SlotRemaining(4)==0,"Pads never unlocked");
            yield return Capture(folder,"04-complete");screen.Show(VideoScreen.Page.Result);yield return Capture(folder,"05-result");
            screen.Play();c.enabled=false;yield return null;Check(c.Game.SlotRemaining(3)==150 && FindObjectsOfType<ChainLinkView>().Length==2,"Restart did not reset stage");
            File.WriteAllText(Path.Combine(folder,"checks.txt"),"PASS: Level 201 board and exported queues\nPASS: Initial 9/6 regions and 150/250 locks\nPASS: Two same-lane linked pairs\nPASS: Real pouring and full winning replay ("+replay.lanes.Length+" moves)\nPASS: All regions settled and both pads unlocked\nPASS: Restart restored stage\n");Application.Quit(0);
        }
    }
}
