using System.Collections.Generic;
using UnityEngine;

namespace SandJamTest.Scene3D
{
    // Presentation-only effects modelled on the original: pour dust, region-complete bubble burst,
    // shooter recoil, arrival puff and departure poof. Polls game state; never changes rules.
    public sealed class SandFx : MonoBehaviour
    {
        const float Front = -.45f; // towards the camera so effects draw over the board
        SandJamSceneController controller;
        ParticleSystem dust, sparkle, puff;
        readonly List<Bubble> bubbles = new List<Bubble>();
        readonly Dictionary<SceneActorView, ActorState> actors = new Dictionary<SceneActorView, ActorState>();
        bool[] regionDone;
        SandGame lastGame;
        Material bubbleMaterial;
        static Texture2D softDot, ring;

        sealed class ActorState { public int Ammo; public bool Departing, AtRest; public float Pulse; public Vector3 BaseScale; }
        sealed class Bubble { public Transform Root; public SpriteRenderer Disc, Rim; public float Age, Life, Size; }

        public void Bind(SandJamSceneController owner)
        {
            controller = owner;
            BuildTextures();
            dust = MakeSystem("Pour dust", softDot, 400, .35f, .05f, .12f);
            sparkle = MakeSystem("Region sparkles", softDot, 600, .9f, .05f, .14f);
            puff = MakeSystem("Character puffs", softDot, 300, .5f, .12f, .28f);
            bubbleMaterial = new Material(Shader.Find("Sprites/Default"));
        }

        static void BuildTextures()
        {
            if (softDot) return;
            const int n = 64;
            softDot = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "Fx soft dot", wrapMode = TextureWrapMode.Clamp };
            ring = new Texture2D(n * 2, n * 2, TextureFormat.RGBA32, false) { name = "Fx bubble", wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                softDot.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(1 - d) * Mathf.Clamp01(1 - d)));
            }
            softDot.Apply();
            int m = n * 2;
            for (int y = 0; y < m; y++) for (int x = 0; x < m; x++)
            {
                var p = new Vector2(x + .5f, y + .5f) / (m / 2f) - Vector2.one;
                float d = p.magnitude;
                // Translucent bubble: faint body, bright rim and a highlight in the upper left.
                float body = d < 1 ? .18f + .25f * d * d : 0;
                float rim = Mathf.Clamp01(1 - Mathf.Abs(d - .93f) / .07f);
                float shine = Mathf.Clamp01(1 - Vector2.Distance(p, new Vector2(-.4f, .45f)) / .22f);
                ring.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(body + rim * .9f + shine * .8f)));
            }
            ring.Apply();
        }

        ParticleSystem MakeSystem(string name, Texture2D texture, int max, float life, float minSize, float maxSize)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(transform, false);
            var ps = obj.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            // Looping with emission disabled keeps the system simulating particles sent via Emit().
            main.loop = true; main.playOnAwake = false; main.maxParticles = max;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * .6f, life);
            main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
            main.startSpeed = 0; main.gravityModifier = .35f; main.useUnscaledTime = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission; emission.enabled = false;
            var shape = ps.shape; shape.enabled = false;
            var fade = ps.colorOverLifetime; fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                             new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(.9f, .5f), new GradientAlphaKey(0, 1) });
            fade.color = gradient;
            var size = ps.sizeOverLifetime; size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.EaseInOut(0, 1, 1, .25f));
            var renderer = obj.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Sprites/Default")) { mainTexture = texture };
            renderer.sortingOrder = 30;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false;
            ps.Play();
            return ps;
        }

        static void Emit(ParticleSystem ps, Vector3 position, Vector3 velocity, Color color, float size, float life)
        {
            var p = new ParticleSystem.EmitParams { position = position, velocity = velocity, startColor = color, startSize = size, startLifetime = life, applyShapeToPosition = false };
            ps.Emit(p, 1);
        }

        void LateUpdate()
        {
            if (controller == null || controller.Game == null) return;
            var game = controller.Game;
            if (game != lastGame) Reset(game);
            float dt = Mathf.Min(Time.unscaledDeltaTime, .1f); // matches the controller's clock
            PourDust(dt);
            RegionBursts(game);
            Characters(game, dt);
            AnimateBubbles(dt);
        }

        void Reset(SandGame game)
        {
            lastGame = game;
            regionDone = new bool[game.Regions.Length];
            foreach (var pair in actors) if (pair.Key) pair.Key.transform.localScale = pair.Value.BaseScale;
            actors.Clear();
            foreach (var b in bubbles) if (b.Root) Destroy(b.Root.gameObject);
            bubbles.Clear();
            dust.Clear(); sparkle.Clear(); puff.Clear();
        }

        float dustTimer;
        void PourDust(float dt)
        {
            dustTimer += dt;
            if (dustTimer < .03f) return;
            dustTimer = 0;
            foreach (var region in controller.Regions)
            {
                Vector3 hit;
                if (!region.TryGetPourImpact(out hit)) continue;
                Color c = region.SolidColor; c = Color.Lerp(c, Color.white, .25f);
                hit.z += Front;
                for (int i = 0; i < 2; i++)
                    Emit(dust, hit + new Vector3(Random.Range(-.03f, .03f), .02f, 0),
                        new Vector3(Random.Range(-.55f, .55f), Random.Range(.25f, .7f), 0), c, Random.Range(.04f, .08f), Random.Range(.18f, .35f));
            }
        }

        void RegionBursts(SandGame game)
        {
            for (int i = 0; i < controller.Regions.Length; i++)
            {
                var view = controller.Regions[i];
                bool done = view.IsSettled && game.Regions[view.PartIndex].Remaining == 0;
                if (!done || regionDone[i]) { regionDone[i] = done; continue; }
                regionDone[i] = true;
                var center = view.Counter ? view.Counter.transform.position : view.Target.position;
                center.z = view.Board.transform.position.z + Front;
                Color color = view.SolidColor;
                // Original: a few translucent bubbles pop over the finished region with coloured sparkles.
                for (int b = 0; b < 5; b++)
                    SpawnBubble(center + (Vector3)(Random.insideUnitCircle * .55f), Random.Range(.5f, .85f), b * .07f);
                for (int s = 0; s < 46; s++)
                {
                    var dir = Random.insideUnitCircle.normalized * Random.Range(.6f, 2.2f);
                    Color sc = s % 3 == 0 ? Color.white : Color.Lerp(color, Color.white, Random.Range(0f, .35f));
                    Emit(sparkle, center + (Vector3)(Random.insideUnitCircle * .25f), new Vector3(dir.x, dir.y + .6f, 0), sc, Random.Range(.07f, .17f), Random.Range(.45f, .9f));
                }
            }
        }

        void SpawnBubble(Vector3 position, float size, float delay)
        {
            var root = new GameObject("Completion bubble").transform;
            root.SetParent(transform, false); root.position = position;
            var sprite = Sprite.Create(ring, new Rect(0, 0, ring.width, ring.height), new Vector2(.5f, .5f), ring.width);
            var disc = root.gameObject.AddComponent<SpriteRenderer>();
            disc.sprite = sprite; disc.sharedMaterial = bubbleMaterial; disc.sortingOrder = 29;
            disc.color = new Color(.75f, .95f, 1f, 0);
            bubbles.Add(new Bubble { Root = root, Disc = disc, Age = -delay, Life = .55f, Size = size });
        }

        void AnimateBubbles(float dt)
        {
            for (int i = bubbles.Count - 1; i >= 0; i--)
            {
                var b = bubbles[i];
                b.Age += dt;
                if (b.Age < 0) { b.Root.localScale = Vector3.zero; continue; }
                float t = b.Age / b.Life;
                if (t >= 1)
                {
                    // Pop: scatter a ring of droplets where the bubble was.
                    for (int k = 0; k < 10; k++)
                    {
                        float a = k * Mathf.PI * 2 / 10;
                        Emit(sparkle, b.Root.position, new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * 1.4f, new Color(.8f, .97f, 1f), .06f, .3f);
                    }
                    if (b.Disc.sprite) Destroy(b.Disc.sprite);
                    Destroy(b.Root.gameObject); bubbles.RemoveAt(i); continue;
                }
                float grow = 1 - Mathf.Pow(1 - Mathf.Clamp01(t * 1.6f), 3);
                b.Root.localScale = Vector3.one * b.Size * (.3f + .7f * grow + .08f * Mathf.Sin(t * 20));
                b.Disc.color = new Color(.55f, .9f, 1f, Mathf.Clamp01(t * 6) * (1 - t * .3f));
            }
        }

        void Characters(SandGame game, float dt)
        {
            foreach (var actor in controller.Characters)
            {
                if (!actor || actor.Shooter == null) continue;
                ActorState state;
                if (!actors.TryGetValue(actor, out state))
                {
                    state = new ActorState { Ammo = actor.Shooter.Ammo, Departing = actor.Departing, AtRest = actor.AtRest, BaseScale = actor.transform.localScale };
                    actors.Add(actor, state);
                }
                if (!actor.gameObject.activeInHierarchy) continue;
                Color color = ColorPalette.Of(actor.Shooter.Color);
                // Recoil / landing squash now live in SceneActorView (Punch); only particles here.
                state.Ammo = actor.Shooter.Ammo;
                // Landing puff when a character reaches its waiting slot.
                bool inSlot = System.Array.IndexOf(game.Slots, actor.Shooter) >= 0;
                if (actor.AtRest && !state.AtRest && inSlot && !actor.Departing)
                {
                    var p = actor.transform.position + new Vector3(0, -.05f, Front);
                    for (int i = 0; i < 12; i++)
                        Emit(puff, p, new Vector3(Random.Range(-1.2f, 1.2f), Random.Range(.1f, .6f), 0), new Color(1, 1, 1, .85f), Random.Range(.08f, .16f), Random.Range(.25f, .4f));
                }
                state.AtRest = actor.AtRest;
                // Empty character leaves with a coloured poof.
                if (actor.Departing && !state.Departing)
                {
                    var p = actor.AimPoint + new Vector3(0, 0, Front);
                    for (int i = 0; i < 22; i++)
                    {
                        var v = Random.insideUnitCircle.normalized * Random.Range(.8f, 2f);
                        Emit(puff, p, new Vector3(v.x, v.y + .5f, 0), Color.Lerp(color, Color.white, Random.Range(.1f, .6f)), Random.Range(.07f, .16f), Random.Range(.3f, .5f));
                    }
                }
                state.Departing = actor.Departing;
            }
        }

        void OnDestroy()
        {
            if (bubbleMaterial) Destroy(bubbleMaterial);
            foreach (var ps in new[] { dust, sparkle, puff }) if (ps) Destroy(ps.GetComponent<ParticleSystemRenderer>().material);
        }
    }
}
