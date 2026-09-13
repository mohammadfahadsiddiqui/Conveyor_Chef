using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Watermelon.BusStop
{
    /// <summary>
    /// Uses the approved Conveyor Chef artwork as the main-menu presentation while
    /// keeping scene navigation and settings interactions as real Unity buttons.
    /// </summary>
    public sealed class MainMenuRedesignController : MonoBehaviour
    {
        private const string RootName = "CC_MainMenu_Approved";
        private const string SoundPrefKey = "CC_SOUND_MUTED";
        private const string ArtworkResource = "UIReference/MenuReference.b64";

        private sceneloading sceneLoader;
        private Canvas legacyCanvas;
        private TMP_FontAsset font;
        private RectTransform safeArea;
        private Rect lastSafeArea;
        private GameObject settingsPopup;
        private TMP_Text soundLabel;
        private bool initialised;

        public void Initialise(sceneloading loader)
        {
            if (initialised || loader == null)
                return;

            initialised = true;
            sceneLoader = loader;

            GameObject existing = GameObject.Find(RootName);
            if (existing != null)
                Destroy(existing);

            CaptureLegacyReferences();
            BuildApprovedMenu();
            ApplyStoredSoundState();

            if (legacyCanvas != null)
                legacyCanvas.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (safeArea != null && Screen.safeArea != lastSafeArea)
                ApplySafeArea();
        }

        private void CaptureLegacyReferences()
        {
            Scene scene = SceneManager.GetActiveScene();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Canvas[] canvases = root.GetComponentsInChildren<Canvas>(true);
                if (legacyCanvas == null && canvases.Length > 0)
                    legacyCanvas = canvases[0];

                foreach (TextMeshProUGUI text in root.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    if (font == null && text.font != null)
                        font = text.font;
                }
            }
        }

        private void BuildApprovedMenu()
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
            Texture2D artwork = UIReferenceImageLoader.LoadTexture(ArtworkResource);

            GameObject backgroundObject = new GameObject("ApprovedMainMenuArtwork", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            backgroundObject.layer = LayerMask.NameToLayer("UI");
            backgroundObject.transform.SetParent(canvasRect, false);
            RawImage background = backgroundObject.GetComponent<RawImage>();
            background.texture = artwork;
            background.color = artwork != null ? Color.white : new Color(0.95f, 0.72f, 0.32f, 1f);
            background.raycastTarget = false;
            Stretch(background.rectTransform);

            safeArea = CreateRect("SafeArea", canvasRect);
            ApplySafeArea();

            // The approved artwork already contains the visible PLAY button. This is
            // an invisible hit target placed directly over it.
            Button playButton = CreateHitButton("PlayHitTarget", safeArea, PlayGame);
            Place(playButton.GetComponent<RectTransform>(), new Vector2(0.17f, 0.055f), new Vector2(0.83f, 0.185f));

            // Same approach for the settings icon drawn in the artwork.
            Button settingsButton = CreateHitButton("SettingsHitTarget", safeArea, OpenSettings);
            Place(settingsButton.GetComponent<RectTransform>(), new Vector2(0.84f, 0.895f), new Vector2(0.98f, 0.985f));

            BuildSettingsPopup(canvasRect);
        }

        private void BuildSettingsPopup(RectTransform canvasRect)
        {
            settingsPopup = new GameObject("CC_SettingsPopup", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            settingsPopup.layer = LayerMask.NameToLayer("UI");
            settingsPopup.transform.SetParent(canvasRect, false);
            Stretch(settingsPopup.GetComponent<RectTransform>());

            Image dim = settingsPopup.GetComponent<Image>();
            dim.color = new Color(0.06f, 0.03f, 0.02f, 0.78f);

            GameObject cardObject = new GameObject("Card", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            cardObject.layer = LayerMask.NameToLayer("UI");
            cardObject.transform.SetParent(settingsPopup.transform, false);
            Image card = cardObject.GetComponent<Image>();
            card.color = new Color(1f, 0.94f, 0.80f, 1f);
            Place(card.rectTransform, new Vector2(0.13f, 0.29f), new Vector2(0.87f, 0.72f));

            TMP_Text title = CreateText("Title", card.transform, "SETTINGS", 58f, new Color(0.25f, 0.12f, 0.06f, 1f));
            Place(title.rectTransform, new Vector2(0.08f, 0.72f), new Vector2(0.92f, 0.93f));

            Button soundButton = CreateVisibleButton("SoundButton", card.transform, new Color(0.10f, 0.62f, 0.56f, 1f), ToggleSound);
            Place(soundButton.GetComponent<RectTransform>(), new Vector2(0.14f, 0.44f), new Vector2(0.86f, 0.64f));
            soundLabel = CreateText("SoundLabel", soundButton.transform, "SOUND: ON", 36f, Color.white);
            Stretch(soundLabel.rectTransform);

            Button backButton = CreateVisibleButton("BackButton", card.transform, new Color(1f, 0.69f, 0.18f, 1f), CloseSettings);
            Place(backButton.GetComponent<RectTransform>(), new Vector2(0.23f, 0.13f), new Vector2(0.77f, 0.31f));
            TMP_Text back = CreateText("BackLabel", backButton.transform, "BACK", 34f, new Color(0.25f, 0.12f, 0.06f, 1f));
            Stretch(back.rectTransform);

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

        private void ToggleSound()
        {
            bool muteNow = AudioListener.volume > 0.001f;
            AudioListener.volume = muteNow ? 0f : 1f;
            PlayerPrefs.SetInt(SoundPrefKey, muteNow ? 1 : 0);
            PlayerPrefs.Save();
            RefreshSoundLabel();

            if (!muteNow)
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

        private Button CreateHitButton(string name, Transform parent, UnityEngine.Events.UnityAction action)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            Image image = go.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.001f);
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);
            return button;
        }

        private Button CreateVisibleButton(string name, Transform parent, Color color, UnityEngine.Events.UnityAction action)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            Image image = go.GetComponent<Image>();
            image.color = color;
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);
            return button;
        }

        private TMP_Text CreateText(string name, Transform parent, string value, float size, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.raycastTarget = false;
            if (font != null) text.font = font;
            return text;
        }

        private RectTransform CreateRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Place(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
