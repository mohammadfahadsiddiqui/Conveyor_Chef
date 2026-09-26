using System.Collections.Generic;

namespace Watermelon.BusStop
{
    [System.Serializable]
    public class LevelSave : ISaveObject
    {
        public int RealLevelNumber = 0;
        public int DisplayLevelNumber = 0;
        public bool ReplayingLevelAgain = false;

        public int selectedLevelIndex = 0;
        public bool isPlayingFromLevelSelection = false;

        public List<LevelProgressData> levelProgress = new List<LevelProgressData>();

        // NEW: Property to get the correct level number to display
        public int GetCurrentDisplayLevel()
        {
            if (isPlayingFromLevelSelection)
            {
                return selectedLevelIndex + 1; // Convert to 1-based
            }
            else
            {
                return DisplayLevelNumber;
            }
        }

        public void Flush()
        {

        }

        public LevelProgressData GetLevelProgress(int levelIndex)
        {
            for (int i = 0; i < levelProgress.Count; i++)
            {
                if (levelProgress[i].levelIndex == levelIndex)
                    return levelProgress[i];
            }
            return null;
        }

        public int GetTotalStars()
        {
            int total = 0;

            if (levelProgress == null)
                return total;

            for (int i = 0; i < levelProgress.Count; i++)
            {
                LevelProgressData data = levelProgress[i];
                if (data == null)
                    continue;

                total += System.Math.Max(0, data.starsEarned);
            }

            return total;
        }

        public void SetLevelProgress(int levelIndex, bool isCompleted, int starsEarned)
        {
            LevelProgressData data = GetLevelProgress(levelIndex);

            if (data == null)
            {
                data = new LevelProgressData();
                data.levelIndex = levelIndex;
                data.isCompleted = isCompleted;
                data.starsEarned = starsEarned;
                levelProgress.Add(data);
            }
            else
            {
                data.isCompleted = isCompleted;
                data.starsEarned = starsEarned;
            }
        }
    }

    [System.Serializable]
    public class LevelProgressData
    {
        public int levelIndex;
        public bool isCompleted = false;
        public int starsEarned = 0;
    }
}