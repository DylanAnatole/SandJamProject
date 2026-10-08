using System;

namespace SandJamTest
{
    // 7-day login gift (original rewards/reward_daily.asset): one gift per calendar day, in order; the cycle
    // restarts after day 7. Missing days does not reset the streak (the original panel only shows a countdown).
    public static class DailyRewards
    {
        public sealed class Gift { public int Coins, Flares, Swaps, Selects; }
        public static readonly Gift[] Days =
        {
            new Gift { Coins = 50 }, new Gift { Flares = 1 }, new Gift { Coins = 75 }, new Gift { Swaps = 1 },
            new Gift { Coins = 100 }, new Gift { Selects = 1 }, new Gift { Coins = 150, Flares = 1, Swaps = 1, Selects = 1 },
        };

        static DailyRewardSave Data { get { return SaveManager.Data.dailyReward ?? (SaveManager.Data.dailyReward = new DailyRewardSave()); } }
        public static int Today { get { return (int)(DateTime.UtcNow - DateTime.UnixEpoch).TotalDays; } }
        public static int NextDay { get { return Data.nextDay % Days.Length; } }
        public static bool CanClaim { get { return Data.lastClaimDay < Today; } }
        public static TimeSpan UntilNext { get { return DateTime.UnixEpoch.AddDays(Today + 1) - DateTime.UtcNow; } }
        public static Gift Claim()
        {
            if (!CanClaim) return null;
            var gift = Days[NextDay];
            if (gift.Coins > 0) EconomyManager.Add(gift.Coins, "daily-reward");
            if (gift.Flares > 0) EconomyManager.AddBooster("rocket", gift.Flares, "daily-reward");
            if (gift.Swaps > 0) EconomyManager.AddBooster("swap", gift.Swaps, "daily-reward");
            if (gift.Selects > 0) EconomyManager.AddBooster("select", gift.Selects, "daily-reward");
            Data.nextDay = (NextDay + 1) % Days.Length;
            Data.lastClaimDay = Today;
            SaveManager.Save();
            return gift;
        }
    }
}
