using System.Collections.Generic;
using UnityEngine;

namespace SandJamTest.Scene3D
{
    // Level Complete page like the original (Game scene: coinSpawn, Fx_Rotate_Light; recording of level 256):
    // a soft rotating light behind the card, an almost black backdrop over the board, and the reward coins.
    // Recording 1 (93.3-94.5 s, 217.9-220.2 s): the card waits without coins; pressing Next greys the buttons, then
    // the coins stream from the Next button up to the coin counter (93.78-94.35 s) and the card goes. The counter
    // starts rolling as the first coins arrive (CampaignUi waits for CoinsLanded).
    // Soft golden light turning slowly behind a card's title (original Fx_Rotate_Light).
    public sealed class SpinningLight : MonoBehaviour
    {
        Vector3 baseScale;
        public static SpinningLight Add(Transform parent, Vector3 position, float width)
        {
            var light = Resources.Load<Sprite>("Congrats/light-congrats"); if (!light) return null;
            var r = new GameObject("Card light") { layer = parent.gameObject.layer }.AddComponent<SpriteRenderer>();
            r.transform.SetParent(parent, false); r.transform.position = position;
            r.sprite = light; r.color = new Color(1, .9f, .4f, .75f);
            float k = width / light.bounds.size.x / Mathf.Max(.0001f, parent.lossyScale.x); r.transform.localScale = new Vector3(k, k, 1);
            var spin = r.gameObject.AddComponent<SpinningLight>(); spin.baseScale = r.transform.localScale;
            return spin;
        }
        void Update()
        {
            float time = Time.unscaledTime;
            transform.localRotation = Quaternion.Euler(0, 0, time * 14f);
            transform.localScale = baseScale * (1 + .04f * Mathf.Sin(time * 3));
        }
    }

    public sealed class ResultFx : MonoBehaviour
    {
        public static bool CoinsLanded { get; private set; } = true;
        const int CoinCount = 10;
        const float CoinStart = .3f, Spread = .05f, Flight = .22f;
        Transform from, to;
        readonly List<Transform> coins = new List<Transform>();
        readonly List<SpriteRenderer> buttons = new List<SpriteRenderer>();
        readonly List<Color> buttonColors = new List<Color>();
        float time; System.Action collected;

        public void Build()
        {
            from = transform.Find("Next");
            to = transform.Find("icon-coin");
            var backdrop = transform.Find("Dark backdrop");
            // Measured on recording 2 (143 s, 229 s): an even, almost black navy (#06030F) hides the board.
            if (backdrop) { var r = backdrop.GetComponent<SpriteRenderer>(); if (r) r.color = new Color32(6, 3, 16, 255); }
            BuildCardFrame();
            FitRewardBox();
            StyleRewardBand();
            LayoutButtons();
            var header = transform.Find("Gold header");
            if (header) SpinningLight.Add(transform, header.position + new Vector3(0, .1f, .5f), 5.2f);
            var coin = Resources.Load<Sprite>("VideoUI/icon-coin");
            if (coin && from && to)
                for (int i = 0; i < CoinCount; i++)
                {
                    var r = new GameObject("Flying coin") { layer = gameObject.layer }.AddComponent<SpriteRenderer>();
                    r.transform.SetParent(transform, false); r.sprite = coin; r.sortingOrder = 50;
                    float k = .34f / coin.bounds.size.y; r.transform.localScale = new Vector3(k, k, 1);
                    r.gameObject.SetActive(false); coins.Add(r.transform);
                }
            foreach (var name in new[] { "x2", "Next" })
            {
                var t = transform.Find(name); if (!t) continue;
                foreach (var r in t.GetComponentsInChildren<SpriteRenderer>(true)) { buttons.Add(r); buttonColors.Add(r.color); }
            }
        }

        // Original card: a gold frame following the card's rounded, slightly bulging outline (the white card mask,
        // tinted), with a darker gold edge; the glass panel is clipped just inside its own outline so its green rim
        // (unused in the current build) disappears. Replaces the flat rectangular gold panels.
        public const string SlotName = "bg-popUp-slot-levelCompleted-v2", BandName = "Reward caption", FrameName = "Card frame", EdgeName = "Card frame edge", ClipName = "Glass clip", BandTopName = "Reward caption top edge";
        void BuildCardFrame()
        {
            var mask = Resources.Load<Sprite>("VideoUI/bg-popUp-mask-levelCompleted-v2");
            var border = transform.Find("Gold outer border"); var header = transform.Find("Gold header"); var glass = transform.Find("bg-popUp-levelCompleted-v2");
            if (!mask || !border || !glass) return;
            var b = border.GetComponent<Renderer>().bounds;
            SpriteRenderer Piece(string name, Color color, float grow, float z)
            {
                var r = new GameObject(name) { layer = gameObject.layer }.AddComponent<SpriteRenderer>();
                r.transform.SetParent(transform, false); r.sprite = mask; r.color = color;
                r.transform.position = new Vector3(b.center.x, b.center.y, z);
                var ps = transform.lossyScale; var size = mask.bounds.size;
                r.transform.localScale = new Vector3((b.size.x + grow) / size.x / ps.x, (b.size.y + grow) / size.y / ps.y, 1);
                return r;
            }
            float z = border.position.z;
            Piece(EdgeName, new Color(.86f, .5f, .05f), .1f, z + .01f);
            Piece(FrameName, new Color(.98f, .77f, .16f), 0, z);
            border.GetComponent<Renderer>().enabled = false;
            if (header) header.GetComponent<Renderer>().enabled = false;
            var g = glass.GetComponent<SpriteRenderer>();
            // Visible glass, measured on recording 2 (Level Complete, 143 s): 12 px inside the gold frame at the sides,
            // 29 px above its bottom, its top 187 px below the frame's top. The glass art is drawn larger so that its
            // green rim falls outside the clip.
            const float ClipX = .9f, ClipY = .92f; // the art's green rim stays well outside the clip, corners included
            float px = VideoScreen.Px, glassTop = b.max.y - 187 * px, glassBottom = b.min.y + 29 * px, glassWidth = b.size.x - 2 * 12 * px;
            var gsz = g.sprite.bounds.size; var tps = transform.lossyScale;
            g.transform.position = new Vector3(b.center.x, (glassTop + glassBottom) * .5f, g.transform.position.z);
            g.transform.localScale = new Vector3(glassWidth / ClipX / gsz.x / tps.x, (glassTop - glassBottom) / ClipY / gsz.y / tps.y, 1);
            var clip = new GameObject(ClipName) { layer = gameObject.layer }.AddComponent<SpriteMask>();
            clip.transform.SetParent(transform, false); clip.sprite = mask;
            clip.transform.position = g.bounds.center;
            var cs = mask.bounds.size;
            clip.transform.localScale = new Vector3(glassWidth / cs.x / tps.x, (glassTop - glassBottom) / cs.y / tps.y, 1);
            // The project's pixel sprite shader ignores sprite masks; the default sprite shader honours them.
            g.sharedMaterial = SpriteFactory.PlainSprite;
            g.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        }

        // Original reward box: "Your Reward!" sits on a dark band that is the purple box's own bottom strip (full
        // width, square top edge, rounded bottom corners), in pale lavender, not a separate narrower pill.
        void StyleRewardBand()
        {
            var slot = transform.Find(SlotName); var band = transform.Find(BandName);
            if (!slot || !band) return;
            var sb = slot.GetComponent<Renderer>().bounds; var br = band.GetComponent<SpriteRenderer>();
            if (!br || !br.sprite) return;
            var colour = new Color(.227f, .235f, .486f);
            float inset = sb.size.x * .035f, width = sb.size.x - 2 * inset, height = sb.size.y * .22f, bottom = sb.min.y + inset * .9f;
            br.color = colour;
            band.position = new Vector3(sb.center.x, bottom + height * .5f, band.position.z);
            band.localScale = Vector3.one;
            var lossy = band.lossyScale;
            if (br.drawMode != SpriteDrawMode.Simple) br.size = new Vector2(width / lossy.x, height / lossy.y); // sliced: corners keep their radius
            else { var measured = br.bounds.size; band.localScale = new Vector3(width / measured.x, height / measured.y, 1); }
            // Square off the top edge: a plain strip over the upper half of the band.
            var top = new GameObject(BandTopName) { layer = gameObject.layer }.AddComponent<SpriteRenderer>();
            top.transform.SetParent(transform, false);
            top.sprite = SpriteFactory.White;
            top.color = colour; top.sortingOrder = br.sortingOrder; top.sharedMaterial = br.sharedMaterial;
            top.transform.position = new Vector3(sb.center.x, bottom + height * .75f, band.position.z - .001f);
            top.FitWorldSize(width, height * .5f); // from the measured bounds (the parent chain is not uniformly scaled)
            ClipBandToBox(slot, br, top);
            // The reward amount sits clear of the band, as in the original.
            var amount = transform.Find("+ 10");
            if (amount) { float gap = amount.GetComponent<Renderer>().bounds.min.y - (bottom + height); if (gap < .08f) amount.position += new Vector3(0, .08f - gap, 0); }
            var caption = transform.Find("Your Reward!");
            if (caption)
            {
                caption.position = new Vector3(caption.position.x, bottom + height * .5f + .02f, caption.position.z);
                var text = caption.GetComponent<TextMesh>(); if (text) text.color = new Color(.78f, .76f, .84f);
            }
        }

        // The band is clipped to the reward box's inner shape (the box art minus its pale rim, so the rounded bottom
        // corners match): it used to run over the rim at the sides and corners. Its own sorting order keeps this
        // mask off the glass, and the texts on it draw above it.
        const int BandOrder = 5;
        void ClipBandToBox(Transform slot, SpriteRenderer band, SpriteRenderer top)
        {
            var slotArt = slot.GetComponent<SpriteRenderer>(); if (!slotArt || !slotArt.sprite) return;
            var mask = new GameObject("Reward band mask") { layer = gameObject.layer }.AddComponent<SpriteMask>();
            mask.transform.SetParent(transform, false);
            mask.sprite = slotArt.sprite;
            mask.transform.position = slot.position; mask.transform.rotation = slot.rotation;
            // Rim: ~11 px of 316 at the sides, ~9 px of 392 at the bottom.
            var ls = slot.localScale; mask.transform.localScale = new Vector3(ls.x * (1 - 2 * 11f / 316f), ls.y * (1 - 2 * 9f / 392f), ls.z);
            mask.isCustomRangeActive = true; mask.frontSortingOrder = BandOrder + 1; mask.backSortingOrder = BandOrder - 1; // front bound is exclusive
            var plain = SpriteFactory.PlainSprite;
            foreach (var r in new[] { band, top })
            {
                r.sortingOrder = BandOrder; r.sharedMaterial = plain; r.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            }
            // The glass clip only ever meant the glass panel (sorting order 0).
            var clip = transform.Find(ClipName) ? transform.Find(ClipName).GetComponent<SpriteMask>() : null;
            var glass = transform.Find("bg-popUp-levelCompleted-v2") ? transform.Find("bg-popUp-levelCompleted-v2").GetComponent<SpriteRenderer>() : null;
            if (clip && glass) { clip.isCustomRangeActive = true; clip.frontSortingOrder = glass.sortingOrder + 1; clip.backSortingOrder = glass.sortingOrder - 1; }
            foreach (var name in new[] { "Your Reward!", "+ 10" })
            {
                var t = transform.Find(name); if (!t) continue;
                foreach (var r in t.GetComponentsInChildren<Renderer>(true)) r.sortingOrder = BandOrder + 1;
            }
        }

        // The reward box (slot) and the visible glass (clip mask) bounds.
        bool SlotAndGlass(out Transform slot, out Bounds slotBounds, out Bounds glassBounds)
        {
            slot = transform.Find(SlotName); var clip = transform.Find(ClipName);
            slotBounds = glassBounds = default(Bounds);
            if (!slot || !clip) return false;
            slotBounds = slot.GetComponent<Renderer>().bounds; glassBounds = clip.GetComponent<SpriteMask>().bounds;
            return true;
        }

        // The purple reward box (with its pale rim), measured on recording 2: 54 px inside the visible glass at the
        // sides, 13 px below its top, 471 px tall. The coins and the amount move with its centre.
        void FitRewardBox()
        {
            Transform slot; Bounds sb, cb; if (!SlotAndGlass(out slot, out sb, out cb)) return;
            float px = VideoScreen.Px, width = cb.size.x - 2 * 54 * px, height = 471 * px, top = cb.max.y - 13 * px;
            var ls = slot.localScale; slot.localScale = new Vector3(ls.x * width / sb.size.x, ls.y * height / sb.size.y, ls.z);
            var shift = new Vector3(cb.center.x, top - height * .5f, 0) - slot.GetComponent<Renderer>().bounds.center; shift.z = 0;
            slot.position += shift;
            foreach (var name in new[] { "icon-coin-reward", "+ 10" }) { var t = transform.Find(name); if (t) t.position += shift; }
        }

        // The reward box and the two buttons share the same width inside the glass with equal side margins (the
        // buttons used to sit off-centre: 0.35 margin on the left, 0.12 on the right), the buttons split by a small
        // gap and centred in the space between the reward box and the bottom of the glass.
        void LayoutButtons()
        {
            Transform slot; Bounds sb, cb; if (!SlotAndGlass(out slot, out sb, out cb)) return;
            float left = sb.min.x, right = sb.max.x, gap = .14f, width = (right - left - gap) * .5f;
            float centreY = (sb.min.y + cb.min.y) * .5f;
            Fit(transform.Find("x2"), "button-yellow-v2", left, left + width, centreY);
            Fit(transform.Find("Next"), "button-green-v2", right - width, right, centreY);
        }
        static void Fit(Transform button, string bodyName, float min, float max, float centreY)
        {
            if (!button) return;
            var body = button.Find(bodyName); var r = body ? body.GetComponent<Renderer>() : null; if (!r) return;
            var bb = r.bounds; float k = (max - min) / Mathf.Max(.0001f, bb.size.x);
            // Only the button art and its touch area stretch; the caption and icons keep their size.
            var ls = body.localScale; body.localScale = new Vector3(ls.x * k, ls.y, ls.z);
            foreach (var hit in button.GetComponentsInChildren<BoxCollider>(true)) { var size = hit.size; hit.size = new Vector3(size.x * k, size.y, size.z); }
            var nb = r.bounds;
            button.position += new Vector3((min + max) * .5f - nb.center.x, centreY - nb.center.y, 0);
        }

        // Everything inside the card stays centred on the gold frame (glass, reward box, caption, buttons): the
        // design offsets are recorded once and restored every frame, whatever else touched them.
        static readonly string[] Inside = { "bg-popUp-levelCompleted-v2", ClipName, SlotName, BandName, BandTopName,
            "icon-coin-reward", "+ 10", "Your Reward!", "x2", "Next", "Level Complete!", "bg-level-yellow", "Level title", EdgeName };
        readonly List<Transform> insideItems = new List<Transform>(); readonly List<float> insideOffsets = new List<float>();
        Transform frameRoot; bool reportedDrift;
        void RecordLayout()
        {
            frameRoot = transform.Find(FrameName); if (!frameRoot) return;
            foreach (var name in Inside)
            {
                var t = transform.Find(name); if (!t) continue;
                insideItems.Add(t); insideOffsets.Add(t.localPosition.x - frameRoot.localPosition.x);
            }
        }
        void KeepCentred()
        {
            if (!frameRoot) return;
            float fx = frameRoot.localPosition.x;
            for (int i = 0; i < insideItems.Count; i++)
            {
                var t = insideItems[i]; if (!t) continue;
                var p = t.localPosition; float want = fx + insideOffsets[i];
                if (Mathf.Abs(p.x - want) > .0005f)
                {
                    if (!reportedDrift && Mathf.Abs(p.x - want) > .01f) { reportedDrift = true; Debug.Log("[ResultFx] re-centred " + t.name + " (was off by " + (p.x - want).ToString("F3") + ")"); }
                    t.localPosition = new Vector3(want, p.y, p.z);
                }
            }
        }
        // Level Complete only (the popups copied from this card in Awake keep their own text sizes): measured on
        // recording 2 (143 s), larger title, amount, coins and caption; title, amount and coins higher.
        void Start()
        {
            VideoScreen.Place(transform.Find("Level Complete!"), 1.23f, 0, -18);
            VideoScreen.Place(transform.Find("+ 10"), 1.21f, 0, -32.5f);
            VideoScreen.Place(transform.Find("icon-coin-reward"), 1.08f, 16.5f, -20.5f);
            VideoScreen.Place(transform.Find("Your Reward!"), 1.25f, 0, 0);
            RecordLayout();
        }
        void LateUpdate() { KeepCentred(); Pop(); }

        // Recording 3 (244.76-244.90 s, card width every frame): the card grows from ~1/5 of its size to full in ~0.14 s,
        // fast then easing, no overshoot. The page is scaled as a whole; the dark backdrop, the coin bar and the confetti
        // are scaled back so they keep covering the screen / their place.
        const float PopFrom = .2f, PopTime = .14f;
        float popAge = -1; Vector3 rootScale; Transform[] steady; Vector3[] steadyPosition, steadyScale;
        void Pop()
        {
            if (popAge < 0) return;
            if (steady == null)
            {
                rootScale = transform.localScale;
                var list = new List<Transform>();
                foreach (Transform child in transform)
                    if (child.name == "Dark backdrop" || child.name == "Confetti" || System.Array.IndexOf(PopupScreens.CoinBar, child.name) >= 0) list.Add(child);
                steady = list.ToArray(); steadyPosition = new Vector3[steady.Length]; steadyScale = new Vector3[steady.Length];
                for (int i = 0; i < steady.Length; i++) { steadyPosition[i] = steady[i].localPosition; steadyScale[i] = steady[i].localScale; }
            }
            popAge += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(popAge / PopTime), s = PopFrom + (1 - PopFrom) * (1 - (1 - t) * (1 - t));
            transform.localScale = new Vector3(rootScale.x * s, rootScale.y * s, rootScale.z);
            for (int i = 0; i < steady.Length; i++)
            {
                if (!steady[i]) continue;
                steady[i].localPosition = new Vector3(steadyPosition[i].x / s, steadyPosition[i].y / s, steadyPosition[i].z);
                steady[i].localScale = new Vector3(steadyScale[i].x / s, steadyScale[i].y / s, steadyScale[i].z);
            }
            if (t >= 1) popAge = -1;
        }

        void OnEnable() { time = 0; collected = null; CoinsLanded = coins.Count == 0; Flatten(); popAge = 0; }

        // The card's layers spread from z -6 to -8: through any perspective view (or a camera that is not exactly
        // in front) the glass, reward box, texts and buttons slid sideways against the gold frame. They are pressed
        // into a thin slab (one tenth of the depth, same front-to-back order). Done on first show, after the other
        // popups were cloned from this card, so their own added layers keep the depths they were built for.
        bool flattened;
        void Flatten()
        {
            if (flattened) return; flattened = true;
            const float front = -6f, k = .1f;
            var items = new List<Transform>(); var targets = new List<float>();
            foreach (var r in GetComponentsInChildren<Renderer>(true)) Collect(r.transform, items, targets, front, k);
            foreach (var m in GetComponentsInChildren<SpriteMask>(true)) Collect(m.transform, items, targets, front, k);
            for (int i = 0; i < items.Count; i++) { var p = items[i].position; items[i].position = new Vector3(p.x, p.y, targets[i]); }
        }
        static void Collect(Transform t, List<Transform> items, List<float> targets, float front, float k)
        {
            float z = t.position.z;
            if (z > front - .001f || items.Contains(t)) return; // backdrop, confetti and light behind the card stay put
            items.Add(t); targets.Add(front + (z - front) * k);
        }
        void OnDisable() { collected = null; CoinsLanded = true; Dim(0); foreach (var c in coins) if (c) c.gameObject.SetActive(false); }

        void Dim(float amount)
        {
            for (int i = 0; i < buttons.Count; i++)
                if (buttons[i]) buttons[i].color = Color.Lerp(buttonColors[i], new Color(.55f, .55f, .6f, buttonColors[i].a), amount);
        }

        // Next pressed: the coins fly, then `done` runs. False when there is nothing to show (no coin art).
        public bool Collect(System.Action done)
        {
            if (coins.Count == 0 || !from || !to || !isActiveAndEnabled) return false;
            if (collected == null) { collected = done; time = 0; }
            return true;
        }
        void Update()
        {
            if (collected == null) return;
            time += Time.unscaledDeltaTime;
            float end = CoinStart + Spread * (coins.Count - 1) + Flight;
            if (time >= end) { var done = collected; collected = null; done(); return; }
            Dim(time < end ? .65f : Mathf.Clamp01(1 - (time - end) / .2f) * .65f);
            for (int i = 0; i < coins.Count; i++)
            {
                float t = (time - CoinStart - i * Spread) / Flight;
                var c = coins[i];
                if (t <= 0 || t >= 1) { c.gameObject.SetActive(false); if (t >= 1 && i == 0) CoinsLanded = true; continue; }
                c.gameObject.SetActive(true);
                // Burst out of the reward box, then curve up to the counter (ease-in toward the end).
                var a = from.position; var b = to.position;
                float side = ((i * 37) % 11 - 5) * .12f;
                var mid = a + new Vector3(side * .5f, .3f + (i % 3) * .08f, 0);
                float e = t * t;
                var p = Vector3.Lerp(Vector3.Lerp(a, mid, e), Vector3.Lerp(mid, b, e), e);
                p.z = Mathf.Min(a.z, b.z) - .3f;
                c.position = p;
            }
        }
    }
}
