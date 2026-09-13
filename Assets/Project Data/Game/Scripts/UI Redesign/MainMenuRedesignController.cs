using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Watermelon.BusStop
{
    /// <summary>
    /// Presentation-only main menu overhaul. Existing scene navigation remains owned
    /// by sceneloading so the redesign cannot break the game's flow.
    /// </summary>
    public sealed class MainMenuRedesignController : MonoBehaviour
    {
        private const string ROOT_NAME = "CC_MainMenu_Redesign";
        private const string SOUND_PREF_KEY = "CC_SOUND_MUTED";

        private sceneloading sceneLoader;
        private Canvas legacyCanvas;
        private TMP_FontAsset font;
        private Sprite buttonSprite;
        private Sprite panelSprite;
        private Sprite settingsSprite;

        private RectTransform safeArea;
        private Rect lastSafeArea;
        private RectTransform logo;
        private RectTransform hero;
        private RectTransform playArea;
        private CanvasGroup logoGroup;
        private CanvasGroup heroGroup;
        private CanvasGroup playGroup;
        private GameObject settingsPopup;
        private TMP_Text soundLabel;
        private bool initialised;

        public void Initialise(sceneloading loader)
        {
            if (initialised || loader == null) return;
            initialised = true;
            sceneLoader = loader;

            GameObject oldRoot = GameObject.Find(ROOT_NAME);
            if (oldRoot != null) Destroy(oldRoot);

            CaptureLegacyAssets();
            EnsureEventSystem();
            Build();
            ApplyStoredSoundState();

            if (legacyCanvas != null)
                legacyCanvas.gameObject.SetActive(false);

            StartCoroutine(Entrance());
            StartCoroutine(HeroIdle());
            StartCoroutine(PlayPulse());
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
                foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true))
                {
                    if (legacyCanvas == null) legacyCanvas = canvas;
                }

                foreach (TextMeshProUGUI text in root.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    if (text.font != null && font == null) font = text.font;
                }

                foreach (Image image in root.GetComponentsInChildren<Image>(true))
                {
                    string n = image.name.ToLowerInvariant();
                    if (buttonSprite == null && n.Contains("play") && image.sprite != null) buttonSprite = image.sprite;
                    if (panelSprite == null && (n.Contains("panel") || n.Contains("background")) && image.sprite != null) panelSprite = image.sprite;
                    if (settingsSprite == null && (n.Contains("setting") || n.Contains("gear")) && image.sprite != null) settingsSprite = image.sprite;
                }
            }
        }

        private void Build()
        {
            GameObject root = new GameObject(ROOT_NAME, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.layer = LayerMask.NameToLayer("UI");

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform canvasRect = root.GetComponent<RectTransform>();
            BuildBackground(canvasRect);

            safeArea = Rect("SafeArea", canvasRect);
            ApplySafeArea();

            BuildHeader();
            BuildHero();
            BuildPlay();
            BuildFooter();
            BuildSettings(canvasRect);
        }

        private void BuildBackground(RectTransform canvas)
        {
            Image sky = Image("Sky", canvas, ConveyorChefUITheme.Sky, null);
            Stretch(sky.rectTransform);

            Image sunGlow = Image("SunGlow", canvas, new Color(1f, 0.84f, 0.40f, 0.34f), panelSprite);
            Place(sunGlow.rectTransform, new Vector2(0.50f, 0.72f), new Vector2(0.50f, 0.72f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1250, 1250));

            Image street = Image("FoodTown", canvas, new Color(1f, 0.78f, 0.50f, 1f), panelSprite);
            Place(street.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.57f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);

            Image horizon = Image("Horizon", canvas, new Color(0.97f, 0.45f, 0.24f, 0.55f), panelSprite);
            Place(horizon.rectTransform, new Vector2(0f, 0.40f), new Vector2(1f, 0.60f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            // Food-factory silhouettes keep the scene lively without competing with the CTA.
            for (int i = 0; i < 5; i++)
            {
                float x = 0.04f + i * 0.235f;
                float h = 230 + (i % 3) * 75;
                Image building = Image("Shop" + i, canvas, new Color(0.35f, 0.20f, 0.14f, 0.24f), panelSprite);
                Place(building.rectTransform, new Vector2(x, 0.42f), new Vector2(x + 0.19f, 0.42f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0, h));
            }

            Image foreground = Image("ForegroundShade", canvas, new Color(0.18f, 0.09f, 0.04f, 0.22f), null);
            Place(foreground.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.23f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
        }

        private void BuildHeader()
        {
            logo = Rect("Logo", safeArea);
            Place(logo, new Vector2(0.08f, 0.70f), new Vector2(0.92f, 0.96f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            logoGroup = logo.gameObject.AddComponent<CanvasGroup>();

            TMP_Text chefHat = Text("ChefHat", logo, "♨", 88, ConveyorChefUITheme.CreamLight, FontStyles.Bold);
            Place(chefHat.rectTransform, new Vector2(0.38f, 0.75f), new Vector2(0.62f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            TMP_Text title = Text("Title", logo, "CONVEYOR\nCHEF", 112, ConveyorChefUITheme.Gold, FontStyles.Bold);
            title.lineSpacing = -18;
            Place(title.rectTransform, new Vector2(0f, 0.20f), new Vector2(1f, 0.82f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            Image ribbon = Image("Ribbon", logo, ConveyorChefUITheme.Tomato, panelSprite);
            Place(ribbon.rectTransform, new Vector2(0.19f, 0.03f), new Vector2(0.81f, 0.25f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            AddShadow(ribbon, new Vector2(0, -7), 0.28f);

            TMP_Text subtitle = Text("FoodRush", ribbon.transform, "FOOD RUSH", 46, Color.white, FontStyles.Bold);
            Stretch(subtitle.rectTransform);
            subtitle.characterSpacing = 5;

            Button settings = Button("Settings", safeArea, ConveyorChefUITheme.CreamLight, OpenSettings);
            Place(settings.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-38, -38), new Vector2(122, 122));
            AddShadow(settings.GetComponent<Image>(), new Vector2(0, -6), 0.35f);

            if (settingsSprite != null)
            {
                Image icon = Image("Icon", settings.transform, ConveyorChefUITheme.Ink, settingsSprite);
                icon.preserveAspect = true;
                Place(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(66, 66));
            }
            else
            {
                TMP_Text icon = Text("Icon", settings.transform, "⚙", 57, ConveyorChefUITheme.Ink, FontStyles.Bold);
                Stretch(icon.rectTransform);
            }
        }

        private void BuildHero()
        {
            hero = Rect("Hero", safeArea);
            Place(hero, new Vector2(0.05f, 0.22f), new Vector2(0.95f, 0.72f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            heroGroup = hero.gameObject.AddComponent<CanvasGroup>();

            Image glow = Image("HeroGlow", hero, new Color(1f, 0.96f, 0.76f, 0.80f), panelSprite);
            Place(glow.rectTransform, new Vector2(0.14f, 0.12f), new Vector2(0.86f, 0.93f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            AddShadow(glow, new Vector2(0, -12), 0.30f);

            // A clean chef badge replaces the unrelated scooter art from the old attempt.
            Image chefBody = Image("ChefBody", hero, ConveyorChefUITheme.CreamLight, panelSprite);
            Place(chefBody.rectTransform, new Vector2(0.25f, 0.26f), new Vector2(0.75f, 0.75f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            TMP_Text chef = Text("ChefMark", chefBody.transform, "CHEF", 86, ConveyorChefUITheme.Ink, FontStyles.Bold);
            Place(chef.rectTransform, new Vector2(0.04f, 0.22f), new Vector2(0.96f, 0.72f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            TMP_Text ready = Text("Ready", chefBody.transform, "READY!", 38, ConveyorChefUITheme.Tomato, FontStyles.Bold);
            Place(ready.rectTransform, new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.31f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            // Conveyor belt and food category cards visually explain the game before Play.
            RectTransform conveyor = Rect("Conveyor", hero);
            Place(conveyor, new Vector2(0.06f, 0.03f), new Vector2(0.94f, 0.26f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Image belt = Image("Belt", conveyor, new Color(0.18f, 0.16f, 0.15f, 0.94f), panelSprite);
            Stretch(belt.rectTransform);

            CreateFoodCard(conveyor, "BURGER", 0.18f, ConveyorChefUITheme.Gold);
            CreateFoodCard(conveyor, "DONUT", 0.50f, ConveyorChefUITheme.Tomato);
            CreateFoodCard(conveyor, "PASTRY", 0.82f, ConveyorChefUITheme.Orange);
        }

        private void CreateFoodCard(Transform parent, string label, float x, Color color)
        {
            Image card = Image(label, parent, color, panelSprite);
            Place(card.rectTransform, new Vector2(x, 0.50f), new Vector2(x, 0.50f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(230, 112));
            TMP_Text txt = Text("Label", card.transform, label, 27, ConveyorChefUITheme.Ink, FontStyles.Bold);
            Stretch(txt.rectTransform);
        }

        private void BuildPlay()
        {
            playArea = Rect("PlayArea", safeArea);
            Place(playArea, new Vector2(0.12f, 0.045f), new Vector2(0.88f, 0.22f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            playGroup = playArea.gameObject.AddComponent<CanvasGroup>();

            Button play = Button("PlayButton", playArea, ConveyorChefUITheme.Tomato, PlayGame);
            Place(play.GetComponent<RectTransform>(), new Vector2(0f, 0.22f), new Vector2(1f, 0.92f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            AddShadow(play.GetComponent<Image>(), new Vector2(0, -13), 0.42f);
            AddOutline(play.GetComponent<Image>(), ConveyorChefUITheme.Gold, 5);

            TMP_Text label = Text("Play", play.transform, "▶  PLAY", 76, Color.white, FontStyles.Bold);
            Stretch(label.rectTransform);
            label.characterSpacing = 2;

            TMP_Text hint = Text("Hint", playArea, "Continue your food rush", 25, ConveyorChefUITheme.CreamLight, FontStyles.Bold);
            Place(hint.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.24f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        }

        private void BuildFooter()
        {
            TMP_Text footer = Text("Version", safeArea, "CONVEYOR CHEF  •  v" + Application.version, 20, new Color(1f, 1f, 1f, 0.82f), FontStyles.Bold);
            Place(footer.rectTransform, new Vector2(0.15f, 0.005f), new Vector2(0.85f, 0.04f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        }

        private void BuildSettings(RectTransform canvas)
        {
            settingsPopup = new GameObject("CC_SettingsPopup", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            settingsPopup.layer = LayerMask.NameToLayer("UI");
            settingsPopup.transform.SetParent(canvas, false);
            RectTransform root = settingsPopup.GetComponent<RectTransform>();
            Stretch(root);
            Image dim = settingsPopup.GetComponent<Image>();
            dim.color = ConveyorChefUITheme.Overlay;

            Image card = Image("Card", settingsPopup.transform, ConveyorChefUITheme.CreamLight, panelSprite);
            Place(card.rectTransform, new Vector2(0.13f, 0.27f), new Vector2(0.87f, 0.73f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            AddShadow(card, new Vector2(0, -14), 0.45f);

            TMP_Text title = Text("Title", card.transform, "SETTINGS", 61, ConveyorChefUITheme.Ink, FontStyles.Bold);
            Place(title.rectTransform, new Vector2(0.08f, 0.74f), new Vector2(0.92f, 0.94f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            Button sound = Button("Sound", card.transform, ConveyorChefUITheme.Teal, ToggleSound);
            Place(sound.GetComponent<RectTransform>(), new Vector2(0.14f, 0.47f), new Vector2(0.86f, 0.66f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            soundLabel = Text("Label", sound.transform, "SOUND: ON", 36, Color.white, FontStyles.Bold);
            Stretch(soundLabel.rectTransform);

            Button quit = Button("Quit", card.transform, ConveyorChefUITheme.Tomato, QuitGame);
            Place(quit.GetComponent<RectTransform>(), new Vector2(0.14f, 0.25f), new Vector2(0.86f, 0.44f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            TMP_Text quitText = Text("Label", quit.transform, "QUIT GAME", 36, Color.white, FontStyles.Bold);
            Stretch(quitText.rectTransform);

            Button back = Button("Back", card.transform, ConveyorChefUITheme.Cream, CloseSettings);
            Place(back.GetComponent<RectTransform>(), new Vector2(0.25f, 0.06f), new Vector2(0.75f, 0.20f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            TMP_Text backText = Text("Label", back.transform, "BACK", 30, ConveyorChefUITheme.Ink, FontStyles.Bold);
            Stretch(backText.rectTransform);

            settingsPopup.SetActive(false);
        }

        private void PlayGame() => sceneLoader.scenechange();

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

        private void QuitGame() => sceneLoader.quit();

        private void ToggleSound()
        {
            bool mute = AudioListener.volume > 0.001f;
            AudioListener.volume = mute ? 0 : 1;
            PlayerPrefs.SetInt(SOUND_PREF_KEY, mute ? 1 : 0);
            PlayerPrefs.Save();
            RefreshSound();
            if (!mute) sceneLoader.buttonsound();
        }

        private void ApplyStoredSoundState()
        {
            AudioListener.volume = PlayerPrefs.GetInt(SOUND_PREF_KEY, 0) == 1 ? 0 : 1;
            RefreshSound();
        }

        private void RefreshSound()
        {
            if (soundLabel != null)
                soundLabel.text = AudioListener.volume <= 0.001f ? "SOUND: OFF" : "SOUND: ON";
        }

        private IEnumerator Entrance()
        {
            logoGroup.alpha = heroGroup.alpha = playGroup.alpha = 0;
            logo.localScale = hero.localScale = playArea.localScale = Vector3.one * 0.90f;
            float t = 0;
            while (t < 0.60f)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / 0.60f));
                logoGroup.alpha = Mathf.Clamp01(p * 1.6f);
                heroGroup.alpha = Mathf.Clamp01((p - 0.10f) * 1.8f);
                playGroup.alpha = Mathf.Clamp01((p - 0.24f) * 2.3f);
                logo.localScale = Vector3.one * Mathf.Lerp(0.90f, 1f, p);
                hero.localScale = Vector3.one * Mathf.Lerp(0.93f, 1f, p);
                playArea.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, p);
                yield return null;
            }
            logoGroup.alpha = heroGroup.alpha = playGroup.alpha = 1;
            logo.localScale = hero.localScale = playArea.localScale = Vector3.one;
        }

        private IEnumerator HeroIdle()
        {
            yield return new WaitForSecondsRealtime(0.7f);
            Vector2 basePos = hero.anchoredPosition;
            float phase = 0;
            while (hero != null)
            {
                phase += Time.unscaledDeltaTime * 1.3f;
                hero.anchoredPosition = basePos + Vector2.up * Mathf.Sin(phase) * 7f;
                yield return null;
            }
        }

        private IEnumerator PlayPulse()
        {
            yield return new WaitForSecondsRealtime(0.8f);
            float phase = 0;
            while (playArea != null)
            {
                phase += Time.unscaledDeltaTime * 1.8f;
                playArea.localScale = Vector3.one * Mathf.Lerp(1f, 1.025f, (Mathf.Sin(phase) + 1f) * 0.5f);
                yield return null;
            }
        }

        private void ApplySafeArea()
        {
            if (safeArea == null || Screen.width <= 0 || Screen.height <= 0) return;
            Rect area = Screen.safeArea;
            lastSafeArea = area;
            Vector2 min = area.position;
            Vector2 max = area.position + area.size;
            min.x /= Screen.width; min.y /= Screen.height;
            max.x /= Screen.width; max.y /= Screen.height;
            safeArea.anchorMin = min;
            safeArea.anchorMax = max;
            safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
        }

        private void EnsureEventSystem()
        {
            EventSystem[] systems = FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (systems.Length > 0) { systems[0].gameObject.SetActive(true); return; }
            new GameObject("CC_EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private RectTransform Rect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private Image Image(string name, Transform parent, Color color, Sprite sprite)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            Image image = go.GetComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.raycastTarget = false;
            return image;
        }

        private TMP_Text Text(string name, Transform parent, string value, float size, Color color, FontStyles style)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.fontStyle = style;
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = false;
            text.raycastTarget = false;
            if (font != null) text.font = font;
            return text;
        }

        private Button Button(string name, Transform parent, Color color, UnityEngine.Events.UnityAction action)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            Image image = go.GetComponent<Image>();
            image.color = color;
            image.sprite = buttonSprite ?? panelSprite;
            image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);
            return button;
        }

        private void AddShadow(Image image, Vector2 distance, float alpha)
        {
            if (image == null || image.GetComponent<Shadow>() != null) return;
            Shadow shadow = image.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.12f, 0.05f, 0.02f, alpha);
            shadow.effectDistance = distance;
            shadow.useGraphicAlpha = true;
        }

        private void AddOutline(Image image, Color color, float amount)
        {
            if (image == null || image.GetComponent<Outline>() != null) return;
            Outline outline = image.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(amount, -amount);
            outline.useGraphicAlpha = true;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static void Place(RectTransform rect, Vector2 min, Vector2 max, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rect.anchorMin = min; rect.anchorMax = max; rect.pivot = pivot;
            rect.anchoredPosition = pos; rect.sizeDelta = size;
        }
    }
}
