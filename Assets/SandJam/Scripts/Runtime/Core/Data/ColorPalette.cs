using UnityEngine;

namespace SandJamTest
{
    // Colour for a level ColorType. Uses the original ColorDataSO FinalColor (imported into
    // Resources/OriginalConfig) and falls back to the original RenderColor table when that is absent.
    public static class ColorPalette
    {
        public static Color Of(int color)
        {
            var original = OriginalConfig.Color(color);
            if (original != null && original.final.a > 0) { var c = original.final; c.a = 1; return c; }
            switch (color)
            {
                case 1: return Hex("BF1618");
                case 2: return Hex("75C935");
                case 3: return Hex("2E83D8");
                case 4: return Hex("FFE63B");
                case 5: return Hex("FF9A20");
                case 6: return Hex("FF6BBD");
                case 7: return Hex("6D30D8");
                case 8: return Hex("282833");
                case 9: return Hex("FFFFFF");
                case 10: return Hex("C88B52");
                case 11: return Hex("8E8F9B");
                case 12: return Hex("FFEDA0");
                case 13: return Hex("444451");
                case 14: return Hex("FF8A8A");
                case 15: return Hex("267348");
                case 16: return Hex("724524");
                default: return Hex("8797A3");
            }
        }

        static Color Hex(string value) { Color c; ColorUtility.TryParseHtmlString("#" + value, out c); return c; }
    }
}
