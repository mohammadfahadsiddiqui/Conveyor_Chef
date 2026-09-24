using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Final-build marker for the authored Main Menu.
    ///
    /// menu.unity is the single source of truth for all RectTransforms.
    /// This component deliberately performs NO runtime/editor layout mutation:
    /// - no resize
    /// - no re-anchor
    /// - no safe-area offset
    /// - no local-scale changes
    /// - no duplicate/fullscreen backdrop creation
    ///
    /// The serialized Canvas/CanvasScaler is responsible for device scaling, so
    /// the composition seen in the Unity scene remains the composition used in Play Mode/build.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MainMenuResponsiveLayout : MonoBehaviour
    {
        [SerializeField] private Vector2 referenceResolution = new Vector2(1080f, 1920f);

        public Vector2 ReferenceResolution => referenceResolution;
    }
}
