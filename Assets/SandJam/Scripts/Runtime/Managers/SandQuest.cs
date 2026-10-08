using System;
using UnityEngine;

namespace SandJamTest
{
    // Sand Quest (original SandQuestController / SandQuestConfig): a 24-hour race against 99 bots.
    // Win 10 levels in a row before the timer ends; every win moves you one stepping stone towards the chest
    // while bots drop out (botProgressionList ranges). Losing a level or running out of time ends the run.
    // Finishers share the 1000-coin grand prize with the players still standing.
    public static class SandQuest
    {
        public const int StartLevel = 30, Steps = 10, StartPlayers = 100, GrandPrize = 1000;
        public static readonly TimeSpan Duration = TimeSpan.FromSeconds(86400);    // totalTimeInSeconds
        static readonly TimeSpan OfferCooldown = TimeSpan.FromHours(8);
        // Players left after each step: random between x and y (MainMenu_Basic botProgressionList).
        static readonly Vector2Int[] BotProgression =
        {
            new Vector2Int(100, 100), new Vector2Int(88, 95), new Vector2Int(76, 87), new Vector2Int(64, 75), new Vector2Int(52, 63),
            new Vector2Int(42, 51), new Vector2Int(34, 41), new Vector2Int(28, 33), new Vector2Int(23, 27), new Vector2Int(19, 22), new Vector2Int(10, 18),
        };
        static bool offeredThisSession;

        static SandQuestSave Data { get { return SaveManager.Data.quest ?? (SaveManager.Data.quest = new SandQuestSave()); } }
        public static bool Unlocked { get { return Campaign.LevelNumber >= StartLevel; } }
        public static bool Joined { get { CheckExpiry(); return Data.joined; } }
        public static int StepsDone { get { return Data.steps; } }
        public static int Players { get { return Data.players; } }
        public static bool ResultPending { get { CheckExpiry(); return Data.resultPending; } }
        public static bool Won { get { return Data.won; } }
        public static int Prize { get { return Data.prize; } }

        public static TimeSpan Remaining
        {
            get
            {
                if (!Data.joined) return Duration;
                var left = new DateTime(Data.endUtcTicks, DateTimeKind.Utc) - DateTime.UtcNow;
                return left < TimeSpan.Zero ? TimeSpan.Zero : left;
            }
        }
        public static string Clock(TimeSpan t) { return string.Format("{0:00}:{1:00}:{2:00}", (int)t.TotalHours, t.Minutes, t.Seconds); }

        // The "Sand Quest has started!" popup greets the player on Home once per session (and not too often).
        public static bool ShouldOffer
        {
            get
            {
                if (offeredThisSession || !Unlocked || Joined || ResultPending) return false;
                return DateTime.UtcNow.Ticks >= Data.nextOfferUtcTicks;
            }
        }
        public static void MarkOffered()
        {
            offeredThisSession = true;
            Data.nextOfferUtcTicks = (DateTime.UtcNow + OfferCooldown).Ticks; SaveManager.Save();
        }

        public static void Join()
        {
            var d = Data;
            d.joined = true; d.steps = 0; d.players = StartPlayers; d.resultPending = false; d.won = false; d.prize = 0;
            d.endUtcTicks = (DateTime.UtcNow + Duration).Ticks;
            SaveManager.Save();
        }

        // Pays the grand-prize share (if any) and closes the run. Returns the coins awarded.
        public static int Claim()
        {
            var d = Data; int paid = d.won ? d.prize : 0;
            if (paid > 0) EconomyManager.Add(paid, "sand-quest");
            d.resultPending = false; d.won = false; d.prize = 0; d.steps = 0; d.players = StartPlayers;
            SaveManager.Save();
            return paid;
        }

        static void OnWon(int level)
        {
            var d = Data;
            if (!Joined) return;
            d.steps = Mathf.Min(Steps, d.steps + 1);
            var range = BotProgression[Mathf.Min(d.steps, BotProgression.Length - 1)];
            d.players = Mathf.Min(d.players, UnityEngine.Random.Range(range.x, range.y + 1));
            if (d.steps >= Steps)
            {
                d.joined = false; d.resultPending = true; d.won = true;
                d.prize = Mathf.Max(10, GrandPrize / Mathf.Max(1, d.players) / 10 * 10);
            }
            SaveManager.Save();
        }
        static void OnLost(int level)
        {
            var d = Data;
            if (!Joined) return;
            d.joined = false; d.resultPending = true; d.won = false; d.prize = 0;
            SaveManager.Save();
        }
        static void CheckExpiry()
        {
            var d = Data;
            if (d.joined && DateTime.UtcNow.Ticks >= d.endUtcTicks) { d.joined = false; d.resultPending = true; d.won = false; SaveManager.Save(); }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Hook()
        {
            offeredThisSession = false;
            GameEvents.LevelWon -= OnWon; GameEvents.LevelWon += OnWon;
            GameEvents.LevelLost -= OnLost; GameEvents.LevelLost += OnLost;
        }
    }
}
