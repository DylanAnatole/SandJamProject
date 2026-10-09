using System.Collections.Generic;
using UnityEngine;

namespace SandJamTest.Scene3D
{
    // "Superb!" celebration effects, modelled on the original animated win texts (WellDoneText sheet: letters pop
    // in one by one with an overshoot): letter-by-letter pop, rotating light rays, a burst of sparkle stars and a
    // quick white flash. Replays every time the celebration page is shown.
    public sealed class SuperbFx : MonoBehaviour
    {
        public Font Font;
        TextMesh original;
        readonly List<Transform> letters = new List<Transform>();
        readonly List<float> letterBase = new List<float>();
        Transform rays, glow;
        Vector3 raysScale;
        SpriteRenderer flash;
        ParticleSystem stars;
        float time;
        const float LetterDelay = .07f, LetterPop = .28f;

        public void Build(TextMesh title)
        {
            original = title;
            var centre = title.transform.position;
            // Rays and glow behind the text.
            var light = Resources.Load<Sprite>("Congrats/light-congrats");
            if (light)
            {
                rays = Sprite("Rays", light, centre + new Vector3(0, 0, .6f), 6.5f, new Color(1, .86f, .35f, .55f)).transform; raysScale = rays.localScale;
                glow = Sprite("Glow", light, centre + new Vector3(0, 0, .5f), 3.2f, new Color(1, 1, 1, .45f)).transform;
            }
            var white = Texture2D.whiteTexture;
            flash = Sprite("Flash", UnityEngine.Sprite.Create(white, new Rect(0, 0, white.width, white.height), new Vector2(.5f, .5f), white.width), centre + new Vector3(0, 0, -.8f), 1, Color.white);
            flash.transform.localScale = new Vector3(12, 20, 1);
            // One TextMesh per letter so each can pop on its own; the original label is hidden.
            string text = title.text;
            var outlineColor = new Color(.35f, .12f, .55f);
            // Same glyph size as the original label (letters are children of this page, whose scale may differ).
            float size = title.characterSize * title.transform.lossyScale.x / Mathf.Max(.0001f, transform.lossyScale.x) * 1.15f;
            var faces = new List<MeshRenderer>();
            for (int i = 0; i < text.Length; i++)
            {
                var letter = new GameObject("Letter " + text[i]) { layer = gameObject.layer }.transform;
                letter.SetParent(transform, false);
                letter.position = centre + new Vector3(0, 0, -.3f);
                faces.Add(Glyph(letter, text[i].ToString(), Color.white, Vector3.zero, size, 0));
                foreach (var o in new[] { new Vector2(1, 1), new Vector2(-1, 1), new Vector2(1, -1), new Vector2(-1, -1), new Vector2(0, -1.6f) })
                    Glyph(letter, text[i].ToString(), outlineColor, new Vector3(o.x, o.y, 0) * size * .55f, size, .02f);
                letters.Add(letter);
            }
            // Lay the letters out by their real glyph widths, centred like the original word.
            var widths = new List<float>(); float total = 0;
            foreach (var f in faces) { float w = f.bounds.size.x; widths.Add(w); total += w; }
            float gap = faces.Count > 0 ? total / faces.Count * .1f : 0;
            total += gap * (faces.Count - 1);
            float x = centre.x - total / 2;
            for (int i = 0; i < letters.Count; i++)
            {
                letters[i].position = new Vector3(x + widths[i] / 2, centre.y, centre.z - .3f);
                letterBase.Add(centre.y); x += widths[i] + gap;
            }
            foreach (var t in title.GetComponentsInChildren<MeshRenderer>(true)) t.enabled = false;
            stars = Stars(centre);
        }

        MeshRenderer Glyph(Transform parent, string c, Color color, Vector3 offset, float size, float z)
        {
            var go = new GameObject(color == Color.white ? "Face" : "Outline") { layer = gameObject.layer };
            go.transform.SetParent(parent, false); go.transform.localPosition = offset + new Vector3(0, 0, z);
            var t = go.AddComponent<TextMesh>();
            t.text = c; t.fontSize = 64; t.characterSize = size; t.anchor = TextAnchor.MiddleCenter; t.fontStyle = FontStyle.Bold; t.color = color;
            if (Font) { t.font = Font; go.GetComponent<MeshRenderer>().sharedMaterial = Font.material; }
            var mr = go.GetComponent<MeshRenderer>(); mr.sortingOrder = color == Color.white ? 21 : 20;
            return mr;
        }

        SpriteRenderer Sprite(string name, Sprite sprite, Vector3 position, float width, Color color)
        {
            var r = new GameObject(name) { layer = gameObject.layer }.AddComponent<SpriteRenderer>();
            r.transform.SetParent(transform, false); r.transform.position = position;
            r.sprite = sprite; r.color = color; r.sortingOrder = 15;
            if (sprite) { float k = width / sprite.bounds.size.x; r.transform.localScale = new Vector3(k, k, 1); }
            return r;
        }

        ParticleSystem Stars(Vector3 centre)
        {
            var go = new GameObject("Sparkle stars") { layer = gameObject.layer };
            go.transform.SetParent(transform, false); go.transform.position = centre + new Vector3(0, 0, -.5f);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.playOnAwake = false; main.loop = false; main.duration = 1;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.6f, 1.1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 6f);
            main.startSize = new ParticleSystem.MinMaxCurve(.25f, .5f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1, .95f, .55f), new Color(1, 1, 1));
            main.gravityModifier = .6f; main.useUnscaledTime = true; main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission; emission.rateOverTime = 0; emission.SetBursts(new[] { new ParticleSystem.Burst(.05f, 40), new ParticleSystem.Burst(.45f, 24) });
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Circle; shape.radius = .35f;
            var size = ps.sizeOverLifetime; size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.EaseInOut(0, 1, 1, 0));
            var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-4, 4);
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            var star = Resources.Load<Texture2D>("Tasks/StarDisableSprite"); // gold star (original sprite names are swapped)
            renderer.material = new Material(Shader.Find("Sprites/Default")) { mainTexture = star };
            renderer.sortingOrder = 22;
            return ps;
        }

        void OnEnable()
        {
            time = 0;
            if (stars) { stars.Clear(); stars.Play(); }
            Update();
        }

        void Update()
        {
            time += Time.unscaledDeltaTime;
            for (int i = 0; i < letters.Count; i++)
            {
                float t = (time - i * LetterDelay) / LetterPop;
                float s = t <= 0 ? 0 : t >= 1 ? 1 : 1 + 1.7f * Mathf.Sin(t * Mathf.PI) * (1 - t) + (t < .5f ? -(1 - t * 2) * .9f : 0);
                letters[i].localScale = Vector3.one * Mathf.Max(0, s);
                // Gentle wave once settled.
                float wave = time > 1 ? Mathf.Sin((time - 1) * 5f + i * .7f) * .04f : 0;
                var p = letters[i].position; letters[i].position = new Vector3(p.x, letterBase[i] + wave, p.z);
            }
            if (rays) { rays.localRotation = Quaternion.Euler(0, 0, time * 25f); rays.localScale = raysScale * Mathf.Min(1, .3f + time * 2.5f); }
            if (glow) { float g = 1 + .08f * Mathf.Sin(time * 6); glow.localRotation = Quaternion.Euler(0, 0, -time * 12f); glow.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, .45f * g); }
            if (flash) { var c = flash.color; c.a = Mathf.Clamp01(.55f - time * 2.2f); flash.color = c; }
        }
    }
}
