using UnityEngine;
using UnityEngine.SceneManagement;

namespace Watermelon
{
    /// <summary>
    /// One country's Level Selection inside LevelSelection.unity. The scene holds one of these per
    /// country under the Canvas, each a full, editable copy of the layout with its own controller.
    ///
    /// When the scene loads, only the selected country's copy is kept: it is switched on and every
    /// other copy is removed before it ever starts, so their pictures are never loaded (country
    /// pictures load on demand, see <see cref="LevelSelectionLazySprite"/>).
    /// In the editor, selecting a country in the Hierarchy shows that country on the Canvas, and
    /// pressing Play with the scene open plays the country that is shown.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-10000)]
    public sealed class LevelSelectionCountry : MonoBehaviour
    {
        private const string SelectedCountryKey = "CC_CountryMap_SelectedCountry";
        private const string SelectedCountryLevelStartKey = "CC_CountryMap_SelectedLevelStart";

        [Tooltip("Country id from WorldCatalog, e.g. \"india\", \"south_korea\".")]
        [SerializeField] private string countryId = "";

        private static int resolvedScene = -1;
        private static LevelSelectionCountry chosen;
        private static bool anySceneLoaded;

        public string CountryId => countryId;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            resolvedScene = -1;
            chosen = null;
            anySceneLoaded = false;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => anySceneLoaded = true;

        private void Awake()
        {
            if (Application.isPlaying)
                Resolve(this);
        }

        /// <summary>Keeps the selected country's copy and removes the others; true for the kept one.</summary>
        public static bool Resolve(LevelSelectionCountry asking)
        {
            Scene scene = asking.gameObject.scene;
            if (resolvedScene == scene.handle && chosen != null)
                return asking == chosen;

            LevelSelectionCountry[] all = FindObjectsByType<LevelSelectionCountry>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            int selected = PlayerPrefs.GetInt(SelectedCountryKey, 2);
            WorldCatalog.Country wanted = WorldCatalog.GetCountry(selected);
            string wantedId = wanted != null ? wanted.Id : "india";

#if UNITY_EDITOR
            // Play pressed with this scene open: play the country shown in the editor.
            if (!anySceneLoaded)
            {
                LevelSelectionCountry shown = null;
                int shownCount = 0;
                foreach (LevelSelectionCountry country in all)
                {
                    if (country.gameObject.scene == scene && country.gameObject.activeSelf)
                    {
                        shown = country;
                        shownCount++;
                    }
                }

                int index = shownCount == 1 ? IndexOf(shown.countryId) : -1;
                if (index >= 0)
                {
                    wantedId = shown.countryId;
                    PlayerPrefs.SetInt(SelectedCountryKey, index);
                    PlayerPrefs.SetInt(SelectedCountryLevelStartKey, WorldCatalog.FirstLevelOfCountry(index));
                }
            }
#endif

            LevelSelectionCountry match = null, india = null, first = null;
            foreach (LevelSelectionCountry country in all)
            {
                if (country.gameObject.scene != scene)
                    continue;
                first ??= country;
                if (Same(country.countryId, wantedId))
                    match = country;
                if (Same(country.countryId, "india"))
                    india = country;
            }

            chosen = match != null ? match : india != null ? india : first;
            resolvedScene = scene.handle;

            foreach (LevelSelectionCountry country in all)
            {
                if (country == chosen || country.gameObject.scene != scene)
                    continue;

                country.gameObject.SetActive(false);   // never starts
                Destroy(country.gameObject);
            }

            if (chosen != null && !chosen.gameObject.activeSelf)
                chosen.gameObject.SetActive(true);

            return asking == chosen;
        }

        public static int IndexOf(string id)
        {
            for (int i = 0; i < WorldCatalog.CountryCount; i++)
            {
                if (Same(WorldCatalog.Countries[i].Id, id))
                    return i;
            }
            return -1;
        }

        private static bool Same(string a, string b) =>
            string.Equals(a?.Trim(), b?.Trim(), System.StringComparison.OrdinalIgnoreCase);
    }
}
