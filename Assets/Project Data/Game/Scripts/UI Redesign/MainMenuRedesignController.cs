using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Watermelon.BusStop
{
    /// <summary>
    /// Runtime presentation layer for the Conveyor Chef main menu.
    /// Keeps the existing sceneloading component responsible for navigation.
    /// </summary>
    public sealed class MainMenuRedesignController : MonoBehaviour
    {
        private const string RootName = "CC_MainMenu_Redesign";
        private const string SoundPrefKey = "CC_SOUND_MUTED";

        private sceneloading sceneLoader;
        private Canvas legacyCanvas;
        private TMP_FontAsset font;
        private Sprite panelSprite;
        private Sprite buttonSprite;
        private Sprite settingsSprite;

        private RectTransform safeArea;
        private Rect lastSafeArea;
        private GameObject settingsPopup;
        private TMP_Text soundLabel;
        private RectTransform logoRoot;
        private RectTransform heroRoot;
        private RectTransform playRoot;
        private CanvasGroup logoGroup;
        private CanvasGroup heroGroup;
        private CanvasGroup playGroup;
        private bool initialised;

        public void Initialise(sceneloading loader)
        {
            if (initialised || loader == null)
                return;

            initialised = true;
            sceneLoader = loader;

            GameObject oldRoot = GameObject.Find(RootName);
            if (oldRoot != null)
                Destroy(oldRoot);

            CaptureLegacyAssets();
            EnsureEventSystem();
            BuildMenu();
            ApplyStoredSoundState();

            if (legacyCanvas != null)
                legacyCanvas.gameObject.SetActive(false);

            StartCoroutine(EntranceRoutine());
            StartCoroutine(HeroIdleRoutine());
            StartCoroutine(PlayPulseRoutine());
        }

        private void Update()
        {
            if (safeArea != null && Screen.safeArea != lastSafeArea)
                ApplySafeArea();
        }

        private void CaptureLegacyAssets()
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject[] roots = scene.GetRootGameObjects();

            foreach (GameObject root in roots)
            {
                Canvas[] canvases = root.GetComponentsInChildren<Canvas>(true);
                if (legacyCanvas == null && canvases.Length > 0)
                    legacyCanvas = canvases[0];

                foreach (TextMeshProUGUI text in root.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    if (font == null && text.font != null)
                        font = text.font;
                }

                foreach (UnityEngine.UI.Image image in root.GetComponentsInChildren<UnityEngine.UI.Image>(true))
                {
                    string n = image.name.ToLowerInvariant();
                    if (buttonSprite == null && n.Contains("play") && image.sprite != null)
                        buttonSprite = image.sprite;

                    if (panelSprite == null && (n.Contains("panel") || n.Contains("background")) && image.sprite != null)
                        panelSprite = image.sprite;

                    if (settingsSprite == null && (n.Contains("setting") || n.Contains("gear")) && image.sprite != null)
                        settingsSprite = image.sprite;
                }
            }
        }

        private void BuildMenu()
        {
            GameObject root = new GameObject(RootName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.layer = LayerMask.NameToLayer("UI");

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform canvasRect = root.GetComponent<RectTransform>();
            BuildBackground(canvasRect);

            safeArea = CreateRect("SafeArea", canvasRect);
            ApplySafeArea();

            BuildHeader();
            BuildHero();
            BuildPlayArea();
            BuildFooter();
            BuildSettings(canvasRect);
        }

        private void BuildBackground(RectTransform parent)
        {
            UnityEngine.UI.Image sky = CreateImage("Sky", parent, ConveyorChefUITheme.Sky, null);
            Stretch(sky.rectTransform);

            UnityEngine.UI.Image town = CreateImage("FoodTown", parent, new Color(1f, 0.74f, 0.43f, 1f), panelSprite);
            Place(town.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.62f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);

            UnityEngine.UI.Image glow = CreateImage("SunGlow", parent, new Color(1f, 0.88f, 0.50f, 0.35f), panelSprite);
            Place(glow.rectTransform, new Vector2(0.5f, 0.70f), new Vector2(0.5f, 0.70f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200f, 1200f));

            for (int i = 0; i < 5; i++)
            {
                float x = 0.02f + (i * 0.245f);
                UnityEngine.UI.Image building = CreateImage("FoodShop_" + i, parent, new Color(0.28f, 0.14f, 0.08f, 0.22f), panelSprite);
                Place(building.rectTransform, new Vector2(x, 0.39f), new Vector2(x + 0.20f, 0.39f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 240f + (i % 3) * 80f));
            }

            UnityEngine.UI.Image shade = CreateImage("BottomShade", parent, new Color(0.16f, 0.07f, 0.03f, 0.23f), null);
            Place(shade.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.24f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
        }

        private void BuildHeader()
        {
            logoRoot = CreateRect("Logo", safeArea);
            Place(logoRoot, new Vector2(0.08f, 0.72f), new Vector2(0.92f, 0.96f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            logoGroup = logoRoot.gameObject.AddComponent<CanvasGroup>();

            TMP_Text title = CreateText("Title", logoRoot, "CONVEYOR\nCHEF", 108f, ConveyorChefUITheme.Gold, FontStyles.Bold);
            title.lineSpacing = -18f;
            Place(title.rectTransform, new Vector2(0f, 0.18f), new Vector2(1f, 0.88f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            UnityEngine.UI.Image ribbon = CreateImage("FoodRushRibbon", logoRoot, ConveyorChefUITheme.Tomato, panelSprite);
            Place(ribbon.rectTransform, new Vector2(0.20f, 0.02f), new Vector2(0.80f, 0.24f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            AddShadow(ribbon, new Vector2(0f, -7f), 0.30f);

            TMP_Text subtitle = CreateText("FoodRush", ribbon.transform, "FOOD RUSH", 44f, Color.white, FontStyles.Bold);
            Stretch(subtitle.rectTransform);
            subtitle.characterSpacing = 5f;

            Button settings = CreateButton("Settings", safeArea, ConveyorChefUITheme.CreamLight, OpenSettings);
            Place(settings.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-36f, -36f), new Vector2(120f, 120f));
            AddShadow(settings.GetComponent<UnityEngine.UI.Image>(), new Vector2(0f, -6f), 0.35f);

            if (settingsSprite != null)
            {
                UnityEngine.UI.Image icon = CreateImage("Icon", settings.transform, ConveyorChefUITheme.Ink, settingsSprite);
                icon.preserveAspect = true;
                Place(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(66f, 66f));
            }
            else
            {
                TMP_Text icon = CreateText("Icon", settings.transform, "⚙", 54f, ConveyorChefUITheme.Ink, FontStyles.Bold);
                Stretch(icon.rectTransform);
            }
        }

        private void BuildHero()
        {
            heroRoot = CreateRect("Hero", safeArea);
            Place(heroRoot, new Vector2(0.06f, 0.24f), new Vector2(0.94f, 0.72f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            heroGroup = heroRoot.gameObject.AddComponent<CanvasGroup>();

            UnityEngine.UI.Image heroCard = CreateImage("HeroCard", heroRoot, new Color(1f, 0.97f, 0.84f, 0.93f), panelSprite);
            Place(heroCard.rectTransform, new Vector2(0.12f, 0.10f), new Vector2(0.88f, 0.93f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            AddShadow(heroCard, new Vector2(0f, -12f), 0.28f);

            TMP_Text chefTitle = CreateText("ChefReady", heroCard.transform, "CHEF READY!", 72f, ConveyorChefUITheme.Ink, FontStyles.Bold);
            Place(chefTitle.rectTransform, new Vector2(0.08f, 0.55f), new Vector2(0.92f, 0.80f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            TMP_Text instruction = CreateText("Instruction", heroCard.transform, "SORT • SERVE • RUSH", 29f, ConveyorChefUITheme.Tomato, FontStyles.Bold);
            Place(instruction.rectTransform, new Vector2(0.08f, 0.42f), new Vector2(0.92f, 0.57f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            RectTransform conveyor = CreateRect("Conveyor", heroCard.transform);
            Place(conveyor, new Vector2(0.05f, 0.07f), new Vector2(0.95f, 0.37f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            UnityEngine.UI.Image belt = CreateImage("Belt", conveyor, new Color(0.17f, 0.15f, 0.14f, 0.96f), panelSprite);
            Stretch(belt.rectTransform);

            CreateFoodCard(conveyor, "BURGER", 0.18f, ConveyorChefUITheme.Gold);
            CreateFoodCard(conveyor, "DONUT", 0.50f, ConveyorChefUITheme.Tomato);
            CreateFoodCard(conveyor, "PASTRY", 0.82f, ConveyorChefUITheme.Orange);
        }

        private void CreateFoodCard(Transform parent, string label, float x, Color color)
        {
            UnityEngine.UI.Image card = CreateImage(label, parent, color, panelSprite);
            Place(card.rectTransform, new Vector2(x, 0.5f), new Vector2(x, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(220f, 108f));

            TMP_Text text = CreateText("Label", card.transform, label, 25f, ConveyorChefUITheme.Ink, FontStyles.Bold);
            Stretch(text.rectTransform);
        }

        private void BuildPlayArea()
        {
            playRoot = CreateRect("PlayArea", safeArea);
            Place(playRoot, new Vector2(0.12f, 0.045f), new Vector2(0.88f, 0.23f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            playGroup = playRoot.gameObject.AddComponent<CanvasGroup>();

            Button playButton = CreateButton("PlayButton", playRoot, ConveyorChefUITheme.Tomato, PlayGame);
            Place(playButton.GetComponent<RectTransform>(), new Vector2(0f, 0.24f), new Vector2(1f, 0.93f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            AddShadow(playButton.GetComponent<UnityEngine.UI.Image>(), new Vector2(0f, -13f), 0.42f);

            TMP_Text label = CreateText("PlayLabel", playButton.transform, "▶  PLAY", 76f, Color.white, FontStyles.Bold);
            Stretch(label.rectTransform);

            TMP_Text hint = CreateText("PlayHint", playRoot, "Continue your food rush", 24f, ConveyorChefUITheme.CreamLight, FontStyles.Bold);
            Place(hint.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.22f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        }

        private void BuildFooter()
        {
            TMP_Text footer = CreateText("Version", safeArea, "CONVEYOR CHEF • v" + Application.version, 20f, new Color(1f, 1f, 1f, 0.82f), FontStyles.Bold);
            Place(footer.rectTransform, new Vector2(0.15f, 0.006f), new Vector2(0.85f, 0.04f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        }

        private void BuildSettings(RectTransform canvasRect)
        {
            settingsPopup = new GameObject("CC_SettingsPopup", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
            settingsPopup.layer = LayerMask.NameToLayer("UI");
            settingsPopup.transform.SetParent(canvasRect, false);
            Stretch(settingsPopup.GetComponent<RectTransform>());

            UnityEngine.UI.Image dim = settingsPopup.GetComponent<UnityEngine.UI.Image>();
            dim.color = ConveyorChefUITheme.Overlay;

            UnityEngine.UI.Image card = CreateImage("SettingsCard", settingsPopup.transform, ConveyorChefUITheme.CreamLight, panelSprite);
            Place(card.rectTransform, new Vector2(0.13f, 0.28f), new Vector2(0.87f, 0.72f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            AddShadow(card, new Vector2(0f, -14f), 0.45f);

            TMP_Text title = CreateText("SettingsTitle", card.transform, "SETTINGS", 60f, ConveyorChefUITheme.Ink, FontStyles.Bold);
            Place(title.rectTransform, new Vector2(0.08f, 0.75f), new Vector2(0.92f, 0.93f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            Button soundButton = CreateButton("SoundButton", card.transform, ConveyorChefUITheme.Teal, ToggleSound);
            Place(soundButton.GetComponent<RectTransform>(), new Vector2(0.15f, 0.48f), new Vector2(0.85f, 0.66f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            soundLabel = CreateText("SoundLabel", soundButton.transform, "SOUND: ON", 37f, Color.white, FontStyles.Bold);
            Stretch(soundLabel.rectTransform);

            Button quitButton = CreateButton("QuitButton", card.transform, ConveyorChefUITheme.Tomato, QuitGame);
            Place(quitButton.GetComponent<RectTransform>(), new Vector2(0.15f, 0.27f), new Vector2(0.85f, 0.44f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            TMP_Text quitText = CreateText("QuitText", quitButton.transform, "QUIT GAME", 35f, Color.white, FontStyles.Bold);
            Stretch(quitText.rectTransform);

            Button backButton = CreateButton("BackButton", card.transform, ConveyorChefUITheme.Gold, CloseSettings);
            Place(backButton.GetComponent<RectTransform>(), new Vector2(0.25f, 0.07f), new Vector2(0.75f, 0.20f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            TMP_Text backText = CreateText("BackText", backButton.transform, "BACK", 31f, ConveyorChefUITheme.Ink, FontStyles.Bold);
            Stretch(backText.rectTransform);

            settingsPopup.SetActive(false);
        }

        private void PlayGame()
        {
            sceneLoader.scenechange();
        }

        private void OpenSettings()
        {
            sceneLoader.buttonsound();
            settingsPopup.SetActive(true);
            settingsPopup.transform.SetAsLastSibling();
        }

        private void CloseSettings()
        {
            sceneLoader.buttonsound();
            settingsPopup.SetActive(false);
        }

        private void QuitGame()
        {
            sceneLoader.quit();
        }

        private void ToggleSound()
        {
            bool muted = AudioListener.volume > 0.001f;
            AudioListener.volume = muted ? 0f : 1f;
            PlayerPrefs.SetInt(SoundPrefKey, muted ? 1 : 0);
            PlayerPrefs.Save();
            RefreshSoundLabel();

            if (!muted)
                sceneLoader.buttonsound();
        }

        private void ApplyStoredSoundState()
        {
            bool muted = PlayerPrefs.GetInt(SoundPrefKey, 0) == 1;
            AudioListener.volume = muted ? 0f : 1f;
            RefreshSoundLabel();
        }

        private void RefreshSoundLabel()
        {
            if (soundLabel != null)
                soundLabel.text = AudioListener.volume <= 0.001f ? "SOUND: OFF" : "SOUND: ON";
        }

        private IEnumerator EntranceRoutine()
        {
            logoGroup.alpha = 0f;
            heroGroup.alpha = 0f;
            playGroup.alpha = 0f;

            logoRoot.localScale = Vector3.one * 0.90f;
            heroRoot.localScale = Vector3.one * 0.94f;
            playRoot.localScale = Vector3.one * 0.92f;

            float elapsed = 0f;
            const float duration = 0.55f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = Mathf.SmoothStep(0f, 1f, t);

                logoGroup.alpha = Mathf.Clamp01(t * 1.7f);
                heroGroup.alpha = Mathf.Clamp01((t - 0.10f) * 1.9f);
                playGroup.alpha = Mathf.Clamp01((t - 0.26f) * 2.4f);

                logoRoot.localScale = Vector3.one * Mathf.Lerp(0.90f, 1f, eased);
                heroRoot.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, eased);
                playRoot.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, eased);
                yield return null;
            }

            logoGroup.alpha = heroGroup.alpha = playGroup.alpha = 1f;
            logoRoot.localScale = heroRoot.localScale = playRoot.localScale = Vector3.one;
        }

        private IEnumerator HeroIdleRoutine()
        {
            yield return new WaitForSecondsRealtime(0.6f);
            Vector2 basePosition = heroRoot.anchoredPosition;
            float phase = 0f;

            while (heroRoot != null)
            {
                phase += Time.unscaledDeltaTime * 1.25f;
                heroRoot.anchoredPosition = basePosition + Vector2.up * (Mathf.Sin(phase) * 6f);
                yield return null;
            }
        }

        private IEnumerator PlayPulseRoutine()
        {
            yield return new WaitForSecondsRealtime(0.8f);
            float phase = 0f;

            while (playRoot != null)
            {
                phase += Time.unscaledDeltaTime * 2f;
                float pulse = (Mathf.Sin(phase) + 1f) * 0.5f;
                playRoot.localScale = Vector3.one * Mathf.Lerp(1f, 1.022f, pulse);
                yield return null;
            }
        }

        private void ApplySafeArea()
        {
            if (safeArea == null || Screen.width <= 0 || Screen.height <= 0)
                return;

            Rect safe = Screen.safeArea;
            lastSafeArea = safe;

            Vector2 min = safe.position;
            Vector2 max = safe.position + safe.size;
            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;

            safeArea.anchorMin = min;
            safeArea.anchorMax = max;
            safeArea.offsetMin = Vector2.zero;
            safeArea.offsetMax = Vector2.zero;
        }

        private void EnsureEventSystem()
        {
            EventSystem[] systems = FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (systems.Length > 0)
            {
                systems[0].gameObject.SetActive(true);
                return;
            }

            new GameObject("CC_EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private RectTransform CreateRect(string objectName, Transform parent)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private UnityEngine.UI.Image CreateImage(string objectName, Transform parent, Color color, Sprite sprite)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);

            UnityEngine.UI.Image image = go.GetComponent<UnityEngine.UI.Image>();
            image.color = color;
            image.sprite = sprite;
            image.type = sprite != null ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            image.raycastTarget = false;
            return image;
        }

        private TMP_Text CreateText(string objectName, Transform parent, string value, float size, Color color, FontStyles style)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);

            TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.fontStyle = style;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.overflowMode = TextOverflowModes.Overflow;

            if (font != null)
                text.font = font;

            return text;
        }

        private Button CreateButton(string objectName, Transform parent, Color color, UnityEngine.Events.UnityAction action)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image), typeof(Button));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);

            UnityEngine.UI.Image image = go.GetComponent<UnityEngine.UI.Image>();
            image.color = color;
            image.sprite = buttonSprite != null ? buttonSprite : panelSprite;
            image.type = image.sprite != null ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;

            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);
            return button;
        }

        private static void AddShadow(UnityEngine.UI.Image image, Vector2 distance, float alpha)
        {
            if (image == null || image.GetComponent<Shadow>() != null)
                return;

            Shadow shadow = image.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.12f, 0.05f, 0.02f, alpha);
            shadow.effectDistance = distance;
            shadow.useGraphicAlpha = true;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Place(RectTransform rect, Vector2 min, Vector2 max, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }
    }
}
