namespace SandJamTest
{
    // Game-wide event hub (Observer pattern, see Core/Patterns/Observer.cs). Gameplay and UI raise events; managers
    // (audio, save, stats, effects, UI badges) listen with += / -=, so systems never poll each other's state.
    // To add an event: declare a GameEvent field here and a Raise method next to the others.
    public static class GameEvents
    {
        public static GameEvent<int> LevelStarted = new GameEvent<int>();                       // level number
        public static GameEvent<int> LevelWon = new GameEvent<int>();                           // level number
        public static GameEvent<int> LevelLost = new GameEvent<int>();                          // level number
        public static GameEvent<int, int> RegionCompleted = new GameEvent<int, int>();          // region index, colour type
        public static GameEvent<int> CharacterSelected = new GameEvent<int>();                  // colour type
        public static GameEvent<int> CharacterEmptied = new GameEvent<int>();                   // colour type
        public static GameEvent<string, int> BoosterUsed = new GameEvent<string, int>();        // action, coins paid (0 = from inventory)
        public static GameEvent<int, int, string> CoinsChanged = new GameEvent<int, int, string>(); // balance, delta, reason
        public static GameEvent<string> ButtonClicked = new GameEvent<string>();                // button action id
        public static GameEvent SettingsChanged = new GameEvent();
        public static GameEvent GalleryChanged = new GameEvent();                               // a collection milestone was reached or claimed
        public static GameEvent<int, int, int> SandPoured = new GameEvent<int, int, int>();     // colour type, sand units, level uiDivider

        public static void RaiseLevelStarted(int level) { LevelStarted.Raise(level); }
        public static void RaiseLevelWon(int level) { LevelWon.Raise(level); }
        public static void RaiseLevelLost(int level) { LevelLost.Raise(level); }
        public static void RaiseRegionCompleted(int region, int color) { RegionCompleted.Raise(region, color); }
        public static void RaiseCharacterSelected(int color) { CharacterSelected.Raise(color); }
        public static void RaiseCharacterEmptied(int color) { CharacterEmptied.Raise(color); }
        public static void RaiseBoosterUsed(string action, int paid) { BoosterUsed.Raise(action, paid); }
        public static void RaiseCoinsChanged(int balance, int delta, string reason) { CoinsChanged.Raise(balance, delta, reason); }
        public static void RaiseButtonClicked(string action) { ButtonClicked.Raise(action); }
        public static void RaiseSettingsChanged() { SettingsChanged.Raise(); }
        public static void RaiseGalleryChanged() { GalleryChanged.Raise(); }
        public static void RaiseSandPoured(int color, int amount, int divider) { SandPoured.Raise(color, amount, divider); }
    }
}
