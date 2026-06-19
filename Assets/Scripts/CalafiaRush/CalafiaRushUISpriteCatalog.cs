using System;
using System.Collections.Generic;
using UnityEngine;

namespace CalafiaRush
{
    [CreateAssetMenu(
        fileName = "CalafiaRushUISpriteCatalog",
        menuName = "Calafia Rush/UI Sprite Catalog")]
    public sealed class CalafiaRushUISpriteCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class NamedSprite
        {
            public string name;
            public Sprite sprite;
        }

        [SerializeField] private List<NamedSprite> sprites = new List<NamedSprite>();

        private Dictionary<string, Sprite> _lookup;

        public IReadOnlyList<NamedSprite> Sprites => sprites;

        public void SetSprites(IReadOnlyDictionary<string, Sprite> spriteMap)
        {
            sprites.Clear();
            foreach (var pair in spriteMap)
            {
                sprites.Add(new NamedSprite { name = pair.Key, sprite = pair.Value });
            }

            _lookup = null;
        }

        public Sprite Get(string spriteName)
        {
            if (string.IsNullOrEmpty(spriteName))
            {
                return null;
            }

            EnsureLookup();
            return _lookup.TryGetValue(spriteName, out var sprite) ? sprite : null;
        }

        public bool TryGet(string spriteName, out Sprite sprite)
        {
            EnsureLookup();
            return _lookup.TryGetValue(spriteName, out sprite);
        }

        private void EnsureLookup()
        {
            if (_lookup != null)
            {
                return;
            }

            _lookup = new Dictionary<string, Sprite>(sprites.Count, StringComparer.Ordinal);
            foreach (var entry in sprites)
            {
                if (entry?.sprite == null || string.IsNullOrEmpty(entry.name))
                {
                    continue;
                }

                _lookup[entry.name] = entry.sprite;
            }
        }
    }
}
