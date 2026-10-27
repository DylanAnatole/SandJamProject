using UnityEngine;

namespace SandJamTest
{
    // Frame rate and render size for phones. Many phones render 1440x3200 (iPhones 1170x2532): drawing every native
    // pixel costs the GPU (several full-screen transparent layers) and heats the phone, with no visible gain for this
    // art. The render's short side is capped at 1080, aspect kept; the views re-fit when the screen size changes.
    public static class MobileDisplay
    {
        const int MaxShortSide = 1080;
        static bool configured;

        public static void Configure()
        {
            Application.targetFrameRate = 60;
            if (configured) return;
            configured = true;
            int shortSide = Mathf.Min(Screen.width, Screen.height);
            if (!Application.isMobilePlatform || shortSide <= MaxShortSide) return;
            float scale = (float)MaxShortSide / shortSide;
            Screen.SetResolution(Mathf.RoundToInt(Screen.width * scale), Mathf.RoundToInt(Screen.height * scale), true);
        }
    }
}
