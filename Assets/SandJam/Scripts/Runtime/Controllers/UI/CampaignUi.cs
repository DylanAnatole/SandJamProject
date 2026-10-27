using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SandJamTest.Scene3D
{
    // Connects the reconstructed screens to the campaign: live level numbers and coin balance,
    // working x2 / settings buttons (the popup itself lives in PopupScreens). Presentation only.
    public sealed class CampaignUi : MonoBehaviour
    {
        VideoScreen screen;
        readonly List<TextMesh> coinLabels = new List<TextMesh>();
        TextMesh homeNumber, hudLevel, resultTitle, rewardLabel;
        GameObject doubleButton;
        TextMesh livesCount, livesStatus;
        float nextLivesRefresh;
        SpriteRenderer levelBadge;
        Color badgeColor = Color.white;
        int shownCoins = -1, shownLevel = -1;
        TextMesh[] joinLabels = new TextMesh[0];
        // Sand Quest chest while locked (original SandQuestUWidgetSlot): grey chest and plaque, padlock, "Lv. N".
        SpriteRenderer chestArt, plaqueArt; Sprite chestOpen, plaqueOpen; GameObject padlock; Vector3[] joinPlaces;
        int shownLocked = -1;
        static readonly Color LockedInk = new Color32(70, 70, 70, 255);
        bool shownDoubled;
        public bool SettingsOpen { get { return screen.Popups.Open; } }

        public void Initialize(VideoScreen owner)
        {
            screen = owner;
            foreach (var label in owner.GetComponentsInChildren<TextMesh>(true))
            {
                if (label.name == "Text outline") continue;
                if (label.text == "2040") coinLabels.Add(label);
                else if (label.name == "147" && label.transform.parent == owner.Home.transform) homeNumber = label;
                else if (label.name == "Level 147") hudLevel = label;
                else if (label.name == "Level title") resultTitle = label;
                else if (label.name == "+ 10") rewardLabel = label;
                else if (label.transform.parent == owner.Home.transform && label.name == "5") livesCount = label;
                else if (label.transform.parent == owner.Home.transform && label.name == "Full") livesStatus = label;
            }
            // Development shortcuts for the first three prototype levels are replaced by the campaign.
            foreach (Transform child in owner.Home.transform)
                if (child.name.StartsWith("LV ", System.StringComparison.Ordinal)) child.gameObject.SetActive(false);
            var x2 = owner.Result.transform.Find("x2");
            if (x2) { doubleButton = x2.gameObject; AddHit(x2, "double"); DecorateDouble(x2); }
            var hud = owner.Gameplay.transform.Find("HUD - visual controls only/Settings - UI only");
            if (hud) AddHit(hud, "settings");
            var badge = owner.Gameplay.transform.Find("HUD - visual controls only/Level badge");
            if (badge) { levelBadge = badge.GetComponentInChildren<SpriteRenderer>(true); if (levelBadge) badgeColor = levelBadge.color; }
            coinBar = owner.Gameplay.GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault(r => r.name == "Coin bar");
            if (coinBar) { coinColor = coinBar.color; GoldRim(coinBar); }
            // Home lives / coin bars and the coin bar of the result card (cloned into every popup) have it too.
            foreach (var root in new[] { owner.Home, owner.Result })
                foreach (var bar in root.GetComponentsInChildren<SpriteRenderer>(true))
                    if (bar.name == "bg-currency") GoldRim(bar);
            // Sand Quest chest ("JOIN"): opens the event; shows the run progress while joined.
            var chest = owner.Home.transform.Find("Chest_Enable");
            if (chest) AddHit(chest, "sand-quest");
            joinLabels = owner.Home.GetComponentsInChildren<TextMesh>(true).Where(t => t.text == "JOIN").ToArray();
            BuildQuestLock(chest, owner.Home.transform.Find("Join label"));
            // Navbar SHOP opens the Sand Shop page.
            var shopIcon = owner.Home.transform.Find("icon-shop");
            if (shopIcon) AddHit(shopIcon, "shop");
            var galleryIcon = owner.Home.transform.Find("icon-cards");
            if (galleryIcon) { AddHit(galleryIcon, "gallery"); galleryBadge = AlertBadge(galleryIcon); }
            var homeGear = owner.Home.transform.Find("icon-settings-v2");
            if (homeGear) AddHit(homeGear, "settings");
            Campaign.Changed += Refresh;
            // Observer: the badge reacts to the events that can change it (a won level reaches a milestone, a claim
            // collects one) instead of polling.
            GameEvents.GalleryChanged += RefreshBadge; GameEvents.LevelWon += OnLevelWon;
            RefreshBadge();
            Refresh();
        }
        void OnDestroy() { Campaign.Changed -= Refresh; GameEvents.GalleryChanged -= RefreshBadge; GameEvents.LevelWon -= OnLevelWon; }
        void OnLevelWon(int level) { RefreshBadge(); }
        void RefreshBadge() { if (galleryBadge) galleryBadge.SetActive(GalleryCollections.HasUnclaimedReward()); }

        // Original x2 button: a coin stack on the left of "X2" and the rewarded-video TV badge on its top-left corner.
        static void DecorateDouble(Transform x2)
        {
            var body = x2.GetComponentsInChildren<SpriteRenderer>(true).OrderByDescending(r => r.bounds.size.x * r.bounds.size.y).FirstOrDefault();
            if (!body) return;
            var b = body.bounds; float h = b.size.y;
            var label = x2.GetComponentsInChildren<TextMesh>(true).FirstOrDefault(t => t.name != "Text outline");
            float z = (label ? label.transform.position.z : b.center.z) - .02f;
            // Original: a small coin stack inside the button's left half, "X2" beside it (the larger stack stuck out
            // past the rounded left end).
            float coinHeight = h * .5f, coinX = b.min.x + h * .38f + coinHeight * .7f;
            if (label) { label.text = "X2"; label.transform.position += Vector3.right * b.size.x * .1f; }
            Icon(x2, "VideoUI/icon-coin-reward", new Vector3(coinX, b.center.y + h * .02f, z), coinHeight, body.sortingOrder + 1, 0);
            Icon(x2, "VideoUI/iconRW", new Vector3(b.min.x + h * .1f, b.max.y - h * .02f, z - .02f), h * .62f, body.sortingOrder + 2, 12);
        }
        static void Icon(Transform parent, string sprite, Vector3 position, float height, int order, float angle)
        {
            var art = Resources.Load<Sprite>(sprite);
            if (!art) return;
            var go = new GameObject(System.IO.Path.GetFileName(sprite)) { layer = parent.gameObject.layer };
            go.transform.SetParent(parent, false);
            go.transform.position = position; go.transform.rotation = Quaternion.Euler(0, 0, angle);
            var r = go.AddComponent<SpriteRenderer>(); r.sprite = art; r.sortingOrder = order;
            float s = height / art.bounds.size.y / Mathf.Max(.0001f, parent.lossyScale.y);
            go.transform.localScale = new Vector3(s, s, 1);
        }

        // Original gameplay HUD: the coin bar has a thin gold rim (every gameplay frame of both recordings).
        static void GoldRim(SpriteRenderer bar)
        {
            var rim = new GameObject("Coin bar gold rim") { layer = bar.gameObject.layer }.AddComponent<SpriteRenderer>();
            rim.transform.SetParent(bar.transform.parent, false);
            // White rounded panel tinted gold (tinting the bar's own light-blue art came out olive).
            rim.sprite = SpriteFactory.Rounded(48, 16, 100); rim.drawMode = SpriteDrawMode.Sliced;
            rim.sharedMaterial = SpriteFactory.PlainSprite;
            rim.sortingLayerID = bar.sortingLayerID; rim.sortingOrder = bar.sortingOrder - 1;
            rim.color = new Color(1f, .76f, .12f);
            var b = bar.bounds; const float grow = .03f;
            rim.transform.position = new Vector3(b.center.x, b.center.y, bar.transform.position.z + .01f);
            var lossy = rim.transform.lossyScale;
            rim.size = new Vector2((b.size.x + 2 * grow) / lossy.x, (b.size.y + 2 * grow) / lossy.y);
        }
        GameObject galleryBadge;
        // Original alert badge: red disc with a white ring and a white "!", at the icon's top-right corner.
        static GameObject AlertBadge(Transform icon)
        {
            var renderer = icon.GetComponentInChildren<SpriteRenderer>(true); if (!renderer) return null;
            var b = renderer.bounds;
            var root = new GameObject("Alert badge") { layer = icon.gameObject.layer }.transform;
            root.SetParent(icon.parent, true);
            root.position = new Vector3(b.max.x + b.size.x * .19f, b.max.y - b.size.y * .26f, icon.position.z - .3f); // measured on the original navbar
            var disc = SpriteFactory.Disc;
            var ring = new GameObject("Ring") { layer = root.gameObject.layer }.AddComponent<SpriteRenderer>();
            ring.transform.SetParent(root, false); ring.sprite = disc; ring.color = Color.white; ring.sortingOrder = renderer.sortingOrder; // in front of the icon by depth, under popup layers
            var fill = new GameObject("Fill") { layer = root.gameObject.layer }.AddComponent<SpriteRenderer>();
            fill.transform.SetParent(root, false); fill.transform.localPosition = new Vector3(0, 0, -.01f); fill.sprite = disc; fill.color = new Color(.92f, .2f, .3f); fill.sortingOrder = renderer.sortingOrder;
            float d = b.size.y * .58f, s = root.lossyScale.x;
            ring.transform.localScale = Vector3.one * d / s; fill.transform.localScale = Vector3.one * d * .84f / s;
            var mark = new GameObject("Mark") { layer = root.gameObject.layer }.AddComponent<TextMesh>();
            mark.transform.SetParent(root, false); mark.transform.localPosition = new Vector3(0, 0, -.02f);
            mark.text = "!"; mark.anchor = TextAnchor.MiddleCenter; mark.alignment = TextAlignment.Center; mark.fontSize = 64;
            mark.characterSize = d * .11f / s; mark.color = Color.white;
            UiFont.Apply(mark); mark.GetComponent<MeshRenderer>().sortingOrder = renderer.sortingOrder; // depth-sorted with the disc, so pages cover it
            root.gameObject.SetActive(false);
            return root.gameObject;
        }
        // Adds a UI-layer click area that covers every sprite under the target.
        static void AddHit(Transform target, string action)
        {
            var renderers = target.GetComponentsInChildren<Renderer>(true).Where(r => r is SpriteRenderer).ToArray();
            if (renderers.Length == 0) return;
            var bounds = renderers.CombinedBounds();
            var hit = new GameObject("Hit area - " + action);
            hit.layer = 9;
            hit.transform.SetParent(target, true);
            hit.transform.position = bounds.center;
            hit.transform.rotation = Quaternion.identity;
            var scale = hit.transform.lossyScale;
            var box = hit.AddComponent<BoxCollider>();
            box.size = new Vector3(bounds.size.x / Mathf.Max(.0001f, scale.x), bounds.size.y / Mathf.Max(.0001f, scale.y), .2f / Mathf.Max(.0001f, scale.z));
            hit.AddComponent<VideoUiButton>().Action = action;
        }

        int DisplayedLevel { get { return screen.Levels.PlayedNumber; } }

        public void Refresh()
        {
            int coins = EconomyManager.Coins, level = DisplayedLevel;
            bool doubled = screen.Levels.RewardDoubled;
            if (coins == shownCoins && level == shownLevel && doubled == shownDoubled) return;
            shownCoins = coins; shownLevel = level; shownDoubled = doubled;
            if (homeNumber) OutlinedText.SetText(homeNumber, Campaign.LevelNumber.ToString());
            if (hudLevel) OutlinedText.SetText(hudLevel, "Level " + level);
            // Original EasyHardLevelConfig: hard levels get a red badge + skull, bonus ("easy") levels a green one + coins.
            ApplyLevelKindBadge(OriginalConfig.IsEasy(level) ? 2 : OriginalConfig.IsHard(level) ? 1 : 0);
            if (resultTitle) OutlinedText.SetText(resultTitle, "Level " + level);
            if (rewardLabel) OutlinedText.SetText(rewardLabel, "+ " + (EconomyManager.WinRewardFor(level) * (doubled ? 2 : 1)));
            if (doubleButton) doubleButton.SetActive(!doubled);
        }

        // Original level-kind HUD (current build: level 260 hard, level 255 bonus): the level badge and the
        // gameplay coin bar change colour and an icon sits on the badge's left end.
        Vector3 labelPosition; SpriteRenderer skull, coinBar; Color coinColor = Color.white; bool labelPlaced;
        readonly Dictionary<SpriteRenderer, KeyValuePair<Sprite, Vector3>> originals = new Dictionary<SpriteRenderer, KeyValuePair<Sprite, Vector3>>();
        void SwapSprite(SpriteRenderer target, Sprite replacement, Color normalColor)
        {
            if (!target) return;
            if (!originals.ContainsKey(target)) originals[target] = new KeyValuePair<Sprite, Vector3>(target.sprite, target.transform.localScale);
            var original = originals[target];
            if (replacement && original.Key)
            {
                target.sprite = replacement;
                // Keep the on-screen size: scale the new sprite to the old sprite's bounds.
                var a = original.Key.bounds.size; var b = replacement.bounds.size;
                target.transform.localScale = new Vector3(original.Value.x * a.x / b.x, original.Value.y * a.y / b.y, original.Value.z);
                target.color = Color.white;
            }
            else { target.sprite = original.Key; target.transform.localScale = original.Value; target.color = normalColor; }
        }
        // kind: 0 normal, 1 hard (red badge + skull), 2 bonus (green badge + coin stack). The coin bar follows the badge.
        void ApplyLevelKindBadge(int kind)
        {
            if (!levelBadge) return;
            var badge = kind == 1 ? Resources.Load<Sprite>("HardLevel/bg-level-hard") : kind == 2 ? Resources.Load<Sprite>("BonusLevel/bg-level-easy") : null;
            SwapSprite(levelBadge, badge, badgeColor);
            SwapSprite(coinBar, badge, coinColor);
            var icon = kind == 1 ? Resources.Load<Sprite>("HardLevel/icon-hardLevel") : kind == 2 ? Resources.Load<Sprite>("BonusLevel/icon-easyLevel") : null;
            if (icon && !skull)
            {
                var obj = new GameObject("Level kind icon") { layer = levelBadge.gameObject.layer };
                obj.transform.SetParent(levelBadge.transform.parent, false);
                skull = obj.AddComponent<SpriteRenderer>(); skull.sortingOrder = levelBadge.sortingOrder; // in front of the badge by depth, still under popup layers
            }
            if (skull)
            {
                skull.gameObject.SetActive(icon);
                if (icon)
                {
                    skull.sprite = icon;
                    var bounds = levelBadge.bounds;
                    skull.transform.position = new Vector3(bounds.min.x - bounds.size.y * .1f, bounds.center.y + bounds.size.y * .05f, levelBadge.transform.position.z - .2f);
                    float s = bounds.size.y * 1.35f / Mathf.Max(icon.bounds.size.x, icon.bounds.size.y) * (icon.bounds.size.x > icon.bounds.size.y ? 1.15f : 1f);
                    skull.transform.localScale = Vector3.one * s / Mathf.Max(.0001f, skull.transform.parent.lossyScale.y);
                }
            }
            // The icon covers the badge's left end, so the level text moves right to stay readable.
            if (hudLevel)
            {
                if (!labelPlaced) { labelPosition = hudLevel.transform.position; labelPlaced = true; }
                hudLevel.transform.position = labelPosition + (icon ? Vector3.right * levelBadge.bounds.size.y * .3f : Vector3.zero);
            }
        }
        // The coin balance rolls up instead of jumping (reward on the Result screen / Home), with a little pop.
        float rollingCoins = -1, coinPop; int shownRolling = -1;
        readonly Dictionary<TextMesh, Vector3> coinScales = new Dictionary<TextMesh, Vector3>();
        void RollCoins()
        {
            int target = EconomyManager.Coins;
            bool canRoll = (screen.Current == VideoScreen.Page.Result && ResultFx.CoinsLanded) || screen.Current == VideoScreen.Page.Home;
            if (rollingCoins < 0 || target < rollingCoins) rollingCoins = target; // spending is instant
            else if (rollingCoins < target && canRoll)
            {
                float speed = Mathf.Max(12, (target - rollingCoins) * 2.2f);
                rollingCoins = Mathf.MoveTowards(rollingCoins, target, speed * Time.unscaledDeltaTime);
                coinPop = 1;
            }
            int shown = Mathf.FloorToInt(rollingCoins + .0001f);
            if (shown != shownRolling)
            {
                shownRolling = shown;
                foreach (var label in coinLabels) if (label) OutlinedText.SetText(label, shown.ToString());
            }
            coinPop = Mathf.MoveTowards(coinPop, 0, Time.unscaledDeltaTime * 4);
            foreach (var label in coinLabels)
            {
                if (!label) continue;
                Vector3 baseScale;
                if (!coinScales.TryGetValue(label, out baseScale)) coinScales[label] = baseScale = label.transform.localScale;
                label.transform.localScale = baseScale * (1 + .14f * coinPop * coinPop);
            }
        }

        void Update()
        {
            Refresh();
            RollCoins();
            // Hearts: "Full" when full, otherwise the countdown to the next heart (original Home HUD).
            if (Time.unscaledTime < nextLivesRefresh) return;
            nextLivesRefresh = Time.unscaledTime + .5f;
            if (livesCount) OutlinedText.SetText(livesCount, LivesManager.Lives.ToString());
            ShowQuestLock(!SandQuest.Unlocked);
            string join = !SandQuest.Unlocked ? "Lv. " + SandQuest.StartLevel : SandQuest.Joined ? SandQuest.StepsDone + "/" + SandQuest.Steps : "JOIN";
            var ink = SandQuest.Unlocked ? VideoScreen.JoinInk : LockedInk;
            foreach (var label in joinLabels)
            {
                if (!label) continue;
                OutlinedText.SetText(label, join);
                var edge = label.GetComponent<OutlinedText>();
                if (edge && edge.Ink != ink) edge.SetInk(ink);
            }
            if (livesStatus) OutlinedText.SetText(livesStatus, LivesManager.IsFull ? "Full" : LivesManager.Countdown(LivesManager.UntilNext));
        }

        // Before SandQuest.StartLevel the original keeps the chest on Home, locked: its disabled art (Chest_Disable,
        // Chest_Label_Disable), a padlock on the plaque's left (65x90 of the 262x111 plaque, 87 left of centre) and
        // "Lv. N" 12 right of centre in place of JOIN. A locked chest does not respond to taps.
        void BuildQuestLock(Transform chest, Transform plaque)
        {
            chestArt = chest ? chest.GetComponent<SpriteRenderer>() : null; plaqueArt = plaque ? plaque.GetComponent<SpriteRenderer>() : null;
            if (chestArt) chestOpen = chestArt.sprite;
            if (plaqueArt) plaqueOpen = plaqueArt.sprite;
            joinPlaces = joinLabels.Select(l => l.transform.position).ToArray();
            var lockArt = Resources.Load<Sprite>("VideoUI/icon-lock-quest");
            if (!plaqueArt || !lockArt) return;
            var b = plaqueArt.bounds;
            var r = new GameObject("Quest padlock").AddComponent<SpriteRenderer>();
            r.sprite = lockArt; r.sharedMaterial = SpriteFactory.PlainSprite; r.sortingOrder = plaqueArt.sortingOrder;
            r.gameObject.layer = plaque.gameObject.layer; r.transform.SetParent(plaque, true);
            r.transform.position = new Vector3(b.center.x - b.size.x * 87 / 262, b.center.y + b.size.y * 7 / 111, plaque.position.z - .02f);
            r.FitWorldSize(b.size.x * 65 / 262, b.size.y * 90 / 111);
            padlock = r.gameObject;
        }
        void ShowQuestLock(bool locked)
        {
            if (shownLocked == (locked ? 1 : 0)) return;
            shownLocked = locked ? 1 : 0;
            if (chestArt)
            {
                var art = locked ? Resources.Load<Sprite>("VideoUI/Chest_Disable") : chestOpen;
                if (art) chestArt.sprite = art;
                var hit = chestArt.GetComponent<Collider>(); if (hit) hit.enabled = !locked;
            }
            if (plaqueArt)
            {
                var art = locked ? Resources.Load<Sprite>("VideoUI/chest-label-locked") : plaqueOpen;
                if (art) plaqueArt.sprite = art;
            }
            if (padlock) padlock.SetActive(locked);
            float shift = locked && plaqueArt ? plaqueArt.bounds.size.x * 12 / 262 : 0;
            for (int i = 0; i < joinLabels.Length; i++) if (joinLabels[i]) joinLabels[i].transform.position = joinPlaces[i] + new Vector3(shift, 0, 0);
        }

        public void OpenSettings()
        {
            screen.Popups.ShowSettings();
        }
    }
}
