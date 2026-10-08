using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
namespace SandJamTest.Scene3D
{
    public sealed partial class SandJamSceneController
    {
        void Require(bool ok,string message){if(!ok)throw new Exception(message);}
        IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();
            Require(image.GetPixels32().Any(p=>p.r>60 && p.g>60),"Black render");
            File.WriteAllBytes(Path.Combine(smokeOutput,name+".png"),image.EncodeToPNG());Destroy(image);
        }
        IEnumerator SmokeTest()
        {
            Directory.CreateDirectory(smokeOutput);yield return null;
            Require(error==null,error??"Initialization failed");Require(Game.Slots.Length==5,"Five slots required");
            var first=Characters.Single(a=>a.SourceLane==0 && a.SourceOrder==0);
            var rear=Characters.Single(a=>a.SourceLane==0 && a.SourceOrder==1);
            Physics.SyncTransforms();RaycastHit hit;var screen=GameCamera.WorldToScreenPoint(first.ClickCollider.bounds.center);
            Require(Physics.Raycast(GameCamera.ScreenPointToRay(screen),out hit,100,1<<8) && hit.collider==first.ClickCollider,"Front raycast failed");
            Require(!SelectActor(rear),"Rear actor selectable");Require(first.AmmoLabel.text=="20" && Regions[0].Counter.text=="59" && DisplayAmount.Units(Game.Remaining,level.uiDivider)==225,"Display units wrong");
            foreach(var view in Regions.Where(v=>!Game.Regions[v.PartIndex].Open))
            {
                var part=Game.Regions[view.PartIndex].Data;
                Require(!view.Counter.gameObject.activeSelf && view.Counter.text=="","Locked number leaked");
                Require(boardTexture.ReadPixel(part.cols[0],part.rows[0]).Equals((Color32)new Color(.88f,.93f,.95f)),"Locked color leaked");
            }
            Require(boardTexture.Texture.width==84 && boardTexture.Texture.height==112 && boardTexture.Renderer.sprite.vertices.Length==4 && boardTexture.Texture.filterMode==FilterMode.Point,"Sprite texture invalid");
            Require(Regions.All(v=>!v.Geometry.GetComponent<MeshRenderer>().enabled),"Mesh still enabled");
            yield return Capture("01-scene-start");Require(boardTexture.UploadCount>0,"Texture upload missing");
            Require(SelectActor(first),"Selection failed");Advance(.08f);Require(Game.SpentAmmo==0,"Fired during movement");
            for(int i=0;i<12;i++){Advance(.08f);yield return null;}
            Require(Game.SpentAmmo>0 && Regions.Any(r=>r.FlyingCount>0),"No sand flow");yield return Capture("02-scene-shooting");
            int guard=0;
            while(Game.State==GameState.Playing && guard++<1600)
            {
                if(!Game.Slots.Any(s=>Game.Target(s)>=0)){int lane=Game.HintLane();if(lane>=0)SelectLane(lane);}
                var locked=Game.Regions.Where(r=>!r.Open).ToArray();Advance(.08f);foreach(var view in Regions)view.ValidateFlow();
                foreach(var opened in locked.Where(r=>r.Open))Require(Game.Regions.Any(r=>opened.Data.prerequests.Contains(r.Data.name) && r.Remaining==0 && Regions[Array.IndexOf(level.parts,r.Data)].SettledCount==r.Data.rows.Length),"Early unlock");
                yield return null;
            }
            Require(Game.State==GameState.Won && Game.SpentAmmo==9000,"Win failed");
            for(int i=0;i<40;i++){Advance(.08f);yield return null;}
            Require(Regions.All(r=>r.IsSettled) && Regions.Sum(r=>r.SettledCount)==9077,"Incomplete coverage");yield return Capture("03-scene-win");
            Restart();Advance(0);Require(Characters.All(a=>a.gameObject.activeSelf) && Game.SpentAmmo==0,"Restart failed");
            foreach(int lane in new[]{0,1,0,1,2,1,1,1}){SelectLane(lane);for(int i=0;i<52;i++){Advance(.08f);yield return null;}}
            Require(Game.State==GameState.Lost,"Lose failed");yield return Capture("04-scene-lose");
            Restart();Advance(0);Screen.SetResolution(1000,700,false);for(int i=0;i<4;i++)yield return null;
            FitCamera();Physics.SyncTransforms();screen=GameCamera.WorldToScreenPoint(first.ClickCollider.bounds.center);
            Require(Physics.Raycast(GameCamera.ScreenPointToRay(screen),out hit,100,1<<8) && hit.collider==first.ClickCollider,"Resize raycast failed");yield return Capture("05-scene-resized");
            File.WriteAllText(Path.Combine(smokeOutput,"scene-tests.txt"),"PASS: 5 slots, selection and raycast\nPASS: Display units and locked regions\nPASS: Shared texture sprite and disabled meshes\nPASS: Shooting after movement\nPASS: Flow occupancy and delayed unlock\nPASS: Win 9000 ammo / 9077 grains\nPASS: Restart and blocked stash loss\nPASS: Resize and screenshots\n");Application.Quit(0);
        }
        IEnumerator GuardSmoke(IEnumerator test)
        {
            var stack=new Stack<IEnumerator>();stack.Push(test);
            while(stack.Count>0)
            {
                object current=null;Exception failure=null;bool moved=false;
                try{moved=stack.Peek().MoveNext();if(moved)current=stack.Peek().Current;}catch(Exception ex){failure=ex;}
                if(failure!=null){Directory.CreateDirectory(smokeOutput);File.WriteAllText(Path.Combine(smokeOutput,"FAILED.txt"),failure.ToString());Debug.LogException(failure);Application.Quit(1);yield break;}
                if(!moved){stack.Pop();continue;}if(current is IEnumerator)stack.Push((IEnumerator)current);else yield return current;
            }
        }
    }
}
