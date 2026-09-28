using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// "Country Completed!" panel, shown once when the player finishes the last level of a
    /// country. Country name, landmark, decoration and the next country are filled in from
    /// <see cref="CountryCatalog"/> and Resources/CountryCompleteArt. It builds itself on
    /// first use on its own overlay canvas above all scene UI and pops in as one piece.
    /// </summary>
    public sealed class CountryCompletePanel : MonoBehaviour
    {
        private const int SortingOrder = 4500;
        private static readonly Color32 Brown = new Color32(92, 40, 18, 255);
        private const string NextCountryColor = "#1E4FB0";

        private static CountryCompletePanel instance;

        private CountryCompleteArt art;
        private GameObject root;
        private RectTransform content;
        private TMP_Text countryName;
        private TMP_Text summary;
        private Image landmark;
        private Image decorWide;
        private Image decorLeft;
        private Image decorRight;
        private GameObject nextCardGroup;
        private Image nextLandmark;
        private TMP_Text nextName;
        private Action onContinue;
        private Action onHome;
        private TweenCase popTween;

        public static bool IsOpen => instance != null && instance.root != null && instance.root.activeSelf;

        public static void Show(int country, Action continueAction, Action homeAction)
        {
            if (instance == null)
            {
                GameObject go = new GameObject("[COUNTRY COMPLETE PANEL]");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<CountryCompletePanel>();
                instance.Build(Resources.Load<CountryCompleteArt>(CountryCompleteArt.ResourcePath));
            }

            instance.Open(country, continueAction, homeAction);
        }

        public static void Hide()
        {
            if (instance != null && instance.root != null)
                instance.root.SetActive(false);
        }

        private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
        private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Hide();

        private void Open(int country, Action continueAction, Action homeAction)
        {
            onContinue = continueAction;
            onHome = homeAction;

            string name = CountryCatalog.GetName(country);
            int next = CountryCatalog.GetNext(country);

            countryName.text = name.ToUpperInvariant();

            SetSprite(landmark, art != null ? art.GetLandmark(country) : null);

            bool wide = art != null && art.IsDecorWide(country);
            Sprite decor = art != null ? art.GetDecor(country) : null;
            SetSprite(decorWide, wide ? decor : null);
            SetSprite(decorLeft, wide ? null : decor);
            SetSprite(decorRight, wide ? null : decor);

            if (next >= 0)
            {
                string nextNameText = CountryCatalog.GetName(next).ToUpperInvariant();
                summary.text = "Great job! " + name + " is complete.\nYour next destination is <color=" +
                               NextCountryColor + ">" + nextNameText + "</color>";
                nextName.text = nextNameText;
                SetSprite(nextLandmark, art != null ? art.GetLandmark(next) : null);
                nextCardGroup.SetActive(true);
            }
            else
            {
                summary.text = "Amazing! " + name + " is complete.\nYou have completed every country!";
                nextCardGroup.SetActive(false);
            }

            root.SetActive(true);

            // Everything, buttons included, pops in together.
            popTween.KillActive();
            content.localScale = Vector3.zero;
            popTween = content.DOPushScale(Vector3.one * 1.08f, Vector3.one, 0.32f, 0.18f, Ease.Type.CubicOut, Ease.Type.CubicIn);

            PlaySound(true);
        }

        private void Continue()
        {
            PlaySound(false);
            root.SetActive(false);
            onContinue?.Invoke();
        }

        private void Home()
        {
            PlaySound(false);
            root.SetActive(false);
            onHome?.Invoke();
        }

        #region Build

        // Layout at 1080x1920, measured from the Country Completed reference image.
        private void Build(CountryCompleteArt panelArt)
        {
            art = panelArt;
            if (art == null)
                Debug.LogWarning("[CountryComplete] Resources/" + CountryCompleteArt.ResourcePath + " not found.");

            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            // Expand keeps the whole 1080x1920 layout on screen on tall phones and tablets.
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            gameObject.AddComponent<GraphicRaycaster>();

            RectTransform rootRect = CreateRect("Country Complete", transform);
            Stretch(rootRect);
            root = rootRect.gameObject;

            Image dim = CreateImage("Dim", rootRect, null, new Color(0.04f, 0.05f, 0.12f, 0.8f));
            Stretch(dim.rectTransform);
            dim.raycastTarget = true; // blocks taps on the scene behind

            content = CreateRect("Content", rootRect);
            content.anchorMin = content.anchorMax = content.pivot = new Vector2(0.5f, 0.5f);
            content.sizeDelta = new Vector2(1080f, 1920f);

            Sprite Art(Func<CountryCompleteArt, Sprite> pick) => art != null ? pick(art) : null;

            Add("Sparkle", Art(a => a.sparkle), 0, 760, 900, 675);
            Add("Frame", Art(a => a.frame), 0, -60, 1020, 1600, preserveAspect: false);
            Add("Crown", Art(a => a.crown), 0, 770, 620, 413);
            Add("Title", Art(a => a.title), 0, 560, 820, 273);

            TMP_Text you = AddText("You Completed", "You completed", Brown, 46f, 0, 402, 600, 52);
            you.fontStyle = FontStyles.Bold;

            decorWide = Add("Decor", null, 0, 312, 440, 147);
            decorLeft = Add("Decor Left", null, -300, 322, 140, 140);
            decorRight = Add("Decor Right", null, 300, 322, 140, 140);
            decorRight.rectTransform.localScale = new Vector3(-1f, 1f, 1f);

            countryName = AddText("Country Name", "COUNTRY", new Color32(255, 214, 60, 255), 96f, 0, 322, 640, 104);
            Outline(countryName, new Color32(110, 40, 10, 255), 0.28f);

            landmark = Add("Landmark", null, 0, 165, 560, 420);

            Add("Ribbon", Art(a => a.ribbon), 0, -28, 880, 293);
            Add("Flowers Left", Art(a => a.flowersLeft), -390, -35, 200, 200);
            Add("Flowers Right", Art(a => a.flowersRight), 390, -35, 200, 200);
            for (int i = -1; i <= 1; i++)
            {
                float x = 185f * i;
                Add("Star Slot", Art(a => a.starSlot), x, -20, 165, 165);
                Add("Star", Art(a => a.star), x, -16, 116, 116);
                Add("Check", Art(a => a.check), x + 40, -56, 69, 69);
            }

            summary = AddText("Summary", "", Brown, 40f, 0, -168, 820, 96);
            summary.richText = true;
            summary.textWrappingMode = TextWrappingModes.Normal;

            RectTransform card = CreateRect("Next Country", content);
            Stretch(card);
            nextCardGroup = card.gameObject;
            AddTo(card, "Card", Art(a => a.nextCard), 0, -398, 560, 300, preserveAspect: false);
            nextLandmark = AddTo(card, "Next Landmark", null, 0, -440, 330, 248);
            nextName = AddTextTo(card, "Next Name", "NEXT", new Color32(190, 30, 40, 255), 70f, 0, -335, 380, 78);
            Outline(nextName, Color.white, 0.25f);
            AddTo(card, "Next Label", Art(a => a.nextLabel), 0, -258, 320, 107);

            Button continueButton = AddButton(Add("Continue Button", Art(a => a.continueButton), 0, -606, 420, 140), Continue);
            Button homeButton = AddButton(Add("Home Button", Art(a => a.homeButton), 0, -732, 130, 130), Home);

            if (art == null || art.continueButton == null)
                AddTextTo(continueButton.transform, "Label", "CONTINUE", Color.white, 48f, 0, 0, 380, 110);
            if (art == null || art.homeButton == null)
                AddTextTo(homeButton.transform, "Label", "HOME", Color.white, 30f, 0, 0, 120, 60);

            root.SetActive(false);
        }

        private Image Add(string name, Sprite sprite, float x, float y, float w, float h, bool preserveAspect = true)
        {
            return AddTo(content, name, sprite, x, y, w, h, preserveAspect);
        }

        private Image AddTo(Transform parent, string name, Sprite sprite, float x, float y, float w, float h, bool preserveAspect = true)
        {
            RectTransform rect = CreateRect(name, parent);
            Place(rect, x, y, w, h);
            rect.gameObject.AddComponent<CanvasRenderer>();
            Image image = rect.gameObject.AddComponent<Image>();
            image.preserveAspect = preserveAspect;
            image.raycastTarget = false;
            SetSprite(image, sprite);
            return image;
        }

        private TMP_Text AddText(string name, string value, Color color, float size, float x, float y, float w, float h)
        {
            return AddTextTo(content, name, value, color, size, x, y, w, h);
        }

        private TMP_Text AddTextTo(Transform parent, string name, string value, Color color, float size, float x, float y, float w, float h)
        {
            RectTransform rect = CreateRect(name, parent);
            Place(rect, x, y, w, h);
            rect.gameObject.AddComponent<CanvasRenderer>();
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (art != null && art.font != null)
                text.font = art.font;
            text.text = value;
            text.color = color;
            text.fontSize = size;
            text.enableAutoSizing = true;
            text.fontSizeMin = size * 0.45f;
            text.fontSizeMax = size;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            return text;
        }

        private static void Outline(TMP_Text text, Color color, float width)
        {
            // Runtime material instance: only this text gets the outline.
            text.fontMaterial.EnableKeyword("OUTLINE_ON");
            text.outlineColor = color;
            text.outlineWidth = width;
        }

        private static Button AddButton(Image image, Action action)
        {
            image.raycastTarget = true;
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => action());
            return button;
        }

        private static void SetSprite(Image image, Sprite sprite)
        {
            image.sprite = sprite;
            image.enabled = sprite != null;
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
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void Place(RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(w, h);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void PlaySound(bool complete)
        {
            try
            {
                AudioController.PlaySound(complete ? AudioController.Sounds.completeSound : AudioController.Sounds.buttonSound);
            }
            catch
            {
                // Audio may not be initialised when testing a scene directly.
            }
        }

        #endregion
    }
}
