using System;
using UnityEngine;

namespace SandJamTest
{
    // Player-facing campaign: the original level order exactly (Default_LevelOrderConfig, 915 entries), so level N
    // here is level N in the shipped game — art-style blocks, hard levels and stage themes line up with it.
    // Progress, coins and settings are stored by SaveManager; coin changes go through EconomyManager.
    // After the last entry the order repeats, as live games do.
    public static class Campaign
    {
        [Serializable] sealed class CampaignFile { public string[] levels; }
        static string[] levels;

        public static string[] Levels
        {
            get
            {
                if (levels != null) return levels;
                var asset = Resources.Load<TextAsset>("Levels/Campaign");
                levels = asset ? JsonUtility.FromJson<CampaignFile>(asset.text).levels : null;
                if (levels == null || levels.Length == 0) levels = new[] { "112x84_Level31_Tutorial" };
                return levels;
            }
        }

        // 1-based level number shown to the player.
        public static int LevelNumber
        {
            get { return Math.Max(1, SaveManager.Data.level); }
            set { SaveManager.Data.level = Math.Max(1, value); SaveManager.Save(); }
        }
        public static string CurrentId { get { return IdAt(LevelNumber); } }
        // Level id of a level number (the campaign wraps around after its last entry).
        public static string IdAt(int level) { return Levels[(Math.Max(1, level) - 1) % Levels.Length]; }
        // Picture of a level (LevelRenders/<id>): repeated levels ("<id>_Dupe") share their original's.
        public static string PictureId(string id) { return id.EndsWith("_Dupe", StringComparison.Ordinal) ? id.Substring(0, id.Length - 5) : id; }
        public static TextAsset LoadCurrent()
        {
            var asset = LoadLevel(CurrentId);
            if (!asset) throw new InvalidOperationException("Missing level data: " + CurrentId);
            return asset;
        }
        // Original levels (local only) live in Resources/Levels/Data; levels designed for this project in
        // Resources/CustomLevels, which is kept in the repository.
        public const string CustomFolder = "CustomLevels";
        public static TextAsset LoadLevel(string id)
        {
            var asset = Resources.Load<TextAsset>("Levels/Data/" + id);
            return asset ? asset : Resources.Load<TextAsset>(CustomFolder + "/" + id);
        }

        public static bool SoundOn
        {
            get { return SaveManager.Data.soundOn; }
            set { SaveManager.Data.soundOn = value; SaveManager.Save(); AudioListener.volume = value ? 1 : 0; GameEvents.RaiseSettingsChanged(); }
        }

        // Set before reloading the scene so the next level opens straight into gameplay.
        public static bool SkipIntro;
        // Set when a scene reload should land on the Home screen directly (no loading splash).
        public static bool OpenHome;
        public static event Action Changed;
        public static void NotifyChanged() { if (Changed != null) Changed(); }
        static Campaign() { GameEvents.CoinsChanged += (balance, delta, reason) => NotifyChanged(); }

        public static void CompleteCurrent() { LevelNumber = LevelNumber + 1; NotifyChanged(); }
    }
}
