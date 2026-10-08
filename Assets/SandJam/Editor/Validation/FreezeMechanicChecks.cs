using System;
using System.Linq;
using UnityEngine;
namespace SandJamTest.Editor
{
    public static class FreezeMechanicChecks
    {
        static LevelData Data(){var d=JsonUtility.FromJson<LevelData>(Resources.Load<TextAsset>("VideoUI/PopArtOriginal").text);BuildVideoScene.ConfigureFreezeDemo(d);return d;}
        static void Check(bool b,string m){if(!b)throw new Exception("Freeze: "+m);}
        public static void Run()
        {
            var data=Data();var game=new SandGame(data);var one=game.Lanes[1].Peek();var two=game.Lanes[2].Peek();
            Check(game.Supply==game.TotalRequired,"Fixture supply mismatch");
            Check(!game.SelectLane(1) && !game.CanSelectPriority(two),"Frozen box selectable");
            game.Tick();Check(one.FreezeRemaining==1 && two.FreezeRemaining==2 && game.Moves==0,"Blocked click/tick thawed ice");
            Check(game.SelectLane(0),"Ordinary box rejected");Check(!one.IsFrozen && two.FreezeRemaining==1,"One admission must thaw one layer");
            Check(game.SelectLane(1),"Thawed box not selectable");Check(!two.IsFrozen,"Second admission did not thaw second box");
            game=new SandGame(Data());var hidden=game.Lanes[1].ToArray()[1];
            Check(game.SwapFrontRows() && hidden.FreezeRemaining==3 && !game.CanSelectPriority(hidden),"Swap or priority bypassed ice");
            var ordinary=game.Lanes[2].Peek();Check(game.SelectPriority(ordinary),"Priority ordinary selection failed");Check(hidden.FreezeRemaining==2,"Priority admission must thaw one layer");
            data=Data();data.laneData[0].ColorAmmoDatas[0].IsChain=true;data.laneData[1].ColorAmmoDatas[0].IsChain=true;
            game=new SandGame(data);Check(!game.SelectLane(0),"Pair bypassed frozen partner");
            data.laneData[1].ColorAmmoDatas[0].IsFreeze=false;game=new SandGame(data);two=game.Lanes[2].Peek();
            Check(game.SelectLane(0) && game.Slots.Count(s=>s!=null)==2 && !two.IsFrozen,"Pair must thaw two layers atomically");
            data=Data();data.laneData[0].ColorAmmoDatas[0].IsFreeze=true;data.laneData[0].ColorAmmoDatas[0].FreezeCount=1;
            game=new SandGame(data);game.Tick();Check(game.State==GameState.Lost,"All fronts frozen should fail instead of deadlock");
            data.laneData[0].ColorAmmoDatas[0].FreezeCount=0;bool rejected=false;try{SandGame.Validate(data);}catch(ArgumentException){rejected=true;}Check(rejected,"Invalid freeze count accepted");
            game=new SandGame(Data());Check(game.Lanes[2].Peek().FreezeRemaining==2,"Restart count differs");
            Debug.Log("PASS Freeze checks: blocked clicks, ticks, thaw, priority, swap, paired admission, deadlock, validation, reset");
        }
    }
}
