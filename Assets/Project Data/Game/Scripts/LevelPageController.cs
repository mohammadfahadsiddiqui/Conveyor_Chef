

// using UnityEngine;
// using UnityEngine.UI;

// namespace Watermelon.BusStop
// {
//     public class LevelPageController : MonoBehaviour
//     {
//         [Header("References")]
//         [SerializeField] LevelButton levelButtonPrefab;
//         [SerializeField] Transform buttonsContainer;
//         [SerializeField] UILineRenderer uiLineRenderer; // We'll create this component

//         [Header("Vertical Curve Settings")]
//         [SerializeField] AnimationCurve curveShape = AnimationCurve.EaseInOut(0, 0, 1, 0);
//         [SerializeField] float curveWidth = 150f; // Horizontal offset for curve
//         [SerializeField] float verticalSpacing = 150f; // Space between buttons vertically
//         [SerializeField] Vector2 startOffset = new Vector2(0f, -300f); // Start from bottom

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
//             UpdateCurveLine();
//         }

//         private void CreateLevelButtons()
//         {
//             levelButtons = new LevelButton[levelCount];

//             for (int i = 0; i < levelCount; i++)
//             {
//                 int levelIndex = startLevelIndex + i;

//                 // Create button
//                 LevelButton button = Instantiate(levelButtonPrefab, buttonsContainer);

//                 // Position button along the vertical curve (bottom to top)
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
//             // Calculate normalized position (0 to 1) - bottom to top
//             float t = total > 1 ? (float)index / (total - 1) : 0.5f;

//             // Calculate vertical position (bottom to top)
//             float yPos = startOffset.y + (t * verticalSpacing * (total - 1));

//             // Calculate horizontal position using curve (creates left-right wave)
//             float curveValue = curveShape.Evaluate(t);
//             float xPos = startOffset.x + (curveValue * curveWidth);

//             return new Vector2(xPos, yPos);
//         }

//         private void UpdateCurveLine()
//         {
//             if (uiLineRenderer == null || levelButtons == null || levelButtons.Length == 0)
//                 return;

//             // Set number of points for smooth curve
//             int resolution = 50;
//             Vector2[] points = new Vector2[resolution];

//             // Create smooth curve through all button positions
//             for (int i = 0; i < resolution; i++)
//             {
//                 float t = (float)i / (resolution - 1);

//                 // Calculate position along curve using interpolated index
//                 float interpolatedIndex = t * (levelCount - 1);
//                 points[i] = CalculateCurvePosition(interpolatedIndex, levelCount);
//             }

//             uiLineRenderer.SetPoints(points);
//         }

//         private Vector2 CalculateCurvePosition(float index, int total)
//         {
//             // Calculate normalized position (0 to 1)
//             float t = total > 1 ? index / (total - 1) : 0.5f;

//             // Calculate vertical position
//             float yPos = startOffset.y + (t * verticalSpacing * (total - 1));

//             // Calculate horizontal position using curve
//             float curveValue = curveShape.Evaluate(t);
//             float xPos = startOffset.x + (curveValue * curveWidth);

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


// using UnityEngine;
// using UnityEngine.UI;

// namespace Watermelon.BusStop
// {
//     public class LevelPageController : MonoBehaviour
//     {
//         [Header("References")]
//         [SerializeField] LevelButton levelButtonPrefab;
//         [SerializeField] Transform buttonsContainer;
//         [SerializeField] UILineRenderer uiLineRenderer;

//         [Header("Vertical Curve Settings")]
//         [SerializeField] AnimationCurve curveShape = AnimationCurve.EaseInOut(0, 0, 1, 0);
//         [SerializeField] float curveWidth = 400f; // Increased for more visible curve
//         [SerializeField] float verticalSpacing = 250f;
//         [SerializeField] Vector2 startOffset = new Vector2(0f, -500f);
//         [SerializeField] int curveResolution = 100; // Smoother line

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
//             UpdateCurveLine();
//         }

//         private void CreateLevelButtons()
//         {
//             levelButtons = new LevelButton[levelCount];

//             for (int i = 0; i < levelCount; i++)
//             {
//                 int levelIndex = startLevelIndex + i;

//                 // Create button
//                 LevelButton button = Instantiate(levelButtonPrefab, buttonsContainer);

//                 // Position button along the vertical curve (bottom to top)
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
//             // Calculate normalized position (0 to 1) - bottom to top
//             float t = total > 1 ? (float)index / (total - 1) : 0.5f;

//             // Calculate vertical position (bottom to top)
//             float yPos = startOffset.y + (t * verticalSpacing * (total - 1));

//             // Calculate horizontal position using curve (creates left-right wave)
//             float curveValue = curveShape.Evaluate(t);
//             float xPos = startOffset.x + (curveValue * curveWidth);

//             return new Vector2(xPos, yPos);
//         }

//         private void UpdateCurveLine()
//         {
//             if (uiLineRenderer == null || levelButtons == null || levelButtons.Length == 0)
//                 return;

//             // Set number of points for smooth curve
//             Vector2[] points = new Vector2[curveResolution];

//             // Create smooth curve through all button positions
//             for (int i = 0; i < curveResolution; i++)
//             {
//                 float t = (float)i / (curveResolution - 1);
//                 points[i] = CalculateCurvePosition(t, levelCount);
//             }

//             uiLineRenderer.SetPoints(points);
//         }

//         private Vector2 CalculateCurvePosition(float t, int total)
//         {
//             // Calculate vertical position
//             float yPos = startOffset.y + (t * verticalSpacing * (total - 1));

//             // Calculate horizontal position using curve
//             float curveValue = curveShape.Evaluate(t);
//             float xPos = startOffset.x + (curveValue * curveWidth);

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

//         // Helper for debugging - call this to see the curve in editor
//         private void OnDrawGizmos()
//         {
//             if (curveShape == null) return;

//             Gizmos.color = Color.yellow;
//             Vector2 prevPoint = CalculateCurvePosition(0, 5);

//             for (int i = 1; i < 100; i++)
//             {
//                 float t = (float)i / 99f;
//                 Vector2 point = CalculateCurvePosition(t, 5);

//                 // Convert to world position (only works if this is in scene)
//                 Vector3 worldPrev = transform.TransformPoint(prevPoint);
//                 Vector3 worldPoint = transform.TransformPoint(point);

//                 Gizmos.DrawLine(worldPrev, worldPoint);
//                 prevPoint = point;
//             }
//         }
//     }
// }



//without white line


// using UnityEngine;
// using UnityEngine.UI;

// namespace Watermelon.BusStop
// {
//     public class LevelPageController : MonoBehaviour
//     {
//         [Header("References")]
//         [SerializeField] LevelButton levelButtonPrefab;
//         [SerializeField] Transform buttonsContainer;
//         // [SerializeField] UILineRenderer uiLineRenderer; // Comment this out or remove

//         [Header("Vertical Curve Settings")]
//         [SerializeField] AnimationCurve curveShape = AnimationCurve.EaseInOut(0, 0, 1, 0);
//         [SerializeField] float curveWidth = 400f;
//         [SerializeField] float verticalSpacing = 250f;
//         [SerializeField] Vector2 startOffset = new Vector2(0f, -500f);

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
//             // UpdateCurveLine(); // Remove this line
//         }

//         private void CreateLevelButtons()
//         {
//             levelButtons = new LevelButton[levelCount];

//             for (int i = 0; i < levelCount; i++)
//             {
//                 int levelIndex = startLevelIndex + i;

//                 // Create button
//                 LevelButton button = Instantiate(levelButtonPrefab, buttonsContainer);

//                 // Position button along the vertical curve (bottom to top)
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
//             // Calculate normalized position (0 to 1) - bottom to top
//             float t = total > 1 ? (float)index / (total - 1) : 0.5f;

//             // Calculate vertical position (bottom to top)
//             float yPos = startOffset.y + (t * verticalSpacing * (total - 1));

//             // Calculate horizontal position using curve (creates left-right wave)
//             float curveValue = curveShape.Evaluate(t);
//             float xPos = startOffset.x + (curveValue * curveWidth);

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



//grid

using UnityEngine;
using UnityEngine.UI;

namespace Watermelon.BusStop
{
    public class LevelPageController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] LevelButton levelButtonPrefab;
        [SerializeField] Transform buttonsContainer;

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
        }

        private void CreateLevelButtons()
        {
            levelButtons = new LevelButton[levelCount];

            for (int i = 0; i < levelCount; i++)
            {
                int levelIndex = startLevelIndex + i;

                // Create button - GridLayoutGroup will position it automatically
                LevelButton button = Instantiate(levelButtonPrefab, buttonsContainer);

                // Setup button state
                bool isUnlocked = LevelController.IsLevelUnlocked(levelIndex);
                bool isCompleted = LevelController.IsLevelCompleted(levelIndex);
                int starsEarned = LevelController.GetLevelStars(levelIndex);

                button.Setup(levelIndex, isUnlocked, isCompleted, starsEarned);

                levelButtons[i] = button;
            }
        }

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