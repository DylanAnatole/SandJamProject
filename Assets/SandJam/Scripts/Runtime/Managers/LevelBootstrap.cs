using System;
using UnityEngine;
namespace SandJamTest.Scene3D
{
    // One scene. The first catalog prefab is the visual template; the campaign's current
    // level JSON is injected and its regions/characters are built at runtime.
    public sealed class LevelBootstrap : MonoBehaviour
    {
        const int MaxSkips=10;
        // A broken or missing level must never strand the player on an error screen:
        // log it, advance the campaign and try the next entry.
        static TextAsset LoadPlayableLevel()
        {
            for(int attempt=0;attempt<=MaxSkips;attempt++)
            {
                try{var asset=Campaign.LoadCurrent();LevelDataManager.Load(asset);return asset;}
                catch(Exception e)
                {
                    Debug.LogWarning("Skipping level "+Campaign.LevelNumber+" ("+Campaign.CurrentId+"): "+e.Message);
                    Campaign.LevelNumber=Campaign.LevelNumber+1;
                }
            }
            throw new InvalidOperationException("No playable level found after "+MaxSkips+" attempts");
        }
        public const string SceneName="SandJamGame";
        public static int SelectedIndex;
        public LevelCatalog Catalog;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSelection(){SelectedIndex=0;}
        void Awake()
        {
            Catalog.Validate();
            var entry=Catalog.Get(0);var prefab=Resources.Load<GameObject>(entry.PrefabResource);
            if(!prefab)throw new InvalidOperationException("Missing level template prefab: "+entry.PrefabResource);
            GameServices.Ensure();
            AudioListener.volume=Campaign.SoundOn?1:0;
            SandJamSceneController.PendingLevel=LoadPlayableLevel();
            Instantiate(prefab,transform);
        }
    }
}
