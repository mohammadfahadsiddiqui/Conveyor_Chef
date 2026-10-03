using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// Loads this image's picture from Resources only when the image is shown, so the country
    /// pictures of LevelSelection.unity are not all loaded with the scene. The scene file stores
    /// no picture for it (the editor shows it as a preview and leaves it out when saving).
    /// To change the picture, assign another sprite on the Image: if it is inside a Resources
    /// folder it is loaded the same way, otherwise it is kept as a normal reference.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public sealed class LevelSelectionLazySprite : MonoBehaviour
    {
        [Tooltip("Resources path of the sprite, without extension, e.g. World/asia/china/level_1.")]
        [SerializeField] private string resourcePath = "";

        public string ResourcePath => resourcePath;

        /// <summary>Sprite shown as an editor preview (not saved).</summary>
        public Sprite Preview { get; private set; }

        private void OnEnable()
        {
            Load();
        }

        public void Load()
        {
            if (string.IsNullOrEmpty(resourcePath))
                return;

            Image image = GetComponent<Image>();
            if (image == null || image.sprite != null)
                return;

            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite == null)
            {
                Debug.LogWarning("[LevelSelection] Sprite not found in Resources: " + resourcePath, this);
                return;
            }

            image.sprite = sprite;
            if (!Application.isPlaying)
                Preview = sprite;
        }

#if UNITY_EDITOR
        public void EditorSetPath(string path)
        {
            resourcePath = path ?? string.Empty;
            Preview = null;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
