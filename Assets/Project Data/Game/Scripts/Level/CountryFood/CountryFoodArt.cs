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

        [Tooltip("Per dish, same order as Dishes: how far above the image's bottom edge the food starts " +
                 "(0..1 of the image height). Used to stand every dish in the middle of its plate.")]
        public float[] dishBottoms = new float[35];

        [Header("Board look")]
        [Tooltip("Dish size relative to the tile footprint (the character tap collider).")]
        public float sizeMultiplier = 1.35f;
        [Tooltip("Tint of dishes that cannot be picked yet (the donut outline did this before).")]
        public Color blockedTint = new Color(0.86f, 0.86f, 0.86f, 1f);
        [Range(0f, 1f)] public float shadowAlpha = 0.35f;
        [Tooltip("Stand every dish on a solid plate in its tray colour, so players can see which dish goes where.")]
        public bool showColourPlate = true;
        [Tooltip("Plate width relative to the dish card.")]
        [Range(0.6f, 1.2f)] public float plateWidth = 0.96f;

        /// <summary>Empty space under the food in this dish image, as a fraction of its height.</summary>
        public float GetDishBottom(Sprite dish)
        {
            if (dish != null && dishes != null && dishBottoms != null)
            {
                int index = System.Array.IndexOf(dishes, dish);
                if (index >= 0 && index < dishBottoms.Length)
                    return dishBottoms[index];
            }

            return DefaultDishBottom;
        }

        public const float DefaultDishBottom = 0.14f;

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
