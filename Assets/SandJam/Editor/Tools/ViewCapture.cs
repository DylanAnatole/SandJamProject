using System.IO;
using System.Linq;
using UnityEngine;

namespace SandJamTest.Editor
{
    // Renders every enabled camera (stage + UI overlay) in depth order into one PNG for review.
    public static class ViewCapture
    {
        public static string Capture(string path, int width = 540, int height = 1200)
        {
            var rt = RenderTexture.GetTemporary(width, height, 24);
            var cameras = Object.FindObjectsOfType<Camera>().Where(c => c.enabled && c.gameObject.activeInHierarchy).OrderBy(c => c.depth).ToArray();
            foreach (var cam in cameras)
            {
                var previous = cam.targetTexture;
                cam.targetTexture = rt; cam.Render(); cam.targetTexture = previous;
            }
            var active = RenderTexture.active; RenderTexture.active = rt;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0); texture.Apply();
            RenderTexture.active = active; RenderTexture.ReleaseTemporary(rt);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            return Path.GetFullPath(path);
        }
    }
}
