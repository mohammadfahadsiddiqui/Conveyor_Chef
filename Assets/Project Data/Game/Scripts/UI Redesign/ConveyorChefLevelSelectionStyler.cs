using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon.BusStop
{
    /// <summary>
    /// Uses the approved level-selection artwork as the visual backdrop while the
    /// existing real level buttons continue to own progression and click behaviour.
    /// </summary>
    public static class ConveyorChefLevelSelectionStyler
    {
        private const string FullQualityArtworkResource = "UIReference/LevelSelectionReference";
        private const string FallbackArtworkResource = "UIReference/LevelSelectionReference.b64";

        public static void Style(LevelSelectionController controller)
        {
            if (controller == null)
                return;

            Canvas canvas = controller.GetComponentInParent<Canvas>();
            if (canvas == null)
                canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
                return;

            AddApprovedBackdrop(canvas.transform);
            HideLegacyBackdropArt(canvas.gameObject);
            RestyleLevelButtons(canvas.gameObject);
            StyleNavigation(canvas.gameObject);
        }

        private static void AddApprovedBackdrop(Transform canvas)
        {
            if (canvas.Find("CC_ApprovedLevelSelectionArtwork") != null)
                return;

            Texture2D artwork = Resources.Load<Texture2D>(FullQualityArtworkResource);
            if (artwork == null)
            {
                artwork = UIReferenceImageLoader.LoadTexture(FallbackArtworkResource);
                Debug.LogWarning("[Conveyor Chef UI] Full-quality LevelSelectionReference.png was not found. Using fallback artwork.");
            }
            else
            {
                Debug.Log($"[Conveyor Chef UI] Using full-quality LevelSelectionReference.png ({artwork.width}x{artwork.height}).");
            }

            GameObject backdrop = new GameObject("CC_ApprovedLevelSelectionArtwork", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            backdrop.layer = LayerMask.NameToLayer("UI");
            backdrop.transform.SetParent(canvas, false);
            backdrop.transform.SetAsFirstSibling();

            RectTransform rect = backdrop.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            RawImage image = backdrop.GetComponent<RawImage>();
            image.texture = artwork;
            image.color = artwork != null ? Color.white : new Color(0.55f, 0.83f, 0.96f, 1f);
            image.raycastTarget = false;
        }

        private static void HideLegacyBackdropArt(GameObject root)
        {
            Image[] images = root.GetComponentsInChildren<Image>(true);
            foreach (Image image in images)
            {
                if (image == null || image.GetComponent<Button>() != null)
                    continue;

                string name = image.name.ToLowerInvariant();
                if (name.Contains("background") || name == "bg" || name.Contains("scooter") || name.Contains("map"))
                    image.enabled = false;
            }
        }

        private static void RestyleLevelButtons(GameObject root)
        {
            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
            {
                if (!LooksLikeLevelButton(button))
                    continue;

                int levelNumber = ExtractLevelNumber(button);
                if (levelNumber <= 0)
                    continue;

                int levelIndex = levelNumber - 1;
                bool completed = false;
                try
                {
                    completed = LevelController.IsLevelCompleted(levelIndex);
                }
                catch (Exception)
                {
                    // Existing button state remains authoritative while saves initialise.
                }

                Color stateColor;
                if (!button.interactable)
                    stateColor = new Color(0.38f, 0.39f, 0.42f, 1f);
                else if (completed)
                    stateColor = new Color(1f, 0.66f, 0.08f, 1f);
                else
                    stateColor = new Color(0.35f, 0.84f, 0.17f, 1f);

                Image image = button.GetComponent<Image>();
                if (image != null)
                {
                    image.color = stateColor;
                    Shadow shadow = image.GetComponent<Shadow>();
                    if (shadow == null)
                        shadow = image.gameObject.AddComponent<Shadow>();
                    shadow.effectColor = new Color(0.18f, 0.08f, 0.02f, 0.42f);
                    shadow.effectDistance = new Vector2(0f, -7f);
                    shadow.useGraphicAlpha = true;
                }

                TMP_Text[] labels = button.GetComponentsInChildren<TMP_Text>(true);
                foreach (TMP_Text label in labels)
                {
                    label.fontStyle = FontStyles.Bold;
                    if (IsNumeric(label.text))
                    {
                        label.fontSize = Mathf.Max(label.fontSize, 48f);
                        label.color = button.interactable ? new Color(0.28f, 0.12f, 0.05f, 1f) : Color.white;
                    }
                }
            }
        }

        private static void StyleNavigation(GameObject root)
        {
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            {
                string name = button.name.ToLowerInvariant();
                if (!(name.Contains("back") || name.Contains("previous") || name.Contains("prev") || name.Contains("next") || name.Contains("forward")))
                    continue;

                Image image = button.GetComponent<Image>();
                if (image != null)
                    image.color = new Color(1f, 0.94f, 0.80f, 1f);

                foreach (TMP_Text text in button.GetComponentsInChildren<TMP_Text>(true))
                {
                    text.fontStyle = FontStyles.Bold;
                    text.color = new Color(0.31f, 0.14f, 0.06f, 1f);
                }
            }
        }

        private static bool LooksLikeLevelButton(Button button)
        {
            string name = button.name.ToLowerInvariant();
            if (name.Contains("level"))
                return true;
            return ExtractLevelNumber(button) > 0;
        }

        private static int ExtractLevelNumber(Button button)
        {
            foreach (TMP_Text label in button.GetComponentsInChildren<TMP_Text>(true))
            {
                if (int.TryParse(label.text == null ? string.Empty : label.text.Trim(), out int value))
                    return value;
            }
            return -1;
        }

        private static bool IsNumeric(string value)
        {
            return int.TryParse(value == null ? string.Empty : value.Trim(), out _);
        }
    }
}
