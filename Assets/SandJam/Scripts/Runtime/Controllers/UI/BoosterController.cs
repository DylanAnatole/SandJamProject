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
        GameObject rocket;

        // Select booster armed (recording 3, 209.6-214.5 s): a see-through purple card over the waiting slots,
        // "SELECT ANY SHOOTER" / "Tap on any shooter you want" with a red close button, and every cube that can be
        // picked rocks side to side (~3.5 deg, 0.53 s). Measured at 211.5 s: card x 158-730, y 1108-1295 px.
        const float RockDegrees = 3.5f, RockSeconds = .53f;
        GameObject selectCard;
        void ShowSelectCard(bool show)
        {
            if (show && !selectCard) selectCard = BuildSelectCard();
            if (selectCard) selectCard.SetActive(show);
        }
        GameObject BuildSelectCard()
        {
            var cam = screen.UiCamera; var anchor = buttons[BoosterCatalog.Select];
            System.Func<float, float, Vector3> spot = (x, y) => new Vector3(cam.transform.position.x + (x - 444) * VideoScreen.Px, cam.transform.position.y + (960 - y) * VideoScreen.Px, anchor.position.z - .4f);
            var root = new GameObject("Select booster card") { layer = anchor.gameObject.layer };
            root.transform.SetParent(anchor.parent, true); root.transform.position = spot(444, 1201);
            SpriteRenderer Panel(string name, Color colour, float w, float h, float dz)
            {
                var r = new GameObject(name) { layer = root.layer }.AddComponent<SpriteRenderer>();
                r.transform.SetParent(root.transform, false); r.transform.localPosition = new Vector3(0, 0, dz);
                r.sprite = SpriteFactory.Rounded(64, 24, 120); r.drawMode = SpriteDrawMode.Sliced; r.size = new Vector2(w, h); r.color = colour;
                r.sharedMaterial = SpriteFactory.PlainSprite; return r;
            }
            float w = 572 * VideoScreen.Px, h = 187 * VideoScreen.Px;
            // See-through like the original (the slots show through): one tinted card, measured 168,160,208 over the slots.
            Panel("Card", new Color32(168, 153, 230, 128), w, h, 0);
            var line = Panel("Line", new Color(.4f, .3f, .75f, .8f), 330 * VideoScreen.Px, .02f, -.02f); line.transform.position = spot(400, 1200) + Vector3.forward * -.02f;
            TextMesh Text(string text, float x, float y, float size)
            {
                var t = new GameObject(text) { layer = root.layer }.AddComponent<TextMesh>();
                t.transform.SetParent(root.transform, true); t.transform.position = spot(x, y) + Vector3.forward * -.05f;
                t.text = text; t.fontSize = 64; t.characterSize = size; t.anchor = TextAnchor.MiddleCenter; t.alignment = TextAlignment.Center; t.color = Color.white;
                if (controller.InterfaceFont) { t.font = controller.InterfaceFont; t.GetComponent<MeshRenderer>().sharedMaterial = controller.InterfaceFont.material; }
                OutlinedText.Add(t, TextOutliner.Ink); return t;
            }
            Text("SELECT ANY SHOOTER", 388, 1163, .033f);
            Text("Tap on any shooter you want", 390, 1238, .021f);
            var close = Resources.Load<Sprite>("VideoUI/button-exit-v2");
            if (close)
            {
                var x = new GameObject("Close") { layer = root.layer }.AddComponent<SpriteRenderer>();
                x.transform.SetParent(root.transform, true); x.transform.position = spot(655, 1195) + Vector3.forward * -.05f;
                x.sprite = close; x.SetLocalHeight(110 * VideoScreen.Px / Mathf.Max(.0001f, root.transform.lossyScale.y));
            }
            return root.gameObject;
        }
        void RockCubes(bool on)
        {
            float rock = on ? RockDegrees * Mathf.Sin(Time.unscaledTime * 2 * Mathf.PI / RockSeconds) : 0;
            foreach (var actor in controller.Characters)
                if (actor) actor.Rock = on && actor.Shooter != null && controller.Game.CanSelectPriority(actor.Shooter) ? rock : 0;
        }

        public void Initialize(VideoScreen owner)
        {
            screen = owner; controller = owner.Controller;
            foreach (var placeholder in owner.Placeholders)
            {
                string action = placeholder.ActionId;
                if (BoosterCatalog.Find(action) == null) continue;
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

        // Original gameplay HUD (recording 2, 888 x 1920): the booster buttons run off the right screen edge, their
        // left edge at ~85% of the screen width, 86 px tall and 124 px apart (ours were 104 tall, 139 apart).
        const float ButtonScale = .955f, ButtonSpacing = .89f;
        void MatchOriginalButtonLayout()
        {
            var placed = new List<KeyValuePair<Transform, Transform>>(); // group root, button image
            foreach (var pair in buttons)
            {
                var image = pair.Value; var root = placeholders.ContainsKey(pair.Key) ? placeholders[pair.Key].transform : image.parent;
                var r = image.GetComponent<SpriteRenderer>(); if (!r) continue;
                var cam = SpriteFactory.CameraForLayer(image.gameObject.layer);
                if (!cam) continue;
                var pivot = r.bounds.center;
                root.localScale *= ButtonScale; placed.Add(new KeyValuePair<Transform, Transform>(root, image));
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
            // Closer together, the top button kept in place (measured on the button images: the groups' own pivots
            // sit far from the buttons).
            if (placed.Count == 0) return;
            float top = placed.Max(e => e.Value.position.y);
            foreach (var e in placed) e.Key.position += Vector3.up * (top + (e.Value.position.y - top) * ButtonSpacing - e.Value.position.y);
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
            // Original badge (recording 1, 720 s): ~46 px across, on the button's lower-left corner.
            float worldHeight = BadgeSize;
            badge.transform.localScale = new Vector3(worldHeight / (sprite.bounds.size.y * lossy.x), worldHeight / (sprite.bounds.size.y * lossy.y), 1);
            var r = badge.AddComponent<SpriteRenderer>(); r.sprite = sprite; r.sortingOrder = image.sortingOrder; // in front of the button by depth, under popup layers
            var text = new GameObject("Quantity") { layer = badge.layer };
            text.transform.SetParent(badge.transform, false); text.transform.localPosition = new Vector3(0, 0, -.05f);
            var label = text.AddComponent<TextMesh>();
            label.text = "1"; label.fontSize = 64; label.anchor = TextAnchor.MiddleCenter; label.alignment = TextAlignment.Center;
            label.fontStyle = FontStyle.Bold; label.color = Color.white; // white with a dark outline (recording 1, 720 s)
            label.characterSize = sprite.bounds.size.y * .11f;
            if (controller.InterfaceFont) { label.font = controller.InterfaceFont; text.GetComponent<MeshRenderer>().sharedMaterial = controller.InterfaceFont.material; }
            text.GetComponent<MeshRenderer>().sortingOrder = r.sortingOrder;
            OutlinedText.Add(label, TextOutliner.Ink, .08f);
            // The badge is tiny in world units: stretch the label's depth so its outline copies sit clearly behind it.
            text.transform.localScale = new Vector3(1, 1, 4); // copies at -.01: behind the digit, in front of the badge
            badges[action] = badge; badgeLabels[action] = label;
            badge.transform.position += new Vector3(BadgeShiftPx.x, -BadgeShiftPx.y, 0) * VideoScreen.Px;
        }
        const float BadgeSize = .3f;
        static readonly Vector2 BadgeShiftPx = new Vector2(6, 17);
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
            if (action == BoosterCatalog.Swap)
            {
                bool changed = controller.SwapFrontRows();
                if (changed) Pay(action);
                return changed;
            }
            if (action == BoosterCatalog.Select)
            {
                if (!controller.Game.Lanes.SelectMany(l=>l).Any(controller.Game.CanSelectPriority)) return false;
                PickerOpen = true; pickerOpenedFrame = Time.frameCount; controller.BoosterInputBlocked = true;
                ShowSelectCard(true);
                return true;
            }
            if (action == BoosterCatalog.Flare)
            {
                var choices = Enumerable.Range(0,controller.Game.Regions.Length).Where(controller.Game.CanRevealRegion).ToArray();
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
            var origin = buttons[BoosterCatalog.Flare].position;
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
            // The game waits while the player picks; a zero step still poses the cubes, so they rock.
            RockCubes(true); controller.Advance(0);
            if (Time.frameCount == pickerOpenedFrame || !Input.GetMouseButtonDown(0)) return;
            RaycastHit hit;
            var cam = controller.GameCamera;
            var actor = cam && Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out hit, 100, 1 << 8) ? hit.collider.GetComponentInParent<SceneActorView>() : null;
            if (!actor || actor.Shooter == null) { Cancel(); return; }
            if (controller.SelectPriority(actor.Shooter)) { Pay(BoosterCatalog.Select); Cancel(); }
            else actor.Shake();
        }
        public void Cancel()
        {
            StopAllCoroutines(); if (rocket) Destroy(rocket);
            if (PickerOpen) { ShowSelectCard(false); RockCubes(false); if (controller) controller.Advance(0); }
            PickerOpen = false; Busy = false;
            if (controller) { controller.BoosterInputBlocked = false; controller.SuppressInputThisFrame(); }
        }
        void OnDisable() { Cancel(); GameEvents.CoinsChanged -= OnWallet; GameEvents.BoosterUsed -= OnBooster; }
    }
}
