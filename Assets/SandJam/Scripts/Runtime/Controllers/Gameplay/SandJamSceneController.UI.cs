using System.Linq;
using UnityEngine;
namespace SandJamTest.Scene3D
{
    public sealed partial class SandJamSceneController
    {
        void OnGUI()
        {
            if(HidePrototypeHud) return;
            float scale=Mathf.Min(Screen.width/600f,Screen.height/1000f);
            GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-600*scale)/2,(Screen.height-1000*scale)/2,0),Quaternion.identity,Vector3.one*scale);
            if(textStyle==null) textStyle=new GUIStyle(GUI.skin.label){font=InterfaceFont,wordWrap=true,padding=new RectOffset(0,0,0,0)};
            Label(new Rect(24,13,330,34),"SAND JAM",27,Ink);
            Label(new Rect(25,47,330,19),"LEVEL 01  /  FIRST PICTURE",10,Accent);
            if(Button(new Rect(356,20,103,33),muted?"Sound: Off":"Sound: On")){muted=!muted;sound.mute=muted;}
            if(Button(new Rect(469,20,107,33),"Retry  ↻")) Restart();
            if(error!=null){Label(new Rect(40,120,520,200),error,18,Color.red);return;}
            if(Game==null)return;
            float progress=1-(float)Game.Remaining/Game.TotalRequired;
            Panel(new Rect(24,72,552,5),new Color(.80f,.86f,.89f));
            if(progress>0)Panel(new Rect(24,72,552*progress,5),Accent);
            Label(new Rect(26,108,116,25),"PALETTE",10,Accent);
            string[] names={"RED","GREEN","BLUE","YELLOW","PURPLE"};
            for(int i=0;i<Game.Regions.Length;i++)
            {
                var region=Game.Regions[i];float y=154+i*58;
                Panel(new Rect(26,y+3,9,9),region.Open?ColorPalette.Of(region.Data.ColorType):Color.gray);
                Label(new Rect(42,y-3,106,23),region.Open?names[i]:"LOCKED",9,Ink);
                Label(new Rect(42,y+19,105,22),region.Remaining==0?(Regions[i].IsSettled?"Done":"Filling"):region.Open?"Open":"Locked",9,region.Open?Accent:Color.gray);
            }
            Label(new Rect(467,108,120,25),"PROGRESS",10,Accent);
            Label(new Rect(467,138,120,39),Mathf.FloorToInt(progress*100)+"%",28,Ink);
            Label(new Rect(467,220,114,50),""+DisplayAmount.Units(Game.Remaining,level.uiDivider)+"\ncolor units",12,Ink);
            Label(new Rect(467,327,110,79),"Fill the open areas to unlock more colors.",12,Accent);
            Label(new Rect(28,493,430,23),"STASH",11,Ink);
            Label(new Rect(448,493,128,23),Game.Slots.Count(s=>s!=null)+" / "+Game.Slots.Length+" slots",10,Accent,TextAnchor.MiddleRight);
            for(int i=0;i<3;i++)
            {
                var point=GameCamera.WorldToViewportPoint(LaneStarts[i].position+Vector3.up*1f);
                var rect=new Rect(point.x*600-66,(1-point.y)*1000-14,132,24);
                if(hintLane==i && Time.unscaledTime<hintTime)Panel(rect,new Color(.63f,.87f,.76f));
                Label(rect,"LANE "+(i+1)+"  ·  "+Game.Lanes[i].Count,10,Accent,TextAnchor.MiddleCenter);
            }
            Panel(new Rect(24,914,552,32),new Color(.86f,.93f,.92f));
            Label(new Rect(35,918,530,24),Time.unscaledTime<messageTime?message:"Tap a lane or press 1 / 2 / 3 to pick a cube.",10,Accent,TextAnchor.MiddleCenter);
            if(Button(new Rect(24,958,128,31),"Hint")){hintLane=Game.HintLane();hintTime=Time.unscaledTime+4;Notify(hintLane<0?"Wait for the cubes to finish the open area.":"Try lane "+(hintLane+1)+".");}
            if(Button(new Rect(164,958,125,31),fast?"Speed ×2":"Speed ×1"))fast=!fast;
            if(Button(new Rect(301,958,131,31),paused?"Continue":"Pause"))paused=!paused;
            Label(new Rect(443,958,136,31),"R: retry",10,Ink,TextAnchor.MiddleRight);
            bool transitioning=Characters.Any(c=>c.gameObject.activeSelf && !c.AtRest);
            if(Game.State!=GameState.Playing && !transitioning && Regions.All(r=>r.IsSettled))EndOverlay();
            else if(paused)
            {
                Panel(new Rect(0,80,600,825),new Color(.08f,.16f,.22f,.73f));
                Label(new Rect(70,395,460,75),"PAUSED",29,Color.white,TextAnchor.MiddleCenter);
                if(Button(new Rect(200,497,200,46),"Continue"))paused=false;
            }
        }
        void EndOverlay()
        {
            bool won=Game.State==GameState.Won;
            Panel(new Rect(0,0,600,1000),new Color(.07f,.14f,.20f,.73f));Panel(new Rect(65,325,470,340),Color.white);
            Label(new Rect(95,345,410,35),won?"PICTURE COMPLETE":"TRY ANOTHER ORDER",11,Accent,TextAnchor.MiddleCenter);
            Label(new Rect(95,394,410,56),won?"Superb!":"Out of Space",34,Ink,TextAnchor.MiddleCenter);
            Label(new Rect(95,465,410,65),won?"Used "+DisplayAmount.Units(Game.TotalRequired,level.uiDivider)+" color units in "+Game.Moves+" picks.":"The cubes are waiting for locked colors.\nPick colors of the open areas first.",16,Ink,TextAnchor.MiddleCenter);
            var rect=new Rect(167,558,266,78);GUI.DrawTexture(rect,PlayButtonTexture,ScaleMode.StretchToFill);
            Label(new Rect(167,558,266,65),"Retry this level",15,Ink,TextAnchor.MiddleCenter);
            if(Event.current.type==EventType.MouseDown && Event.current.button==0 && rect.Contains(Event.current.mousePosition)){Event.current.Use();Restart();}
        }
        void Panel(Rect r,Color c){GUI.DrawTexture(r,Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,c,0,Mathf.Min(12,r.height/2));}
        void Label(Rect r,string s,int size,Color color,TextAnchor align=TextAnchor.MiddleLeft){textStyle.fontSize=size;textStyle.normal.textColor=color;textStyle.alignment=align;GUI.Label(r,s,textStyle);}
        bool Button(Rect r,string text)
        {
            Panel(r,Color.white);Label(r,text,11,Ink,TextAnchor.MiddleCenter);
            if(Event.current.type==EventType.MouseDown && Event.current.button==0 && r.Contains(Event.current.mousePosition)){Event.current.Use();return true;}return false;
        }
    }
}

