using UnityEngine;

namespace SandJamTest.Scene3D
{
    // Vertical drag / mouse-wheel scrolling for a full page (Shop, Gallery). Buttons on the page do not act on
    // press: they Hold() their action, which runs on release only if the pointer was not dragged.
    public sealed class DragScroller
    {
        public float Value, Max;
        bool pressing, dragged;
        float pressY, pressValue;
        string held;

        public void Reset() { Value = 0; pressing = false; held = null; }
        public void Hold(string action) { held = action; }

        // Call every frame while the page is open. Dragging starts only below dragTop (world y).
        // Returns true when Value changed.
        public bool Tick(Camera cam, System.Action<string> run, float dragTop = float.MaxValue)
        {
            if (!cam) return false;
            var pointer = Input.mousePosition;
            if (float.IsInfinity(pointer.x) || float.IsNaN(pointer.x) || float.IsInfinity(pointer.y) || float.IsNaN(pointer.y)) return false; // no pointer (unfocused editor)
            float y = cam.ScreenToWorldPoint(pointer).y, before = Value;
            if (Input.GetMouseButtonDown(0)) { pressing = true; dragged = false; pressY = y; pressValue = Value; }
            if (pressing && Input.GetMouseButton(0) && y < dragTop)
            {
                float delta = y - pressY;
                if (Mathf.Abs(delta) > .12f) dragged = true;
                if (dragged) Value = Mathf.Clamp(pressValue + delta, 0, Max);
            }
            if (pressing && Input.GetMouseButtonUp(0))
            {
                pressing = false;
                var action = held; held = null;
                if (!dragged && action != null) run(action);
            }
            float wheel = Input.mouseScrollDelta.y;
            if (Mathf.Abs(wheel) > .01f) Value = Mathf.Clamp(Value - wheel * .6f, 0, Max);
            return Value != before;
        }
    }
}
