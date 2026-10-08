namespace SandJamTest
{
    public enum GameState { Playing, Won, Lost }
    public sealed class Shooter
    {
        public readonly int Color;
        public int InitialAmmo { get; internal set; }
        // Secret characters show as "?" until they reach the front of their lane; rules are unchanged.
        public readonly bool Secret;
        // Key cube (IsUnlocker): pours only into its colour's padlock part, never into ordinary regions.
        public readonly bool Key;
        // Half cube: asleep in the stash until a same-colour half joins it; the pair merges into one full cube.
        public bool Half { get; internal set; }
        public readonly bool StartedHalf;
        // Set on the half that was absorbed by a merge (its view walks into the partner and disappears).
        public Shooter MergedInto { get; internal set; }
        public int Ammo;
        public int FreezeRemaining { get; internal set; }
        public bool IsFrozen { get { return FreezeRemaining > 0; } }
        public Shooter Partner { get; internal set; }
        public Shooter(CharacterData data)
        {
            Color = data.ColorType; Ammo = data.AmmoCount; InitialAmmo = data.AmmoCount;
            Secret = data.IsSecret; Key = data.IsUnlocker; Half = StartedHalf = data.IsHalf;
            FreezeRemaining = data.IsFreeze ? data.FreezeCount : 0;
        }
    }
    public sealed class Region
    {
        public readonly PartData Data;
        public int Remaining;
        bool open;
        public bool Revealed;
        // Order in which regions opened (0 = open at start); used to pick which region a cube pours into.
        public int OpenedOrder { get; private set; }
        // Lowest grid row of the region (row 0 = bottom of the picture).
        public readonly int LowestRow;
        // Regions opened during the same game step share one stamp (SandGame advances it every step).
        public static int OpenStamp;
        public bool Open
        {
            get { return open; }
            set { if (value && !open) OpenedOrder = OpenStamp; open = value; }
        }
        public bool InformationVisible { get { return Open || Revealed; } }
        public Region(PartData data)
        {
            Data = data; Remaining = data.amount; open = data.isOpenedAtStart;
            LowestRow = data.rows != null && data.rows.Length > 0 ? System.Linq.Enumerable.Min(data.rows) : 0;
        }
    }
    public struct Shot
    {
        public int Slot, Region, Color, Amount;
    }

}

