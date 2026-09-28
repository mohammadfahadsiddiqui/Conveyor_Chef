namespace Watermelon.BusStop
{
    /// <summary>
    /// Display names of the 35 country dishes, in the same order as CountryFoodArt.dishes
    /// (country-major: China, Japan, India, South Korea, Thailand; colour-minor: Red, Green,
    /// Pink, Blue, Yellow, Teal, Purple), plus a short line about each country's kitchen.
    /// </summary>
    public static class DishCatalog
    {
        public static readonly string[] Names =
        {
            "Tanghulu", "Jiaozi Dumplings", "Red Bean Bao", "Chow Mein", "Spring Rolls", "Siu Mai", "Taro Bun",
            "Tuna Nigiri", "Matcha Dango", "Sakura Mochi", "Ramen", "Shrimp Tempura", "Melon Kakigori", "Taiyaki",
            "Tandoori Chicken", "Pani Puri", "Falooda", "Chicken Biryani", "Samosa", "Masala Chai", "Gulab Jamun",
            "Tteokbokki", "Kimbap", "Strawberry Bingsu", "Bibimbap", "Honey Fried Chicken", "Japchae", "Goguma Bread",
            "Tom Yum", "Green Curry", "Pink Milk", "Blue Sticky Rice", "Mango Sticky Rice", "Pad Thai", "Butterfly Pea Soda",
        };

        public static readonly string[] CountryStories =
        {
            "Lantern-lit night markets: sweet tanghulu, steaming baskets of dumplings and golden spring rolls.",
            "Quiet sushi counters and cherry-blossom sweets: nigiri, ramen, mochi and crispy tempura.",
            "Busy street stalls full of spice: tandoori, pani puri, biryani and a cup of masala chai.",
            "Seoul's food alleys: spicy tteokbokki, kimbap rolls, bibimbap and crunchy fried chicken.",
            "Floating markets and hot woks: tom yum, green curry, pad thai and mango sticky rice.",
        };

        public static string GetName(int dish) => dish >= 0 && dish < Names.Length ? Names[dish] : "???";
    }
}
