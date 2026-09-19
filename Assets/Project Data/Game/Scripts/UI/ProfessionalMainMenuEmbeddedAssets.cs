using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Reconstructs the approved Conveyor Chef main-menu artwork from compressed
    /// text chunks stored in Resources. This keeps the generated binary artwork
    /// inside GitHub even when the repository connector cannot write binary files.
    /// </summary>
    public static class ProfessionalMainMenuEmbeddedAssets
    {
        private const string DataRoot = "ProfessionalMainMenuData/";
        private const int AtlasWidth = 1024;
        private const int AtlasHeight = 1222;

        private static Texture2D atlasTexture;
        private static Texture2D backgroundTexture;
        private static Sprite backgroundSprite;
        private static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

        private readonly struct AtlasRegion
        {
            public readonly int X;
            public readonly int YTop;
            public readonly int Width;
            public readonly int Height;

            public AtlasRegion(int x, int yTop, int width, int height)
            {
                X = x;
                YTop = yTop;
                Width = width;
                Height = height;
            }
        }

        private static readonly Dictionary<string, AtlasRegion> Regions = new Dictionary<string, AtlasRegion>(StringComparer.OrdinalIgnoreCase)
        {
            { "logo",         new AtlasRegion(6,   6,    390, 293) },
            { "chef",         new AtlasRegion(402, 6,    337, 450) },
            { "avatar",       new AtlasRegion(745, 6,    128, 128) },
            { "play",         new AtlasRegion(6,   462,  390, 131) },
            { "story",        new AtlasRegion(402, 462,  390, 131) },
            { "challenges",   new AtlasRegion(6,   599,  390, 131) },
            { "customize",    new AtlasRegion(402, 599,  390, 131) },
            { "settings",     new AtlasRegion(6,   736,  390, 131) },
            { "coin_bar",     new AtlasRegion(402, 736,  338, 113) },
            { "diamond_bar",  new AtlasRegion(6,   873,  338, 113) },
            { "star",         new AtlasRegion(350, 873,   96,  96) },
            { "shop",         new AtlasRegion(452, 873,  165, 165) },
            { "collection",   new AtlasRegion(623, 873,  165, 172) },
            { "achievements", new AtlasRegion(794, 873,  165, 165) },
            { "leaderboard",  new AtlasRegion(6,   1051, 165, 165) },
        };

        public static Sprite GetSprite(string assetName)
        {
            if (string.IsNullOrWhiteSpace(assetName))
                return null;

            if (spriteCache.TryGetValue(assetName, out Sprite cached))
                return cached;

            if (string.Equals(assetName, "background", StringComparison.OrdinalIgnoreCase))
            {
                EnsureBackground();
                return backgroundSprite;
            }

            if (!Regions.TryGetValue(assetName, out AtlasRegion region))
            {
                Debug.LogError("[ProfessionalMainMenu] Unknown embedded asset: " + assetName);
                return null;
            }

            EnsureAtlas();
            if (atlasTexture == null)
                return null;

            int unityY = AtlasHeight - region.YTop - region.Height;
            Rect rect = new Rect(region.X, unityY, region.Width, region.Height);

            Sprite sprite = Sprite.Create(
                atlasTexture,
                rect,
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect);

            sprite.name = "PMM_" + assetName;
            spriteCache[assetName] = sprite;
            return sprite;
        }

        private static void EnsureAtlas()
        {
            if (atlasTexture != null)
                return;

            byte[] bytes = DecodeChunks("atlas_", 6);
            if (bytes == null || bytes.Length == 0)
                return;

            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
            texture.name = "ProfessionalMainMenu_Atlas";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            if (!ImageConversion.LoadImage(texture, bytes, false))
            {
                UnityEngine.Object.Destroy(texture);
                Debug.LogError("[ProfessionalMainMenu] Failed to decode embedded UI atlas.");
                return;
            }

            atlasTexture = texture;
        }

        private static void EnsureBackground()
        {
            if (backgroundSprite != null)
                return;

            byte[] bytes = DecodeChunks("bg_", 4);
            if (bytes == null || bytes.Length == 0)
                return;

            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGB24, false, false);
            texture.name = "ProfessionalMainMenu_Background";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            if (!ImageConversion.LoadImage(texture, bytes, false))
            {
                UnityEngine.Object.Destroy(texture);
                Debug.LogError("[ProfessionalMainMenu] Failed to decode embedded menu background.");
                return;
            }

            backgroundTexture = texture;
            backgroundSprite = Sprite.Create(
                backgroundTexture,
                new Rect(0f, 0f, backgroundTexture.width, backgroundTexture.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect);

            backgroundSprite.name = "PMM_background";
            spriteCache["background"] = backgroundSprite;
        }

        private static byte[] DecodeChunks(string prefix, int count)
        {
            StringBuilder builder = new StringBuilder(count * 24000);

            for (int i = 0; i < count; i++)
            {
                string resourceName = DataRoot + prefix + i.ToString("00");
                TextAsset chunk = Resources.Load<TextAsset>(resourceName);

                if (chunk == null)
                {
                    Debug.LogError("[ProfessionalMainMenu] Missing embedded data chunk: " + resourceName);
                    return null;
                }

                builder.Append(chunk.text.Trim());
            }

            try
            {
                return Convert.FromBase64String(builder.ToString());
            }
            catch (FormatException ex)
            {
                Debug.LogError("[ProfessionalMainMenu] Corrupted embedded asset data: " + ex.Message);
                return null;
            }
        }
    }
}
