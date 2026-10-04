# Country Map – image guide for all continents

The Country Map is the screen between the World Map and Level Selection: one continent, its
5 countries as island dioramas joined by a dotted route, and a progress panel at the bottom.
Asia is finished. This guide lists every image the other 5 continents need.

## 1. What changes per continent (and what does not)

Everything else on the screen is shared and **must not be generated again**: title board,
back / settings / coin buttons, treasure-map scroll, route dots and locks, country name frame,
stars, selection halo, lock badge, completion badge, chef, speech bubble, compass and the whole
progress panel (except its globe).

Per continent you generate **12 images**:

| # | Image | File name | Size | Background | How it is shown |
|---|---|---|---|---|---|
| 1 | Continent map | `continent_map.png` | 1080 × 1920 (9:16, portrait) | Opaque, fills the frame | Full-screen background |
| 2 | Progress globe | `progress_globe.png` | 1254 × 1254 (square) | Transparent | 145 × 145 inside a gold ring |
| 3–7 | Country diorama × 5 | `map_diorama.png` | 1254 × 1254 (square) | Transparent | 270 × 270 on the map |
| 8–12 | Flag badge × 5 | `flag_badge.png` | 1254 × 1254 (square) | Transparent | 72 × 72 next to the country name |

5 continents × 12 = **60 images**.

## 2. Reference images (attach them in ChatGPT)

- `style_reference.png` – the Asia versions of every image type. Attach it to **every** prompt
  and say "match this style exactly".
- `layout_reference.png` – the Asia map with the 5 diorama spots and the areas covered by UI.
  Attach it to every **continent map** prompt.
- For the continent's shape, also attach its World Map art from
  `Assets/Project Data/Game/Images/WorldMap/`:
  `colorful_cartoon_north_america_map.png`, `colourful_south_america_game_map.png`,
  `vibrant_cartoon_europe_map.png`, `whimsical_africa_adventure_map.png`,
  `australia_and_oceania_adventure_map.png`.

## 3. Shared style (paste at the start of every prompt)

> Bright, glossy, hand-painted mobile game art in the exact style of the attached reference
> (Conveyor Chef, a cooking puzzle game). Saturated cheerful colours, soft warm sunlight, thick
> clean shapes, gentle highlights, slightly 3D cartoon look. No text, no letters, no logos, no
> watermark, no UI, no people unless asked.

## 4. Rules per image type

**Continent map** (`continent_map.png`)
- Portrait 1080 × 1920, fills the whole canvas, no border, no transparency.
- A stylised, slightly tilted top-down fantasy map of the continent: land, forests, mountains,
  rivers, beaches, blue ocean with small islands, a few clouds and birds at the edges, like the
  Asia map.
- **Keep the top 19 % and bottom 17 % calm** (sky, clouds, sea, distant land): buttons, the title
  board and the progress panel cover them.
- **Keep the 5 diorama spots free of important details** (open land or sea, nothing that must be
  seen): they are covered by the country dioramas. Spots (centre, from the left / from the top):
  1 = 24 % / 32 %, 2 = 76 % / 37 %, 3 = 22 % / 53 %, 4 = 77 % / 59 %, 5 = 52 % / 76 %
  (see `layout_reference.png`). Each covers a circle about one third of the screen width.
- Small painted landmarks between the spots are welcome (like the pagoda, Great Wall and Mt Fuji
  on Asia).

**Progress globe** (`progress_globe.png`)
- Square 1254 × 1254, transparent background.
- A glossy blue cartoon globe (ocean) with the continent as bright green land in the centre,
  facing the viewer, white shine at the top left. No ring, no stand, no shadow: the gold ring is
  a separate image. The globe fills about 85 % of the square, centred.

**Country diorama** (`map_diorama.png`)
- Square 1254 × 1254, transparent background, the diorama fills about 95 % of the width.
- A small round floating island seen from slightly above (isometric), standing on rocks and a
  ring of turquoise water. On it: the country's famous landmark (back, largest), local plants,
  and 1–2 of its signature dishes (front, on a plate, bowl or stall), exactly like the China and
  India dioramas.
- No ground shadow outside the island, no text, no sky.

**Flag badge** (`flag_badge.png`)
- Square 1254 × 1254, transparent background.
- A round glossy button: the country's flag inside a white inner rim, a light-blue outer rim and
  a thick dark-navy outline, top-left shine, exactly like the India and South Korea badges.
- The flag must be **correct** (colours, stripes, symbols, orientation). Badge fills ~90 %.

## 5. Prompts

Order of countries = their number on the map (1 top-left … 5 bottom-centre).

### North America (`north_america`)

**Continent map**
> [Shared style] Portrait 1080×1920 fantasy game map of North America in the attached style:
> Canadian Rockies and pine forests in the north, the USA's plains and cities in the middle,
> Mexico's deserts and Mayan jungle in the south, Caribbean sea with Cuba and Jamaica islands on
> the right. Follow the attached layout guide: calm sky and sea in the top 19 % and bottom 17 %,
> no important details in the 5 marked circles.

**Progress globe**
> [Shared style] Glossy cartoon globe, transparent background, North America (with Central America
> and the Caribbean) as bright green land facing the viewer, blue ocean, white shine top left.
> No ring, no stand.

| # | Country | Diorama prompt (after the shared style) | Flag |
|---|---|---|---|
| 1 | United States (`usa`) | Floating island diorama with the Statue of Liberty, a small red-chrome diner, a plate of BBQ ribs and a strawberry milkshake, green trees, turquoise water ring. | Stars and Stripes: 13 red/white stripes, blue canton with white stars |
| 2 | Mexico (`mexico`) | Floating island diorama with the Chichén Itzá pyramid, cacti and agave, a colourful taco stand with blue corn tacos and guacamole, papel picado. | Vertical green-white-red, eagle on cactus emblem in the centre |
| 3 | Canada (`canada`) | Floating island diorama with a snowy Rocky mountain peak, turquoise lake with a red canoe, maple trees in red, a bowl of poutine and maple-glazed salmon. | Red-white-red vertical, red maple leaf in the centre |
| 4 | Cuba (`cuba`) | Floating island diorama with the Havana Capitolio dome, a pink classic car, palm trees, a Cuban sandwich and guava pastries on a café table. | 5 blue/white stripes, red triangle at the hoist with a white star |
| 5 | Jamaica (`jamaica`) | Floating island diorama with Dunn's River waterfall cascading over rocks, palm trees, a jerk chicken grill with smoke and fresh coconuts. | Gold diagonal cross, green top/bottom, black left/right triangles |

### South America (`south_america`)

**Continent map**
> [Shared style] Portrait 1080×1920 fantasy game map of South America in the attached style: the
> Amazon rainforest and rivers in the north, Brazil's beaches on the east coast, the Andes
> mountains along the west, Patagonia's glaciers in the south, blue ocean on both sides. Follow
> the attached layout guide: calm sky and sea in the top 19 % and bottom 17 %, no important
> details in the 5 marked circles.

**Progress globe**
> [Shared style] Glossy cartoon globe, transparent background, South America as bright green land
> facing the viewer, blue ocean, white shine top left. No ring, no stand.

| # | Country | Diorama prompt | Flag |
|---|---|---|---|
| 1 | Brazil (`brazil`) | Floating island diorama with Christ the Redeemer on Sugarloaf-style hills, a beach kiosk, churrasco skewers on a grill and an açaí bowl. | Green field, yellow diamond, blue starry globe with a white band |
| 2 | Argentina (`argentina`) | Floating island diorama with Patagonia peaks (Fitz Roy) and the Buenos Aires Obelisco, an asado grill with steaks, empanadas and a mate cup. | Light-blue / white / light-blue horizontal, golden Sun of May |
| 3 | Peru (`peru`) | Floating island diorama with Machu Picchu terraces and Huayna Picchu, a llama, a bowl of ceviche and purple chicha morada. | Vertical red-white-red |
| 4 | Colombia (`colombia`) | Floating island diorama with colourful Cartagena colonial houses with balconies and bougainvillea, coffee plants, arepas and a fruit juice. | Horizontal yellow (top half), blue, red |
| 5 | Chile (`chile`) | Floating island diorama with snowy Andes peaks above vineyards, a starry Atacama telescope dome, salmon ceviche and sopaipillas. | White over red, blue square top-left with a white star |

### Europe (`europe`)

**Continent map**
> [Shared style] Portrait 1080×1920 fantasy game map of Europe in the attached style:
> Scandinavian fjords and forests in the north, central European castles and farmland, the Alps,
> the Mediterranean coast with Italy, Greece's islands and Spain in the south, blue seas. Follow
> the attached layout guide: calm sky and sea in the top 19 % and bottom 17 %, no important
> details in the 5 marked circles.

**Progress globe**
> [Shared style] Glossy cartoon globe, transparent background, Europe as bright green land facing
> the viewer, blue ocean, white shine top left. No ring, no stand.

| # | Country | Diorama prompt | Flag |
|---|---|---|---|
| 1 | Italy (`italy`) | Floating island diorama with the Colosseum, cypress trees, a Venice gondola in the water ring, a margherita pizza and gelato. | Vertical green-white-red |
| 2 | France (`france`) | Floating island diorama with the Eiffel Tower, lavender fields, a pâtisserie stall with croissants and pastel macarons. | Vertical blue-white-red |
| 3 | Spain (`spain`) | Floating island diorama with the Sagrada Família, orange trees, a large seafood paella pan and churros. | Red-yellow-red horizontal (yellow twice as tall), coat of arms on the left |
| 4 | Greece (`greece`) | Floating island diorama with white Santorini houses and blue domes on a cliff, olive trees, a Greek salad and spanakopita. | 9 blue/white stripes, blue canton with a white cross |
| 5 | Germany (`germany`) | Floating island diorama with Neuschwanstein Castle, a Black Forest edge, soft pretzels and a Black Forest cake. | Horizontal black-red-gold |

### Africa (`africa`)

**Continent map**
> [Shared style] Portrait 1080×1920 fantasy game map of Africa in the attached style: the Sahara
> desert and Atlas mountains in the north, the Nile with palms, savanna with acacia trees in the
> middle, rainforest, Table Mountain and coast in the south, blue ocean. Follow the attached layout
> guide: calm sky and sea in the top 19 % and bottom 17 %, no important details in the 5 marked
> circles.

**Progress globe**
> [Shared style] Glossy cartoon globe, transparent background, Africa as bright green land facing
> the viewer, blue ocean, white shine top left. No ring, no stand.

| # | Country | Diorama prompt | Flag |
|---|---|---|---|
| 1 | Morocco (`morocco`) | Floating island diorama with the Koutoubia minaret, palm trees, a blue Chefchaouen doorway, a chicken tagine and mint tea with a silver teapot. | Red field with a green five-pointed star outline |
| 2 | Egypt (`egypt`) | Floating island diorama with the Giza pyramids and the Sphinx, the Nile with a felucca in the water ring, falafel and koshari. | Horizontal red-white-black, golden eagle in the centre |
| 3 | Nigeria (`nigeria`) | Floating island diorama with Zuma Rock, green hills, a market stall with jollof rice and puff-puff. | Vertical green-white-green |
| 4 | Ethiopia (`ethiopia`) | Floating island diorama with the rock-cut Church of St George in Lalibela, coffee plants, injera with doro wat and a clay coffee pot (jebena). | Horizontal green-yellow-red, blue disc with a yellow star |
| 5 | South Africa (`south_africa`) | Floating island diorama with Table Mountain, protea flowers, a braai grill with boerewors and bobotie. | Green Y shape with gold and white edges, red top, blue bottom, black triangle |

### Oceania (`oceania`)

**Continent map**
> [Shared style] Portrait 1080×1920 fantasy game map of Oceania in the attached style: Australia's
> red outback, Sydney harbour and the Great Barrier Reef, New Zealand's green mountains and
> fjords, and the tropical Pacific islands (Fiji, Samoa, Tonga) with lagoons, wide blue ocean.
> Follow the attached layout guide: calm sky and sea in the top 19 % and bottom 17 %, no important
> details in the 5 marked circles.

**Progress globe**
> [Shared style] Glossy cartoon globe, transparent background, Australia, New Zealand and the
> Pacific islands as bright green land facing the viewer, blue ocean, white shine top left.
> No ring, no stand.

| # | Country | Diorama prompt | Flag |
|---|---|---|---|
| 1 | Australia (`australia`) | Floating island diorama with the Sydney Opera House, eucalyptus trees, a koala, an Aussie meat pie and a pavlova. | Blue field, Union Jack top-left, big white star below it, Southern Cross on the right |
| 2 | New Zealand (`new_zealand`) | Floating island diorama with Milford Sound peaks and a waterfall, silver ferns, a kiwi bird, a hāngī food basket and a pavlova. | Blue field, Union Jack top-left, four red stars with white edges |
| 3 | Fiji (`fiji`) | Floating island diorama with thatched bure huts over a turquoise lagoon, palm trees, kokoda in a coconut shell and a lovo pit. | Light blue field, Union Jack top-left, shield on the right |
| 4 | Samoa (`samoa`) | Floating island diorama with the To Sua ocean trench and its ladder, tropical plants, palusami and pani popo buns. | Red field, blue rectangle top-left with five white stars |
| 5 | Tonga (`tonga`) | Floating island diorama with the white-and-red Royal Palace of Nuku'alofa, palm trees, a feast table with lu pulu and fresh fish. | Red field, white rectangle top-left with a red cross |

## 6. How to deliver

Put the PNGs in a zip with this structure (same as the Level Selection batches):

```
north_america/continent_map.png
north_america/progress_globe.png
north_america/usa/map_diorama.png
north_america/usa/flag_badge.png
north_america/mexico/map_diorama.png
...
oceania/tonga/flag_badge.png
```

Folder names are the ids in brackets above. They go into
`Assets/Project Data/Game/Resources/World/<continent>/...`, where the game already looks for them:
as soon as a file is there, that continent or country uses it instead of the Asian stand-in. No
scene work is needed, and you can deliver one continent at a time.

### Checklist before sending
- Correct size and shape (portrait map, square others).
- Transparent background on globe, dioramas and flags (no white or checkerboard pattern baked in).
- Flags checked against the real flag.
- No text anywhere in the images.
- Map: calm top and bottom bands, nothing important inside the 5 circles.
