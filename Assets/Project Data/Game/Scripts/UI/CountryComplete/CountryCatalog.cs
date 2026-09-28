using Watermelon.BusStop;

namespace Watermelon
{
    /// <summary>
    /// Country order and level ranges, matching LevelSelectionController and
    /// CountryMapSceneController: country i owns levels i*3 .. i*3+2.
    /// </summary>
    public static class CountryCatalog
    {
        public const int CountryCount = 5;
        public const int LevelsPerCountry = 3;

        public static readonly string[] Names = { "China", "Japan", "India", "South Korea", "Thailand" };

        public static int GetCountryOfLevel(int levelIndex)
        {
            return levelIndex < 0 ? -1 : levelIndex / LevelsPerCountry;
        }

        public static bool IsValid(int country) => country >= 0 && country < CountryCount;

        public static string GetName(int country) => IsValid(country) ? Names[country] : string.Empty;

        /// <summary>The next country, or -1 when this was the last one.</summary>
        public static int GetNext(int country) => IsValid(country + 1) ? country + 1 : -1;

        public static bool IsLastLevelOfCountry(int levelIndex)
        {
            return levelIndex >= 0 && levelIndex % LevelsPerCountry == LevelsPerCountry - 1;
        }

        public static bool IsCountryComplete(int country, LevelSave save)
        {
            if (!IsValid(country) || save == null)
                return false;

            int start = country * LevelsPerCountry;
            for (int i = 0; i < LevelsPerCountry; i++)
            {
                LevelProgressData progress = save.GetLevelProgress(start + i);
                if (progress == null || !progress.isCompleted)
                    return false;
            }

            return true;
        }
    }
}
