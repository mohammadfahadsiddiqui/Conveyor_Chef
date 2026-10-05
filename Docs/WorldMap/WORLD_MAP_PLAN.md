# World Map – one painted real world map (option B)

## The plan

The World Map becomes a **real world map** the player scrolls in every direction: each continent
where it really is (North and South America on the left, Europe and Africa in the middle, Asia on
the right, Oceania bottom-right), roughly in real proportion (Asia biggest, Europe smallest), with
sea between them. A dotted sea route joins them in chapter order (Asia → North America over the
north → South America → Europe → Africa → Oceania). See `layout_mockup.png`; it uses the current
continent pictures as stand-ins.

A single generated picture would be blurry: the map is about 3600 × 2400 units, while ChatGPT
images are at most ~1700 px. So the map is built from pieces painted to match:

| Piece | Who | How it stays sharp |
|---|---|---|
| 6 continents, one matching set | you generate | each shown about the size it is painted |
| Ocean | you generate one seamless tile | repeated underneath, any map size |
| Dotted route, numbers, pins, locks, glow | me, in Unity | drawn by the game |
| Ships, whale, small islands, clouds (optional) | you generate | small props |

## What you generate

### 1. Six continents – `continent_<id>.png`

- 1254 × 1254, **transparent background**, the continent fills about 90 % of the square.
- **All six must look like one set:** same camera (seen from above with a slight tilt), same
  sunlight from the top-left, same amount of detail, same colours of water and sand.
- Each has a ring of **turquoise shallow water and sand** around its coast that **fades out to
  transparent** at the edges, so it melts into the ocean (no hard cut-out edge, no square, no
  dark outline).
- Small painted landmarks and nature on the land, like the current World Map continents.
- No text, no labels, no pins, no ocean beyond the shallow-water ring.

Attach the current `whimsical_africa_adventure_map.png` and `vibrant_cartoon_europe_map.png`
(Images/WorldMap) as style references to **every** continent prompt, and generate all six in
the same chat so they stay consistent.

**Prompt template:**
> Bright, glossy, hand-painted mobile game art matching the attached references (Conveyor Chef
> world map). A single continent seen from above with a slight tilt, sunlight from the top-left:
> **[CONTINENT]**. Lush cartoon land with small landmarks and nature, a ring of turquoise shallow
> water and sand around the coast that fades smoothly to fully transparent at the edges.
> Transparent background, continent fills about 90 % of a 1254×1254 square, centred. No text, no
> labels, no pins, no open ocean, no dark outline. Same style, camera and lighting as the other
> continents in this set.

| File | [CONTINENT] |
|---|---|
| `continent_asia.png` | Asia: the Great Wall and pagodas, Mount Fuji, the Taj Mahal, Korean palace roofs, Thai temples, bamboo, cherry blossoms, rice terraces, Himalayan snow peaks |
| `continent_north_america.png` | North America: the Rockies and pine forests, the Statue of Liberty, desert mesas and cacti, a Mayan pyramid, Caribbean islands with palms |
| `continent_south_america.png` | South America: the Amazon rainforest and river, the Andes, Machu Picchu, Christ the Redeemer, Patagonia glaciers, toucans |
| `continent_europe.png` | Europe: the Eiffel Tower, the Colosseum, Neuschwanstein castle, Santorini white houses, lavender fields, Alps, windmills |
| `continent_africa.png` | Africa: the pyramids and the Nile, Sahara dunes, savanna with acacias and giraffes, Table Mountain, rainforest, Moroccan minaret |
| `continent_oceania.png` | Australia with the red outback, Uluru and the Sydney Opera House, New Zealand's green mountains, the Great Barrier Reef, small Pacific islands with palms |

**Shape:** each continent's real outline, north at the top, so the six fit together as a real
world map (Africa looks like Africa). Their size on the map is set in Unity, so all six can fill
their square the same way.

### 2. Ocean tile – `ocean_tile.png`

- 1024 × 1024, opaque, **seamless / tileable** in both directions (left edge continues the
  right edge, top continues the bottom).
- Calm cartoon ocean, medium blue, soft lighter wave marks and gentle sparkles, the same water
  colour as the continents' outer shallow water.
- **Nothing else**: no islands, ships, compass, clouds or land (they would repeat).

> Seamless tileable texture, 1024×1024, bright glossy cartoon ocean water for a mobile game,
> medium blue with soft lighter wave marks and small sparkles, even lighting, no objects, no
> islands, no land, no text. The left edge must continue the right edge and the top must continue
> the bottom.

### 3. Props (optional, nice to have) – `prop_<name>.png`

1024 × 1024, transparent, same style, seen from the same slight top-down angle, no text:
`prop_sailboat.png` (small sailing boat with a red flag), `prop_steamship.png` (small cartoon
steamboat with smoke), `prop_whale.png` (blue whale with a water spout), `prop_island_a.png` and
`prop_island_b.png` (tiny palm islands with shallow-water ring fading out), `prop_cloud.png`
(soft white cloud cluster).

## Delivery

One zip:

```
worldmap/continent_asia.png
worldmap/continent_north_america.png
worldmap/continent_south_america.png
worldmap/continent_europe.png
worldmap/continent_africa.png
worldmap/continent_oceania.png
worldmap/ocean_tile.png
worldmap/props/prop_sailboat.png   (optional, and the other props)
```

Before sending: transparent backgrounds on continents and props, soft faded coast edges, the
ocean tile tested by placing two copies side by side (no visible seam), no text anywhere.

## What I do when the images arrive

1. Rebuild the scrolling map in WorldMap.unity: about 3600 × 2400 units, the ocean tile repeated
   underneath, the six continents at their real places and proportions, the dotted sea route in
   chapter order with the props along it and clouds at the edges.
2. Move each continent's pin, lock, glow and tap area with it; the chapter bar, arrows and
   "focus on the selected continent" keep working as they do now.
3. Open the map centred on the player's current chapter; free scrolling in every direction.
4. Everything stays normal scene objects you can move and edit in the Hierarchy.
