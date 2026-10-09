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
        bool shownDoubled;
        public bool SettingsOpen { get { return screen.Popups && screen.Popups.Open; } }

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
                if (child.name.StartsWith("LV ")) child.gameObject.SetActive(false);
            var x2 = owner.Result.transform.Find("x2");
            if (x2) { doubleButton = x2.gameObject; AddHit(x2, "double"); }
            var hud = owner.Gameplay.transform.Find("HUD - visual controls only/Settings - UI only");
            if (hud) AddHit(hud, "settings");
            var badge = owner.Gameplay.transform.Find("HUD - visual controls only/Level badge");
            if (badge) { levelBadge = badge.GetComponentInChildren<SpriteRenderer>(true); if (levelBadge) badgeColor = levelBadge.color; }
            coinBar = owner.Gameplay.GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault(r => r.name == "Coin bar");
            if (coinBar) coinColor = coinBar.color;
            // Sand Quest chest ("JOIN"): opens the event; shows the run progress while joined.
            var chest = owner.Home.transform.Find("Chest_Enable");
            if (chest) AddHit(chest, "sand-quest");
            joinLabels = owner.Home.GetComponentsInChildren<TextMesh>(true).Where(t => t.text == "JOIN").ToArray();
            // Navbar SHOP opens the Sand Shop page.
            var shopIcon = owner.Home.transform.Find("icon-shop");
            if (shopIcon) AddHit(shopIcon, "shop");
            var galleryIcon = owner.Home.transform.Find("icon-cards");
            if (galleryIcon) AddHit(galleryIcon, "gallery");
            var homeGear = owner.Home.transform.Find("icon-settings-v2");
            if (homeGear) AddHit(homeGear, "settings");
            Campaign.Changed += Refresh;
            Refresh();
        }
        void OnDestroy() { Campaign.Changed -= Refresh; }

        // Adds a UI-layer click area that covers every sprite under the target.
        static void AddHit(Transform target, string action)
        {
            var renderers = target.GetComponentsInChildren<Renderer>(true).Where(r => r is SpriteRenderer).ToArray();
            if (renderers.Length == 0) return;
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
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

        int DisplayedLevel { get { return screen.Levels ? screen.Levels.PlayedNumber : Campaign.LevelNumber; } }

        public void Refresh()
        {
            int coins = Campaign.Coins, level = DisplayedLevel;
            bool doubled = screen.Levels && screen.Levels.RewardDoubled;
            if (coins == shownCoins && level == shownLevel && doubled == shownDoubled) return;
            shownCoins = coins; shownLevel = level; shownDoubled = doubled;
            if (homeNumber) homeNumber.text = Campaign.LevelNumber.ToString();
            if (hudLevel) hudLevel.text = "Level " + level;
            // Original EasyHardLevelConfig: hard levels get a red badge + skull, bonus ("easy") levels a green one + coins.
            ApplyLevelKindBadge(OriginalConfig.IsEasy(level) ? 2 : OriginalConfig.IsHard(level) ? 1 : 0);
            if (resultTitle) resultTitle.text = "Level " + level;
            if (rewardLabel) rewardLabel.text = "+ " + (EconomyManager.WinRewardFor(level) * (doubled ? 2 : 1));
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
                skull = obj.AddComponent<SpriteRenderer>(); skull.sortingOrder = levelBadge.sortingOrder + 1;
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
            int target = Campaign.Coins;
            bool canRoll = screen.Current == VideoScreen.Page.Result || screen.Current == VideoScreen.Page.Home;
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
                foreach (var label in coinLabels) if (label) label.text = shown.ToString();
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
            if (livesCount) livesCount.text = LivesManager.Lives.ToString();
            string join = SandQuest.Joined ? SandQuest.StepsDone + "/" + SandQuest.Steps : "JOIN";
            foreach (var label in joinLabels) if (label) label.text = join;
            if (livesStatus) livesStatus.text = LivesManager.IsFull ? "Full" : LivesManager.Countdown(LivesManager.UntilNext);
        }

        public void OpenSettings()
        {
            if (screen.Popups) screen.Popups.ShowSettings();
        }
    }
}
