using System.Collections.Generic;
using UnityEngine;

namespace SandJamTest.Scene3D
{
    // Level Complete page like the original (Game scene: coinSpawn, Fx_Rotate_Light; recording of level 256):
    // a soft rotating light behind the card, the finished picture faintly visible through the dark backdrop, and
    // the reward coins flying from the reward box up to the coin counter. The buttons stay dimmed while the coins
    // fly; the counter starts rolling as the first coins arrive (CampaignUi waits for CoinsLanded).
    public sealed class ResultFx : MonoBehaviour
    {
        public static bool CoinsLanded { get; private set; } = true;
        const int CoinCount = 10;
        const float Start = .35f, Spread = .06f, Flight = .55f;
        Transform from, to, rays;
        readonly List<Transform> coins = new List<Transform>();
        readonly List<SpriteRenderer> buttons = new List<SpriteRenderer>();
        readonly List<Color> buttonColors = new List<Color>();
        Vector3 raysScale;
        float time;

        public void Build()
        {
            from = transform.Find("icon-coin-reward");
            to = transform.Find("icon-coin");
            var backdrop = transform.Find("Dark backdrop");
            if (backdrop) { var r = backdrop.GetComponent<SpriteRenderer>(); if (r) { var c = r.color; c.a = .86f; r.color = c; } }
            var light = Resources.Load<Sprite>("Congrats/light-congrats");
            var header = transform.Find("Gold header");
            if (light && header)
            {
                var r = new GameObject("Card light") { layer = gameObject.layer }.AddComponent<SpriteRenderer>();
                r.transform.SetParent(transform, false);
                r.transform.position = header.position + new Vector3(0, .1f, .5f);
                r.sprite = light; r.color = new Color(1, .85f, .35f, .5f);
                float k = 5.2f / light.bounds.size.x; r.transform.localScale = new Vector3(k, k, 1);
                rays = r.transform; raysScale = rays.localScale;
            }
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

        void OnEnable() { time = 0; CoinsLanded = coins.Count == 0; }
        void OnDisable() { CoinsLanded = true; Dim(0); foreach (var c in coins) if (c) c.gameObject.SetActive(false); }

        void Dim(float amount)
        {
            for (int i = 0; i < buttons.Count; i++)
                if (buttons[i]) buttons[i].color = Color.Lerp(buttonColors[i], new Color(.55f, .55f, .6f, buttonColors[i].a), amount);
        }

        void Update()
        {
            time += Time.unscaledDeltaTime;
            if (rays) { rays.localRotation = Quaternion.Euler(0, 0, time * 14f); rays.localScale = raysScale * (1 + .04f * Mathf.Sin(time * 3)); }
            if (coins.Count == 0 || !from || !to) return;
            float end = Start + Spread * (coins.Count - 1) + Flight;
            Dim(time < end ? .65f : Mathf.Clamp01(1 - (time - end) / .2f) * .65f);
            for (int i = 0; i < coins.Count; i++)
            {
                float t = (time - Start - i * Spread) / Flight;
                var c = coins[i];
                if (t <= 0 || t >= 1) { c.gameObject.SetActive(false); if (t >= 1 && i == 0) CoinsLanded = true; continue; }
                c.gameObject.SetActive(true);
                // Burst out of the reward box, then curve up to the counter (ease-in toward the end).
                var a = from.position; var b = to.position;
                float side = ((i * 37) % 11 - 5) * .12f;
                var mid = a + new Vector3(side, .6f + (i % 3) * .15f, 0);
                float e = t * t;
                var p = Vector3.Lerp(Vector3.Lerp(a, mid, e), Vector3.Lerp(mid, b, e), e);
                p.z = Mathf.Min(a.z, b.z) - .3f;
                c.position = p;
            }
        }
    }
}
