using UnityEngine;

namespace SandJamTest.Scene3D
{
    // Settings popup rebuilt from the original UiSettingsPanel (Game scene, 1080x1920 canvas): light-blue panel,
    // blue ribbon with the SETTINGS wording, Vibration and Sounds rows with swipe toggles, red exit button,
    // white buttons and a big green HOME button. Positions are the original RectTransform values.
    public sealed partial class PopupScreens
    {
        const float CanvasUnit = 5.2f / 1080f;   // original canvas pixels → popup units
        static readonly Vector2 PanelCentre = new Vector2(0, 130.75f);
        SpriteRenderer soundKnob, hapticKnob;
        GameObject settingsReplay, settingsHome;

        static Vector3 Px(float x, float y, float z) { return new Vector3((PanelCentre.x + x) * CanvasUnit, (PanelCentre.y + y) * CanvasUnit, z); }

        void BuildSettings()
        {
            settings = Blank("Settings popup");
            settings.RightButton.transform.parent.gameObject.SetActive(false);
            Sized("Settings/bg-settings", 0, 0, 867.2f, 1221.7f, -6.2f);
            Sized("Settings/ribbon-blue", 0, 429, 856.8f, 244.3f, -6.5f);
            Sized("Settings/wording-settings", 0, 438.6f, 364, 116.2f, -6.6f);
            hapticKnob = SettingsRow(302, "Rung", "toggle-haptics");
            soundKnob = SettingsRow(108, "Âm thanh", "toggle-sound");
            // Red exit button (anchored to the panel's top-right corner).
            Sized("Settings/ExitButton_0", 420.6f, 574.5f, 93.6f, 103, -6.7f);
            Hit(settings, Px(420.6f, 574.5f, -8.5f), new Vector2(.6f, .6f), "close");
            // Original: Privacy Policy (y 504) and Restore Purchases (y 343) white buttons, HOME green button (y 157),
            // all anchored to the panel bottom. The clone keeps Restore's slot for "Chơi lại" and the HOME button.
            settingsReplay = SettingsButton("Settings/button-white", -611 + 343, 600, 190, "Chơi lại", "retry", new Color(.17f, .2f, .42f));
            settingsHome = SettingsButton("Settings/button-green", -611 + 157, 768, 244, "TRANG CHỦ", "home", Color.white);
        }

        // Slot with the setting name on the left and a swipe toggle on the right. Returns the knob.
        SpriteRenderer SettingsRow(float y, string label, string action)
        {
            Sized("Settings/bg-settings-slot", 0, y, 712.76f, 174.5f, -6.3f);
            Sized("Settings/bg-settings-slot-button", 205.4f, y, 279.3f, 96.6f, -6.4f);
            var name = Label(settings, label + " label", Px(-142, y, -6.5f), label, .045f, new Color(.17f, .2f, .42f));
            name.anchor = TextAnchor.MiddleCenter;
            var knob = Sized("Settings/button-off", 205.4f - 68, y, 123.9f, 88.2f, -6.5f);
            Hit(settings, Px(0, y, -8.5f), new Vector2(712.76f * CanvasUnit, 174.5f * CanvasUnit), action);
            return knob;
        }

        GameObject SettingsButton(string sprite, float y, float width, float height, string text, string action, Color textColor)
        {
            var art = Sized(sprite, 0, y, width, height, -6.4f);
            var root = art.gameObject;
            var label = textColor == Color.white ? OutlinedLabel(settings, text + " label", Px(0, y + 8, -6.6f), .055f) : new[] { Label(settings, text + " label", Px(0, y + 4, -6.6f), text, .05f, textColor) };
            SetText(label, text);
            foreach (var t in label) t.transform.SetParent(root.transform, true);
            var hit = Hit(settings, Px(0, y, -8.5f), new Vector2(art.bounds.size.x, art.bounds.size.y), action);
            hit.transform.SetParent(root.transform, true);
            return root;
        }

        // Image stretched to its original RectTransform size (uGUI Image without preserve-aspect).
        SpriteRenderer Sized(string sprite, float x, float y, float width, float height, float z)
        {
            var r = Art(settings, sprite, Px(x, y, z), width * CanvasUnit);
            if (r.sprite) r.transform.localScale = new Vector3(width * CanvasUnit / r.sprite.bounds.size.x, height * CanvasUnit / r.sprite.bounds.size.y, 1);
            return r;
        }
        GameObject Hit(Popup p, Vector3 local, Vector2 size, string action)
        {
            var hit = Child<BoxCollider>(p, "Hit area - " + action, local);
            hit.gameObject.layer = 9; hit.size = new Vector3(size.x, size.y, .2f);
            hit.gameObject.AddComponent<VideoUiButton>().Action = action;
            return hit.gameObject;
        }

        // Knob on the right and green when on, on the left and grey when off (original SwipeOnPos / SwipeOffPos).
        void SetToggle(SpriteRenderer knob, bool on)
        {
            var sprite = Resources.Load<Sprite>(on ? "Settings/button-on" : "Settings/button-off");
            if (sprite) knob.sprite = sprite;
            var p = knob.transform.localPosition;
            knob.transform.localPosition = new Vector3((PanelCentre.x + 205.4f + (on ? 68 : -68)) * CanvasUnit, p.y, p.z);
        }
        void RefreshSettings()
        {
            SetToggle(soundKnob, Campaign.SoundOn);
            SetToggle(hapticKnob, SaveManager.Data.hapticsOn);
        }

        public void ShowSettings()
        {
            bool inGame = screen.Current == VideoScreen.Page.Gameplay;
            // Leaving or restarting only makes sense during a level.
            settingsReplay.SetActive(inGame); settingsHome.SetActive(inGame);
            RefreshSettings();
            Show(settings);
        }
    }
}
