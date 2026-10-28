using System;
using System.Collections.Generic;
using UnityEngine;

namespace SandJamTest.Scene3D
{
    // Sand shots as in the original (54 fps recording of level 268): while a cube pours it fires a small square
    // pellet of its colour about every 0.115 s (recording 2, 57.64-58.33 s: seven pellets 0.11-0.12 s apart); each
    // pellet flies straight from the cube top to the region's pour mouth, trailing a short pale streak. The falling thread inside the region is drawn by the region.
    // Read as one steady stream, not a stutter: the cadence keeps its own beat (pours tick every 0.02 s, so timing
    // each pellet from the tick that fired it gave 0.12 / 0.14 s gaps), each pellet keeps the mouth it was fired at
    // (the mouth follows the pour column and dragged pellets sideways in flight), and it eases into the
    // mouth over 0.13 s, a little longer than a beat, so the next one is already out before it lands (0.08 s at an
    // even speed left 30% of every beat empty and jumped ~1 unit a frame).
    public sealed class ProjectileManager
    {
        const float FireInterval = .115f, FlightTime = .13f, PelletSize = .075f, TrailLength = .9f;
        readonly Transform root;
        readonly Color[] colors;
        readonly int[] colorIds;
        readonly Material trailMaterial;
        // Object pooling (Core/Patterns/ObjectPooling.cs): pellets in flight are the pool's active items.
        readonly Pool<Pellet> pool;
        readonly Dictionary<int, float> lastFire = new Dictionary<int, float>();
        float clock;

        sealed class Pellet
        {
            public GameObject Root; public SpriteRenderer Body; public LineRenderer Trail;
            public SceneRegionView Region; public Vector3 From, To; public float Age;
        }

        public ProjectileManager(Transform root, GameObject prefab, Material[] materials, int[] colorIds)
        {
            this.root = root; this.colorIds = colorIds;
            colors = new Color[materials.Length];
            for (int i = 0; i < materials.Length; i++) colors[i] = materials[i].color;
            trailMaterial = new Material(Shader.Find("Sprites/Default"));
            pool = new Pool<Pellet>(Create, p => p.Root, 16, 64);
        }

        public void Dispose() { pool.Dispose(); if (trailMaterial) UnityEngine.Object.Destroy(trailMaterial); }

        public void Reset()
        {
            pool.ReleaseAll();
            lastFire.Clear();
        }

        Pellet Create()
        {
            var go = new GameObject("Sand pellet"); go.transform.SetParent(root, false);
            var body = new GameObject("Body").AddComponent<SpriteRenderer>();
            body.transform.SetParent(go.transform, false); body.sprite = SpriteFactory.White; body.sortingOrder = 31;
            body.transform.localScale = Vector3.one * PelletSize;
            var trail = go.AddComponent<LineRenderer>();
            trail.useWorldSpace = true; trail.positionCount = 2; trail.sharedMaterial = trailMaterial;
            trail.startWidth = PelletSize * .9f; trail.endWidth = PelletSize * .2f; trail.numCapVertices = 0; trail.sortingOrder = 30;
            trail.startColor = new Color(1, 1, 1, .55f); trail.endColor = new Color(1, 1, 1, 0);
            trail.NoShadows();
            return new Pellet { Root = go, Body = body, Trail = trail };
        }

        // Called for every pour tick; pellets are emitted at the original cadence regardless of tick size.
        public void Spawn(SceneActorView actor, SceneRegionView region, int color, int slot = 0)
        {
            float last;
            bool pouring = lastFire.TryGetValue(slot, out last) && clock - last < FireInterval * 2;
            if (pouring && clock - last < FireInterval) return;
            // On the beat while the pour goes on; a new pour starts its own beat now.
            float beat = pouring ? last + FireInterval : clock;
            lastFire[slot] = beat;
            var p = pool.Get();
            // Already as far along as its beat (pours tick every 0.02 s, a little after the beat).
            p.Region = region; p.From = actor.AimPoint; p.To = region.Target.position; p.Age = clock - beat;
            int index = Array.IndexOf(colorIds, color);
            p.Body.color = index >= 0 ? colors[index] : Color.white;
            p.Root.SetActive(true);
            Place(p);
        }

        void Place(Pellet p)
        {
            var to = p.To;
            float t = Mathf.Clamp01(p.Age / FlightTime);
            t = 1 - (1 - t) * (1 - t); // eases into the mouth
            var pos = Vector3.Lerp(p.From, to, t);
            pos.z = Mathf.Min(p.From.z, to.z) - .4f; // in front of the board and the cubes
            p.Body.transform.position = pos;
            var dir = (to - p.From); dir.z = 0;
            float travelled = dir.magnitude * t;
            var tail = pos - dir.normalized * Mathf.Min(TrailLength, travelled);
            p.Trail.SetPosition(0, pos); p.Trail.SetPosition(1, new Vector3(tail.x, tail.y, pos.z));
        }

        public void Advance(float delta)
        {
            clock += delta;
            var pellets = pool.Active();
            for (int i = pellets.Count - 1; i >= 0; i--)
            {
                var p = pellets[i];
                p.Age += delta;
                if (p.Age >= FlightTime || !p.Region) { pool.ReleaseAt(i); continue; }
                Place(p);
            }
        }
    }
}
