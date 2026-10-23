using System;
using System.Linq;

namespace SandJamTest
{
    public sealed partial class SandGame
    {
        // Flare: a random locked region shows its colour and amount and opens, so it can be filled right away.
        // Parts behind a padlock (unlocker mechanic) are left alone: only their key cubes open them.
        public bool CanRevealRegion(int index)
        {
            if (State != GameState.Playing || index < 0 || index >= Regions.Length) return false;
            var region = Regions[index];
            return !region.Open && region.Remaining > 0 && !region.Data.IsUnlockedPart;
        }
        public bool RevealRegion(int index)
        {
            if (!CanRevealRegion(index)) return false;
            var region = Regions[index];
            Region.OpenStamp = ++openStep; // painted in the order regions opened
            region.Revealed = true; region.Open = true; Revision++;
            Evaluate();
            return true;
        }

        public bool SwapFrontRows()
        {
            if (State != GameState.Playing || !Lanes.Any(l => l.Count >= 2)) return false;
            foreach (var lane in Lanes)
            {
                if (lane.Count < 2) continue;
                var queue = lane.ToArray();
                var first = queue[0]; queue[0] = queue[1]; queue[1] = first;
                lane.Clear(); foreach (var shooter in queue) lane.Enqueue(shooter);
            }
            Revision++;
            Evaluate();
            return true;
        }

        // Select booster: any single cube still in a lane whose colour shows (not a chained pair, not a mystery cube
        // waiting behind another, not frozen) jumps to the first free slot, whether or not it can pour yet.
        public bool CanSelectPriority(Shooter shooter)
        {
            return State == GameState.Playing && PrioritySlot(shooter) >= 0;
        }

        int PrioritySlot(Shooter shooter)
        {
            if (shooter == null || shooter.IsFrozen || shooter.Partner != null) return -1;
            int lane = Array.FindIndex(Lanes, l => l.Contains(shooter));
            if (lane < 0 || IsHidden(shooter, lane)) return -1;
            return FreeSlotFor(shooter);
        }

        public bool SelectPriority(Shooter shooter)
        {
            if (!CanSelectPriority(shooter)) return false;
            int slot = PrioritySlot(shooter);
            // Everyone else keeps their queue order.
            foreach (var lane in Lanes)
            {
                if (!lane.Contains(shooter)) continue;
                var remaining = lane.Where(s => s != shooter).ToArray();
                lane.Clear(); foreach (var s in remaining) lane.Enqueue(s);
            }
            Slots[slot] = shooter;
            AdvanceFreeze(1);
            Moves++; Revision++;
            Evaluate();
            return true;
        }
    }
}
