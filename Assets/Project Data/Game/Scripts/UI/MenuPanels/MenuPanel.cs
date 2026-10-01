using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// Base of the main-menu panels. Each panel builds itself on first use on its own overlay
    /// canvas (above the scene UI, below the Settings panel), survives scene loads and closes
    /// when a new scene loads. The frame, title ribbon, close button, optional tabs and
    /// currency bar and the scrolling content area are shared; a panel only fills
    /// <see cref="Content"/> in <see cref="BuildContent"/>, which runs on every open and on
    /// <see cref="Refresh"/>, so the numbers are always current.
    /// </summary>
    public abstract class MenuPanel : MonoBehaviour
    {
        private const int SortingOrder = 4000;
        private const float FrameWidth = 980f;
        private const float MaxFrameHeight = 1620f;
        private const float FrameSliceScale = 1.25f;    // thinner frame borders than the source art

        private static readonly Dictionary<Type, MenuPanel> instances = new Dictionary<Type, MenuPanel>();

        protected MenuPanelArt Art { get; private set; }
        protected RectTransform Content { get; private set; }

        private RectTransform root;
        private RectTransform frame;
        private MenuPanelFrame design;
        private CanvasGroup rootGroup;
        private ScrollRect scroll;
        private RectTransform tabsRow;
        private RectTransform currencyRow;
        private TextMeshProUGUI coinsText;
        private TextMeshProUGUI diamondsText;
        private RectTransform toastRoot;
        private Coroutine animation;
        private int selectedTab;

        protected abstract string Title { get; }
        protected virtual string[] Tabs => null;
        protected virtual bool ShowCurrencies => false;
        /// <summary>Designed frame picture in Resources/MenuPanelFrames (e.g. StoryPanel -> "story").</summary>
        protected virtual string DesignKey => GetType().Name.Replace("Panel", string.Empty).ToLowerInvariant();
        /// <summary>Tab to show each time the panel opens; -1 keeps the last one.</summary>
        protected virtual int OpeningTab => -1;
        protected int SelectedTab => selectedTab;

        public static bool AnyOpen
        {
            get
            {
                foreach (MenuPanel panel in instances.Values)
                {
                    if (panel != null && panel.root != null && panel.root.gameObject.activeSelf)
                        return true;
                }
                return false;
            }
        }

        public static T Show<T>() where T : MenuPanel
        {
            if (!instances.TryGetValue(typeof(T), out MenuPanel panel) || panel == null)
            {
                GameObject go = new GameObject("[" + typeof(T).Name.ToUpperInvariant() + "]");
                DontDestroyOnLoad(go);
                panel = go.AddComponent<T>();
                panel.Build();
                instances[typeof(T)] = panel;
            }

            panel.Open();
            return (T)panel;
        }

        protected abstract void BuildContent();

        protected virtual void OnOpened() { }

        protected virtual void OnClosed() { }

        /// <summary>Rebuild the content (keeps the scroll position).</summary>
        protected void Refresh()
        {
            float position = scroll != null ? scroll.verticalNormalizedPosition : 1f;
            RebuildContent();
            if (scroll != null)
            {
                Canvas.ForceUpdateCanvases();
                scroll.verticalNormalizedPosition = position;
            }
        }

        protected void SelectTab(int index)
        {
            selectedTab = index;
            BuildTabs();
            RebuildContent();
            if (scroll != null)
                scroll.verticalNormalizedPosition = 1f;
        }

        public void Close()
        {
            if (root == null || !root.gameObject.activeSelf)
                return;

            root.gameObject.SetActive(false);
            OnClosed();
        }

        #region Messages and rewards

        /// <summary>Short message that rises and fades above the panel.</summary>
        protected void Toast(string message, Color? color = null)
        {
            if (toastRoot == null)
                return;

            TextMeshProUGUI text = MenuUI.OutlinedText("Toast", toastRoot, message, 54f, color ?? new Color32(60, 40, 20, 255));
            MenuUI.Place(text.rectTransform, Vector2.zero, new Vector2(900f, 90f));
            StartCoroutine(FloatAway(text));
        }

        protected static bool Spend(CurrencyType currency, int amount)
        {
            try
            {
                if (!CurrenciesController.HasAmount(currency, amount))
                    return false;
                CurrenciesController.Substract(currency, amount);
                SaveNow();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[MenuPanel] Could not spend currency: " + ex.Message);
                return false;
            }
        }

        protected void Grant(CurrencyType currency, int amount)
        {
            try
            {
                CurrenciesController.Add(currency, amount);
                SaveNow();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[MenuPanel] Could not add currency: " + ex.Message);
            }

            try { AudioController.PlaySound(AudioController.Sounds.completeSound); } catch { }
            Toast("+" + amount.ToString("N0") + (currency == CurrencyType.Coins ? " COINS" : " DIAMONDS"), new Color32(40, 110, 30, 255));
            UpdateCurrencies();
        }

        /// <summary>Writes the save right away, so purchases and rewards survive closing the app.</summary>
        protected static void SaveNow()
        {
            try
            {
                SaveController.MarkAsSaveIsRequired();
                SaveController.Save(true);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[MenuPanel] Could not save: " + ex.Message);
            }
        }

        protected Sprite CurrencyIcon(CurrencyType currency) => currency == CurrencyType.Coins ? Art.coin : Art.diamond;

        protected static int GetCurrency(CurrencyType currency)
        {
            try { return CurrenciesController.Get(currency); } catch { return 0; }
        }

        #endregion

        #region Open / close

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
            instances.Remove(GetType());
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Close();
        }

        private void Open()
        {
            MenuUI.PlayClick();
            FitToScreen();
            if (OpeningTab >= 0)
                selectedTab = OpeningTab;
            selectedTab = Mathf.Clamp(selectedTab, 0, Tabs != null ? Tabs.Length - 1 : 0);
            BuildTabs();
            RebuildContent();
            rootGroup.alpha = 0f;
            root.gameObject.SetActive(true);
            scroll.verticalNormalizedPosition = 1f;
            OnOpened();

            if (animation != null)
                StopCoroutine(animation);
            animation = StartCoroutine(PopIn());
        }

        private void RebuildContent()
        {
            ClearChildren(Content);
            UpdateCurrencies();
            BuildContent();
        }

        // Destroy is deferred, so children are detached first and the layout ignores them at once.
        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }
        }

        private IEnumerator PopIn()
        {
            const float duration = 0.22f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / duration);
                float back = 1f + 2.2f * Mathf.Pow(k - 1f, 3f) + 1.2f * Mathf.Pow(k - 1f, 2f);   // ease-out-back
                frame.localScale = Vector3.one * Mathf.LerpUnclamped(0.86f, 1f, back);
                rootGroup.alpha = Mathf.Clamp01(k * 2f);
                yield return null;
            }

            frame.localScale = Vector3.one;
            rootGroup.alpha = 1f;
            animation = null;
        }

        private IEnumerator FloatAway(TextMeshProUGUI text)
        {
            const float duration = 1.1f;
            float t = 0f;
            RectTransform rect = text.rectTransform;
            while (t < duration && text != null)
            {
                t += Time.unscaledDeltaTime;
                float k = t / duration;
                rect.anchoredPosition = new Vector2(0f, 140f * k);
                text.alpha = 1f - Mathf.Clamp01((k - 0.5f) * 2f);
                yield return null;
            }

            if (text != null)
                Destroy(text.gameObject);
        }

        private void FitToScreen()
        {
            Canvas.ForceUpdateCanvases();
            float height = ((RectTransform)transform).rect.height;
            if (height <= 1f)
                height = 1920f;

            if (design != null)
            {
                // The designed picture keeps its proportions: as tall as fits, no wider than the screen.
                float width = ((RectTransform)transform).rect.width;
                if (width <= 1f)
                    width = 1080f;
                float frameHeight = Mathf.Min(DesignedMaxHeight, height - 120f);
                float frameWidth = frameHeight * design.Aspect;
                if (frameWidth > width - 40f)
                {
                    frameWidth = width - 40f;
                    frameHeight = frameWidth / design.Aspect;
                }
                frame.sizeDelta = new Vector2(frameWidth, frameHeight);
                return;
            }

            frame.sizeDelta = new Vector2(FrameWidth, Mathf.Min(MaxFrameHeight, height - 250f));
        }

        private const float DesignedMaxHeight = 1780f;
        private const float DesignedInset = 14f;   // keeps content off the cream area's rounded rim

        #endregion

        #region Build

        private void Build()
        {
            Art = Resources.Load<MenuPanelArt>(MenuPanelArt.ResourcePath);
            if (Art == null)
            {
                Debug.LogWarning("[MenuPanel] Resources/" + MenuPanelArt.ResourcePath + " not found; panels use plain shapes.");
                Art = ScriptableObject.CreateInstance<MenuPanelArt>();
            }
            MenuUI.Font = Art.font;

            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            gameObject.AddComponent<GraphicRaycaster>();

            root = MenuUI.Rect(GetType().Name, transform);
            MenuUI.Stretch(root);
            rootGroup = root.gameObject.AddComponent<CanvasGroup>();

            // Dim background: tapping outside the panel closes it.
            Image dim = MenuUI.Image("Dim", root, null, new Color(0.03f, 0.05f, 0.1f, 0.75f));
            MenuUI.Stretch(dim.rectTransform);
            MenuUI.Button(dim, Close, false).transition = Selectable.Transition.None;

            design = MenuPanelFrame.Load(DesignKey);

            RectTransform body;
            if (design != null)
            {
                // The panel's own designed picture, title ribbon and close button included.
                Image frameImage = MenuUI.Image("Frame", root, design.Sprite, null, true);
                frameImage.raycastTarget = true;   // taps on the panel itself do not close it
                frame = frameImage.rectTransform;
                MenuUI.Place(frame, Vector2.zero, new Vector2(FrameWidth, FrameWidth / design.Aspect));

                body = MenuUI.Rect("Body", frame);
                body.anchorMin = design.CreamMin;
                body.anchorMax = design.CreamMax;
                body.offsetMin = new Vector2(DesignedInset, DesignedInset);
                body.offsetMax = new Vector2(-DesignedInset, -DesignedInset);
            }
            else
            {
                Image frameImage = MenuUI.Image("Frame", root, Art.frame, Art.frame == null ? MenuUI.CardFill : (Color?)null, false);
                frameImage.pixelsPerUnitMultiplier = FrameSliceScale;
                frameImage.raycastTarget = true;   // taps on the panel itself do not close it
                frame = frameImage.rectTransform;
                MenuUI.Place(frame, new Vector2(0f, -20f), new Vector2(FrameWidth, MaxFrameHeight));

                // Inside of the frame (the cream area), from the 9-slice borders.
                Vector4 border = Art.frame != null ? Art.frame.border / FrameSliceScale : new Vector4(40f, 40f, 40f, 40f);
                body = MenuUI.Rect("Body", frame);
                MenuUI.Stretch(body, border.x + 14f, border.y + 16f, border.z + 14f, border.w + 18f);
            }

            BuildHeader();

            RectTransform stack = MenuUI.Rect("Stack", body);
            MenuUI.Stretch(stack);
            MenuUI.Vertical(stack, 12f).childForceExpandHeight = false;

            if (ShowCurrencies)
            {
                currencyRow = MenuUI.Rect("Currencies", stack);
                LayoutElement currencyLayout = currencyRow.gameObject.AddComponent<LayoutElement>();
                currencyLayout.preferredHeight = 72f;
                currencyLayout.flexibleHeight = 0f;   // only the scroll area takes the spare height
                BuildCurrencyRow();
            }

            if (Tabs != null && Tabs.Length > 0)
            {
                tabsRow = MenuUI.Rect("Tabs", stack);
                LayoutElement tabsLayout = tabsRow.gameObject.AddComponent<LayoutElement>();
                tabsLayout.preferredHeight = 96f;
                tabsLayout.flexibleHeight = 0f;
            }

            RectTransform scrollRect = MenuUI.Rect("Scroll", stack);
            LayoutElement scrollLayout = scrollRect.gameObject.AddComponent<LayoutElement>();
            scrollLayout.flexibleHeight = 1f;
            BuildScroll(scrollRect);

            toastRoot = MenuUI.Rect("Toasts", root);
            MenuUI.Place(toastRoot, new Vector2(0f, 120f), new Vector2(900f, 200f));

            root.gameObject.SetActive(false);
        }

        private void BuildHeader()
        {
            if (design != null)
            {
                // Title and X are drawn in the picture; only the X needs a hit area.
                Image closeArea = MenuUI.Image("Close Button", frame, null, new Color(1f, 1f, 1f, 0f));
                closeArea.rectTransform.anchorMin = design.CloseMin;
                closeArea.rectTransform.anchorMax = design.CloseMax;
                closeArea.rectTransform.offsetMin = new Vector2(-12f, -12f);   // a little larger than the drawn button
                closeArea.rectTransform.offsetMax = new Vector2(12f, 12f);
                MenuUI.Button(closeArea, Close, false).transition = Selectable.Transition.None;
                return;
            }

            Image ribbon = MenuUI.Image("Title Ribbon", frame, Art.titleRibbon, Art.titleRibbon == null ? new Color32(200, 40, 40, 255) : (Color?)null);
            MenuUI.Anchor(ribbon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(760f, 204f));

            TextMeshProUGUI title = MenuUI.OutlinedText("Title", ribbon.transform, Title, 62f, new Color32(110, 20, 20, 255));
            MenuUI.Stretch(title.rectTransform, 150f, 70f, 150f, 50f);

            Image close = MenuUI.Image("Close Button", frame, Art.closeButton, Art.closeButton == null ? new Color32(220, 60, 50, 255) : (Color?)null);
            MenuUI.Anchor(close.rectTransform, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-34f, -34f), new Vector2(118f, 118f));
            MenuUI.Button(close, Close);
        }

        private void BuildScroll(RectTransform area)
        {
            scroll = area.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 40f;
            scroll.decelerationRate = 0.12f;

            RectTransform viewport = MenuUI.Rect("Viewport", area);
            MenuUI.Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            Image hit = viewport.gameObject.AddComponent<Image>();   // lets drags start anywhere
            hit.color = new Color(1f, 1f, 1f, 0f);

            Content = MenuUI.Rect("Content", viewport);
            Content.anchorMin = new Vector2(0f, 1f);
            Content.anchorMax = new Vector2(1f, 1f);
            Content.pivot = new Vector2(0.5f, 1f);
            Content.anchoredPosition = Vector2.zero;
            Content.sizeDelta = Vector2.zero;
            MenuUI.Vertical(Content, 20f, new RectOffset(6, 6, 8, 24));
            ContentSizeFitter fitter = Content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = Content;
        }

        private void BuildTabs()
        {
            if (tabsRow == null)
                return;

            ClearChildren(tabsRow);

            HorizontalLayoutGroup layout = tabsRow.GetComponent<HorizontalLayoutGroup>();
            if (layout == null)
            {
                layout = tabsRow.gameObject.AddComponent<HorizontalLayoutGroup>();
                layout.spacing = 10f;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = true;
            }

            string[] tabs = Tabs;
            for (int i = 0; i < tabs.Length; i++)
            {
                int index = i;
                bool selected = i == selectedTab;
                MenuUI.LabelButton("Tab " + tabs[i], tabsRow, selected ? Art.buttonOrange : Art.buttonGray, tabs[i],
                    tabs.Length > 4 ? 30f : 38f, () => { if (index != selectedTab) SelectTab(index); }, null,
                    selected ? new Color32(140, 70, 10, 255) : new Color32(70, 70, 70, 255));
            }
        }

        private void BuildCurrencyRow()
        {
            HorizontalLayoutGroup layout = currencyRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleRight;
            layout.spacing = 16f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            layout.padding = new RectOffset(0, 8, 4, 4);

            coinsText = BuildCurrencyPill(Art.coin);
            diamondsText = BuildCurrencyPill(Art.diamond);
        }

        private TextMeshProUGUI BuildCurrencyPill(Sprite icon)
        {
            Image pill = MenuUI.Image("Pill", currencyRow, MenuUI.Rounded(30, 3, new Color32(90, 56, 30, 235), new Color32(226, 178, 96, 255)), null, false);
            pill.gameObject.AddComponent<LayoutElement>().preferredWidth = 250f;

            Image iconImage = MenuUI.Image("Icon", pill.transform, icon);
            MenuUI.Anchor(iconImage.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(34f, 0f), new Vector2(66f, 66f));

            TextMeshProUGUI text = MenuUI.Text("Amount", pill.transform, "0", 40f, Color.white);
            MenuUI.Stretch(text.rectTransform, 70f, 4f, 16f, 4f);
            return text;
        }

        protected void UpdateCurrencies()
        {
            if (coinsText != null)
                coinsText.text = GetCurrency(CurrencyType.Coins).ToString("N0");
            if (diamondsText != null)
                diamondsText.text = GetCurrency(CurrencyType.Diamonds).ToString("N0");
        }

        #endregion
    }
}
