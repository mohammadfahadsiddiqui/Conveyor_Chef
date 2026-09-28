using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// The game's single Settings panel, opened by every settings button (main menu,
    /// World Map, Country Map, Continent Map, Level Selection, Game HUD) through
    /// <see cref="Show"/>. It builds itself on first use on its own overlay canvas above
    /// all scene UI, survives scene loads, and closes when a new scene loads.
    /// Art comes from Resources/SettingsPanelArt.
    /// </summary>
    public sealed class ConveyorSettingsPanel : MonoBehaviour
    {
        private const int SortingOrder = 5000;

        private static ConveyorSettingsPanel instance;

        private GameObject root;
        private Image soundImage;
        private Image vibrationImage;
        private TMP_Text soundLabel;
        private TMP_Text vibrationLabel;
        private SettingsPanelArt art;

        public static bool IsOpen => instance != null && instance.root != null && instance.root.activeSelf;

        public static void Show()
        {
            EnsureInstance();
            instance.Open();
        }

        public static void Hide()
        {
            if (instance != null)
                instance.Close(playSound: false);
        }

        private static void EnsureInstance()
        {
            if (instance != null)
                return;

            GameObject go = new GameObject("[SETTINGS PANEL]");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<ConveyorSettingsPanel>();
            instance.Build(Resources.Load<SettingsPanelArt>(SettingsPanelArt.ResourcePath));
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Close(playSound: false);
        }

        private void Open()
        {
            PlayClick();
            Refresh();
            root.SetActive(true);
        }

        private void Close(bool playSound)
        {
            if (root == null || !root.activeSelf)
                return;

            if (playSound)
                PlayClick();

            root.SetActive(false);
        }

        private void ToggleSound()
        {
            bool enabled = IsSoundOn();
            try { AudioController.SetVolume(enabled ? 0f : 1f); } catch { }
            PlayClick();
            Refresh();
        }

        private void ToggleVibration()
        {
            bool enabled = IsVibrationOn();
            try { AudioController.SetVibrationState(!enabled); } catch { }
            PlayClick();
            Refresh();
        }

        // Audio may not be initialised when a scene is opened directly in the editor;
        // the panel keeps working and assumes the defaults.
        private static bool IsSoundOn()
        {
            try { return AudioController.GetVolume() > 0.001f; } catch { return true; }
        }

        private static bool IsVibrationOn()
        {
            try { return AudioController.IsVibrationEnabled(); } catch { return true; }
        }

        private void Refresh()
        {
            bool soundOn = IsSoundOn();
            bool vibrationOn = IsVibrationOn();

            ApplyState(soundImage, soundLabel, soundOn,
                art != null ? art.soundOn : null, art != null ? art.soundOff : null, "SOUND");
            ApplyState(vibrationImage, vibrationLabel, vibrationOn,
                art != null ? art.vibrationOn : null, art != null ? art.vibrationOff : null, "VIBRATION");
        }

        private static void ApplyState(Image image, TMP_Text label, bool on, Sprite onSprite, Sprite offSprite, string name)
        {
            Sprite sprite = on ? onSprite : offSprite;
            if (sprite != null)
            {
                image.sprite = sprite;
                image.color = Color.white;
            }
            else
            {
                image.sprite = null;
                image.color = on ? new Color(0.2f, 0.65f, 0.25f) : new Color(0.75f, 0.25f, 0.2f);
            }

            label.gameObject.SetActive(sprite == null);
            label.text = name + (on ? ": ON" : ": OFF");
        }

        #region Build

        // Layout at 1080x1920. Panel: pause_popup_panel.png (660x880, blue title band at
        // 11%, cream area 19-88% of its height).
        private void Build(SettingsPanelArt panelArt)
        {
            art = panelArt;
            if (art == null)
                Debug.LogWarning("[Settings] Resources/" + SettingsPanelArt.ResourcePath + " not found; using plain buttons.");

            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;

            gameObject.AddComponent<GraphicRaycaster>();

            RectTransform rootRect = CreateRect("Settings", transform);
            Stretch(rootRect);
            root = rootRect.gameObject;

            // Dim background: tapping outside the panel closes it.
            Image dim = CreateImage("Dim", rootRect, null, new Color(0.03f, 0.05f, 0.1f, 0.72f));
            Stretch(dim.rectTransform);
            AddButton(dim, () => Close(playSound: true)).transition = Selectable.Transition.None;

            const float panelH = 880f;
            float top = panelH * 0.5f;

            Image panel = CreateImage("Panel", rootRect, art != null ? art.panel : null, new Color(0.98f, 0.93f, 0.8f));
            Place(panel.rectTransform, Vector2.zero, new Vector2(660f, panelH));
            panel.preserveAspect = true;
            panel.raycastTarget = true; // taps on the panel itself do not close it

            if (art != null && art.title != null)
            {
                Image title = CreateImage("Title", rootRect, art.title, Color.white);
                Place(title.rectTransform, new Vector2(0f, top - panelH * 0.11f), new Vector2(500f, 150f));
                title.preserveAspect = true;
            }
            else
            {
                TMP_Text title = CreateText("Title", rootRect, "SETTINGS", Color.white, 64f);
                Place(title.rectTransform, new Vector2(0f, top - panelH * 0.11f), new Vector2(500f, 110f));
            }

            Image close = CreateImage("Close Button", rootRect, art != null ? art.closeButton : null, new Color(0.85f, 0.2f, 0.2f));
            Place(close.rectTransform, new Vector2(282f, top - 52f), new Vector2(100f, 100f));
            close.preserveAspect = true;
            AddButton(close, () => Close(playSound: true));

            soundImage = CreateToggle("Sound Button", rootRect, new Vector2(0f, top - panelH * 0.40f), out soundLabel);
            AddButton(soundImage, ToggleSound);

            vibrationImage = CreateToggle("Vibration Button", rootRect, new Vector2(0f, top - panelH * 0.60f), out vibrationLabel);
            AddButton(vibrationImage, ToggleVibration);

            TMP_Text version = CreateText("Version", rootRect, "VERSION " + Application.version, new Color32(35, 48, 70, 255), 34f);
            Place(version.rectTransform, new Vector2(0f, top - panelH * 0.80f), new Vector2(500f, 56f));

            root.SetActive(false);
        }

        private Image CreateToggle(string name, Transform parent, Vector2 position, out TMP_Text label)
        {
            Image image = CreateImage(name, parent, null, Color.white);
            Place(image.rectTransform, position, new Vector2(470f, 157f));
            image.preserveAspect = true;

            label = CreateText("Label", image.transform, name, Color.white, 44f);
            Stretch(label.rectTransform);
            return image;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            rect.gameObject.AddComponent<CanvasRenderer>();
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = sprite != null ? Color.white : color;
            image.raycastTarget = false;
            return image;
        }

        private TMP_Text CreateText(string name, Transform parent, string value, Color color, float size)
        {
            RectTransform rect = CreateRect(name, parent);
            rect.gameObject.AddComponent<CanvasRenderer>();
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (art != null && art.font != null)
                text.font = art.font;
            text.text = value;
            text.color = color;
            text.fontSize = size;
            text.enableAutoSizing = true;
            text.fontSizeMin = size * 0.5f;
            text.fontSizeMax = size;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            return text;
        }

        private static Button AddButton(Image image, UnityEngine.Events.UnityAction action)
        {
            image.raycastTarget = true;
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);
            return button;
        }

        private static void Place(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void PlayClick()
        {
            try { AudioController.PlaySound(AudioController.Sounds.buttonSound); } catch { }
        }

        #endregion
    }
}
