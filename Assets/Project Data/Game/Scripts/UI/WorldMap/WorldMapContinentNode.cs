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
                mapButton.onClick.RemoveAllListeners();
                mapButton.onClick.AddListener(HandlePressed);
            }

            if (cardButton != null)
            {
                cardButton.onClick.RemoveAllListeners();
                cardButton.onClick.AddListener(HandlePressed);
            }
        }

        public void Refresh(bool unlocked, bool selected)
        {
            // Runtime owns progression state only. Text, colors, sizes and positions
            // remain exactly as authored in WorldMap.unity so Scene edits are never
            // overwritten after the player scrolls or changes selection.

            if (pinImage != null)
            {
                Sprite targetPin = unlocked ? unlockedPinSprite : lockedPinSprite;
                if (targetPin != null && pinImage.sprite != targetPin)
                    pinImage.sprite = targetPin;
            }

            if (cardFrameImage != null)
            {
                Sprite targetCard = unlocked ? activeCardSprite : lockedCardSprite;
                if (targetCard != null && cardFrameImage.sprite != targetCard)
                    cardFrameImage.sprite = targetCard;
            }

            if (glowImage != null)
                glowImage.gameObject.SetActive(selected && unlocked);

            // Do NOT modify:
            // continentImage.color
            // mapLabel.text / mapLabel.color
            // cardLabel.text / cardLabel.color
            // Those values belong to the serialized designer-authored scene.
        }

        private void HandlePressed()
        {
            // Explicit Unity null check; null-conditional does not detect
            // destroyed UnityEngine.Object instances.
            if (owner != null)
                owner.HandleContinentPressed(continentIndex);
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
