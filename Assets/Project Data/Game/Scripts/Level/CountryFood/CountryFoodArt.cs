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

        [Tooltip("Per dish, same order as Dishes: the area the food covers in its image, as fractions of " +
                 "the image (x = left, y = bottom, z = right, w = top). Every dish is sized and centred from " +
                 "this, so the empty margins of the images do not matter.")]
        public Vector4[] dishRects = new Vector4[35];

        [Header("Board look")]
        [Tooltip("Dish size relative to the tile footprint (the character tap collider).")]
        public float sizeMultiplier = 1.35f;
        [Tooltip("Cover dishes that cannot be picked yet with a clear glass cloche that lifts off when they become pickable.")]
        public bool showCloche = true;
        [Tooltip("Tint of dishes (and plates) under the cloche.")]
        public Color blockedTint = new Color(0.95f, 0.95f, 0.95f, 1f);
        [Range(0f, 1f)] public float shadowAlpha = 0.35f;
        [Tooltip("Stand every dish on a solid plate in its tray colour, so players can see which dish goes where.")]
        public bool showColourPlate = true;
        [Tooltip("Plate width relative to the dish card.")]
        [Range(0.6f, 1.2f)] public float plateWidth = 0.96f;
        [Tooltip("Width of the food on its plate, relative to the dish card.")]
        [Range(0.5f, 1.1f)] public float foodWidth = 0.8f;
        [Tooltip("Tallest the food may be (tall drinks), relative to the dish card.")]
        [Range(0.5f, 1.1f)] public float foodMaxHeight = 0.82f;

        /// <summary>Area the food covers in this dish image (see <see cref="dishRects"/>).</summary>
        public Vector4 GetDishRect(Sprite dish)
        {
            if (dish != null && dishes != null && dishRects != null)
            {
                int index = System.Array.IndexOf(dishes, dish);
                if (index >= 0 && index < dishRects.Length && dishRects[index].z > dishRects[index].x)
                    return dishRects[index];
            }

            return DefaultDishRect;
        }

        public static readonly Vector4 DefaultDishRect = new Vector4(0.07f, 0.14f, 0.93f, 0.86f);

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
