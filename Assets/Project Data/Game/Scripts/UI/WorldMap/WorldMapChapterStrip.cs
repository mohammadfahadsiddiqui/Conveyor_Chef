using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon.BusStop
{
    /// <summary>
    /// The chapter bar at the bottom of the World Map: one card per continent with its map
    /// artwork, chapter number, short name and level progress. Locked continents show a
    /// darkened card with a lock; the selected continent gets a gold frame and stands out.
    /// Built at runtime inside the scene's SelectorPanel, replacing the small text cards;
    /// pressing a card works exactly like the old cards (select, and open if unlocked).
    /// </summary>
    public sealed class WorldMapChapterStrip : MonoBehaviour
    {
        private static readonly string[] ShortNames = { "ASIA", "N. AMERICA", "S. AMERICA", "EUROPE", "AFRICA", "OCEANIA" };

        private static readonly Color32 LockedFill = new Color32(64, 78, 104, 255);
        private static readonly Color32 LockedBorder = new Color32(36, 46, 66, 255);
        private static readonly Color32 LockedText = new Color32(196, 206, 222, 255);

        private sealed class Card
        {
            public RectTransform Root;
            public Image Frame;
            public Image Thumbnail;
            public Image ThumbBack;
            public Image Lock;
            public TextMeshProUGUI Name;
            public RectTransform Progress;
            public Image NumberBadge;
        }

        private WorldMapSceneController owner;
        private WorldMapContinentNode[] nodes;
        private Card[] cards;
        private MenuPanelArt art;

        public static WorldMapChapterStrip Create(WorldMapSceneController controller, WorldMapContinentNode[] continentNodes)
        {
            if (continentNodes == null || continentNodes.Length == 0)
                return null;

            // The old cards sit in the ChapterSelector next to its SelectorPanel.
            Transform selector = null;
            foreach (WorldMapContinentNode node in continentNodes)
            {
                if (node != null && node.CardButton != null)
                {
                    selector = node.CardButton.transform.parent;
                    break;
                }
            }
            if (selector == null)
                return null;

            Transform panel = selector.Find("SelectorPanel");
            RectTransform parent = (RectTransform)(panel != null ? panel : selector);

            // The panel art only fills the lower part of its image, so the bar gets its own
            // full-size rounded panel (deep blue with a gold rim) for the taller cards.
            Image panelImage = panel != null ? panel.GetComponent<Image>() : null;
            if (panelImage != null)
            {
                panelImage.sprite = MenuUI.Rounded(40, 6, new Color32(24, 74, 168, 245), new Color32(255, 196, 40, 255));
                panelImage.type = Image.Type.Sliced;
                panelImage.preserveAspect = false;
                panelImage.color = Color.white;
            }

            RectTransform root = MenuUI.Rect("Chapter Strip", parent);
            MenuUI.Stretch(root, 22f, 20f, 22f, 20f);
            HorizontalLayoutGroup layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            WorldMapChapterStrip strip = root.gameObject.AddComponent<WorldMapChapterStrip>();
            strip.Build(controller, continentNodes);
            return strip;
        }

        private void Build(WorldMapSceneController controller, WorldMapContinentNode[] continentNodes)
        {
            owner = controller;
            nodes = continentNodes;
            art = Resources.Load<MenuPanelArt>(MenuPanelArt.ResourcePath);
            if (art != null && MenuUI.Font == null)
                MenuUI.Font = art.font;

            cards = new Card[nodes.Length];
            for (int i = 0; i < nodes.Length; i++)
            {
                WorldMapContinentNode node = nodes[i];

                // The new card replaces the old one (its button still exists for the controller).
                if (node != null && node.CardButton != null)
                    node.CardButton.gameObject.SetActive(false);

                cards[i] = BuildCard(i, node);
            }
        }

        private Card BuildCard(int index, WorldMapContinentNode node)
        {
            Card card = new Card();

            // Holder keeps the layout slot; the frame inside can scale up when selected.
            RectTransform holder = MenuUI.Rect("Chapter " + (index + 1), transform);
            holder.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            card.Frame = MenuUI.Image("Card", holder, MenuUI.Rounded(24, 4, MenuUI.CardFill, MenuUI.CardBorder), null, false);
            card.Root = card.Frame.rectTransform;
            MenuUI.Stretch(card.Root);

            int serializedIndex = node != null ? node.ContinentIndex : index;
            MenuUI.Button(card.Frame, () => owner.HandleContinentPressed(serializedIndex));

            // Map artwork thumbnail.
            Image thumbBack = MenuUI.Image("Thumb Back", card.Root, MenuUI.Rounded(18, 0, new Color32(150, 214, 240, 255), Color.clear), null, false);
            thumbBack.rectTransform.anchorMin = new Vector2(0f, 1f);
            thumbBack.rectTransform.anchorMax = new Vector2(1f, 1f);
            thumbBack.rectTransform.offsetMin = new Vector2(8f, -108f);
            thumbBack.rectTransform.offsetMax = new Vector2(-8f, -8f);

            card.ThumbBack = thumbBack;
            card.Thumbnail = MenuUI.Image("Thumbnail", thumbBack.transform, node != null ? node.ArtworkSprite : null);
            MenuUI.Stretch(card.Thumbnail.rectTransform, 4f, 4f, 4f, 4f);

            card.Lock = MenuUI.Image("Lock", thumbBack.transform, art != null ? art.lockBadge : null);
            MenuUI.Place(card.Lock.rectTransform, Vector2.zero, new Vector2(78f, 63f));

            // Chapter number badge on the thumbnail corner.
            card.NumberBadge = MenuUI.Image("Number", card.Root, MenuUI.Rounded(20, 3, new Color32(255, 196, 40, 255), new Color32(150, 80, 10, 255)), null, false);
            MenuUI.Anchor(card.NumberBadge.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(18f, -16f), new Vector2(40f, 40f));
            TextMeshProUGUI number = MenuUI.Text("Value", card.NumberBadge.transform, (index + 1).ToString(), 26f, new Color32(90, 40, 5, 255));
            MenuUI.Stretch(number.rectTransform);

            // Name and progress.
            string shortName = index < ShortNames.Length ? ShortNames[index] : (node != null ? node.ContinentName : "CHAPTER " + (index + 1));
            card.Name = MenuUI.Text("Name", card.Root, shortName, 24f, MenuUI.TextDark);
            card.Name.rectTransform.anchorMin = new Vector2(0f, 0f);
            card.Name.rectTransform.anchorMax = new Vector2(1f, 0f);
            card.Name.rectTransform.pivot = new Vector2(0.5f, 0f);
            card.Name.rectTransform.offsetMin = new Vector2(4f, 44f);
            card.Name.rectTransform.offsetMax = new Vector2(-4f, 78f);

            card.Progress = MenuUI.Rect("Progress Slot", card.Root);
            card.Progress.anchorMin = new Vector2(0f, 0f);
            card.Progress.anchorMax = new Vector2(1f, 0f);
            card.Progress.pivot = new Vector2(0.5f, 0f);
            card.Progress.offsetMin = new Vector2(10f, 10f);
            card.Progress.offsetMax = new Vector2(-10f, 40f);

            return card;
        }

        public void Refresh(int selected)
        {
            if (cards == null)
                return;

            for (int i = 0; i < cards.Length; i++)
            {
                Card card = cards[i];
                if (card == null)
                    continue;

                bool unlocked = owner.IsContinentUnlocked(i);
                bool isSelected = i == selected;

                Color fill = unlocked ? (isSelected ? MenuUI.CardHighlight : MenuUI.CardFill) : (Color)LockedFill;
                Color border = isSelected ? MenuUI.Gold : unlocked ? MenuUI.CardBorder : (Color)LockedBorder;
                card.Frame.sprite = MenuUI.Rounded(24, isSelected ? 7 : 4, fill, border);
                card.Root.localScale = isSelected ? new Vector3(1.06f, 1.06f, 1f) : Vector3.one;

                card.Thumbnail.color = unlocked ? Color.white : new Color(0.34f, 0.37f, 0.45f, 1f);
                card.ThumbBack.color = unlocked ? Color.white : new Color(0.32f, 0.38f, 0.5f, 1f);
                card.Lock.gameObject.SetActive(!unlocked);
                card.NumberBadge.color = unlocked ? Color.white : new Color(0.6f, 0.62f, 0.68f, 1f);
                card.Name.color = unlocked ? MenuUI.TextDark : (Color)LockedText;

                // Progress: a small bar for open continents, "LOCKED" for the rest.
                for (int c = card.Progress.childCount - 1; c >= 0; c--)
                {
                    Transform child = card.Progress.GetChild(c);
                    child.SetParent(null, false);
                    Destroy(child.gameObject);
                }

                if (unlocked)
                {
                    int done = owner.GetContinentLevelsCompleted(i);
                    int total = owner.LevelsPerContinentCount;
                    RectTransform bar = MenuUI.ProgressBar(card.Progress, (float)done / total, done >= total ? MenuUI.Gold : MenuUI.Green, done + "/" + total);
                    MenuUI.Stretch(bar);
                    TextMeshProUGUI label = bar.GetComponentInChildren<TextMeshProUGUI>();
                    if (label != null)
                    {
                        label.enableAutoSizing = false;
                        label.fontSize = 20f;
                    }
                }
                else
                {
                    TextMeshProUGUI locked = MenuUI.Text("Locked", card.Progress, "LOCKED", 20f, LockedText);
                    MenuUI.Stretch(locked.rectTransform);
                }
            }
        }
    }
}
