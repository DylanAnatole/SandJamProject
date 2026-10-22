using System.Collections.Generic;
using UnityEngine;

namespace SandJamTest
{
    public enum Sfx { Select, Victory, Pop, Button, ClaimCoin, ClaimGift, LevelCompletePanel }

    // Plays the original sound effects in response to game events and honours the sound setting. As in the original
    // (recording 1, every sound matched against the game's clips): a tap on a cube plays Input, PLAY plays pop, the
    // finished picture plays the level-complete jingle and the Level Complete card Victory (~4.3 s later); pouring,
    // filled regions, emptied cubes and the win reward are silent.
    public sealed class AudioManager : Singleton<AudioManager>
    {
        readonly Dictionary<Sfx, AudioClip> clips = new Dictionary<Sfx, AudioClip>();
        static readonly Dictionary<Sfx, float> Volumes = new Dictionary<Sfx, float>
        {
            { Sfx.Select, .25f }, { Sfx.Victory, .45f }, { Sfx.Pop, .25f },
            { Sfx.Button, .35f }, { Sfx.ClaimCoin, .4f }, { Sfx.ClaimGift, .4f }, { Sfx.LevelCompletePanel, .4f },
        };
        AudioSource source;

        public static AudioManager Create(Transform parent)
        {
            var manager = new GameObject("Audio Manager").AddComponent<AudioManager>();
            manager.transform.SetParent(parent, false);
            return manager;
        }

        protected override void OnSingletonAwake()
        {
            source = gameObject.AddComponent<AudioSource>(); source.playOnAwake = false;
            Load(Sfx.Select, "Select"); Load(Sfx.Victory, "Victory");
            Load(Sfx.Pop, "Pop"); Load(Sfx.Button, "Button"); Load(Sfx.ClaimCoin, "ClaimCoin");
            Load(Sfx.ClaimGift, "ClaimGift"); Load(Sfx.LevelCompletePanel, "LevelCompletePanel");
            ApplySetting();
            GameEvents.CharacterSelected += OnSelected;
            GameEvents.LevelWon += OnWon;
            GameEvents.ButtonClicked += OnButton;
            GameEvents.CoinsChanged += OnCoins;
            GameEvents.SettingsChanged += ApplySetting;
            GameEvents.BoosterUsed += OnBooster;
        }
        void OnBooster(string action, int paid) { Play(Sfx.Select); }

        protected override void OnSingletonDestroy()
        {
            GameEvents.CharacterSelected -= OnSelected;
            GameEvents.LevelWon -= OnWon;
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
        void OnWon(int level) { Play(Sfx.LevelCompletePanel); }
        void OnButton(string action) { Play(action == "play" ? Sfx.Pop : Sfx.Button); }
        void OnCoins(int balance, int delta, string reason) { if (delta > 0 && reason == "double") Play(Sfx.ClaimCoin); }
    }
}
