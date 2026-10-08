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
        AudioSource source;
        float lastRegionSound;

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
        void OnRegion(int region, int color) { if (Time.unscaledTime - lastRegionSound > .12f) { lastRegionSound = Time.unscaledTime; Play(Sfx.RegionComplete); } }
        void OnWon(int level) { Play(Sfx.Victory); }
        void OnEmptied(int color) { Play(Sfx.Pop); }
        void OnButton(string action) { Play(Sfx.Button); }
        void OnCoins(int balance, int delta, string reason) { if (delta > 0 && (reason == "win" || reason == "double")) Play(Sfx.ClaimCoin); }
    }
}
