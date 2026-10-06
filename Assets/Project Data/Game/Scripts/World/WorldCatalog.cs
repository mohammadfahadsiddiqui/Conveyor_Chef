using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// The whole culinary world: 7 continents x 5 countries (Antarctica: 5 regions) x 3 levels.
    ///
    /// Countries have a global index (continent * 5 + position), and country c owns levels
    /// c*3 .. c*3+2, so Asia keeps levels 1-15 exactly as before, North America is 16-30,
    /// and so on up to 105. Continent order matches the World Map chapters.
    /// Art for each continent and country is looked up by name through <see cref="WorldArt"/>.
    /// </summary>
    public static class WorldCatalog
    {
        public const int ContinentCount = 7;
        public const int CountriesPerContinent = 5;
        public const int LevelsPerCountry = 3;
        public const int CountryCount = ContinentCount * CountriesPerContinent;
        public const int LevelsPerContinent = CountriesPerContinent * LevelsPerCountry;
        public const int TotalLevels = CountryCount * LevelsPerCountry;

        /// <summary>Dish colour order, same as LevelElement.Type Block_Red .. Block_Purple.</summary>
        public static readonly string[] DishColours = { "red", "green", "pink", "blue", "yellow", "teal", "purple" };

        public sealed class Continent
        {
            public readonly int Index;
            public readonly string Id;
            public readonly string Name;
            public readonly string Info;

            public Continent(int index, string id, string name, string info)
            {
                Index = index;
                Id = id;
                Name = name;
                Info = info;
            }
        }

        public sealed class Country
        {
            public readonly int Index;
            public readonly int Continent;
            public readonly string Id;
            public readonly string Name;
            public readonly string Description;
            public readonly string[] Missions;
            public readonly string[] Dishes;
            public readonly string Story;

            public Country(int index, string id, string name, string description, string story, string[] missions, string[] dishes)
            {
                Index = index;
                Continent = index / CountriesPerContinent;
                Id = id;
                Name = name;
                Description = description;
                Story = story;
                Missions = missions;
                Dishes = dishes;
            }

            public int FirstLevel => Index * LevelsPerCountry;
            public int PositionInContinent => Index % CountriesPerContinent;
        }

        public static readonly Continent[] Continents =
        {
            new Continent(0, "asia", "Asia", "Explore amazing cuisines\nand cultures across Asia!"),
            new Continent(1, "north_america", "North America", "From BBQ pits to taco stands,\nserve North America's favourites!"),
            new Continent(2, "south_america", "South America", "Grills, markets and tropical sweets\nall across South America!"),
            new Continent(3, "europe", "Europe", "Bistros, trattorias and bakeries\nfrom every corner of Europe!"),
            new Continent(4, "africa", "Africa", "Spice markets and family feasts\nacross the African continent!"),
            new Continent(5, "oceania", "Oceania", "Island feasts and seaside kitchens\nacross Oceania!"),
            new Continent(6, "antarctica", "Antarctica", "Icy kitchens and warm meals\nat the bottom of the world!"),
        };

        public static readonly Country[] Countries =
        {
            // ---------------- Asia (levels 1-15) ----------------
            new Country(0, "china", "China",
                "Explore China's iconic cities and bold regional flavors as you master three culinary missions.",
                "Lantern-lit night markets: sweet tanghulu, steaming baskets of dumplings and golden spring rolls.",
                new[] { "Beijing Bites", "Shanghai Rush", "Sichuan Station" },
                new[] { "Tanghulu", "Jiaozi Dumplings", "Red Bean Bao", "Chow Mein", "Spring Rolls", "Siu Mai", "Taro Bun" }),
            new Country(1, "japan", "Japan",
                "Travel across Japan through fast kitchens, classic streets and unforgettable food destinations.",
                "Quiet sushi counters and cherry-blossom sweets: nigiri, ramen, mochi and crispy tempura.",
                new[] { "Tokyo Treats", "Kyoto Kitchen", "Osaka Rush" },
                new[] { "Tuna Nigiri", "Matcha Dango", "Sakura Mochi", "Ramen", "Shrimp Tempura", "Melon Kakigori", "Taiyaki" }),
            new Country(2, "india", "India",
                "Explore India's rich food culture, vibrant cities and iconic destinations as you deliver delicious dishes across the country!",
                "Busy street stalls full of spice: tandoori, pani puri, biryani and a cup of masala chai.",
                new[] { "Varanasi Ghats", "Delhi Streets", "Mumbai Docks" },
                new[] { "Tandoori Chicken", "Pani Puri", "Falooda", "Chicken Biryani", "Samosa", "Masala Chai", "Gulab Jamun" }),
            new Country(3, "south_korea", "South Korea",
                "Discover Korea's energetic food streets, coastal stops and traditional culinary culture.",
                "Seoul's food alleys: spicy tteokbokki, kimbap rolls, bibimbap and crunchy fried chicken.",
                new[] { "Seoul Street Food", "Busan Harbor", "Jeonju Kitchen" },
                new[] { "Tteokbokki", "Kimbap", "Strawberry Bingsu", "Bibimbap", "Honey Fried Chicken", "Japchae", "Goguma Bread" }),
            new Country(4, "thailand", "Thailand",
                "Serve your way through Thailand's colorful markets, northern kitchens and tropical waterfronts.",
                "Floating markets and hot woks: tom yum, green curry, pad thai and mango sticky rice.",
                new[] { "Bangkok Market", "Chiang Mai Feast", "Phuket Pier" },
                new[] { "Tom Yum", "Green Curry", "Pink Milk", "Blue Sticky Rice", "Mango Sticky Rice", "Pad Thai", "Butterfly Pea Soda" }),

            // ---------------- North America (levels 16-30) ----------------
            new Country(5, "usa", "United States",
                "Fire up the grills and diners of the USA, from big-city food trucks to southern BBQ pits.",
                "Diners and backyard grills: BBQ ribs, pancakes, mac and cheese and ice-cold milkshakes.",
                new[] { "New York Diner", "Texas BBQ", "California Coast" },
                new[] { "BBQ Ribs", "Key Lime Pie", "Strawberry Milkshake", "Blueberry Pancakes", "Mac and Cheese", "Mint Chip Sundae", "Grape Soda Float" }),
            new Country(6, "mexico", "Mexico",
                "Cook through Mexico's colorful markets, taco stands and seaside kitchens.",
                "Fiesta flavours: pozole, guacamole, conchas, elote and chilled aguas frescas.",
                new[] { "Mexico City Tacos", "Oaxaca Market", "Cancun Beach" },
                new[] { "Pozole Rojo", "Guacamole", "Pink Conchas", "Blue Corn Tacos", "Elote", "Lime Agua Fresca", "Jamaica Agua Fresca" }),
            new Country(7, "canada", "Canada",
                "Serve cozy classics from Canada's cities, forests and snowy mountain lodges.",
                "Maple country: poutine, glazed salmon, butter tarts and wild berry pies.",
                new[] { "Toronto Eats", "Montreal Bistro", "Vancouver Harbour" },
                new[] { "Maple Glazed Salmon", "Fiddlehead Salad", "Raspberry Butter Tart", "Blueberry Grunt", "Poutine", "Mint Nanaimo Bar", "Saskatoon Berry Pie" }),
            new Country(8, "cuba", "Cuba",
                "Bring rhythm to Cuba's sunny streets, cafés and beachside kitchens.",
                "Salsa and sunshine: ropa vieja, Cuban sandwiches, guava pastries and cool mocktails.",
                new[] { "Havana Cafe", "Trinidad Plaza", "Varadero Beach" },
                new[] { "Ropa Vieja", "Mojito Mocktail", "Guava Pastry", "Moros y Cristianos", "Cuban Sandwich", "Tostones", "Purple Yam Fritters" }),
            new Country(9, "jamaica", "Jamaica",
                "Spice things up across Jamaica's jerk stands, markets and reggae beach bars.",
                "Island heat: jerk chicken, ackee and saltfish, callaloo and fresh coconut water.",
                new[] { "Kingston Jerk Stand", "Montego Bay", "Blue Mountains" },
                new[] { "Jerk Chicken", "Callaloo", "Sorrel Punch", "Blue Drawers", "Ackee and Saltfish", "Coconut Water", "Otaheite Apple Jam" }),

            // ---------------- South America (levels 31-45) ----------------
            new Country(10, "brazil", "Brazil",
                "Keep up with Brazil's carnival crowds, beach kiosks and churrasco grills.",
                "Carnival flavours: churrasco, brigadeiros, pao de queijo and acai bowls.",
                new[] { "Rio Beach Kiosk", "Sao Paulo Grill", "Salvador Market" },
                new[] { "Churrasco", "Caipirinha Lime Soda", "Brigadeiro", "Coconut Cocada", "Pao de Queijo", "Guarana Soda", "Acai Bowl" }),
            new Country(11, "argentina", "Argentina",
                "Master the grills of Argentina, from Buenos Aires cafés to Patagonian lodges.",
                "Asado country: empanadas, grilled steak with chimichurri and dulce de leche.",
                new[] { "Buenos Aires Cafe", "Mendoza Vineyard", "Patagonia Lodge" },
                new[] { "Asado Steak", "Chimichurri", "Strawberry Alfajor", "Panqueque", "Empanadas", "Mate Tea", "Purple Grape Juice" }),
            new Country(12, "peru", "Peru",
                "Explore Peru's mountain markets, Andean kitchens and fresh ceviche bars.",
                "Andes to ocean: ceviche, lomo saltado, purple corn chicha and golden causa.",
                new[] { "Lima Ceviche Bar", "Cusco Market", "Machu Picchu Trail" },
                new[] { "Lomo Saltado", "Aji Verde Chicken", "Picarones", "Blue Potato Causa", "Papa a la Huancaina", "Ceviche", "Chicha Morada" }),
            new Country(13, "colombia", "Colombia",
                "Cook up Colombia's colourful towns, coffee farms and Caribbean coast.",
                "Coffee country: arepas, bandeja paisa, fresh fruit juices and sweet obleas.",
                new[] { "Bogota Bakery", "Medellin Market", "Cartagena Coast" },
                new[] { "Bandeja Paisa", "Avocado Salad", "Guava Oblea", "Ajiaco Soup", "Arepa de Choclo", "Lulada", "Mora Juice" }),
            new Country(14, "chile", "Chile",
                "Race from Chile's desert villages to its fishing ports and mountain kitchens.",
                "Coast and cordillera: empanadas de pino, sopaipillas, seafood and berry kuchen.",
                new[] { "Santiago Market", "Valparaiso Port", "Atacama Stars" },
                new[] { "Salmon Ceviche", "Pebre Salsa", "Mote con Huesillo", "Blueberry Kuchen", "Sopaipillas", "Leche Nevada", "Maqui Berry Shake" }),

            // ---------------- Europe (levels 46-60) ----------------
            new Country(15, "italy", "Italy",
                "Serve pizza, pasta and gelato across Italy's piazzas, canals and coastal towns.",
                "Trattoria classics: margherita pizza, pesto pasta, tiramisu and gelato.",
                new[] { "Rome Trattoria", "Venice Canals", "Naples Pizzeria" },
                new[] { "Margherita Pizza", "Pesto Pasta", "Strawberry Gelato", "Blueberry Panna Cotta", "Risotto alla Milanese", "Pistachio Gelato", "Tiramisu" }),
            new Country(16, "france", "France",
                "Bake and serve your way through France's bistros, patisseries and markets.",
                "Patisserie perfection: croissants, macarons, crepes and ratatouille.",
                new[] { "Paris Patisserie", "Lyon Bistro", "Provence Market" },
                new[] { "Ratatouille", "Pistachio Macaron", "Raspberry Macaron", "Blueberry Crepe", "Croissant", "Mint Eclair", "Cassis Tart" }),
            new Country(17, "spain", "Spain",
                "Keep up with Spain's tapas bars, fiestas and sunny seaside kitchens.",
                "Tapas time: paella, gazpacho, churros and golden tortilla.",
                new[] { "Madrid Tapas", "Barcelona Market", "Valencia Paella" },
                new[] { "Gazpacho", "Pimientos de Padron", "Fresas con Nata", "Crema Catalana", "Seafood Paella", "Horchata", "Grape Sangria Mocktail" }),
            new Country(18, "greece", "Greece",
                "Serve fresh Mediterranean flavours from Greece's islands and village tavernas.",
                "Taverna favourites: gyros, Greek salad, spanakopita and honey loukoumades.",
                new[] { "Athens Taverna", "Santorini Cliffs", "Crete Harbour" },
                new[] { "Tomato Gemista", "Greek Salad", "Rose Loukoumi", "Yogurt with Honey", "Spanakopita", "Tzatziki", "Moussaka" }),
            new Country(19, "germany", "Germany",
                "Serve hearty classics across Germany's markets, castles and festivals.",
                "Festival food: pretzels, bratwurst, black forest cake and apple strudel.",
                new[] { "Berlin Currywurst", "Munich Festival", "Black Forest Cafe" },
                new[] { "Currywurst", "Green Sauce Potatoes", "Strawberry Cake", "Blueberry Pancake", "Soft Pretzel", "Apple Spritzer", "Black Forest Cake" }),

            // ---------------- Africa (levels 61-75) ----------------
            new Country(20, "morocco", "Morocco",
                "Cook through Morocco's spice souks, desert camps and blue city streets.",
                "Souk spices: tagine, couscous, mint tea and honey chebakia.",
                new[] { "Marrakech Souk", "Chefchaouen Blue", "Sahara Camp" },
                new[] { "Harira Soup", "Mint Tea", "Milk Pastilla", "Chebakia", "Chicken Tagine", "Couscous", "Almond Briouat" }),
            new Country(21, "egypt", "Egypt",
                "Serve street-food favourites from Egypt's Nile cafés and pyramid markets.",
                "Nile-side classics: koshari, falafel, basbousa and hibiscus karkade.",
                new[] { "Cairo Street Food", "Giza Pyramids", "Luxor Nile Cafe" },
                new[] { "Shakshuka", "Falafel", "Basbousa", "Om Ali", "Koshari", "Mint Lemonade", "Karkade" }),
            new Country(22, "nigeria", "Nigeria",
                "Keep up with Nigeria's lively markets, suya grills and family feasts.",
                "Party favourites: jollof rice, suya skewers, puff-puff and zobo drink.",
                new[] { "Lagos Market", "Abuja Grill", "Calabar Kitchen" },
                new[] { "Jollof Rice", "Efo Riro", "Chin Chin", "Moi Moi", "Puff-Puff", "Kunu Drink", "Zobo Drink" }),
            new Country(23, "ethiopia", "Ethiopia",
                "Share Ethiopia's communal feasts, coffee ceremonies and highland kitchens.",
                "Shared plates: doro wat, injera, gomen and a slow coffee ceremony.",
                new[] { "Addis Ababa Feast", "Lalibela Kitchen", "Coffee Highlands" },
                new[] { "Doro Wat", "Gomen", "Ambasha Bread", "Injera Platter", "Shiro Wat", "Spiced Coffee", "Beetroot Salad" }),
            new Country(24, "south_africa", "South Africa",
                "Serve braai favourites and sweet treats from South Africa's cities and safaris.",
                "Braai time: boerewors, bobotie, koeksisters and malva pudding.",
                new[] { "Cape Town Braai", "Johannesburg Market", "Safari Lodge" },
                new[] { "Boerewors", "Chakalaka", "Malva Pudding", "Rusks", "Bobotie", "Rooibos Tea", "Koeksisters" }),

            // ---------------- Oceania (levels 76-90) ----------------
            new Country(25, "australia", "Australia",
                "Serve beach barbies, bakery treats and café classics across Australia.",
                "Aussie favourites: meat pies, lamingtons, pavlova and avo toast.",
                new[] { "Sydney Harbour", "Outback Barbie", "Great Barrier Reef" },
                new[] { "Aussie Meat Pie", "Avocado Toast", "Pavlova", "Lamington", "Honey Crumpets", "Mint Slice", "Passionfruit Tart" }),
            new Country(26, "new_zealand", "New Zealand",
                "Cook through New Zealand's green valleys, harbours and mountain cafés.",
                "Kiwi classics: hangi feast, kiwifruit salad, pavlova and hokey pokey ice cream.",
                new[] { "Auckland Harbour", "Rotorua Hangi", "Queenstown Cafe" },
                new[] { "Lamb Roast", "Kiwifruit Salad", "Pink Pavlova", "Blueberry Muffin", "Hokey Pokey Ice Cream", "Feijoa Smoothie", "Boysenberry Pie" }),
            new Country(27, "fiji", "Fiji",
                "Serve fresh island dishes across Fiji's beaches, reefs and village feasts.",
                "Island feast: kokoda, lovo, cassava cake and fresh coconut.",
                new[] { "Suva Market", "Coral Coast", "Lovo Village Feast" },
                new[] { "Lovo Chicken", "Palusami", "Guava Pudding", "Fish in Lolo", "Cassava Cake", "Kokoda", "Taro Pudding" }),
            new Country(28, "samoa", "Samoa",
                "Share Samoa's Sunday umu feasts, reef-side kitchens and tropical sweets.",
                "Umu Sunday: oka, palusami, pani popo and koko Samoa.",
                new[] { "Apia Market", "To Sua Trench", "Umu Sunday Feast" },
                new[] { "Sapasui", "Palusami", "Pani Popo", "Oka", "Fa'ausi", "Coconut Lime Drink", "Koko Samoa" }),
            new Country(29, "tonga", "Tonga",
                "Sail to Tonga for island feasts, reef fishing and royal celebrations.",
                "Royal feasts: lu pulu, ota ika, faikakai and watermelon juice.",
                new[] { "Nuku'alofa Market", "Ha'apai Islands", "Royal Feast" },
                new[] { "Lu Pulu", "Ota Ika", "Watermelon Juice", "Coconut Pudding", "Faikakai Topai", "Keke Doughnuts", "Kumala Pudding" }),

            // ---------------- Antarctica (levels 91-105): regions instead of countries ----------------
            new Country(30, "peninsula", "Peninsula",
                "Start your polar adventure where icebergs drift past penguin-filled bays.",
                "Penguin bays and icy channels: hot soups, fresh fish and warm cocoa for chilly explorers.",
                new[] { "Paradise Harbour", "Lemaire Channel", "Penguin Point" },
                new[] { "Hot Tomato Soup", "Pea Soup", "Pink Salmon Bites", "Blueberry Porridge", "Golden Pancakes", "Mint Hot Cocoa", "Plum Crumble" }),
            new Country(31, "weddell_sea", "Weddell Sea",
                "Sail through the sea ice to cook for scientists and emperor penguin watchers.",
                "Sea-ice camps by emperor penguin colonies: fish stew, herb dumplings and berry muffins.",
                new[] { "Emperor Colony", "Sea Ice Camp", "Iceberg Alley" },
                new[] { "Fish Stew", "Herb Dumplings", "Strawberry Jam Roll", "Icy Blue Jelly", "Corn Chowder", "Seaweed Crackers", "Berry Muffin" }),
            new Country(32, "ross_shelf", "Ross Shelf",
                "Serve warm meals on the edge of the biggest ice shelf in the world.",
                "Glacier kitchens on the great ice shelf: grilled sausages, stews and sweet pies.",
                new[] { "Ice Shelf Snack Bar", "Glacier Grill", "Whale Bay" },
                new[] { "Grilled Sausage", "Green Bean Stew", "Pink Cupcake", "Blue Ice Pop", "Cheese Toastie", "Mint Tea", "Grape Pie" }),
            new Country(33, "mcmurdo", "McMurdo",
                "Run the busiest kitchen in Antarctica beneath a smoking volcano.",
                "The busiest station kitchen beneath Mount Erebus: pizza night, burgers and hot drinks.",
                new[] { "Station Mess Hall", "Volcano View Cafe", "Supply Ship Dock" },
                new[] { "Station Pizza", "Veggie Burger", "Raspberry Waffles", "Blueberry Pie", "Fried Chicken", "Mint Milkshake", "Purple Potato Mash" }),
            new Country(34, "south_pole", "South Pole",
                "Reach the bottom of the world and cook the final feast of your world tour.",
                "The final feast at the South Pole: roast dinner, hot chocolate and an ice-cream celebration.",
                new[] { "Amundsen Base", "Polar Night Diner", "Pole Marker Feast" },
                new[] { "Roast Dinner", "Spinach Pie", "Cherry Ice Cream", "Blue Velvet Cake", "Banana Bread", "Hot Chocolate", "Grape Smoothie" }),
        };

        public static bool IsValidCountry(int country) => country >= 0 && country < CountryCount;
        public static bool IsValidContinent(int continent) => continent >= 0 && continent < ContinentCount;

        public static Country GetCountry(int country) => IsValidCountry(country) ? Countries[country] : null;
        public static Continent GetContinent(int continent) => IsValidContinent(continent) ? Continents[continent] : null;

        public static int ContinentOfCountry(int country) => IsValidCountry(country) ? country / CountriesPerContinent : -1;
        public static int CountryOfLevel(int levelIndex) => levelIndex < 0 ? -1 : Mathf.Min(levelIndex / LevelsPerCountry, CountryCount - 1);
        public static int ContinentOfLevel(int levelIndex) => ContinentOfCountry(CountryOfLevel(levelIndex));
        public static int GlobalCountry(int continent, int position) => continent * CountriesPerContinent + position;
        public static int FirstLevelOfCountry(int country) => country * LevelsPerCountry;
        public static int FirstLevelOfContinent(int continent) => continent * LevelsPerContinent;

        /// <summary>The Asian country in the same position, whose art stands in until a country has its own.</summary>
        public static int StandInCountry(int country) => IsValidCountry(country) ? country % CountriesPerContinent : 0;

        public static string GetCountryName(int country) => IsValidCountry(country) ? Countries[country].Name : string.Empty;
        public static string GetContinentName(int continent) => IsValidContinent(continent) ? Continents[continent].Name : string.Empty;

        public static string GetDishName(int country, int colour)
        {
            Country c = GetCountry(country);
            return c != null && colour >= 0 && colour < c.Dishes.Length ? c.Dishes[colour] : "???";
        }

        public static int CountryLevelsCompleted(int country)
        {
            int done = 0;
            int first = FirstLevelOfCountry(country);
            for (int i = 0; i < LevelsPerCountry; i++)
            {
                if (BusStop.GameProgress.IsLevelCompleted(first + i))
                    done++;
            }
            return done;
        }

        public static bool IsCountryComplete(int country) => CountryLevelsCompleted(country) >= LevelsPerCountry;

        /// <summary>A country opens when the level before its first level is done (Asia's first is always open).</summary>
        public static bool IsCountryUnlocked(int country) => IsValidCountry(country) && BusStop.GameProgress.IsLevelUnlocked(FirstLevelOfCountry(country));

        public static int ContinentLevelsCompleted(int continent)
        {
            int done = 0;
            int first = FirstLevelOfContinent(continent);
            for (int i = 0; i < LevelsPerContinent; i++)
            {
                if (BusStop.GameProgress.IsLevelCompleted(first + i))
                    done++;
            }
            return done;
        }

        public static bool IsContinentComplete(int continent) => ContinentLevelsCompleted(continent) >= LevelsPerContinent;

        public static bool IsContinentUnlocked(int continent) => IsValidContinent(continent) && BusStop.GameProgress.IsLevelUnlocked(FirstLevelOfContinent(continent));
    }
}
