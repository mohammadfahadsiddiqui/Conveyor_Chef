using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Marker for the designer-authored Level Selection layout.
    /// CanvasScaler is the only runtime scaling system. This class deliberately
    /// never changes RectTransforms so Scene view == Play Mode. Layout v7 keeps the level-card strip aligned to the approved Level Selection reference.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LevelSelectionResponsiveLayout : MonoBehaviour
    {
        [SerializeField] private Vector2 referenceResolution = new Vector2(1080f, 1920f);
        [SerializeField] private int layoutVersion = 7;

        public Vector2 ReferenceResolution => referenceResolution;
        public int LayoutVersion => layoutVersion;

#if UNITY_EDITOR
        public void EditorConfigure(int version)
        {
            referenceResolution = new Vector2(1080f, 1920f);
            layoutVersion = version;
        }
#endif
    }
}
