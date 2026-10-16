using System;
using System.Linq;

namespace SandJamTest
{
    public sealed partial class SandGame
    {
        public bool RevealRegion(int index)
        {
            if (State != GameState.Playing || index < 0 || index >= Regions.Length) return false;
            var region = Regions[index];
            if (region.Open || region.Revealed || region.Remaining <= 0) return false;
            region.Revealed = true; Revision++;
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

        public bool CanSelectPriority(Shooter shooter)
        {
            return State == GameState.Playing && Target(shooter) >= 0 && PrioritySlot(shooter) >= 0;
        }

        int PrioritySlot(Shooter shooter)
        {
            if (shooter == null || shooter.IsFrozen || (shooter.Partner != null && shooter.Partner.IsFrozen) || !Lanes.Any(l => l.Contains(shooter))) return -1;
            if (shooter.Partner != null && !PairUnblocked(shooter)) return -1;
            return FreeSlotFor(shooter);
        }
        // A chained pair only moves together when nothing stands in front of it: one behind the other at the
        // front of the same lane, or both at the front of their lanes.
        bool PairUnblocked(Shooter shooter)
        {
            int laneA, rowA, laneB, rowB;
            if (!Find(shooter, out laneA, out rowA) || !Find(shooter.Partner, out laneB, out rowB)) return false;
            if (laneA == laneB) return Math.Min(rowA, rowB) == 0 && Math.Abs(rowA - rowB) == 1;
            return rowA == 0 && rowB == 0;
        }
        bool Find(Shooter shooter, out int lane, out int row)
        {
            for (lane = 0; lane < Lanes.Length; lane++)
            {
                row = 0;
                foreach (var queued in Lanes[lane]) { if (queued == shooter) return true; row++; }
            }
            lane = row = -1; return false;
        }

        public bool SelectPriority(Shooter shooter)
        {
            if (!CanSelectPriority(shooter)) return false;
            int slot = PrioritySlot(shooter);
            var pair = shooter.Partner;
            // Preserve queue order for everyone except the selected member(s).
            var ordered = Lanes.SelectMany(l => l).Where(s => s == shooter || s == pair).ToArray();
            foreach (var lane in Lanes)
            {
                var remaining = lane.Where(s => s != shooter && s != pair).ToArray();
                lane.Clear(); foreach (var s in remaining) lane.Enqueue(s);
            }
            for (int i = 0; i < ordered.Length; i++) Slots[slot + i] = ordered[i];
            AdvanceFreeze(ordered.Length);
            Moves++; Revision++;
            Evaluate();
            return true;
        }
    }
}
