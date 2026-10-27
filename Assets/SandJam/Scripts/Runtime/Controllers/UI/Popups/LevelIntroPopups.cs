using System.Collections.Generic;
using UnityEngine;

namespace SandJamTest.Scene3D
{
    // Level-start popups, shown in this order when a level begins: NEW ART STYLE (first level of a collection),
    // HARD LEVEL / BONUS LEVEL, then a NEW FEATURE / NEW BOOSTER intro with its tutorial gif for each mechanic or
    // booster the level introduces (once per save).
    public sealed class LevelIntroPopups : PopupModule
    {
        Popup intro, hard, bonus, artStyle;
        TextMesh hardLevelLabel, bonusLevelLabel, artStyleLabel;
        SpriteRenderer artStylePicture;
        int pendingHardLevel, pendingBonusLevel;
        SpriteSequence introGif;
        readonly Queue<OriginalConfig.Intro> pendingIntros = new Queue<OriginalConfig.Intro>();
        readonly HashSet<OriginalConfig.Intro> boosterIntros = new HashSet<OriginalConfig.Intro>();

        // The HARD / BONUS level cards grow more slowly than other popups (recording of level 260).
        public bool SlowPopIn { get { return IsOpen(hard) || IsOpen(bonus); } }

        public override void Build()
        {
            intro = Build("Feature intro popup");
            PopupScreens.HideCoinBar(intro); // dimmed with the HUD, as on every original card over the board
            intro.Big.text = "";
            intro.LeftButton.transform.parent.gameObject.SetActive(false);
            foreach (Transform child in intro.RightButton.transform.parent)
                child.localPosition = new Vector3(0, child.localPosition.y, child.localPosition.z);
            Configure(intro.RightButton, intro.Right, "close", "Continue");
            BuildHardPopup();
            BuildArtStylePopup();
        }
        public override bool Handles(string action) { return action == "hard-play" || action == "art-style-next"; }
        public override bool Handle(string action) { Close(); return true; }
        public override void Tick() { if (IsOpen(intro) && introGif) intro.Icon.sprite = introGif.FrameAt(Time.unscaledTime - ShownAt); }

        // Original HARD LEVEL / BONUS LEVEL popups share one layout (Resources/HardLevel, Resources/BonusLevel):
        // crest on top (winged skull / coin cup), ribbon with the title art, level pill, mascot, one line of text
        // and a green PLAY button. Only the template's backdrop and coin bar are kept.
        void BuildHardPopup()
        {
            hard = BuildLevelKindPopup("Hard level popup", "HardLevel/", "bg-popUp-hardLevel-v2", "icon-hardLevel-popUp-v2", "icon-cup-hardLevel-v2",
                "ribbon-red-v2", "text-hardLevel-v2", "bg-level-hardLevel-v2", "Challenge yourself to beat\nthis level at once!", "hard-play", out hardLevelLabel);
            bonus = BuildLevelKindPopup("Bonus level popup", "BonusLevel/", "bg-popUp-easyLevel-v2", "icon-easyLevel-popUp-v2", "icon-cup-easyLevel-v2",
                "ribbon-green-v2", "bonusLevel", "bg-level-easyLevel-v2", "Beat this level to earn\nextra coins!", "hard-play", out bonusLevelLabel);
            // The bonus mascot holds a stack of coins: smaller and higher so the caption stays clear.
            var bonusMascot = bonus.Root.transform.Find("icon-easyLevel-popUp-v2");
            bonusMascot.localScale *= .74f; bonusMascot.localPosition += new Vector3(0, .42f, 0);
            bonus.Root.transform.Find("Bonus level popup caption").localPosition += new Vector3(0, .12f, 0);
        }
        Popup BuildLevelKindPopup(string name, string folder, string frame, string mascot, string crest, string ribbon, string title, string pill,
            string caption, string action, out TextMesh levelLabel)
        {
            var p = Blank(name, true);
            p.Root.transform.Find("Dark backdrop").GetComponent<SpriteRenderer>().color = PopupScreens.DeepDim;
            PopupScreens.HideCoinBar(p); // the HUD counter stays dimmed under the layer, as in the original
            // Measured on the recording (level 260): the frame reaches lower, the mascot and caption are larger and
            // lower, the PLAY button is bigger and sits on the frame's bottom edge, and a glow of rays shines behind
            // the crest (red for hard, gold for bonus).
            var frameArt = Art(p, folder + frame, new Vector3(0, -.21f, -6.2f), 4.3f);
            frameArt.transform.localScale = new Vector3(frameArt.transform.localScale.x, frameArt.transform.localScale.y * 1.17f, 1);
            var rays = Art(p, "Congrats/light-congrats", new Vector3(0, 3.35f, -6.15f), 5.6f); // behind the frame, around the crest
            rays.color = name.StartsWith("Hard", System.StringComparison.Ordinal) ? new Color(1f, .15f, .15f, .75f) : new Color(1f, .85f, .3f, .7f);
            Art(p, folder + mascot, new Vector3(0, -.25f, -6.5f), 3.2f);
            Art(p, folder + crest, new Vector3(0, 3.05f, -6.55f), 3.1f);
            Art(p, folder + ribbon, new Vector3(0, 2.2f, -6.6f), 4.9f);
            Art(p, folder + title, new Vector3(0, 2.28f, -6.7f), 3.45f);
            Art(p, folder + pill, new Vector3(0, 1.55f, -6.7f), 1.95f);
            levelLabel = Label(p, name + " number", new Vector3(0, 1.57f, -6.8f), "Level 1", .042f, Color.white);
            // Original caption: white letters with the game's dark outline (added by TextOutliner).
            Label(p, name + " caption", new Vector3(0, -1.68f, -6.8f), caption, .042f, Color.white);
            foreach (Transform child in p.RightButton.transform.parent)
                child.localPosition = new Vector3(0, -1.55f, child.localPosition.z);
            p.RightButton.transform.parent.localScale = new Vector3(1.5f, 1.5f, 1);
            p.RightButton.transform.parent.localPosition = new Vector3(0, -.38f, 0);
            Configure(p.RightButton, p.Right, action, "PLAY");
            return p;
        }        // Original NEW ART STYLE popup (current build, level 256): purple frame and ribbon, the art-style name,
        // the finished picture of the level about to be played and a green NEXT button under the frame.
        void BuildArtStylePopup()
        {
            artStyle = Blank("Art style popup", true);
            Art(artStyle, "ArtStyle/bg-popUp-newArtStyle-v2", new Vector3(0, .55f, -6.2f), 3.9f);
            // White rounded slot framing the 84x112 picture (stretched from the feature-slot art).
            var slot = Art(artStyle, "ArtStyle/bg-popUp-slot-newFeature-v2", new Vector3(0, .03f, -6.4f), 3.15f);
            slot.SetLocalSize(3.15f, 4.1f);
            artStylePicture = Child<SpriteRenderer>(artStyle, "Art style picture", new Vector3(0, -.3f, -6.5f)); // the slot art has an empty top margin
            Art(artStyle, "ArtStyle/ribbon-purple-v2", new Vector3(0, 3.05f, -6.6f), 4.3f);
            Art(artStyle, "ArtStyle/text-newArtStyle-v2", new Vector3(0, 3.12f, -6.7f), 2.8f);
            artStyleLabel = Label(artStyle, "Art style name", new Vector3(0, 2.33f, -6.8f), "", .05f, new Color(.62f, .2f, .82f));
            foreach (Transform child in artStyle.RightButton.transform.parent)
                child.localPosition = new Vector3(0, -2.94f, child.localPosition.z);
            artStyle.RightButton.transform.parent.localScale = new Vector3(1.1f, 1.1f, 1);
            Configure(artStyle.RightButton, artStyle.Right, "art-style-next", "NEXT");
            // Recording 3 (75 s): the card ~8% larger and ~60 px higher.
            PopupScreens.FitCard(artStyle, ArtStyleFit, 60);
        }
        const float ArtStyleFit = 1.08f;
        // Shown the first time a level of a new art style (collection) is played.
        bool TryShowArtStyle(int level)
        {
            if (level <= 1) return false;
            string id = Campaign.IdAt(level), previous = Campaign.IdAt(level - 1);
            string category = Collections.CategoryOf(id);
            if (category == null || string.Equals(category, Collections.CategoryOf(previous), System.StringComparison.OrdinalIgnoreCase)) return false;
            string key = "art:" + category.ToLowerInvariant();
            if (SaveManager.HasSeenIntro(key)) return false;
            SaveManager.MarkIntroSeen(key);
            // Recording 3 (75 s): the collection's name as in the Gallery ("Infinite Canvas") and its flat cover art,
            // rounded inside the slot; the level render when the collection has no cover.
            var info = GalleryCollections.ForLevel(level);
            artStyleLabel.text = info != null && info.StyleName != null ? info.StyleName : Collections.DisplayName(category);
            Sprite picture, unusedGrey;
            CoverPair(info != null ? info.StyleCover : null, Campaign.PictureId(id), out picture, out unusedGrey);
            artStylePicture.sprite = RoundedCopy(picture);
            // Recording 3: the picture fills the slot to a thin rim (FitCard scaled the card at build, so scale it too).
            artStylePicture.SetLocalSize(2.91f * ArtStyleFit, 3.64f * ArtStyleFit);
            Show(artStyle);
            return true;
        }
        void ShowHard(int level)
        {
            hardLevelLabel.text = "Level " + level;
            Show(hard);
        }

        // Level-start sequence: NEW ART STYLE → HARD LEVEL → feature / booster intros.
        // Long captions break at the space nearest the middle (one tiny line was unreadable).
        static string TwoLines(string text)
        {
            if (text.Length <= 28) return text;
            int best = -1;
            for (int i = 0; i < text.Length; i++)
                if (text[i] == ' ' && (best < 0 || Mathf.Abs(i - text.Length / 2) < Mathf.Abs(best - text.Length / 2))) best = i;
            return best < 0 ? text : text.Substring(0, best) + "\n" + text.Substring(best + 1);
        }

        public void ShowNextQueued()
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
            if (boosterIntros.Contains(entry)) EconomyManager.AddBooster(entry.type == 1 ? BoosterCatalog.Flare : entry.type == 2 ? BoosterCatalog.Swap : BoosterCatalog.Select, 1, "unlock-gift");
            introGif = Resources.Load<SpriteSequence>("OriginalGifs/" + entry.gif);
            intro.Title.text = boosterIntros.Contains(entry) ? "New Booster!" : "New Feature!";
            Configure(null, intro.Caption, null, TwoLines(entry.info), 24f);
            intro.Icon.sprite = introGif ? introGif.FrameAt(0) : null;
            Show(intro); // measured once visible (inactive renderers have no bounds)
            if (!captionBandGrown) { captionBandGrown = true; GrowCaptionBand(); }
            Host.FillRewardBox(intro, intro.Icon); // fills the picture slot, clipped to it
        }
        // Two-line captions: the band's square top strip grows upward (the rounded bottom stays as it is) and the
        // text is centred on the taller band.
        bool captionBandGrown;
        void GrowCaptionBand()
        {
            var band = intro.Root.transform.Find(ResultFx.BandName); var strip = intro.Root.transform.Find(ResultFx.BandTopName);
            var stripArt = strip ? strip.GetComponent<SpriteRenderer>() : null;
            if (!band || !stripArt) return;
            var bandBounds = band.GetComponent<Renderer>().bounds; var sb = stripArt.bounds;
            stripArt.FitWorldSize(sb.size.x, sb.size.y + bandBounds.size.y * .8f);
            strip.position += Vector3.up * (sb.min.y - stripArt.bounds.min.y);
            var at = intro.Caption.transform.position;
            intro.Caption.transform.position = new Vector3(at.x, (bandBounds.min.y + stripArt.bounds.max.y) * .5f, at.z);
        }
    }
}
