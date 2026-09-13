using System;
using UnityEngine;

namespace Watermelon.BusStop
{
    internal static class UIReferenceImageLoader
    {
        public static Texture2D LoadTexture(string resourcePath)
        {
            TextAsset encodedAsset = Resources.Load<TextAsset>(resourcePath);
            if (encodedAsset == null || string.IsNullOrWhiteSpace(encodedAsset.text))
            {
                Debug.LogWarning($"[UI Reference] Missing encoded artwork at Resources/{resourcePath}.");
                return null;
            }

            try
            {
                byte[] bytes = Convert.FromBase64String(encodedAsset.text.Trim());
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGB24, false);
                texture.name = resourcePath.Replace('/', '_');
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.filterMode = FilterMode.Bilinear;

                if (!texture.LoadImage(bytes, false))
                {
                    UnityEngine.Object.Destroy(texture);
                    Debug.LogWarning($"[UI Reference] Failed to decode artwork at Resources/{resourcePath}.");
                    return null;
                }

                return texture;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[UI Reference] Could not decode Resources/{resourcePath}: {exception.Message}");
                return null;
            }
        }
    }
}
