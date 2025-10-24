using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Watermelon.BusStop
{
    public class UIOrderItem : MonoBehaviour
    {
        [SerializeField] Image busIcon;
        [SerializeField] TextMeshProUGUI orderText;
        [SerializeField] Image[] checkmarks; // Visual checkmarks for each required bus
        [SerializeField] GameObject completedOverlay;
        
        private LevelElement.Type busType;
        private int requiredAmount;
        private int completedAmount;
        
        public void Initialize(LevelElement.Type busType, int requiredAmount, Sprite busSprite)
        {
            this.busType = busType;
            this.requiredAmount = requiredAmount;
            this.completedAmount = 0;
            
            busIcon.sprite = busSprite;
            
            // Setup checkmarks
            if (checkmarks != null && checkmarks.Length > 0)
            {
                for (int i = 0; i < checkmarks.Length; i++)
                {
                    if (i < requiredAmount)
                    {
                        checkmarks[i].gameObject.SetActive(true);
                        checkmarks[i].color = new Color(1, 1, 1, 0.3f); // Dim/uncompleted
                    }
                    else
                    {
                        checkmarks[i].gameObject.SetActive(false);
                    }
                }
            }
            
            if (completedOverlay != null)
                completedOverlay.SetActive(false);
            
            UpdateDisplay();
        }
        
        public void UpdateProgress(int completedAmount)
        {
            this.completedAmount = completedAmount;
            UpdateDisplay();
        }
        
        private void UpdateDisplay()
        {
            orderText.text = $"{completedAmount}/{requiredAmount}";
            
            // Update checkmarks
            if (checkmarks != null)
            {
                for (int i = 0; i < checkmarks.Length && i < requiredAmount; i++)
                {
                    if (i < completedAmount)
                    {
                        checkmarks[i].color = Color.white; // Bright/completed
                    }
                    else
                    {
                        checkmarks[i].color = new Color(1, 1, 1, 0.3f); // Dim/uncompleted
                    }
                }
            }
            
            // Show completed overlay if all done
            if (completedOverlay != null)
            {
                completedOverlay.SetActive(completedAmount >= requiredAmount);
            }
        }
    }
}