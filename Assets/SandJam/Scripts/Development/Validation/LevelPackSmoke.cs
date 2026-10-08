using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
namespace SandJamTest.Scene3D
{
    public sealed class LevelPackSmoke : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--levelpack-smoke")<0)return;
            var obj=new GameObject("Level pack validation");DontDestroyOnLoad(obj);obj.AddComponent<LevelPackSmoke>();
        }
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"--levelpack-smoke");string folder=args[at+1];Directory.CreateDirectory(folder);var run=Run(folder);
            while(true){object current;try{if(!run.MoveNext())break;current=run.Current;}catch(Exception e){File.WriteAllText(Path.Combine(folder,"FAILED.txt"),e.ToString());Debug.LogException(e);Application.Quit(1);yield break;}yield return current;}
        }
        static void Check(bool b,string m){if(!b)throw new Exception(m);}
        IEnumerator Capture(string folder,string name){yield return null;yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(folder,name+".png"),t.EncodeToPNG());Destroy(t);}
        IEnumerator Run(string folder)
        {
            yield return null;
            var initial=FindObjectOfType<LevelManager>();Check(initial && initial.Index==0,"Pack did not start at level 1");
            foreach(var entry in initial.Catalog.Levels)initial.Progress.Clear(entry.Number);
            for(int index=0;index<3;index++)
            {
                yield return null;yield return null;
                var manager=FindObjectOfType<LevelManager>();Check(manager && manager.Index==index,"Next loaded wrong scene");
                var screen=manager.Screen;screen.enabled=false;screen.Show(VideoScreen.Page.Home);yield return Capture(folder,"level-"+(index+1)+"-home");
                Check(screen.Home.GetComponentsInChildren<VideoUiButton>(true).Count(b=>b.Action.StartsWith("level:"))==3,"Level selector missing");
                screen.Play();var c=manager.Controller;c.enabled=false;yield return null;
                Check(!manager.RecordCompletion() && !manager.LoadNext(),"Unfinished level advanced or saved");
                Check(!manager.Select(-1) && !manager.Select(3),"Invalid index accepted");
                if(index>0)Check(manager.Progress.IsCompleted(index),"Saved completion was lost between scenes");
                yield return Capture(folder,"level-"+(index+1)+"-start");
                var replay=JsonUtility.FromJson<LevelReplay>(manager.Current.TestReplay.text);
                foreach(int lane in replay.lanes)
                {
                    Check(c.SelectLane(lane),"Selection failed at level "+(index+1));int steps=0;
                    do{
                        for(int i=0;i<20;i++){c.Advance(.03f);steps++;}
                        yield return null;Check(steps<50000,"Simulation stalled");
                    }while(c.Game.Slots.Any(s=>c.Game.Target(s)>=0) || c.Regions.Any(r=>r.FlyingCount>0) || c.Game.Regions.Select((r,i)=>r.Remaining==0 && !c.Regions[i].IsSettled).Any(x=>x));
                    Check(c.Game.State!=GameState.Lost,"Replay lost");
                }
                c.Advance(.1f);Check(c.Game.State==GameState.Won && c.Regions.All(r=>r.IsSettled),"Board did not finish");
                Check(manager.RecordCompletion() && manager.Progress.IsCompleted(index+1),"Win not persisted");
                Check(new LevelProgressManager("smoke."+manager.Catalog.ProgressId).IsCompleted(index+1),"Progress reload failed");
                yield return Capture(folder,"level-"+(index+1)+"-complete");
                if(index<2)Check(manager.LoadNext(),"Next failed");
                else {Check(!manager.LoadNext() && screen.Current==VideoScreen.Page.Home,"Last level did not return Home");c.Restart();c.Advance(0);Check(c.Game.State==GameState.Playing && manager.Progress.IsCompleted(3),"Restart broke progression");}
            }
            var last=FindObjectOfType<LevelManager>();foreach(var entry in last.Catalog.Levels)last.Progress.Clear(entry.Number);
            File.WriteAllText(Path.Combine(folder,"checks.txt"),"PASS: All three original levels render and win\nPASS: Home offers three level buttons\nPASS: Unfinished levels cannot advance/save\nPASS: Invalid selection rejected\nPASS: Next loads levels 2 and 3\nPASS: Completion persists across scene loads\nPASS: Last level returns Home\nPASS: Restart preserves completed progress\n");Application.Quit(0);
        }
    }
}
