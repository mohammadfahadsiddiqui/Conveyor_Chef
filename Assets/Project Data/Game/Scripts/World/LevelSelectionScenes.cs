using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// A country can have its own Level Selection scene, Scenes/LevelSelection_&lt;Country&gt;.unity
    /// (name without spaces, e.g. LevelSelection_SouthKorea), added to Build Settings. Its art is
    /// set in the Hierarchy like India's. Countries without one use the shared LevelSelection scene.
    /// </summary>
    public static class LevelSelectionScenes
    {
        public const string SharedScene = "LevelSelection";
        private const string SelectedCountryKey = "CC_CountryMap_SelectedCountry";

        public static string SceneName(int country) =>
            SharedScene + "_" + WorldCatalog.GetCountryName(country).Replace(" ", string.Empty);

        /// <summary>The Level Selection scene for the country picked last (Country Map, Story, ...).</summary>
        public static string ForSelectedCountry()
        {
            int country = PlayerPrefs.GetInt(SelectedCountryKey, 2);
            if (!WorldCatalog.IsValidCountry(country))
                return SharedScene;

            string scene = SceneName(country);
            return Application.CanStreamedLevelBeLoaded(scene) ? scene : SharedScene;
        }
    }
}
