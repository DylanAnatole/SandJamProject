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
            if (shooter.Partner != null && !Lanes.Any(l => l.Contains(shooter.Partner))) return -1;
            for (int i = 0; i < Slots.Length; i++)
                if (Slots[i] == null && SlotRemaining(i) == 0 &&
                    (shooter.Partner == null || i + 1 < Slots.Length && Slots[i + 1] == null && SlotRemaining(i + 1) == 0)) return i;
            return -1;
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
