using UnityEngine;
using UnityEngine.SceneManagement;

namespace Watermelon
{
    /// <summary>
    /// One continent's Country Map inside CountryMap.unity. The scene holds one of these per
    /// continent under the Canvas, each a full, editable copy of the layout with its own controller.
    ///
    /// When the scene loads, only the continent chosen on the World Map is kept: it is switched on
    /// and every other copy is removed before it ever starts, so their pictures are never loaded
    /// (continent pictures load on demand, see <see cref="LevelSelectionLazySprite"/>).
    /// In the editor, selecting a continent in the Hierarchy shows it on the Canvas, and pressing
    /// Play with the scene open plays the continent that is shown.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-10000)]
    public sealed class CountryMapContinent : MonoBehaviour
    {
        private const string SelectedContinentKey = "CC_WorldMap_SelectedContinent";

        [Tooltip("Continent id from WorldCatalog, e.g. \"asia\", \"north_america\".")]
        [SerializeField] private string continentId = "";

        private static int resolvedScene = -1;
        private static CountryMapContinent chosen;
        private static bool anySceneLoaded;

        public string ContinentId => continentId;

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

        /// <summary>Keeps the chosen continent's copy and removes the others; true for the kept one.</summary>
        public static bool Resolve(CountryMapContinent asking)
        {
            Scene scene = asking.gameObject.scene;
            if (resolvedScene == scene.handle && chosen != null)
                return asking == chosen;

            CountryMapContinent[] all = FindObjectsByType<CountryMapContinent>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            WorldCatalog.Continent wanted = WorldCatalog.GetContinent(PlayerPrefs.GetInt(SelectedContinentKey, 0));
            string wantedId = wanted != null ? wanted.Id : "asia";

#if UNITY_EDITOR
            // Play pressed with this scene open: play the continent shown in the editor.
            if (!anySceneLoaded)
            {
                CountryMapContinent shown = null;
                int shownCount = 0;
                foreach (CountryMapContinent continent in all)
                {
                    if (continent.gameObject.scene == scene && continent.gameObject.activeSelf)
                    {
                        shown = continent;
                        shownCount++;
                    }
                }

                int index = shownCount == 1 ? IndexOf(shown.continentId) : -1;
                if (index >= 0)
                {
                    wantedId = shown.continentId;
                    PlayerPrefs.SetInt(SelectedContinentKey, index);
                }
            }
#endif

            CountryMapContinent match = null, asia = null, first = null;
            foreach (CountryMapContinent continent in all)
            {
                if (continent.gameObject.scene != scene)
                    continue;
                first ??= continent;
                if (Same(continent.continentId, wantedId))
                    match = continent;
                if (Same(continent.continentId, "asia"))
                    asia = continent;
            }

            chosen = match != null ? match : asia != null ? asia : first;
            resolvedScene = scene.handle;

            foreach (CountryMapContinent continent in all)
            {
                if (continent == chosen || continent.gameObject.scene != scene)
                    continue;

                continent.gameObject.SetActive(false);   // never starts
                Destroy(continent.gameObject);
            }

            if (chosen != null && !chosen.gameObject.activeSelf)
                chosen.gameObject.SetActive(true);

            return asking == chosen;
        }

        public static int IndexOf(string id)
        {
            for (int i = 0; i < WorldCatalog.ContinentCount; i++)
            {
                if (Same(WorldCatalog.Continents[i].Id, id))
                    return i;
            }
            return -1;
        }

        private static bool Same(string a, string b) =>
            string.Equals(a?.Trim(), b?.Trim(), System.StringComparison.OrdinalIgnoreCase);
    }
}
