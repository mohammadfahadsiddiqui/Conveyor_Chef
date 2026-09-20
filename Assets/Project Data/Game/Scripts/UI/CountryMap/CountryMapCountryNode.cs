using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon.BusStop
{
    /// <summary>
    /// Behaviour-only component for one designer-authored country node.
    /// It never creates, destroys, moves or resizes UI. The hierarchy saved in
    /// CountryMap.unity remains authoritative in Edit mode and Play mode.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CountryMapCountryNode : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private int countryIndex;
        [SerializeField] private string countryName;

        [Header("Interaction")]
        [SerializeField] private Button button;

        [Header("Visuals")]
        [SerializeField] private Image landmarkImage;
        [SerializeField] private Image flagImage;
        [SerializeField] private Image selectionGlow;
        [SerializeField] private Image countryLabelFrame;
        [SerializeField] private TextMeshProUGUI countryNameText;
        [SerializeField] private Image progressFrame;
        [SerializeField] private Image starImage;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private GameObject lockedOverlay;
        [SerializeField] private GameObject completedBadge;

        private CountryMapSceneController owner;

        public int CountryIndex => countryIndex;
        public string CountryName => countryName;
        public RectTransform MapTarget => transform as RectTransform;

        public void Bind(CountryMapSceneController controller)
        {
            owner = controller;

            if (button == null)
                return;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandlePressed);
        }

        public void Refresh(bool unlocked, bool selected, int completedLevels, int levelsPerCountry)
        {
            completedLevels = Mathf.Clamp(completedLevels, 0, Mathf.Max(1, levelsPerCountry));
            bool completed = completedLevels >= levelsPerCountry;

            if (countryNameText != null)
                countryNameText.text = countryName;

            if (progressText != null)
                progressText.text = completedLevels + "/" + levelsPerCountry;

            if (selectionGlow != null)
                selectionGlow.gameObject.SetActive(selected && unlocked);

            if (lockedOverlay != null)
                lockedOverlay.SetActive(!unlocked);

            if (completedBadge != null)
                completedBadge.SetActive(completed);

            Color normal = Color.white;
            Color locked = new Color(0.58f, 0.64f, 0.72f, 0.9f);

            if (landmarkImage != null)
                landmarkImage.color = unlocked ? normal : locked;

            if (flagImage != null)
                flagImage.color = unlocked ? normal : new Color(0.72f, 0.76f, 0.82f, 0.92f);

            if (countryLabelFrame != null)
                countryLabelFrame.color = unlocked ? normal : new Color(0.80f, 0.84f, 0.9f, 0.95f);

            if (progressFrame != null)
                progressFrame.color = unlocked ? normal : new Color(0.68f, 0.72f, 0.8f, 0.95f);

            if (starImage != null)
                starImage.color = unlocked ? normal : new Color(0.7f, 0.7f, 0.74f, 0.9f);

            // Locked countries stay clickable so the player gets an explanation.
            if (button != null)
                button.interactable = true;
        }

        private void HandlePressed()
        {
            if (owner != null)
                owner.HandleCountryPressed(countryIndex);
        }

#if UNITY_EDITOR
        public void EditorConfigure(
            int index,
            string displayName,
            Button clickButton,
            Image landmark,
            Image flag,
            Image glow,
            Image labelFrame,
            TextMeshProUGUI nameText,
            Image progressBackground,
            Image progressStar,
            TextMeshProUGUI progressLabel,
            GameObject lockRoot,
            GameObject completedRoot)
        {
            countryIndex = index;
            countryName = displayName;
            button = clickButton;
            landmarkImage = landmark;
            flagImage = flag;
            selectionGlow = glow;
            countryLabelFrame = labelFrame;
            countryNameText = nameText;
            progressFrame = progressBackground;
            starImage = progressStar;
            progressText = progressLabel;
            lockedOverlay = lockRoot;
            completedBadge = completedRoot;
        }
#endif
    }
}
