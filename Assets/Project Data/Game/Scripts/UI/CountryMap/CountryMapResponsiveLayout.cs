using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Marker/validator for the designer-authored Country Map layout.
    /// CanvasScaler is the only runtime scaling system. This class intentionally
    /// never changes RectTransforms, anchors, safe-area offsets or scale.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CountryMapResponsiveLayout : MonoBehaviour
    {
        [SerializeField] private Vector2 referenceResolution = new Vector2(1080f, 1920f);

        public Vector2 ReferenceResolution => referenceResolution;
    }
}
