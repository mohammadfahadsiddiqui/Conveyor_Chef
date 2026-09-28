using Watermelon.BusStop;

namespace Watermelon
{
    /// <summary>
    /// Country order and level ranges for the whole world (see <see cref="WorldCatalog"/>):
    /// country i owns levels i*3 .. i*3+2; countries 0-4 are Asia, 5-9 North America, etc.
    /// </summary>
    public static class CountryCatalog
    {
        public const int CountryCount = WorldCatalog.CountryCount;
        public const int LevelsPerCountry = WorldCatalog.LevelsPerCountry;

        public static int GetCountryOfLevel(int levelIndex) => WorldCatalog.CountryOfLevel(levelIndex);

        public static bool IsValid(int country) => WorldCatalog.IsValidCountry(country);

        public static string GetName(int country) => WorldCatalog.GetCountryName(country);

        /// <summary>The next country, or -1 when this was the last one.</summary>
        public static int GetNext(int country) => IsValid(country + 1) ? country + 1 : -1;

        /// <summary>True when the next country is on another continent (or there is none).</summary>
        public static bool IsLastOfContinent(int country) =>
            WorldCatalog.ContinentOfCountry(country) != WorldCatalog.ContinentOfCountry(GetNext(country));

        public static bool IsLastLevelOfCountry(int levelIndex)
        {
            return levelIndex >= 0 && levelIndex % LevelsPerCountry == LevelsPerCountry - 1;
        }

        public static bool IsCountryComplete(int country, LevelSave save) => IsValid(country) && WorldCatalog.IsCountryComplete(country);
    }
}
