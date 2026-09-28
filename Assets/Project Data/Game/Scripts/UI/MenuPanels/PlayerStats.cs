using System;
using UnityEngine;
using Watermelon.BusStop;

namespace Watermelon
{
    /// <summary>
    /// Player statistics for the menu panels (challenges, achievements, collection and
    /// leaderboard). Totals and today's counters are kept in PlayerPrefs; level progress and
    /// stars come from the level save. Gameplay reports through <see cref="RecordDishServed"/>,
    /// <see cref="RecordLevelWon"/> and power-up use.
    /// </summary>
    public static class PlayerStats
    {
        public const int TotalLevels = CountryCatalog.CountryCount * CountryCatalog.LevelsPerCountry;
        public const int TotalStars = TotalLevels * 3;
        public const int TotalDishes = CountryCatalog.CountryCount * CountryFoodArt.ColoursPerCountry;

        private const string DishesServedKey = "CC_Stats_DishesServed";
        private const string DishServedKey = "CC_Stats_Dish_";
        private const string PowerUpsUsedKey = "CC_Stats_PowerUpsUsed";
        private const string LevelsWonKey = "CC_Stats_LevelsWon";

        private const string DailyDateKey = "CC_Daily_Date";
        private const string DailyPrefix = "CC_Daily_";

        public enum Daily { Levels, Stars, Dishes, PowerUps }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Subscribe()
        {
            PUController.OnPowerUpUsed -= OnPowerUpUsed;
            PUController.OnPowerUpUsed += OnPowerUpUsed;
        }

        #region Recording

        public static void RecordDishServed(LevelElement.Type type)
        {
            Add(DishesServedKey, 1);
            AddDaily(Daily.Dishes, 1);

            int dish = GetDishIndex(CountryFood.CurrentCountry, CountryFood.GetColourIndex(type));
            if (dish >= 0)
                Add(DishServedKey + dish, 1);
        }

        public static void RecordLevelWon(int stars)
        {
            Add(LevelsWonKey, 1);
            AddDaily(Daily.Levels, 1);
            AddDaily(Daily.Stars, Mathf.Max(0, stars));
            PlayerPrefs.Save();
        }

        private static void OnPowerUpUsed(PUType type)
        {
            Add(PowerUpsUsedKey, 1);
            AddDaily(Daily.PowerUps, 1);
        }

        #endregion

        #region Totals

        public static int DishesServed => PlayerPrefs.GetInt(DishesServedKey, 0);
        public static int PowerUpsUsed => PlayerPrefs.GetInt(PowerUpsUsedKey, 0);

        public static int GetDishIndex(int country, int colour)
        {
            return country >= 0 && country < CountryCatalog.CountryCount && colour >= 0 && colour < CountryFoodArt.ColoursPerCountry
                ? country * CountryFoodArt.ColoursPerCountry + colour
                : -1;
        }

        public static int GetDishServed(int dish) => PlayerPrefs.GetInt(DishServedKey + dish, 0);

        /// <summary>
        /// A dish is discovered once it has been served, or once any level of its country has
        /// been completed (so progress made before the collection existed still counts).
        /// </summary>
        public static bool IsDishDiscovered(int dish)
        {
            if (GetDishServed(dish) > 0)
                return true;

            int country = dish / CountryFoodArt.ColoursPerCountry;
            return GetCountryLevelsCompleted(country) > 0;
        }

        public static int DishesDiscovered
        {
            get
            {
                int count = 0;
                for (int i = 0; i < TotalDishes; i++)
                {
                    if (IsDishDiscovered(i))
                        count++;
                }
                return count;
            }
        }

        public static void GetProgress(out int completed, out int stars, out int bestLevel)
        {
            completed = 0;
            stars = 0;
            bestLevel = 0;

            LevelSave save = GetLevelSave();
            if (save == null || save.levelProgress == null)
                return;

            for (int i = 0; i < save.levelProgress.Count; i++)
            {
                LevelProgressData progress = save.levelProgress[i];
                if (progress == null || !progress.isCompleted)
                    continue;

                completed++;
                stars += Mathf.Clamp(progress.starsEarned, 0, 3);
                bestLevel = Mathf.Max(bestLevel, progress.levelIndex + 1);
            }
        }

        public static int LevelsCompleted { get { GetProgress(out int c, out _, out _); return c; } }
        public static int StarsEarned { get { GetProgress(out _, out int s, out _); return s; } }

        public static int GetCountryLevelsCompleted(int country)
        {
            LevelSave save = GetLevelSave();
            if (save == null || save.levelProgress == null)
                return 0;

            int first = country * CountryCatalog.LevelsPerCountry;
            int count = 0;
            for (int i = 0; i < save.levelProgress.Count; i++)
            {
                LevelProgressData progress = save.levelProgress[i];
                if (progress != null && progress.isCompleted &&
                    progress.levelIndex >= first && progress.levelIndex < first + CountryCatalog.LevelsPerCountry)
                    count++;
            }
            return count;
        }

        public static int GetCountryStars(int country)
        {
            LevelSave save = GetLevelSave();
            if (save == null || save.levelProgress == null)
                return 0;

            int first = country * CountryCatalog.LevelsPerCountry;
            int stars = 0;
            for (int i = 0; i < save.levelProgress.Count; i++)
            {
                LevelProgressData progress = save.levelProgress[i];
                if (progress != null && progress.isCompleted &&
                    progress.levelIndex >= first && progress.levelIndex < first + CountryCatalog.LevelsPerCountry)
                    stars += Mathf.Clamp(progress.starsEarned, 0, 3);
            }
            return stars;
        }

        public static bool IsCountryComplete(int country) => GetCountryLevelsCompleted(country) >= CountryCatalog.LevelsPerCountry;

        /// <summary>The first country is always open; each next one opens when the previous is complete.</summary>
        public static bool IsCountryUnlocked(int country) => country <= 0 || IsCountryComplete(country - 1);

        public static int CountriesCompleted
        {
            get
            {
                int count = 0;
                for (int i = 0; i < CountryCatalog.CountryCount; i++)
                {
                    if (IsCountryComplete(i))
                        count++;
                }
                return count;
            }
        }

        /// <summary>Leaderboard score: stars, completed levels and dishes served.</summary>
        public static int ChefScore
        {
            get
            {
                GetProgress(out int completed, out int stars, out _);
                return stars * 100 + completed * 50 + DishesServed * 2;
            }
        }

        private static LevelSave GetLevelSave()
        {
            try
            {
                return SaveController.IsSaveLoaded ? SaveController.GetSaveObject<LevelSave>("level") : null;
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region Daily

        public static string Today => DateTime.Now.ToString("yyyyMMdd");

        public static TimeSpan TimeUntilNextDay => DateTime.Today.AddDays(1) - DateTime.Now;

        public static int GetDaily(Daily counter)
        {
            RollDay();
            return PlayerPrefs.GetInt(DailyPrefix + counter, 0);
        }

        public static bool IsDailyFlagSet(string flag)
        {
            RollDay();
            return PlayerPrefs.GetInt(DailyPrefix + "Flag_" + flag, 0) == 1;
        }

        public static void SetDailyFlag(string flag)
        {
            RollDay();
            PlayerPrefs.SetInt(DailyPrefix + "Flag_" + flag, 1);
            RememberDailyFlag(flag);
            PlayerPrefs.Save();
        }

        private static void AddDaily(Daily counter, int amount)
        {
            RollDay();
            Add(DailyPrefix + counter, amount);
        }

        // A new day clears the daily counters and flags.
        private static void RollDay()
        {
            string today = Today;
            if (PlayerPrefs.GetString(DailyDateKey, string.Empty) == today)
                return;

            foreach (Daily counter in Enum.GetValues(typeof(Daily)))
                PlayerPrefs.DeleteKey(DailyPrefix + counter);

            string flags = PlayerPrefs.GetString(DailyPrefix + "Flags", string.Empty);
            foreach (string flag in flags.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
                PlayerPrefs.DeleteKey(DailyPrefix + "Flag_" + flag);
            PlayerPrefs.DeleteKey(DailyPrefix + "Flags");

            PlayerPrefs.SetString(DailyDateKey, today);
            PlayerPrefs.Save();
        }

        private static void RememberDailyFlag(string flag)
        {
            string flags = PlayerPrefs.GetString(DailyPrefix + "Flags", string.Empty);
            if (!("|" + flags + "|").Contains("|" + flag + "|"))
                PlayerPrefs.SetString(DailyPrefix + "Flags", flags.Length == 0 ? flag : flags + "|" + flag);
        }

        #endregion

        #region Flags

        public static bool GetFlag(string key) => PlayerPrefs.GetInt("CC_Flag_" + key, 0) == 1;

        public static void SetFlag(string key)
        {
            PlayerPrefs.SetInt("CC_Flag_" + key, 1);
            PlayerPrefs.Save();
        }

        #endregion

        private static void Add(string key, int amount)
        {
            PlayerPrefs.SetInt(key, PlayerPrefs.GetInt(key, 0) + amount);
        }
    }
}
