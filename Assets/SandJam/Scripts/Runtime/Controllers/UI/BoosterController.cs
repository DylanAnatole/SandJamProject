using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SandJamTest.Scene3D
{
    // Booster input and presentation; mutations are implemented in the pure game model.
    public sealed class BoosterController : MonoBehaviour
    {
        VideoScreen screen;
        SandJamSceneController controller;
        readonly Dictionary<string, Transform> buttons = new Dictionary<string, Transform>();
        // Inventory first, then coins (EconomyManager).
        void Pay(string action) { EconomyManager.UseBooster(action); }
        // Select booster armed: the next tap on a cube on screen sends it to the stash.
        public bool PickerOpen { get; private set; }
        public bool Busy { get; private set; }
        int pickerOpenedFrame;
        Vector3 selectButtonScale;
        GameObject rocket;

        public void Initialize(VideoScreen owner)
        {
            screen = owner; controller = owner.Controller;
            foreach (var placeholder in owner.Placeholders)
            {
                string action = placeholder.ActionId;
                if (action != "rocket" && action != "swap" && action != "select") continue;
                var image = placeholder.GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault(r => r.name == "Purple booster button");
                if (!image) continue;
                buttons[action] = image.transform;
                var hit = new GameObject("Booster hit area");
                hit.transform.SetParent(image.transform, false); hit.layer = 9;
                var collider = hit.AddComponent<BoxCollider>();
                collider.center = Vector3.zero;
                collider.size = new Vector3(image.sprite.bounds.size.x, image.sprite.bounds.size.y, .1f);
                var button = hit.AddComponent<VideoUiButton>();
                button.Action = "booster:" + action;
                if (Campaign.LevelNumber < OriginalConfig.BoosterUnlockLevel(action))
                {
                    // Locked until its NewBoosterConfig level: show "Level N" instead of the price and dim the button.
                    var priceLabel = placeholder.transform.Find("Price");
                    if (priceLabel) foreach (var label in priceLabel.GetComponentsInChildren<TextMesh>(true)) label.text = "Level " + OriginalConfig.BoosterUnlockLevel(action);
                    var coinIcon = placeholder.transform.Find("Price coin"); if (coinIcon) coinIcon.gameObject.SetActive(false);
                    foreach (var sprite in placeholder.GetComponentsInChildren<SpriteRenderer>(true)) sprite.color = new Color(.55f, .55f, .62f, sprite.color.a);
                }
                placeholders[action] = placeholder; AddBadge(action, image);
            }
            MatchOriginalButtonLayout();
        }

        // Original gameplay HUD: the booster buttons are ~15% larger and run off the right screen edge, their left
        // edge at ~85% of the screen width (ours sat fully on screen with a margin).
        void MatchOriginalButtonLayout()
        {
            foreach (var pair in buttons)
            {
                var image = pair.Value; var root = placeholders.ContainsKey(pair.Key) ? placeholders[pair.Key].transform : image.parent;
                var r = image.GetComponent<SpriteRenderer>(); if (!r) continue;
                var cam = SpriteFactory.CameraForLayer(image.gameObject.layer);
                if (!cam) continue;
                var pivot = r.bounds.center;
                root.localScale *= 1.15f;
                root.position += pivot - r.bounds.center; // grow about the button's centre
                var b = r.bounds; float z = cam.WorldToScreenPoint(b.center).z;
                var target = cam.ViewportToWorldPoint(new Vector3(.852f, 0, z));
                root.position += new Vector3(target.x - b.min.x, 0, 0);
                // The price (shown while none are owned) stays on screen: icon and price centred in the visible part.
                float visibleRight = cam.ViewportToWorldPoint(new Vector3(.975f, 0, z)).x, visibleCentre = (r.bounds.min.x + visibleRight) * .5f;
                FitWidth(root.Find("Price"), (visibleRight - r.bounds.min.x) * .86f);
                foreach (var name in new[] { "Price", "Price coin", "Booster icon" })
                {
                    var t = root.FindDeep(name);
                    if (!t || t == image) continue;
                    t.position += new Vector3(visibleCentre - r.bounds.center.x, 0, 0);
                }
            }
        }

        // Shrinks a label (with its outline copies) about its centre so it is at most maxWidth wide
        // (the locked "Level 22" text is wider than the visible part of the button).
        static void FitWidth(Transform label, float maxWidth)
        {
            if (!label) return;
            var renderers = label.GetComponentsInChildren<Renderer>(true); if (renderers.Length == 0) return;
            var b = renderers.CombinedBounds();
            if (b.size.x <= maxWidth) return;
            var centre = b.center; label.localScale *= maxWidth / b.size.x;
            label.position += centre - renderers.CombinedBounds().center;
        }

        // Original HUD: a yellow quantity badge on the booster's left edge when the player owns some;
        // otherwise the coin price is shown.
        readonly Dictionary<string, ReferenceUiPlaceholder> placeholders = new Dictionary<string, ReferenceUiPlaceholder>();
        readonly Dictionary<string, GameObject> badges = new Dictionary<string, GameObject>();
        readonly Dictionary<string, TextMesh> badgeLabels = new Dictionary<string, TextMesh>();
        void AddBadge(string action, SpriteRenderer image)
        {
            // The ReferenceUI copy is imported as a Sprite (the VideoUI copy is a plain texture).
            var sprite = Resources.Load<Sprite>("ReferenceUI/bg-booster-quantity");
            if (!sprite) return;
            var badge = new GameObject("Quantity badge") { layer = image.gameObject.layer };
            badge.transform.SetParent(image.transform, false);
            var size = image.sprite.bounds.size;
            badge.transform.localPosition = new Vector3(-size.x * .5f, -size.y * .22f, -.05f);
            var lossy = image.transform.lossyScale;
            float worldHeight = .24f; // original badge: small, on the button's left edge
            badge.transform.localScale = new Vector3(worldHeight / (sprite.bounds.size.y * lossy.x), worldHeight / (sprite.bounds.size.y * lossy.y), 1);
            var r = badge.AddComponent<SpriteRenderer>(); r.sprite = sprite; r.sortingOrder = image.sortingOrder + 2;
            var text = new GameObject("Quantity") { layer = badge.layer };
            text.transform.SetParent(badge.transform, false); text.transform.localPosition = new Vector3(0, 0, -.05f);
            var label = text.AddComponent<TextMesh>();
            label.text = "1"; label.fontSize = 64; label.anchor = TextAnchor.MiddleCenter; label.alignment = TextAlignment.Center;
            label.fontStyle = FontStyle.Bold; label.color = new Color(.33f, .2f, .05f);
            label.characterSize = sprite.bounds.size.y * .11f;
            if (controller.InterfaceFont) { label.font = controller.InterfaceFont; text.GetComponent<MeshRenderer>().sharedMaterial = controller.InterfaceFont.material; }
            text.GetComponent<MeshRenderer>().sortingOrder = r.sortingOrder + 1;
            badges[action] = badge; badgeLabels[action] = label;
        }
        void RefreshBadges()
        {
            foreach (var pair in placeholders)
            {
                string action = pair.Key;
                bool locked = Campaign.LevelNumber < OriginalConfig.BoosterUnlockLevel(action);
                int count = EconomyManager.BoosterCount(action);
                bool owned = !locked && count > 0;
                GameObject badge;
                if (badges.TryGetValue(action, out badge)) { badge.SetActive(owned); badgeLabels[action].text = count.ToString(); }
                if (locked) continue;
                var price = pair.Value.transform.Find("Price"); if (price) price.gameObject.SetActive(!owned);
                var coin = pair.Value.transform.Find("Price coin"); if (coin) coin.gameObject.SetActive(!owned);
                CentreIcon(action, pair.Value.transform, owned);
            }
        }
        // Original: with boosters in stock (no price shown) the icon sits larger in the middle of the button.
        readonly Dictionary<string, Vector3> iconPositions = new Dictionary<string, Vector3>(), iconScales = new Dictionary<string, Vector3>();
        void CentreIcon(string action, Transform root, bool owned)
        {
            var icon = root.Find("Booster icon"); Transform button; if (!icon || !buttons.TryGetValue(action, out button)) return;
            if (!iconPositions.ContainsKey(action)) { iconPositions[action] = icon.localPosition; iconScales[action] = icon.localScale; }
            icon.localPosition = iconPositions[action]; icon.localScale = iconScales[action];
            if (!owned) return;
            icon.localScale = iconScales[action] * 1.2f;
            var ib = icon.GetComponent<Renderer>(); var bb = button.GetComponent<Renderer>();
            if (ib && bb) icon.position += new Vector3(0, bb.bounds.center.y - ib.bounds.center.y, 0);
        }
        void OnEnable() { GameEvents.CoinsChanged += OnWallet; GameEvents.BoosterUsed += OnBooster; }
        void OnWallet(int balance, int delta, string reason) { RefreshBadges(); }
        void OnBooster(string action, int paid) { RefreshBadges(); }
        void Start() { RefreshBadges(); }

        public bool Activate(string action)
        {
            if (screen.Current != VideoScreen.Page.Gameplay || Busy || controller.Game == null || controller.Game.State != GameState.Playing) return false;
            controller.SuppressInputThisFrame();
            if (PickerOpen) { Cancel(); return false; }
            // Boosters work silently, as in the original (no caption under the board).
            if (screen.Levels.PlayedNumber < OriginalConfig.BoosterUnlockLevel(action) || !EconomyManager.CanUseBooster(action)) return false;
            if (action == "swap")
            {
                bool changed = controller.SwapFrontRows();
                if (changed) Pay(action);
                return changed;
            }
            if (action == "select")
            {
                if (!controller.Game.Lanes.SelectMany(l=>l).Any(controller.Game.CanSelectPriority)) return false;
                PickerOpen = true; pickerOpenedFrame = Time.frameCount; controller.BoosterInputBlocked = true;
                selectButtonScale = buttons["select"].localScale;
                return true;
            }
            if (action == "rocket")
            {
                var choices = Enumerable.Range(0,controller.Game.Regions.Length).Where(i=>!controller.Game.Regions[i].InformationVisible).ToArray();
                if (choices.Length == 0) return false;
                Pay(action);
                StartCoroutine(Reveal(choices[Random.Range(0, choices.Length)])); return true;
            }
            return false;
        }

        IEnumerator Reveal(int index)
        {
            Busy = true; controller.BoosterInputBlocked = true;
            var game = controller.Game;
            var origin = buttons["rocket"].position;
            var pixel = screen.UiCamera.WorldToScreenPoint(origin);
            var from = controller.GameCamera.ScreenToWorldPoint(new Vector3(pixel.x,pixel.y,28));
            var to = controller.Regions[index].Counter.transform.position; to.z = -1;
            rocket = new GameObject("Reveal rocket");
            var sprite = rocket.AddComponent<SpriteRenderer>();
            sprite.sprite = Resources.Load<Sprite>("VideoUI/icon-booster-3");
            if (sprite.sprite) rocket.transform.localScale = Vector3.one * (.55f / sprite.sprite.bounds.size.y);
            for (float time = 0; time < .4f; time += Time.unscaledDeltaTime)
            {
                if (controller.Game != game) break;
                float t = Mathf.Clamp01(time / .4f);
                rocket.transform.position = Vector3.Lerp(from,to,t) + Vector3.up * Mathf.Sin(t*Mathf.PI) * .45f;
                yield return null;
            }
            if (controller.Game == game && game.RevealRegion(index)) controller.Advance(0);
            if (rocket) Destroy(rocket);
            Busy = false; controller.BoosterInputBlocked = false; controller.SuppressInputThisFrame();
        }

        // While the Select booster is armed: a tap on a cube visible on screen sends it to the stash (a chained pair
        // only when nothing blocks it); a cube that cannot go shakes; a tap anywhere else, or on the booster again,
        // disarms it without charging.
        void Update()
        {
            if (!PickerOpen) return;
            // The armed booster button pulses gently until a cube is chosen.
            buttons["select"].localScale = selectButtonScale * (1 + .08f * Mathf.Sin(Time.unscaledTime * 8f));
            if (Time.frameCount == pickerOpenedFrame || !Input.GetMouseButtonDown(0)) return;
            RaycastHit hit;
            var cam = controller.GameCamera;
            var actor = cam && Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out hit, 100, 1 << 8) ? hit.collider.GetComponentInParent<SceneActorView>() : null;
            if (!actor || actor.Shooter == null) { Cancel(); return; }
            if (controller.SelectPriority(actor.Shooter)) { Pay("select"); Cancel(); }
            else actor.Shake();
        }
        public void Cancel()
        {
            StopAllCoroutines(); if (rocket) Destroy(rocket);
            if (PickerOpen) buttons["select"].localScale = selectButtonScale;
            PickerOpen = false; Busy = false;
            if (controller) { controller.BoosterInputBlocked = false; controller.SuppressInputThisFrame(); }
        }
        void OnDisable() { Cancel(); GameEvents.CoinsChanged -= OnWallet; GameEvents.BoosterUsed -= OnBooster; }
    }
}
