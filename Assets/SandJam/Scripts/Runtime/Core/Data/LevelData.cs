using System;
namespace SandJamTest
{
    [Serializable] public sealed class LevelData
    {
        public string sceneName;
        public int rowCount, columnCount, uiDivider, characterAmmo;
        public float gridSlotScale;
        public PartData[] parts;
        public int[] opr, opc, gridSlotNeedAmmoCount;
        public LaneData[] laneData;
    }
    [Serializable] public sealed class PartData
    {
        public string name;
        public int ColorType, amount;
        public bool isOpenedAtStart, IsUnlockedPart, IsUnlockerPart, isManualPosition;
        // On an unlocker (padlock) part: the name of the locked part it opens. Both share the same pixels.
        public string unlockerPartName;
        public string[] prerequests;
        public int[] rows, cols;
    }
    [Serializable] public sealed class LaneData { public CharacterData[] ColorAmmoDatas; }
    [Serializable] public sealed class CharacterData
    {
        public int ColorType, AmmoCount, FreezeCount;
        public bool IsChain, IsChainSameLane, IsSecret, IsFreeze, IsHalf, IsUnlocker;
    }
}
