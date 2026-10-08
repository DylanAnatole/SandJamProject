using UnityEngine;

namespace SandJamTest
{
    // Stage colour themes from the original Game scene (ColorChanger.ColorChangeDatas):
    // BG1 = backdrop, BG2 = backdrop shade, Frame = picture frame/stand, Side = lane banks, Obstacle = obstacle tiles.
    // Display* are the on-screen colours sampled from gameplay recordings (lighting included); the stage uses those.
    public struct StageTheme
    {
        public Color Bg1, Bg2, Frame, Side, Obstacle;
        public Color DisplayTop, DisplayMiddle, DisplayFrame, DisplaySide;
        public StageTheme(int bg1, int bg2, int frame, int side, int obstacle)
        {
            Bg1 = Hex(bg1); Bg2 = Hex(bg2); Frame = Hex(frame); Side = Hex(side); Obstacle = Hex(obstacle);
            DisplayTop = DisplayMiddle = Bg1; DisplayFrame = Frame; DisplaySide = Side;
        }
        public StageTheme Seen(int top, int middle, int frame, int side)
        {
            var t = this; t.DisplayTop = Hex(top); t.DisplayMiddle = Hex(middle); t.DisplayFrame = Hex(frame); t.DisplaySide = Hex(side); return t;
        }
        static Color Hex(int rgb) { return new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f); }
    }

    public static class OriginalThemes
    {
        public static readonly StageTheme[] All =
        {
            new StageTheme(0x789DBC, 0x241365, 0x3A557B, 0x2B3348, 0x7598CA).Seen(0x6B87A8, 0x7591B2, 0x698CAD, 0x3F4765), // 0 steel blue
            new StageTheme(0xF6BDFF, 0x180054, 0xCA62C8, 0x4A1E63, 0xF18EEE), // 1 pink (not seen in play)
            new StageTheme(0xE8EDFF, 0x7366D6, 0x423B6A, 0x504B6C, 0x8880B9), // 2 lavender
            new StageTheme(0x464A57, 0x000000, 0x4A4757, 0x42404D, 0x7D7A90).Seen(0x33333F, 0x393D47, 0x3E4250, 0x42404D), // 3 dark
            new StageTheme(0x572C84, 0x140026, 0x3C6FA6, 0x2B1743, 0x79BDE7), // 4 deep purple (not seen in play)
            new StageTheme(0x71608E, 0x140026, 0x71569C, 0x4A3C60, 0xBB99F1), // 5 dusk (not seen in play)
        };
        public const int Blue = 0, Lavender = 2, Dark = 3;

        // The theme changes every 15 levels (block = level / 15) and cycles lavender → dark → blue.
        // Matches every sampled level of both builds: 5-10 L, 15-25 D, 30-40 B, 45-55 L, 60-65 D, 76 B,
        // 107-113 D, 147 L, 254 D, 255-267 B (recordings of the current build and the walkthrough playlist).
        static readonly int[] Cycle = { Lavender, Dark, Blue };
        public const int BlockSize = 15;
        public static int IndexForLevel(int level) { return Cycle[(Mathf.Max(1, level) / BlockSize) % Cycle.Length]; }
        public static StageTheme ForLevel(int level) { return All[IndexForLevel(level)]; }
    }
}
