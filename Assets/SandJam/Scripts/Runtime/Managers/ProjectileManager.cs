using System;
using System.Collections.Generic;
using UnityEngine;

namespace SandJamTest.Scene3D
{
    // Sand shots as in the original (54 fps recording of level 268): while a cube pours it fires a small square
    // pellet of its colour about every 0.07 s; each pellet flies straight from the cube top to the region's pour
    // mouth in ~0.08 s, trailing a short pale streak. The falling thread inside the region is drawn by the region.
    public sealed class ProjectileManager
    {
        const float FireInterval = .07f, FlightTime = .08f, PelletSize = .075f, TrailLength = .9f;
        readonly Transform root;
        readonly Color[] colors;
        readonly int[] colorIds;
        readonly Material trailMaterial;
        static Sprite square;
        readonly List<Pellet> pellets = new List<Pellet>();
        readonly Dictionary<int, float> lastFire = new Dictionary<int, float>();
        float clock;

        sealed class Pellet
        {
            public GameObject Root; public SpriteRenderer Body; public LineRenderer Trail;
            public SceneRegionView Region; public Vector3 From; public float Age; public bool Live;
        }

        public ProjectileManager(Transform root, GameObject prefab, Material[] materials, int[] colorIds)
        {
            this.root = root; this.colorIds = colorIds;
            colors = new Color[materials.Length];
            for (int i = 0; i < materials.Length; i++) colors[i] = materials[i].color;
            trailMaterial = new Material(Shader.Find("Sprites/Default"));
        }

        public void Dispose() { if (trailMaterial) UnityEngine.Object.Destroy(trailMaterial); }

        public void Reset()
        {
            foreach (var p in pellets) { p.Live = false; p.Root.SetActive(false); }
            lastFire.Clear();
        }

        static Sprite Square()
        {
            if (square) return square;
            var t = Texture2D.whiteTexture;
            square = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(.5f, .5f), t.width);
            return square;
        }

        Pellet Take()
        {
            foreach (var free in pellets) if (!free.Live) return free;
            var go = new GameObject("Sand pellet"); go.transform.SetParent(root, false);
            var body = new GameObject("Body").AddComponent<SpriteRenderer>();
            body.transform.SetParent(go.transform, false); body.sprite = Square(); body.sortingOrder = 31;
            body.transform.localScale = Vector3.one * PelletSize;
            var trail = go.AddComponent<LineRenderer>();
            trail.useWorldSpace = true; trail.positionCount = 2; trail.sharedMaterial = trailMaterial;
            trail.startWidth = PelletSize * .9f; trail.endWidth = PelletSize * .2f; trail.numCapVertices = 0; trail.sortingOrder = 30;
            trail.startColor = new Color(1, 1, 1, .55f); trail.endColor = new Color(1, 1, 1, 0);
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; trail.receiveShadows = false;
            var p = new Pellet { Root = go, Body = body, Trail = trail };
            pellets.Add(p);
            return p;
        }

        // Called for every pour tick; pellets are emitted at the original cadence regardless of tick size.
        public void Spawn(SceneActorView actor, SceneRegionView region, int color, int slot = 0)
        {
            float last;
            if (lastFire.TryGetValue(slot, out last) && clock - last < FireInterval) return;
            lastFire[slot] = clock;
            var p = Take();
            p.Region = region; p.From = actor.AimPoint; p.Age = 0; p.Live = true;
            int index = Array.IndexOf(colorIds, color);
            p.Body.color = index >= 0 ? colors[index] : Color.white;
            p.Root.SetActive(true);
            Place(p);
        }

        void Place(Pellet p)
        {
            var to = p.Region.Target.position;
            float t = Mathf.Clamp01(p.Age / FlightTime);
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
            foreach (var p in pellets)
            {
                if (!p.Live) continue;
                p.Age += delta;
                if (p.Age >= FlightTime || !p.Region) { p.Live = false; p.Root.SetActive(false); continue; }
                Place(p);
            }
        }
    }
}
