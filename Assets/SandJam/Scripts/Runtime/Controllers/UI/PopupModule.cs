using System.Collections.Generic;
using UnityEngine;

namespace SandJamTest.Scene3D
{
    // One popup or page (Settings, Shop, Sand Quest, Gallery, daily tasks, lives...). PopupScreens owns the modules:
    // it builds them once, forwards button actions to the module that Handles them and ticks them every frame while a
    // popup is open. A module keeps its own state and uses the shared building blocks below (same names as on
    // PopupScreens, so popup code reads the same wherever it lives).
    //
    // Adding a popup: derive from PopupModule, build it in Build(), claim its action prefix in Handles(), react in
    // Handle(), animate in Tick(), and register it with Add(new ...) in PopupScreens.Initialize.
    public abstract class PopupModule
    {
        protected PopupScreens Host { get; private set; }
        internal void Attach(PopupScreens host) { Host = host; }

        public abstract void Build();
        // True when this module owns the button action (e.g. its prefix "shop-").
        public virtual bool Handles(string action) { return false; }
        // Reacts to an action this module Handles; true when consumed.
        public virtual bool Handle(string action) { return true; }
        // Every frame while a popup is open (only the open one usually has work to do).
        public virtual void Tick() { }
        // Full pages (Shop, Gallery, the Sand Quest map) do not zoom in like dialogs.
        public virtual bool IsPage(GameObject root) { return false; }
        // True when one of this module's popups is the open one.
        protected bool IsOpen(Popup p) { return p != null && Open == p.Root; }

        // Shared state.
        protected VideoScreen screen { get { return Host.Screen; } }
        protected GameObject Open { get { return Host.Open; } }
        protected List<Object> runtimeArt { get { return Host.RuntimeArt; } }
        protected float ShownAt { get { return Host.ShownAt; } }

        // Shared building blocks (see PopupScreens / PopupScreens.Kit).
        protected Popup Build(string name) { return Host.Build(name); }
        protected Popup Blank(string name, bool keepHitAreas = false) { return Host.Blank(name, keepHitAreas); }
        protected SpriteRenderer Art(Popup p, string sprite, Vector3 local, float width) { return Host.Art(p, sprite, local, width); }
        protected TextMesh Label(Popup p, string name, Vector3 local, string text, float size, Color color) { return Host.Label(p, name, local, text, size, color); }
        protected TextMesh[] OutlinedLabel(Popup p, string name, Vector3 local, float size) { return Host.OutlinedLabel(p, name, local, size); }
        protected static T Child<T>(Popup p, string name, Vector3 local) where T : Component { return PopupScreens.Child<T>(p, name, local); }
        protected static void SetText(TextMesh[] labels, string text) { PopupScreens.SetText(labels, text); }
        protected static void ScaleText(TextMesh label, float factor) { PopupScreens.ScaleText(label, factor); }
        protected void Configure(VideoUiButton button, TextMesh label, string action, string text, float fitChars = 5.5f) { Host.Configure(button, label, action, text, fitChars); }
        protected void GreenButton(Popup p, float y, string action, string text, float scale = 1.05f) { Host.GreenButton(p, y, action, text, scale); }
        protected void CloseX(Popup p, Vector3 local, string action) { Host.CloseX(p, local, action); }
        protected GameObject Hit(Popup p, Vector3 local, Vector2 size, string action) { return Host.Hit(p, local, size, action); }
        protected void AddPageChrome(Popup page, string activeIcon) { Host.AddPageChrome(page, activeIcon); }
        protected void RefreshPageLives(Popup page) { Host.RefreshPageLives(page); }
        protected Sprite Greyscale(Sprite source) { return Host.Greyscale(source); }
        protected Sprite CoverCrop(Sprite source) { return Host.CoverCrop(source); }
        protected void RefreshPopupCoins() { Host.RefreshPopupCoins(); }
        protected void Show(Popup p) { Host.Show(p); }
        protected void Close() { Host.Close(); }
        protected static T Instantiate<T>(T original, Transform parent) where T : Object { return Object.Instantiate(original, parent); }
        protected static void Destroy(Object o) { Object.Destroy(o); }
        // Destroys the objects a popup built for its current content and empties the list.
        protected static void DestroyAll(List<GameObject> objects) { foreach (var o in objects) if (o) Object.Destroy(o); objects.Clear(); }
    }
}
