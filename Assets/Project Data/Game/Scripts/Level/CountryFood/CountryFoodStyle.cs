using UnityEngine;

namespace Watermelon.BusStop
{
    /// <summary>
    /// Plate and glass-lid styles for the dishes on the board, bought and picked in the
    /// Customize panel. The first style of each is free and always owned.
    /// </summary>
    public static class CountryFoodStyle
    {
        public static readonly string[] PlateNames = { "Classic", "Gold Rim", "Porcelain" };
        public static readonly int[] PlatePrices = { 0, 500, 800 };

        public static readonly string[] LidNames = { "Clear Glass", "Frosted Glass", "Golden Glass" };
        public static readonly int[] LidPrices = { 0, 500, 800 };

        private const string PlateKey = "CC_Style_Plate";
        private const string LidKey = "CC_Style_Lid";

        public static int Plate
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(PlateKey, 0), 0, PlateNames.Length - 1);
            set { PlayerPrefs.SetInt(PlateKey, value); PlayerPrefs.Save(); }
        }

        public static int Lid
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(LidKey, 0), 0, LidNames.Length - 1);
            set { PlayerPrefs.SetInt(LidKey, value); PlayerPrefs.Save(); }
        }

        public static bool OwnsPlate(int style) => style == 0 || PlayerPrefs.GetInt(PlateKey + "_Owned_" + style, 0) == 1;

        public static bool OwnsLid(int style) => style == 0 || PlayerPrefs.GetInt(LidKey + "_Owned_" + style, 0) == 1;

        public static void UnlockPlate(int style)
        {
            PlayerPrefs.SetInt(PlateKey + "_Owned_" + style, 1);
            PlayerPrefs.Save();
        }

        public static void UnlockLid(int style)
        {
            PlayerPrefs.SetInt(LidKey + "_Owned_" + style, 1);
            PlayerPrefs.Save();
        }
    }
}
