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
            public bool Locked;                  // the next collection, shown dark with a padlock on the Gallery
            public string Name, Cover;           // Cover = "Gallery/cover-N" or null (use RenderId)
            public string RenderId;              // first picture of the unlocked style
            // The block's own art style, as shown on the GALLERY page (recording: "MUSICIAN" page = the
            // Michael Jackson cover and the musician pictures of levels 241-255).
            public string StyleName, StyleCover, StyleRenderId;
            public int Completed(int wonLevel) { return Mathf.Clamp(wonLevel - Start + 1, 0, Count); }
            public int Percent(int wonLevel) { return Count == 0 ? 0 : 100 * Completed(wonLevel) / Count; } // floor: 6%, 13%, 93%
        }

        const int TutorialLevels = 15, FallbackCount = 15;
        // FeatureConfig.FeatureList (FeatureInfo, LevelCount, the cover its sprite points to), all 57 art styles. Styles
        // 18+ use a level's flat picture as cover (CustomSprite), copied locally to Resources/GalleryCovers.
        sealed class Style { public readonly string Name, Cover; public readonly int Count; public Style(string name, int count, string cover) { Name = name; Count = count; Cover = cover; } }
        static readonly Style[] Styles =
        {
            new Style("Casual Arts", 10, "Gallery/cover-3"),
            new Style("Animals", 10, "Gallery/cover-2"),
            new Style("Abstract", 10, "Gallery/cover-5"),
            new Style("Heroes & Celebrities", 15, "Gallery/cover-1"),
            new Style("Landscape & Portraits", 15, "Gallery/cover-4"),
            new Style("Cartoons", 15, "Gallery/cover-6"),
            new Style("Princesses", 15, "Gallery/cover-7"),
            new Style("Wild Life", 15, "Gallery/cover-8"),
            new Style("Women In Art", 15, "Gallery/cover-12"),
            new Style("Pop Art", 15, "Gallery/cover-9"),
            new Style("Flowers", 15, "Gallery/cover-10"),
            new Style("Summer Vibes", 15, "Gallery/cover-11"),
            new Style("Cosmic Looks", 15, "Gallery/cover-15"),
            new Style("Sports", 15, "Gallery/cover-14"),
            new Style("Travel & Journey", 15, "Gallery/cover-13"),
            new Style("Family & Love", 15, "Gallery/cover-16"),
            new Style("Musician", 15, "Gallery/cover-17"),
            new Style("Accessories", 15, "GalleryCovers/112x84_Accessories_1"),
            new Style("Makeup", 15, "GalleryCovers/MakeUp_1"),
            new Style("Perfumes", 15, "GalleryCovers/Perfumes_1"),
            new Style("School", 15, "GalleryCovers/School_1"),
            new Style("Fusion Frames", 15, "GalleryCovers/112x84_New_WomenInArt_6"),
            new Style("Parallel Visions", 15, "GalleryCovers/112x84_wildlife_4"),
            new Style("Echoes of Culture", 15, "GalleryCovers/112x84_Cartoon_4"),
            new Style("Infinite Canvas", 15, "GalleryCovers/112x84_Celebs_5"),
            new Style("Iconic Realms", 15, "GalleryCovers/112x84_musicians_9"),
            new Style("Mosaic Worlds", 15, "GalleryCovers/112x84_Level31_Heros"),
            new Style("Myth & Modernity", 15, "GalleryCovers/112x84_New_Magic_3"),
            new Style("Vivid Chronicles", 15, "GalleryCovers/112x84_New_PopArt_6"),
            new Style("Imaginary Realms", 15, "GalleryCovers/112x84_New_Magic_6"),
            new Style("Art Collisions", 15, "GalleryCovers/112x84_NewLevel_Princess_8"),
            new Style("Hidden Icons", 15, "GalleryCovers/112x84_musicians_14"),
            new Style("Timeless Frames", 15, "GalleryCovers/112x84_musicians_10"),
            new Style("Bold Perspectives", 15, "GalleryCovers/112x84_Travel_10"),
            new Style("Creative Blend", 15, "GalleryCovers/112x84_Travel_5"),
            new Style("Global Visions", 15, "GalleryCovers/112x84_wildlife_3"),
            new Style("Shattered Styles", 15, "GalleryCovers/112x84_Flower_10"),
            new Style("Urban Legends", 15, "GalleryCovers/112x84_New_Magic_14"),
            new Style("Altered Realities", 15, "GalleryCovers/112x84_Family_5"),
            new Style("Picture Mosaic", 15, "GalleryCovers/112x84_Cartoon_5"),
            new Style("Spectrum Stories", 15, "GalleryCovers/112x84_New_PopArt_3"),
            new Style("Icons Reimagined", 15, "GalleryCovers/112x84_New_WomenInArt_8"),
            new Style("Chromatic Worlds", 15, "GalleryCovers/112x84_New_Sport_9"),
            new Style("Crossover Culture", 15, "GalleryCovers/112x84_New_Magic_9"),
            new Style("Visual Mosaic", 15, "GalleryCovers/112x84_New_PopArt_14"),
            new Style("Framed Fusion", 15, "GalleryCovers/112x84_wildlife_1"),
            new Style("Color Realms", 15, "GalleryCovers/112x84_wildlife_3"),
            new Style("Surreal Icons", 15, "GalleryCovers/112x84_New_Sport_2"),
            new Style("Culture Collage", 15, "GalleryCovers/112x84_Level18_H"),
            new Style("Mythic Portraits", 15, "GalleryCovers/112x84_NewLevel_Princess_8"),
            new Style("Infinite Blend", 15, "GalleryCovers/112x84_Travel_11"),
            new Style("Legendary Faces", 15, "GalleryCovers/112x84_New_Sport_13"),
            new Style("Collective Canvas", 15, "GalleryCovers/112x84_New_Sport_12"),
            new Style("Pop Visions", 15, "GalleryCovers/112x84_New_WomenInArt_8"),
            new Style("Motion & Still", 15, "GalleryCovers/112x84_Family_11"),
            new Style("Faces in Focus", 15, "GalleryCovers/112x84_New_Magic_6"),
            new Style("Timeless Icons", 15, "GalleryCovers/112x84_musicians_9"),
        };

        // Block containing the level: start and size (block 0 = tutorial).
        static void Block(int level, out int index, out int start, out int count)
        {
            if (level <= TutorialLevels) { index = 0; start = 1; count = TutorialLevels; return; }
            start = TutorialLevels + 1;
            for (int i = 0; i < Styles.Length; i++)
            {
                if (level < start + Styles[i].Count) { index = i + 1; count = Styles[i].Count; return; }
                start += Styles[i].Count;
            }
            int extra = (level - start) / FallbackCount;
            index = Styles.Length + 1 + extra; start += extra * FallbackCount; count = FallbackCount;
        }

        // Collections the player has reached (block start <= current level), in order.
        public static System.Collections.Generic.List<Info> Unlocked()
        {
            var list = new System.Collections.Generic.List<Info>();
            for (int level = TutorialLevels + 1; level <= Campaign.LevelNumber; )
            {
                var info = ForLevel(level); list.Add(info); level = info.Start + info.Count;
            }
            return list;
        }
        // The collection after the last unlocked one, for the Gallery's locked preview (null past the last style).
        public static Info NextLocked(System.Collections.Generic.List<Info> unlocked)
        {
            if (unlocked.Count == 0) return null;
            var last = unlocked[unlocked.Count - 1];
            if (last.Index >= Styles.Length) return null;
            var info = ForLevel(last.Start + last.Count); info.Locked = true;
            return info;
        }

        // Milestones at 1/3, 2/3 and the full collection (original GalleryConfig: 20/30/gift for the first three
        // collections, 40/60/gift afterwards). Gift contents are remote in the original; estimated here.
        public static int MilestoneCoins(Info info, int milestone)
        {
            if (milestone == 2) return 150; // recording 3 (65-67 s): the gift adds 150 coins, nothing else
            return info.Index <= 3 ? (milestone == 0 ? 20 : 30) : (milestone == 0 ? 40 : 60);
        }
        public static bool MilestoneReached(Info info, int milestone)
        {
            int done = info.Completed(Campaign.LevelNumber - 1);
            return done >= Mathf.CeilToInt(info.Count * (milestone + 1) / 3f);
        }
        public static bool MilestoneClaimed(Info info, int milestone) { return SaveManager.Data.galleryClaims.Contains(info.Index + ":" + milestone); }
        public static bool ClaimMilestone(Info info, int milestone)
        {
            if (!MilestoneReached(info, milestone) || MilestoneClaimed(info, milestone)) return false;
            SaveManager.Data.galleryClaims.Add(info.Index + ":" + milestone);
            EconomyManager.Add(MilestoneCoins(info, milestone), "gallery");
            SaveManager.Save();
            GameEvents.RaiseGalleryChanged();
            return true;
        }

        // Original cards: the picture of a won level waits face down ("? Claim") until the player claims it, one by
        // one or with "Claim All Cards". Stored with the milestone claims as "card:<level>".
        public static bool CardWaiting(int level) { return level <= Campaign.LevelNumber - 1 && !SaveManager.Data.galleryClaims.Contains("card:" + level); }
        public static bool ClaimCard(int level)
        {
            if (!CardWaiting(level)) return false;
            SaveManager.Data.galleryClaims.Add("card:" + level);
            SaveManager.Save();
            GameEvents.RaiseGalleryChanged();
            return true;
        }
        public static void ClaimAllCards(Info info)
        {
            bool any = false;
            for (int level = info.Start; level < info.Start + info.Count; level++)
                if (CardWaiting(level)) { SaveManager.Data.galleryClaims.Add("card:" + level); any = true; }
            if (!any) return;
            SaveManager.Save();
            GameEvents.RaiseGalleryChanged();
        }

        // Original navbar badge: a card waits to be claimed or a milestone reward to be collected.
        public static bool HasUnclaimedReward()
        {
            foreach (var info in Unlocked())
            {
                for (int m = 0; m < 3; m++)
                    if (MilestoneReached(info, m) && !MilestoneClaimed(info, m)) return true;
                for (int level = info.Start; level < info.Start + info.Count; level++)
                    if (CardWaiting(level)) return true;
            }
            return false;
        }

        public static Info ForLevel(int level)
        {
            int index, start, count;
            Block(level, out index, out start, out count);
            var info = new Info { Index = index, Start = start, Count = count };
            // The collection being filled is the art style of the next block.
            int unlock = index + 1; // feature number (1-based) of the next block = the style this block unlocks
            if (unlock >= 1 && unlock <= Styles.Length) { info.Name = Styles[unlock - 1].Name; info.Cover = Styles[unlock - 1].Cover; }
            var levels = Campaign.Levels;
            string first = levels[(start + count - 1) % levels.Length]; // first level of the next block
            info.RenderId = System.Text.RegularExpressions.Regex.Replace(first, "_Dupe$", "");
            if (info.Name == null)
            {
                string category = Collections.CategoryOf(first);
                info.Name = category != null ? Collections.DisplayName(category) : "Collection " + index;
            }
            string own = levels[(start - 1) % levels.Length];
            info.StyleRenderId = System.Text.RegularExpressions.Regex.Replace(own, "_Dupe$", "");
            if (index >= 1 && index <= Styles.Length) { info.StyleName = Styles[index - 1].Name; info.StyleCover = Styles[index - 1].Cover; }
            else
            {
                string category = Collections.CategoryOf(own);
                info.StyleName = category != null ? Collections.DisplayName(category) : "Collection " + index;
            }
            return info;
        }
    }
}
