using System.Text;

namespace SandJamTest
{
    // Loading-screen tips, copied from the original MainMenu_Basic scene (hint component, "hints" list).
    public static class LoadingHints
    {
        public static readonly string[] All =
        {
            "Some tiles need more pixels than others — aim smart to conserve your stickmen!",
            "Collect completed canvases in your Gallery to unlock exclusive rewards!",
            "Don't rush! Filling outer tiles first can unlock easier access to inner ones.",
            "Stash the right colors — sending the wrong ones wastes valuable space!",
            "If you tap the screen exactly 88 times during loading, the next level paints itself. (Probably not.)",
            "Legend says a 6-armed stickman once painted an entire canvas in 3 seconds. We’ve never seen him.",
            "Turning your phone upside down might reveal secret colors. (Spoiler: It won’t.)",
            "Equip the invisible hat to double your pixel accuracy. If you can't see it, you're wearing it.",
            "Tile #37 is cursed. Don’t stare too long or it might start blinking back.",
        };

        public static string Random() { return All[UnityEngine.Random.Range(0, All.Length)]; }

        // TextMesh has no word wrap: break the tip into lines of at most maxChars characters.
        public static string Wrap(string text, int maxChars)
        {
            var result = new StringBuilder(); int line = 0;
            foreach (var word in text.Split(' '))
            {
                if (line > 0 && line + 1 + word.Length > maxChars) { result.Append('\n'); line = 0; }
                else if (line > 0) { result.Append(' '); line++; }
                result.Append(word); line += word.Length;
            }
            return result.ToString();
        }
    }
}
