using UnityEngine;
using UnityEngine.SceneManagement;
namespace SandJamTest.Scene3D
{
    // Campaign flow for the gameplay scene: reward on win, advance, reload into the next level.
    public sealed class LevelManager : MonoBehaviour
    {
        public LevelCatalog Catalog;
        public int Index;
        public SandJamSceneController Controller;
        public VideoScreen Screen;
        public LevelEntry Current {get{return Catalog.Get(Index);}}
        // Level number that was being played (stays stable on the result screen after advancing).
        public int PlayedNumber {get;private set;}
        public bool RewardDoubled {get;private set;}
        bool recorded,loading;
        public string PlayedId {get;private set;}
        void Awake(){PlayedNumber=Campaign.LevelNumber;PlayedId=Campaign.CurrentId;}
        public bool RecordCompletion()
        {
            if(Controller.Game==null || Controller.Game.State!=GameState.Won)return false;
            if(recorded)return true;
            recorded=true;
            var stats=SaveManager.Data.stats;if(PlayedNumber+1>stats.highestLevel)stats.highestLevel=PlayedNumber+1;
            Collections.MarkCompleted(PlayedId);
            EconomyManager.Add(EconomyManager.WinRewardFor(PlayedNumber),"win");
            Campaign.CompleteCurrent();
            GameEvents.RaiseLevelWon(PlayedNumber);
            return true;
        }
        // The original's "x2" (rewarded ad) simply doubles the reward here.
        public bool DoubleReward()
        {
            if(!recorded || RewardDoubled)return false;
            RewardDoubled=true;EconomyManager.Add(EconomyManager.WinRewardFor(PlayedNumber),"double");return true;
        }
        public bool LoadNext()
        {
            if(!RecordCompletion())return false;
            return GoHome();
        }
        // After a win and after Try Again the game goes back to the Home screen; the player taps PLAY to go on.
        public bool Retry(){return GoHome();}
        bool GoHome(){if(loading)return false;Campaign.OpenHome=true;return Reload(false);}
        public bool Reload(bool skipIntro)
        {
            if(loading)return false;
            loading=true;Campaign.SkipIntro=skipIntro;SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);return true;
        }
    }
}
