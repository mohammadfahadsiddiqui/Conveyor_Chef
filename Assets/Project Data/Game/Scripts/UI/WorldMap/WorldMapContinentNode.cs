using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon.BusStop
{
    [DisallowMultipleComponent]
    public sealed class WorldMapContinentNode : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private int continentIndex;
        [SerializeField] private string continentName;

        [Header("Map")]
        [SerializeField] private Image continentImage;
        [SerializeField] private Button mapButton;
        [SerializeField] private Image pinImage;
        [SerializeField] private Image glowImage;
        [SerializeField] private TextMeshProUGUI mapLabel;

        [Header("Chapter Card")]
        [SerializeField] private Button cardButton;
        [SerializeField] private Image cardFrameImage;
        [SerializeField] private TextMeshProUGUI cardLabel;

        [Header("State Art")]
        [SerializeField] private Sprite unlockedPinSprite;
        [SerializeField] private Sprite lockedPinSprite;
        [SerializeField] private Sprite activeCardSprite;
        [SerializeField] private Sprite lockedCardSprite;

        private WorldMapSceneController owner;

        public int ContinentIndex => continentIndex;
        public string ContinentName => continentName;
        public RectTransform MapTarget => transform as RectTransform;

        public void Bind(WorldMapSceneController controller)
        {
            owner = controller;

            if (mapButton != null)
            {
                mapButton.interactable = true;
                mapButton.onClick.RemoveAllListeners();
                mapButton.onClick.AddListener(HandlePressed);
            }

            if (cardButton != null)
            {
                cardButton.interactable = true;
                cardButton.onClick.RemoveAllListeners();
                cardButton.onClick.AddListener(HandlePressed);
            }
        }

        public void Refresh(bool unlocked, bool selected)
        {
            if (pinImage != null)
            {
                Sprite targetPin = unlocked ? unlockedPinSprite : lockedPinSprite;
                if (targetPin != null)
                    pinImage.sprite = targetPin;
            }

            if (cardFrameImage != null)
            {
                Sprite targetCard = unlocked ? activeCardSprite : lockedCardSprite;
                if (targetCard != null)
                    cardFrameImage.sprite = targetCard;
                cardFrameImage.color = selected && unlocked
                    ? Color.white
                    : (unlocked ? new Color(0.86f, 0.9f, 1f, 0.9f) : Color.white);
            }

            if (glowImage != null)
                glowImage.gameObject.SetActive(selected && unlocked);

            if (continentImage != null)
            {
                continentImage.color = unlocked
                    ? Color.white
                    : new Color(0.55f, 0.62f, 0.72f, 0.82f);
            }

            if (mapLabel != null)
            {
                mapLabel.text = continentName;
                mapLabel.color = unlocked ? Color.white : new Color(0.82f, 0.86f, 0.93f, 1f);
            }

            if (cardLabel != null)
            {
                cardLabel.text = "CHAPTER " + (continentIndex + 1) + "\n" + continentName.ToUpperInvariant();
                cardLabel.color = unlocked ? new Color(0.08f, 0.17f, 0.35f, 1f) : new Color(0.78f, 0.82f, 0.9f, 1f);
            }
        }

        private void HandlePressed()
        {
            owner?.HandleContinentPressed(continentIndex);
        }

#if UNITY_EDITOR
        public void EditorConfigure(
            int index,
            string displayName,
            Image continent,
            Button map,
            Image pin,
            Image glow,
            TextMeshProUGUI label,
            Sprite unlockedPin,
            Sprite lockedPin,
            Sprite activeCard,
            Sprite lockedCard)
        {
            continentIndex = index;
            continentName = displayName;
            continentImage = continent;
            mapButton = map;
            pinImage = pin;
            glowImage = glow;
            mapLabel = label;
            unlockedPinSprite = unlockedPin;
            lockedPinSprite = lockedPin;
            activeCardSprite = activeCard;
            lockedCardSprite = lockedCard;
        }

        public void EditorAssignCard(Button button, Image frame, TextMeshProUGUI label)
        {
            cardButton = button;
            cardFrameImage = frame;
            cardLabel = label;
        }
#endif
    }
}
