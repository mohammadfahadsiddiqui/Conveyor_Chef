using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// Keeps the main menu responsive without overwriting the designer-authored
    /// RectTransform values saved in menu.unity.
    ///
    /// IMPORTANT:
    /// - Child positions, sizes, anchors and scales are NEVER recalculated here.
    /// - The scene hierarchy is the single source of truth for menu layout.
    /// - CanvasScaler handles resolution scaling automatically.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class MainMenuResponsiveLayout : MonoBehaviour
    {
        private static readonly Vector2 ReferenceResolution = new Vector2(1080f, 1920f);

        [Header("Resolution Scaling")]
        [SerializeField] private bool configureCanvasScaler = true;

        private void OnEnable()
        {
            ConfigureCanvasScalerOnly();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ConfigureCanvasScalerOnly();
        }
#endif

        /// <summary>
        /// Intentionally does not touch any child RectTransform.
        /// Whatever is arranged and saved in menu.unity remains exactly the
        /// runtime layout.
        /// </summary>
        private void ConfigureCanvasScalerOnly()
        {
            if (!configureCanvasScaler)
                return;

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
                return;

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
                return;

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }
    }
}
