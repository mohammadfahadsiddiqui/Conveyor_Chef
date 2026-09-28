using TMPro;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Art for the Country Completed panel (Images/CountryComplete). Lives in a Resources
    /// folder as "CountryCompleteArt". Per-country arrays follow <see cref="CountryCatalog"/>
    /// order: China, Japan, India, South Korea, Thailand.
    /// </summary>
    [CreateAssetMenu(fileName = "CountryCompleteArt", menuName = "Conveyor Chef/Country Complete Art")]
    public sealed class CountryCompleteArt : ScriptableObject
    {
        public const string ResourcePath = "CountryCompleteArt";

        [Header("Panel")]
        public Sprite frame;
        public Sprite sparkle;
        public Sprite crown;
        public Sprite title;
        public Sprite ribbon;
        public Sprite starSlot;
        public Sprite star;
        public Sprite check;
        public Sprite flowersLeft;
        public Sprite flowersRight;
        public Sprite nextCard;
        public Sprite nextLabel;
        public Sprite continueButton;
        public Sprite homeButton;
        public TMP_FontAsset font;

        [Header("Asian countries (stand-ins for the rest until they have their own art)")]
        public Sprite[] landmarks = new Sprite[WorldCatalog.CountriesPerContinent];
        public Sprite[] decors = new Sprite[WorldCatalog.CountriesPerContinent];
        [Tooltip("Wide decor sits behind the country name; square decor is placed on both sides of it.")]
        public bool[] wideDecor = new bool[WorldCatalog.CountriesPerContinent];

        // Each country's own art (Resources/World/<continent>/<country>/landmark|decor) wins;
        // countries without it use the Asian country in the same position.
        public Sprite GetLandmark(int country) => Pick(WorldArt.ForCountry(country, WorldArt.Landmark), Get(landmarks, WorldCatalog.StandInCountry(country)));
        public Sprite GetDecor(int country) => Pick(WorldArt.ForCountry(country, WorldArt.Decor), Get(decors, WorldCatalog.StandInCountry(country)));

        public bool IsDecorWide(int country)
        {
            int index = WorldArt.HasOwn(country, WorldArt.Decor) ? -1 : WorldCatalog.StandInCountry(country);
            return wideDecor != null && index >= 0 && index < wideDecor.Length && wideDecor[index];
        }

        private static Sprite Pick(Sprite preferred, Sprite fallback) => preferred != null ? preferred : fallback;

        private static Sprite Get(Sprite[] sprites, int index)
        {
            return sprites != null && index >= 0 && index < sprites.Length ? sprites[index] : null;
        }
    }
}
