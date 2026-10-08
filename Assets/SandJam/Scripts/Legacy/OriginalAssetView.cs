using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SandJamTest
{
    // Portraits are rendered from the extracted skinned mesh and its original bone pose.
    // A small built-in shader replaces the unavailable original URP dissolve shader.
    public sealed class OriginalAssetView : IDisposable
    {
        [Serializable] sealed class PaletteFile { public PaletteEntry[] colors; }
        [Serializable] sealed class PaletteEntry { public int id; public float r, g, b; }
        readonly Dictionary<int, RenderTexture> portraits = new Dictionary<int, RenderTexture>();
        public Texture2D Shadow { get; private set; }
        public Texture2D PlayButton { get; private set; }
        public AudioClip SelectSound { get; private set; }
        public AudioClip CompleteSound { get; private set; }
        public AudioClip VictorySound { get; private set; }
        public int PortraitCount { get { return portraits.Count; } }

        public void Load()
        {
            Shadow = Resources.Load<Texture2D>("Original/Shadow");
            PlayButton = Resources.Load<Texture2D>("Original/PlayButton");
            SelectSound = Resources.Load<AudioClip>("Original/Select");
            CompleteSound = Resources.Load<AudioClip>("Original/RegionComplete");
            VictorySound = Resources.Load<AudioClip>("Original/Victory");
            var prefab = Resources.Load<GameObject>("Original/CharacterVisual");
            var shader = Resources.Load<Shader>("OriginalSand");
            var texture = Resources.Load<Texture2D>("Original/Sand");
            var paletteJson = Resources.Load<TextAsset>("Original/Palette");
            if (!prefab || !shader || !texture || !paletteJson || !Shadow || !PlayButton || !SelectSound || !CompleteSound || !VictorySound)
                throw new InvalidOperationException("Thiếu asset gốc cho bản thử.");
            var palette = JsonUtility.FromJson<PaletteFile>(paletteJson.text);
            GameObject model = null, cameraObject = null;
            Material material = null;
            Mesh baked = null;
            try
            {
                model = UnityEngine.Object.Instantiate(prefab);
                model.name = "Original character - portrait stage";
                model.transform.position = Vector3.zero;
                foreach (var tr in model.GetComponentsInChildren<Transform>(true)) tr.gameObject.layer = 30;
                var renderer = model.GetComponentInChildren<SkinnedMeshRenderer>(true);
                if (!renderer || !renderer.sharedMesh) throw new InvalidOperationException("Không tải được mesh nhân vật gốc.");
                material = new Material(shader) { mainTexture = texture };
                renderer.sharedMaterial = material;
                renderer.updateWhenOffscreen = true;
                baked = new Mesh();
                renderer.BakeMesh(baked);
                var vertices = baked.vertices;
                if (vertices.Length == 0) throw new InvalidOperationException("Mesh nhân vật không có đỉnh.");
                Bounds bounds = new Bounds(renderer.transform.TransformPoint(vertices[0]), Vector3.zero);
                for (int i = 1; i < vertices.Length; i++) bounds.Encapsulate(renderer.transform.TransformPoint(vertices[i]));
                // Bake the recovered pose once. Normalize the exported centimeter-scale
                // hierarchy before rendering to avoid clipping and floating-point loss.
                renderer.enabled = false;
                var filter = renderer.gameObject.AddComponent<MeshFilter>();
                filter.sharedMesh = baked;
                renderer.gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;
                model.transform.localScale *= 2f / Mathf.Max(.0001f, Mathf.Max(bounds.size.y, bounds.size.x));
                bounds = new Bounds(renderer.transform.TransformPoint(vertices[0]), Vector3.zero);
                for (int i = 1; i < vertices.Length; i++) bounds.Encapsulate(renderer.transform.TransformPoint(vertices[i]));
                cameraObject = new GameObject("Original asset preview camera");
                var camera = cameraObject.AddComponent<Camera>();
                camera.enabled = false;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0, 0, 0, 0);
                camera.cullingMask = 1 << 30;
                camera.orthographic = true;
                float size = Mathf.Max(bounds.size.y, bounds.size.x * 1.2f);
                camera.orthographicSize = size * .66f;
                camera.nearClipPlane = .01f;
                camera.farClipPlane = size * 20 + 10;
                camera.transform.position = bounds.center + new Vector3(0, size * .3f, -size * 3);
                camera.transform.LookAt(bounds.center);
                foreach (var color in palette.colors)
                {
                    material.color = new Color(color.r, color.g, color.b, 1);
                    var target = new RenderTexture(192, 224, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4, name = "Original portrait " + color.id };
                    target.Create();
                    portraits.Add(color.id, target);
                    camera.targetTexture = target;
                    camera.Render();
                }
                camera.targetTexture = null;
                Debug.Log("Original assets ready: " + portraits.Count + " portraits, " + vertices.Length + " vertices, " + renderer.bones.Length + " bones; bounds " + bounds.size);
            }
            finally
            {
                if (model) { model.SetActive(false); UnityEngine.Object.Destroy(model); }
                if (cameraObject) UnityEngine.Object.Destroy(cameraObject);
                if (material) UnityEngine.Object.Destroy(material);
                if (baked) UnityEngine.Object.Destroy(baked);
            }
        }

        public Texture Portrait(int color) { RenderTexture image; return portraits.TryGetValue(color, out image) ? image : null; }
        public void SavePreviews(string directory)
        {
            var previous = RenderTexture.active;
            try
            {
                foreach (var pair in portraits)
                {
                    RenderTexture.active = pair.Value;
                    var image = new Texture2D(pair.Value.width, pair.Value.height, TextureFormat.RGBA32, false);
                    image.ReadPixels(new Rect(0, 0, image.width, image.height), 0, 0); image.Apply();
                    int visible = 0;
                    foreach (var pixel in image.GetPixels32()) if (pixel.a > 30) visible++;
                    File.WriteAllBytes(Path.Combine(directory, "asset-character-" + pair.Key + ".png"), image.EncodeToPNG());
                    UnityEngine.Object.Destroy(image);
                    if (visible < 100) throw new InvalidOperationException("Original character rendered empty: " + pair.Key);
                }
            }
            finally { RenderTexture.active = previous; }
        }
        public void Dispose() { foreach (var image in portraits.Values) { image.Release(); UnityEngine.Object.Destroy(image); } portraits.Clear(); }
    }
}
