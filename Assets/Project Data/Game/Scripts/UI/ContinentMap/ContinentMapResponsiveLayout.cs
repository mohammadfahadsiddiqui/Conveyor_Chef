using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Marker component for the serialized ContinentMap UI.
    /// The CanvasScaler stored in ContinentMap.unity is the only layout authority.
    /// No RectTransform is moved/resized here, so Scene/Simulator authoring matches Play mode.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ContinentMapResponsiveLayout : MonoBehaviour
    {
        [SerializeField] private Vector2 referenceResolution = new Vector2(1080f, 1920f);
        public Vector2 ReferenceResolution => referenceResolution;
    }
}
