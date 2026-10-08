using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SandJamTest
{
    // Daily and weekly tasks (original Default_RemoteDailyQuestConfig): each period draws one task per tier
    // (4 daily tiers, 6 weekly tiers). Finishing a task adds its stars; stars fill the board's bar and pay
    // star milestones (weekly: the three reward_gift_weekly gifts; daily: one coin chest — the original daily
    // reward steps are remote, so that amount is an estimate).
    public static class TaskBoard
    {
        public enum Kind { Daily, Weekly }
        public enum Metric { Shots, Cards, Tiles, Flare, Swap, Select, Coins, Levels, PurpleShots, YellowShots, BlueTiles, GreenTiles, ExtraSlot }
        public sealed class Task
        {
            public string Id, Name; public Kind Kind; public Metric Metric; public int Target, Stars, Tier; public string Icon;
        }
        public sealed class Milestone { public float Fraction; public int Coins, Flares, Swaps, Selects; }

        public static readonly Task[] All =
        {
            T("D01", "BẮN CÁT", Kind.Daily, Metric.Shots, 8000, 5, 1, "ShotsFired"),
            T("D02", "MỞ KHỐI", Kind.Daily, Metric.Cards, 50, 5, 1, "RevealCards"),
            T("D03", "TÔ Ô", Kind.Daily, Metric.Tiles, 1250, 15, 2, "PaintTiles"),
            T("D04", "DÙNG FLARE", Kind.Daily, Metric.Flare, 3, 15, 2, "FlareIcon"),
            T("D05", "DÙNG SWAP", Kind.Daily, Metric.Swap, 3, 20, 3, "SwapIcon"),
            T("D06", "KIẾM XU", Kind.Daily, Metric.Coins, 2000, 20, 3, "EarnCoin"),
            T("D07", "DÙNG SELECT", Kind.Daily, Metric.Select, 3, 25, 4, "HandIcon"),
            T("D08", "QUA MÀN", Kind.Daily, Metric.Levels, 120, 25, 4, "CompleteLevels"),
            T("W01", "QUA MÀN", Kind.Weekly, Metric.Levels, 250, 5, 1, "CompleteLevels"),
            T("W02", "DÙNG FLARE", Kind.Weekly, Metric.Flare, 5, 5, 1, "FlareIcon"),
            T("W03", "BẮN CÁT", Kind.Weekly, Metric.Shots, 40000, 10, 2, "ShotsFired"),
            T("W04", "MỞ KHỐI", Kind.Weekly, Metric.Cards, 250, 10, 2, "RevealCards"),
            T("W05", "BẮN CÁT TÍM", Kind.Weekly, Metric.PurpleShots, 15000, 15, 3, "FirePurpleShots"),
            T("W06", "BẮN CÁT VÀNG", Kind.Weekly, Metric.YellowShots, 15000, 15, 3, "FireYellowShots"),
            T("W07", "TÔ Ô XANH DƯƠNG", Kind.Weekly, Metric.BlueTiles, 450, 20, 4, "PaintBlueTiles"),
            T("W08", "TÔ Ô XANH LÁ", Kind.Weekly, Metric.GreenTiles, 450, 20, 4, "PaintGreenTiles"),
            T("W09", "DÙNG SWAP", Kind.Weekly, Metric.Swap, 5, 30, 5, "SwapIcon"),
            T("W10", "TÔ Ô", Kind.Weekly, Metric.Tiles, 5000, 30, 5, "PaintTiles"),
            T("W11", "DÙNG SELECT", Kind.Weekly, Metric.Select, 6, 60, 6, "HandIcon"),
            T("W12", "THÊM Ô CHỜ", Kind.Weekly, Metric.ExtraSlot, 5, 60, 6, "UseExtraSlotIcon"),
        };
        static Task T(string id, string name, Kind kind, Metric metric, int target, int stars, int tier, string icon)
        { return new Task { Id = id, Name = name, Kind = kind, Metric = metric, Target = target, Stars = stars, Tier = tier, Icon = icon }; }

        // Star milestones. Weekly = reward_gift_weekly gift_1..3; daily = one estimated coin chest at full bar.
        public static readonly Milestone[] WeeklyMilestones =
        {
            new Milestone { Fraction = 1f / 3, Coins = 200, Flares = 1 }, new Milestone { Fraction = 2f / 3, Coins = 300, Swaps = 1 },
            new Milestone { Fraction = 1f, Coins = 500, Selects = 1 },
        };
        public static readonly Milestone[] DailyMilestones = { new Milestone { Fraction = 1f, Coins = 100 } };
        public static Milestone[] MilestonesOf(Kind kind) { return kind == Kind.Daily ? DailyMilestones : WeeklyMilestones; }

        static int Day { get { return (int)(DateTime.UtcNow - DateTime.UnixEpoch).TotalDays; } }
        static int Week { get { return (Day + 3) / 7; } } // weeks start on Monday (1970-01-01 was a Thursday)
        static TaskSave Save(Kind kind)
        {
            var d = SaveManager.Data;
            if (kind == Kind.Daily) return d.dailyTasks ?? (d.dailyTasks = new TaskSave());
            return d.weeklyTasks ?? (d.weeklyTasks = new TaskSave());
        }

        // The current board, drawn again when a new day / week starts (one random task per tier).
        public static TaskSave Board(Kind kind)
        {
            var s = Save(kind); int period = kind == Kind.Daily ? Day : Week;
            if (s.period == period && s.ids.Count > 0) return s;
            var random = new System.Random(period * 31 + (int)kind);
            s.period = period; s.stars = 0; s.ids.Clear(); s.progress.Clear(); s.claimedMilestones.Clear();
            foreach (var tier in All.Where(t => t.Kind == kind).Select(t => t.Tier).Distinct().OrderBy(t => t))
            {
                var options = All.Where(t => t.Kind == kind && t.Tier == tier).ToArray();
                s.ids.Add(options[random.Next(options.Length)].Id); s.progress.Add(0);
            }
            SaveManager.Save();
            return s;
        }
        public static Task Find(string id) { return All.First(t => t.Id == id); }
        public static int MaxStars(Kind kind) { return Board(kind).ids.Sum(id => Find(id).Stars); }
        public static TimeSpan Remaining(Kind kind)
        {
            var end = kind == Kind.Daily ? DateTime.UnixEpoch.AddDays(Day + 1) : DateTime.UnixEpoch.AddDays((Week + 1) * 7 - 3);
            return end - DateTime.UtcNow;
        }
        public static bool HasUnclaimed(Kind kind)
        {
            var s = Board(kind); int max = MaxStars(kind); var ms = MilestonesOf(kind);
            for (int i = 0; i < ms.Length; i++) if (!s.claimedMilestones.Contains(i) && s.stars >= Mathf.CeilToInt(ms[i].Fraction * max)) return true;
            return false;
        }

        // Pays every reached milestone not yet claimed; returns the coins paid (boosters are added too).
        public static int ClaimMilestones(Kind kind)
        {
            var s = Board(kind); int max = MaxStars(kind), coins = 0; var ms = MilestonesOf(kind);
            for (int i = 0; i < ms.Length; i++)
            {
                if (s.claimedMilestones.Contains(i) || s.stars < Mathf.CeilToInt(ms[i].Fraction * max)) continue;
                s.claimedMilestones.Add(i); coins += ms[i].Coins;
                if (ms[i].Coins > 0) EconomyManager.Add(ms[i].Coins, kind + "-tasks");
                if (ms[i].Flares > 0) EconomyManager.AddBooster("rocket", ms[i].Flares, kind + "-tasks");
                if (ms[i].Swaps > 0) EconomyManager.AddBooster("swap", ms[i].Swaps, kind + "-tasks");
                if (ms[i].Selects > 0) EconomyManager.AddBooster("select", ms[i].Selects, kind + "-tasks");
            }
            SaveManager.Save();
            return coins;
        }

        static void Count(Metric metric, int amount)
        {
            if (amount <= 0) return;
            bool changed = false;
            foreach (Kind kind in new[] { Kind.Daily, Kind.Weekly })
            {
                var s = Board(kind);
                for (int i = 0; i < s.ids.Count; i++)
                {
                    var task = Find(s.ids[i]);
                    if (task.Metric != metric || s.progress[i] >= task.Target) continue;
                    s.progress[i] = Mathf.Min(task.Target, s.progress[i] + amount); changed = true;
                    if (s.progress[i] >= task.Target) s.stars += task.Stars;
                }
            }
            if (changed) SaveManager.MarkDirty();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Hook()
        {
            GameEvents.SandPoured -= OnSand; GameEvents.SandPoured += OnSand;
            GameEvents.CharacterSelected -= OnCube; GameEvents.CharacterSelected += OnCube;
            GameEvents.BoosterUsed -= OnBooster; GameEvents.BoosterUsed += OnBooster;
            GameEvents.CoinsChanged -= OnCoins; GameEvents.CoinsChanged += OnCoins;
            GameEvents.LevelWon -= OnWon; GameEvents.LevelWon += OnWon;
        }
        static void OnSand(int color, int amount, int divider)
        {
            int tiles = Mathf.Max(1, amount / Mathf.Max(1, divider));
            Count(Metric.Shots, amount); Count(Metric.Tiles, tiles);
            if (color == 7) Count(Metric.PurpleShots, amount);
            if (color == 4) Count(Metric.YellowShots, amount);
            if (color == 3) Count(Metric.BlueTiles, tiles);
            if (color == 2) Count(Metric.GreenTiles, tiles);
        }
        static void OnCube(int color) { Count(Metric.Cards, 1); } // "REVEAL CARDS": cubes sent to the stash
        static void OnBooster(string action, int paid)
        {
            if (action == "rocket") Count(Metric.Flare, 1);
            else if (action == "swap") Count(Metric.Swap, 1);
            else if (action == "select") Count(Metric.Select, 1);
        }
        static void OnCoins(int balance, int delta, string reason)
        {
            if (delta > 0 && !reason.EndsWith("-tasks") && reason != "refund") Count(Metric.Coins, delta);
            if (reason == "play-on") Count(Metric.ExtraSlot, 1);
        }
        static void OnWon(int level) { Count(Metric.Levels, 1); }
    }
}
