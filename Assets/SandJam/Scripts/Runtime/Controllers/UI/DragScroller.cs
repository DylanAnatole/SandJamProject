using UnityEngine;

namespace SandJamTest.Scene3D
{
    // Vertical drag / mouse-wheel scrolling for a full page (Shop, Gallery). Buttons on the page do not act on
    // press: they Hold() their action, which runs on release only if the pointer was not dragged.
    // A flick keeps gliding and slows like the original's ScrollRect (recording 2, Shop 4.0-10.7 s: the speed falls
    // ~e^-2t, Unity's default deceleration 0.135 a second) and stops dead at either end (no bounce).
    public sealed class DragScroller
    {
        public float Value, Max;
        bool pressing, dragged;
        float pressY, pressValue, velocity, lastValue;
        const float Deceleration = .135f, StopSpeed = .05f; // world units a second
        string held;

        public void Reset() { Value = 0; velocity = 0; pressing = false; held = null; }
        public void Hold(string action) { held = action; }

        // Call every frame while the page is open. Dragging starts only below dragTop (world y).
        // Returns true when Value changed.
        public bool Tick(Camera cam, System.Action<string> run, float dragTop = float.MaxValue)
        {
            if (!cam) return false;
            float before = Value, dt = Mathf.Min(Time.unscaledDeltaTime, .05f); // one hitch must not throw the page
            if (!pressing && velocity != 0 && dt > 0)
            {
                Value += velocity * dt;
                velocity *= Mathf.Pow(Deceleration, dt);
                if (Value <= 0 || Value >= Max || Mathf.Abs(velocity) < StopSpeed) { Value = Mathf.Clamp(Value, 0, Max); velocity = 0; }
            }
            var pointer = Input.mousePosition;
            if (float.IsInfinity(pointer.x) || float.IsNaN(pointer.x) || float.IsInfinity(pointer.y) || float.IsNaN(pointer.y)) return Value != before; // no pointer (unfocused editor)
            float y = cam.ScreenToWorldPoint(pointer).y;
            if (Input.GetMouseButtonDown(0)) { pressing = true; dragged = false; pressY = y; pressValue = Value; velocity = 0; lastValue = Value; }
            if (pressing && Input.GetMouseButton(0) && y < dragTop)
            {
                float delta = y - pressY;
                if (Mathf.Abs(delta) > .12f) dragged = true;
                if (dragged) Value = Mathf.Clamp(pressValue + delta, 0, Max);
                // Finger speed, smoothed as ScrollRect does, carried on after release.
                if (dt > 0) velocity = Mathf.Lerp(velocity, (Value - lastValue) / dt, Mathf.Clamp01(dt * 10));
                lastValue = Value;
            }
            if (pressing && Input.GetMouseButtonUp(0))
            {
                pressing = false;
                var action = held; held = null;
                if (!dragged) velocity = 0;
                if (!dragged && action != null) run(action);
            }
            float wheel = Input.mouseScrollDelta.y;
            if (Mathf.Abs(wheel) > .01f) Value = Mathf.Clamp(Value - wheel * .6f, 0, Max);
            return Value != before;
        }
    }
}
