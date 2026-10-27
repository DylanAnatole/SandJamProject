using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SandJamTest
{
    // Gallery collections derived from original level ids ("112x84_Accessories_7" -> "Accessories").
    // Generic ids ("112x84_Level31_Tutorial", "112x84_Level3_B") belong to no collection.
    public static class Collections
    {

        public static string CategoryOf(string levelId)
        {
            if (string.IsNullOrEmpty(levelId)) return null;
            string id = Regex.Replace(levelId, @"^\d+x\d+_", "");
            id = Regex.Replace(id, @"^(New_|NewLevel_|New)", "");
            id = Regex.Replace(id, @"[_ ]?\d+(_[A-Za-z]+)?$", "");
            if (id.Length < 3 || id.StartsWith("Level", System.StringComparison.Ordinal)) return null;
            return id.Replace(" ", "");
        }

        // "WomenInArt" -> "Women In Art", "musicians" -> "Musicians".
        public static string DisplayName(string category)
        {
            if (category == "MakeUp") return "Makeup"; // original card title
            var spaced = Regex.Replace(category.Replace("_", " "), @"(?<=[a-z])(?=[A-Z])", " ");
            return spaced.Length > 0 ? char.ToUpper(spaced[0]) + spaced.Substring(1) : spaced;
        }

        public static void MarkCompleted(string levelId)
        {
            var done = SaveManager.Data.completedLevels;
            if (!string.IsNullOrEmpty(levelId) && !done.Contains(levelId)) { done.Add(levelId); SaveManager.Save(); }
        }
    }
}
