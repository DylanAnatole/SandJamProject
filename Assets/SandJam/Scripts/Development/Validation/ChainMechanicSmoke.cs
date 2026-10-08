using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SandJamTest.Scene3D
{
    public sealed class ChainMechanicSmoke : MonoBehaviour
    {
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"--chain-smoke");
            if(index<0 || index+1>=args.Length)yield break;
            string folder=args[index+1];Directory.CreateDirectory(folder);
            var routine=Run(folder);
            while(true)
            {
                object current;
                try { if(!routine.MoveNext())break; current=routine.Current; }
                catch(Exception e) { File.WriteAllText(Path.Combine(folder,"FAILED.txt"),e.ToString());Debug.LogException(e);Application.Quit(1);yield break; }
                yield return current;
            }
        }
        static void Check(bool value,string message){if(!value)throw new Exception(message);}
        IEnumerator Capture(string folder,string name)
        {
            yield return new WaitForEndOfFrame();
            var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToPNG());Destroy(image);
        }
        IEnumerator Run(string folder)
        {
            yield return null;
            var screen=FindObjectOfType<VideoScreen>();var controller=screen.Controller;
            screen.enabled=false;screen.Play();controller.enabled=false;
            yield return null;
            Check(FindObjectsOfType<ChainLinkView>().Length==1,"Missing or duplicate chain");
            yield return Capture(folder,"01-pair-ready");
            var chain=FindObjectOfType<ChainLinkView>();
            Check(chain.IsInQueue && chain.LoopsVisible,"Queue chain is not visible");
            var right=controller.Characters.Single(a=>a.SourceLane==1);
            Check(controller.SelectActor(right),"Right endpoint cannot select pair");
            Check(controller.Game.Slots.Count(s=>s!=null)==2 && controller.Game.Moves==1,"Pair not selected atomically");
            Check(!chain.IsInQueue && !chain.LoopsVisible,"Chain must disappear immediately on selection");
            var first=controller.Characters.Single(a=>a.SourceLane==0);
            var a=first.transform.position;var b=right.transform.position;
            controller.Advance(.2f);
            Check(first.transform.position!=a && right.transform.position!=b,"Both actors must move");
            Check(controller.Game.SpentAmmo==0,"Pair fired before arrival");
            yield return null;yield return Capture(folder,"02-pair-moving");
            Check(!chain.LoopsVisible,"Chain reappeared during movement");
            controller.Advance(.45f);
            yield return Capture(folder,"02b-pair-pouring");
            Check(first.AtRest && right.AtRest && !chain.LoopsVisible,"Chain visible at pouring slots");
            for(int i=0;i<20000 && controller.Game.State==GameState.Playing;i++)
            {controller.Advance(.03f);if(i%20==0)yield return null;}
            Check(controller.Game.State==GameState.Won,"Linked pair did not complete board");
            Check(controller.Game.Slots.All(s=>s==null),"Pair did not release slots");
            for(int i=0;i<15;i++)controller.Advance(.03f);
            yield return null;yield return Capture(folder,"03-pair-complete");
            controller.Restart();controller.Advance(0);yield return null;
            Check(FindObjectsOfType<ChainLinkView>().Length==1,"Restart duplicated chain");
            chain=FindObjectOfType<ChainLinkView>();
            Check(chain.IsInQueue && chain.LoopsVisible,"Restart did not restore queue chain");
            Check(controller.SelectActor(first),"Left endpoint cannot select after restart");
            File.WriteAllText(Path.Combine(folder,"checks.txt"),"PASS: Chain visible only in queue\nPASS: Chain hidden immediately on selection, during movement and at pouring slots\nPASS: Either endpoint selects both\nPASS: Both move before pouring\nPASS: Full board completed\nPASS: Slots released\nPASS: Restart restores one visible queue chain\n");
            Application.Quit(0);
        }
    }
}
