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
        public LevelProgressManager Progress {get;private set;}
        public LevelEntry Current {get{return Catalog.Get(Index);}}
        // Level number that was being played (stays stable on the result screen after advancing).
        public int PlayedNumber {get;private set;}
        public bool RewardDoubled {get;private set;}
        bool recorded,loading;
        public string PlayedId {get;private set;}
        void Awake(){PlayedNumber=Campaign.LevelNumber;PlayedId=Campaign.CurrentId;Progress=new LevelProgressManager(Catalog?Catalog.ProgressId:"campaign");}
        public bool RecordCompletion()
        {
            if(Controller.Game==null || Controller.Game.State!=GameState.Won)return false;
            if(recorded)return true;
            recorded=true;
            Progress.MarkCompleted(PlayedNumber);
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
            return Reload(true);
        }
        public bool Retry(){return Reload(true);}
        public bool Reload(bool skipIntro)
        {
            if(loading)return false;
            loading=true;Campaign.SkipIntro=skipIntro;SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);return true;
        }
        // Kept for older level buttons; selection now follows the campaign.
        public bool Select(int index){return false;}
    }
}
