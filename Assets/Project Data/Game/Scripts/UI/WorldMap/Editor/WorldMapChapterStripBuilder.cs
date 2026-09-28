#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Watermelon.BusStop;

namespace Watermelon.EditorTools
{
    /// <summary>
    /// Builds the World Map chapter bar as real, editable objects inside
    /// ChapterSelector/SelectorPanel in WorldMap.unity and saves the scene. Runs by itself
    /// once when WorldMap.unity is opened without a bar; "Rebuild Chapter Bar" replaces it
    /// (scene edits to the bar are lost on a rebuild).
    /// </summary>
    [InitializeOnLoad]
    public static class WorldMapChapterStripBuilder
    {
        private const string ScenePath = "Assets/Project Data/Game/Scenes/WorldMap.unity";
        private const string ArtFolder = "Assets/Project Data/Game/Images/WorldMap/ChapterBar/";
        private const string FontPath = "Assets/Project Data/Game/Fonts/FredokaOne/Fredoka One 120/FredokaOne 120.asset";
        private const string LockPath = "Assets/Project Data/Game/Images/MenuPanels/lock_badge.png";
        private const string StripName = "Chapter Strip";
        private const float SliceMultiplier = 2f;   // the bar sprites are drawn at 2x

        private static readonly string[] ShortNames = { "ASIA", "N. AMERICA", "S. AMERICA", "EUROPE", "AFRICA", "OCEANIA" };

        private static bool queued;

        static WorldMapChapterStripBuilder()
        {
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            Queue();
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode) => Queue();

        private static void Queue()
        {
            if (queued)
                return;
            queued = true;
            EditorApplication.delayCall += TryAutoBuild;
        }

        private static void TryAutoBuild()
        {
            queued = false;

            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                Queue();
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                return;

            if (Object.FindFirstObjectByType<WorldMapChapterStrip>(FindObjectsInactive.Include) != null)
                return;

            if (Build() != null)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("[World Map] Chapter bar built into WorldMap.unity (Canvas > ... > ChapterSelector > SelectorPanel > Chapter Strip).");
            }
        }

        [MenuItem("Conveyor Chef/World Map/Rebuild Chapter Bar", priority = 40)]
        public static void RebuildMenu()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    return;
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            WorldMapChapterStrip existing = Object.FindFirstObjectByType<WorldMapChapterStrip>(FindObjectsInactive.Include);
            if (existing != null)
                Undo.DestroyObjectImmediate(existing.gameObject);

            GameObject built = Build();
            if (built == null)
                return;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = built;
            EditorGUIUtility.PingObject(built);
        }

        [MenuItem("Conveyor Chef/World Map/Select Chapter Bar", priority = 41)]
        public static void SelectMenu()
        {
            WorldMapChapterStrip strip = Object.FindFirstObjectByType<WorldMapChapterStrip>(FindObjectsInactive.Include);
            if (strip == null)
            {
                Debug.LogWarning("[World Map] No chapter bar in the open scene. Open WorldMap.unity or use Rebuild Chapter Bar.");
                return;
            }

            Selection.activeGameObject = strip.gameObject;
            EditorGUIUtility.PingObject(strip.gameObject);
        }

        private static GameObject Build()
        {
            WorldMapSceneController controller = Object.FindFirstObjectByType<WorldMapSceneController>(FindObjectsInactive.Include);
            List<WorldMapContinentNode> nodes = new List<WorldMapContinentNode>(
                Object.FindObjectsByType<WorldMapContinentNode>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            if (controller == null || nodes.Count == 0)
            {
                Debug.LogWarning("[World Map] WorldMapSceneController or continents not found; chapter bar not built.");
                return null;
            }

            // Same order the controller uses at runtime (Asia first).
            nodes.Sort((a, b) => WorldMapSceneController.GetCanonicalContinentIndex(a).CompareTo(WorldMapSceneController.GetCanonicalContinentIndex(b)));

            Transform selector = null;
            foreach (WorldMapContinentNode node in nodes)
            {
                if (node.CardButton != null)
                {
                    selector = node.CardButton.transform.parent;
                    break;
                }
            }
            if (selector == null)
            {
                Debug.LogWarning("[World Map] ChapterSelector not found; chapter bar not built.");
                return null;
            }

            Sprite panelSprite = LoadSprite("chapter_bar_panel");
            Sprite cardSprite = LoadSprite("chapter_card");
            Sprite selectedSprite = LoadSprite("chapter_card_selected");
            Sprite lockedSprite = LoadSprite("chapter_card_locked");
            Sprite thumbSprite = LoadSprite("chapter_thumb_back");
            Sprite badgeSprite = LoadSprite("chapter_number_badge");
            Sprite trackSprite = LoadSprite("chapter_progress_track");
            Sprite fillSprite = LoadSprite("chapter_progress_fill");
            Sprite lockSprite = AssetDatabase.LoadAssetAtPath<Sprite>(LockPath);
            MenuUI.Font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

            Transform panel = selector.Find("SelectorPanel");
            RectTransform parent = (RectTransform)(panel != null ? panel : selector);

            // The old panel art only fills the lower part of its image: use the full rounded panel.
            Image panelImage = panel != null ? panel.GetComponent<Image>() : null;
            if (panelImage != null && panelSprite != null)
            {
                Undo.RecordObject(panelImage, "Chapter bar panel");
                panelImage.sprite = panelSprite;
                panelImage.type = Image.Type.Sliced;
                panelImage.pixelsPerUnitMultiplier = SliceMultiplier;
                panelImage.preserveAspect = false;
                panelImage.color = Color.white;
            }

            RectTransform root = MenuUI.Rect(StripName, parent);
            MenuUI.Stretch(root, 22f, 20f, 22f, 20f);
            HorizontalLayoutGroup layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            WorldMapChapterStrip.ChapterCard[] cards = new WorldMapChapterStrip.ChapterCard[nodes.Count];
            for (int i = 0; i < nodes.Count; i++)
            {
                cards[i] = BuildCard(root, i, nodes[i], cardSprite, thumbSprite, badgeSprite, trackSprite, fillSprite, lockSprite);

                if (nodes[i].CardButton != null)
                {
                    Undo.RecordObject(nodes[i].CardButton.gameObject, "Hide old chapter card");
                    nodes[i].CardButton.gameObject.SetActive(false);
                }
            }

            WorldMapChapterStrip strip = root.gameObject.AddComponent<WorldMapChapterStrip>();
            strip.EditorSetup(cards, cardSprite, selectedSprite, lockedSprite);

            SerializedObject so = new SerializedObject(controller);
            SerializedProperty property = so.FindProperty("chapterStrip");
            if (property != null)
            {
                property.objectReferenceValue = strip;
                so.ApplyModifiedProperties();
            }

            // Show the first card selected and open, the way the game starts.
            PreviewState(cards, cardSprite, selectedSprite, lockedSprite);

            Undo.RegisterCreatedObjectUndo(root.gameObject, "Build chapter bar");
            return root.gameObject;
        }

        private static WorldMapChapterStrip.ChapterCard BuildCard(Transform parent, int index, WorldMapContinentNode node,
            Sprite cardSprite, Sprite thumbSprite, Sprite badgeSprite, Sprite trackSprite, Sprite fillSprite, Sprite lockSprite)
        {
            WorldMapChapterStrip.ChapterCard card = new WorldMapChapterStrip.ChapterCard();

            // Holder keeps the layout slot; the card inside scales up when selected.
            RectTransform holder = MenuUI.Rect("Chapter " + (index + 1), parent);
            holder.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            card.frame = Sliced(MenuUI.Image("Card", holder, cardSprite, null, false));
            MenuUI.Stretch(card.frame.rectTransform);
            card.frame.raycastTarget = true;
            card.button = card.frame.gameObject.AddComponent<Button>();
            card.button.targetGraphic = card.frame;
            card.frame.gameObject.AddComponent<MenuButtonPress>();
            RectTransform cardRect = card.frame.rectTransform;

            // Map artwork thumbnail.
            card.thumbBack = Sliced(MenuUI.Image("Thumb Back", cardRect, thumbSprite, new Color32(150, 214, 240, 255), false));
            RectTransform thumbRect = card.thumbBack.rectTransform;
            thumbRect.anchorMin = new Vector2(0f, 1f);
            thumbRect.anchorMax = new Vector2(1f, 1f);
            thumbRect.pivot = new Vector2(0.5f, 1f);
            thumbRect.offsetMin = new Vector2(8f, -108f);
            thumbRect.offsetMax = new Vector2(-8f, -8f);

            card.thumbnail = MenuUI.Image("Thumbnail", thumbRect, node.ArtworkSprite);
            MenuUI.Stretch(card.thumbnail.rectTransform, 4f, 4f, 4f, 4f);

            card.lockIcon = MenuUI.Image("Lock", thumbRect, lockSprite);
            MenuUI.Place(card.lockIcon.rectTransform, Vector2.zero, new Vector2(78f, 63f));

            // Chapter number badge.
            card.numberBadge = Sliced(MenuUI.Image("Number Badge", cardRect, badgeSprite, null, false));
            MenuUI.Anchor(card.numberBadge.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(18f, -16f), new Vector2(40f, 40f));
            TextMeshProUGUI number = MenuUI.Text("Number", card.numberBadge.transform, (index + 1).ToString(), 26f, new Color32(90, 40, 5, 255));
            MenuUI.Stretch(number.rectTransform);

            // Name.
            string name = index < ShortNames.Length ? ShortNames[index] : node.ContinentName.ToUpperInvariant();
            TextMeshProUGUI nameText = MenuUI.Text("Name", cardRect, name, 24f, MenuUI.TextDark);
            BottomBand(nameText.rectTransform, 44f, 78f, 4f);
            card.nameText = nameText;

            // Progress bar (open continents) and LOCKED label (locked ones) share the bottom band.
            Image track = Sliced(MenuUI.Image("Progress", cardRect, trackSprite, null, false));
            BottomBand(track.rectTransform, 10f, 40f, 10f);
            card.progressRoot = track.gameObject;

            card.progressFill = Sliced(MenuUI.Image("Fill", track.transform, fillSprite, MenuUI.Green, false));
            RectTransform fillRect = card.progressFill.rectTransform;
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0.3f, 1f);
            fillRect.offsetMin = new Vector2(3f, 3f);
            fillRect.offsetMax = new Vector2(-3f, -3f);

            TextMeshProUGUI progressText = MenuUI.Text("Value", track.transform, "0/15", 20f, new Color32(60, 40, 20, 255));
            progressText.enableAutoSizing = false;
            MenuUI.Stretch(progressText.rectTransform);
            card.progressText = progressText;

            TextMeshProUGUI lockedText = MenuUI.Text("Locked", cardRect, "LOCKED", 20f, new Color32(196, 206, 222, 255));
            BottomBand(lockedText.rectTransform, 10f, 40f, 10f);
            card.lockedText = lockedText;

            return card;
        }

        private static void PreviewState(WorldMapChapterStrip.ChapterCard[] cards, Sprite card, Sprite selected, Sprite locked)
        {
            for (int i = 0; i < cards.Length; i++)
            {
                bool open = i == 0;
                cards[i].frame.sprite = open ? selected : locked;
                cards[i].frame.rectTransform.localScale = open ? new Vector3(1.06f, 1.06f, 1f) : Vector3.one;
                cards[i].thumbBack.color = open ? new Color32(150, 214, 240, 255) : new Color32(48, 82, 120, 255);
                cards[i].thumbnail.color = open ? Color.white : new Color(0.34f, 0.37f, 0.45f, 1f);
                cards[i].lockIcon.gameObject.SetActive(!open);
                cards[i].numberBadge.color = open ? Color.white : new Color(0.6f, 0.62f, 0.68f, 1f);
                cards[i].nameText.color = open ? (Color)MenuUI.TextDark : new Color32(196, 206, 222, 255);
                cards[i].progressRoot.SetActive(open);
                cards[i].lockedText.gameObject.SetActive(!open);
            }
        }

        private static void BottomBand(RectTransform rect, float bottom, float top, float side)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(side, bottom);
            rect.offsetMax = new Vector2(-side, top);
        }

        private static Image Sliced(Image image)
        {
            if (image.sprite != null && image.sprite.border != Vector4.zero)
            {
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = SliceMultiplier;
            }
            return image;
        }

        private static Sprite LoadSprite(string name)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + name + ".png");
            if (sprite == null)
                Debug.LogWarning("[World Map] Missing chapter bar sprite: " + ArtFolder + name + ".png");
            return sprite;
        }
    }
}
#endif
