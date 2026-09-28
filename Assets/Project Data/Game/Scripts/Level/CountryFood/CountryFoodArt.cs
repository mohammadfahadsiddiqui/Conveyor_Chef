using UnityEngine;

namespace Watermelon.BusStop
{
    /// <summary>
    /// Dish images per country (Images/CountryFood), in Resources as "CountryFoodArt".
    /// <see cref="dishes"/> is country-major, colour-minor: index = country * 7 + colour,
    /// countries in CountryCatalog order (China, Japan, India, South Korea, Thailand),
    /// colours in LevelElement.Type order (Red, Green, Pink, Blue, Yellow, Teal, Purple).
    /// </summary>
    [CreateAssetMenu(fileName = "CountryFoodArt", menuName = "Conveyor Chef/Country Food Art")]
    public sealed class CountryFoodArt : ScriptableObject
    {
        public const string ResourcePath = "CountryFoodArt";
        public const int ColoursPerCountry = 7;

        public Sprite[] dishes = new Sprite[35];

        [Header("Board look")]
        [Tooltip("Dish size relative to the tile footprint (the character tap collider).")]
        public float sizeMultiplier = 1.35f;
        [Tooltip("Tint of dishes that cannot be picked yet (the donut outline did this before).")]
        public Color blockedTint = new Color(0.72f, 0.72f, 0.72f, 1f);
        [Range(0f, 1f)] public float shadowAlpha = 0.35f;
        [Range(0f, 1f)] public float ringAlpha = 0.85f;

        public Sprite GetDish(int country, int colourIndex)
        {
            int index = country * ColoursPerCountry + colourIndex;
            return dishes != null && country >= 0 && colourIndex >= 0 && colourIndex < ColoursPerCountry &&
                   index < dishes.Length
                ? dishes[index]
                : null;
        }
    }
}
