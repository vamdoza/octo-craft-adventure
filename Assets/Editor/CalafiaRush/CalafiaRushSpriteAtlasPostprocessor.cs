using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace CalafiaRush.Editor
{
    public sealed class CalafiaRushSpriteAtlasPostprocessor : AssetPostprocessor
    {
        private static readonly (string atlasPath, string manifestPath)[] AtlasEntries =
        {
            ("Assets/Sprites/calafia-rush-sprite-atlas.png", "Assets/Sprites/calafia-rush-sprites.json"),
            ("Assets/Sprites/calafia-rush-sprite-atlas2.png", "Assets/Sprites/calafia-rush-sprites-atlas2.json")
        };

        private static bool _isApplyingSlices;

        private void OnPreprocessTexture()
        {
            if (!TryGetManifestPath(assetPath, out _))
            {
                return;
            }

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.alphaIsTransparency = true;
            importer.spritePixelsToUnits = 100;
            importer.wrapMode = TextureWrapMode.Clamp;
        }

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            var atlasImported = false;
            var manifestImported = false;
            foreach (var path in importedAssets)
            {
                if (TryGetManifestPath(path, out _))
                {
                    atlasImported = true;
                }
                else if (IsManifestPath(path))
                {
                    manifestImported = true;
                }
            }

            if (!atlasImported && !manifestImported)
            {
                return;
            }

            if (!_isApplyingSlices)
            {
                ApplySpriteSlices();
            }

            CalafiaRushUIAssetBuilder.RebuildCatalog();
        }

        [MenuItem("Calafia Rush/Reimport Sprite Atlases")]
        internal static void ApplySpriteSlices()
        {
            if (_isApplyingSlices)
            {
                return;
            }

            _isApplyingSlices = true;
            try
            {
                foreach (var entry in AtlasEntries)
                {
                    ApplySpriteSlices(entry.atlasPath, entry.manifestPath);
                }
            }
            finally
            {
                _isApplyingSlices = false;
            }
        }

        private static void ApplySpriteSlices(string atlasPath, string manifestPath)
        {
            if (!File.Exists(manifestPath))
            {
                Debug.LogWarning("Calafia Rush sprite manifest not found at " + manifestPath);
                return;
            }

            var manifest = JsonUtility.FromJson<SpriteManifest>(File.ReadAllText(manifestPath));
            if (manifest?.sprites == null || manifest.sprites.Length == 0)
            {
                Debug.LogWarning("Calafia Rush sprite manifest is empty: " + manifestPath);
                return;
            }

            var importer = AssetImporter.GetAtPath(atlasPath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
            dataProvider.InitSpriteEditorDataProvider();

            var spriteRects = new List<SpriteRect>(manifest.sprites.Length);
            var nameFileIdPairs = new List<SpriteNameFileIdPair>(manifest.sprites.Length);
            foreach (var definition in manifest.sprites)
            {
                var border = definition.border ?? EmptyBorder;
                var unityY = manifest.atlasHeight - definition.y - definition.h;
                var spriteRect = new SpriteRect
                {
                    name = definition.name,
                    spriteID = StableSpriteId(atlasPath + ":" + definition.name),
                    rect = new Rect(definition.x, unityY, definition.w, definition.h),
                    alignment = SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                    border = new Vector4(border[0], border[1], border[2], border[3])
                };

                spriteRects.Add(spriteRect);
                nameFileIdPairs.Add(new SpriteNameFileIdPair(definition.name, spriteRect.spriteID));
            }

            dataProvider.SetSpriteRects(spriteRects.ToArray());
            var nameFileIdProvider = dataProvider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            nameFileIdProvider.SetNameFileIdPairs(nameFileIdPairs);
            dataProvider.Apply();
            importer.SaveAndReimport();
            Debug.Log("Calafia Rush sliced " + Path.GetFileName(atlasPath) + " into " + spriteRects.Count + " sprites.");
        }

        private static bool TryGetManifestPath(string assetPath, out string manifestPath)
        {
            foreach (var entry in AtlasEntries)
            {
                if (entry.atlasPath == assetPath)
                {
                    manifestPath = entry.manifestPath;
                    return true;
                }
            }

            manifestPath = null;
            return false;
        }

        private static bool IsManifestPath(string assetPath)
        {
            foreach (var entry in AtlasEntries)
            {
                if (entry.manifestPath == assetPath)
                {
                    return true;
                }
            }

            return false;
        }

        private static GUID StableSpriteId(string key)
        {
            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes("calafia-rush-" + key));
            var builder = new StringBuilder(32);
            foreach (var value in hash)
            {
                builder.Append(value.ToString("x2"));
            }

            return new GUID(builder.ToString());
        }

        private static readonly int[] EmptyBorder = { 0, 0, 0, 0 };

        [Serializable]
        private sealed class SpriteManifest
        {
            public int atlasHeight = 1024;
            public SpriteDefinition[] sprites;
        }

        [Serializable]
        private sealed class SpriteDefinition
        {
            public string name;
            public int x;
            public int y;
            public int w;
            public int h;
            public int[] border = { 0, 0, 0, 0 };
        }
    }
}
