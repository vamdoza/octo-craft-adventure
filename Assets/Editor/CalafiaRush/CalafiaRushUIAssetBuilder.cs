using System.Collections.Generic;
using System.IO;
using System.Linq;
using CalafiaRush;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CalafiaRush.Editor
{
    public static class CalafiaRushUIAssetBuilder
    {
        private const string AtlasPath = "Assets/Sprites/calafia-rush-sprite-atlas.png";
        private const string Atlas2Path = "Assets/Sprites/calafia-rush-sprite-atlas2.png";
        private const string CatalogPath = "Assets/Sprites/CalafiaRushUISpriteCatalog.asset";
        private const string PrefabPath = "Assets/Prefabs/CalafiaRushUIMockup.prefab";

        [MenuItem("Calafia Rush/Rebuild UI Assets")]
        public static void RebuildAll()
        {
            CalafiaRushSpriteAtlasPostprocessor.ApplySpriteSlices();
            RebuildCatalog();
            RebuildPrefab();
        }

        [MenuItem("Calafia Rush/Rebuild Sprite Catalog")]
        public static void RebuildCatalog()
        {
            var sprites = LoadAtlasSprites();
            if (sprites.Count == 0)
            {
                Debug.LogWarning("No Calafia Rush atlas sprites found. Reimport the atlas first.");
                return;
            }

            var catalog = AssetDatabase.LoadAssetAtPath<CalafiaRushUISpriteCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<CalafiaRushUISpriteCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.SetSprites(sprites);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log("Updated Calafia Rush sprite catalog with " + sprites.Count + " sprites.");
        }

        [MenuItem("Calafia Rush/Rebuild UI Mockup Prefab")]
        public static void RebuildPrefab()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CalafiaRushUISpriteCatalog>(CatalogPath);
            if (catalog == null)
            {
                RebuildCatalog();
                catalog = AssetDatabase.LoadAssetAtPath<CalafiaRushUISpriteCatalog>(CatalogPath);
            }

            if (catalog == null)
            {
                Debug.LogError("Unable to build Calafia Rush UI mockup prefab without a sprite catalog.");
                return;
            }

            var root = new GameObject(
                "CalafiaRushUIMockup",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(CalafiaRushUIMockupView));

            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var view = root.GetComponent<CalafiaRushUIMockupView>();
            view.Configure(catalog, rebuildOnAwake: false);
            view.BuildMockup(root.GetComponent<RectTransform>());

            EnsureDirectory(Path.GetDirectoryName(PrefabPath));
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);

            Debug.Log("Saved Calafia Rush UI mockup prefab to " + PrefabPath + ".", prefab);
        }

        internal static Dictionary<string, Sprite> LoadAtlasSprites()
        {
            var sprites = new Dictionary<string, Sprite>();
            MergeAtlasSprites(sprites, AtlasPath);
            MergeAtlasSprites(sprites, Atlas2Path);
            return sprites;
        }

        private static void MergeAtlasSprites(Dictionary<string, Sprite> sprites, string atlasPath)
        {
            foreach (var sprite in AssetDatabase.LoadAllAssetsAtPath(atlasPath).OfType<Sprite>())
            {
                if (sprites.ContainsKey(sprite.name))
                {
                    Debug.LogWarning(
                        "Calafia Rush sprite name collision: '" + sprite.name +
                        "' in " + atlasPath + " overrides an earlier atlas entry.");
                }

                sprites[sprite.name] = sprite;
            }
        }

        private static void EnsureDirectory(string directory)
        {
            if (string.IsNullOrEmpty(directory) || Directory.Exists(directory))
            {
                return;
            }

            Directory.CreateDirectory(directory);
            AssetDatabase.Refresh();
        }
    }
}
