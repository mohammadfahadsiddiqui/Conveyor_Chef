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

        [Header("Per Country (CountryCatalog order)")]
        public Sprite[] landmarks = new Sprite[CountryCatalog.CountryCount];
        public Sprite[] decors = new Sprite[CountryCatalog.CountryCount];
        [Tooltip("Wide decor sits behind the country name; square decor is placed on both sides of it.")]
        public bool[] wideDecor = new bool[CountryCatalog.CountryCount];

        public Sprite GetLandmark(int country) => Get(landmarks, country);
        public Sprite GetDecor(int country) => Get(decors, country);
        public bool IsDecorWide(int country) => wideDecor != null && country >= 0 && country < wideDecor.Length && wideDecor[country];

        private static Sprite Get(Sprite[] sprites, int index)
        {
            return sprites != null && index >= 0 && index < sprites.Length ? sprites[index] : null;
        }
    }
}
