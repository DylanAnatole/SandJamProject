using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SandJamTest
{
    [Serializable]
    public sealed class SaveData
    {
        public int version = SaveManager.CurrentVersion;
        public int level = 1;
        public int coins = 200;
        public bool soundOn = true;
        public bool hapticsOn = true;
        public List<string> introsSeen = new List<string>();
        // Booster inventory (the original grants boosters as rewards, e.g. daily gift day 2 = 1 flare).
        public int flares, swaps, selects;
        public int lives = LivesManager.Max;
        public long nextLifeUtcTicks;
        // Level ids already finished, used for collection (gallery category) progress.
        public List<string> completedLevels = new List<string>();
        public Stats stats = new Stats();
        public string lastPlayedUtc = "";
        public SandQuestSave quest = new SandQuestSave();
    }

    // Sand Quest event state (original SandQuestController: joined flag, step index, bot count, end time).
    [Serializable]
    public sealed class SandQuestSave
    {
        public bool joined;
        public long endUtcTicks;
        public int steps, players = SandQuest.StartPlayers;
        public bool resultPending, won;
        public int prize;
        public long nextOfferUtcTicks;
    }

    [Serializable]
    public sealed class Stats
    {
        public int levelsStarted, levelsWon, levelsLost, boostersUsed, coinsEarned, coinsSpent;
        public float secondsPlayed;
        public int highestLevel = 1;
    }

    // One versioned JSON save file (with a backup copy) replacing scattered PlayerPrefs keys.
    // Older PlayerPrefs progress is migrated automatically the first time the file is created.
    public static class SaveManager
    {
        public const int CurrentVersion = 2;
        const string FileName = "sandjam_save.json";
        static SaveData data;
        static bool dirty;

        public static string FilePath { get { return Path.Combine(Application.persistentDataPath, FileName); } }
        public static SaveData Data { get { if (data == null) Load(); return data; } }

        public static void Load()
        {
            data = TryRead(FilePath) ?? TryRead(FilePath + ".bak");
            if (data == null) { data = MigrateFromPlayerPrefs(); Save(); }
            if (data.introsSeen == null) data.introsSeen = new List<string>();
            if (data.stats == null) data.stats = new Stats();
            if (data.completedLevels == null) data.completedLevels = new List<string>();
            // v1 -> v2: lives were added; existing players start with full hearts.
            if (data.version < 2 && data.lives <= 0 && data.nextLifeUtcTicks == 0) data.lives = LivesManager.Max;
            data.version = CurrentVersion;
        }

        static SaveData TryRead(string path)
        {
            try { return File.Exists(path) ? JsonUtility.FromJson<SaveData>(File.ReadAllText(path)) : null; }
            catch (Exception e) { Debug.LogWarning("Save file unreadable (" + path + "): " + e.Message); return null; }
        }

        static SaveData MigrateFromPlayerPrefs()
        {
            var d = new SaveData();
            d.level = Math.Max(1, PlayerPrefs.GetInt("SandJam.Campaign.Level", 1));
            d.coins = PlayerPrefs.GetInt("SandJam.Coins", d.coins);
            d.soundOn = PlayerPrefs.GetInt("SandJam.Sound", 1) == 1;
            foreach (var gif in new[] { "ChainSameLine", "SecretAnim", "ChainAnotherLane", "StashNeedAmmo", "FunFlareGif", "SwapGif", "SelectAny" })
                if (PlayerPrefs.GetInt("SandJam.IntroSeen." + gif, 0) == 1) d.introsSeen.Add(gif);
            d.stats.highestLevel = d.level;
            return d;
        }

        public static void MarkDirty() { dirty = true; }

        // Writes atomically: temp file, then swap, keeping the previous file as .bak.
        public static void Save()
        {
            if (data == null) return;
            dirty = false;
            data.lastPlayedUtc = DateTime.UtcNow.ToString("o");
            try
            {
                string tmp = FilePath + ".tmp";
                File.WriteAllText(tmp, JsonUtility.ToJson(data, true));
                if (File.Exists(FilePath)) File.Copy(FilePath, FilePath + ".bak", true);
                File.Copy(tmp, FilePath, true); File.Delete(tmp);
            }
            catch (Exception e) { Debug.LogError("Could not write save: " + e.Message); }
        }

        public static void SaveIfDirty() { if (dirty) Save(); }

        public static void ResetAll()
        {
            data = new SaveData();
            Save();
            GameEvents.RaiseSettingsChanged();
        }

        public static bool HasSeenIntro(string id) { return Data.introsSeen.Contains(id); }
        public static void MarkIntroSeen(string id) { if (!Data.introsSeen.Contains(id)) { Data.introsSeen.Add(id); Save(); } }
    }
}
