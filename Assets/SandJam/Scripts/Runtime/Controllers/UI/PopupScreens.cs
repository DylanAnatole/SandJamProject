using System.Linq;
using UnityEngine;

namespace SandJamTest.Scene3D
{
    // "Out of space" and "Settings" popups built from the original Level Complete popup art
    // (gold frame, header ribbon, yellow/green buttons) so every dialog shares one style.
    public sealed partial class PopupScreens : MonoBehaviour
    {
        VideoScreen screen;
        Popup lose, settings, intro, failed, congrats, hard, bonus, artStyle;
        TextMesh hardLevelLabel, bonusLevelLabel, artStyleLabel;
        SpriteRenderer artStylePicture;
        int pendingHardLevel, pendingBonusLevel;
        public const int PlayOnPrice = 400; // original "Play on 400" for one extra waiting slot
        SpriteRenderer[] hearts;
        TextMesh lostHeartLabel;
        bool gaveUp, failedIsNoLives;
        SpriteRenderer cardBw, cardColor; Transform congratsRays; TextMesh[] cardName, cardPercent;
        readonly System.Collections.Generic.List<Object> runtimeArt = new System.Collections.Generic.List<Object>();
        SpriteSequence introGif;
        readonly System.Collections.Generic.Queue<OriginalConfig.Intro> pendingIntros = new System.Collections.Generic.Queue<OriginalConfig.Intro>();
        readonly System.Collections.Generic.HashSet<OriginalConfig.Intro> boosterIntros = new System.Collections.Generic.HashSet<OriginalConfig.Intro>();
        // Vietnamese captions for the original NewFeature/NewBooster texts (English kept as fallback).
        static readonly System.Collections.Generic.Dictionary<string, string> Captions = new System.Collections.Generic.Dictionary<string, string>
        {
            { "Vertical Chained Cubes move together to stash area", "Khối nối dọc cùng lên ô chờ" },
            { "Mystery Cubes become visible when they reach the front row", "Khối bí ẩn hiện màu khi ra hàng đầu" },
            { "Horizontal Chained Cubes move together to stash area", "Khối nối ngang cùng lên ô chờ" },
            { "Locked grids become available as the cubes color tiles!", "Vùng khóa mở khi các khối tô màu" },
            { "Flare reveals a random tile on the canvas", "Pháo sáng hé lộ một vùng" },
            { "You can swap first and second line", "Đổi chỗ hàng 1 và hàng 2" },
            { "You can tap on any shooter you want", "Chọn bất kỳ khối nào bạn muốn" },
        };
        float shownAt;
        public GameObject Open { get; private set; }
        public bool SettingsOpen { get { return Open && settings != null && Open == settings.Root; } }

        sealed class Popup
        {
            public GameObject Root;
            public TextMesh Header, Title, Big, Caption, Left, Right;
            public SpriteRenderer Icon, Border, Ribbon;
            public VideoUiButton LeftButton, RightButton;
        }

        public void Initialize(VideoScreen owner)
        {
            screen = owner;
            // Original "Out of Space": gold frame, red level ribbon, stash picture, "Free Extra 1 Slot",
            // green "Play on 400" and a red close button that gives the level up.
            lose = Build("Out of space popup");
            TintRibbon(lose, new Color(.93f, .25f, .36f));
            lose.Title.text = "Hết chỗ!";
            lose.Icon.gameObject.SetActive(false); lose.Big.text = "";
            BuildStashPicture(lose);
            lose.Caption.text = "Thêm 1 ô chờ";
            SingleButton(lose);
            Configure(lose.RightButton, lose.Right, "play-on", "Chơi tiếp · " + PlayOnPrice, 7f);
            AddCloseButton(lose, "give-up");
            // Original "Level Failed!": red frame, hearts with the lost one marked -1, refill timer, Try Again.
            failed = Build("Level failed popup");
            failed.Border.color = new Color(.86f, .16f, .2f); failed.Root.transform.Find("Gold header").GetComponent<SpriteRenderer>().color = new Color(.9f, .2f, .24f);
            failed.Icon.gameObject.SetActive(false);
            BuildHearts(failed);
            failed.Big.transform.localPosition += new Vector3(0, -.15f, 0); ScaleText(failed.Big, .5f);
            SingleButton(failed);
            AddCloseButton(failed, "home");
            // Collection card shown after a win (original "CONGRATS · Accessories · 90% Completed").
            BuildCongratsCard();
            BuildOutOfSpaceBanner();
            BuildSandQuest();
            BuildShop();
            BuildNoLives();
            BuildRewardsPanel();
            BuildTasksPanel();
            BuildHomeWidgets();
            BuildGallery();
            BuildSettings();
            intro = Build("Feature intro popup");
            intro.Big.text = "";
            intro.Icon.transform.localPosition = new Vector3(0, .8f, intro.Icon.transform.localPosition.z);
            intro.LeftButton.transform.parent.gameObject.SetActive(false);
            foreach (Transform child in intro.RightButton.transform.parent)
                child.localPosition = new Vector3(0, child.localPosition.y, child.localPosition.z);
            Configure(intro.RightButton, intro.Right, "close", "Tiếp tục");
            BuildHardPopup();
            BuildArtStylePopup();
            owner.Feedback.UseLegacyOverlay = false;
        }

        Popup Build(string name)
        {
            var root = Instantiate(screen.Result, screen.Result.transform.parent);
            root.name = name; root.SetActive(false);
            // In front of every page (Home art reaches z -8), not only the gameplay screen.
            root.transform.localPosition += new Vector3(0, 0, -5f);
            var confetti = root.transform.Find("Confetti"); if (confetti) Destroy(confetti.gameObject);
            var placeholder = root.GetComponentInChildren<ReferenceUiPlaceholder>(true); if (placeholder) Destroy(placeholder);
            // Let the paused board show through, unlike the full-screen result page.
            var backdrop = root.transform.Find("Dark backdrop").GetComponent<SpriteRenderer>();
            var dim = backdrop.color; dim.a = .78f; backdrop.color = dim;
            var p = new Popup { Root = root };
            p.Header = Text(root, "Level title");
            p.Title = Text(root, "Level Complete!");
            p.Big = Text(root, "+ 10");
            p.Caption = Text(root, "Your Reward!");
            p.Icon = root.transform.Find("icon-coin-reward").GetComponent<SpriteRenderer>();
            p.Border = root.transform.Find("Gold outer border").GetComponent<SpriteRenderer>();
            p.Ribbon = root.transform.Find("Gold header").GetComponent<SpriteRenderer>();
            var x2 = root.transform.Find("x2"); var next = root.transform.Find("Next");
            p.Left = x2.Find("x2").GetComponent<TextMesh>(); p.Right = next.Find("Next").GetComponent<TextMesh>();
            p.LeftButton = x2.GetComponentInChildren<VideoUiButton>(true);
            p.RightButton = next.GetComponentInChildren<VideoUiButton>(true);
            return p;
        }

        static TextMesh Text(GameObject root, string name) { return root.transform.Find(name).GetComponent<TextMesh>(); }

        static void TintRibbon(Popup p, Color color)
        {
            var ribbon = p.Root.transform.Find("bg-level-yellow"); if (ribbon) ribbon.GetComponent<SpriteRenderer>().color = color;
            p.Header.color = Color.white;
        }
        // Hide the yellow button and centre the green one.
        static void SingleButton(Popup p)
        {
            p.LeftButton.transform.parent.gameObject.SetActive(false);
            foreach (Transform child in p.RightButton.transform.parent)
                child.localPosition = new Vector3(0, child.localPosition.y, child.localPosition.z);
        }
        static T Child<T>(Popup p, string name, Vector3 local) where T : Component
        {
            var obj = new GameObject(name) { layer = p.Root.layer };
            obj.transform.SetParent(p.Root.transform, false); obj.transform.localPosition = local;
            return obj.AddComponent<T>();
        }
        TextMesh Label(Popup p, string name, Vector3 local, string text, float size, Color color)
        {
            var label = Child<TextMesh>(p, name, local);
            label.text = text; label.characterSize = size; label.fontSize = 64; label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center; label.color = color; label.fontStyle = FontStyle.Bold;
            var font = screen.Controller.InterfaceFont;
            if (font) { label.font = font; label.GetComponent<MeshRenderer>().sharedMaterial = font.material; }
            return label;
        }
        // Red square "X" in the top-right corner of the frame.
        void AddCloseButton(Popup p, string action)
        {
            var border = p.Border;
            var corner = new Vector3(border.bounds.max.x - .12f, border.bounds.max.y - .12f, 0);
            var local = p.Root.transform.InverseTransformPoint(corner); local.z = -8.5f;
            var panel = Child<SpriteRenderer>(p, "Close button", local);
            panel.sprite = p.Ribbon.sprite; panel.color = new Color(.92f, .16f, .2f);
            float s = .5f / Mathf.Max(.01f, panel.sprite.bounds.size.y);
            panel.transform.localScale = new Vector3(s * panel.sprite.bounds.size.y / Mathf.Max(.01f, panel.sprite.bounds.size.x), s, 1);
            Label(p, "Close X", local + new Vector3(0, 0, -.2f), "X", .06f, Color.white);
            var hit = Child<BoxCollider>(p, "Hit area - " + action, local + new Vector3(0, 0, -.5f));
            hit.gameObject.layer = 9; hit.size = new Vector3(.6f, .6f, .2f);
            hit.gameObject.AddComponent<VideoUiButton>().Action = action;
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
            for (int i = 0; i < hearts.Length; i++)
                hearts[i].color = i < lives ? Color.white : new Color(.32f, .18f, .62f);
            bool show = justLost && lives < hearts.Length;
            lostHeartLabel.gameObject.SetActive(show);
            if (show) { var pos = hearts[lives].transform.localPosition; lostHeartLabel.transform.localPosition = new Vector3(pos.x, pos.y, -7.4f); }
        }
        string TimerText { get { return LivesManager.IsFull ? "Đầy tim" : "Hồi tim: " + LivesManager.Countdown(LivesManager.UntilNext); } }

        // Original HARD LEVEL / BONUS LEVEL popups share one layout (Resources/HardLevel, Resources/BonusLevel):
        // crest on top (winged skull / coin cup), ribbon with the title art, level pill, mascot, one line of text
        // and a green PLAY button. Only the template's backdrop and coin bar are kept.
        void BuildHardPopup()
        {
            hard = BuildLevelKindPopup("Hard level popup", "HardLevel/", "bg-popUp-hardLevel-v2", "icon-hardLevel-popUp-v2", "icon-cup-hardLevel-v2",
                "ribbon-red-v2", "text-hardLevel-v2", "bg-level-hardLevel-v2", "Thử thách: vượt qua màn này\nngay lần đầu!", "hard-play", out hardLevelLabel);
            bonus = BuildLevelKindPopup("Bonus level popup", "BonusLevel/", "bg-popUp-easyLevel-v2", "icon-easyLevel-popUp-v2", "icon-cup-easyLevel-v2",
                "ribbon-green-v2", "bonusLevel", "bg-level-easyLevel-v2", "Thắng màn này để nhận\nthêm xu!", "hard-play", out bonusLevelLabel);
            // The bonus mascot holds a stack of coins: smaller and higher so the caption stays clear.
            var bonusMascot = bonus.Root.transform.Find("icon-easyLevel-popUp-v2");
            bonusMascot.localScale *= .78f; bonusMascot.localPosition += new Vector3(0, .18f, 0);
            bonus.Root.transform.Find("Bonus level popup caption").localPosition += new Vector3(0, -.32f, 0);
        }
        Popup BuildLevelKindPopup(string name, string folder, string frame, string mascot, string crest, string ribbon, string title, string pill,
            string caption, string action, out TextMesh levelLabel)
        {
            var p = Build(name);
            var keep = new[] { "Dark backdrop", "bg-currency", "button-plus_0", "icon-coin", "2040", "Next" };
            foreach (Transform child in p.Root.transform)
                if (System.Array.IndexOf(keep, child.name) < 0 && !child.name.StartsWith("Hit area")) child.gameObject.SetActive(false);
            Art(p, folder + frame, new Vector3(0, .1f, -6.2f), 4.3f);
            Art(p, folder + mascot, new Vector3(0, .42f, -6.5f), 2.75f);
            Art(p, folder + crest, new Vector3(0, 3.05f, -6.55f), 3.1f);
            Art(p, folder + ribbon, new Vector3(0, 2.2f, -6.6f), 4.9f);
            Art(p, folder + title, new Vector3(0, 2.28f, -6.7f), 3.45f);
            Art(p, folder + pill, new Vector3(0, 1.55f, -6.7f), 1.95f);
            levelLabel = Label(p, name + " number", new Vector3(0, 1.57f, -6.8f), "Level 1", .042f, Color.white);
            Label(p, name + " caption", new Vector3(0, -.62f, -6.8f), caption, .036f, new Color(.16f, .17f, .32f));
            foreach (Transform child in p.RightButton.transform.parent)
                child.localPosition = new Vector3(0, -1.55f, child.localPosition.z);
            p.RightButton.transform.parent.localScale = new Vector3(1.25f, 1.25f, 1);
            p.RightButton.transform.parent.localPosition = new Vector3(0, .35f, 0);
            Configure(p.RightButton, p.Right, action, "PLAY");
            return p;
        }        // Original NEW ART STYLE popup (current build, level 256): purple frame and ribbon, the art-style name,
        // the finished picture of the level about to be played and a green NEXT button under the frame.
        void BuildArtStylePopup()
        {
            artStyle = Build("Art style popup");
            var keep = new[] { "Dark backdrop", "bg-currency", "button-plus_0", "icon-coin", "2040", "Next" };
            foreach (Transform child in artStyle.Root.transform)
                if (System.Array.IndexOf(keep, child.name) < 0 && !child.name.StartsWith("Hit area")) child.gameObject.SetActive(false);
            Art(artStyle, "ArtStyle/bg-popUp-newArtStyle-v2", new Vector3(0, .55f, -6.2f), 3.9f);
            // White rounded slot framing the 84x112 picture (stretched from the feature-slot art).
            var slot = Art(artStyle, "ArtStyle/bg-popUp-slot-newFeature-v2", new Vector3(0, .03f, -6.4f), 3.15f);
            if (slot.sprite) slot.transform.localScale = new Vector3(3.15f / slot.sprite.bounds.size.x, 4.1f / slot.sprite.bounds.size.y, 1);
            artStylePicture = Child<SpriteRenderer>(artStyle, "Art style picture", new Vector3(0, .03f, -6.5f));
            Art(artStyle, "ArtStyle/ribbon-purple-v2", new Vector3(0, 3.05f, -6.6f), 4.3f);
            Art(artStyle, "ArtStyle/text-newArtStyle-v2", new Vector3(0, 3.12f, -6.7f), 2.8f);
            artStyleLabel = Label(artStyle, "Art style name", new Vector3(0, 2.33f, -6.8f), "", .05f, new Color(.62f, .2f, .82f));
            foreach (Transform child in artStyle.RightButton.transform.parent)
                child.localPosition = new Vector3(0, -2.94f, child.localPosition.z);
            artStyle.RightButton.transform.parent.localScale = new Vector3(1.1f, 1.1f, 1);
            Configure(artStyle.RightButton, artStyle.Right, "art-style-next", "NEXT");
        }
        // Shown the first time a level of a new art style (collection) is played.
        bool TryShowArtStyle(int level)
        {
            if (!screen.Levels || level <= 1) return false;
            string id = Campaign.Levels[(level - 1) % Campaign.Levels.Length], previous = Campaign.Levels[(level - 2) % Campaign.Levels.Length];
            string category = Collections.CategoryOf(id);
            if (category == null || string.Equals(category, Collections.CategoryOf(previous), System.StringComparison.OrdinalIgnoreCase)) return false;
            string key = "art:" + category.ToLowerInvariant();
            if (SaveManager.HasSeenIntro(key)) return false;
            SaveManager.MarkIntroSeen(key);
            artStyleLabel.text = Collections.DisplayName(category);
            artStylePicture.sprite = Resources.Load<Sprite>("LevelRenders/" + System.Text.RegularExpressions.Regex.Replace(id, "_Dupe$", ""));
            if (artStylePicture.sprite)
            {
                // Fit the 84x112 render inside the white slot.
                var size = artStylePicture.sprite.bounds.size;
                float k = Mathf.Min(2.85f / size.x, 3.8f / size.y);
                artStylePicture.transform.localScale = new Vector3(k, k, 1);
            }
            Show(artStyle);
            return true;
        }
        SpriteRenderer Art(Popup p, string sprite, Vector3 local, float width)
        {
            var r = Child<SpriteRenderer>(p, System.IO.Path.GetFileName(sprite), local);
            r.sprite = Resources.Load<Sprite>(sprite.Contains("/") ? sprite : "HardLevel/" + sprite);
            if (r.sprite) { float s = width / r.sprite.bounds.size.x; r.transform.localScale = new Vector3(s, s, 1); }
            return r;
        }
        void ShowHard(int level)
        {
            hardLevelLabel.text = "Level " + level;
            Show(hard);
        }

        void ShowFailed(bool justLost, bool noLives)
        {
            failedIsNoLives = noLives;
            failed.Title.text = noLives ? "Hết tim!" : "Thua rồi!";
            Configure(null, failed.Caption, null, noLives || !LivesManager.CanPlay ? "Bạn đã hết tim!" : "Cố lên!", 18f);
            failed.Big.text = TimerText;
            RefreshHearts(justLost);
            bool canPlay = LivesManager.CanPlay;
            // Original no-lives popup: "Refill 600" buys a full set of hearts; ✕ goes home.
            if (!canPlay) Configure(failed.RightButton, failed.Right, "refill-lives", "Hồi tim · " + EconomyManager.RefillLivesPrice, 7f);
            else Configure(failed.RightButton, failed.Right, !noLives ? "retry" : "close-failed", !noLives ? "Chơi lại" : "Đóng");
            Show(failed);
        }

        // Home PLAY with no hearts left: the big NO MORE LIVES popup (original canvas_nolives).
        public void ShowNoLives() { ShowNoLivesPopup(); }

        // Original "OUT OF SPACE!" banner: a red band across the board for about a second before the popup.
        GameObject banner; float bannerShownAt = -1;
        const float BannerTime = 1.1f;
        void BuildOutOfSpaceBanner()
        {
            banner = new GameObject("Out of space banner") { layer = screen.Result.layer };
            banner.transform.SetParent(screen.Result.transform.parent, false);
            banner.transform.localPosition = screen.Result.transform.localPosition;
            var p = new Popup { Root = banner };
            var white = Texture2D.whiteTexture;
            var sprite = Sprite.Create(white, new Rect(0, 0, white.width, white.height), new Vector2(.5f, .5f), white.width);
            runtimeArt.Add(sprite);
            foreach (var part in new[] { new { y = .38f, h = .95f, z = -6.2f, c = new Color(.9f, .16f, .26f, .88f) },
                                         new { y = .38f + .5f, h = .07f, z = -6.25f, c = new Color(1f, .5f, .55f, .95f) },
                                         new { y = .38f - .5f, h = .07f, z = -6.25f, c = new Color(1f, .5f, .55f, .95f) } })
            {
                var r = Child<SpriteRenderer>(p, "Band", new Vector3(0, part.y, part.z));
                r.sprite = sprite; r.color = part.c; r.transform.localScale = new Vector3(8f, part.h, 1);
            }
            SetText(OutlinedLabel(p, "Out of space text", new Vector3(0, .38f, -6.4f), .07f), "HẾT CHỖ!");
            banner.SetActive(false);
        }
        void UpdateOutOfSpaceBanner()
        {
            if (!screen.Feedback.FailureVisible || gaveUp)
            {
                if (bannerShownAt >= 0 && !screen.Feedback.FailureVisible) bannerShownAt = -1;
                banner.SetActive(false); return;
            }
            if (bannerShownAt < 0) bannerShownAt = Time.unscaledTime;
            float t = Time.unscaledTime - bannerShownAt;
            if (t >= BannerTime) { banner.SetActive(false); Show(lose); return; }
            banner.SetActive(true);
            // Slides open horizontally, then holds.
            banner.transform.localScale = new Vector3(Mathf.SmoothStep(0, 1, t / .18f), 1, 1);
        }

        // Original CONGRATS card (current build): yellow ribbon with light rays, gold-framed glass card, the gallery
        // collection name, its cover revealed in colour from the bottom up as levels are won, "N% Completed" and
        // "Tap To Continue" anywhere on screen.
        const float CoverWidth = 2.5f, CoverHeight = 3.15f, CoverY = .17f;
        void BuildCongratsCard()
        {
            congrats = Build("Congrats popup");
            var keep = new[] { "Dark backdrop", "bg-currency", "button-plus_0", "icon-coin", "2040" };
            foreach (Transform child in congrats.Root.transform)
                if (System.Array.IndexOf(keep, child.name) < 0) child.gameObject.SetActive(false);
            // The original card sits on an almost black screen.
            var backdrop = congrats.Root.transform.Find("Dark backdrop").GetComponent<SpriteRenderer>();
            var dim = backdrop.color; dim.a = .94f; backdrop.color = dim;
            congratsRays = Art(congrats, "Congrats/light-congrats", new Vector3(0, 3.1f, -6.1f), 4.2f).transform;
            Art(congrats, "Congrats/bg-popUp-congrats-v2", new Vector3(0, .06f, -6.2f), 3.86f);
            cardBw = Child<SpriteRenderer>(congrats, "Cover grey", new Vector3(0, CoverY, -6.4f));
            cardColor = Child<SpriteRenderer>(congrats, "Cover colour", new Vector3(0, CoverY, -6.45f));
            Art(congrats, "Congrats/ribbon-yellow-v2", new Vector3(0, 2.74f, -6.6f), 4.95f);
            Art(congrats, "Congrats/text-congrats-v2", new Vector3(0, 2.8f, -6.7f), 3.3f);
            cardName = OutlinedLabel(congrats, "Collection name", new Vector3(0, 2.04f, -6.8f), .042f);
            cardPercent = OutlinedLabel(congrats, "Collection percent", new Vector3(0, -1.63f, -6.8f), .038f);
            SetText(OutlinedLabel(congrats, "Tap to continue", new Vector3(0, -3.48f, -6.8f), .042f), "Chạm để tiếp tục");
            // The whole screen continues, in front of everything else in the popup.
            var hit = Child<BoxCollider>(congrats, "Hit area - congrats-continue", new Vector3(0, 0, -9.5f));
            hit.gameObject.layer = 9; hit.size = new Vector3(30, 40, .2f);
            hit.gameObject.AddComponent<VideoUiButton>().Action = "congrats-continue";
        }
        // White caption with a dark purple outline (four offset copies behind it). Returns [label, outlines...].
        TextMesh[] OutlinedLabel(Popup p, string name, Vector3 local, float size)
        {
            var main = Label(p, name, local, "", size, Color.white);
            var all = new System.Collections.Generic.List<TextMesh> { main };
            foreach (var o in new[] { new Vector2(1, 1), new Vector2(-1, 1), new Vector2(1, -1), new Vector2(-1, -1) })
            {
                var shadow = Instantiate(main.gameObject, main.transform);
                shadow.name = "Text outline";
                shadow.transform.localPosition = new Vector3(o.x, o.y, 0) * size * .55f + new Vector3(0, 0, .05f);
                shadow.transform.localScale = Vector3.one;
                var t = shadow.GetComponent<TextMesh>(); t.color = new Color(.23f, .16f, .42f);
                all.Add(t);
            }
            return all.ToArray();
        }
        static void SetText(TextMesh[] labels, string text) { foreach (var t in labels) if (t) t.text = text; }

        // After a win: the gallery card of the collection this level belongs to (none for the tutorial levels).
        public bool ShowCongrats()
        {
            if (!screen.Levels) return false;
            int level = screen.Levels.PlayedNumber;
            var info = GalleryCollections.ForLevel(level);
            if (info == null) return false;
            Sprite colour, grey;
            if (info.Cover != null) { colour = Resources.Load<Sprite>(info.Cover); grey = Resources.Load<Sprite>(info.Cover + "-bw"); }
            else { colour = Resources.Load<Sprite>("LevelRenders/" + info.RenderId); grey = Greyscale(colour); }
            if (!colour || !grey) return false;
            SetText(cardName, info.Name);
            SetText(cardPercent, info.Percent(level) + "% hoàn thành");
            var size = grey.bounds.size;
            float k = Mathf.Min(CoverWidth / size.x, CoverHeight / size.y);
            cardBw.sprite = grey; cardBw.transform.localScale = new Vector3(k, k, 1);
            // Colour slice from the bottom up, proportional to the levels won in this collection.
            float fraction = (float)info.Completed(level) / info.Count;
            var rect = colour.textureRect;
            cardColor.gameObject.SetActive(fraction > 0);
            if (fraction > 0)
            {
                var slice = Sprite.Create(colour.texture, new Rect(rect.x, rect.y, rect.width, Mathf.Max(1, Mathf.Round(rect.height * fraction))), new Vector2(.5f, 0), colour.pixelsPerUnit);
                runtimeArt.Add(slice);
                cardColor.sprite = slice;
                cardColor.transform.localScale = new Vector3(k * colour.bounds.size.x / size.x, k * colour.bounds.size.y / size.y, 1);
                cardColor.transform.localPosition = new Vector3(0, CoverY - k * size.y / 2, cardColor.transform.localPosition.z);
            }
            // The card replaces the Level Complete page (same depth); the next level loads right after it.
            screen.Result.SetActive(false);
            Show(congrats);
            return true;
        }

        // Grey copy of a level render (collections without a configured black-and-white cover).
        Sprite Greyscale(Sprite source)
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
        void OnDestroy() { foreach (var o in runtimeArt) if (o) Destroy(o); }
        static void FitIcon(SpriteRenderer icon, float height)
        {
            if (!icon.sprite) return;
            float s = height / icon.sprite.bounds.size.y;
            icon.transform.localScale = new Vector3(s, s, 1);
        }
        // Scales a label together with its outline copies.
        static void ScaleText(TextMesh label, float factor)
        {
            foreach (var t in label.GetComponentsInChildren<TextMesh>(true)) t.characterSize *= factor;
        }
        readonly System.Collections.Generic.Dictionary<TextMesh, float> baseSizes = new System.Collections.Generic.Dictionary<TextMesh, float>();
        void Configure(VideoUiButton button, TextMesh label, string action, string text, float fitChars = 5.5f)
        {
            if (button) button.Action = action;
            // Long labels must fit their panel width; outline/shadow copies follow the label.
            float factor = Mathf.Clamp(fitChars / Mathf.Max(fitChars, text.Length), .5f, 1f);
            foreach (var t in label.GetComponentsInChildren<TextMesh>(true))
            {
                float size;
                if (!baseSizes.TryGetValue(t, out size)) baseSizes[t] = size = t.characterSize;
                t.text = text; t.characterSize = size * factor;
            }
        }
        int LevelShown { get { return screen.Levels ? screen.Levels.PlayedNumber : Campaign.LevelNumber; } }

        void Show(Popup p)
        {
            if (Open) Open.SetActive(false);
            Open = p.Root; shownAt = Time.unscaledTime;
            p.Header.text = "Level " + LevelShown;
            foreach (var label in p.Root.GetComponentsInChildren<TextMesh>(true))
                if (label.name == "2040") label.text = Campaign.Coins.ToString();
            p.Root.SetActive(true);
            p.Root.transform.localScale = Vector3.one * .85f;
            if (screen.Controller) screen.Controller.BoosterInputBlocked = true;
        }

        public void Close()
        {
            if (!Open) return;
            Open.SetActive(false); Open = null;
            if (screen.Controller) { screen.Controller.BoosterInputBlocked = false; screen.Controller.SuppressInputThisFrame(); }
            ShowNextQueued();
        }
        // Level-start sequence: NEW ART STYLE → HARD LEVEL → feature / booster intros.
        void ShowNextQueued()
        {
            if (pendingHardLevel > 0) { int level = pendingHardLevel; pendingHardLevel = 0; ShowHard(level); return; }
            if (pendingBonusLevel > 0) { int level = pendingBonusLevel; pendingBonusLevel = 0; bonusLevelLabel.text = "Level " + level; Show(bonus); return; }
            if (pendingIntros.Count > 0) ShowIntro(pendingIntros.Dequeue());
        }

        // Original flow: the first time a level introduces a mechanic or booster, a popup plays its tutorial gif.
        public void ShowIntrosFor(int level)
        {
            pendingIntros.Clear(); boosterIntros.Clear(); pendingHardLevel = 0; pendingBonusLevel = 0;
            foreach (var f in OriginalConfig.Features) if (f.showLevel == level) Queue(f, false);
            foreach (var b in OriginalConfig.Boosters) if (b.showLevel == level) Queue(b, true);
            // Hard levels greet the player with the HARD LEVEL popup; feature intros follow on close.
            if (OriginalConfig.IsHard(level)) pendingHardLevel = level;
            if (OriginalConfig.IsEasy(level)) pendingBonusLevel = level; // EasyHardLevelConfig "easy" = BONUS LEVEL
            if (Open) return;
            if (!TryShowArtStyle(level)) ShowNextQueued();
        }
        void Queue(OriginalConfig.Intro entry, bool booster)
        {
            if (string.IsNullOrEmpty(entry.gif) || SaveManager.HasSeenIntro(entry.gif)) return;
            if (booster) boosterIntros.Add(entry);
            pendingIntros.Enqueue(entry);
        }
        void ShowIntro(OriginalConfig.Intro entry)
        {
            SaveManager.MarkIntroSeen(entry.gif);
            // Original: a newly unlocked booster arrives with one free use (HUD badge "1").
            if (boosterIntros.Contains(entry)) EconomyManager.AddBooster(entry.type == 1 ? "rocket" : entry.type == 2 ? "swap" : "select", 1, "unlock-gift");
            introGif = Resources.Load<SpriteSequence>("OriginalGifs/" + entry.gif);
            intro.Title.text = boosterIntros.Contains(entry) ? "Booster mới!" : "Tính năng mới!";
            string caption; if (!Captions.TryGetValue(entry.info, out caption)) caption = entry.info;
            Configure(null, intro.Caption, null, caption, 22f);
            intro.Icon.sprite = introGif ? introGif.FrameAt(0) : null;
            FitIcon(intro.Icon, 1.9f);
            Show(intro);
        }

        public bool Owns(VideoUiButton button) { return Open && button && button.transform.IsChildOf(Open.transform); }

        // Returns true when the popup consumed the action.
        public bool Handle(string action)
        {
            if (!Open || Time.unscaledTime - shownAt < .15f) return Open;
            if (action.StartsWith("quest-")) return HandleSandQuest(action);
            if (action.StartsWith("shop-")) return HandleShop(action);
            if (action.StartsWith("daily-") || action.StartsWith("tasks-")) return HandlePanels(action);
            if (action.StartsWith("gallery-")) return HandleGallery(action);
            switch (action)
            {
                case "close": Close(); return true;
                case "toggle-sound":
                    Campaign.SoundOn = !Campaign.SoundOn;
                    RefreshSettings(); return true;
                case "toggle-haptics":
                    SaveManager.Data.hapticsOn = !SaveManager.Data.hapticsOn; SaveManager.Save();
                    RefreshSettings(); return true;
                case "retry":
                    // In a level the current build keeps the Level Failed card ("You Have No Lives!" + Refill).
                    if (!LivesManager.CanPlay) { ShowFailed(false, true); return true; }
                    Close(); screen.Feedback.Retry(); return true;
                case "home": Close(); gaveUp = false; screen.Feedback.GoHome(); return true;
                case "play-on":
                    if (!EconomyManager.TrySpend(PlayOnPrice, "play-on")) { Configure(null, lose.Caption, null, "Không đủ xu · cần " + PlayOnPrice, 18f); return true; }
                    if (!screen.Controller.AddExtraSlot()) { EconomyManager.Add(PlayOnPrice, "refund"); return true; }
                    lose.Caption.text = "Thêm 1 ô chờ";
                    Close(); return true;
                case "give-up":
                    // Giving up the level costs a heart (original: Out of Space ✕ → Level Failed with -1).
                    gaveUp = true;
                    LivesManager.LoseLife();
                    if (screen.Levels) GameEvents.RaiseLevelLost(screen.Levels.PlayedNumber);
                    ShowFailed(true, false); return true;
                case "close-failed":
                    Close();
                    if (!failedIsNoLives) { gaveUp = false; screen.Feedback.GoHome(); }
                    return true;
                case "refill-lives":
                    if (!EconomyManager.TrySpend(EconomyManager.RefillLivesPrice, "refill-lives"))
                    {
                        string need = "Không đủ xu · cần " + EconomyManager.RefillLivesPrice;
                        if (Open == noLives.Root) SetText(noLivesInfo, need); else Configure(null, failed.Caption, null, need, 18f);
                        return true;
                    }
                    LivesManager.Refill();
                    Close();
                    if (screen.Current == VideoScreen.Page.Gameplay) { gaveUp = false; screen.Feedback.Retry(); }
                    return true;
                case "hard-play": Close(); return true;
                case "art-style-next": Close(); return true;
                case "congrats-continue": Close(); if (screen.Levels) screen.Levels.LoadNext(); return true;
            }
            return true;
        }

        void Update()
        {
            if (!screen.Feedback.FailureVisible) gaveUp = false;
            if (!Open) { UpdateOutOfSpaceBanner(); return; }
            if (Open == failed.Root && Time.frameCount % 15 == 0) failed.Big.text = TimerText;
            // Pop-in scale like the reference popups.
            float t = Mathf.Clamp01((Time.unscaledTime - shownAt) / .22f);
            float s = .85f + .15f * (1 + 2.2f * Mathf.Pow(t - 1, 3) + 1.2f * Mathf.Pow(t - 1, 2));
            Open.transform.localScale = Vector3.one * s;
            if (Open == intro.Root && introGif) intro.Icon.sprite = introGif.FrameAt(Time.unscaledTime - shownAt);
            if (Open == congrats.Root && congratsRays) congratsRays.localRotation = Quaternion.Euler(0, 0, Time.unscaledTime * 12f);
            UpdateSandQuest();
            UpdateShop();
            UpdateNoLives();
            UpdatePanels();
            if ((Open == lose.Root || (Open == failed.Root && !failedIsNoLives)) && !screen.Feedback.FailureVisible) Close();
            if (Input.GetKeyDown(KeyCode.Escape) && Open == settings.Root) Close();
        }
    }
}
