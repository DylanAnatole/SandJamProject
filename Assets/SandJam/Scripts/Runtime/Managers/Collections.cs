using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SandJamTest
{
    // Gallery collections derived from original level ids ("112x84_Accessories_7" -> "Accessories").
    // Generic ids ("112x84_Level31_Tutorial", "112x84_Level3_B") belong to no collection.
    public static class Collections
    {
        static Dictionary<string, string[]> members;

        public static string CategoryOf(string levelId)
        {
            if (string.IsNullOrEmpty(levelId)) return null;
            string id = Regex.Replace(levelId, @"^\d+x\d+_", "");
            id = Regex.Replace(id, @"^(New_|NewLevel_|New)", "");
            id = Regex.Replace(id, @"[_ ]?\d+(_[A-Za-z]+)?$", "");
            if (id.Length < 3 || id.StartsWith("Level")) return null;
            return id.Replace(" ", "");
        }

        // "WomenInArt" -> "Women In Art", "musicians" -> "Musicians".
        public static string DisplayName(string category)
        {
            var spaced = Regex.Replace(category.Replace("_", " "), @"(?<=[a-z])(?=[A-Z])", " ");
            return spaced.Length > 0 ? char.ToUpper(spaced[0]) + spaced.Substring(1) : spaced;
        }

        static Dictionary<string, string[]> Members
        {
            get
            {
                if (members != null) return members;
                members = Campaign.Levels.Distinct()
                    .Select(id => new { id, category = CategoryOf(id) })
                    .Where(x => x.category != null)
                    .GroupBy(x => x.category, System.StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.id).ToArray(), System.StringComparer.OrdinalIgnoreCase);
                return members;
            }
        }

        public static int PercentComplete(string category)
        {
            string[] ids;
            if (category == null || !Members.TryGetValue(category, out ids) || ids.Length == 0) return 0;
            var done = SaveManager.Data.completedLevels;
            return (int)System.Math.Round(100.0 * ids.Count(done.Contains) / ids.Length);
        }

        public static void MarkCompleted(string levelId)
        {
            var done = SaveManager.Data.completedLevels;
            if (!string.IsNullOrEmpty(levelId) && !done.Contains(levelId)) { done.Add(levelId); SaveManager.Save(); }
        }
    }
}
