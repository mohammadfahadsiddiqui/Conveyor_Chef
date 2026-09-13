using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Watermelon.BusStop
{
    /// <summary>
    /// Builds the Conveyor Chef main menu presentation at runtime while preserving
    /// the existing scene-loading logic and the original menu scene as a fallback.
    /// </summary>
    public sealed class MainMenuRedesignController : MonoBehaviour
    {
        private const string ROOT_NAME = "CC_MainMenu_Redesign";
        private const string SOUND_PREF_KEY = "CC_SOUND_MUTED";

        private static readonly Color Cream = new Color(1.00f, 0.95f, 0.84f, 1.00f);
        private static readonly Color CreamLight = new Color(1.00f, 0.985f, 0.94f, 1.00f);
        private static readonly Color Tomato = new Color(0.90f, 0.20f, 0.20f, 1.00f);
        private static readonly Color TomatoDark = new Color(0.67f, 0.10f, 0.11f, 1.00f);
        private static readonly Color Teal = new Color(0.10f, 0.59f, 0.55f, 1.00f);
        private static readonly Color Honey = new Color(1.00f, 0.70f, 0.18f, 1.00f);
        private static readonly Color Ink = new Color(0.19f, 0.12f, 0.10f, 1.00f);
        private static readonly Color White = new Color(1.00f, 1.00f, 1.00f, 1.00f);

        private sceneloading sceneLoader;
        private TMP_FontAsset legacyFont;
        private Sprite legacyPlaySprite;
        private Sprite legacyMapSprite;
        private Sprite legacyScooterSprite;
        private Sprite legacySettingsIcon;
        private Sprite legacyPanelSprite;
        private Canvas legacyCanvas;

        private RectTransform safeAreaRoot;
        private Rect lastSafeArea;
        private GameObject settingsOverlay;
        private TextMeshProUGUI soundStateText;

        private RectTransform logoRoot;
        private RectTransform heroRoot;
        private RectTransform playButtonRoot;
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

            if (FindSceneObject(ROOT_NAME) != null)
                return;

            CaptureLegacyReferences();
            EnsureEventSystem();
            BuildMenu();
            ApplySavedSoundState();

            if (legacyCanvas != null)
                legacyCanvas.gameObject.SetActive(false);

            StartCoroutine(EntranceRoutine());
            StartCoroutine(HeroFloatRoutine());
            StartCoroutine(PlayPulseRoutine());
        }

        private void Update()
        {
            if (safeAreaRoot != null && Screen.safeArea != lastSafeArea)
                ApplySafeArea();
        }

        private void CaptureLegacyReferences()
        {
            GameObject playObject = FindSceneObject("play");
            if (playObject != null)
            {
                Image image = playObject.GetComponent<Image>();
                if (image != null)
                    legacyPlaySprite = image.sprite;

                legacyCanvas = playObject.GetComponentInParent<Canvas>(true);
            }

            GameObject mapObject = FindSceneObject("map");
            if (mapObject != null)
            {
                Image image = mapObject.GetComponent<Image>();
                if (image != null)
                    legacyMapSprite = image.sprite;
            }

            GameObject scooterObject = FindSceneObject("scooter");
            if (scooterObject != null)
            {
                Image image = scooterObject.GetComponent<Image>();
                if (image != null)
                    legacyScooterSprite = image.sprite;
            }

            GameObject settingsObject = FindSceneObject("setting");
            if (settingsObject != null)
            {
                Image[] images = settingsObject.GetComponentsInChildren<Image>(true);
                for (int i = 0; i < images.Length; i++)
                {
                    if (images[i].gameObject != settingsObject && images[i].sprite != null)
                    {
                        legacySettingsIcon = images[i].sprite;
                        break;
                    }
                }
            }

            GameObject settingsPanelObject = FindSceneObject("setting panel");
            if (settingsPanelObject != null)
            {
                Image image = settingsPanelObject.GetComponent<Image>();
                if (image != null)
                    legacyPanelSprite = image.sprite;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            GameObject[] roots = activeScene.GetRootGameObjects();
            for (int i = 0; i < roots.Length && legacyFont == null; i++)
            {
                TextMeshProUGUI[] labels = roots[i].GetComponentsInChildren<TextMeshProUGUI>(true);
                for (int j = 0; j < labels.Length; j++)
                {
                    if (labels[j].font != null)
                    {
                        legacyFont = labels[j].font;
                        break;
                    }
                }
            }
        }

        private void EnsureEventSystem()
        {
            EventSystem[] eventSystems = FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (eventSystems.Length > 0)
            {
                eventSystems[0].gameObject.SetActive(true);
                return;
            }

            GameObject eventSystemObject = new GameObject("CC_EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            eventSystemObject.transform.SetParent(null);
        }

        private void BuildMenu()
        {
            GameObject root = new GameObject(ROOT_NAME, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.layer = LayerMask.NameToLayer("UI");

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform canvasRect = root.GetComponent<RectTransform>();

            Image background = CreateImage("Background", canvasRect, CreamLight, null);
            Stretch(background.rectTransform);

            Image topBand = CreateImage("TopWarmBand", canvasRect, new Color(1f, 0.84f, 0.59f, 0.75f), legacyPanelSprite);
            SetRect(topBand.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 40f), new Vector2(1180f, 550f));

            Image bottomBand = CreateImage("BottomWarmBand", canvasRect, new Color(0.96f, 0.86f, 0.68f, 0.95f), legacyPanelSprite);
            SetRect(bottomBand.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, -40f), new Vector2(1180f, 570f));

            Image accentStrip = CreateImage("AccentStrip", canvasRect, Tomato, null);
            SetRect(accentStrip.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 18f));

            safeAreaRoot = CreateRect("SafeArea", canvasRect);
            ApplySafeArea();

            BuildHeader();
            BuildHero();
            BuildActions();
            BuildFooter();
            BuildSettingsOverlay(canvasRect);
        }

        private void BuildHeader()
        {
            RectTransform header = CreateRect("Header", safeAreaRoot);
            SetRect(header, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(980f, 360f));

            logoRoot = CreateRect("Logo", header);
            SetRect(logoRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(860f, 285f));
            logoGroup = logoRoot.gameObject.AddComponent<CanvasGroup>();

            TextMeshProUGUI title = CreateText("Title", logoRoot, "CONVEYOR CHEF", 96f, Ink, FontStyles.Bold);
            SetRect(title.rectTransform, new Vector2(0f, 0.53f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            TextMeshProUGUI subtitle = CreateText("Subtitle", logoRoot, "FOOD RUSH", 58f, Tomato, FontStyles.Bold);
            SetRect(subtitle.rectTransform, new Vector2(0f, 0.22f), new Vector2(1f, 0.58f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            subtitle.characterSpacing = 5f;

            TextMeshProUGUI tagline = CreateText("Tagline", logoRoot, "SORT  •  SERVE  •  RUSH", 25f, new Color(Ink.r, Ink.g, Ink.b, 0.72f), FontStyles.Bold);
            SetRect(tagline.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.27f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            tagline.characterSpacing = 3f;

            Button settingsButton = CreateButton("SettingsButton", header, Cream, legacyPanelSprite, OpenSettings);
            SetRect(settingsButton.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -25f), new Vector2(128f, 108f));

            if (legacySettingsIcon != null)
            {
                Image icon = CreateImage("Icon", settingsButton.transform, TomatoDark, legacySettingsIcon);
                icon.preserveAspect = true;
                SetRect(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(62f, 62f));
            }
            else
            {
                TextMeshProUGUI settingsLabel = CreateText("Label", settingsButton.transform, "SET", 28f, TomatoDark, FontStyles.Bold);
                Stretch(settingsLabel.rectTransform);
            }
        }

        private void BuildHero()
        {
            heroRoot = CreateRect("HeroArea", safeAreaRoot);
            SetRect(heroRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(940f, 870f));
            heroGroup = heroRoot.gameObject.AddComponent<CanvasGroup>();

            Image heroCard = CreateImage("HeroCard", heroRoot, new Color(1f, 1f, 1f, 0.63f), legacyPanelSprite);
            SetRect(heroCard.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(900f, 760f));

            Image innerAccent = CreateImage("InnerAccent", heroRoot, new Color(Teal.r, Teal.g, Teal.b, 0.12f), legacyPanelSprite);
            SetRect(innerAccent.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(835f, 695f));

            if (legacyMapSprite != null)
            {
                Image map = CreateImage("KitchenMap", heroRoot, new Color(1f, 1f, 1f, 0.30f), legacyMapSprite);
                map.preserveAspect = true;
                SetRect(map.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(720f, 720f));
            }

            if (legacyScooterSprite != null)
            {
                Image scooter = CreateImage("ChefDeliveryHero", heroRoot, White, legacyScooterSprite);
                scooter.preserveAspect = true;
                SetRect(scooter.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 35f), new Vector2(600f, 600f));
            }
            else
            {
                TextMeshProUGUI fallback = CreateText("HeroFallback", heroRoot, "THE KITCHEN\nIS READY!", 54f, Ink, FontStyles.Bold);
                fallback.alignment = TextAlignmentOptions.Center;
                SetRect(fallback.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(700f, 250f));
            }

            RectTransform badges = CreateRect("FeatureBadges", heroRoot);
            SetRect(badges, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 25f), new Vector2(760f, 115f));

            CreateBadge(badges, "SORT", -250f, Tomato);
            CreateBadge(badges, "SERVE", 0f, Honey);
            CreateBadge(badges, "RUSH", 250f, Teal);
        }

        private void BuildActions()
        {
            playButtonRoot = CreateRect("PlayArea", safeAreaRoot);
            SetRect(playButtonRoot, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 245f), new Vector2(760f, 235f));
            playGroup = playButtonRoot.gameObject.AddComponent<CanvasGroup>();

            Button playButton = CreateButton("PlayButton", playButtonRoot, Tomato, legacyPlaySprite, PlayGame);
            SetRect(playButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700f, 180f));

            TextMeshProUGUI playLabel = CreateText("PlayLabel", playButton.transform, "PLAY", 72f, White, FontStyles.Bold);
            SetRect(playLabel.rectTransform, new Vector2(0f, 0.25f), new Vector2(1f, 0.92f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            playLabel.characterSpacing = 4f;

            TextMeshProUGUI playSubLabel = CreateText("PlaySubLabel", playButton.transform, "CONTINUE YOUR FOOD RUSH", 20f, new Color(1f, 1f, 1f, 0.84f), FontStyles.Bold);
            SetRect(playSubLabel.rectTransform, new Vector2(0f, 0.06f), new Vector2(1f, 0.34f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            playSubLabel.characterSpacing = 2f;
        }

        private void BuildFooter()
        {
            TextMeshProUGUI footer = CreateText("Version", safeAreaRoot, $"CONVEYOR CHEF  •  v{Application.version}", 22f, new Color(Ink.r, Ink.g, Ink.b, 0.56f), FontStyles.Bold);
            SetRect(footer.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(0f, 50f));
        }

        private void BuildSettingsOverlay(RectTransform canvasRect)
        {
            settingsOverlay = new GameObject("SettingsOverlay", typeof(RectTransform));
            settingsOverlay.layer = LayerMask.NameToLayer("UI");
            settingsOverlay.transform.SetParent(canvasRect, false);
            Stretch(settingsOverlay.GetComponent<RectTransform>());

            Image dim = settingsOverlay.AddComponent<Image>();
            dim.color = new Color(0.08f, 0.05f, 0.04f, 0.72f);
            dim.raycastTarget = true;

            Image card = CreateImage("SettingsCard", settingsOverlay.transform, CreamLight, legacyPanelSprite);
            SetRect(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(790f, 800f));

            Image cardAccent = CreateImage("Accent", card.transform, Tomato, null);
            SetRect(cardAccent.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 16f));

            TextMeshProUGUI title = CreateText("Title", card.transform, "SETTINGS", 64f, Ink, FontStyles.Bold);
            SetRect(title.rectTransform, new Vector2(0.08f, 0.77f), new Vector2(0.92f, 0.95f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            TextMeshProUGUI description = CreateText("Description", card.transform, "Adjust the kitchen and jump back into the rush.", 26f, new Color(Ink.r, Ink.g, Ink.b, 0.68f), FontStyles.Normal);
            SetRect(description.rectTransform, new Vector2(0.10f, 0.66f), new Vector2(0.90f, 0.78f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            Button soundButton = CreateButton("SoundButton", card.transform, Teal, legacyPlaySprite, ToggleSound);
            SetRect(soundButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 85f), new Vector2(560f, 145f));
            soundStateText = CreateText("SoundState", soundButton.transform, "SOUND: ON", 40f, White, FontStyles.Bold);
            Stretch(soundStateText.rectTransform);

            Button quitButton = CreateButton("QuitButton", card.transform, Tomato, legacyPlaySprite, QuitGame);
            SetRect(quitButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -100f), new Vector2(560f, 130f));
            TextMeshProUGUI quitLabel = CreateText("QuitLabel", quitButton.transform, "QUIT GAME", 36f, White, FontStyles.Bold);
            Stretch(quitLabel.rectTransform);

            Button closeButton = CreateButton("CloseButton", card.transform, Cream, legacyPanelSprite, CloseSettings);
            SetRect(closeButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 65f), new Vector2(390f, 105f));
            TextMeshProUGUI closeLabel = CreateText("CloseLabel", closeButton.transform, "BACK", 32f, Ink, FontStyles.Bold);
            Stretch(closeLabel.rectTransform);

            settingsOverlay.SetActive(false);
        }

        private void CreateBadge(RectTransform parent, string text, float x, Color accent)
        {
            Image badge = CreateImage(text + "Badge", parent, new Color(accent.r, accent.g, accent.b, 0.17f), legacyPanelSprite);
            SetRect(badge.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, 0f), new Vector2(205f, 74f));

            TextMeshProUGUI label = CreateText("Label", badge.transform, text, 25f, accent, FontStyles.Bold);
            Stretch(label.rectTransform);
            label.characterSpacing = 2f;
        }

        private void PlayGame()
        {
            sceneLoader.scenechange();
        }

        private void OpenSettings()
        {
            sceneLoader.buttonsound();
            settingsOverlay.SetActive(true);
            settingsOverlay.transform.SetAsLastSibling();
        }

        private void CloseSettings()
        {
            sceneLoader.buttonsound();
            settingsOverlay.SetActive(false);
        }

        private void QuitGame()
        {
            sceneLoader.quit();
        }

        private void ToggleSound()
        {
            bool muted = AudioListener.volume <= 0.001f;
            muted = !muted;

            AudioListener.volume = muted ? 0f : 1f;
            PlayerPrefs.SetInt(SOUND_PREF_KEY, muted ? 1 : 0);
            PlayerPrefs.Save();

            RefreshSoundLabel();

            if (!muted)
                sceneLoader.buttonsound();
        }

        private void ApplySavedSoundState()
        {
            bool muted = PlayerPrefs.GetInt(SOUND_PREF_KEY, 0) == 1;
            AudioListener.volume = muted ? 0f : 1f;
            RefreshSoundLabel();
        }

        private void RefreshSoundLabel()
        {
            if (soundStateText != null)
                soundStateText.text = AudioListener.volume <= 0.001f ? "SOUND: OFF" : "SOUND: ON";
        }

        private IEnumerator EntranceRoutine()
        {
            logoGroup.alpha = 0f;
            heroGroup.alpha = 0f;
            playGroup.alpha = 0f;

            logoRoot.localScale = Vector3.one * 0.90f;
            heroRoot.localScale = Vector3.one * 0.94f;
            playButtonRoot.localScale = Vector3.one * 0.92f;

            float elapsed = 0f;
            const float duration = 0.55f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = Mathf.SmoothStep(0f, 1f, t);

                logoGroup.alpha = Mathf.Clamp01(t * 1.7f);
                heroGroup.alpha = Mathf.Clamp01((t - 0.12f) * 1.8f);
                playGroup.alpha = Mathf.Clamp01((t - 0.28f) * 2.3f);

                logoRoot.localScale = Vector3.one * Mathf.Lerp(0.90f, 1f, eased);
                heroRoot.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, eased);
                playButtonRoot.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, eased);

                yield return null;
            }

            logoGroup.alpha = 1f;
            heroGroup.alpha = 1f;
            playGroup.alpha = 1f;
            logoRoot.localScale = Vector3.one;
            heroRoot.localScale = Vector3.one;
            playButtonRoot.localScale = Vector3.one;
        }

        private IEnumerator HeroFloatRoutine()
        {
            yield return new WaitForSecondsRealtime(0.6f);

            Vector2 basePosition = heroRoot.anchoredPosition;
            float phase = 0f;

            while (heroRoot != null)
            {
                phase += Time.unscaledDeltaTime * 1.35f;
                heroRoot.anchoredPosition = basePosition + Vector2.up * (Mathf.Sin(phase) * 7f);
                yield return null;
            }
        }

        private IEnumerator PlayPulseRoutine()
        {
            yield return new WaitForSecondsRealtime(0.8f);

            float phase = 0f;
            while (playButtonRoot != null)
            {
                phase += Time.unscaledDeltaTime * 2f;
                float pulse = (Mathf.Sin(phase) + 1f) * 0.5f;
                playButtonRoot.localScale = Vector3.one * Mathf.Lerp(1f, 1.022f, pulse);
                yield return null;
            }
        }

        private void ApplySafeArea()
        {
            if (safeAreaRoot == null || Screen.width <= 0 || Screen.height <= 0)
                return;

            Rect safe = Screen.safeArea;
            lastSafeArea = safe;

            Vector2 min = safe.position;
            Vector2 max = safe.position + safe.size;
            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;

            safeAreaRoot.anchorMin = min;
            safeAreaRoot.anchorMax = max;
            safeAreaRoot.offsetMin = Vector2.zero;
            safeAreaRoot.offsetMax = Vector2.zero;
        }

        private GameObject FindSceneObject(string objectName)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            GameObject[] roots = activeScene.GetRootGameObjects();

            for (int i = 0; i < roots.Length; i++)
            {
                Transform[] transforms = roots[i].GetComponentsInChildren<Transform>(true);
                for (int j = 0; j < transforms.Length; j++)
                {
                    if (transforms[j].name == objectName)
                        return transforms[j].gameObject;
                }
            }

            return null;
        }

        private RectTransform CreateRect(string objectName, Transform parent)
        {
            GameObject gameObject = new GameObject(objectName, typeof(RectTransform));
            gameObject.layer = LayerMask.NameToLayer("UI");
            gameObject.transform.SetParent(parent, false);
            return gameObject.GetComponent<RectTransform>();
        }

        private Image CreateImage(string objectName, Transform parent, Color color, Sprite sprite)
        {
            GameObject gameObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            gameObject.layer = LayerMask.NameToLayer("UI");
            gameObject.transform.SetParent(parent, false);

            Image image = gameObject.GetComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.raycastTarget = false;
            return image;
        }

        private TextMeshProUGUI CreateText(string objectName, Transform parent, string value, float size, Color color, FontStyles style)
        {
            GameObject gameObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            gameObject.layer = LayerMask.NameToLayer("UI");
            gameObject.transform.SetParent(parent, false);

            TextMeshProUGUI text = gameObject.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.fontStyle = style;
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = false;
            text.raycastTarget = false;
            text.overflowMode = TextOverflowModes.Overflow;

            if (legacyFont != null)
                text.font = legacyFont;

            return text;
        }

        private Button CreateButton(string objectName, Transform parent, Color color, Sprite sprite, UnityEngine.Events.UnityAction action)
        {
            GameObject gameObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            gameObject.layer = LayerMask.NameToLayer("UI");
            gameObject.transform.SetParent(parent, false);

            Image image = gameObject.GetComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.raycastTarget = true;

            Button button = gameObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;

            ColorBlock colors = button.colors;
            colors.normalColor = White;
            colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            colors.selectedColor = White;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.40f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            if (action != null)
                button.onClick.AddListener(action);

            return button;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }
    }
}
