using UnityEngine;

namespace Watermelon.BusStop
{
    /// <summary>
    /// Picks the dish shown for each food colour, based on the country of the level being
    /// played (country i owns levels i*3 .. i*3+2). The colours and all gameplay stay the
    /// same; only the picture changes. Levels past the last country cycle through the
    /// countries again.
    /// </summary>
    public static class CountryFood
    {
        private static CountryFoodArt art;
        private static bool artLoaded;

        public static CountryFoodArt Art
        {
            get
            {
                if (!artLoaded)
                {
                    art = Resources.Load<CountryFoodArt>(CountryFoodArt.ResourcePath);
                    artLoaded = true;
                }

                return art;
            }
        }

        /// <summary>Country of the level being played, or -1 when unknown.</summary>
        public static int CurrentCountry
        {
            get
            {
                LevelSave save = SaveController.GetSaveObject<LevelSave>("level");
                if (save == null)
                    return -1;

                int levelIndex = save.isPlayingFromLevelSelection ? save.selectedLevelIndex : save.RealLevelNumber;
                if (levelIndex < 0)
                    return -1;

                return (levelIndex / Watermelon.CountryCatalog.LevelsPerCountry) % Watermelon.CountryCatalog.CountryCount;
            }
        }

        public static int GetColourIndex(LevelElement.Type type)
        {
            int index = (int)type - (int)LevelElement.Type.Block_Red;
            return index >= 0 && index < CountryFoodArt.ColoursPerCountry ? index : -1;
        }

        /// <summary>Dish for this colour in the current country, or null to keep the original look.</summary>
        public static Sprite GetDish(LevelElement.Type type)
        {
            CountryFoodArt foodArt = Art;
            int colour = GetColourIndex(type);
            int country = CurrentCountry;
            return foodArt != null && colour >= 0 && country >= 0 ? foodArt.GetDish(country, colour) : null;
        }

        /// <summary>The game's colour for a food type, used for the ring under each dish.</summary>
        public static Color GetTypeColour(LevelElement.Type type)
        {
            switch (type)
            {
                case LevelElement.Type.Block_Red: return new Color32(235, 64, 52, 255);
                case LevelElement.Type.Block_Green: return new Color32(76, 196, 72, 255);
                case LevelElement.Type.Block_Pink: return new Color32(246, 104, 178, 255);
                case LevelElement.Type.Block_Blue: return new Color32(58, 110, 232, 255);
                case LevelElement.Type.Block_Yellow: return new Color32(250, 204, 40, 255);
                case LevelElement.Type.Block_Teal: return new Color32(32, 196, 190, 255);
                case LevelElement.Type.Block_Purple: return new Color32(150, 80, 220, 255);
                default: return Color.white;
            }
        }
    }
}
