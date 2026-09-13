using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon.BusStop
{
    /// <summary>
    /// Visual treatment for the live gameplay HUD. It never moves gameplay world
    /// objects or changes power-up/order callbacks; it only changes UI presentation.
    /// </summary>
    public static class ConveyorChefGameplayHUDStyler
    {
        public static void Style(global::Watermelon.UIMainMenu mainMenu)
        {
            if (mainMenu == null || mainMenu.transform.Find("CC_GameplayChrome") != null)
                return;

            Transform root = mainMenu.transform;
            GameObject marker = new GameObject("CC_GameplayChrome", typeof(RectTransform));
            marker.transform.SetParent(root, false);
            marker.transform.SetAsFirstSibling();

            StyleTopCounters(root);
            StyleOrderPanel(root);
            StyleStoreButtons(root);
        }

        public static void StylePowerups(GameObject gameRoot)
        {
            if (gameRoot == null)
                return;

            Button[] buttons = gameRoot.GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
            {
                string name = button.name.ToLowerInvariant();
                if (name.Contains("undo"))
                    ConveyorChefUITheme.StyleButton(button, ConveyorChefUITheme.Orange, Color.white, 1f);
                else if (name.Contains("hint"))
                    ConveyorChefUITheme.StyleButton(button, ConveyorChefUITheme.Gold, ConveyorChefUITheme.Ink, 1f);
                else if (name.Contains("shuffle"))
                    ConveyorChefUITheme.StyleButton(button, ConveyorChefUITheme.Teal, Color.white, 1f);
                else if (name.Contains("pause") || name.Contains("menu") || name.Contains("quit"))
                    ConveyorChefUITheme.StyleButton(button, ConveyorChefUITheme.Cream, ConveyorChefUITheme.Ink, 1f);
            }

            StylePauseAndExitPopups(gameRoot.transform);
        }

        private static void StyleTopCounters(Transform root)
        {
            TMP_Text[] labels = root.GetComponentsInChildren<TMP_Text>(true);
            foreach (TMP_Text label in labels)
            {
                string n = label.name.ToLowerInvariant();
                string value = label.text == null ? string.Empty : label.text.ToLowerInvariant();
                if (n.Contains("level") || value.Contains("level"))
                {
                    label.color = Color.white;
                    label.fontStyle = FontStyles.Bold;
                    label.fontSize = Mathf.Max(40, label.fontSize);
                    EnsurePill(label.rectTransform, ConveyorChefUITheme.Ink, "CC_LevelPill");
                }
                else if (n.Contains("coin") || n.Contains("currency"))
                {
                    label.fontStyle = FontStyles.Bold;
                    label.color = ConveyorChefUITheme.Ink;
                    EnsurePill(label.rectTransform, ConveyorChefUITheme.Gold, "CC_CoinPill");
                }
                else if (n.Contains("life") || n.Contains("lives") || n.Contains("heart"))
                {
                    label.fontStyle = FontStyles.Bold;
                    label.color = Color.white;
                    EnsurePill(label.rectTransform, ConveyorChefUITheme.Tomato, "CC_LivesPill");
                }
            }
        }

        private static void StyleOrderPanel(Transform root)
        {
            Transform orderRoot = null;
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                string n = t.name.ToLowerInvariant();
                if (n.Contains("order") && (n.Contains("panel") || n.Contains("container") || n == "order"))
                {
                    orderRoot = t;
                    break;
                }
            }

            if (orderRoot == null)
                return;

            Image bg = orderRoot.GetComponent<Image>();
            if (bg != null)
            {
                bg.color = new Color(ConveyorChefUITheme.CreamLight.r, ConveyorChefUITheme.CreamLight.g, ConveyorChefUITheme.CreamLight.b, 0.94f);
                AddShadowOnce(bg, new Vector2(0, -7), 0.30f);
            }

            foreach (TMP_Text text in orderRoot.GetComponentsInChildren<TMP_Text>(true))
            {
                text.fontStyle = FontStyles.Bold;
                text.color = ConveyorChefUITheme.Ink;
            }

            foreach (Image image in orderRoot.GetComponentsInChildren<Image>(true))
            {
                string n = image.name.ToLowerInvariant();
                if (n.Contains("check") || n.Contains("complete"))
                    image.color = ConveyorChefUITheme.Green;
            }
        }

        private static void StylePauseAndExitPopups(Transform root)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                string n = t.name.ToLowerInvariant();
                if (!(n.Contains("popup") || n.Contains("pop up") || n.Contains("exitpanel") || n.Contains("quitpanel")))
                    continue;

                Image panel = t.GetComponent<Image>();
                if (panel != null)
                {
                    panel.color = new Color(ConveyorChefUITheme.CreamLight.r, ConveyorChefUITheme.CreamLight.g, ConveyorChefUITheme.CreamLight.b, 0.98f);
                    AddShadowOnce(panel, new Vector2(0, -12), 0.42f);
                }

                foreach (TMP_Text text in t.GetComponentsInChildren<TMP_Text>(true))
                {
                    text.fontStyle = FontStyles.Bold;
                    text.color = ConveyorChefUITheme.Ink;
                }

                foreach (Button button in t.GetComponentsInChildren<Button>(true))
                {
                    string buttonName = button.name.ToLowerInvariant();
                    if (buttonName.Contains("confirm") || buttonName.Contains("yes") || buttonName.Contains("replay"))
                        ConveyorChefUITheme.StyleButton(button, ConveyorChefUITheme.Tomato, Color.white, 1f);
                    else if (buttonName.Contains("cancel") || buttonName.Contains("no") || buttonName.Contains("back"))
                        ConveyorChefUITheme.StyleButton(button, ConveyorChefUITheme.Cream, ConveyorChefUITheme.Ink, 1f);
                }
            }
        }

        private static void StyleStoreButtons(Transform root)
        {
            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
            {
                string n = button.name.ToLowerInvariant();
                if (n.Contains("home"))
                    ConveyorChefUITheme.StyleButton(button, ConveyorChefUITheme.Cream, ConveyorChefUITheme.Ink, 1f);
            }
        }

        private static void EnsurePill(RectTransform textRect, Color color, string name)
        {
            Transform parent = textRect.parent;
            if (parent == null || parent.Find(name) != null)
                return;

            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            go.transform.SetSiblingIndex(Mathf.Max(0, textRect.GetSiblingIndex()));

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = textRect.anchorMin;
            rect.anchorMax = textRect.anchorMax;
            rect.pivot = textRect.pivot;
            rect.anchoredPosition = textRect.anchoredPosition;
            rect.sizeDelta = textRect.sizeDelta + new Vector2(46, 24);

            Image image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            AddShadowOnce(image, new Vector2(0, -5), 0.30f);
        }

        private static void AddShadowOnce(Image image, Vector2 distance, float alpha)
        {
            if (image == null || image.GetComponent<Shadow>() != null)
                return;

            Shadow shadow = image.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.10f, 0.05f, 0.02f, alpha);
            shadow.effectDistance = distance;
            shadow.useGraphicAlpha = true;
        }
    }
}
