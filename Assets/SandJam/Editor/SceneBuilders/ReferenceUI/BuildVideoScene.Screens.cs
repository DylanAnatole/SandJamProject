using UnityEngine;
using UnityEditor;
using SandJamTest.Scene3D;

namespace SandJamTest.Editor
{
    public static partial class BuildVideoScene
    {
        static void Panel(Transform parent,string name,float x,float y,float w,float h,string color,float z=-6)
        { Image(name,rounded,parent,x,y,w,h,Hex(color),z,true); }
        static void Picture(Transform parent,string asset,float x,float y,float w,float h,float z=-7)
        { Image(asset,Asset(asset),parent,x,y,w,h,Color.white,z); }
        static void Label(Transform parent,string text,float x,float y,float size,float z=-8)
        { Text(text,text,parent,x,y,size,Color.white,z); }
        static void Button(Transform parent,string label,string action,float x,float y,float w,float h,string asset="button-green-v2")
        {
            var t=Group(label,parent);Picture(t,asset,x,y,w,h);Label(t,label,x,y,32);
            if(string.IsNullOrEmpty(action)){Placeholder(t,label+" - UI only",label,0);return;}
            var hit=Group("Hit area - "+action,t);hit.position=P(x,y,-9);hit.gameObject.layer=9;
            var collider=hit.gameObject.AddComponent<BoxCollider>();collider.size=new Vector3(w*.01f,h*.01f,.1f);
            hit.gameObject.AddComponent<VideoUiButton>().Action=action;
        }
        static void Wallet(Transform t)
        {
            Picture(t,"bg-currency",401,86,123,33);Picture(t,"button-plus_0",352,86,32,32,-7.1f);
            Picture(t,"icon-coin",450,86,40,40,-7.1f);Label(t,"2040",410,86,21);
        }
        static void ComposeScreens(Transform root,VideoScreen screen,SandJamSceneController controller)
        {
            var play=Group("Gameplay - Level 147",root);
            foreach(var child in new[]{"Backdrop and frame","HUD - visual controls only","Stash - 5 positions","Pop Art face - grid texture","Characters - copied original mesh","Lane 1","Lane 2","Lane 3","Pooled sand projectiles"})
            {var t=root.Find(child);if(t)t.SetParent(play,true);}
            screen.Gameplay=play.gameObject;
            // Original ordinary JSON supplies the playable model; secret flags drive covers only.
            controller.AmmoPerShot=10;
            controller.EnableSlotUnlocks=true;
            var slotLock=Group("Fifth waiting slot - locked until 300",play);screen.FifthSlotLock=slotLock.gameObject;
            Image("Locked pad",pad,slotLock,371,687,63,43,Hex("828387"),-.5f,true);
            Picture(slotLock,"icon-lock",371,674,17,18,-.6f);
            screen.FifthSlotRemaining=Text("Unlock remaining","300",slotLock,371,696,19,Color.white,-.7f);
            var home=Group("Home screen",root);screen.Home=home.gameObject;
            Picture(home,"generalBackground-main",241.5f,425,483,680,-5);
            Panel(home,"Lower stage",241.5f,875,510,480,"234C68",-5.1f);
            Panel(home,"Walkway",241.5f,682,510,65,"7DB8DF",-5.2f);
            for(int i=0;i<7;i++)Panel(home,"Paving joint",i*90,682,2,64,"487C9C",-5.3f);
            Panel(home,"Walkway edge",241.5f,714,510,5,"173E5B",-5.3f);
            Panel(home,"Top bar",241.5f,69,510,119,"2C6084",-5.2f);
            Picture(home,"icon-settings-v2",42,86,43,43);Wallet(home);
            Picture(home,"bg-currency",245,86,125,33);Picture(home,"button-plus_0",193,86,32,32,-7.1f);
            Label(home,"Full",244,86,18);Picture(home,"icon-heart-big",296,86,37,40,-8);Label(home,"5",296,85,20,-8.1f);
            Panel(home,"Hanger left",214,146,8,40,"31648D");Panel(home,"Hanger right",269,146,8,40,"31648D");
            Panel(home,"Level sign shadow",241.5f,279,181,229,"254B70");Panel(home,"Level sign rim",241.5f,273,169,214,"9CDAF2",-6.1f);
            Panel(home,"Level sign blue",241.5f,273,154,199,"437DB7",-6.2f);Panel(home,"Sign inset",241.5f,255,138,136,"559AC9",-6.3f);
            Label(home,"147",241.5f,250,77);Label(home,"LEVEL",241.5f,330,27);
            Picture(home,"Chest_Enable",414,418,84,77);Panel(home,"Join label",414,464,91,28,"C99726",-7.1f);Label(home,"JOIN",414,463,20);
            Button(home,"PLAY","play",241.5f,823,214,78);
            Picture(home,"bg-navbar",241.5f,1005,490,123,-6);
            Picture(home,"bg-navBar-seleckted",241.5f,1007,160,140,-6.1f);
            Picture(home,"icon-shop",80,967,75,76);Picture(home,"icon-home",241.5f,959,91,86);Picture(home,"icon-cards",402,967,78,76);
            Label(home,"SHOP",80,1028,28);Label(home,"HOME",241.5f,1028,28);Label(home,"GALLERY",402,1028,24);
            foreach(var name in new[]{"Settings","Shop","Gallery","Join","Lives","Coins"})Placeholder(home,name+" - UI only",name,0);
            var mascot=Group("Walking sand character",home);mascot.position=P(86,685,-7);
            AnimatedCharacter(mascot,6,.85f,true);
            screen.Mascot=mascot;

            var loading=Group("Loading screen",root);screen.Loading=loading.gameObject;
            Picture(loading,"bg-splashScene",241.5f,537.5f,500,1100,-5);
            Picture(loading,"header-splashScene",241.5f,228,371,237);
            Picture(loading,"image-1-splashScene",241.5f,510,362,342);
            Label(loading,"Loading...",241.5f,701,16);Picture(loading,"bg-fillbar-splashScene",241.5f,738,341,31);
            screen.LoadingFill=Image("Loading fill",Asset("fillbar-splashScene"),loading,241.5f,738,325,19,Color.white,-7.1f).transform;
            Label(loading,"Voodoo",241.5f,831,52);Label(loading,"Entertain the world",241.5f,867,12);
            Picture(loading,"logo",241.5f,957,202,75);Label(loading,"UI reconstruction",413,1050,10);

            var celebration=Group("Celebration screen",root);screen.Celebration=celebration.gameObject;
            Panel(celebration,"Dark backdrop",241.5f,537.5f,540,1180,"19162D",-5);Wallet(celebration);
            Label(celebration,"Superb!",241.5f,569,53);Confetti(celebration);

            var result=Group("Level complete screen",root);screen.Result=result.gameObject;
            Panel(result,"Dark backdrop",241.5f,537.5f,540,1180,"100C20",-5);Wallet(result);Confetti(result);
            Panel(result,"Gold outer border",241.5f,499,437,512,"F4BD17",-6);
            Panel(result,"Gold header",241.5f,307,412,114,"F9C427",-6.1f);
            Picture(result,"bg-popUp-levelCompleted-v2",241.5f,554,410,392,-6.2f);
            Picture(result,"bg-level-yellow",241.5f,239,205,59,-6.4f);
            Text("Level title","Level 147",result,241.5f,236,28,Hex("613F00"),-8,false);
            Label(result,"Level Complete!",241.5f,300,36);
            Picture(result,"bg-popUp-slot-levelCompleted-v2",241.5f,476,355,257,-7);
            Picture(result,"icon-coin-reward",241.5f,444,170,113,-7.1f);Label(result,"+ 10",241.5f,518,53);
            Panel(result,"Reward caption",241.5f,582,339,55,"343572",-7.2f);Label(result,"Your Reward!",241.5f,581,26);
            Button(result,"x2",null,162,672,159,72,"button-yellow-v2");Button(result,"Next","home",339,672,169,72);
            screen.Home.SetActive(true);screen.Gameplay.SetActive(false);screen.Loading.SetActive(false);screen.Celebration.SetActive(false);screen.Result.SetActive(false);
        }
        static void Confetti(Transform parent)
        {
            var particles=Group("Confetti",parent);var random=new System.Random(147);
            string[] colors={"FFD839","31D9F1","FC35BA","8DE443","FA8242"};
            for(int i=0;i<65;i++){var p=Image("Paper "+i,pad,particles,random.Next(0,483),random.Next(0,1075),random.Next(4,9),random.Next(9,18),Hex(colors[i%colors.Length]),-5.6f);p.transform.rotation=Quaternion.Euler(0,0,random.Next(180));}
            particles.gameObject.AddComponent<VideoConfetti>();
        }
    }
}

