using UnityEngine;

namespace SandJamTest.Scene3D
{
    // When a region is completed the whole picture (board + frame) gives a short elastic bump, so finishing an
    // area feels like a small reward beat on top of the bubbles and sparkles.
    public sealed class BoardPunch : MonoBehaviour
    {
        public float Strength = .035f, Duration = .32f;
        public Vector3 Pivot; // world point the picture swells around (its centre)
        Vector3 baseScale, basePosition; float time = -1;

        void Awake() { baseScale = transform.localScale; basePosition = transform.position; }
        void OnEnable() { GameEvents.RegionCompleted += OnRegion; }
        void OnDisable() { GameEvents.RegionCompleted -= OnRegion; transform.localScale = baseScale; transform.position = basePosition; time = -1; }
        void OnRegion(int region, int color) { time = 0; }

        void LateUpdate()
        {
            if (time < 0) return;
            time += Time.deltaTime;
            float t = time / Duration;
            if (t >= 1) { transform.localScale = baseScale; transform.position = basePosition; time = -1; return; }
            // Quick swell then a damped wobble back.
            float k = Mathf.Sin(t * Mathf.PI * 2.5f) * (1 - t) * Strength;
            transform.localScale = baseScale * (1 + k);
            var p = Pivot + (basePosition - Pivot) * (1 + k); p.z = basePosition.z;
            transform.position = p;
        }
    }
}
