using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon.BusStop
{
    [DisallowMultipleComponent]
    public sealed class ContinentMapLevelNode : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image pinImage;
        [SerializeField] private Image currentGlow;
        [SerializeField] private TextMeshProUGUI levelLabel;
        [SerializeField] private TextMeshProUGUI starsLabel;
        [SerializeField] private GameObject completedBadge;

        [Header("State Art")]
        [SerializeField] private Sprite unlockedPinSprite;
        [SerializeField] private Sprite lockedPinSprite;

        private ContinentMapSceneController owner;
        private int globalLevelIndex;
        private int localLevelIndex;
        private bool unlocked;

        public int GlobalLevelIndex => globalLevelIndex;
        public int LocalLevelIndex => localLevelIndex;
        public RectTransform RectTransform => transform as RectTransform;

        public void Bind(
            ContinentMapSceneController controller,
            int globalIndex,
            int localIndex,
            string displayNumber,
            bool isUnlocked,
            bool isCompleted,
            int stars,
            bool isCurrent)
        {
            owner = controller;
            globalLevelIndex = globalIndex;
            localLevelIndex = localIndex;
            unlocked = isUnlocked;

            if (button == null)
                button = GetComponent<Button>();

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(HandlePressed);
                button.interactable = true;
            }

            if (levelLabel != null)
                levelLabel.text = displayNumber;

            if (starsLabel != null)
            {
                starsLabel.text = isCompleted
                    ? new string('★', Mathf.Clamp(stars, 0, 3)) + new string('☆', 3 - Mathf.Clamp(stars, 0, 3))
                    : (isUnlocked ? "PLAY" : "LOCKED");
            }

            if (completedBadge != null)
                completedBadge.SetActive(isCompleted);

            if (currentGlow != null)
                currentGlow.gameObject.SetActive(isCurrent && isUnlocked);

            if (pinImage != null)
            {
                Sprite target = isUnlocked ? unlockedPinSprite : lockedPinSprite;
                if (target != null)
                    pinImage.sprite = target;

                pinImage.color = isUnlocked
                    ? Color.white
                    : new Color(0.65f, 0.7f, 0.78f, 0.92f);
            }
        }

        private void HandlePressed()
        {
            if (owner == null)
                return;

            if (!unlocked)
            {
                owner.ShowLockedMessage(localLevelIndex);
                return;
            }

            owner.OpenLevel(globalLevelIndex, localLevelIndex);
        }

#if UNITY_EDITOR
        public void EditorConfigure(
            Button nodeButton,
            Image nodePin,
            Image glow,
            TextMeshProUGUI number,
            TextMeshProUGUI stars,
            GameObject completeBadge,
            Sprite unlockedSprite,
            Sprite lockedSprite)
        {
            button = nodeButton;
            pinImage = nodePin;
            currentGlow = glow;
            levelLabel = number;
            starsLabel = stars;
            completedBadge = completeBadge;
            unlockedPinSprite = unlockedSprite;
            lockedPinSprite = lockedSprite;
        }
#endif
    }
}
