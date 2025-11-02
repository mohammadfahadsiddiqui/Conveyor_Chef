// using UnityEngine;
// using UnityEngine.UI;

// namespace Watermelon.BusStop
// {
//     public class LevelPageController : MonoBehaviour
//     {
//         [Header("References")]
//         [SerializeField] LevelButton levelButtonPrefab;
//         [SerializeField] Transform buttonsContainer;
//         [SerializeField] LineRenderer splineRenderer;

//         [Header("Curve Settings")]
//         [SerializeField] AnimationCurve curveShape = AnimationCurve.EaseInOut(0, 0, 1, 0);
//         [SerializeField] float curveHeight = 100f;
//         [SerializeField] float horizontalSpacing = 200f;
//         [SerializeField] Vector2 startOffset = new Vector2(-400f, 0f);

//         private int pageIndex;
//         private int startLevelIndex;
//         private int levelCount;
//         private LevelButton[] levelButtons;

//         public void Setup(int pageIndex, int startLevelIndex, int levelCount)
//         {
//             this.pageIndex = pageIndex;
//             this.startLevelIndex = startLevelIndex;
//             this.levelCount = levelCount;

//             CreateLevelButtons();
//             UpdateSpline();
//         }

//         private void CreateLevelButtons()
//         {
//             levelButtons = new LevelButton[levelCount];

//             for (int i = 0; i < levelCount; i++)
//             {
//                 int levelIndex = startLevelIndex + i;

//                 // Create button
//                 LevelButton button = Instantiate(levelButtonPrefab, buttonsContainer);

//                 // Position button along the curve
//                 Vector2 position = CalculateButtonPosition(i, levelCount);
//                 button.GetComponent<RectTransform>().anchoredPosition = position;

//                 // Setup button state
//                 bool isUnlocked = LevelController.IsLevelUnlocked(levelIndex);
//                 bool isCompleted = LevelController.IsLevelCompleted(levelIndex);
//                 int starsEarned = LevelController.GetLevelStars(levelIndex);

//                 button.Setup(levelIndex, isUnlocked, isCompleted, starsEarned);

//                 levelButtons[i] = button;
//             }
//         }

//         private Vector2 CalculateButtonPosition(int index, int total)
//         {
//             // Calculate normalized position (0 to 1)
//             float t = total > 1 ? (float)index / (total - 1) : 0.5f;

//             // Calculate horizontal position
//             float xPos = startOffset.x + (t * horizontalSpacing * (total - 1));

//             // Calculate vertical position using curve
//             float curveValue = curveShape.Evaluate(t);
//             float yPos = startOffset.y + (curveValue * curveHeight);

//             return new Vector2(xPos, yPos);
//         }

//         private void UpdateSpline()
//         {
//             if (splineRenderer == null || levelButtons == null || levelButtons.Length == 0)
//                 return;

//             // Set number of points for smooth curve
//             int resolution = 50;
//             splineRenderer.positionCount = resolution;

//             // Create smooth curve through all button positions
//             for (int i = 0; i < resolution; i++)
//             {
//                 float t = (float)i / (resolution - 1);
                
//                 // Calculate position along curve using interpolated index
//                 float interpolatedIndex = t * (levelCount - 1);
//                 Vector2 position = CalculateCurvePosition(interpolatedIndex, levelCount);
                
//                 // Convert from RectTransform space to world space
//                 Vector3 worldPos = buttonsContainer.TransformPoint(position);
//                 splineRenderer.SetPosition(i, worldPos);
//             }
//         }

//         private Vector2 CalculateCurvePosition(float index, int total)
//         {
//             // Calculate normalized position (0 to 1)
//             float t = total > 1 ? index / (total - 1) : 0.5f;

//             // Calculate horizontal position
//             float xPos = startOffset.x + (t * horizontalSpacing * (total - 1));

//             // Calculate vertical position using curve
//             float curveValue = curveShape.Evaluate(t);
//             float yPos = startOffset.y + (curveValue * curveHeight);

//             return new Vector2(xPos, yPos);
//         }

//         // Call this if you need to refresh the page (e.g., after completing a level)
//         public void RefreshButtons()
//         {
//             for (int i = 0; i < levelButtons.Length; i++)
//             {
//                 int levelIndex = startLevelIndex + i;

//                 bool isUnlocked = LevelController.IsLevelUnlocked(levelIndex);
//                 bool isCompleted = LevelController.IsLevelCompleted(levelIndex);
//                 int starsEarned = LevelController.GetLevelStars(levelIndex);

//                 levelButtons[i].Setup(levelIndex, isUnlocked, isCompleted, starsEarned);
//             }
//         }
//     }
// }





using UnityEngine;
using UnityEngine.UI;

namespace Watermelon.BusStop
{
    public class LevelPageController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] LevelButton levelButtonPrefab;
        [SerializeField] Transform buttonsContainer;
        [SerializeField] UILineRenderer uiLineRenderer; // We'll create this component

        [Header("Vertical Curve Settings")]
        [SerializeField] AnimationCurve curveShape = AnimationCurve.EaseInOut(0, 0, 1, 0);
        [SerializeField] float curveWidth = 150f; // Horizontal offset for curve
        [SerializeField] float verticalSpacing = 150f; // Space between buttons vertically
        [SerializeField] Vector2 startOffset = new Vector2(0f, -300f); // Start from bottom

        private int pageIndex;
        private int startLevelIndex;
        private int levelCount;
        private LevelButton[] levelButtons;

        public void Setup(int pageIndex, int startLevelIndex, int levelCount)
        {
            this.pageIndex = pageIndex;
            this.startLevelIndex = startLevelIndex;
            this.levelCount = levelCount;

            CreateLevelButtons();
            UpdateCurveLine();
        }

        private void CreateLevelButtons()
        {
            levelButtons = new LevelButton[levelCount];

            for (int i = 0; i < levelCount; i++)
            {
                int levelIndex = startLevelIndex + i;

                // Create button
                LevelButton button = Instantiate(levelButtonPrefab, buttonsContainer);

                // Position button along the vertical curve (bottom to top)
                Vector2 position = CalculateButtonPosition(i, levelCount);
                button.GetComponent<RectTransform>().anchoredPosition = position;

                // Setup button state
                bool isUnlocked = LevelController.IsLevelUnlocked(levelIndex);
                bool isCompleted = LevelController.IsLevelCompleted(levelIndex);
                int starsEarned = LevelController.GetLevelStars(levelIndex);

                button.Setup(levelIndex, isUnlocked, isCompleted, starsEarned);

                levelButtons[i] = button;
            }
        }

        private Vector2 CalculateButtonPosition(int index, int total)
        {
            // Calculate normalized position (0 to 1) - bottom to top
            float t = total > 1 ? (float)index / (total - 1) : 0.5f;

            // Calculate vertical position (bottom to top)
            float yPos = startOffset.y + (t * verticalSpacing * (total - 1));

            // Calculate horizontal position using curve (creates left-right wave)
            float curveValue = curveShape.Evaluate(t);
            float xPos = startOffset.x + (curveValue * curveWidth);

            return new Vector2(xPos, yPos);
        }

        private void UpdateCurveLine()
        {
            if (uiLineRenderer == null || levelButtons == null || levelButtons.Length == 0)
                return;

            // Set number of points for smooth curve
            int resolution = 50;
            Vector2[] points = new Vector2[resolution];

            // Create smooth curve through all button positions
            for (int i = 0; i < resolution; i++)
            {
                float t = (float)i / (resolution - 1);
                
                // Calculate position along curve using interpolated index
                float interpolatedIndex = t * (levelCount - 1);
                points[i] = CalculateCurvePosition(interpolatedIndex, levelCount);
            }

            uiLineRenderer.SetPoints(points);
        }

        private Vector2 CalculateCurvePosition(float index, int total)
        {
            // Calculate normalized position (0 to 1)
            float t = total > 1 ? index / (total - 1) : 0.5f;

            // Calculate vertical position
            float yPos = startOffset.y + (t * verticalSpacing * (total - 1));

            // Calculate horizontal position using curve
            float curveValue = curveShape.Evaluate(t);
            float xPos = startOffset.x + (curveValue * curveWidth);

            return new Vector2(xPos, yPos);
        }

        // Call this if you need to refresh the page (e.g., after completing a level)
        public void RefreshButtons()
        {
            for (int i = 0; i < levelButtons.Length; i++)
            {
                int levelIndex = startLevelIndex + i;

                bool isUnlocked = LevelController.IsLevelUnlocked(levelIndex);
                bool isCompleted = LevelController.IsLevelCompleted(levelIndex);
                int starsEarned = LevelController.GetLevelStars(levelIndex);

                levelButtons[i].Setup(levelIndex, isUnlocked, isCompleted, starsEarned);
            }
        }
    }
}