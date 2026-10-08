using System;

namespace SandJamTest
{
    // Original lives (hearts): up to 5; giving up a failed level costs one; one heart refills every 30 minutes.
    // Refill time is stored as UTC ticks so it keeps counting while the game is closed.
    public static class LivesManager
    {
        public const int Max = 5;
        public static readonly TimeSpan RefillTime = TimeSpan.FromMinutes(30);

        public static int Lives { get { Tick(); return SaveManager.Data.lives; } }
        public static bool IsFull { get { return Lives >= Max; } }
        public static bool CanPlay { get { return Lives > 0; } }

        // Time until the next heart (zero when full).
        public static TimeSpan UntilNext
        {
            get
            {
                Tick();
                if (SaveManager.Data.lives >= Max) return TimeSpan.Zero;
                var left = new DateTime(SaveManager.Data.nextLifeUtcTicks, DateTimeKind.Utc) - DateTime.UtcNow;
                return left < TimeSpan.Zero ? TimeSpan.Zero : left;
            }
        }

        public static string Countdown(TimeSpan t)
        {
            return t.TotalHours >= 1 ? string.Format("{0}h {1:00}m", (int)t.TotalHours, t.Minutes) : string.Format("{0}m {1:00}s", t.Minutes, t.Seconds);
        }

        public static void LoseLife()
        {
            Tick();
            var d = SaveManager.Data;
            if (d.lives <= 0) return;
            if (d.lives >= Max) d.nextLifeUtcTicks = (DateTime.UtcNow + RefillTime).Ticks;
            d.lives--;
            SaveManager.Save();
            GameEvents.RaiseSettingsChanged();
        }

        public static void Refill()
        {
            SaveManager.Data.lives = Max; SaveManager.Data.nextLifeUtcTicks = 0;
            SaveManager.Save();
            GameEvents.RaiseSettingsChanged();
        }

        // Grants every heart whose refill time has passed (including time spent offline).
        static void Tick()
        {
            var d = SaveManager.Data;
            if (d.lives >= Max) { d.lives = Max; return; }
            if (d.nextLifeUtcTicks <= 0) d.nextLifeUtcTicks = (DateTime.UtcNow + RefillTime).Ticks;
            bool changed = false;
            while (d.lives < Max && DateTime.UtcNow.Ticks >= d.nextLifeUtcTicks)
            {
                d.lives++; changed = true;
                d.nextLifeUtcTicks += RefillTime.Ticks;
            }
            if (d.lives >= Max) d.nextLifeUtcTicks = 0;
            if (changed) SaveManager.MarkDirty();
        }
    }
}
