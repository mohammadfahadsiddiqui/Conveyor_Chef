using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Marker for the designer-authored Conveyor Chef gameplay UI.
    ///
    /// Game.unity is authored at 1080x1920. CanvasScaler is the only system
    /// allowed to resize the layout. This component intentionally never moves,
    /// resizes, reparents or regenerates UI objects at runtime, so the Scene view
    /// remains the source of truth and matches Play Mode.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameUIResponsiveLayout : MonoBehaviour
    {
        [SerializeField] private Vector2 referenceResolution = new Vector2(1080f, 1920f);
        [SerializeField] private int layoutVersion = 1;

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
