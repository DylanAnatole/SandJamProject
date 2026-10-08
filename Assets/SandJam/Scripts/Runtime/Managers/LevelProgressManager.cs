namespace SandJamTest
{
    // The campaign is linear, so a level is completed once the saved level number has passed it.
    // Kept as a thin view over SaveManager for existing callers and smoke tests.
    public sealed class LevelProgressManager
    {
        public LevelProgressManager(string profile) { }
        public bool IsCompleted(int number) { return number < SaveManager.Data.level; }
        public void MarkCompleted(int number)
        {
            var stats = SaveManager.Data.stats;
            if (number + 1 > stats.highestLevel) stats.highestLevel = number + 1;
            SaveManager.Save();
        }
        public void Clear(int number) { }
    }
}
