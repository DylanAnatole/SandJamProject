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
        readonly Dictionary<string, int> prices = new Dictionary<string, int>();
        // Prices apply only in the campaign scene (a LevelManager is present); test scenes stay free.
        int Price(string action) { return screen.Levels ? EconomyManager.BoosterPrice(action) : 0; }
        bool CanAfford(string action)
        {
            if (!screen.Levels || EconomyManager.CanUseBooster(action)) return true;
            Tell("Not enough coins · need " + Price(action)); return false;
        }
        // Campaign: inventory first, then coins (EconomyManager). Test scenes stay free.
        void Pay(string action) { if (screen.Levels) EconomyManager.UseBooster(action); else GameEvents.RaiseBoosterUsed(action, 0); }
        public bool PickerOpen { get; private set; }
        public bool Busy { get; private set; }
        public string Message { get { return Time.unscaledTime < messageUntil ? message : ""; } }
        string message;
        float messageUntil;
        Vector2 scroll;
        GameObject rocket;
        GUIStyle titleStyle, itemStyle, infoStyle;

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
                placeholder.FunctionImplemented = true;
                // Original prices (150 / 500 / 700 coins) are paid from the campaign wallet.
                prices[action] = placeholder.DisplayPrice;
                if (owner.Levels && Campaign.LevelNumber < OriginalConfig.BoosterUnlockLevel(action))
                {
                    // Locked until its NewBoosterConfig level: show "Level N" instead of the price and dim the button.
                    var priceLabel = placeholder.transform.Find("Price");
                    if (priceLabel) foreach (var label in priceLabel.GetComponentsInChildren<TextMesh>(true)) label.text = "Level " + OriginalConfig.BoosterUnlockLevel(action);
                    var coinIcon = placeholder.transform.Find("Price coin"); if (coinIcon) coinIcon.gameObject.SetActive(false);
                    foreach (var sprite in placeholder.GetComponentsInChildren<SpriteRenderer>(true)) sprite.color = new Color(.55f, .55f, .62f, sprite.color.a);
                }
                if (owner.Levels) { placeholders[action] = placeholder; AddBadge(action, image); }
                if(owner.ReferenceBoosterLabels || owner.Levels)continue;
                var price = placeholder.transform.Find("Price");
                if (price) foreach (var label in price.GetComponentsInChildren<TextMesh>(true)) label.text = "Use";
                var coin = placeholder.transform.Find("Price coin"); if (coin) coin.gameObject.SetActive(false);
            }
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
            float worldHeight = .3f;
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
            }
        }
        void OnEnable() { GameEvents.CoinsChanged += OnWallet; GameEvents.BoosterUsed += OnBooster; }
        void OnWallet(int balance, int delta, string reason) { RefreshBadges(); }
        void OnBooster(string action, int paid) { RefreshBadges(); }
        void Start() { RefreshBadges(); }

        void Tell(string text) { message = text; messageUntil = Time.unscaledTime + 3.5f; }
        public bool Activate(string action)
        {
            if (screen.Current != VideoScreen.Page.Gameplay || Busy || controller.Game == null || controller.Game.State != GameState.Playing) return false;
            controller.SuppressInputThisFrame();
            if (PickerOpen) { Cancel(); return false; }
            if (screen.Levels && screen.Levels.PlayedNumber < OriginalConfig.BoosterUnlockLevel(action))
            { Tell("Unlocks at level " + OriginalConfig.BoosterUnlockLevel(action)); return false; }
            if (!CanAfford(action)) return false;
            if (action == "swap")
            {
                bool changed = controller.SwapFrontRows();
                if (changed) Pay(action);
                Tell(changed ? "Front rows swapped" : "Not enough rows to swap");
                return changed;
            }
            if (action == "select")
            {
                if (!controller.Game.Lanes.SelectMany(l=>l).Any(controller.Game.CanSelectPriority))
                { Tell("No suitable cube or the stash is full"); return false; }
                PickerOpen = true; scroll = Vector2.zero; controller.BoosterInputBlocked = true;
                return true;
            }
            if (action == "rocket")
            {
                var choices = Enumerable.Range(0,controller.Game.Regions.Length).Where(i=>!controller.Game.Regions[i].InformationVisible).ToArray();
                if (choices.Length == 0) { Tell("All areas revealed"); return false; }
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
            if (controller.Game == game && game.RevealRegion(index))
            { controller.Advance(0); Tell("Color revealed · area still locked"); }
            if (rocket) Destroy(rocket);
            Busy = false; controller.BoosterInputBlocked = false; controller.SuppressInputThisFrame();
        }

        public bool Choose(Shooter shooter)
        {
            if (!PickerOpen) return false;
            bool selected = controller.SelectPriority(shooter);
            if (selected) { Pay("select"); Cancel(); Tell("Cube sent to the stash"); }
            else Tell("This cube cannot be sent right now");
            return selected;
        }
        public void Cancel()
        {
            StopAllCoroutines(); if (rocket) Destroy(rocket);
            PickerOpen = false; Busy = false;
            message = ""; messageUntil = 0;
            if (controller) { controller.BoosterInputBlocked = false; controller.SuppressInputThisFrame(); }
        }
        void OnDisable() { Cancel(); GameEvents.CoinsChanged -= OnWallet; GameEvents.BoosterUsed -= OnBooster; }

        Color ColorFor(int id)
        {
            var region = controller.Regions.FirstOrDefault(r=>controller.Game.Regions[r.PartIndex].Data.ColorType==id);
            return region && region.OverrideSandColor ? region.SandColor : ColorPalette.Of(id);
        }
        void OnGUI()
        {
            if (!PickerOpen) return;
            var oldMatrix = GUI.matrix; var oldColor = GUI.color; var oldBackground = GUI.backgroundColor;
            float scale = Mathf.Min(Screen.width / 483f, Screen.height / 1075f);
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width-483*scale)/2,(Screen.height-1075*scale)/2,0),Quaternion.identity,Vector3.one*scale);
            GUI.color = new Color(0,0,0,.8f); GUI.DrawTexture(new Rect(0,0,483,1075),Texture2D.whiteTexture); GUI.color = Color.white;
            GUI.color = new Color(.17f,.14f,.24f,1); GUI.DrawTexture(new Rect(18,165,447,790),Texture2D.whiteTexture); GUI.color=Color.white;
            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label) { font=controller.InterfaceFont,fontSize=25,alignment=TextAnchor.MiddleCenter,wordWrap=true };
                titleStyle.normal.textColor=Color.white;
                itemStyle = new GUIStyle(GUI.skin.button) { font=controller.InterfaceFont,fontSize=19,alignment=TextAnchor.MiddleLeft,padding=new RectOffset(68,12,8,8) };
                infoStyle = new GUIStyle(titleStyle) {fontSize=17};
            }
            GUI.Label(new Rect(25,180,433,45),"PICK ANY CUBE",titleStyle);
            GUI.Label(new Rect(30,232,423,62),"Only open colors can be picked.\nChained pairs need two free slots side by side.",infoStyle);
            var options = controller.Game.Lanes.SelectMany((lane,laneIndex)=>lane.Select((shooter,order)=>new { shooter,laneIndex,order })).Where(x=>controller.Game.Target(x.shooter)>=0).ToArray();
            scroll = GUI.BeginScrollView(new Rect(30,310,423,540),scroll,new Rect(0,0,397,options.Length*76));
            for (int i=0;i<options.Length;i++)
            {
                var option=options[i]; var box=new Rect(0,i*76,392,67);
                GUI.color=new Color(.30f,.25f,.39f,1);GUI.DrawTexture(box,Texture2D.whiteTexture);GUI.color=Color.white;
                GUI.enabled=controller.Game.CanSelectPriority(option.shooter);
                if (GUI.Button(box,"Lane "+(option.laneIndex+1)+" · Row "+(option.order+1)+"\n"+DisplayAmount.Units(option.shooter.Ammo,controller.Game.Data.uiDivider)+" sand"+(option.shooter.Partner!=null?" · Chained":""),itemStyle)) Choose(option.shooter);
                GUI.color=ColorFor(option.shooter.Color);GUI.DrawTexture(new Rect(12,i*76+12,43,43),Texture2D.whiteTexture);GUI.color=Color.white;
            }
            GUI.enabled=true;GUI.EndScrollView();
            if(GUI.Button(new Rect(130,885,223,55),"Cancel")) Cancel();
            GUI.matrix=oldMatrix;GUI.color=oldColor;GUI.backgroundColor=oldBackground;
        }
    }
}
