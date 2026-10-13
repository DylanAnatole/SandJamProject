using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SandJamTest.Scene3D
{
    public sealed class BoosterSmoke : MonoBehaviour
    {
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"--booster-smoke");
            if(index<0 || index+1>=args.Length)yield break;
            string folder=args[index+1];Directory.CreateDirectory(folder);var routine=Run(folder);
            while(true)
            {
                object current;
                try {if(!routine.MoveNext())break;current=routine.Current;}
                catch(Exception e){File.WriteAllText(Path.Combine(folder,"FAILED.txt"),e.ToString());Debug.LogException(e);Application.Quit(1);yield break;}
                yield return current;
            }
        }
        static void Check(bool value,string text){if(!value)throw new Exception(text);}
        IEnumerator Capture(string folder,string name)
        {
            yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToPNG());Destroy(image);
        }
        IEnumerator Run(string folder)
        {
            yield return null;
            var screen=GetComponent<VideoScreen>();var c=screen.Controller;screen.enabled=false;screen.Play();c.enabled=false;
            yield return null;Physics.SyncTransforms();
            foreach(var action in new[]{"rocket","swap","select"})
            {
                var button=FindObjectsOfType<VideoUiButton>().Single(b=>b.Action=="booster:"+action);
                RaycastHit hit;
                Check(Physics.Raycast(screen.UiCamera.ScreenPointToRay(screen.UiCamera.WorldToScreenPoint(button.transform.position)),out hit,100,1<<9) && hit.collider.GetComponent<VideoUiButton>()==button,"Booster button cannot be clicked: "+action);
            }
            yield return Capture(folder,"01-buttons");
            var game=c.Game;var oldOpen=game.Regions.Select(r=>r.Open).ToArray();
            Check(screen.Boosters.Activate("rocket"),"Rocket rejected");
            yield return new WaitForSecondsRealtime(.18f);yield return Capture(folder,"02-rocket");
            for(int frame=0;screen.Boosters.Busy && frame<180;frame++)yield return null;
            Check(!screen.Boosters.Busy && game.Regions.Count(r=>r.Revealed)==1,"Rocket did not reveal exactly one region");
            int revealed=Array.FindIndex(game.Regions,r=>r.Revealed);
            var counter=c.Regions[revealed].Counter;
            File.WriteAllText(Path.Combine(folder,"revealed-region.txt"),"Region: "+revealed+"\nText: "+counter.text+"\nPosition: "+counter.transform.position+"\nScale: "+counter.transform.lossyScale+"\nScreen: "+c.GameCamera.WorldToScreenPoint(counter.transform.position)+"\nRenderer enabled: "+counter.GetComponent<Renderer>().enabled+"\nSorting order: "+counter.GetComponent<Renderer>().sortingOrder);
            Check(game.Regions.Select(r=>r.Open).SequenceEqual(oldOpen) && c.Regions[revealed].Counter.gameObject.activeSelf && c.Regions[revealed].Counter.text.Length>0,"Reveal did not preserve lock or show counter");
            yield return Capture(folder,"03-revealed");
            var queues=game.Lanes.Select(l=>l.ToArray()).ToArray();
            Check(screen.Boosters.Activate("swap"),"Swap rejected");
            for(int i=0;i<game.Lanes.Length;i++)Check(game.Lanes[i].Peek()==queues[i][1],"Wrong row swapped");
            c.Advance(.65f);yield return Capture(folder,"04-swapped");
            Check(screen.Boosters.Activate("select") && screen.Boosters.PickerOpen && c.BoosterInputBlocked,"Picker did not open and block input");
            yield return Capture(folder,"05-picker");
            var candidate=game.Lanes.SelectMany(l=>l.Skip(1)).First(game.CanSelectPriority);
            Check(screen.Boosters.Choose(candidate) && game.Slots.Contains(candidate),"Cannot choose a deep matching color");
            Check(!screen.Boosters.PickerOpen && !c.BoosterInputBlocked,"Picker did not close");
            c.Advance(.65f);yield return Capture(folder,"06-priority");
            screen.Play();c.enabled=false;yield return null;
            Check(c.Game.Regions.All(r=>!r.Revealed) && !c.BoosterInputBlocked,"Restart did not clear boosters");
            Check(screen.Boosters.Activate("select"),"Cannot reopen picker");screen.Boosters.Cancel();
            Check(!c.BoosterInputBlocked && c.Game.Moves==0,"Cancel selected a shooter");
            File.WriteAllText(Path.Combine(folder,"checks.txt"),"PASS: All three HUD button hit areas work\nPASS: Rocket reveals one region without unlocking\nPASS: Counter appears on revealed locked region\nPASS: First two rows swap across three lanes\nPASS: Priority picker opens and lists deep matching colors\nPASS: Selected deep shooter enters stash\nPASS: Cancel and restart clear booster state\n");
            Application.Quit(0);
        }
    }
}
