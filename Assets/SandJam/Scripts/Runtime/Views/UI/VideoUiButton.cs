using UnityEngine;
namespace SandJamTest.Scene3D
{
    public sealed class VideoUiButton : MonoBehaviour
    {
        public string Action;
        // Tactile press: the button graphic dips and springs back with a small overshoot.
        public void Press()
        {
            var target = transform.parent;
            if (!target) return;
            var press = target.GetComponent<ButtonPress>();
            if (!press) press = target.gameObject.AddComponent<ButtonPress>();
            press.Play();
        }
    }

    public sealed class ButtonPress : MonoBehaviour
    {
        const float Duration = .24f;
        Vector3 baseScale; bool hasBase; float time = -1;
        public void Play() { if (!hasBase) { baseScale = transform.localScale; hasBase = true; } time = 0; }
        void OnDisable() { if (hasBase) transform.localScale = baseScale; time = -1; }
        void LateUpdate()
        {
            if (time < 0) return;
            time += Time.unscaledDeltaTime;
            float t = time / Duration;
            if (t >= 1) { transform.localScale = baseScale; time = -1; return; }
            // Dip to 0.88 in the first quarter, then spring back with a small overshoot.
            float k = t < .25f ? 1 - .12f * (t / .25f) : 1 - .12f * Mathf.Cos((t - .25f) / .75f * Mathf.PI * 1.5f) * (1 - t) / .75f;
            transform.localScale = baseScale * k;
        }
    }
}
