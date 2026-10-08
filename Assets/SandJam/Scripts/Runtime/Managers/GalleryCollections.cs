using UnityEngine;

namespace SandJamTest
{
    // Gallery collections (original FeatureConfig + recordings). The campaign is split into blocks: the 15 tutorial
    // levels, then one block per art style (10 or 15 levels). While playing a block the CONGRATS card reveals, one
    // slice per win, the cover of the art style that the block unlocks — the next block's style:
    // levels 241-255 fill "Accessories" (heart ring, 93% after level 254), levels 256-270 fill "Makeup" (jar,
    // 6% after level 256); finishing the block opens that style with the NEW ART STYLE popup.
    public static class GalleryCollections
    {
        public sealed class Info
        {
            public int Index, Start, Count;      // Start/Count = the block being played
            public string Name, Cover;           // Cover = "Gallery/cover-N" or null (use RenderId)
            public string RenderId;              // first picture of the unlocked style
            public int Completed(int wonLevel) { return Mathf.Clamp(wonLevel - Start + 1, 0, Count); }
            public int Percent(int wonLevel) { return Count == 0 ? 0 : 100 * Completed(wonLevel) / Count; } // floor: 6%, 13%, 93%
        }

        const int TutorialLevels = 15, FallbackCount = 15;
        // FeatureConfig.FeatureList (FeatureInfo, LevelCount) and the cover each FeatureSprite points to.
        static readonly string[] Names = { "Casual Arts", "Animals", "Abstract", "Heroes & Celebrities", "Landscape & Portraits", "Cartoons",
            "Princesses", "Wild Life", "Women In Art", "Pop Art", "Flowers", "Summer Vibes", "Cosmic Looks", "Sports",
            "Travel & Journey", "Family & Love", "Musician" };
        static readonly int[] Counts = { 10, 10, 10, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15 };
        static readonly int[] Covers = { 3, 2, 5, 1, 4, 6, 7, 8, 12, 9, 10, 11, 15, 14, 13, 16, 17 };

        // Block containing the level: start and size (block 0 = tutorial).
        static void Block(int level, out int index, out int start, out int count)
        {
            if (level <= TutorialLevels) { index = 0; start = 1; count = TutorialLevels; return; }
            start = TutorialLevels + 1;
            for (int i = 0; i < Counts.Length; i++)
            {
                if (level < start + Counts[i]) { index = i + 1; count = Counts[i]; return; }
                start += Counts[i];
            }
            int extra = (level - start) / FallbackCount;
            index = Counts.Length + 1 + extra; start += extra * FallbackCount; count = FallbackCount;
        }

        public static Info ForLevel(int level)
        {
            int index, start, count;
            Block(level, out index, out start, out count);
            var info = new Info { Index = index, Start = start, Count = count };
            // The collection being filled is the art style of the next block.
            int unlock = index + 1; // feature number (1-based) of the next block = the style this block unlocks
            if (unlock >= 1 && unlock <= Names.Length) { info.Name = Names[unlock - 1]; info.Cover = "Gallery/cover-" + Covers[unlock - 1]; }
            var levels = Campaign.Levels;
            string first = levels[(start + count - 1) % levels.Length]; // first level of the next block
            info.RenderId = System.Text.RegularExpressions.Regex.Replace(first, "_Dupe$", "");
            if (info.Name == null)
            {
                string category = Collections.CategoryOf(first);
                info.Name = category != null ? Collections.DisplayName(category) : "Collection " + index;
            }
            return info;
        }
    }
}
