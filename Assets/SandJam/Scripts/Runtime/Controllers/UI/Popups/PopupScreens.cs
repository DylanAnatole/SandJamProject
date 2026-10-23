using System.Linq;
using UnityEngine;

namespace SandJamTest.Scene3D
{
    // "Out of space" and "Settings" popups built from the original Level Complete popup art
    // (gold frame, header ribbon, yellow/green buttons) so every dialog shares one style.
    public sealed partial class PopupScreens : MonoBehaviour
    {
        VideoScreen screen;
        Popup lose, failed;
        public const int PlayOnPrice = 400; // original "Play on 400" for one extra waiting slot
        SpriteRenderer[] hearts;
        TextMesh lostHeartLabel;
        bool gaveUp;
        internal bool failedIsNoLives;
        readonly System.Collections.Generic.List<Object> runtimeArt = new System.Collections.Generic.List<Object>();
        float shownAt;
        public GameObject Open { get; private set; }
        public bool SettingsOpen { get { return settingsPopup != null && settingsPopup.Visible; } }
        internal VideoScreen Screen { get { return screen; } }
        internal System.Collections.Generic.List<Object> RuntimeArt { get { return runtimeArt; } }
        internal float ShownAt { get { return shownAt; } }

        // Popups / pages that live in their own classes (see PopupModule).
        readonly System.Collections.Generic.List<PopupModule> modules = new System.Collections.Generic.List<PopupModule>();
        SettingsPopup settingsPopup; LevelIntroPopups intros; CongratsPopup congrats; SandQuestPopup sandQuest; ShopPopup shop; LivesPopup lives; TasksPopup tasks; GalleryPopup gallery;
        public void OpenShop() { shop.OpenShop(); }
        public void OpenGallery() { gallery.OpenGallery(); }
        public void OpenSandQuest() { sandQuest.OpenSandQuest(); }
        public void ShowSandQuestOnHome() { sandQuest.ShowSandQuestOnHome(); }
        public void OpenDailyRewards() { tasks.OpenDailyRewards(); }
        public void OpenTasks(TaskBoard.Kind kind) { tasks.OpenTasks(kind); }
        bool IsPage(GameObject root) { foreach (var m in modules) if (m.IsPage(root)) return true; return false; }
        T Add<T>(T module) where T : PopupModule { module.Attach(this); module.Build(); modules.Add(module); return module; }
        // After a win: the collection card (false when the level belongs to none).
        public bool ShowCongrats() { return congrats.ShowCongrats(); }
        public void ShowIntrosFor(int level) { intros.ShowIntrosFor(level); }
        public void ShowSettings() { settingsPopup.ShowSettings(); }


        public void Initialize(VideoScreen owner)
        {
            screen = owner;
            // Original "Out of Space": gold frame, red level ribbon, stash picture, "Free Extra 1 Slot",
            // green "Play on 400" and a red close button that gives the level up.
            lose = Build("Out of space popup");
            TintRibbon(lose, "VideoUI/bg-level-red", new Color(.93f, .25f, .36f));
            lose.Title.text = "Out of Space";
            lose.Icon.gameObject.SetActive(false); lose.Big.text = "";
            BuildStashPicture(lose);
            lose.Caption.text = "Free Extra 1 Slot";
            SingleButton(lose);
            Configure(lose.RightButton, lose.Right, "play-on", "Play on", 7f);
            PriceTag(lose, PlayOnPrice);
            StylePlayOn(lose);
            AddCloseButton(lose, "give-up");
            BuildOutOfSpaceBanner();
            // Measured on recording 2 (74 s): a slightly smaller card set higher, a much larger title, a bigger Play on
            // button sitting lower, the close button further in; the coin bar stays dimmed under the backdrop.
            FitCard(lose, .937f, 41);
            ScaleText(lose.Title, 1.41f);
            var close = lose.Root.transform.Find("Close button"); if (close) close.position += new Vector3(-15.5f, 16.5f, 0) * VideoScreen.Px;
            var playOn = lose.RightButton.transform.parent; playOn.localScale *= 1.12f; playOn.position += Vector3.up * 9f * VideoScreen.Px;
            // The level ribbon keeps its full width, a little higher.
            var ribbon = lose.Root.transform.Find("bg-level-yellow");
            if (ribbon) { ribbon.localScale = Vector3.Scale(ribbon.localScale, new Vector3(1.094f, 1.13f, 1)); ribbon.position += Vector3.up * 9f * VideoScreen.Px; }
            lose.Header.transform.position += Vector3.up * 9f * VideoScreen.Px;
            if (ribbon) SpinningLight.Add(lose.Root.transform, ribbon.position + new Vector3(0, .1f, .5f), 3.2f);
            HideCoinBar(lose);
            lose.Caption.color = Color.white; // "Free Extra 1 Slot" in white, as in the original
            // The picture box has no pale rim in the original: the slot art darkened to a plain dark navy frame.
            var loseSlot = lose.Root.transform.Find(ResultFx.SlotName);
            if (loseSlot) loseSlot.GetComponent<SpriteRenderer>().color = PictureFrameTint;
            // Original "Level Failed!": red frame, hearts with the lost one marked -1, refill timer, Try Again.
            failed = Build("Level failed popup");
            failed.Border.color = new Color(.86f, .16f, .2f); failed.Root.transform.Find("Gold header").GetComponent<SpriteRenderer>().color = new Color(.9f, .2f, .24f);
            failed.Icon.gameObject.SetActive(false);
            BuildHearts(failed);
            foreach (Transform child in failed.RightButton.transform.parent) failedRightX[child] = child.localPosition.x;
            failed.Big.transform.localPosition += new Vector3(0, .12f, 0); ScaleText(failed.Big, .62f); // clear of the caption band
            // Original timer row: stopwatch icon left of the countdown.
            var watch = Resources.Load<Sprite>("VideoUI/icon-watch");
            if (watch)
            {
                var w = Child<SpriteRenderer>(failed, "Timer watch", failed.Root.transform.InverseTransformPoint(failed.Big.transform.position) + new Vector3(-.95f, .02f, -.2f));
                w.sprite = watch; float k = .42f / watch.bounds.size.y; w.transform.localScale = new Vector3(k, k, 1);
                failed.Big.transform.position += failed.Root.transform.right * .25f * failed.Root.transform.lossyScale.x;
            }
            StyleFailedSlot();
            SingleButton(failed);
            AddCloseButton(failed, "home");
            // Measured on recording 1 (1187 s): a slightly smaller card set higher, a larger title, the coin bar dimmed.
            FitCard(failed, .965f, 37);
            VideoScreen.Place(failed.Title.transform, 1.12f, 0, 10);
            failed.RightButton.transform.parent.position += Vector3.up * 17 * VideoScreen.Px;
            FitFailedBox();
            failed.Caption.color = Color.white; // "Keep Going!" in white (the reward band's lavender is for Level Complete)
            HideCoinBar(failed);
            BuildNoLivesButtons();
            sandQuest = Add(new SandQuestPopup());
            shop = Add(new ShopPopup());
            lives = Add(new LivesPopup());
            tasks = Add(new TasksPopup());
            gallery = Add(new GalleryPopup());
            settingsPopup = Add(new SettingsPopup());
            intros = Add(new LevelIntroPopups());
            congrats = Add(new CongratsPopup());
        }

        internal Popup Build(string name)
        {
            var root = Instantiate(screen.Result, screen.Result.transform.parent);
            root.name = name; root.SetActive(false);
            // In front of every page (Home art reaches z -8), not only the gameplay screen.
            root.transform.localPosition += new Vector3(0, 0, -5f);
            var confetti = root.transform.Find("Confetti"); if (confetti) Destroy(confetti.gameObject);
            // Level Complete extras do not belong to the other popups built from it.
            var fx = root.GetComponent<ResultFx>(); if (fx) Destroy(fx);
            // The glass clip stays: it trims the glass panel's unused green rim (only one popup is open at a time, so
            // each popup's mask only ever meets its own glass).
            foreach (Transform child in root.transform)
                if (child.name == "Card light" || child.name == "Flying coin" || child.name == ResultFx.BandTopName)
                    Destroy(child.gameObject);
            // Every card keeps the Level Complete's rounded, slightly bulging frame (original: the red Level Failed
            // card has the same shape); popups recolour it through Border. The flat rectangular panels stay hidden.
            bool shapedFrame = root.transform.Find(ResultFx.FrameName);
            if (!shapedFrame) foreach (var n in new[] { "Gold outer border", "Gold header" }) { var t = root.transform.Find(n); if (t) t.GetComponent<Renderer>().enabled = true; }
            var placeholder = root.GetComponentInChildren<ReferenceUiPlaceholder>(true); if (placeholder) Destroy(placeholder);
            // Let the paused board show through, unlike the full-screen result page: plain black at ~68% (recording 2,
            // Out of Space: the board's colours read at ~0.32 of their brightness).
            root.transform.Find("Dark backdrop").GetComponent<SpriteRenderer>().color = new Color(0, 0, 0, .68f);
            var p = new Popup { Root = root };
            p.Header = Text(root, "Level title");
            p.Title = Text(root, "Level Complete!");
            p.Big = Text(root, "+ 10");
            p.Caption = Text(root, "Your Reward!");
            p.Icon = root.transform.Find("icon-coin-reward").GetComponent<SpriteRenderer>();
            p.Border = (shapedFrame ? root.transform.Find(ResultFx.FrameName) : root.transform.Find("Gold outer border")).GetComponent<SpriteRenderer>();
            p.Ribbon = root.transform.Find("Gold header").GetComponent<SpriteRenderer>();
            var x2 = root.transform.Find("x2"); var next = root.transform.Find("Next");
            p.Left = x2.Find("x2").GetComponent<TextMesh>(); p.Right = next.Find("Next").GetComponent<TextMesh>();
            p.LeftButton = x2.GetComponentInChildren<VideoUiButton>(true);
            p.RightButton = next.GetComponentInChildren<VideoUiButton>(true);
            return p;
        }

        static TextMesh Text(GameObject root, string name) { return root.transform.Find(name).GetComponent<TextMesh>(); }

        // The level ribbon in another colour: the original's own ribbon art when it is present locally (a tint of the
        // yellow art cannot reach pink), the tint otherwise.
        internal static void TintRibbon(Popup p, string art, Color fallback)
        {
            var ribbon = p.Root.transform.Find("bg-level-yellow");
            if (ribbon)
            {
                var r = ribbon.GetComponent<SpriteRenderer>(); var sprite = Resources.Load<Sprite>(art);
                if (sprite) { r.sprite = sprite; r.color = Color.white; } else r.color = fallback;
            }
            p.Header.color = Color.white;
        }
        // Hide the yellow button and centre the green one.
        internal static void SingleButton(Popup p)
        {
            p.LeftButton.transform.parent.gameObject.SetActive(false);
            var group = p.RightButton.transform.parent; var gp = group.localPosition; group.localPosition = new Vector3(0, gp.y, gp.z); // the card layout shifts the pair
            foreach (Transform child in p.RightButton.transform.parent)
                child.localPosition = new Vector3(0, child.localPosition.y, child.localPosition.z);
        }
        internal static T Child<T>(Popup p, string name, Vector3 local) where T : Component
        {
            var obj = new GameObject(name) { layer = p.Root.layer };
            obj.transform.SetParent(p.Root.transform, false); obj.transform.localPosition = local;
            return obj.AddComponent<T>();
        }
        internal TextMesh Label(Popup p, string name, Vector3 local, string text, float size, Color color)
        {
            var label = Child<TextMesh>(p, name, local);
            label.text = text; label.characterSize = size; label.fontSize = 64; label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center; label.color = color; label.fontStyle = FontStyle.Bold;
            var font = screen.Controller.InterfaceFont;
            if (font) { label.font = font; label.GetComponent<MeshRenderer>().sharedMaterial = font.material; }
            return label;
        }
        // Red square "X" in the top-right corner of the frame.
        // The hearts / timer / "Keep Going!" box sits well inside the glass (recording 1, 1187 s: 22 px below the glass
        // top, 413 px tall); built from the Level Complete reward box it was taller and touched the glass top.
        // Its top comes down by FailedBoxTopPx and its bottom (with the caption band) up by FailedBoxBottomPx.
        const float FailedBoxTopPx = 8, FailedBoxBottomPx = 22;
        void FitFailedBox()
        {
            var slot = failed.Root.transform.Find(ResultFx.SlotName); if (!slot) return;
            var r = slot.GetComponent<Renderer>(); var b = r.bounds;
            float top = b.max.y - FailedBoxTopPx * VideoScreen.Px, bottom = b.min.y + FailedBoxBottomPx * VideoScreen.Px;
            var ls = slot.localScale; slot.localScale = new Vector3(ls.x, ls.y * (top - bottom) / b.size.y, ls.z);
            slot.position += Vector3.up * ((top + bottom) * .5f - r.bounds.center.y);
            foreach (var name in new[] { ResultFx.BandName, ResultFx.BandTopName, "Reward band mask", "Your Reward!" })
            { var t = failed.Root.transform.Find(name); if (t) t.position += Vector3.up * FailedBoxBottomPx * VideoScreen.Px; }
        }
        void AddCloseButton(Popup p, string action)
        {
            var border = p.Border;
            var corner = new Vector3(border.bounds.max.x - .12f, border.bounds.max.y - .12f, 0);
            var local = p.Root.transform.InverseTransformPoint(corner); local.z = -8.5f;
            CloseX(p, local, action);
        }
        // Mini stash: five white pads plus a green sixth one, like the reference illustration.
        void BuildStashPicture(Popup p)
        {
            var pad = screen.Controller.StashSlots[0].parent.GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault(r => r.name.StartsWith("Waiting pad"));
            for (int i = 0; i < 6; i++)
            {
                var r = Child<SpriteRenderer>(p, "Stash picture " + i, new Vector3(-1.25f + i * .5f, .8f, -7.2f));
                r.sprite = pad ? pad.sprite : p.Ribbon.sprite;
                r.color = i < 5 ? Color.white : new Color(.45f, .9f, .35f);
                var size = r.sprite.bounds.size;
                r.transform.localScale = new Vector3(.42f / size.x, .42f / size.y, 1);
            }
            Label(p, "Stash picture plus", new Vector3(1.25f, .8f, -7.4f), "+", .07f, Color.white);
        }
        // Original Level Failed reward box: the hearts sit on a pale blue strip across its top, the countdown in a dark
        // pill, "Keep Going!" on the dark band at the bottom; white title.
        void StyleFailedSlot()
        {
            var slot = failed.Root.transform.Find(ResultFx.SlotName);
            if (!slot || hearts == null || hearts.Length == 0 || !failed.Ribbon) return;
            var sb = slot.GetComponent<Renderer>().bounds;
            float inset = sb.size.x * .035f, top = sb.max.y - inset, bottom = hearts[0].bounds.min.y - .12f;
            var pale = new Color(.655f, .745f, 1f); var dark = new Color(.227f, .235f, .486f);
            Block(failed, "Hearts strip", new Vector3(sb.center.x, (top + bottom) * .5f, 0), new Vector2(sb.size.x - 2 * inset, top - bottom), pale, -7.05f, true);
            Block(failed, "Hearts strip square edge", new Vector3(sb.center.x, bottom + (top - bottom) * .25f, 0), new Vector2(sb.size.x - 2 * inset, (top - bottom) * .5f), pale, -7.06f, false);
            float timerY = failed.Big.transform.position.y + .02f;
            Block(failed, "Timer pill", new Vector3(sb.center.x, timerY, 0), new Vector2(sb.size.x * .5f, .56f), dark, -7.1f, true);
            failed.Title.color = Color.white;
            // Original Try Again: a larger button.
            var tryAgain = failed.RightButton.transform.parent; tryAgain.localScale *= 1.22f;
            tryAgain.position += new Vector3(0, .3f, 0); // clear of the glass's bottom edge
            ScaleText(failed.Title, 1.2f); // original title is larger
        }
        // Stopwatch and countdown as one centred group in the timer pill (original), whatever the text width.
        void LayoutTimerRow()
        {
            var watch = failed.Root.transform.Find("Timer watch"); var pill = failed.Root.transform.Find("Timer pill");
            var text = failed.Big.GetComponent<Renderer>(); if (!watch || !pill || !text) return;
            var wr = watch.GetComponent<Renderer>(); float ww = wr.bounds.size.x, tw = text.bounds.size.x, gap = .08f;
            float total = ww + gap + tw, left = pill.GetComponent<Renderer>().bounds.center.x - total * .5f;
            watch.position += new Vector3(left + ww * .5f - wr.bounds.center.x, 0, 0);
            failed.Big.transform.position += new Vector3(left + ww + gap + tw * .5f - text.bounds.center.x, 0, 0);
        }
        // Coloured panel in world size: rounded (the popup's sliced panel art) or a plain rectangle.
        void Block(Popup p, string name, Vector3 world, Vector2 size, Color color, float z, bool rounded)
        {
            var r = Child<SpriteRenderer>(p, name, Vector3.zero);
            r.transform.position = new Vector3(world.x, world.y, p.Root.transform.position.z);
            r.transform.localPosition = new Vector3(r.transform.localPosition.x, r.transform.localPosition.y, z);
            if (rounded && p.Ribbon.sprite)
            {
                // A Full Rect white rounded panel: slicing the ribbon art (tight mesh) logged a tiling warning each time.
                r.sprite = SpriteFactory.Rounded(48, 16, 100); r.drawMode = SpriteDrawMode.Sliced; r.sharedMaterial = SpriteFactory.PlainSprite;
                var lossy = r.transform.lossyScale; r.size = new Vector2(size.x / lossy.x, size.y / lossy.y);
            }
            else
            {
                r.sprite = SpriteFactory.White;
                r.FitWorldSize(size.x, size.y);
            }
            r.color = color;
        }
        void BuildHearts(Popup p)
        {
            var heart = Resources.Load<Sprite>("VideoUI/icon-heart-big");
            hearts = new SpriteRenderer[LivesManager.Max];
            for (int i = 0; i < hearts.Length; i++)
            {
                var r = Child<SpriteRenderer>(p, "Heart " + i, new Vector3(-1.2f + i * .6f, 1.05f, -7.2f));
                r.sprite = heart;
                if (heart) { float s = .52f / heart.bounds.size.y; r.transform.localScale = new Vector3(s, s, 1); }
                hearts[i] = r;
            }
            lostHeartLabel = Label(p, "Lost heart", new Vector3(0, 1.05f, -7.4f), "-1", .05f, Color.white);
        }
        void RefreshHearts(bool justLost)
        {
            int lives = LivesManager.Lives;
            var lost = Resources.Load<Sprite>("VideoUI/icon-heart-lost"); var full = Resources.Load<Sprite>("VideoUI/icon-heart-big");
            for (int i = 0; i < hearts.Length; i++)
            {
                // Original: spent hearts are the dark purple heart art, not a tinted red one.
                bool have = i < lives;
                hearts[i].sprite = have || !lost ? full : lost;
                hearts[i].color = have || lost ? Color.white : new Color(.32f, .18f, .62f);
                hearts[i].SetLocalHeight(.52f);
            }
            bool show = justLost && lives < hearts.Length;
            lostHeartLabel.gameObject.SetActive(show);
            if (show) { var pos = hearts[lives].transform.localPosition; lostHeartLabel.transform.localPosition = new Vector3(pos.x, pos.y, -7.4f); }
        }
        string TimerText { get { return LivesManager.IsFull ? "Full" : LivesManager.Countdown(LivesManager.UntilNext); } }

        // Original "Play on (coin) 400": the label moves left, a coin and the price sit to its right.
        void PriceTag(Popup p, int price)
        {
            var root = p.Root.transform;
            var at = root.InverseTransformPoint(p.Right.transform.position);
            p.Right.transform.position -= root.right * .45f * root.lossyScale.x;
            var coin = Resources.Load<Sprite>("VideoUI/icon-coin");
            if (coin)
            {
                var c = Child<SpriteRenderer>(p, "Price coin", at + new Vector3(.5f, .02f, -.3f));
                c.sprite = coin; float k = .3f / coin.bounds.size.y; c.transform.localScale = new Vector3(k, k, 1);
                c.sortingOrder = p.Right.GetComponent<MeshRenderer>().sortingOrder;
            }
            var label = Label(p, "Price", at + new Vector3(.98f, 0, -.3f), price.ToString(), p.Right.characterSize, Color.white);
            label.GetComponent<MeshRenderer>().sortingOrder = p.Right.GetComponent<MeshRenderer>().sortingOrder;
        }

        // Original Out of Space shows the player's own stash: the gameplay camera is rendered once when the popup
        // opens and the strip around the waiting slots is shown inside the panel.
        Texture2D stashShot; SpriteRenderer stashPicture;
        // Original picture (recording 1, 1184 s): the cubes keep their colours while the board, lanes and floor turn
        // dark grey (lavender board 192 -> ~155, light floor 140 -> ~61): pastel pixels are greyed and darkened.
        static readonly Color PictureFrameTint = new Color(.36f, .36f, .5f);
        const float MuteGamma = 1.7f, VividFrom = .45f, VividRange = .25f;
        static void MuteBackground(Texture2D t)
        {
            var px = t.GetPixels32();
            for (int i = 0; i < px.Length; i++)
            {
                var c = px[i]; int max = Mathf.Max(c.r, Mathf.Max(c.g, c.b)), min = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
                float sat = max == 0 ? 0 : (max - min) / (float)max, keep = Mathf.Clamp01((sat - VividFrom) / VividRange);
                if (keep >= 1) continue;
                float lum = (.3f * c.r + .59f * c.g + .11f * c.b) / 255f, g = Mathf.Pow(lum, MuteGamma) * 255f;
                px[i] = new Color32((byte)Mathf.Lerp(g, c.r, keep), (byte)Mathf.Lerp(g, c.g, keep), (byte)Mathf.Lerp(g, c.b, keep), 255);
            }
            t.SetPixels32(px);
        }
        void CaptureStash()
        {
            var cam = screen.Controller.GameCamera;
            var slots = screen.Controller.StashSlots;
            if (!cam || slots == null || slots.Length == 0) return;
            int w = 720, h = Mathf.RoundToInt(w * (float)cam.pixelHeight / Mathf.Max(1, cam.pixelWidth));
            var rt = RenderTexture.GetTemporary(w, h, 24);
            var previous = cam.targetTexture;
            // Original picture: the five slots plus the extra empty white slot Play on would add, at the right end.
            var extra = ExtraSlotPreview(slots);
            // The camera is moved sideways for this one render so the six-slot row sits in the middle of the shot.
            var camPosition = cam.transform.position; float camFov = cam.fieldOfView;
            cam.fieldOfView = camFov * 1.3f; // a wider view: margins around the row as in the original picture
            if (extra) cam.transform.position += new Vector3((slots[0].position.x + extra.transform.position.x) * .5f - camPosition.x, 0, 0);
            // Frame the six-slot row centred, with some margin on both sides.
            float left = cam.WorldToViewportPoint(slots[0].position).x, right = extra ? cam.WorldToViewportPoint(extra.transform.position).x : cam.WorldToViewportPoint(slots[slots.Length - 1].position).x;
            float span = Mathf.Min(1f, (right - left) * 1.5f), centre = (left + right) * .5f;
            float x0 = Mathf.Clamp(centre - span * .5f, 0, 1 - span), x1 = x0 + span;
            cam.targetTexture = rt; cam.Render(); cam.targetTexture = previous;
            float middle = cam.WorldToViewportPoint(slots[slots.Length / 2].position).y;
            if (extra) DestroyImmediate(extra);
            cam.transform.position = camPosition; cam.fieldOfView = camFov;
            // Same shape as the reward box it fills (~4:3.4), the stash in its upper third with the board's base above
            // and two lane rows below, like the original's picture.
            float aspect = .82f; float rl, rr, rt2, rb2;
            if (RewardRegion(lose, out rl, out rr, out rt2, out rb2)) aspect = (rt2 - rb2) / Mathf.Max(.01f, rr - rl); // same shape as the box: no side crop
            int cw = Mathf.RoundToInt((x1 - x0) * w), ch = Mathf.Min(h, Mathf.RoundToInt(cw * aspect));
            int y0 = Mathf.Clamp(Mathf.RoundToInt(middle * h) - Mathf.RoundToInt(ch * .62f), 0, h - ch);
            if (!stashShot || stashShot.width != cw || stashShot.height != ch)
            {
                if (stashShot) Destroy(stashShot);
                stashShot = new Texture2D(cw, ch, TextureFormat.RGB24, false) { name = "Out of space stash" };
            }
            var active = RenderTexture.active; RenderTexture.active = rt;
            stashShot.ReadPixels(new Rect(Mathf.RoundToInt(x0 * w), y0, cw, ch), 0, 0);
            MuteBackground(stashShot); stashShot.Apply();
            RenderTexture.active = active; RenderTexture.ReleaseTemporary(rt);
            if (!stashPicture)
            {
                stashPicture = Child<SpriteRenderer>(lose, "Stash snapshot", new Vector3(0, .82f, -7.3f));
                foreach (Transform child in lose.Root.transform)
                    if (child.name.StartsWith("Stash picture")) child.gameObject.SetActive(false);
            }
            stashPicture.sprite = Sprite.Create(stashShot, new Rect(0, 0, cw, ch), new Vector2(.5f, .5f), 100);
            FillRewardBox(lose, stashPicture);
        }

        GameObject ExtraSlotPreview(Transform[] slots)
        {
            if (slots.Length < 2) return null;
            var stash = slots[0].parent; var first = stash.Find("Solid waiting pad 0"); if (!first) return null;
            var step = slots[slots.Length - 1].position - slots[slots.Length - 2].position;
            var copy = Instantiate(first.gameObject, stash);
            copy.name = "Extra slot preview";
            copy.transform.position = first.position + (slots[slots.Length - 1].position - slots[0].position) + step;
            var r = copy.GetComponent<Renderer>(); var src = first.GetComponent<Renderer>();
            if (r && src) r.sharedMaterial = src.sharedMaterial; // the plain white tile, never the grey locked tint
            var shadow = stash.Find("Pad shadow 0");
            if (shadow)
            {
                var s = Instantiate(shadow.gameObject, stash); s.transform.position = shadow.position + (copy.transform.position - first.position);
                s.transform.SetParent(copy.transform, true);
            }
            return copy;
        }

        // The stash picture fills the reward box's inner area (above the caption band, under it at the bottom)
        // and is clipped to the box's rounded inner shape, as in the original; it used to run over the box's rim.
        // Inside of the card's white picture slot (inside its rim), down to the middle of the caption band.
        static readonly Vector2 SlotRim = new Vector2(11f / 316f, 9f / 392f);
        bool RewardRegion(Popup p, out float left, out float right, out float top, out float pictureBottom)
        {
            left = right = top = pictureBottom = 0;
            var slot = p.Root.transform.Find(ResultFx.SlotName); var art = slot ? slot.GetComponent<SpriteRenderer>() : null;
            if (!art) return false;
            var sb = art.bounds; float rimX = sb.size.x * SlotRim.x, rimY = sb.size.y * SlotRim.y;
            left = sb.min.x + rimX; right = sb.max.x - rimX; top = sb.max.y - rimY;
            var band = p.Root.transform.Find(ResultFx.BandName);
            pictureBottom = band ? band.GetComponent<Renderer>().bounds.center.y : sb.min.y + rimY;
            return true;
        }
        // Fills the picture slot with a picture (cover fit, centred) clipped to the slot's rounded inside.
        internal void FillRewardBox(Popup p, SpriteRenderer picture)
        {
            float left, right, top, pictureBottom;
            if (!picture.sprite || !RewardRegion(p, out left, out right, out top, out pictureBottom)) return;
            picture.transform.localScale = Vector3.one;
            var size = picture.bounds.size;
            float k = Mathf.Max((right - left) / size.x, (top - pictureBottom) / size.y);
            picture.transform.localScale = new Vector3(k, k, 1);
            // Taller than the box: the bottom stays on the caption band and the top is cropped (never below the band).
            float height = size.y * k, y = Mathf.Max((top + pictureBottom) * .5f, pictureBottom + height * .5f);
            picture.transform.position = new Vector3((left + right) * .5f, y, picture.transform.position.z);
            var maskObject = p.Root.transform.Find("Picture mask"); // the card has other masks (glass, caption band)
            if (!maskObject)
            {
                var slot = p.Root.transform.Find(ResultFx.SlotName);
                var mask = Child<SpriteMask>(p, "Picture mask", Vector3.zero);
                mask.sprite = slot.GetComponent<SpriteRenderer>().sprite;
                mask.transform.position = slot.position;
                var ls = slot.localScale; mask.transform.localScale = new Vector3(ls.x * (1 - 2 * SlotRim.x), ls.y * (1 - 2 * SlotRim.y), ls.z);
                mask.isCustomRangeActive = true; mask.frontSortingOrder = 3; mask.backSortingOrder = 1;
            }
            picture.sortingOrder = 2; picture.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            picture.sharedMaterial = SpriteFactory.PlainSprite;
        }

        // Original "Play on (coin) 400": one big green button centred on the card's bottom edge (half over it), the
        // label, coin and price as one centred row inside it.
        void StylePlayOn(Popup p)
        {
            var group = p.RightButton.transform.parent; var body = group.Find("button-green-v2");
            var bodyR = body ? body.GetComponent<Renderer>() : null; if (!bodyR || !p.Border) return;
            var card = p.Border.bounds; var bb = bodyR.bounds;
            float width = card.size.x * .6f, height = width * .34f;
            var ls = body.localScale; body.localScale = new Vector3(ls.x * width / bb.size.x, ls.y * height / bb.size.y, ls.z);
            foreach (var hit in group.GetComponentsInChildren<BoxCollider>(true)) { var hs = hit.size; hit.size = new Vector3(hs.x * width / bb.size.x, hs.y * height / bb.size.y, hs.z); }
            bb = bodyR.bounds;
            group.position += new Vector3(card.center.x - bb.center.x, card.min.y + height * .12f - bb.center.y, 0);
            bb = bodyR.bounds;
            // Row: "Play on" + coin + price, centred in the button.
            var root = p.Root.transform;
            var coin = root.Find("Price coin"); var price = root.Find("Price");
            Configure(null, p.Right, null, "Play on", 99f);
            if (price) foreach (var t in price.GetComponentsInChildren<TextMesh>(true)) t.characterSize = p.Right.characterSize;
            var parts = new System.Collections.Generic.List<Transform> { p.Right.transform };
            if (coin) parts.Add(coin);
            if (price) parts.Add(price);
            float gap = height * .1f, total = -gap; var widths = new System.Collections.Generic.List<float>();
            foreach (var t in parts) total += t.GetComponent<Renderer>().bounds.size.x + gap;
            // The whole row scales to ~78% of the button's width.
            float fit = width * .78f / Mathf.Max(.0001f, total);
            foreach (var t in parts) t.localScale *= fit;
            gap *= fit; total = -gap;
            foreach (var t in parts) { float w = t.GetComponent<Renderer>().bounds.size.x; widths.Add(w); total += w + gap; }
            float x = bb.center.x - total * .5f;
            for (int i = 0; i < parts.Count; i++)
            {
                var r = parts[i].GetComponent<Renderer>(); float cx = x + widths[i] * .5f;
                parts[i].position += new Vector3(cx - r.bounds.center.x, bb.center.y + height * .04f - r.bounds.center.y, 0);
                if (parts[i] != p.Right.transform) parts[i].SetParent(group, true);
                x += widths[i] + gap;
            }
            // The reward box reaches down to just above the button (original), its caption band following it.
            var slot = root.Find(ResultFx.SlotName); var slotR = slot ? slot.GetComponent<Renderer>() : null;
            if (slotR)
            {
                var sb = slotR.bounds; float newBottom = bb.max.y + height * .2f, delta = newBottom - sb.min.y;
                if (delta < 0)
                {
                    float k = (sb.max.y - newBottom) / sb.size.y; var sl = slot.localScale;
                    slot.localScale = new Vector3(sl.x, sl.y * k, sl.z);
                    slot.position += new Vector3(0, (sb.max.y + newBottom) * .5f - slotR.bounds.center.y, 0);
                    foreach (var name in new[] { ResultFx.BandName, ResultFx.BandTopName })
                    { var t = root.Find(name); if (t) t.position += new Vector3(0, delta, 0); }
                    p.Caption.transform.position += new Vector3(0, delta, 0);
                    var mask = root.Find("Reward band mask");
                    if (mask) { mask.position = slot.position; var ml = slot.localScale; mask.localScale = new Vector3(ml.x * (1 - 2 * 11f / 316f), ml.y * (1 - 2 * 9f / 392f), ml.z); }
                }
            }
        }


        internal SpriteRenderer Art(Popup p, string sprite, Vector3 local, float width)
        {
            var r = Child<SpriteRenderer>(p, System.IO.Path.GetFileName(sprite), local);
            r.sprite = Resources.Load<Sprite>(sprite.Contains("/") ? sprite : "HardLevel/" + sprite);
            r.SetLocalWidth(width);
            return r;
        }
        void ShowFailed(bool justLost, bool noLives)
        {
            failedIsNoLives = noLives;
            failed.Title.text = noLives ? "Level Failed!" : "Level Failed!";
            Configure(null, failed.Caption, null, noLives || !LivesManager.CanPlay ? "You Have No Lives!" : "Keep Going!", 18f);
            failed.Big.text = TimerText; LayoutTimerRow();
            // White title with the same near-black outline as every other caption (recording 1, 1187 s).
            OutlinedText.Add(failed.Title, TextOutliner.Ink);
            RefreshHearts(justLost);
            bool canPlay = LivesManager.CanPlay;
            LayoutNoLives(!canPlay);
            // Original no-lives popup: "Speed Up" (video) and purple "Refill (coin) 600", a green Home button below.
            if (!canPlay)
            {
                Configure(failed.LeftButton, failed.Left, "speed-up", "Speed Up", 7f);
                Configure(failed.RightButton, failed.Right, "refill-lives", "Refill", 5f);
            }
            else Configure(failed.RightButton, failed.Right, !noLives ? "retry" : "close-failed", !noLives ? "Try Again" : "Close", 9f);
            // The original's Level Failed card has no close button: Try Again only (the no-lives card keeps it).
            var close = failed.Root.transform.Find("Close button"); if (close) close.gameObject.SetActive(!canPlay);
            Show(failed);
        }

        // Two-button no-lives layout of the failed popup (Speed Up | Refill), built from the template's x2/Next.
        readonly System.Collections.Generic.Dictionary<Transform, float> failedRightX = new System.Collections.Generic.Dictionary<Transform, float>();
        GameObject failedHome, failedRefillPrice;
        SpriteRenderer failedRightBody; Sprite failedGreen, failedPurple; Vector3 failedGreenScale;
        void BuildNoLivesButtons()
        {
            var root = failed.Root.transform;
            failedRightBody = failed.RightButton.transform.parent.GetComponentsInChildren<SpriteRenderer>(true).OrderByDescending(r => r.bounds.size.x * r.bounds.size.y).FirstOrDefault();
            if (failedRightBody) { failedGreen = failedRightBody.sprite; failedGreenScale = failedRightBody.transform.localScale; }
            failedPurple = Resources.Load<Sprite>("Lives/button-purple");
            // TV badge on Speed Up.
            var left = failed.LeftButton.transform.parent;
            var leftBody = left.GetComponentsInChildren<SpriteRenderer>(true).OrderByDescending(r => r.bounds.size.x * r.bounds.size.y).FirstOrDefault();
            // The x2 template already carries the coin stack and TV badge; Speed Up keeps only the TV and a centred label.
            foreach (Transform child in left) if (child.name == "icon-coin-reward") Destroy(child.gameObject);
            if (leftBody) { var lp = failed.Left.transform.position; lp.x = leftBody.bounds.center.x; failed.Left.transform.position = lp; }
            var tv = left.Find("iconRW") ? null : Resources.Load<Sprite>("VideoUI/iconRW");
            if (leftBody && tv)
            {
                var b = leftBody.bounds;
                var badge = Child<SpriteRenderer>(failed, "Speed up badge", root.InverseTransformPoint(new Vector3(b.min.x + b.size.y * .1f, b.max.y - b.size.y * .02f, b.center.z)) + new Vector3(0, 0, -.4f));
                badge.sprite = tv; badge.sortingOrder = leftBody.sortingOrder + 2;
                badge.transform.localRotation = Quaternion.Euler(0, 0, 12);
                float k = b.size.y * .62f / tv.bounds.size.y / Mathf.Max(.0001f, root.lossyScale.y); badge.transform.localScale = new Vector3(k, k, 1);
                badge.transform.SetParent(left, true);
            }
            // Price on the Refill button: coin + 600, children of the right group so they follow its layout.
            var right = failed.RightButton.transform.parent;
            var price = new GameObject("Refill price") { layer = failed.Root.layer };
            price.transform.SetParent(right, false);
            failedRefillPrice = price;
            var coin = Resources.Load<Sprite>("VideoUI/icon-coin");
            var at = failed.Right.transform.localPosition;
            if (coin)
            {
                var c = new GameObject("Price coin") { layer = failed.Root.layer }.AddComponent<SpriteRenderer>();
                c.transform.SetParent(price.transform, false); c.sprite = coin; c.sortingOrder = failed.Right.GetComponent<MeshRenderer>().sortingOrder;
                c.transform.position = failed.Right.transform.position + root.right * .22f * root.lossyScale.x - root.forward * .01f;
                float k = .24f * root.lossyScale.y / coin.bounds.size.y / Mathf.Max(.0001f, c.transform.parent.lossyScale.y); c.transform.localScale = new Vector3(k, k, 1);
            }
            var amount = Instantiate(failed.Right.gameObject, price.transform, true); amount.name = "Price amount";
            foreach (var t in amount.GetComponentsInChildren<TextMesh>(true)) t.text = EconomyManager.RefillLivesPrice.ToString();
            amount.transform.position = failed.Right.transform.position + root.right * .6f * root.lossyScale.x;
            amount.transform.localScale *= .8f;
            failedRightX[price.transform] = failedRightX.ContainsKey(failed.Right.transform) ? failedRightX[failed.Right.transform] : 0;
            // Big green Home button under the card.
            failedHome = Instantiate(right.gameObject, root, true); failedHome.name = "Failed home button";
            foreach (Transform child in failedHome.transform) if (child.name == "Refill price") Destroy(child.gameObject);
            // Its top sits just under the card's bottom edge.
            var homeBody = failedHome.GetComponentsInChildren<SpriteRenderer>(true).OrderByDescending(r => r.bounds.size.x * r.bounds.size.y).FirstOrDefault();
            if (homeBody) failedHome.transform.position += Vector3.up * (failed.Border.bounds.min.y - .1f * root.lossyScale.y - homeBody.bounds.max.y);
            foreach (var t in failedHome.GetComponentsInChildren<TextMesh>(true)) t.text = "Home";
            foreach (var button in failedHome.GetComponentsInChildren<VideoUiButton>(true)) button.Action = "home";
            failedHome.SetActive(false);
        }
        void LayoutNoLives(bool noLivesLayout)
        {
            failed.LeftButton.transform.parent.gameObject.SetActive(noLivesLayout);
            foreach (var pair in failedRightX)
                if (pair.Key) pair.Key.localPosition = new Vector3(noLivesLayout ? pair.Value : 0, pair.Key.localPosition.y, pair.Key.localPosition.z);
            if (failedRefillPrice) failedRefillPrice.SetActive(noLivesLayout);
            if (failedHome) failedHome.SetActive(noLivesLayout);
            // The label makes room for the price.
            var label = failed.Right.transform; float x = failedRightX.ContainsKey(label) ? failedRightX[label] : 0;
            label.localPosition = new Vector3(noLivesLayout ? x - .36f * Mathf.Abs(1 / Mathf.Max(.0001f, label.parent.lossyScale.x)) * failed.Root.transform.lossyScale.x : 0, label.localPosition.y, label.localPosition.z);
            if (failedRightBody && failedPurple && failedGreen)
            {
                failedRightBody.sprite = noLivesLayout ? failedPurple : failedGreen;
                var a = failedGreen.bounds.size; var b = failedRightBody.sprite.bounds.size;
                failedRightBody.transform.localScale = new Vector3(failedGreenScale.x * a.x / b.x, failedGreenScale.y * a.y / b.y, failedGreenScale.z);
            }
        }

        // Home PLAY with no hearts left: the big NO MORE LIVES popup (original canvas_nolives).
        public void ShowNoLives() { lives.ShowPopup(); }

        // Original "OUT OF SPACE!" (recording 1, 1181.3-1183.6 s, frame by frame): a red band across the board appears
        // at once and holds ~1.75 s, then the board shows bare for ~0.5 s before the popup.
        GameObject banner; float outOfSpaceAt = -1;

        const float BannerTime = 1.75f, LosePause = 2.3f;
        void BuildOutOfSpaceBanner()
        {
            banner = new GameObject("Out of space banner") { layer = screen.Result.layer };
            banner.transform.SetParent(screen.Result.transform.parent, false);
            banner.transform.localPosition = screen.Result.transform.localPosition;
            var p = new Popup { Root = banner };
            var sprite = SpriteFactory.White;
            // Measured on the original (level 261): a see-through pink-red band (~76% opaque) a little below the
            // board's middle, two solid red lines just inside its edges and a thin dark rim; white caption.
            const float y = 1.08f;
            var pink = new Color(.886f, .267f, .38f, .76f); var red = new Color(.875f, .204f, .29f, 1f); var rim = new Color(.3f, .12f, .22f, .55f);
            foreach (var part in new[] { new { y = y, h = 1.25f, z = -6.2f, c = pink },
                                         new { y = y + .475f, h = .1f, z = -6.25f, c = red },
                                         new { y = y - .48f, h = .1f, z = -6.25f, c = red },
                                         new { y = y + .63f, h = .03f, z = -6.25f, c = rim },
                                         new { y = y - .63f, h = .03f, z = -6.25f, c = rim } })
            {
                var r = Child<SpriteRenderer>(p, "Band", new Vector3(0, part.y, part.z));
                r.sprite = sprite; r.color = part.c; r.transform.localScale = new Vector3(8f, part.h, 1);
            }
            var caption = OutlinedLabel(p, "Out of space text", new Vector3(0, y, -6.4f), .062f);
            SetText(caption, "OUT OF SPACE!");
            banner.SetActive(false);
        }
        void UpdateOutOfSpace()
        {
            if (!screen.Feedback.FailureVisible || gaveUp) { if (!screen.Feedback.FailureVisible) outOfSpaceAt = -1; banner.SetActive(false); return; }
            if (outOfSpaceAt < 0) outOfSpaceAt = Time.unscaledTime;
            float t = Time.unscaledTime - outOfSpaceAt;
            banner.SetActive(t < BannerTime);
            if (t >= LosePause) { CaptureStash(); Show(lose); }
        }

        // White caption with a dark purple outline (OutlinedText). Returned as an array so callers can treat any label
        // and its parts alike; the outline copies follow the label by themselves.
        internal TextMesh[] OutlinedLabel(Popup p, string name, Vector3 local, float size)
        {
            var main = Label(p, name, local, "", size, Color.white);
            OutlinedText.Add(main, TextOutliner.Ink);
            return new[] { main };
        }
        internal static void SetText(TextMesh[] labels, string text) { foreach (var t in labels) if (t) t.text = text; }

        // Gallery covers (316x376) carry a transparent margin (~12 px each side) and wide rounded corners, which left
        // gaps inside the slot; the card uses the picture inset past them, centred, so it fills the slot edge to edge.
        readonly System.Collections.Generic.Dictionary<Sprite, Sprite> coverCrops = new System.Collections.Generic.Dictionary<Sprite, Sprite>();
        internal Sprite CoverCrop(Sprite source)
        {
            if (coverCrops.TryGetValue(source, out var cropped)) return cropped;
            var r = source.rect;
            float insetX = r.width * .07f, insetY = r.height * .03f;
            var rect = new Rect(r.x + insetX, r.y + insetY, r.width - 2 * insetX, r.height - 2 * insetY);
            cropped = Sprite.Create(source.texture, rect, new Vector2(.5f, .5f), source.pixelsPerUnit);
            cropped.name = source.name + " (card)";
            coverCrops[source] = cropped; runtimeArt.Add(cropped);
            return cropped;
        }
        // Grey copy of a level render (collections without a configured black-and-white cover).
        internal Sprite Greyscale(Sprite source)
        {
            if (!source || !source.texture.isReadable) return null;
            var r = source.textureRect;
            var pixels = source.texture.GetPixels((int)r.x, (int)r.y, (int)r.width, (int)r.height);
            for (int i = 0; i < pixels.Length; i++) { float g = pixels[i].grayscale * .85f + .1f; pixels[i] = new Color(g, g, g, pixels[i].a); }
            var texture = new Texture2D((int)r.width, (int)r.height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels(pixels); texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0, 0, r.width, r.height), new Vector2(.5f, .5f), source.pixelsPerUnit);
            runtimeArt.Add(texture); runtimeArt.Add(sprite);
            return sprite;
        }
        void OnDestroy() { foreach (var o in runtimeArt) if (o) Destroy(o); if (stashShot) Destroy(stashShot); }
        // Scales a label together with its outline copies.
        internal static void ScaleText(TextMesh label, float factor)
        {
            foreach (var t in label.GetComponentsInChildren<TextMesh>(true)) t.characterSize *= factor;
        }
        readonly System.Collections.Generic.Dictionary<TextMesh, float> baseSizes = new System.Collections.Generic.Dictionary<TextMesh, float>();
        internal void Configure(VideoUiButton button, TextMesh label, string action, string text, float fitChars = 5.5f)
        {
            if (button) button.Action = action;
            // Long labels must fit their panel width; outline/shadow copies follow the label.
            int longest = 0; foreach (var line in text.Split('\n')) longest = Mathf.Max(longest, line.Length);
            float factor = Mathf.Clamp(fitChars / Mathf.Max(fitChars, longest), .5f, 1f);
            foreach (var t in label.GetComponentsInChildren<TextMesh>(true))
            {
                float size;
                if (!baseSizes.TryGetValue(t, out size)) baseSizes[t] = size = t.characterSize;
                t.text = text; t.characterSize = size * factor;
            }
        }
        // Recording 2 (21.17-21.57 s open, 20.22-20.55 s close): cards grow from ~1/10 of their size to full in
        // ~0.4 s, about evenly and without overshoot, and shrink away in ~0.33 s (fast at first, easing out) before
        // the dim layer goes.
        const float PopStart = .1f, PopTime = .4f, SlowPopTime = .5f, FastPopTime = .13f, CloseTime = .33f;
        GameObject closing; float closedAt; Collider[] closingHits;
        // The dimmed backdrop always covers the whole screen; only the card zooms.
        // Pop scale of a card: across the screen only - its depth stays, so the dim layer keeps its place between the
        // page under it and the card (scaled in depth too, it slid behind Home's buttons while the card was small and
        // they flashed bright for the first ~0.1 s of every pop). The dim layer undoes the scale to cover the screen.
        void SetPopScale(GameObject root, float scale)
        {
            root.transform.localScale = new Vector3(scale, scale, 1);
            var dim = root.transform.Find("Dark backdrop");
            if (!dim) return;
            Vector3 full;
            if (!backdrops.TryGetValue(dim, out full)) backdrops[dim] = full = dim.localScale;
            float k = Mathf.Max(.001f, scale); // same floor as the closing card: at .05 the dim shrank to a dark box at the end
            dim.localScale = new Vector3(full.x / k, full.y / k, full.z);
        }
        readonly System.Collections.Generic.Dictionary<Transform, Vector3> backdrops = new System.Collections.Generic.Dictionary<Transform, Vector3>();
        int LevelShown { get { return screen.Levels.PlayedNumber; } }

        // A card that waits a moment over its dimmed screen before popping in: everything but the backdrop and the
        // coin bar is hidden until then (pop-in starts at the new shownAt; taps wait for it too).
        readonly System.Collections.Generic.List<GameObject> heldCard = new System.Collections.Generic.List<GameObject>();
        internal void HoldCard(float seconds)
        {
            if (!Open) return;
            shownAt = Time.unscaledTime + seconds;
            foreach (Transform child in Open.transform)
                if (child.gameObject.activeSelf && child.name != "Dark backdrop" && System.Array.IndexOf(CoinBar, child.name) < 0)
                { child.gameObject.SetActive(false); heldCard.Add(child.gameObject); }
        }
        void ReleaseCard() { foreach (var g in heldCard) if (g) g.SetActive(true); heldCard.Clear(); }

        // Recording 1 (1327.9-1328.15 s out of space, 1331.24-1331.45 s no lives): a see-through dim layer fades in over
        // ~0.25 s rather than appearing at once. Opaque screens (CONGRATS, pages) cut in as before.
        const float DimFade = .25f;
        SpriteRenderer fadingDim; Color fadingColor; float dimmedAt;
        void StartDimFade(GameObject root)
        {
            EndDimFade();
            var dim = root.transform.Find("Dark backdrop"); var r = dim ? dim.GetComponent<SpriteRenderer>() : null;
            if (!r || r.color.a >= 1 || IsPage(root)) return;
            fadingDim = r; fadingColor = r.color; dimmedAt = Time.unscaledTime;
            r.color = new Color(fadingColor.r, fadingColor.g, fadingColor.b, 0);
        }
        void TickDimFade()
        {
            if (!fadingDim) return;
            float k = Mathf.Clamp01((Time.unscaledTime - dimmedAt) / DimFade);
            fadingDim.color = new Color(fadingColor.r, fadingColor.g, fadingColor.b, fadingColor.a * k);
            if (k >= 1) fadingDim = null;
        }
        void EndDimFade() { if (fadingDim) fadingDim.color = fadingColor; fadingDim = null; }

        internal void Show(Popup p)
        {
            ReleaseCard();
            if (Open) Open.SetActive(false);
            if (closing) FinishClosing();
            Open = p.Root; shownAt = Time.unscaledTime;
            p.Header.text = "Level " + LevelShown;
            RefreshPopupCoins();
            // The frame's darker edge follows the popup's frame colour (gold, red...).
            var edge = p.Root.transform.Find(ResultFx.EdgeName);
            if (edge && p.Border) edge.GetComponent<SpriteRenderer>().color = Color.Lerp(p.Border.color, new Color(.45f, .08f, .04f), .35f);
            p.Root.SetActive(true);
            bool fullPage = IsPage(p.Root);
            SetPopScale(p.Root, fullPage ? 1 : PopStart);
            StartDimFade(p.Root);
            if (fullPage) StartPageSlide(p.Root);
            if (screen.Controller) screen.Controller.BoosterInputBlocked = true;
        }

        public void Close()
        {
            ReleaseCard(); EndDimFade();
            if (!Open) return;
            LeavePage(Open);
            if (closing) FinishClosing();
            // The card shrinks away (pages go at once); its touch areas are off meanwhile. Open is cleared now, so
            // the game carries on as before.
            if (IsPage(Open)) Open.SetActive(false);
            else
            {
                closing = Open; closedAt = Time.unscaledTime;
                closingHits = closing.GetComponentsInChildren<Collider>();
                foreach (var c in closingHits) c.enabled = false;
            }
            Open = null;
            if (screen.Controller) { screen.Controller.BoosterInputBlocked = false; screen.Controller.SuppressInputThisFrame(); }
            intros.ShowNextQueued();
        }
        void FinishClosing()
        {
            foreach (var c in closingHits) if (c) c.enabled = true;
            closing.SetActive(false); closing = null;
        }
        void TickClosing()
        {
            if (!closing) return;
            float t = Mathf.Clamp01((Time.unscaledTime - closedAt) / CloseTime);
            float s = Mathf.Max(.001f, Mathf.Pow(1 - t, 1.6f));
            SetPopScale(closing, s);
            if (t >= 1) FinishClosing();
        }
        public bool Owns(VideoUiButton button) { return Open && button && button.transform.IsChildOf(Open.transform); }

        // Returns true when the popup consumed the action.
        public bool Handle(string action)
        {
            if (!Open || Time.unscaledTime - shownAt < .15f) return Open;
            if (action.StartsWith("nav-")) return HandleNav(action);
            foreach (var module in modules) if (module.Handles(action)) return module.Handle(action);
            switch (action)
            {
                case "close": Close(); return true;
                case "retry":
                    // In a level the current build keeps the Level Failed card ("You Have No Lives!" + Refill).
                    if (!LivesManager.CanPlay) { ShowFailed(false, true); return true; }
                    Close(); screen.Feedback.Retry(); return true;
                case "home": Close(); gaveUp = false; screen.Feedback.GoHome(); return true;
                case "play-on":
                    if (!EconomyManager.TrySpend(PlayOnPrice, "play-on")) return true; // not enough coins: nothing happens
                    if (!screen.Controller.AddExtraSlot()) { EconomyManager.Add(PlayOnPrice, "refund"); return true; }
                    Close(); return true;
                case "give-up":
                    // Giving up the level costs a heart (original: Out of Space ✕ → Level Failed with -1).
                    gaveUp = true;
                    LivesManager.LoseLife();
                    GameEvents.RaiseLevelLost(screen.Levels.PlayedNumber);
                    ShowFailed(true, false); return true;
                case "speed-up":
                    // No ads in the clone: the rewarded video is granted straight away.
                    LivesManager.SpeedUp();
                    if (LivesManager.CanPlay) ShowFailed(false, false); else failed.Big.text = TimerText;
                    return true;
                case "close-failed":
                    Close();
                    if (!failedIsNoLives) { gaveUp = false; screen.Feedback.GoHome(); }
                    return true;
                case "refill-lives":
                    if (!EconomyManager.TrySpend(EconomyManager.RefillLivesPrice, "refill-lives")) return true; // not enough coins: nothing happens
                    LivesManager.Refill();
                    Close();
                    if (screen.Current == VideoScreen.Page.Gameplay) { gaveUp = false; screen.Feedback.Retry(); }
                    return true;
            }
            return true;
        }

        void Update()
        {
            if (!screen.Feedback.FailureVisible) gaveUp = false;
            TickClosing(); TickDimFade(); TickPageSlide();
            if (!Open) { UpdateOutOfSpace(); return; }
            if (Open == failed.Root && Time.frameCount % 15 == 0) { failed.Big.text = TimerText; LayoutTimerRow(); }
            // Pop-in (see PopStart); the HARD / BONUS level cards grow a little more slowly (recording of level 260).
            bool page = IsPage(Open);
            if (heldCard.Count > 0 && Time.unscaledTime >= shownAt) ReleaseCard();
            bool fast = congrats.FastPopIn;
            float t = Mathf.Clamp01((Time.unscaledTime - shownAt) / (intros.SlowPopIn ? SlowPopTime : fast ? FastPopTime : PopTime));
            if (fast) t = 1 - (1 - t) * (1 - t);
            float s = page || heldCard.Count > 0 ? 1 : PopStart + (1 - PopStart) * t;
            SetPopScale(Open, s);
            if ((Open == lose.Root || (Open == failed.Root && !failedIsNoLives)) && !screen.Feedback.FailureVisible) Close();
            foreach (var module in modules) module.Tick();
        }
    }
}
