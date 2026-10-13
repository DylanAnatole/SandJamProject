using System;
using System.IO;
using System.Linq;

namespace SandJamTest.Editor
{
    public static class BoosterChecks
    {
        static void Check(bool value,string message) { if(!value)throw new Exception(message); }
        static CharacterData Character(int color) { return new CharacterData {ColorType=color,AmmoCount=20}; }
        static LevelData Data()
        {
            return new LevelData {rowCount=1,columnCount=3,uiDivider=1,opr=new int[0],opc=new int[0],
                parts=Enumerable.Range(0,3).Select(i=>new PartData{name="region"+i,ColorType=i+1,amount=60,isOpenedAtStart=i!=1,prerequests=i==1?new[]{"region0"}:new string[0],rows=new[]{0},cols=new[]{i}}).ToArray(),
                laneData=new[]{
                    new LaneData {ColorAmmoDatas=new[]{Character(2),Character(1),Character(3)}},
                    new LaneData {ColorAmmoDatas=new[]{Character(3),Character(2),Character(1)}},
                    new LaneData {ColorAmmoDatas=new[]{Character(2),Character(1),Character(3)}}}
            };
        }
        public static void Run()
        {
            var game=new SandGame(Data());
            Check(game.RevealRegion(1) && !game.Regions[1].Open && game.Regions[1].InformationVisible,"Reveal must not unlock");
            Check(!game.RevealRegion(1) && !game.RevealRegion(0) && !game.RevealRegion(-1),"Invalid or duplicate reveal accepted");
            var locked=game.Lanes[0].Peek();
            Check(game.Target(locked)<0 && !game.SelectPriority(locked),"Revealed locked color became eligible");
            Check(game.SelectLane(0),"Cannot park locked color for reveal test");
            int amount=game.Regions[1].Remaining;game.Tick();Check(game.Regions[1].Remaining==amount,"Revealed locked region received sand");
            game=new SandGame(Data());
            var before=game.Lanes.Select(l=>l.ToArray()).ToArray();int supply=game.Supply;
            Check(game.SwapFrontRows(),"Swap failed");
            for(int lane=0;lane<3;lane++)Check(game.Lanes[lane].SequenceEqual(new[]{before[lane][1],before[lane][0],before[lane][2]}),"Swap changed wrong rows");
            Check(game.Supply==supply && game.Moves==0,"Swap changed supply or selection count");
            Check(game.SwapFrontRows() && game.Lanes[0].SequenceEqual(before[0]),"Second swap did not restore order");
            var deep=game.Lanes[1].Last();
            Check(game.SelectPriority(deep) && game.Slots[0]==deep && game.Lanes[1].SequenceEqual(before[1].Take(2)),"Priority did not extract the selected deep shooter");
            Check(game.Supply==supply && !game.SelectPriority(deep),"Priority duplicated or lost ammo");
            var full=new SandGame(Data(),1);Check(full.SelectLane(1),"Slot setup failed");
            var candidate=full.Lanes[0].ElementAt(1);int moves=full.Moves;
            Check(!full.SelectPriority(candidate) && full.Moves==moves && full.Lanes[0].Contains(candidate),"Full stash changed queue");
            var chainedData=Data();
            chainedData.laneData[0].ColorAmmoDatas[1].IsChain=true;
            chainedData.laneData[1].ColorAmmoDatas[1]=Character(3);chainedData.laneData[1].ColorAmmoDatas[1].IsChain=true;
            var chained=new SandGame(chainedData);var a=chained.Lanes[0].ElementAt(1);var b=chained.Lanes[1].ElementAt(1);
            Check(chained.SelectPriority(b) && chained.Slots[0]==a && chained.Slots[1]==b,"Priority broke linked pair");
            var shortData=Data();shortData.laneData[2].ColorAmmoDatas=new[]{Character(1)};
            var shortGame=new SandGame(shortData);var single=shortGame.Lanes[2].Peek();
            Check(shortGame.SwapFrontRows() && shortGame.Lanes[2].Peek()==single,"Short lane lost its shooter");
            Check(!new SandGame(Data()).Regions[1].Revealed,"New game retained reveal");
            Directory.CreateDirectory("TestResults/Boosters");
            File.WriteAllText("TestResults/Boosters/model-checks.txt","PASS: Reveal shows information without unlocking or accepting sand\nPASS: Swap first two rows in all lanes, preserving deeper rows and ammo\nPASS: Short lanes preserved\nPASS: Deep matching shooter extracted without reordering others\nPASS: Locked colors and full stash rejected without mutation\nPASS: Priority selection preserves linked pairs\nPASS: New game resets reveal\n");
        }
    }
}
