# Country dishes – one picture per country

Only the five Asian countries have dish pictures today. Every other country borrows the dishes
of the Asian country in the same position (that is why Canada shows pani puri and samosa). As soon
as a country's dishes arrive, the board, the conveyor trays and the orders panel show its own food.

## What to generate

One image per country / Antarctic region (**30 images**), each showing that country's 7 dishes.

- **1536 × 1024**, **transparent background**.
- **7 separate dishes in a grid**: 4 in the top row, 3 in the bottom row, in the **exact order**
  of the table (1-4 top row left to right, 5-7 bottom row left to right). Clear empty space
  between dishes; no dish touches or overlaps another or the edge.
- Each dish **alone**: no plate under it unless the dish is served in its own bowl, glass or cup
  (the game draws a coloured plate under every dish).
- Same style as the Asian dishes: attach `Assets/Project Data/Game/Images/CountryFood/_country_food_sheet.png`.

**Prompt** (one per country, all in the same chat):

> Glossy, bright, hand-painted mobile game food icons in the exact style of the attached
> reference (Conveyor Chef cooking game): appetising, slightly 3D, soft top-left light, gentle
> shine. 7 separate dishes from **[COUNTRY]** on a fully transparent background, arranged in a
> grid: 4 in the top row, 3 in the bottom row, in this order: **[1-7]**. Each dish centred in its
> cell with clear empty space around it, all dishes the same size, seen from slightly above. No
> table, no tablecloth, no shadows on the background, no text, no labels.

| Folder | Country | Dishes (order) |
|---|---|---|
| north_america/usa | United States | 1 BBQ Ribs · 2 Key Lime Pie · 3 Strawberry Milkshake · 4 Blueberry Pancakes · 5 Mac and Cheese · 6 Mint Chip Sundae · 7 Grape Soda Float |
| north_america/mexico | Mexico | 1 Pozole Rojo · 2 Guacamole · 3 Pink Conchas · 4 Blue Corn Tacos · 5 Elote · 6 Lime Agua Fresca · 7 Jamaica Agua Fresca |
| north_america/canada | Canada | 1 Maple Glazed Salmon · 2 Fiddlehead Salad · 3 Raspberry Butter Tart · 4 Blueberry Grunt · 5 Poutine · 6 Mint Nanaimo Bar · 7 Saskatoon Berry Pie |
| north_america/cuba | Cuba | 1 Ropa Vieja · 2 Mojito Mocktail · 3 Guava Pastry · 4 Moros y Cristianos · 5 Cuban Sandwich · 6 Tostones · 7 Purple Yam Fritters |
| north_america/jamaica | Jamaica | 1 Jerk Chicken · 2 Callaloo · 3 Sorrel Punch · 4 Blue Drawers · 5 Ackee and Saltfish · 6 Coconut Water · 7 Otaheite Apple Jam |
| south_america/brazil | Brazil | 1 Churrasco · 2 Caipirinha Lime Soda · 3 Brigadeiro · 4 Coconut Cocada · 5 Pao de Queijo · 6 Guarana Soda · 7 Acai Bowl |
| south_america/argentina | Argentina | 1 Asado Steak · 2 Chimichurri · 3 Strawberry Alfajor · 4 Panqueque · 5 Empanadas · 6 Mate Tea · 7 Purple Grape Juice |
| south_america/peru | Peru | 1 Lomo Saltado · 2 Aji Verde Chicken · 3 Picarones · 4 Blue Potato Causa · 5 Papa a la Huancaina · 6 Ceviche · 7 Chicha Morada |
| south_america/colombia | Colombia | 1 Bandeja Paisa · 2 Avocado Salad · 3 Guava Oblea · 4 Ajiaco Soup · 5 Arepa de Choclo · 6 Lulada · 7 Mora Juice |
| south_america/chile | Chile | 1 Salmon Ceviche · 2 Pebre Salsa · 3 Mote con Huesillo · 4 Blueberry Kuchen · 5 Sopaipillas · 6 Leche Nevada · 7 Maqui Berry Shake |
| europe/italy | Italy | 1 Margherita Pizza · 2 Pesto Pasta · 3 Strawberry Gelato · 4 Blueberry Panna Cotta · 5 Risotto alla Milanese · 6 Pistachio Gelato · 7 Tiramisu |
| europe/france | France | 1 Ratatouille · 2 Pistachio Macaron · 3 Raspberry Macaron · 4 Blueberry Crepe · 5 Croissant · 6 Mint Eclair · 7 Cassis Tart |
| europe/spain | Spain | 1 Gazpacho · 2 Pimientos de Padron · 3 Fresas con Nata · 4 Crema Catalana · 5 Seafood Paella · 6 Horchata · 7 Grape Sangria Mocktail |
| europe/greece | Greece | 1 Tomato Gemista · 2 Greek Salad · 3 Rose Loukoumi · 4 Yogurt with Honey · 5 Spanakopita · 6 Tzatziki · 7 Moussaka |
| europe/germany | Germany | 1 Currywurst · 2 Green Sauce Potatoes · 3 Strawberry Cake · 4 Blueberry Pancake · 5 Soft Pretzel · 6 Apple Spritzer · 7 Black Forest Cake |
| africa/morocco | Morocco | 1 Harira Soup · 2 Mint Tea · 3 Milk Pastilla · 4 Chebakia · 5 Chicken Tagine · 6 Couscous · 7 Almond Briouat |
| africa/egypt | Egypt | 1 Shakshuka · 2 Falafel · 3 Basbousa · 4 Om Ali · 5 Koshari · 6 Mint Lemonade · 7 Karkade |
| africa/nigeria | Nigeria | 1 Jollof Rice · 2 Efo Riro · 3 Chin Chin · 4 Moi Moi · 5 Puff-Puff · 6 Kunu Drink · 7 Zobo Drink |
| africa/ethiopia | Ethiopia | 1 Doro Wat · 2 Gomen · 3 Ambasha Bread · 4 Injera Platter · 5 Shiro Wat · 6 Spiced Coffee · 7 Beetroot Salad |
| africa/south_africa | South Africa | 1 Boerewors · 2 Chakalaka · 3 Malva Pudding · 4 Rusks · 5 Bobotie · 6 Rooibos Tea · 7 Koeksisters |
| oceania/australia | Australia | 1 Aussie Meat Pie · 2 Avocado Toast · 3 Pavlova · 4 Lamington · 5 Honey Crumpets · 6 Mint Slice · 7 Passionfruit Tart |
| oceania/new_zealand | New Zealand | 1 Lamb Roast · 2 Kiwifruit Salad · 3 Pink Pavlova · 4 Blueberry Muffin · 5 Hokey Pokey Ice Cream · 6 Feijoa Smoothie · 7 Boysenberry Pie |
| oceania/fiji | Fiji | 1 Lovo Chicken · 2 Palusami · 3 Guava Pudding · 4 Fish in Lolo · 5 Cassava Cake · 6 Kokoda · 7 Taro Pudding |
| oceania/samoa | Samoa | 1 Sapasui · 2 Palusami · 3 Pani Popo · 4 Oka · 5 Fa'ausi · 6 Coconut Lime Drink · 7 Koko Samoa |
| oceania/tonga | Tonga | 1 Lu Pulu · 2 Ota Ika · 3 Watermelon Juice · 4 Coconut Pudding · 5 Faikakai Topai · 6 Keke Doughnuts · 7 Kumala Pudding |
| antarctica/peninsula | Peninsula | 1 Hot Tomato Soup · 2 Pea Soup · 3 Pink Salmon Bites · 4 Blueberry Porridge · 5 Golden Pancakes · 6 Mint Hot Cocoa · 7 Plum Crumble |
| antarctica/weddell_sea | Weddell Sea | 1 Fish Stew · 2 Herb Dumplings · 3 Strawberry Jam Roll · 4 Icy Blue Jelly · 5 Corn Chowder · 6 Seaweed Crackers · 7 Berry Muffin |
| antarctica/ross_shelf | Ross Shelf | 1 Grilled Sausage · 2 Green Bean Stew · 3 Pink Cupcake · 4 Blue Ice Pop · 5 Cheese Toastie · 6 Mint Tea · 7 Grape Pie |
| antarctica/mcmurdo | McMurdo | 1 Station Pizza · 2 Veggie Burger · 3 Raspberry Waffles · 4 Blueberry Pie · 5 Fried Chicken · 6 Mint Milkshake · 7 Purple Potato Mash |
| antarctica/south_pole | South Pole | 1 Roast Dinner · 2 Spinach Pie · 3 Cherry Ice Cream · 4 Blue Velvet Cake · 5 Banana Bread · 6 Hot Chocolate · 7 Grape Smoothie |

## Delivery

A zip with `<continent>/<country>/dishes.png` (folder names from the table), e.g.
`north_america/canada/dishes.png`. You can send one continent at a time. I cut each sheet into
its 7 dishes, trim them and put them in `Resources/World/<continent>/<country>/dish_<colour>.png`,
where the game already looks for them.

**Before sending:** transparent background (no white or checkerboard baked in), 7 dishes in the
right order, nothing touching, no text.
