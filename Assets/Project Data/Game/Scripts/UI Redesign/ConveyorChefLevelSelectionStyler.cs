using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon.BusStop
{
    /// <summary>
    /// Applies the generated level-map visual language to the existing real level
    /// buttons while leaving unlock/progression callbacks untouched.
    /// </summary>
    public static class ConveyorChefLevelSelectionStyler
    {
        public static void Style(LevelSelectionController controller)
        {
            if (controller == null) return;

            ConveyorChefUITheme.StyleLevelSelectionScene(controller);

            Canvas canvas = controller.GetComponentInParent<Canvas>();
            if (canvas == null)
                canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null) return;

            AddMapBackdrop(canvas.transform);
            RestyleLevelButtons(canvas.gameObject);
            StyleNavigation(canvas.gameObject);
        }

        private static void AddMapBackdrop(Transform canvas)
        {
            if (canvas.Find("CC_LevelMapBackdrop") != null) return;

            GameObject backdrop = new GameObject("CC_LevelMapBackdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            backdrop.layer = LayerMask.NameToLayer("UI");
            backdrop.transform.SetParent(canvas, false);
            backdrop.transform.SetAsFirstSibling();

            RectTransform rect = backdrop.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            Image image = backdrop.GetComponent<Image>();
            image.color = new Color(0.55f, 0.83f, 0.96f, 1f);
            image.raycastTarget = false;

            // Warm road/map panel behind the actual level buttons.
            GameObject road = new GameObject("RoadMap", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            road.layer = LayerMask.NameToLayer("UI");
            road.transform.SetParent(backdrop.transform, false);
            RectTransform roadRect = road.GetComponent<RectTransform>();
            roadRect.anchorMin = new Vector2(0.05f, 0.08f);
            roadRect.anchorMax = new Vector2(0.95f, 0.82f);
            roadRect.offsetMin = roadRect.offsetMax = Vector2.zero;
            Image roadImage = road.GetComponent<Image>();
            roadImage.color = new Color(0.98f, 0.82f, 0.57f, 0.94f);
            roadImage.raycastTarget = false;

            // Decorative map path made from alternating road markers.
            for (int i = 0; i < 7; i++)
            {
                GameObject marker = new GameObject("PathMarker" + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                marker.layer = LayerMask.NameToLayer("UI");
                marker.transform.SetParent(road.transform, false);
                RectTransform m = marker.GetComponent<RectTransform>();
                float y = 0.09f + i * 0.13f;
                m.anchorMin = new Vector2(i % 2 == 0 ? 0.29f : 0.58f, y);
                m.anchorMax = m.anchorMin;
                m.sizeDelta = new Vector2(160, 18);
                Image mi = marker.GetComponent<Image>();
                mi.color = new Color(1f, 1f, 1f, 0.58f);
                mi.raycastTarget = false;
            }
        }

        private static void RestyleLevelButtons(GameObject root)
        {
            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
            {
                if (!LooksLikeLevelButton(button)) continue;

                int levelNumber = ExtractLevelNumber(button);
                if (levelNumber <= 0) continue;

                int levelIndex = levelNumber - 1;
                bool completed = false;
                try
                {
                    completed = LevelController.IsLevelCompleted(levelIndex);
                }
                catch (Exception)
                {
                    // Existing button state remains authoritative if saves are not ready yet.
                }

                Color stateColor;
                if (!button.interactable)
                    stateColor = ConveyorChefUITheme.Locked;
                else if (completed)
                    stateColor = ConveyorChefUITheme.Gold;
                else
                    stateColor = ConveyorChefUITheme.Green;

                ConveyorChefUITheme.StyleButton(button, stateColor,
                    button.interactable ? ConveyorChefUITheme.Ink : Color.white,
                    completed || !button.interactable ? 1f : 1.07f);

                TMP_Text[] labels = button.GetComponentsInChildren<TMP_Text>(true);
                foreach (TMP_Text label in labels)
                {
                    label.fontStyle = FontStyles.Bold;
                    if (IsNumeric(label.text))
                    {
                        label.fontSize = Mathf.Max(label.fontSize, 48f);
                        label.color = button.interactable ? ConveyorChefUITheme.Ink : Color.white;
                    }
                }
            }
        }

        private static void StyleNavigation(GameObject root)
        {
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            {
                string n = button.name.ToLowerInvariant();
                if (n.Contains("back") || n.Contains("previous") || n.Contains("prev"))
                    ConveyorChefUITheme.StyleButton(button, ConveyorChefUITheme.CreamLight, ConveyorChefUITheme.Ink, 1f);
                else if (n.Contains("next") || n.Contains("forward"))
                    ConveyorChefUITheme.StyleButton(button, ConveyorChefUITheme.CreamLight, ConveyorChefUITheme.Ink, 1f);
            }
        }

        private static bool LooksLikeLevelButton(Button button)
        {
            string n = button.name.ToLowerInvariant();
            if (n.Contains("level")) return true;
            return ExtractLevelNumber(button) > 0;
        }

        private static int ExtractLevelNumber(Button button)
        {
            TMP_Text[] labels = button.GetComponentsInChildren<TMP_Text>(true);
            foreach (TMP_Text label in labels)
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
