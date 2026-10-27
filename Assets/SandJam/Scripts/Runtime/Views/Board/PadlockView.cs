using UnityEngine;

namespace SandJamTest.Scene3D
{
    // Padlock animation (original UnlockerGif, 0.14 s frames): every key cube received pops the lock ~20% bigger; at zero
    // it flashes white while the shackle swings open (the mesh's blend shape), turns gold again and shrinks away with its
    // count. Runs only while animating.
    public sealed class PadlockView : MonoBehaviour
    {
        public MeshRenderer Body;
        public Mesh Closed;
        public Transform Counter;
        public Color Gold;
        const float PopScale = .22f, PopTime = .2f;
        const float SwingTime = .2f, FlashTime = .4f, OpenPop = .15f, ShrinkStart = .5f, ShrinkTime = .22f;
        static readonly Color FlashColor = new Color(1.3f, 1.3f, 1.25f);
        float pop = -1, open = -1;
        Vector3 bodyScale, rootScale, counterScale, waitingScale;
        Transform waiting; // the opened part's count, drawn over the lock: shown once the lock has gone
        MeshFilter filter;

        public void Init()
        {
            rootScale = transform.localScale;
            if (Body) { bodyScale = Body.transform.localScale; filter = Body.GetComponent<MeshFilter>(); }
            enabled = false;
        }
        public void Pop() { if (open < 0) { pop = 0; enabled = true; } }
        public void Open(Transform waitingCount)
        {
            if (open >= 0) return;
            open = 0; pop = -1; enabled = true;
            if (Counter) counterScale = Counter.localScale;
            waiting = waitingCount;
            if (waiting) { waitingScale = waiting.localScale; waiting.localScale = Vector3.zero; }
        }
        void ShowWaiting() { if (waiting) waiting.localScale = waitingScale; waiting = null; }
        public void ResetClosed()
        {
            bool wasOpen = open >= 0;
            open = pop = -1; enabled = false;
            gameObject.SetActive(true);
            transform.localScale = rootScale;
            if (wasOpen && Counter) Counter.localScale = counterScale;
            ShowWaiting();
            if (Body) { Body.transform.localScale = bodyScale; filter.sharedMesh = Closed; Tint(0); }
        }

        void Tint(float white)
        {
            Body.SetBlockColor(Color.Lerp(Gold, FlashColor, white));
            Body.SetBlockFloat("_Albedo", 1 - white);
        }

        void LateUpdate()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, .1f);
            if (open >= 0)
            {
                open += dt;
                if (Body)
                {
                    var mesh = BlendShapeCache.At(Closed, Mathf.Clamp01(open / SwingTime));
                    if (filter.sharedMesh != mesh) filter.sharedMesh = mesh;
                    Tint(Mathf.Clamp01(1 - open / FlashTime));
                    Body.transform.localScale = bodyScale * (1 + OpenPop * Mathf.Sin(Mathf.PI * Mathf.Clamp01(open / SwingTime)));
                }
                float t = Mathf.Clamp01((open - ShrinkStart) / ShrinkTime), s = 1 - t * t;
                transform.localScale = rootScale * s;
                if (Counter) Counter.localScale = counterScale * s;
                if (t >= 1) { gameObject.SetActive(false); if (Counter) Counter.gameObject.SetActive(false); ShowWaiting(); enabled = false; }
                return;
            }
            if (pop >= 0 && Body)
            {
                pop += dt;
                float t = Mathf.Clamp01(pop / PopTime);
                Body.transform.localScale = bodyScale * (1 + PopScale * Mathf.Sin(Mathf.PI * t));
                if (t >= 1) { pop = -1; enabled = false; }
            }
            else enabled = false;
        }
    }
}
