using System;
namespace SandJamTest
{
    public sealed partial class SandGame
    {
        // Reconstructed rule: one thaw step per other character admitted to the stash.
        // Called only after successful selection, never for blocked clicks, ticks or row swaps.
        void AdvanceFreeze(int admitted)
        {
            foreach(var lane in Lanes)
                foreach(var shooter in lane)
                    if(shooter.IsFrozen)shooter.FreezeRemaining=Math.Max(0,shooter.FreezeRemaining-admitted);
        }
    }
}
