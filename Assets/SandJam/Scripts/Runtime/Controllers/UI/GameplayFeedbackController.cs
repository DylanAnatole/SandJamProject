using UnityEngine;

namespace SandJamTest.Scene3D
{
    public sealed class GameplayFeedbackController : MonoBehaviour
    {
        VideoScreen screen;
        GameObject[] locks;
        TextMesh[] counters;
        float failedTime;
        SandGame previousGame;
        public bool FailureVisible { get; private set; }

        public void Initialize(VideoScreen owner)
        {
            screen=owner;locks=new GameObject[5];counters=new TextMesh[5];
            locks[4]=owner.FifthSlotLock;counters[4]=owner.FifthSlotRemaining;
            for(int i=0;i<4;i++)
            {
                locks[i]=Instantiate(locks[4],locks[4].transform.parent);
                locks[i].name="Waiting slot "+(i+1)+" unlock counter";
                locks[i].transform.position=locks[4].transform.position+owner.Controller.StashSlots[i].position-owner.Controller.StashSlots[4].position;
                counters[i]=locks[i].transform.Find("Unlock remaining").GetComponent<TextMesh>();
                locks[i].SetActive(false);
            }
            StyleLockedPads(owner);
        }
        // Original locked slot: the waiting tile itself turns grey, a big white padlock on it and the sand still
        // needed in small white digits underneath. The lock art used to be a flat 2D pad floating in front of the
        // tilted 3D tile, which showed offset (perspective) with the white tile peeking out behind it. Now the 3D
        // tile is tinted and the icon and digits are placed on the camera ray through its centre.
        Renderer[] pads; Color[] padColors; bool[] padLocked;
        static readonly Color LockedPad = new Color(.6f, .62f, .67f);
        void StyleLockedPads(VideoScreen owner)
        {
            var controller = owner.Controller; var stash = controller.StashSlots[0].parent; var cam = controller.GameCamera;
            pads = new Renderer[5]; padColors = new Color[5]; padLocked = new bool[5];
            for (int i = 0; i < 5; i++)
            {
                var t = stash.Find("Solid waiting pad " + i); var pad = t ? t.GetComponent<Renderer>() : null;
                if (!pad || !cam) continue;
                pads[i] = pad; padColors[i] = pad.material.HasProperty("_Color") ? pad.material.color : Color.white;
                var flat = locks[i].transform.Find("Locked pad"); if (flat) { var r = flat.GetComponent<Renderer>(); if (r) r.enabled = false; }
                var icon = locks[i].transform.Find("icon-lock"); var digits = counters[i] ? counters[i].transform : null;
                var b = pad.bounds; var eye = cam.transform.position;
                // Point on the camera ray through a pad point, at the depth of the lock art (same spot on screen).
                System.Func<Vector3, float, Vector3> onRay = (p, z) => eye + (p - eye) * ((z - eye.z) / (p.z - eye.z));
                if (icon)
                {
                    float z = icon.position.z; float k = (z - eye.z) / (b.center.z - eye.z);
                    icon.position = onRay(new Vector3(b.center.x, b.max.y - b.size.y * .3f, b.center.z), z);
                    var sr = icon.GetComponent<SpriteRenderer>();
                    // The padlock fills ~72% of its sprite's height; it stands about half as tall as the tile.
                    if (sr && sr.sprite) { float s = b.size.y * .58f * k / (sr.sprite.bounds.size.y * .72f); icon.localScale = new Vector3(s, s, 1); }
                }
                if (digits)
                {
                    float z = digits.position.z;
                    digits.position = onRay(new Vector3(b.center.x, b.max.y - b.size.y * .7f, b.center.z), z);
                    digits.localScale *= .72f;
                    var ink = new Color(.22f, .24f, .3f);
                    var outlined = digits.GetComponent<OutlinedText>(); if (outlined) outlined.Ink = ink;
                    foreach (var o in digits.GetComponentsInChildren<TextMesh>(true)) if (o.transform != digits) o.color = ink;
                }
            }
        }
        void TintPad(int i, bool locked)
        {
            if (pads == null || !pads[i] || padLocked[i] == locked) return;
            padLocked[i] = locked;
            if (pads[i].material.HasProperty("_Color")) pads[i].material.color = locked ? LockedPad : padColors[i];
        }
        public void Refresh(float delta)
        {
            if(!screen || screen.Controller.Game==null)return;
            var game=screen.Controller.Game;
            if(previousGame!=game){failedTime=0;previousGame=game;}
            for(int i=0;i<5;i++)
            {
                int remaining=game.SlotRemaining(i);
                locks[i].SetActive(remaining>0);counters[i].text=DisplayAmount.Text(remaining);TintPad(i,remaining>0);
            }
            bool lost=screen.Current==VideoScreen.Page.Gameplay && game.State==GameState.Lost;
            failedTime=lost?failedTime+delta:0;
            FailureVisible=lost && failedTime>=.7f;        }
        void LateUpdate(){Refresh(Time.unscaledDeltaTime);}
        // Campaign retries reload the scene so a bought extra slot never carries over into the new attempt.
        public void Retry(){FailureVisible=false;failedTime=0;screen.Levels.Retry();}
        public void GoHome(){FailureVisible=false;failedTime=0;screen.Show(VideoScreen.Page.Home);}
    }
}
