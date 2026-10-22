using System.Collections.Generic;
using UnityEngine;

namespace SandJamTest.Scene3D
{
    // Presentation-only effects modelled on the original: pour dust, region-open bubble burst,
    // sand needles out of a pouring box, the flash where a chain snaps when its pair is picked, the
    // smoke a mystery cube shows its colour in and the ring of smoke a locked waiting slot opens with. Polls game
    // state; never changes rules.
    public sealed class SandFx : MonoBehaviour
    {
        const float Front = -.45f; // towards the camera so effects draw over the board
        SandJamSceneController controller;
        ParticleSystem dust, sparkle, puff, voxels, spray;
        readonly Dictionary<SceneActorView, ActorState> actors = new Dictionary<SceneActorView, ActorState>();
        bool[] regionShown;
        int[] slotLocked = new int[0];
        SandGame lastGame;
        Material bubbleMaterial;
        static Texture2D softDot, ring, voxel;

        sealed class ActorState { public int Ammo; public bool Hidden; public Vector3 BaseScale; public SceneActorView Partner; public Renderer[] Box; }
        sealed class Bubble { public Transform Root; public SpriteRenderer Disc, Glow; public float Age, Life, Size; }
        // Region-open bubbles of the original (recording 2, 153.05-153.35 s): one per region that just opened, on its
        // counter: nearly clear (the region and its number show through), a thin bright rim, gone in ~0.3 s. Around it
        // a few big sand cubes in the region's colour and some green confetti.
        const float BubbleScale = 1.6f, BubbleLife = .45f, BubbleSize = .75f;
        const int BurstGrains = 34, BurstSparkles = 16;
        const float ConfettiShare = .25f; static readonly Color Confetti = new Color(.25f, .85f, .3f);
        // Sand needles out of a pouring box (count range per shot, speed, life, width; colour a little darker).
        const int SprayMin = 1, SprayMax = 3;
        const float SpraySpeed = 3.6f, SprayLife = .12f, SprayWidth = .034f, SprayShade = .82f;

        public void Bind(SandJamSceneController owner)
        {
            controller = owner;
            BuildTextures();
            dust = MakeSystem("Pour dust", softDot, 400, .35f, .05f, .12f);
            sparkle = MakeSystem("Region sparkles", softDot, 600, .9f, .05f, .14f);
            puff = MakeSystem("Character puffs", softDot, 300, .5f, .12f, .28f);
            // Original region-open shower: tiny sand cubes in the region's colour pop up and fall.
            voxels = MakeSystem("Region voxels", voxel, 1500, 1.3f, .05f, .1f);
            // Original (recording 2, 58.5-59.1 s): every shot flings a few thin needles of sand up out of the box's
            // mouth: stretched along their flight, no gravity, gone in ~0.1 s.
            spray = MakeSystem("Pour spray", Texture2D.whiteTexture, 300, SprayLife, SprayWidth, SprayWidth);
            var sm = spray.main; sm.gravityModifier = 0;
            var sr = spray.GetComponent<ParticleSystemRenderer>(); sr.renderMode = ParticleSystemRenderMode.Stretch; sr.velocityScale = .07f; sr.lengthScale = 1;
            var vm = voxels.main; vm.gravityModifier = .9f; vm.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
            var vrot = voxels.rotationOverLifetime; vrot.enabled = true; vrot.z = new ParticleSystem.MinMaxCurve(-6, 6);
            var vsize = voxels.sizeOverLifetime; vsize.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, .7f));
            voxels.GetComponent<ParticleSystemRenderer>().sortingOrder = 31;
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
            // A little sand cube: solid colour with a darker rim.
            voxel = new Texture2D(16, 16, TextureFormat.RGBA32, false) { name = "Fx voxel", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Point };
            for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++)
            {
                bool rim = x < 2 || y < 2 || x > 13 || y > 13;
                float shade = rim ? .62f : (x + y < 12 ? 1f : .9f);
                voxel.SetPixel(x, y, new Color(shade, shade, shade, 1));
            }
            voxel.Apply();
            int m = n * 2;
            for (int y = 0; y < m; y++) for (int x = 0; x < m; x++)
            {
                var p = new Vector2(x + .5f, y + .5f) / (m / 2f) - Vector2.one;
                float d = p.magnitude;
                // Translucent bubble: faint body, bright rim and a highlight in the upper left.
                float body = d < 1 ? .04f + .22f * d * d * d : 0;
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

        SceneActorView PartnerOf(SceneActorView actor)
        {
            foreach (var other in controller.Characters) if (other && other.Shooter == actor.Shooter.Partner) return other;
            return null;
        }
        // A locked waiting slot opening (its counter reached 0): a ring of white smoke bursts out around it.
        void SlotUnlocks(SandGame game)
        {
            var slots = controller.StashSlots;
            if (slotLocked.Length != game.Slots.Length) System.Array.Resize(ref slotLocked, game.Slots.Length); // a bought extra slot
            for (int i = 0; i < slotLocked.Length; i++)
            {
                int remaining = game.SlotRemaining(i);
                if (slotLocked[i] > 0 && remaining == 0 && i < slots.Length && slots[i])
                {
                    var p = slots[i].position + new Vector3(0, .28f, Front); // the slot's pivot sits at the tile's lower edge
                    for (int k = 0; k < 22; k++)
                    {
                        float a = k * Mathf.PI * 2 / 22;
                        var dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a) * .75f, 0);
                        Emit(puff, p + dir * .3f, dir * Random.Range(.6f, .9f), new Color(1, 1, 1, .9f), Random.Range(.5f, .7f), Random.Range(.45f, .6f));
                    }
                }
                slotLocked[i] = remaining;
            }
        }
        void RevealSmoke(Vector3 p)
        {
            for (int i = 0; i < 12; i++)
            {
                var v = Random.insideUnitCircle * .45f;
                Emit(puff, p + (Vector3)(Random.insideUnitCircle * .3f), new Vector3(v.x, .3f + Random.Range(0, .3f), 0), new Color(1, 1, 1, .85f), Random.Range(.8f, 1.2f), Random.Range(.55f, .8f));
            }
        }
        void ChainSnap(Vector3 p)
        {
            Emit(sparkle, p, Vector3.zero, new Color(1, 1, .85f, 1), 1.4f, .22f); // the glow
            for (int i = 0; i < 16; i++)
            {
                var v = Random.insideUnitCircle.normalized * Random.Range(3f, 4.5f);
                Emit(sparkle, p, v, Color.white, Random.Range(.09f, .14f), Random.Range(.16f, .26f));
            }
        }
        // A shot: a few needles of the box's sand fly up from its sand surface, fanning out a little.
        void Spray(SceneActorView actor, ActorState state, Color color)
        {
            if (state.Box == null) state.Box = actor.Visual.GetComponentsInChildren<MeshRenderer>();
            // The mouth from the box as drawn (its rig is pitched toward the camera, so its own "up" points at the
            // camera); the sand surface sinks with the fill. Sorting order draws the needles over the box.
            var box = state.Box.CombinedBounds();
            var mouth = new Vector3(box.center.x, box.max.y - box.size.y * (1 - actor.DisplayedFill) * .6f, box.center.z);
            Color shade = color * SprayShade; shade.a = 1;
            int n = Random.Range(SprayMin, SprayMax + 1);
            for (int i = 0; i < n; i++)
            {
                var v = new Vector3(Random.Range(-.35f, .35f), 1, 0).normalized * SpraySpeed * Random.Range(.7f, 1.15f);
                Emit(spray, mouth + new Vector3(Random.Range(-.15f, .15f), 0, 0), v, shade, SprayWidth, SprayLife * Random.Range(.7f, 1.2f));
            }
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
            SlotUnlocks(game);
            AnimateBubbles(dt);
            AnimateFlashes(dt);
        }

        void Reset(SandGame game)
        {
            lastGame = game;
            // Regions open at the start show no burst.
            regionShown = new bool[controller.Regions.Length];
            for (int i = 0; i < regionShown.Length; i++) regionShown[i] = controller.Regions[i] && game.Regions[controller.Regions[i].PartIndex].InformationVisible;
            slotLocked = new int[game.Slots.Length];
            for (int i = 0; i < slotLocked.Length; i++) slotLocked[i] = game.SlotRemaining(i);
            foreach (var pair in actors) if (pair.Key) pair.Key.transform.localScale = pair.Value.BaseScale;
            actors.Clear();
            bubblePool.ReleaseAll();
            dust.Clear(); sparkle.Clear(); puff.Clear(); voxels.Clear();
            flashPool.ReleaseAll();
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

        // Original (recording 2, 153.1 s): a finished region shows no burst of its own; every region it opens pops
        // at once, each with one glowing bubble on its counter and a shower of sand cubes in its own colour.
        void RegionBursts(SandGame game)
        {
            for (int i = 0; i < controller.Regions.Length; i++)
            {
                var view = controller.Regions[i];
                if (!view) continue;
                bool shown = game.Regions[view.PartIndex].InformationVisible;
                if (shown && !regionShown[i]) RegionOpens(view);
                regionShown[i] = shown;
            }
        }
        void RegionOpens(SceneRegionView view)
        {
            var center = view.Counter ? view.Counter.transform.position : view.Target.position;
            center.z = view.Board.transform.position.z + Front;
            Color color = view.SolidColor;
            SpawnFlash(center, 1.6f);
            SpawnBubble(center, BubbleSize * BubbleScale, 0);
            for (int v = 0; v < BurstGrains; v++)
            {
                // The cubes stay around the region, drifting up a little and falling back.
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad, speed = Random.Range(.3f, 1.4f);
                var vel = new Vector3(Mathf.Cos(angle) * speed, Mathf.Sin(angle) * speed + .9f, 0);
                Color vc = Random.value < ConfettiShare ? Confetti : Color.Lerp(color, Color.white, Random.Range(0f, .25f));
                Emit(voxels, center + (Vector3)(Random.insideUnitCircle * .4f), vel, vc, Random.Range(.06f, .1f), Random.Range(.6f, 1f));
            }
            for (int s = 0; s < BurstSparkles; s++)
            {
                var dir = Random.insideUnitCircle.normalized * Random.Range(.6f, 2f);
                Color sc = s % 3 == 0 ? Color.white : Color.Lerp(color, Color.white, Random.Range(0f, .35f));
                Emit(sparkle, center + (Vector3)(Random.insideUnitCircle * .2f), new Vector3(dir.x, dir.y + .6f, 0), sc, Random.Range(.07f, .15f), Random.Range(.4f, .8f));
            }
        }

        // Object pooling (Core/Patterns/ObjectPooling.cs): completion flashes and bubbles are reused instead of
        // created and destroyed each time. Both pools share one sprite each.
        Sprite flashSprite, bubbleSprite;
        Pool<Bubble> flashPoolValue, bubblePoolValue;
        Sprite SoftSprite { get { return flashSprite ? flashSprite : (flashSprite = Sprite.Create(softDot, new Rect(0, 0, softDot.width, softDot.height), new Vector2(.5f, .5f), softDot.width)); } }
        Pool<Bubble> flashPool { get { return flashPoolValue ?? (flashPoolValue = MakePool("Completion flash", 30, () => SoftSprite)); } }
        Pool<Bubble> bubblePool { get { return bubblePoolValue ?? (bubblePoolValue = MakePool("Completion bubble", 29, () => bubbleSprite ? bubbleSprite : (bubbleSprite = Sprite.Create(ring, new Rect(0, 0, ring.width, ring.height), new Vector2(.5f, .5f), ring.width)), true)); } }
        Pool<Bubble> MakePool(string name, int order, System.Func<Sprite> sprite, bool glow = false)
        {
            return new Pool<Bubble>(() =>
            {
                var root = new GameObject(name).transform; root.SetParent(transform, false);
                var disc = root.gameObject.AddComponent<SpriteRenderer>();
                disc.sprite = sprite(); disc.sharedMaterial = bubbleMaterial; disc.sortingOrder = order;
                var bubble = new Bubble { Root = root, Disc = disc };
                if (glow)
                {
                    // Soft white-cyan halo around the bubble (the original's bubbles glow beyond their rim).
                    bubble.Glow = new GameObject("Glow").AddComponent<SpriteRenderer>();
                    bubble.Glow.transform.SetParent(root, false); bubble.Glow.transform.localScale = Vector3.one * 1.7f;
                    bubble.Glow.sprite = SoftSprite; bubble.Glow.sharedMaterial = bubbleMaterial; bubble.Glow.sortingOrder = order - 1;
                }
                return bubble;
            }, b => b.Root ? b.Root.gameObject : null, 0, 64);
        }

        void SpawnFlash(Vector3 position, float size = 2.6f)
        {
            var f = flashPool.Get();
            f.Root.position = position + new Vector3(0, 0, -.02f);
            f.Age = 0; f.Life = .42f; f.Size = size;
        }
        void AnimateFlashes(float dt)
        {
            var flashes = flashPool.Active();
            for (int i = flashes.Count - 1; i >= 0; i--)
            {
                var f = flashes[i]; f.Age += dt;
                float t = f.Age / f.Life;
                if (t >= 1) { flashPool.ReleaseAt(i); continue; }
                f.Root.localScale = Vector3.one * f.Size * (.35f + .65f * (1 - (1 - t) * (1 - t)));
                f.Disc.color = new Color(.9f, 1f, 1f, Mathf.Clamp01(t * 8) * (1 - t * t));
            }
        }

        void SpawnBubble(Vector3 position, float size, float delay)
        {
            var b = bubblePool.Get();
            b.Root.position = position; b.Root.localScale = Vector3.zero;
            b.Disc.color = new Color(.75f, .95f, 1f, 0);
            b.Age = -delay; b.Life = BubbleLife; b.Size = size;
        }

        void AnimateBubbles(float dt)
        {
            var bubbles = bubblePool.Active();
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
                    bubblePool.ReleaseAt(i); continue;
                }
                float grow = 1 - Mathf.Pow(1 - Mathf.Clamp01(t * 1.6f), 3);
                b.Root.localScale = Vector3.one * b.Size * (.3f + .7f * grow + .08f * Mathf.Sin(t * 20));
                float alpha = Mathf.Clamp01(t * 6) * (1 - t * .3f);
                b.Disc.color = new Color(.75f, .95f, 1f, alpha);
                // The halo glows as the bubble appears, then only the clear bubble is left.
                if (b.Glow) b.Glow.color = new Color(.8f, .97f, 1f, alpha * .6f * (1 - t) * (1 - t));
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
                    state = new ActorState { Ammo = actor.Shooter.Ammo, BaseScale = actor.transform.localScale };
                    state.Hidden = game.IsHidden(actor.Shooter, actor.SourceLane);
                    actors.Add(actor, state);
                }
                // Mystery cube reaching the front of its lane: it shows its colour inside a soft white cloud.
                bool hidden = game.IsHidden(actor.Shooter, actor.SourceLane);
                if (state.Hidden && !hidden && actor.gameObject.activeInHierarchy) RevealSmoke(actor.transform.position + new Vector3(0, .35f, Front));
                state.Hidden = hidden;
                // Chained pair picked: the chain snaps with a white flash between the two cubes (shown once per pair).
                if (actor.Shooter.Partner != null)
                {
                    if (!state.Partner || state.Partner.Shooter != actor.Shooter.Partner) state.Partner = PartnerOf(actor);
                }
                else if (state.Partner)
                {
                    if (actor.GetInstanceID() < state.Partner.GetInstanceID()) ChainSnap((actor.transform.position + state.Partner.transform.position) * .5f + new Vector3(0, .45f, Front));
                    state.Partner = null;
                }
                if (!actor.gameObject.activeInHierarchy) continue;
                Color color = ColorPalette.Of(actor.Shooter.Color);
                // Recoil / landing squash now live in SceneActorView (Punch); only particles here.
                bool inSlot = System.Array.IndexOf(game.Slots, actor.Shooter) >= 0;
                if (inSlot && !actor.Departing && actor.Shooter.Ammo < state.Ammo) Spray(actor, state, color);
                state.Ammo = actor.Shooter.Ammo;
                // The original lands and leaves without puffs (recording 2, 155.9-156.1 s and 158.3-158.7 s); an emptied
                // box turns white itself (SceneActorView.EmptyWhite).
            }
        }

        void OnDestroy()
        {
            if (bubbleMaterial) Destroy(bubbleMaterial);
            if (flashSprite) Destroy(flashSprite); if (bubbleSprite) Destroy(bubbleSprite);
            flashPoolValue?.Dispose(); bubblePoolValue?.Dispose();
            foreach (var ps in new[] { dust, sparkle, puff, voxels, spray }) if (ps) Destroy(ps.GetComponent<ParticleSystemRenderer>().material);
        }
    }
}
