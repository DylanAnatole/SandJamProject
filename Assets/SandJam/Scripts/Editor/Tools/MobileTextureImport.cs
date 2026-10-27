using UnityEditor;
using UnityEngine;

namespace SandJamTest.Editor
{
    // Phones get compressed textures: without a platform override Android / iOS builds kept the art uncompressed
    // (RGBA32: a 2000x1500 logo is 11 MB of GPU memory, ASTC 4x4 under 3 MB) and every popup background loaded at
    // start-up. ASTC 4x4 keeps UI gradients clean at a quarter of the size. Pixel art (point filtering) and textures
    // read at runtime (Read/Write) stay as they are, and an override set by hand is never replaced.
    sealed class MobileTextureImport : AssetPostprocessor
    {
        static readonly string[] Platforms = { "Android", "iPhone" };

        public override uint GetVersion() { return 1; }

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ProjectPaths.Root)) return;
            var importer = (TextureImporter)assetImporter;
            if (importer.filterMode == FilterMode.Point || importer.isReadable) return;
            foreach (var platform in Platforms)
            {
                var settings = importer.GetPlatformTextureSettings(platform);
                if (settings.overridden) continue;
                settings.overridden = true;
                settings.maxTextureSize = importer.maxTextureSize;
                settings.format = TextureImporterFormat.ASTC_4x4;
                importer.SetPlatformTextureSettings(settings);
            }
        }
    }
}
