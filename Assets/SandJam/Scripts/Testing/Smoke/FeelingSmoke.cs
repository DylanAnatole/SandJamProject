using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SandJamTest.Scene3D
{
    public sealed class FeelingSmoke : MonoBehaviour
    {
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"--feeling-smoke");if(at<0)yield break;
            string folder=args[at+1];Directory.CreateDirectory(folder);var run=Run(folder);
            while(true)
            {
                object current;try{if(!run.MoveNext())break;current=run.Current;}
                catch(Exception e){File.WriteAllText(Path.Combine(folder,"FAILED.txt"),e.ToString());Debug.LogException(e);Application.Quit(1);yield break;}
                yield return current;
            }
        }
        static void Check(bool value,string text){if(!value)throw new Exception(text);}
        IEnumerator Capture(string folder,string name)
        {
            yield return null;yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(folder,name+".png"),t.EncodeToPNG());Destroy(t);
        }
        IEnumerator Run(string folder)
        {
            yield return null;var screen=GetComponent<VideoScreen>();screen.enabled=false;screen.Play();var c=screen.Controller;c.enabled=false;
            var actor=c.Characters.Single(a=>a.SourceLane==0 && a.SourceOrder==0);var scale=actor.Visual.localScale;
            Check(c.SelectLane(0),"Cannot select first actor");
            yield return Capture(folder,"01-full");
            for(int i=0;i<1000 && actor.Shooter.Ammo>actor.Shooter.InitialAmmo/2;i++){c.Advance(.03f);yield return null;}
            Check(actor.DisplayedFill>.3f && actor.DisplayedFill<.75f,"Fill does not follow remaining ammo");
            yield return Capture(folder,"02-half");
            for(int i=0;i<1000 && actor.DisplayedFill>.003f;i++){c.Advance(.03f);yield return null;}
            Check(actor.Departing && actor.DisplayedFill==0 && actor.Visual.localScale==scale,"Empty character shrank or retained sand");
            yield return Capture(folder,"03-empty-exit");
            var style=actor.GetComponent<CharacterDepthStyle>();
            var vessel=actor.GetComponent<CharacterSandVessel>();
            Check(vessel && !actor.Visual.Find("Contained sand volume").gameObject.activeSelf,"Empty vessel retained its sand volume");
            var legMesh=style.Skin.sharedMesh;
            Check(legMesh==Resources.Load<Mesh>("CharacterAnimation/CharacterLegs") && Enumerable.Range(0,legMesh.subMeshCount).Sum(i=>(long)legMesh.GetIndexCount(i))>0,"Recovered leg geometry missing");
            var sourceMesh=Resources.Load<GameObject>("Original/CharacterVisual").GetComponentInChildren<SkinnedMeshRenderer>(true).sharedMesh;
            Check(Enumerable.Range(0,legMesh.subMeshCount).Sum(i=>(long)legMesh.GetIndexCount(i))<Enumerable.Range(0,sourceMesh.subMeshCount).Sum(i=>(long)sourceMesh.GetIndexCount(i)),"Original body still present");
            var properties=new MaterialPropertyBlock();style.Skin.GetPropertyBlock(properties);
            Check(properties.GetFloat("_FillAmount")==0 && properties.GetColor("_OutlineColor")==style.Skin.sharedMaterial.GetColor("_Color"),"Empty shader did not preserve original rim colour");
            for(int i=0;i<100 && actor.gameObject.activeSelf;i++){c.Advance(.03f);yield return null;}
            Check(!actor.gameObject.activeSelf && Mathf.Abs(actor.transform.position.x)>5,"Character did not walk off screen");
            Check(c.SelectLane(1),"Cannot fill remaining purple region");
            for(int i=0;i<2000 && !c.Regions.Any(r=>r.IsSettled && c.Game.Regions[r.PartIndex].Remaining==0);i++){c.Advance(.03f);yield return null;}
            var region=c.Regions.First(r=>r.IsSettled && c.Game.Regions[r.PartIndex].Remaining==0);var data=c.Game.Regions[region.PartIndex].Data;
            var beforeGlow=(Color)region.Board.ReadPixel(data.cols[0],data.rows[0]);
            for(int i=0;i<20;i++)c.Advance(.03f);
            Check(!region.Counter.gameObject.activeSelf && region.Counter.text=="","Completed region still has a marker");
            var pixel=(Color)region.Board.ReadPixel(data.cols[0],data.rows[0]);
            Check(pixel.g>beforeGlow.g+.04f,"Completed region did not brighten");
            yield return Capture(folder,"04-bright-completion");
            c.Restart();c.Advance(0);yield return null;
            Check(actor.DisplayedFill==1 && actor.Visual.localScale==scale && actor.gameObject.activeSelf && !actor.Departing,"Restart did not restore actor");
            File.WriteAllText(Path.Combine(folder,"checks.txt"),"PASS: Fill follows remaining ammo\nPASS: Empty interior and original colour rim\nPASS: Character leaves screen without shrinking\nPASS: Completed region brightens without checkmark\nPASS: Restart restores colour, scale and state\n");Application.Quit(0);
        }
    }
}
