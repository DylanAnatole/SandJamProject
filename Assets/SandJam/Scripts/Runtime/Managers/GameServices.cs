using UnityEngine;

namespace SandJamTest
{
    // Persistent root created once per run: loads the save, owns the AudioManager and records stats
    // from game events. Survives scene reloads between levels.
    public sealed class GameServices : Singleton<GameServices>
    {
        protected override bool Persistent { get { return true; } }
        float levelStartedAt = -1;

        // Created on demand by the campaign scene (LevelBootstrap) so test scenes stay untouched.
        public static GameServices Ensure()
        {
            if (Instance) return Instance;
            return new GameObject("Game Services").AddComponent<GameServices>();
        }

        protected override void OnSingletonAwake()
        {
            SaveManager.Load();
            AudioManager.Create(transform);
            GameEvents.LevelStarted += OnStarted;
            GameEvents.LevelWon += OnWon;
            GameEvents.LevelLost += OnLost;
        }

        protected override void OnSingletonDestroy()
        {
            GameEvents.LevelStarted -= OnStarted;
            GameEvents.LevelWon -= OnWon;
            GameEvents.LevelLost -= OnLost;
            SaveManager.Save();
        }

        void OnStarted(int level)
        {
            var s = SaveManager.Data.stats;
            s.levelsStarted++;
            s.highestLevel = Mathf.Max(s.highestLevel, level);
            levelStartedAt = Time.unscaledTime;
            SaveManager.Save();
        }
        void OnWon(int level) { SaveManager.Data.stats.levelsWon++; AddPlayTime(); SaveManager.Save(); }
        void OnLost(int level) { SaveManager.Data.stats.levelsLost++; AddPlayTime(); SaveManager.Save(); }
        void AddPlayTime()
        {
            if (levelStartedAt < 0) return;
            SaveManager.Data.stats.secondsPlayed += Time.unscaledTime - levelStartedAt;
            levelStartedAt = -1;
        }

        // Flush pending changes when the app is backgrounded or closed (mobile kills apps without OnDestroy).
        void OnApplicationPause(bool paused) { if (paused) SaveManager.Save(); }
        void OnApplicationQuit() { SaveManager.Save(); }
    }
}
