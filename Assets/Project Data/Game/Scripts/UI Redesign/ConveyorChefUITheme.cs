using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Watermelon.BusStop
{
    /// <summary>
    /// Shared visual system for the Conveyor Chef UI overhaul.
    /// It deliberately styles existing controls instead of replacing gameplay logic.
    /// </summary>
    public static class ConveyorChefUITheme
    {
        public static readonly Color Ink = Hex("3B2419");
        public static readonly Color Cream = Hex("FFF3D6");
        public static readonly Color CreamLight = Hex("FFF9EA");
        public static readonly Color Tomato = Hex("E9342D");
        public static readonly Color TomatoDark = Hex("A91519");
        public static readonly Color Orange = Hex("FF9F1C");
        public static readonly Color Gold = Hex("FFC928");
        public static readonly Color Green = Hex("53C83C");
        public static readonly Color Teal = Hex("21A59A");
        public static readonly Color Sky = Hex("7FD3FF");
        public static readonly Color Locked = Hex("8C8C8C");
        public static readonly Color White = Color.white;
        public static readonly Color Overlay = new Color(0.12f, 0.07f, 0.04f, 0.72f);

        private const string SCENE_STYLER = "CC_UI_SceneStyler";
        private static TMP_FontAsset cachedFont;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallSceneHook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (GameObject.Find(SCENE_STYLER) != null)
                return;

            GameObject runner = new GameObject(SCENE_STYLER);
            UnityEngine.Object.DontDestroyOnLoad(runner);
            runner.AddComponent<SceneStyleRunner>().Begin(scene.name);
        }

        public static void StyleLevelSelectionScene(MonoBehaviour owner)
        {
            if (owner == null || owner.gameObject == null)
                return;

            CacheFont(owner.gameObject);
            Canvas canvas = FindBestCanvas(owner.gameObject.scene);
            if (canvas == null)
                return;

            canvas.GetComponent<CanvasScaler>()?.SetReferenceResolutionSafe(new Vector2(1080, 1920));
            EnsureSafeAreaBackdrop(canvas.transform, "SELECT LEVEL", "Delicious challenges await!");
            StyleButtons(canvas.gameObject, true);
            StyleTexts(canvas.gameObject, true);

            Button[] buttons = canvas.GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
            {
                string n = button.name.ToLowerInvariant();
                if (n.Contains("level"))
                {
                    Image image = button.GetComponent<Image>();
                    if (image != null)
                        image.color = button.interactable ? Orange : Locked;

                    AddShadow(image, new Vector2(0, -8), 0.34f);
                }
                else if (n.Contains("back") || n.Contains("prev") || n.Contains("next"))
                {
                    StyleButton(button, Cream, Ink, 1.0f);
                }
            }
        }

        public static void StyleGameplayPage(GameObject pageRoot, TMP_Text levelText, Button replayButton)
        {
            if (pageRoot == null)
                return;

            CacheFont(pageRoot);
            StyleButtons(pageRoot, false);
            StyleTexts(pageRoot, false);

            if (levelText != null)
            {
                levelText.fontStyle = FontStyles.Bold;
                levelText.color = White;
                levelText.fontSize = Mathf.Max(levelText.fontSize, 42f);
                levelText.enableWordWrapping = false;
            }

            if (replayButton != null)
                StyleButton(replayButton, Cream, Ink, 1f);

            Transform safeRoot = FindSafeRoot(pageRoot.transform);
            if (safeRoot != null && safeRoot.Find("CC_GameplayTopFrame") == null)
            {
                RectTransform frame = CreatePanel("CC_GameplayTopFrame", safeRoot, Ink, 0.88f);
                frame.SetAsFirstSibling();
                SetRect(frame, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(0f, 150f));

                RectTransform accent = CreatePanel("Accent", frame, Tomato, 1f);
                SetRect(accent, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 8f));
            }
        }

        public static void StyleCompletePage(GameObject pageRoot)
        {
            if (pageRoot == null)
                return;

            CacheFont(pageRoot);
            AddPopupCard(pageRoot.transform, "LEVEL COMPLETED!", Gold, Tomato);
            StyleButtons(pageRoot, false);
            StyleTexts(pageRoot, false);

            foreach (Button button in pageRoot.GetComponentsInChildren<Button>(true))
            {
                string n = button.name.ToLowerInvariant();
                if (n.Contains("multiply") || n.Contains("reward") || n.Contains("x3"))
                    StyleButton(button, Green, White, 1.05f);
                else if (n.Contains("no") || n.Contains("continue") || n.Contains("next"))
                    StyleButton(button, Tomato, White, 1.05f);
                else if (n.Contains("home"))
                    StyleButton(button, Cream, Ink, 1f);
            }
        }

        public static void StyleGameOverPage(GameObject pageRoot)
        {
            if (pageRoot == null)
                return;

            CacheFont(pageRoot);
            AddPopupCard(pageRoot.transform, "LEVEL FAILED", Tomato, TomatoDark);
            StyleButtons(pageRoot, false);
            StyleTexts(pageRoot, false);

            foreach (Button button in pageRoot.GetComponentsInChildren<Button>(true))
            {
                string n = button.name.ToLowerInvariant();
                if (n.Contains("replay"))
                    StyleButton(button, Tomato, White, 1.06f);
                else if (n.Contains("home"))
                    StyleButton(button, Cream, Ink, 1f);
            }
        }

        public static void StyleLoadingScene(Scene scene)
        {
            Canvas canvas = FindBestCanvas(scene);
            if (canvas == null)
                return;

            CacheFont(canvas.gameObject);
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            scaler?.SetReferenceResolutionSafe(new Vector2(1080, 1920));

            Transform root = canvas.transform;
            if (root.Find("CC_LoadingPolish") != null)
                return;

            RectTransform polish = CreatePanel("CC_LoadingPolish", root, Color.clear, 0f);
            Stretch(polish);
            polish.SetAsLastSibling();

            TMP_Text title = CreateText("Brand", polish, "CONVEYOR CHEF", 74, Gold, FontStyles.Bold);
            SetRect(title.rectTransform, new Vector2(0.08f, 0.73f), new Vector2(0.92f, 0.88f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            TMP_Text sub = CreateText("SubBrand", polish, "FOOD RUSH", 38, White, FontStyles.Bold);
            SetRect(sub.rectTransform, new Vector2(0.12f, 0.69f), new Vector2(0.88f, 0.76f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            sub.characterSpacing = 5;

            RectTransform track = CreatePanel("ProgressTrack", polish, new Color(0.12f, 0.07f, 0.04f, 0.9f), 1f);
            SetRect(track, new Vector2(0.16f, 0.08f), new Vector2(0.84f, 0.08f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0, 44));
            AddOutline(track.GetComponent<Image>(), Gold, 4f);

            RectTransform fill = CreatePanel("ProgressFill", track, Orange, 1f);
            fill.anchorMin = new Vector2(0f, 0f);
            fill.anchorMax = new Vector2(0.72f, 1f);
            fill.offsetMin = new Vector2(7f, 7f);
            fill.offsetMax = new Vector2(-7f, -7f);

            TMP_Text tip = CreateText("Tip", polish, "Preparing today’s food rush…", 27, White, FontStyles.Bold);
            SetRect(tip.rectTransform, new Vector2(0.08f, 0.015f), new Vector2(0.92f, 0.075f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        }

        public static void StyleButton(Button button, Color background, Color textColor, float scale = 1f)
        {
            if (button == null)
                return;

            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = background;
                AddShadow(image, new Vector2(0, -7), 0.32f);
            }

            TMP_Text[] labels = button.GetComponentsInChildren<TMP_Text>(true);
            foreach (TMP_Text label in labels)
            {
                label.color = textColor;
                label.fontStyle = FontStyles.Bold;
                if (cachedFont != null)
                    label.font = cachedFont;
            }

            button.transform.localScale = Vector3.one * scale;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.96f, 0.88f, 1f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            colors.selectedColor = Color.white;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
        }

        private static void StyleButtons(GameObject root, bool selectionScreen)
        {
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            {
                string n = button.name.ToLowerInvariant();
                if (n.Contains("hint")) StyleButton(button, Gold, Ink);
                else if (n.Contains("shuffle")) StyleButton(button, Teal, White);
                else if (n.Contains("undo")) StyleButton(button, Orange, White);
                else if (n.Contains("replay")) StyleButton(button, Cream, Ink);
                else if (n.Contains("settings") || n.Contains("setting")) StyleButton(button, Cream, Ink);
                else if (!selectionScreen) StyleButton(button, Tomato, White);
            }
        }

        private static void StyleTexts(GameObject root, bool selectionScreen)
        {
            foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (cachedFont != null)
                    text.font = cachedFont;

                string n = text.name.ToLowerInvariant();
                if (n.Contains("level"))
                    text.fontStyle = FontStyles.Bold;

                if (selectionScreen && (n.Contains("title") || n.Contains("header")))
                    text.color = CreamLight;
            }
        }

        private static void EnsureSafeAreaBackdrop(Transform canvas, string titleText, string subtitleText)
        {
            if (canvas.Find("CC_LevelSelectionChrome") != null)
                return;

            RectTransform chrome = CreatePanel("CC_LevelSelectionChrome", canvas, Color.clear, 0f);
            Stretch(chrome);
            chrome.SetAsFirstSibling();

            RectTransform header = CreatePanel("Header", chrome, Ink, 0.93f);
            SetRect(header, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 245f));

            RectTransform accent = CreatePanel("Accent", header, Tomato, 1f);
            SetRect(accent, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 12f));

            TMP_Text title = CreateText("Title", header, titleText, 68, CreamLight, FontStyles.Bold);
            SetRect(title.rectTransform, new Vector2(0.06f, 0.28f), new Vector2(0.94f, 0.88f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            TMP_Text subtitle = CreateText("Subtitle", header, subtitleText, 27, Gold, FontStyles.Bold);
            SetRect(subtitle.rectTransform, new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.34f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        }

        private static void AddPopupCard(Transform root, string title, Color accent, Color titleColor)
        {
            if (root.Find("CC_PopupCard") != null)
                return;

            RectTransform card = CreatePanel("CC_PopupCard", root, CreamLight, 0.98f);
            card.SetAsFirstSibling();
            SetRect(card, new Vector2(0.10f, 0.18f), new Vector2(0.90f, 0.82f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            AddShadow(card.GetComponent<Image>(), new Vector2(0, -12), 0.42f);

            RectTransform strip = CreatePanel("Accent", card, accent, 1f);
            SetRect(strip, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 18f));

            TMP_Text header = CreateText("Header", card, title, 60, titleColor, FontStyles.Bold);
            SetRect(header.rectTransform, new Vector2(0.06f, 0.77f), new Vector2(0.94f, 0.96f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            header.raycastTarget = false;
        }

        private static Transform FindSafeRoot(Transform root)
        {
            RectTransform[] rects = root.GetComponentsInChildren<RectTransform>(true);
            foreach (RectTransform rect in rects)
            {
                string n = rect.name.ToLowerInvariant();
                if (n.Contains("safe"))
                    return rect;
            }
            return root;
        }

        private static Canvas FindBestCanvas(Scene scene)
        {
            Canvas[] all = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Canvas best = null;
            int bestScore = int.MinValue;
            foreach (Canvas canvas in all)
            {
                if (canvas.gameObject.scene != scene)
                    continue;

                int score = canvas.sortingOrder;
                if (canvas.isRootCanvas) score += 100;
                if (canvas.enabled) score += 10;
                if (score > bestScore)
                {
                    best = canvas;
                    bestScore = score;
                }
            }
            return best;
        }

        private static void CacheFont(GameObject root)
        {
            if (cachedFont != null || root == null)
                return;

            TMP_Text[] labels = root.GetComponentsInChildren<TMP_Text>(true);
            foreach (TMP_Text label in labels)
            {
                if (label.font != null)
                {
                    cachedFont = label.font;
                    break;
                }
            }
        }

        private static TMP_Text CreateText(string name, Transform parent, string value, float size, Color color, FontStyles style)
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
            if (cachedFont != null) text.font = cachedFont;
            return text;
        }

        private static RectTransform CreatePanel(string name, Transform parent, Color color, float alpha)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            Image image = go.GetComponent<Image>();
            image.color = new Color(color.r, color.g, color.b, alpha);
            image.raycastTarget = false;
            return go.GetComponent<RectTransform>();
        }

        private static void AddShadow(Image image, Vector2 effectDistance, float alpha)
        {
            if (image == null || image.GetComponent<Shadow>() != null)
                return;
            Shadow shadow = image.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.14f, 0.07f, 0.03f, alpha);
            shadow.effectDistance = effectDistance;
            shadow.useGraphicAlpha = true;
        }

        private static void AddOutline(Image image, Color color, float size)
        {
            if (image == null || image.GetComponent<Outline>() != null)
                return;
            Outline outline = image.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(size, -size);
            outline.useGraphicAlpha = true;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetRect(RectTransform rect, Vector2 min, Vector2 max, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = pivot;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
        }

        private static Color Hex(string hex)
        {
            if (ColorUtility.TryParseHtmlString("#" + hex, out Color color))
                return color;
            return Color.white;
        }

        private sealed class SceneStyleRunner : MonoBehaviour
        {
            private string targetScene;
            public void Begin(string sceneName)
            {
                targetScene = sceneName;
                StartCoroutine(ApplyAfterFrame());
            }

            private IEnumerator ApplyAfterFrame()
            {
                yield return null;
                yield return null;

                Scene scene = SceneManager.GetSceneByName(targetScene);
                if (scene.IsValid() && targetScene.Equals("loading", StringComparison.OrdinalIgnoreCase))
                    StyleLoadingScene(scene);

                Destroy(gameObject);
            }
        }
    }

    internal static class CanvasScalerExtensions
    {
        public static void SetReferenceResolutionSafe(this CanvasScaler scaler, Vector2 reference)
        {
            if (scaler == null) return;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = reference;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }
    }
}
