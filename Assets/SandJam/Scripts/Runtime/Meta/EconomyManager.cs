using System;

namespace SandJamTest
{
    // Every coin and booster change goes through here, with a reason, so rewards and prices live in one place.
    public static class EconomyManager
    {
        // Win rewards as seen in the current build: +10, hard levels +20, bonus ("easy") levels +30 (EasyHardLevelConfig).
        public const int WinReward = 10, HardWinReward = 20, BonusWinReward = 30;
        public const int RefillLivesPrice = 600; // original "Refill 600" on the no-lives popup
        public static int WinRewardFor(int level) { return OriginalConfig.IsEasy(level) ? BonusWinReward : OriginalConfig.IsHard(level) ? HardWinReward : WinReward; }
        public static int Coins { get { return SaveManager.Data.coins; } }

        public static int BoosterPrice(string action) { var b = BoosterCatalog.Find(action); return b != null ? b.Price : 0; }

        public static int BoosterCount(string action) { var s = Stock(action, false); return s != null ? s.count : 0; }

        public static void AddBooster(string action, int amount, string reason)
        {
            if (BoosterCatalog.Find(action) == null) { UnityEngine.Debug.LogWarning("Unknown booster: " + action); return; }
            var s = Stock(action, true);
            s.count = Math.Max(0, s.count + amount);
            SaveManager.Save();
            GameEvents.RaiseCoinsChanged(Coins, 0, reason);
        }

        static BoosterStock Stock(string action, bool create)
        {
            var list = SaveManager.Data.boosters;
            var s = list.Find(b => b.id == action);
            if (s == null && create) { s = new BoosterStock { id = action }; list.Add(s); }
            return s;
        }

        public static void Add(int amount, string reason)
        {
            if (amount <= 0) return;
            SaveManager.Data.coins += amount;
            SaveManager.Data.stats.coinsEarned += amount;
            SaveManager.Save();
            GameEvents.RaiseCoinsChanged(Coins, amount, reason);
        }

        public static bool TrySpend(int amount, string reason)
        {
            if (amount <= 0) return true;
            if (Coins < amount) return false;
            SaveManager.Data.coins -= amount;
            SaveManager.Data.stats.coinsSpent += amount;
            SaveManager.Save();
            GameEvents.RaiseCoinsChanged(Coins, -amount, reason);
            return true;
        }

        public static bool CanUseBooster(string action) { return BoosterCount(action) > 0 || Coins >= BoosterPrice(action); }

        // Uses one from the inventory first, otherwise pays the coin price. Returns false when unaffordable.
        public static bool UseBooster(string action)
        {
            int paid = 0;
            if (BoosterCount(action) > 0) AddBooster(action, -1, "booster:" + action);
            else { paid = BoosterPrice(action); if (!TrySpend(paid, "booster:" + action)) return false; }
            SaveManager.Data.stats.boostersUsed++;
            GameEvents.RaiseBoosterUsed(action, paid);
            return true;
        }

        // Sets the balance directly (editor/debug tools).
        public static void SetCoins(int value, string reason)
        {
            int delta = Math.Max(0, value) - Coins;
            SaveManager.Data.coins = Math.Max(0, value);
            SaveManager.Save();
            GameEvents.RaiseCoinsChanged(Coins, delta, reason);
        }
    }
}
