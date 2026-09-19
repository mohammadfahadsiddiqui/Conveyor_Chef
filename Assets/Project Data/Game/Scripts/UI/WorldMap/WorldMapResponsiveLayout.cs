using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// Marker/validator for the designer-authored World Map layout.
    ///
    /// IMPORTANT:
    /// Unity's serialized CanvasScaler is the ONLY system allowed to size the UI.
    /// This component deliberately does not change RectTransforms, anchors, scale,
    /// CanvasScaler mode, safe-area offsets, or create runtime backdrop objects.
    ///
    /// That makes:
    ///   Scene/Simulator before Play == Simulator after Play
    ///
    /// The Canvas is authored at 1080x1920 with Scale With Screen Size, so Unity's
    /// Simulator already previews the same responsive calculation used at runtime.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldMapResponsiveLayout : MonoBehaviour
    {
        [SerializeField] private Vector2 referenceResolution = new Vector2(1080f, 1920f);

        public Vector2 ReferenceResolution => referenceResolution;

        // Kept so older serialized builder data remains valid.
        public void EditorConfigure(Image sourceBackground)
        {
            // Intentionally empty. Background artwork is serialized in WorldMap.unity.
        }
    }
}
