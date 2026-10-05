# Antarctica – images to generate (chapter 7)

Antarctica is playable already with stand-in art (Oceania on the Country Map, Chile in Level
Selection, an icy-tinted Oceania on the World Map). These images replace the stand-ins; each one
works as soon as it is in place, no scene work needed.

Regions (instead of countries), in order:

| # | Region (folder id) | Missions (level cards) |
|---|---|---|
| 1 | Peninsula (`peninsula`) | Paradise Harbour · Lemaire Channel · Penguin Point |
| 2 | Weddell Sea (`weddell_sea`) | Emperor Colony · Sea Ice Camp · Iceberg Alley |
| 3 | Ross Shelf (`ross_shelf`) | Ice Shelf Snack Bar · Glacier Grill · Whale Bay |
| 4 | McMurdo (`mcmurdo`) | Station Mess Hall · Volcano View Cafe · Supply Ship Dock |
| 5 | South Pole (`south_pole`) | Amundsen Base · Polar Night Diner · Pole Marker Feast |

Style for every image (paste first, attach the matching image of another continent as reference):
> Bright, glossy, hand-painted mobile game art in the exact style of the attached reference
> (Conveyor Chef cooking game), cheerful and cosy, warm light on cold snow, no text, no logos, no UI.

## 1. Country Map (12 images) – same sizes and rules as the other continents

| File | Size | Prompt (after the style line) |
|---|---|---|
| `antarctica/continent_map.png` | 1080×1920 portrait, opaque | Fantasy game map of Antarctica: snowy plains, glaciers, blue ice cliffs, icebergs in a deep-blue sea, a smoking volcano, penguins and a small red research station. Calm sky and sea in the top 19 % and bottom 17 %; leave the 5 spots of the Country Map layout reference free of important details. |
| `antarctica/progress_globe.png` | 1254×1254, transparent | Glossy cartoon globe seen from below, Antarctica as bright white-and-ice-blue land facing the viewer, blue ocean, shine top-left. No ring, no stand. |
| `antarctica/<region>/map_diorama.png` ×5 | 1254×1254, transparent | Small round floating ice-island diorama with a ring of icy turquoise water: **[DIORAMA]** |
| `antarctica/<region>/flag_badge.png` ×5 | 1254×1254, transparent | Round glossy badge with a white inner rim, light-blue outer rim and thick dark-navy outline, showing **[EMBLEM]** on an ice-blue background. |

| Region | [DIORAMA] | [EMBLEM] |
|---|---|---|
| peninsula | snowy peaks and a sheltered bay with gentoo penguins, a small red expedition boat, a bowl of hot tomato soup and golden pancakes | a cute gentoo penguin |
| weddell_sea | sea ice with an emperor penguin colony, a tent camp, a pot of fish stew and berry muffins | an emperor penguin chick |
| ross_shelf | the tall ice-shelf cliff edge with a whale tail in the water, a grill with sausages and a pink cupcake | a blue glacier with a whale tail |
| mcmurdo | a cosy research station under a smoking volcano (Mount Erebus), a supply ship, a pizza and a milkshake | a snowy volcano with a research hut |
| south_pole | the striped ceremonial pole with a mirror ball and flags around it, a dome station, a roast dinner and hot chocolate | the striped South Pole marker with a star |

## 2. Level Selection (25 images) – same sizes and rules as the other countries

Per region: `antarctica/<region>/levelselect_hero.png` (941×1672 portrait, the region's landmark
for the postcard frame), `level_1.png`, `level_2.png`, `level_3.png` (1448×1086, one per mission)
and `levelselect_background.png` (1080×1920, calm scene without the landmark, see
Docs/LevelSelection/BACKGROUND_PROMPTS.md).

| Region | Postcard (hero) | level_1 / level_2 / level_3 | Background |
|---|---|---|---|
| peninsula | Snowy mountains over a bay full of icebergs and penguins | Paradise Harbour: red huts and boats in a mirror-still bay / Lemaire Channel: ship between tall snowy cliffs / Penguin Point: picnic table among penguins | Pastel icebergs drifting on calm water at sunset |
| weddell_sea | Huge emperor penguin colony on sea ice | Emperor Colony / Sea Ice Camp: tents and a cooking stove on the ice / Iceberg Alley: kayaks among giant icebergs | Endless sea ice under a pink polar sky |
| ross_shelf | The towering Ross Ice Shelf cliff above the sea | Ice Shelf Snack Bar: snack hut on top of the ice / Glacier Grill: grill by a blue glacier / Whale Bay: whales and a small boat | Blue-white glacier field with soft snow clouds |
| mcmurdo | Research station below smoking Mount Erebus | Station Mess Hall: cosy busy canteen / Volcano View Cafe: cafe window facing the volcano / Supply Ship Dock: icebreaker unloading crates | Aurora australis over snowy hills |
| south_pole | The ceremonial South Pole marker with flags | Amundsen Base: dome research base / Polar Night Diner: glowing diner under stars / Pole Marker Feast: party table at the pole | Starry polar night with a green aurora over flat snow |

## 3. World Map (1 image)

`worldmap/continent_antarctica.png` – now a chapter with a pin: generate it with the other World
Map continents (Docs/WorldMap/WORLD_MAP_PLAN.md), 1536×1024 landscape, transparent.

## Delivery

One zip with the folders as written (`antarctica/...`, `worldmap/...`). They go into
`Resources/World/antarctica/...`; stand-ins disappear automatically as each file arrives.
