using System;
using UnityEngine;

namespace SandJamTest
{
    // Short, light vibrations driven by game events (original VibrationManager): tap on pick, a firmer tick when a
    // region is completed, a double pulse on win, a soft buzz on loss. Respects the Vibration toggle in Settings.
    // Android uses the system Vibrator with millisecond durations (Handheld.Vibrate is ~400 ms, far too long);
    // iOS falls back to Handheld.Vibrate only for the big moments. No-op in the editor.
    public static class HapticsManager
    {
        public static bool Enabled { get { return SaveManager.Data.hapticsOn; } }
        static float lastTime = -1;

#if UNITY_ANDROID && !UNITY_EDITOR
        static AndroidJavaObject vibrator;
        static AndroidJavaObject Vibrator()
        {
            if (vibrator != null) return vibrator;
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                    vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
            }
            catch (Exception) { vibrator = null; }
            return vibrator;
        }
#endif

        // milliseconds: 10-15 light tap, 25-35 firm tick, 60+ strong.
        public static void Pulse(int milliseconds, bool important = false)
        {
            if (!Enabled || milliseconds <= 0) return;
            // Avoid buzzing continuously when events arrive in bursts.
            if (Time.unscaledTime - lastTime < .05f) return;
            lastTime = Time.unscaledTime;
#if UNITY_ANDROID && !UNITY_EDITOR
            try { var v = Vibrator(); if (v != null) v.Call("vibrate", (long)milliseconds); } catch (Exception) { }
#elif UNITY_IOS && !UNITY_EDITOR
            if (important) Handheld.Vibrate();
#endif
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Hook()
        {
            GameEvents.CharacterSelected -= OnSelected; GameEvents.CharacterSelected += OnSelected;
            GameEvents.RegionCompleted -= OnRegion; GameEvents.RegionCompleted += OnRegion;
            GameEvents.LevelWon -= OnWon; GameEvents.LevelWon += OnWon;
            GameEvents.LevelLost -= OnLost; GameEvents.LevelLost += OnLost;
            GameEvents.BoosterUsed -= OnBooster; GameEvents.BoosterUsed += OnBooster;
        }
        static void OnSelected(int color) { Pulse(12); }
        static void OnRegion(int region, int color) { Pulse(28); }
        static void OnWon(int level) { Pulse(70, true); }
        static void OnLost(int level) { Pulse(45, true); }
        static void OnBooster(string action, int paid) { Pulse(20); }
    }
}
