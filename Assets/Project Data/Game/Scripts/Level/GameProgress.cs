using UnityEngine;

namespace Watermelon.BusStop
{
    /// <summary>
    /// One answer to "is this level done?" for every screen. Level Selection and the Country
    /// Map also counted levels passed in the old linear progression (DisplayLevelNumber),
    /// while the World Map only read the per-level flags, so a continent could look finished
    /// on one screen and stay locked on the next. Old progress is now written into the
    /// per-level flags once, and every check uses the same rule.
    /// </summary>
    public static class GameProgress
    {
        private const string MigratedKey = "CC_Progress_LegacyMigrated";

        public static bool IsLevelCompleted(int levelIndex)
        {
            LevelSave save = GetSave();
            if (save == null || levelIndex < 0)
                return false;

            LevelProgressData data = save.GetLevelProgress(levelIndex);
            if (data != null && data.isCompleted)
                return true;

            // Levels passed in the old linear progression count as done.
            return levelIndex < Mathf.Max(0, save.DisplayLevelNumber);
        }

        public static bool IsLevelUnlocked(int levelIndex) => levelIndex <= 0 || IsLevelCompleted(levelIndex - 1);

        /// <summary>Writes old linear progress into the per-level flags (once per save).</summary>
        public static void MigrateLegacyProgress()
        {
            if (PlayerPrefs.GetInt(MigratedKey, 0) == 1)
                return;

            LevelSave save = GetSave();
            if (save == null)
                return;

            int passed = Mathf.Max(0, save.DisplayLevelNumber);
            for (int level = 0; level < passed; level++)
            {
                LevelProgressData data = save.GetLevelProgress(level);
                if (data == null || !data.isCompleted)
                    save.SetLevelProgress(level, true, Mathf.Max(3, data != null ? data.starsEarned : 0));
            }

            if (passed > 0)
            {
                SaveController.MarkAsSaveIsRequired();
                SaveController.Save(true);
                Debug.Log("[Progress] Recorded " + passed + " levels from the old progression.");
            }

            PlayerPrefs.SetInt(MigratedKey, 1);
            PlayerPrefs.Save();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void MigrateOnStart()
        {
            try { MigrateLegacyProgress(); }
            catch (System.Exception ex) { Debug.LogWarning("[Progress] Migration skipped: " + ex.Message); }
        }

        private static LevelSave GetSave()
        {
            try
            {
                if (!SaveController.IsSaveLoaded)
                    SaveController.Initialise(useAutoSave: false);
                return SaveController.GetSaveObject<LevelSave>("level");
            }
            catch
            {
                return null;
            }
        }
    }
}
