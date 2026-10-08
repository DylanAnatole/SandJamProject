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
            Label(new Rect(25,47,330,19),"MÀN 01  /  BỨC TRANH ĐẦU TIÊN",10,Accent);
            if(Button(new Rect(356,20,103,33),muted?"Âm: Tắt":"Âm: Bật")){muted=!muted;sound.mute=muted;}
            if(Button(new Rect(469,20,107,33),"Chơi lại  ↻")) Restart();
            if(error!=null){Label(new Rect(40,120,520,200),error,18,Color.red);return;}
            if(Game==null)return;
            float progress=1-(float)Game.Remaining/Game.TotalRequired;
            Panel(new Rect(24,72,552,5),new Color(.80f,.86f,.89f));
            if(progress>0)Panel(new Rect(24,72,552*progress,5),Accent);
            Label(new Rect(26,108,116,25),"BẢNG MÀU",10,Accent);
            string[] names={"ĐỎ","XANH LÁ","XANH DƯƠNG","VÀNG","TÍM"};
            for(int i=0;i<Game.Regions.Length;i++)
            {
                var region=Game.Regions[i];float y=154+i*58;
                Panel(new Rect(26,y+3,9,9),region.Open?ColorPalette.Of(region.Data.ColorType):Color.gray);
                Label(new Rect(42,y-3,106,23),region.Open?names[i]:"CHƯA MỞ",9,Ink);
                Label(new Rect(42,y+19,105,22),region.Remaining==0?(Regions[i].IsSettled?"Hoàn tất":"Đang phủ"):region.Open?"Đang mở":"Chưa mở",9,region.Open?Accent:Color.gray);
            }
            Label(new Rect(467,108,120,25),"TIẾN ĐỘ",10,Accent);
            Label(new Rect(467,138,120,39),Mathf.FloorToInt(progress*100)+"%",28,Ink);
            Label(new Rect(467,220,114,50),"Còn "+DisplayAmount.Units(Game.Remaining,level.uiDivider)+"\nđơn vị màu",12,Ink);
            Label(new Rect(467,327,110,79),"Lấp đầy vùng đang mở để mở thêm màu.",12,Accent);
            Label(new Rect(28,493,430,23),"Ô CHỜ",11,Ink);
            Label(new Rect(448,493,128,23),Game.Slots.Count(s=>s!=null)+" / "+Game.Slots.Length+" vị trí",10,Accent,TextAnchor.MiddleRight);
            for(int i=0;i<3;i++)
            {
                var point=GameCamera.WorldToViewportPoint(LaneStarts[i].position+Vector3.up*1f);
                var rect=new Rect(point.x*600-66,(1-point.y)*1000-14,132,24);
                if(hintLane==i && Time.unscaledTime<hintTime)Panel(rect,new Color(.63f,.87f,.76f));
                Label(rect,"HÀNG "+(i+1)+"  ·  "+Game.Lanes[i].Count,10,Accent,TextAnchor.MiddleCenter);
            }
            Panel(new Rect(24,914,552,32),new Color(.86f,.93f,.92f));
            Label(new Rect(35,918,530,24),Time.unscaledTime<messageTime?message:"Bấm đầu hàng hoặc phím 1 / 2 / 3 để chọn nhân vật.",10,Accent,TextAnchor.MiddleCenter);
            if(Button(new Rect(24,958,128,31),"Gợi ý")){hintLane=Game.HintLane();hintTime=Time.unscaledTime+4;Notify(hintLane<0?"Đợi nhân vật hoàn thành vùng đang mở.":"Thử hàng "+(hintLane+1)+".");}
            if(Button(new Rect(164,958,125,31),fast?"Tốc độ ×2":"Tốc độ ×1"))fast=!fast;
            if(Button(new Rect(301,958,131,31),paused?"Tiếp tục":"Tạm dừng"))paused=!paused;
            Label(new Rect(443,958,136,31),"R: chơi lại",10,Ink,TextAnchor.MiddleRight);
            bool transitioning=Characters.Any(c=>c.gameObject.activeSelf && !c.AtRest);
            if(Game.State!=GameState.Playing && !transitioning && Regions.All(r=>r.IsSettled))EndOverlay();
            else if(paused)
            {
                Panel(new Rect(0,80,600,825),new Color(.08f,.16f,.22f,.73f));
                Label(new Rect(70,395,460,75),"ĐÃ TẠM DỪNG",29,Color.white,TextAnchor.MiddleCenter);
                if(Button(new Rect(200,497,200,46),"Tiếp tục"))paused=false;
            }
        }
        void EndOverlay()
        {
            bool won=Game.State==GameState.Won;
            Panel(new Rect(0,0,600,1000),new Color(.07f,.14f,.20f,.73f));Panel(new Rect(65,325,470,340),Color.white);
            Label(new Rect(95,345,410,35),won?"BỨC TRANH ĐÃ HOÀN THÀNH":"THỬ MỘT THỨ TỰ KHÁC",11,Accent,TextAnchor.MiddleCenter);
            Label(new Rect(95,394,410,56),won?"Tuyệt vời!":"Hết chỗ chờ",34,Ink,TextAnchor.MiddleCenter);
            Label(new Rect(95,465,410,65),won?"Đã dùng đủ "+DisplayAmount.Units(Game.TotalRequired,level.uiDivider)+" đơn vị màu qua "+Game.Moves+" lượt chọn.":"Các nhân vật đang đợi màu chưa mở.\nHãy ưu tiên màu của vùng đang mở.",16,Ink,TextAnchor.MiddleCenter);
            var rect=new Rect(167,558,266,78);GUI.DrawTexture(rect,PlayButtonTexture,ScaleMode.StretchToFill);
            Label(new Rect(167,558,266,65),"Chơi lại màn này",15,Ink,TextAnchor.MiddleCenter);
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

