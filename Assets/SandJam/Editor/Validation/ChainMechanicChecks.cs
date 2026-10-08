using System;
using System.IO;
using System.Linq;

namespace SandJamTest.Editor
{
    public static class ChainMechanicChecks
    {
        static void Check(bool pass, string message) { if (!pass) throw new Exception(message); }
        static LevelData Data(bool sameLane = false)
        {
            var a = new CharacterData { ColorType=8, AmmoCount=20, IsChain=true, IsChainSameLane=sameLane };
            var b = new CharacterData { ColorType=4, AmmoCount=40, IsChain=true, IsChainSameLane=sameLane };
            return new LevelData { rowCount=1, columnCount=2, uiDivider=1, opr=new int[0], opc=new int[0],
                parts=new[] {
                    new PartData { name="black",ColorType=8,amount=20,isOpenedAtStart=true,rows=new[]{0},cols=new[]{0} },
                    new PartData { name="yellow",ColorType=4,amount=40,isOpenedAtStart=true,rows=new[]{0},cols=new[]{1} } },
                laneData = sameLane ? new[] { new LaneData { ColorAmmoDatas=new[]{a,b} } } :
                    new[] { new LaneData { ColorAmmoDatas=new[]{a} },new LaneData { ColorAmmoDatas=new[]{b} } }
            };
        }
        public static void Run()
        {
            foreach (bool same in new[] { false, true })
            {
                var game = new SandGame(Data(same));
                Check(game.SelectLane(same?0:1), "Selecting either end must move the pair");
                Check(game.Moves==1 && game.Lanes.All(l=>l.Count==0) && game.Slots.Count(s=>s!=null)==2,"Pair selection was not atomic");
                Check(game.Slots[0].Color==8 && game.Slots[1].Color==4,"Pair order changed");
                game.Tick(20);
                Check(game.Slots[0]==null && game.Slots[1].Ammo==20,"An emptied partner must free its slot at once");
                game.Tick(20);
                Check(game.State==GameState.Won && game.Slots.All(s=>s==null),"Pair did not finish");
            }
            var blocked=new SandGame(Data(),1);
            Check(!blocked.SelectLane(0) && blocked.Moves==0 && blocked.Lanes.All(l=>l.Count==1),"Insufficient capacity changed queues");
            var lockedData=Data();lockedData.gridSlotNeedAmmoCount=new[]{0,10,0,10,10};
            var locked=new SandGame(lockedData,5,null,true);
            Check(!locked.SelectLane(1) && locked.SpentAmmo==0,"Locked or separated slots accepted a pair");
            var delayedData=Data();
            foreach(var lane in delayedData.laneData)
                lane.ColorAmmoDatas=new[]{new CharacterData{ColorType=8,AmmoCount=1},lane.ColorAmmoDatas[0]};
            var delayed=new SandGame(delayedData);
            Check(delayed.SelectLane(0),"Ordinary leader cannot move");
            Check(!delayed.SelectLane(0) && delayed.Lanes[0].Count==1 && delayed.Lanes[1].Count==2,"Pair moved through a blocking leader");
            var hiddenData=Data();
            hiddenData.laneData[1].ColorAmmoDatas=new[]{new CharacterData{ColorType=4,AmmoCount=1},hiddenData.laneData[1].ColorAmmoDatas[0]};
            // Pair flags must line up in the exported row; malformed exports fail loudly.
            bool rejected=false;try{new SandGame(hiddenData);}catch(ArgumentException){rejected=true;}
            Check(rejected,"Malformed pair accepted");
            Directory.CreateDirectory("TestResults/Chain");
            File.WriteAllText("TestResults/Chain/model-checks.txt","PASS: Cross-lane and same-lane pairs\nPASS: Atomic select and stable left/right order\nPASS: Emptied partner frees its slot at once\nPASS: Insufficient, locked and fragmented slots rejected\nPASS: Partner behind leader blocks entire pair\nPASS: Malformed pair rejected\n");
        }
    }
}
