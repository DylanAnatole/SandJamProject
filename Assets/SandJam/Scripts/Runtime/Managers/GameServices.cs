using UnityEngine;

namespace SandJamTest
{
    // Persistent root created once per run: loads the save, owns the AudioManager and records stats
    // from game events. Survives scene reloads between levels.
    public sealed class GameServices : MonoBehaviour
    {
        public static GameServices Instance { get; private set; }
        public static bool Ready { get { return Instance != null; } }
        float levelStartedAt = -1;

        // Created on demand by the campaign scene (LevelBootstrap) so test scenes stay untouched.
        public static GameServices Ensure()
        {
            if (Instance) return Instance;
            var root = new GameObject("Game Services");
            DontDestroyOnLoad(root);
            return root.AddComponent<GameServices>();
        }

        void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            SaveManager.Load();
            AudioManager.Create(transform);
            GameEvents.LevelStarted += OnStarted;
            GameEvents.LevelWon += OnWon;
            GameEvents.LevelLost += OnLost;
        }

        void OnDestroy()
        {
            if (Instance != this) return;
            GameEvents.LevelStarted -= OnStarted;
            GameEvents.LevelWon -= OnWon;
            GameEvents.LevelLost -= OnLost;
            SaveManager.Save();
            Instance = null;
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
