using System.Collections.Generic;
using UnityEngine;

namespace SandJamTest
{
    public enum Sfx { Select, RegionComplete, Victory, Pop, Button, ClaimCoin, ClaimGift, LevelCompletePanel }

    // Plays the original sound effects in response to game events and honours the sound setting.
    public sealed class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }
        readonly Dictionary<Sfx, AudioClip> clips = new Dictionary<Sfx, AudioClip>();
        static readonly Dictionary<Sfx, float> Volumes = new Dictionary<Sfx, float>
        {
            { Sfx.Select, .25f }, { Sfx.RegionComplete, .3f }, { Sfx.Victory, .45f }, { Sfx.Pop, .25f },
            { Sfx.Button, .35f }, { Sfx.ClaimCoin, .4f }, { Sfx.ClaimGift, .4f }, { Sfx.LevelCompletePanel, .4f },
        };
        AudioSource source, chime, hiss;
        float lastRegionSound, lastPour = -10, hissLevel;
        int regionStreak;
        const float HissVolume = .07f;

        public static AudioManager Create(Transform parent)
        {
            var manager = new GameObject("Audio Manager").AddComponent<AudioManager>();
            manager.transform.SetParent(parent, false);
            return manager;
        }

        void Awake()
        {
            Instance = this;
            source = gameObject.AddComponent<AudioSource>(); source.playOnAwake = false;
            // Region chimes get their own source so their pitch can climb with a combo.
            chime = gameObject.AddComponent<AudioSource>(); chime.playOnAwake = false;
            // Soft looping hiss under the pour: sand trickling, faded in only while a cube is pouring.
            hiss = gameObject.AddComponent<AudioSource>(); hiss.playOnAwake = false; hiss.loop = true;
            hiss.clip = SandHiss(); hiss.volume = 0;
            Load(Sfx.Select, "Select"); Load(Sfx.RegionComplete, "RegionComplete"); Load(Sfx.Victory, "Victory");
            Load(Sfx.Pop, "Pop"); Load(Sfx.Button, "Button"); Load(Sfx.ClaimCoin, "ClaimCoin");
            Load(Sfx.ClaimGift, "ClaimGift"); Load(Sfx.LevelCompletePanel, "LevelCompletePanel");
            ApplySetting();
            GameEvents.CharacterSelected += OnSelected;
            GameEvents.RegionCompleted += OnRegion;
            GameEvents.LevelWon += OnWon;
            GameEvents.CharacterEmptied += OnEmptied;
            GameEvents.ButtonClicked += OnButton;
            GameEvents.CoinsChanged += OnCoins;
            GameEvents.SettingsChanged += ApplySetting;
            GameEvents.BoosterUsed += OnBooster;
            GameEvents.SandPoured += OnPoured;
        }
        void OnPoured(int color, int amount, int divider) { lastPour = Time.unscaledTime; }

        void Update()
        {
            bool pouring = SaveManager.Data.soundOn && Time.unscaledTime - lastPour < .18f;
            hissLevel = Mathf.MoveTowards(hissLevel, pouring ? 1 : 0, Time.unscaledDeltaTime * (pouring ? 5 : 2.5f));
            hiss.volume = hissLevel * HissVolume;
            if (hissLevel > 0 && !hiss.isPlaying) { hiss.time = Random.Range(0, hiss.clip.length * .9f); hiss.Play(); }
            else if (hissLevel <= 0 && hiss.isPlaying) hiss.Stop();
        }

        // Two seconds of soft, low-passed noise with sparse grain ticks; the ends are cross-faded so it loops cleanly.
        static AudioClip SandHiss()
        {
            const int rate = 44100, length = rate * 2, fade = rate / 5;
            var data = new float[length + fade];
            var random = new System.Random(7);
            float low = 0, band = 0, peak = 0;
            for (int i = 0; i < data.Length; i++)
            {
                float white = (float)random.NextDouble() * 2 - 1;
                low += (white - low) * .12f;          // remove harsh highs
                band += (low - band) * .01f;          // and the rumble
                float v = low - band;
                if (random.NextDouble() < .0025) v += ((float)random.NextDouble() - .5f) * .6f; // grain ticks
                data[i] = v; peak = Mathf.Max(peak, Mathf.Abs(v));
            }
            var samples = new float[length];
            for (int i = 0; i < length; i++)
            {
                float v = data[i];
                if (i < fade) { float t = i / (float)fade; v = v * t + data[length + i] * (1 - t); }
                samples[i] = v / Mathf.Max(.0001f, peak) * .8f;
            }
            var clip = AudioClip.Create("Sand hiss", length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }
        void OnBooster(string action, int paid) { Play(Sfx.Select); }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            GameEvents.CharacterSelected -= OnSelected;
            GameEvents.RegionCompleted -= OnRegion;
            GameEvents.LevelWon -= OnWon;
            GameEvents.CharacterEmptied -= OnEmptied;
            GameEvents.ButtonClicked -= OnButton;
            GameEvents.CoinsChanged -= OnCoins;
            GameEvents.SettingsChanged -= ApplySetting;
            GameEvents.BoosterUsed -= OnBooster;
            GameEvents.SandPoured -= OnPoured;
        }

        void Load(Sfx id, string name) { var clip = Resources.Load<AudioClip>("Original/" + name); if (clip) clips[id] = clip; }
        void ApplySetting() { AudioListener.volume = SaveManager.Data.soundOn ? 1 : 0; }

        public void Play(Sfx id)
        {
            AudioClip clip;
            if (!SaveManager.Data.soundOn || !clips.TryGetValue(id, out clip)) return;
            source.PlayOneShot(clip, Volumes[id]);
        }

        void OnSelected(int color) { Play(Sfx.Select); }
        // Several regions can finish on the same tick; one chime is enough.
        // Regions finished in quick succession climb about a semitone each (capped), a small combo reward.
        void OnRegion(int region, int color)
        {
            float since = Time.unscaledTime - lastRegionSound;
            if (since <= .12f) return;
            regionStreak = since < 2.5f ? Mathf.Min(regionStreak + 1, 7) : 0;
            lastRegionSound = Time.unscaledTime;
            AudioClip clip;
            if (!SaveManager.Data.soundOn || !clips.TryGetValue(Sfx.RegionComplete, out clip)) return;
            chime.pitch = Mathf.Pow(1.0595f, regionStreak);
            chime.PlayOneShot(clip, Volumes[Sfx.RegionComplete]);
        }
        void OnWon(int level) { Play(Sfx.Victory); }
        void OnEmptied(int color) { Play(Sfx.Pop); }
        void OnButton(string action) { Play(Sfx.Button); }
        void OnCoins(int balance, int delta, string reason) { if (delta > 0 && (reason == "win" || reason == "double")) Play(Sfx.ClaimCoin); }
    }
}
