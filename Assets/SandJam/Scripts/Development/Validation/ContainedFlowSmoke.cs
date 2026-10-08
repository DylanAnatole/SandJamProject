using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
namespace SandJamTest.Scene3D
{
    public sealed class ContainedFlowSmoke : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void StartCheck(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"--contained-smoke")>=0)new GameObject("Contained sand smoke").AddComponent<ContainedFlowSmoke>();}
        IEnumerator Start(){
            var args=Environment.GetCommandLineArgs();string folder=args[Array.IndexOf(args,"--contained-smoke")+1];Directory.CreateDirectory(folder);
            var run=Run(folder);while(true){object next;try{if(!run.MoveNext())break;next=run.Current;}catch(Exception e){File.WriteAllText(Path.Combine(folder,"FAILED.txt"),e.ToString());Debug.LogException(e);Application.Quit(1);yield break;}yield return next;}
        }
        static void Check(bool value,string message){if(!value)throw new Exception(message);}
        IEnumerator Run(string folder){
            yield return null;var screen=FindObjectOfType<VideoScreen>();screen.enabled=false;screen.Play();var c=screen.Controller;c.enabled=false;yield return null;
            foreach(var renderer in FindObjectsOfType<Renderer>())if(renderer.enabled)
                foreach(var mat in renderer.sharedMaterials)Check(mat && mat.shader && mat.shader.isSupported && mat.shader.name!="Hidden/InternalErrorShader","Invalid visible shader on "+renderer.name);
            var region=c.Game.Regions.First(r=>r.Open && r.Data.ColorType==c.Game.Lanes[0].Peek().Color);
            var view=c.Regions.First(r=>c.Game.Regions[r.PartIndex]==region);var board=view.Board;
            var before=new Color32[board.Width*board.Height];var mask=new bool[before.Length];
            for(int i=0;i<region.Data.rows.Length;i++)mask[region.Data.rows[i]*board.Width+region.Data.cols[i]]=true;
            for(int y=0;y<board.Height;y++)for(int x=0;x<board.Width;x++)before[y*board.Width+x]=board.ReadPixel(x,y);
            Check(c.SelectLane(0),"Cannot select first shooter");
            for(int frame=0;frame<70;frame++){
                c.Advance(.03f);foreach(var r in c.Regions)r.ValidateFlow();
                for(int y=0;y<board.Height;y++)for(int x=0;x<board.Width;x++)if(!mask[y*board.Width+x])Check(board.ReadPixel(x,y).Equals(before[y*board.Width+x]),"Sand changed a pixel outside receiving region");
                foreach(var line in c.ProjectileRoot.GetComponentsInChildren<LineRenderer>())if(line.enabled && line.name.StartsWith("Sand stream")){
                    var a=line.GetPosition(0);var b=line.GetPosition(2);
                    Check(Vector3.Distance(a,view.Target.position)<.0001f && Mathf.Abs(a.x-b.x)<.0001f && a.y>b.y,"Stream is not vertical from region inlet");
                    var properties=new MaterialPropertyBlock();line.GetPropertyBlock(properties);Check(properties.GetTexture("_RegionMask")==view.FlowMask,"Missing region stream mask");
                }
                yield return null;
                if(frame==35){yield return new WaitForEndOfFrame();var shot=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(folder,"pouring.png"),shot.EncodeToPNG());Destroy(shot);}
            }
            var beam=c.ProjectileRoot.GetComponentsInChildren<LineRenderer>().First(l=>l.name=="Sand shot slot 1");
            Check(beam.enabled && Vector3.Distance(beam.GetPosition(2),view.Target.position)<.0001f,"Shot does not reach correct region");
            Check(Vector3.Distance(beam.GetPosition(0),view.Target.position)>.1f,"Shot beam missing");
            Check(view.SettledCount>0,"No sand settled");c.Restart();c.Advance(0);yield return null;
            Check(c.ProjectileRoot.GetComponentsInChildren<LineRenderer>().All(l=>!l.enabled),"Restart left stream active");
            File.WriteAllText(Path.Combine(folder,"checks.txt"),"PASS: All visible shaders supported\nPASS: Every moving grain inside its region, no overlap\nPASS: No pixel outside receiving region changed during 70 frames\nPASS: Vertical stream starts at region inlet and uses region mask\nPASS: Visible shot reaches the matching region inlet\nPASS: Partial sand settles and restart clears flow\n");Application.Quit(0);
        }
    }
}
