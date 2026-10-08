using System;

namespace SandJamTest
{
    // Game-wide event hub. Gameplay and UI raise events; managers (audio, save, stats, effects) listen,
    // so systems no longer poll each other's state.
    public static class GameEvents
    {
        public static event Action<int> LevelStarted;              // level number
        public static event Action<int> LevelWon;                  // level number
        public static event Action<int> LevelLost;                 // level number
        public static event Action<int, int> RegionCompleted;      // region index, colour type
        public static event Action<int> CharacterSelected;         // colour type
        public static event Action<int> CharacterEmptied;          // colour type
        public static event Action<string, int> BoosterUsed;       // action, coins paid (0 = from inventory)
        public static event Action<int, int, string> CoinsChanged; // balance, delta, reason
        public static event Action<string> ButtonClicked;          // button action id
        public static event Action SettingsChanged;
        public static event Action<int, int, int> SandPoured;      // colour type, sand units, level uiDivider (units per board number)

        public static void RaiseLevelStarted(int level) { if (LevelStarted != null) LevelStarted(level); }
        public static void RaiseLevelWon(int level) { if (LevelWon != null) LevelWon(level); }
        public static void RaiseLevelLost(int level) { if (LevelLost != null) LevelLost(level); }
        public static void RaiseRegionCompleted(int region, int color) { if (RegionCompleted != null) RegionCompleted(region, color); }
        public static void RaiseCharacterSelected(int color) { if (CharacterSelected != null) CharacterSelected(color); }
        public static void RaiseCharacterEmptied(int color) { if (CharacterEmptied != null) CharacterEmptied(color); }
        public static void RaiseBoosterUsed(string action, int paid) { if (BoosterUsed != null) BoosterUsed(action, paid); }
        public static void RaiseCoinsChanged(int balance, int delta, string reason) { if (CoinsChanged != null) CoinsChanged(balance, delta, reason); }
        public static void RaiseButtonClicked(string action) { if (ButtonClicked != null) ButtonClicked(action); }
        public static void RaiseSettingsChanged() { if (SettingsChanged != null) SettingsChanged(); }
        public static void RaiseSandPoured(int color, int amount, int divider) { if (SandPoured != null) SandPoured(color, amount, divider); }
    }
}
