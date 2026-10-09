using UnityEngine;

namespace SandJamTest.Scene3D
{
    // Soft contact shadow under a cube, as in the current build where every cube sits on a dark blur on the lane
    // floor. Slightly smaller and fainter while the cube moves; hidden once it leaves.
    public sealed class CubeContactShadow : MonoBehaviour
    {
        public SceneActorView Actor;
        public Vector3 Offset = new Vector3(.04f, -.02f, .25f);
        public float Width = 1.2f, Height = .4f, Alpha = .7f;
        static Sprite blob;
        SpriteRenderer shadow;
        float fade;
        MeshRenderer[] blockRenderers;

        static Sprite Blob()
        {
            if (blob) return blob;
            const int size = 64;
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "Contact shadow" };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + .5f) / size * 2 - 1, dy = (y + .5f) / size * 2 - 1;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1 - d); a = a * a * (3 - 2 * a);
                    px[y * size + x] = new Color32(8, 8, 20, (byte)(a * 255));
                }
            t.SetPixels32(px); t.Apply(false, true);
            blob = Sprite.Create(t, new Rect(0, 0, size, size), new Vector2(.5f, .5f), size);
            return blob;
        }

        void Start()
        {
            var go = new GameObject("Contact shadow") { layer = gameObject.layer };
            go.transform.SetParent(transform.parent, false);
            shadow = go.AddComponent<SpriteRenderer>();
            shadow.sprite = Blob();
            shadow.sortingOrder = -1;
            shadow.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        void LateUpdate()
        {
            if (!shadow) return;
            bool visible = Actor && Actor.gameObject.activeInHierarchy && !Actor.Departing && Actor.Shooter != null;
            if (shadow.enabled != visible) shadow.enabled = visible;
            if (!visible) return;
            // Anchor to the block itself: the actor pivot sits mid-block, so use the visible block's bounds and
            // put the blur just under its bottom edge, slightly behind so the block covers its upper half.
            float moving = Actor.AtRest ? 0 : 1;
            fade = Mathf.MoveTowards(fade, moving, Time.deltaTime * 6);
            if (blockRenderers == null || blockRenderers.Length == 0)
                blockRenderers = Actor.Visual.GetComponentsInChildren<MeshRenderer>(true);
            Bounds b = default(Bounds); bool any = false;
            foreach (var r in blockRenderers)
            {
                if (!r || !r.enabled || !r.gameObject.activeInHierarchy || r.GetComponent<TextMesh>()) continue;
                if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
            }
            if (!any) { shadow.enabled = false; return; }
            shadow.transform.position = new Vector3(b.center.x, b.min.y, b.center.z) + Offset;
            shadow.transform.rotation = Quaternion.identity;
            float k = 1 - .15f * fade;
            shadow.transform.localScale = new Vector3(Width * k, Height * k, 1);
            shadow.color = new Color(1, 1, 1, Alpha * (1 - .35f * fade));
        }

        void OnDisable() { if (shadow) shadow.enabled = false; }
        void OnDestroy() { if (shadow) Destroy(shadow.gameObject); }
    }
}
