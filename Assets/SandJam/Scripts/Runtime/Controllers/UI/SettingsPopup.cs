using System.Collections.Generic;
using UnityEngine;

namespace SandJamTest.Scene3D
{
    // Settings popup rebuilt from the original UiSettingsPanel (1080x1920 canvas). Two variants, as in the original
    // scenes and the gameplay recording: Home (MainMenu) = Vibration, Sounds, Privacy Policy, Restore Purchases;
    // in a level (Game) = the same plus a big green HOME button. Grey SETTINGS title, red exit button, swipe toggles
    // with ON / OFF. Positions are the original RectTransform values.
    public sealed class SettingsPopup : PopupModule
    {
        internal const float CanvasUnit = 5.2f / 1080f;   // original canvas pixels → popup units
        Vector2 settingsCentre; float settingsHeight;
        readonly List<GameObject> settingsDynamic = new List<GameObject>();
        SpriteRenderer soundKnob, hapticKnob;
        TextMesh soundKnobText, hapticKnobText;

        Vector3 Px(float x, float y, float z) { return new Vector3((settingsCentre.x + x) * CanvasUnit, (settingsCentre.y + y) * CanvasUnit, z); }
        float Top(float y) { return settingsHeight / 2 + y; }      // anchor (.5, 1)
        float Bottom(float y) { return -settingsHeight / 2 + y; }  // anchor (.5, 0)

        Popup settings;
        public bool Visible { get { return IsOpen(settings); } }

        public override void Build()
        {
            settings = Blank("Settings popup");
            settings.RightButton.transform.parent.gameObject.SetActive(false);
            // Recording 2 (15 s): over Home the original washes the screen with grey (#3C3D44 at ~83%), its coin bar
            // included.
            settings.Root.transform.Find("Dark backdrop").GetComponent<SpriteRenderer>().color = new Color32(60, 61, 68, 212);
            PopupScreens.HideCoinBar(settings);
        }

        public override bool Handles(string action) { return action == "toggle-sound" || action == "toggle-haptics" || action == "privacy" || action == "restore"; }
        public override bool Handle(string action)
        {
            if (action == "toggle-sound") Campaign.SoundOn = !Campaign.SoundOn;
            else if (action == "toggle-haptics") { SaveManager.Data.hapticsOn = !SaveManager.Data.hapticsOn; SaveManager.Save(); }
            // "privacy" / "restore": no store or web page in the clone.
            RefreshSettings(true); return true;
        }
        public override void Tick()
        {
            if (!Visible) return;
            if (Input.GetKeyDown(KeyCode.Escape)) Close();
            foreach (var k in new[] { soundKnob, hapticKnob }) SlideKnob(k);
        }

        public void ShowSettings()
        {
            bool inGame = screen.Current == VideoScreen.Page.Gameplay;
            DestroyAll(settingsDynamic); knobSlides.Clear(); fitScale = 1;
            // Game: panel 1221.7 tall centred at y 130.75; MainMenu: 1081.5 tall at y 145.6 (260.6 - 115).
            settingsCentre = inGame ? new Vector2(0, 130.75f) : new Vector2(0, 145.6f);
            settingsHeight = inGame ? 1221.7f : 1081.5f;
            var panel = Sized("Settings/bg-settings", 0, 0, 867.2f, settingsHeight, -6.2f);
            Sized("Settings/SETTINGS", 0, Top(-140), 641 * .92f, 111 * .92f, -6.4f); // inside the panel's top edge
            hapticKnob = SettingsRow(Top(-308.85f), "Vibration", "toggle-haptics", out hapticKnobText);
            soundKnob = SettingsRow(Top(-502.85f), "Sounds", "toggle-sound", out soundKnobText);
            float closeY = settingsHeight / 2 - (inGame ? 36.5f : 11f);
            Sized("Settings/ExitButton_0", 433.6f - 13, closeY, 93.6f, 103, -6.7f);
            settingsDynamic.Add(Hit(settings, Px(420.6f, closeY, -8.5f), new Vector2(.6f, .6f), "close"));
            // The button art fills its whole rect (no transparent margin as in the original prefab), so drawn at the
            // original rect size neighbouring buttons touched / overlapped: draw them a bit smaller, same centres.
            SettingsButton("Settings/button-white", Bottom(inGame ? 503.9f : 357), 600 * .9f, 181 * .82f, "Privacy Policy", "privacy", new Color(.2f, .2f, .38f));
            SettingsButton("Settings/button-white", Bottom(inGame ? 343 : 178), 600 * .9f, 190 * .82f, "Restore Purchases", "restore", new Color(.2f, .2f, .38f));
            if (inGame) SettingsButton("Settings/button-green", Bottom(157), 768 * .9f, 244 * .78f, "HOME", "home", Color.white);
            RefreshSettings();
            // Home panel as measured on recording 2 (15 s): ~5% smaller, the panel 50 px higher than the canvas values
            // give, its content only 6 px higher.
            if (!inGame) { FitDynamic(.95f, 6); panel.transform.localPosition += Vector3.up * 44 * VideoScreen.Px; }
            Show(settings);
        }
        // Scales the panel's pieces about the popup centre and moves them up (popup space, at its full size).
        float fitScale = 1; // FitDynamic's scale: knob targets are computed in the panel's unscaled units
        void FitDynamic(float scale, float upPx)
        {
            fitScale = scale;
            var k = new Vector3(scale, scale, 1);
            foreach (var o in settingsDynamic)
            {
                if (!o) continue;
                var t = o.transform;
                t.localPosition = Vector3.Scale(t.localPosition, k) + Vector3.up * upPx * VideoScreen.Px;
                t.localScale = Vector3.Scale(t.localScale, k);
            }
        }

        // Slot with the setting name on the left and a swipe toggle on the right. Returns the knob.
        SpriteRenderer SettingsRow(float y, string label, string action, out TextMesh knobText)
        {
            Sized("Settings/bg-settings-slot", 0, y, 712.76f, 174.5f, -6.3f);
            Sized("Settings/bg-settings-slot-button", 205.4f, y, 279.3f, 96.6f, -6.4f);
            // Original: white name with a dark outline, left-aligned in the slot.
            var name = OutlinedLabel(settings, label + " label", Px(-318, y - 6, -6.5f), .047f);
            SetText(name, label);
            foreach (var t in name) { t.anchor = TextAnchor.MiddleLeft; t.alignment = TextAlignment.Left; }
            settingsDynamic.Add(name[0].gameObject);
            var knob = Sized("Settings/button-off", 205.4f - 68, y, 123.9f, 88.2f, -6.5f);
            knobText = Label(settings, label + " knob", Px(205.4f - 68, y, -6.6f), "OFF", .03f, Color.white);
            settingsDynamic.Add(knobText.gameObject);
            settingsDynamic.Add(Hit(settings, Px(0, y, -8.5f), new Vector2(712.76f * CanvasUnit, 174.5f * CanvasUnit), action));
            return knob;
        }

        void SettingsButton(string sprite, float y, float width, float height, string text, string action, Color textColor)
        {
            var art = Sized(sprite, 0, y, width, height, -6.4f);
            if (textColor == Color.white) { var l = OutlinedLabel(settings, text + " label", Px(0, y + 12, -6.6f), .06f); SetText(l, text); settingsDynamic.Add(l[0].gameObject); }
            else settingsDynamic.Add(Label(settings, text + " label", Px(0, y + 4, -6.6f), text, .038f, textColor).gameObject);
            settingsDynamic.Add(Hit(settings, Px(0, y, -8.5f), new Vector2(art.bounds.size.x, art.bounds.size.y), action));
        }

        // Image stretched to its original RectTransform size (uGUI Image without preserve-aspect).
        SpriteRenderer Sized(string sprite, float x, float y, float width, float height, float z)
        {
            var r = Art(settings, sprite, Px(x, y, z), width * CanvasUnit);
            r.SetLocalSize(width * CanvasUnit, height * CanvasUnit);
            settingsDynamic.Add(r.gameObject);
            return r;
        }
        // Knob on the right, green, "ON" when on; on the left, grey, "OFF" when off (original SwipeOnPos / SwipeOffPos).
        // Recording 2 (14.95-15.3 s): the knob takes its new look at once and slides across in ~0.33 s, easing out.
        const float KnobSlideSeconds = .33f;
        sealed class KnobSlide { public TextMesh Text; public float From, To, At = -1; } // At < 0: settled
        readonly System.Collections.Generic.Dictionary<SpriteRenderer, KnobSlide> knobSlides = new System.Collections.Generic.Dictionary<SpriteRenderer, KnobSlide>();
        void SetToggle(SpriteRenderer knob, TextMesh text, bool on, bool animate)
        {
            var sprite = Resources.Load<Sprite>(on ? "Settings/button-on" : "Settings/button-off");
            if (sprite) knob.sprite = sprite;
            text.text = on ? "ON" : "OFF"; text.color = on ? Color.white : new Color(.45f, .45f, .5f);
            KnobSlide slide;
            if (!knobSlides.TryGetValue(knob, out slide)) knobSlides[knob] = slide = new KnobSlide { Text = text };
            slide.From = knob.transform.localPosition.x; slide.To = (settingsCentre.x + 205.4f + (on ? 68 : -68)) * CanvasUnit * fitScale;
            slide.At = Time.unscaledTime;
            if (!animate) slide.From = slide.To;
            SlideKnob(knob);
        }
        void SlideKnob(SpriteRenderer knob)
        {
            KnobSlide slide;
            if (!knob || !knobSlides.TryGetValue(knob, out slide) || slide.At < 0) return; // settled: FitDynamic may move it
            float t = slide.From == slide.To ? 1 : Mathf.Clamp01((Time.unscaledTime - slide.At) / KnobSlideSeconds);
            if (t >= 1) slide.At = -1;
            float x = Mathf.Lerp(slide.From, slide.To, 1 - (1 - t) * (1 - t));
            foreach (var part in new[] { knob.transform, slide.Text.transform }) part.localPosition = new Vector3(x, part.localPosition.y, part.localPosition.z);
        }
        void RefreshSettings(bool animate = false)
        {
            if (!soundKnob) return;
            SetToggle(soundKnob, soundKnobText, Campaign.SoundOn, animate);
            SetToggle(hapticKnob, hapticKnobText, SaveManager.Data.hapticsOn, animate);
        }
    }
}
