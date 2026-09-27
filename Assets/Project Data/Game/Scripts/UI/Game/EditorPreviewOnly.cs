using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Marks a sample object that only exists so the Game UI can be seen and edited in
    /// the Scene view (sample orders, sample power-up buttons). The real items are
    /// spawned at runtime, so the sample is removed as soon as Play mode starts.
    /// </summary>
    [DefaultExecutionOrder(-2000)]
    public sealed class EditorPreviewOnly : MonoBehaviour
    {
        private void Awake()
        {
            // Deactivate first so layout groups ignore it this frame.
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }
}
