using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Watermelon.BusStop
{
    public sealed class LevelSelectionInteractionRepair : MonoBehaviour
    {
        const int PerPage = 12;
        LevelSelectionController controller;
        TMP_FontAsset font;
        int page;
        Button[] levels;
        TMP_Text[] labels;
        Button prev;
        Button next;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (!SceneManager.GetActiveScene().name.Equals("LevelSelection", System.StringComparison.OrdinalIgnoreCase)) return;
            var go = new GameObject("CC_LevelSelectionInteractionRepair");
            go.AddComponent<LevelSelectionInteractionRepair>();
        }

        IEnumerator Start()
        {
            for (int i = 0; i < 30; i++)
            {
                controller = FindFirstObjectByType<LevelSelectionController>(FindObjectsInactive.Include);
                if (controller != null) break;
                yield return null;
            }
            if (controller == null) yield break;

            yield return null;
            Build();
            Refresh();
        }

        void Build()
        {
            foreach (var lb in FindObjectsByType<LevelButton>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var t = lb.GetComponentInChildren<TextMeshProUGUI>(true);
                if (font == null && t != null) font = t.font;
                lb.gameObject.SetActive(false);
            }

            var root = new GameObject("CC_RealLevelControls", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            levels = new Button[PerPage];
            labels = new TMP_Text[PerPage];
            float[] xs = { .285f, .5f, .715f };
            float[] ys = { .585f, .455f, .325f, .195f };
            int slot = 0;
            for (int r = 0; r < 4; r++)
                for (int c = 0; c < 3; c++, slot++)
                    levels[slot] = CreateLevel(root.transform, slot, xs[c], ys[r]);

            prev = CreateNav(root.transform, "BACK", new Vector2(.055f, .035f), new Vector2(.205f, .115f), Previous);
            next = CreateNav(root.transform, "NEXT", new Vector2(.78f, .035f), new Vector2(.955f, .115f), Next);
        }

        Button CreateLevel(Transform parent, int slot, float x, float y)
        {
            var go = new GameObject("RealLevel_" + slot, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(x, y);
            rect.sizeDelta = new Vector2(185, 175);
            var image = go.GetComponent<Image>();
            image.color = new Color(1f, .68f, .08f, .96f);
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            int captured = slot;
            button.onClick.AddListener(() => Open(captured));
            labels[slot] = Text(go.transform, "", 58);
            return button;
        }

        Button CreateNav(Transform parent, string text, Vector2 min, Vector2 max, UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject(text, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = go.GetComponent<Image>();
            image.color = new Color(1f, .94f, .8f, .98f);
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);
            Text(go.transform, text, 28).color = new Color(.3f, .14f, .06f, 1f);
            return button;
        }

        TMP_Text Text(Transform parent, string value, float size)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var t = go.GetComponent<TextMeshProUGUI>();
            t.text = value; t.fontSize = size; t.fontStyle = FontStyles.Bold; t.alignment = TextAlignmentOptions.Center;
            t.color = new Color(.27f, .12f, .05f, 1f); t.raycastTarget = false;
            if (font != null) t.font = font;
            return t;
        }

        void Refresh()
        {
            int total = 50;
            int start = page * PerPage;
            for (int i = 0; i < PerPage; i++)
            {
                int index = start + i;
                bool exists = index < total;
                levels[i].gameObject.SetActive(exists);
                if (!exists) continue;
                bool unlocked = LevelController.IsLevelUnlocked(index);
                bool completed = LevelController.IsLevelCompleted(index);
                levels[i].interactable = unlocked;
                labels[i].text = unlocked ? (index + 1).ToString() : "LOCK";
                levels[i].GetComponent<Image>().color = !unlocked ? new Color(.45f, .45f, .48f, .98f) : completed ? new Color(1f, .68f, .08f, .98f) : new Color(.32f, .86f, .18f, .98f);
                labels[i].color = unlocked ? new Color(.27f, .12f, .05f, 1f) : Color.white;
            }
            next.interactable = page < Mathf.CeilToInt(total / (float)PerPage) - 1;
        }

        void Open(int slot)
        {
            int index = page * PerPage + slot;
            if (index < 50 && LevelController.IsLevelUnlocked(index)) controller.LoadSelectedLevel(index);
        }

        void Previous()
        {
            if (page > 0) { page--; Refresh(); }
            else SceneManager.LoadScene("menu");
        }

        void Next()
        {
            if (page < Mathf.CeilToInt(50f / PerPage) - 1) { page++; Refresh(); }
        }
    }
}
