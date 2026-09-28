using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon.BusStop
{
    /// <summary>
    /// The chapter bar at the bottom of the World Map: one card per continent with its map
    /// artwork, chapter number, name and level progress. The cards are real objects in
    /// WorldMap.unity (built by Conveyor Chef > World Map > Rebuild Chapter Bar), so their
    /// layout, text and art can be edited in the scene. At runtime this only applies the
    /// state: which card is selected, which continents are locked, and the progress bars.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldMapChapterStrip : MonoBehaviour
    {
        [Serializable]
        public sealed class ChapterCard
        {
            public Button button;
            public Image frame;
            public Image thumbBack;
            public Image thumbnail;
            public Image lockIcon;
            public Image numberBadge;
            public TMP_Text nameText;
            public GameObject progressRoot;
            public Image progressFill;
            public TMP_Text progressText;
            public TMP_Text lockedText;
        }

        [SerializeField] private ChapterCard[] cards = new ChapterCard[0];

        [Header("Card art")]
        [SerializeField] private Sprite cardSprite;
        [SerializeField] private Sprite selectedCardSprite;
        [SerializeField] private Sprite lockedCardSprite;

        [Header("Colours")]
        [SerializeField] private Color thumbBackColor = new Color32(150, 214, 240, 255);
        [SerializeField] private Color lockedThumbBackColor = new Color32(48, 82, 120, 255);
        [SerializeField] private Color lockedThumbnailTint = new Color(0.34f, 0.37f, 0.45f, 1f);
        [SerializeField] private Color nameColor = new Color32(92, 52, 24, 255);
        [SerializeField] private Color lockedNameColor = new Color32(196, 206, 222, 255);
        [SerializeField] private Color lockedBadgeTint = new Color(0.6f, 0.62f, 0.68f, 1f);
        [SerializeField] private Color progressColor = new Color32(64, 170, 64, 255);
        [SerializeField] private Color completeColor = new Color32(255, 196, 40, 255);

        [Header("Selection")]
        [SerializeField] private float selectedScale = 1.06f;

        private WorldMapSceneController owner;

        public int CardCount => cards != null ? cards.Length : 0;

        /// <summary>Hooks the card buttons to the controller (the old small cards are hidden).</summary>
        public void Bind(WorldMapSceneController controller, WorldMapContinentNode[] nodes)
        {
            owner = controller;
            if (cards == null)
                return;

            for (int i = 0; i < cards.Length; i++)
            {
                ChapterCard card = cards[i];
                if (card == null || card.button == null)
                    continue;

                WorldMapContinentNode node = nodes != null && i < nodes.Length ? nodes[i] : null;
                int serializedIndex = node != null ? node.ContinentIndex : i;

                card.button.onClick.RemoveAllListeners();
                card.button.onClick.AddListener(() =>
                {
                    if (owner != null)
                        owner.HandleContinentPressed(serializedIndex);
                });

                if (node != null && node.CardButton != null)
                    node.CardButton.gameObject.SetActive(false);
            }
        }

        public void Refresh(int selected)
        {
            if (cards == null || owner == null)
                return;

            for (int i = 0; i < cards.Length; i++)
            {
                ChapterCard card = cards[i];
                if (card == null)
                    continue;

                bool unlocked = owner.IsContinentUnlocked(i);
                bool isSelected = i == selected;

                if (card.frame != null)
                {
                    Sprite sprite = isSelected && selectedCardSprite != null ? selectedCardSprite
                        : unlocked ? cardSprite : lockedCardSprite;
                    if (sprite != null)
                        card.frame.sprite = sprite;
                    card.frame.rectTransform.localScale = isSelected ? new Vector3(selectedScale, selectedScale, 1f) : Vector3.one;
                }

                if (card.thumbBack != null)
                    card.thumbBack.color = unlocked ? thumbBackColor : lockedThumbBackColor;
                if (card.thumbnail != null)
                    card.thumbnail.color = unlocked ? Color.white : lockedThumbnailTint;
                if (card.lockIcon != null)
                    card.lockIcon.gameObject.SetActive(!unlocked);
                if (card.numberBadge != null)
                    card.numberBadge.color = unlocked ? Color.white : lockedBadgeTint;
                if (card.nameText != null)
                    card.nameText.color = unlocked ? nameColor : lockedNameColor;

                if (card.progressRoot != null)
                    card.progressRoot.SetActive(unlocked);
                if (card.lockedText != null)
                    card.lockedText.gameObject.SetActive(!unlocked);

                if (!unlocked)
                    continue;

                int done = owner.GetContinentLevelsCompleted(i);
                int total = Mathf.Max(1, owner.LevelsPerContinentCount);

                if (card.progressFill != null)
                {
                    float value = Mathf.Clamp01((float)done / total);
                    card.progressFill.gameObject.SetActive(value > 0f);
                    RectTransform fill = card.progressFill.rectTransform;
                    fill.anchorMax = new Vector2(Mathf.Max(value, 0.12f), fill.anchorMax.y);
                    card.progressFill.color = done >= total ? completeColor : progressColor;
                }

                if (card.progressText != null)
                    card.progressText.text = done + "/" + total;
            }
        }

#if UNITY_EDITOR
        public void EditorSetup(ChapterCard[] chapterCards, Sprite card, Sprite selected, Sprite locked)
        {
            cards = chapterCards;
            cardSprite = card;
            selectedCardSprite = selected;
            lockedCardSprite = locked;
        }
#endif
    }
}
