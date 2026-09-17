using System;
using System.Collections.Generic;

namespace Watermelon.BusStop
{
    [Serializable]
    public class LevelProgressSave : ISaveObject
    {
        public int selectedLevel;
        public Dictionary<int, LevelProgress> levelProgressData = new Dictionary<int, LevelProgress>();

        public void Flush() { }
    }

    [Serializable]
    public class LevelProgress
    {
        public bool isCompleted;
        public int starsEarned;
    }
}