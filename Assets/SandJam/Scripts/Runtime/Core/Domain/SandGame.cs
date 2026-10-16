using System;
using System.Collections.Generic;
using System.Linq;

namespace SandJamTest
{
    // Pure simulation. The view advances this with a fixed timestep.
    // Prototype rule: finishing ANY linked region opens an ordinary region.
    public sealed partial class SandGame
    {
        public readonly LevelData Data;
        public readonly Region[] Regions;
        public readonly Queue<Shooter>[] Lanes;
        public Shooter[] Slots { get; private set; }
        public GameState State { get; private set; }
        public int TotalRequired { get; private set; }
        public int SpentAmmo { get; private set; }
        public int Moves { get; private set; }
        public int Revision { get; private set; }
        public int Remaining { get { int sum = 0; for (int i = 0; i < Regions.Length; i++) sum += Regions[i].Remaining; return sum; } }
        public int InitialSupply { get; private set; }
        public int Supply { get { return Lanes.Sum(l => l.Sum(s => s.Ammo)) + Slots.Where(s => s != null).Sum(s => s.Ammo); } }

        readonly Func<Region, bool> isCovered;
        readonly bool useSlotUnlocks;
        int openStep; // advances on every opening pass; regions opened in one pass share it (see Target)
        int[][] prerequisites, padlocks;
        bool[] finished;
        readonly List<Shot> shots = new List<Shot>();
        public SandGame(LevelData data, int capacity = 5, Func<Region, bool> coverageCheck = null, bool enableSlotUnlocks = false)
        {
            Validate(data);
            useSlotUnlocks = enableSlotUnlocks;
            if (capacity < 1) throw new ArgumentOutOfRangeException("capacity");
            Data = data; isCovered = coverageCheck;
            Regions = data.parts.Select(p => new Region(p)).ToArray();
            Lanes = data.laneData.Select(l => new Queue<Shooter>(l.ColorAmmoDatas.Select(c => new Shooter(c)))).ToArray();
            var shooters = Lanes.Select(l => l.ToArray()).ToArray();
            foreach (var pair in ChainPairing.Resolve(data))
            {
                var a = shooters[pair.LaneA][pair.OrderA];
                var b = shooters[pair.LaneB][pair.OrderB];
                a.Partner = b; b.Partner = a;
            }
            Slots = new Shooter[capacity];
            // Region links resolved once (indices), so the per-tick opening pass allocates nothing.
            var byName = new Dictionary<string, int>();
            for (int i = 0; i < Regions.Length; i++) byName[Regions[i].Data.name] = i;
            prerequisites = new int[Regions.Length][];
            padlocks = new int[Regions.Length][];
            for (int i = 0; i < Regions.Length; i++)
            {
                var data_ = Regions[i].Data;
                prerequisites[i] = (data_.prerequests ?? new string[0]).Where(byName.ContainsKey).Select(n => byName[n]).ToArray();
                var locks = new List<int>();
                for (int j = 0; j < Regions.Length; j++)
                    if (Regions[j].Data.IsUnlockerPart && Regions[j].Data.unlockerPartName == data_.name) locks.Add(j);
                padlocks[i] = locks.ToArray();
            }
            finished = new bool[Regions.Length];
            TotalRequired = Remaining;
            InitialSupply = Supply;
            State = GameState.Playing;
        }

        public static void Validate(LevelData data)
        {
            if (data == null || data.rowCount < 1 || data.columnCount < 1 || data.parts == null || data.parts.Length == 0 || data.laneData == null)
                throw new ArgumentException("Level data is incomplete.");
            var names = new HashSet<string>();
            var pixels = new HashSet<int>();
            foreach (var p in data.parts)
            {
                if (string.IsNullOrEmpty(p.name) || !names.Add(p.name)) throw new ArgumentException("Duplicate region ID.");
                if (p.amount <= 0 || p.rows == null || p.cols == null || p.rows.Length != p.cols.Length || p.rows.Length == 0)
                    throw new ArgumentException("Invalid region: " + p.name);
                // A padlock (unlocker) part deliberately shares the pixels of the part it unlocks.
                for (int i = 0; i < p.rows.Length; i++)
                {
                    int r = p.rows[i], c = p.cols[i];
                    if (r < 0 || r >= data.rowCount || c < 0 || c >= data.columnCount || (!p.IsUnlockerPart && !pixels.Add(r * data.columnCount + c)))
                        throw new ArgumentException("Invalid or duplicate pixel: " + p.name);
                }
            }
            foreach (var p in data.parts)
                if (p.IsUnlockerPart && !data.parts.Any(o => o.name == p.unlockerPartName && o.IsUnlockedPart))
                    throw new ArgumentException("Padlock " + p.name + " has no locked part '" + p.unlockerPartName + "'");
            foreach (var p in data.parts)
                foreach (var other in p.prerequests ?? new string[0])
                    if (!names.Contains(other)) throw new ArgumentException("Missing region: " + other);
            if (data.opr == null || data.opc == null || data.opr.Length != data.opc.Length) throw new ArgumentException("Invalid obstacle coordinates.");
            for (int i = 0; i < data.opr.Length; i++)
            {
                int r = data.opr[i], c = data.opc[i];
                if (r < 0 || r >= data.rowCount || c < 0 || c >= data.columnCount || !pixels.Add(r * data.columnCount + c))
                    throw new ArgumentException("Invalid or overlapping obstacle.");
            }
            foreach (var lane in data.laneData)
            {
                if (lane == null || lane.ColorAmmoDatas == null) throw new ArgumentException("Missing lane.");
                foreach (var c in lane.ColorAmmoDatas)
                {
                    if(c.IsFreeze && c.FreezeCount <= 0) throw new ArgumentException("Frozen character needs a positive freeze count.");
                    if (c.AmmoCount <= 0) throw new ArgumentException("Invalid ammo.");
                    if (c.IsUnlocker && !data.parts.Any(p => p.IsUnlockerPart && p.ColorType == c.ColorType))
                        throw new ArgumentException("Key cube colour " + c.ColorType + " has no padlock to open.");
                    if (c.IsHalf && c.IsChain) throw new ArgumentException("A half cube cannot also be chained.");
                }
            }
            ChainPairing.Resolve(data);
        }

        public bool SelectLane(int lane)
        {
            if (State != GameState.Playing || lane < 0 || lane >= Lanes.Length || Lanes[lane].Count == 0) return false;
            int partnerLane;
            int slot = SelectionSlot(lane, out partnerLane);
            if (slot < 0) return false;
            var first = Lanes[lane].Dequeue();
            if (first.Partner == null) Slots[slot] = first;
            else
            {
                var second = Lanes[partnerLane].Dequeue();
                // Keep left/right order stable even when the right member was clicked; the second member takes the
                // next free slot, wherever it is (original: the pair splits across two separate free slots).
                Slots[slot] = partnerLane < lane ? second : first;
                Slots[FreeSlot(slot + 1)] = partnerLane < lane ? first : second;
            }
            bool pair = first.Partner != null;
            AdvanceFreeze(pair ? 2 : 1);
            // Original: the chain breaks once the pair reaches the stash; from then on the two cubes are
            // independent (each pours, empties and leaves on its own).
            if (pair) { first.Partner.Partner = null; first.Partner = null; }
            Moves++;
            Revision++;
            Evaluate();
            return true;
        }

        // Original "Out of Space → Play on": the player buys one extra waiting slot and the lost game resumes.
        public bool ContinueWithExtraSlot()
        {
            if (State != GameState.Lost) return false;
            var expanded = new Shooter[Slots.Length + 1];
            Array.Copy(Slots, expanded, Slots.Length);
            Slots = expanded;
            State = GameState.Playing;
            Revision++;
            Evaluate();
            return true;
        }

        public bool CanSelectLane(int lane) { int partnerLane; return SelectionSlot(lane, out partnerLane) >= 0; }

        // Mystery cube rule: its colour stays hidden while it waits behind another cube in its lane.
        public bool IsHidden(Shooter shooter, int lane)
        {
            if (shooter == null || !shooter.Secret || lane < 0 || lane >= Lanes.Length) return false;
            var queue = Lanes[lane];
            return queue.Count > 0 && queue.Peek() != shooter && queue.Contains(shooter);
        }

        int SelectionSlot(int lane, out int partnerLane)
        {
            partnerLane = -1;
            if (lane < 0 || lane >= Lanes.Length || Lanes[lane].Count == 0) return -1;
            var first = Lanes[lane].Peek();
            if(first.IsFrozen || (first.Partner != null && first.Partner.IsFrozen))return -1;
            if (first.Partner != null)
            {
                int index = 0;
                foreach (var queued in Lanes[lane]) { if (index == 1 && queued == first.Partner) partnerLane = lane; if (++index > 1) break; }
                if (partnerLane != lane) for (int i = 0; i < Lanes.Length; i++)
                    if (i != lane && Lanes[i].Count > 0 && Lanes[i].Peek() == first.Partner) partnerLane = i;
                if (partnerLane < 0) return -1;
            }
            return FreeSlotFor(first);
        }
        // First open, empty stash slot for the shooter; a chained pair also needs a second one, anywhere after it
        // (original: the two cubes run to two separate free slots). -1 when there is no room.
        int FreeSlotFor(Shooter shooter)
        {
            int first = FreeSlot(0);
            return first < 0 || shooter.Partner == null || FreeSlot(first + 1) >= 0 ? first : -1;
        }
        int FreeSlot(int from)
        {
            for (int i = from; i < Slots.Length; i++) if (Slots[i] == null && SlotRemaining(i) == 0) return i;
            return -1;
        }

        // Exported slot thresholds are displayed units, while SpentAmmo is raw ammo.
        // Enabled only for the video scene; older prototypes keep their existing behavior.
        public int SlotRemaining(int index)
        {
            if(!useSlotUnlocks || Data.gridSlotNeedAmmoCount==null || index>=Data.gridSlotNeedAmmoCount.Length)return 0;
            return Math.Max(0,Data.gridSlotNeedAmmoCount[index]-SpentAmmo/Math.Max(1,Data.uiDivider));
        }

        public int Target(Shooter shooter)
        {
            // Sleeping halves never pour; key cubes pour only into padlocks, ordinary cubes never do.
            if (shooter == null || shooter.IsFrozen || shooter.Ammo <= 0 || shooter.Half) return -1;
            // Pour order seen in the current build (level 268 recording): the region that opened first is painted
            // first; regions that opened together are painted from the bottom of the picture upwards.
            int best = -1;
            for (int i = 0; i < Regions.Length; i++)
            {
                var p = Regions[i];
                if (!p.Open || p.Remaining <= 0 || p.Data.ColorType != shooter.Color || p.Data.IsUnlockerPart != shooter.Key) continue;
                if (best < 0) { best = i; continue; }
                var b = Regions[best];
                if (p.OpenedOrder < b.OpenedOrder || (p.OpenedOrder == b.OpenedOrder && p.LowestRow < b.LowestRow)) best = i;
            }
            return best;
        }

        // Two same-colour halves resting in the stash merge into one full cube in the earlier slot.
        void MergeHalves()
        {
            for (int i = 0; i < Slots.Length; i++)
            {
                var a = Slots[i];
                if (a == null || !a.Half) continue;
                for (int j = i + 1; j < Slots.Length; j++)
                {
                    var b = Slots[j];
                    if (b == null || !b.Half || b.Color != a.Color) continue;
                    a.Ammo += b.Ammo; a.InitialAmmo += b.InitialAmmo; a.Half = false;
                    b.Ammo = 0; b.Half = false; b.MergedInto = a;
                    Slots[j] = null; Revision++;
                    break;
                }
            }
        }

        // The returned list is reused by the next call: callers consume it right away.
        public List<Shot> Tick(int ammoPerShooter = 20, Func<int, bool> canShoot = null)
        {
            shots.Clear();
            if (State != GameState.Playing || ammoPerShooter <= 0) return shots;
            OpenNeighbours();
            for (int slot = 0; slot < Slots.Length; slot++)
            {
                if (canShoot != null && !canShoot(slot)) continue;
                var shooter = Slots[slot];
                int target = Target(shooter);
                if (target < 0) continue;
                var region = Regions[target];
                int amount = Math.Min(ammoPerShooter, Math.Min(shooter.Ammo, region.Remaining));
                shooter.Ammo -= amount;
                region.Remaining -= amount;
                SpentAmmo += amount;
                shots.Add(new Shot { Slot = slot, Region = target, Color = shooter.Color, Amount = amount });
                // An emptied cube leaves its slot at once, even while its chained partner keeps pouring.
                if (shooter.Ammo == 0) Slots[slot] = null;
                if (region.Remaining == 0) OpenNeighbours();
                Revision++;
            }
            for (int i = 0; i < Slots.Length; i++)
                if (Slots[i] != null && Slots[i].Ammo == 0) Slots[i] = null;
            Evaluate();
            return shots;
        }

        void OpenNeighbours()
        {
            Region.OpenStamp = ++openStep;
            // A region opens only when one of its prerequisite regions is completely covered with sand
            // (confirmed in play: neighbours stay locked while the previous region is still filling).
            for (int i = 0; i < Regions.Length; i++) finished[i] = Regions[i].Remaining == 0 && (isCovered == null || isCovered(Regions[i]));
            for (int i = 0; i < Regions.Length; i++)
            {
                var region = Regions[i];
                if (region.Open) continue;
                // A locked part opens only when its padlock has received all of its key-cube sand.
                if (region.Data.IsUnlockedPart)
                {
                    foreach (int p in padlocks[i]) if (Regions[p].Remaining == 0) { region.Open = true; break; }
                    continue;
                }
                foreach (int p in prerequisites[i]) if (finished[p]) { region.Open = true; break; }
            }
        }

        void Evaluate()
        {
            MergeHalves();
            // Open regions whose prerequisites finished since the last tick before judging a loss;
            // otherwise a selection made right after sand settles could be declared "out of space" wrongly.
            OpenNeighbours();
            if (isCovered != null) for (int i = 0; i < Regions.Length; i++) if (Regions[i].Remaining == 0 && !isCovered(Regions[i])) return;
            if (Remaining == 0) { State = GameState.Won; return; }
            for (int i = 0; i < Slots.Length; i++) if (Target(Slots[i]) >= 0) return;
            for (int i = 0; i < Lanes.Length; i++) if (CanSelectLane(i)) return;
            State = GameState.Lost;
        }

        public int HintLane()
        {
            for (int i = 0; i < Lanes.Length; i++) if (CanSelectLane(i) && Target(Lanes[i].Peek()) >= 0) return i;
            // Choose the lane whose next useful color is nearest the front.
            int best = -1, distance = int.MaxValue;
            for (int i = 0; i < Lanes.Length; i++)
            {
                if(!CanSelectLane(i))continue;
                int d = 0;
                foreach (var s in Lanes[i]) { if (Useful(s) && d < distance) { best = i; distance = d; } d++; }
            }
            if (best >= 0) return best;
            // While something is still pouring, wait rather than spend a slot on a useless cube.
            if (Slots.Any(s => Target(s) >= 0)) return -1;
            // Nothing useful is reachable: admitting any cube still thaws ice and can pair a sleeping half.
            for (int i = 0; i < Lanes.Length; i++) if (CanSelectLane(i) && Lanes[i].Peek().Half) return i;
            for (int i = 0; i < Lanes.Length; i++) if (CanSelectLane(i)) return i;
            return -1;
        }

        // Would pour into an open region once free (ignores ice and half sleep).
        bool Useful(Shooter s)
        {
            return s != null && s.Ammo > 0 && Array.Exists(Regions, p => p.Open && p.Remaining > 0 && p.Data.ColorType == s.Color && p.Data.IsUnlockerPart == s.Key);
        }
    }
}


