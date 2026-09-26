using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Marker for the designer-authored Level Selection layout.
    /// CanvasScaler is the only runtime scaling system. This class deliberately
    /// never changes RectTransforms so Scene view == Play Mode. Layout v8 keeps mission/hero artwork behind their decorative frames while preserving the approved geometry.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class LevelSelectionResponsiveLayout : MonoBehaviour
    {
        [SerializeField] private Vector2 referenceResolution = new Vector2(1080f, 1920f);
        [SerializeField] private int layoutVersion = 8;

        public Vector2 ReferenceResolution => referenceResolution;
        public int LayoutVersion => layoutVersion;

        private void OnEnable()
        {
            ApplyApprovedHudArtwork();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ApplyApprovedHudArtwork();
        }

        public void EditorConfigure(int version)
        {
            referenceResolution = new Vector2(1080f, 1920f);
            layoutVersion = version;
            ApplyApprovedHudArtwork();
        }
#endif

        private void ApplyApprovedHudArtwork()
        {
            Transform diamondCounter = transform.Find("Top HUD/Diamond Counter");
            if (diamondCounter == null)
                return;

            UnityEngine.UI.Image diamondBar = diamondCounter.GetComponent<UnityEngine.UI.Image>();
            if (diamondBar != null)
            {
                Sprite approvedDiamondBar = ProfessionalMainMenuEmbeddedAssets.GetSprite("diamond_bar");
                if (approvedDiamondBar != null)
                {
                    diamondBar.sprite = approvedDiamondBar;
                    diamondBar.color = Color.white;
                    diamondBar.preserveAspect = false;
                    diamondBar.enabled = true;
                }
            }

            // The approved diamond-bar artwork already contains the faceted blue diamond,
            // matching the Main Menu. Do not draw a second procedural icon on top.
            Transform extraDiamondIcon = diamondCounter.Find("Diamond Icon");
            if (extraDiamondIcon != null && extraDiamondIcon.gameObject.activeSelf)
                extraDiamondIcon.gameObject.SetActive(false);
        }
    }
}
