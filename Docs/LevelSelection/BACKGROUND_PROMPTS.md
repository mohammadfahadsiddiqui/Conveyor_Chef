# Level Selection – new background per country

Today every country's Level Selection uses the same landmark picture twice: small in the gold
"postcard" frame and huge as the full-screen background. The postcard keeps its landmark; the
**background** gets a new, calmer scene of the same country, so the postcard stands out.

## What to generate

- **30 images**, one per country (India and the 4 other Asian countries included).
- **1080 × 1920** portrait (9:16), opaque, fills the frame.
- File name **`levelselect_background.png`**, delivered as
  `<continent>/<country>/levelselect_background.png` in a zip (same as the earlier batches).
  Continent and country ids are in the table below.

## Composition rules (see `background_layout_reference.png`)

The UI covers most of the screen (buttons, title + postcard frame, the 3 level cards, chef and
progress bar). Only the top strip, the left and right edges, the gap under the cards and the
bottom edge are seen. So:

- **No big landmark** and nothing important in the middle: that is the postcard's job.
- **Colour and mood** everywhere: a beautiful sky across the top quarter, the country's landscape
  through the middle, rich foreground details (plants, water, local objects) along the left and
  right edges and the bottom.
- **Slightly softer and less detailed than the postcard**, gentle depth, so text and cards stay
  readable on top of it.
- Warm, inviting light (golden hour, sunrise or soft daylight).

## Prompt template (attach `style_reference` = the country's current postcard picture)

> Bright, glossy, hand-painted mobile game background in the exact art style of the attached
> image (Conveyor Chef cooking game). Portrait 1080×1920, full frame. **[SCENE]**. No famous
> landmark in the centre, keep the centre calm and simple because game UI covers it; a beautiful
> sky across the top quarter, rich foreground details along the left and right edges and the
> bottom. Slightly soft, gentle depth, warm inviting light. No text, no logos, no UI, no people
> in focus.

Replace **[SCENE]** with the country's scene:

| Continent / country (folder) | [SCENE] |
|---|---|
| asia / china | Misty karst mountains along the Li River with bamboo rafts, red lanterns hanging from branches in the foreground, soft dawn light |
| asia / japan | A calm river lined with cherry-blossom trees and glowing paper lanterns at dusk, tiled roofs far in the distance |
| asia / india | Kerala backwaters with coconut palms, a traditional houseboat and lotus flowers, golden hour |
| asia / south_korea | Hanok village rooftops among red and orange autumn maples, misty mountains at sunset |
| asia / thailand | A floating market canal at sunrise with wooden boats full of tropical fruit, palm trees and orchids |
| north_america / usa | A desert highway through red mesas of Monument Valley at sunset, cacti in the foreground |
| north_america / mexico | Colourful hillside houses of Guanajuato with papel picado banners and marigold flowers at dusk |
| north_america / canada | An autumn maple forest beside a calm mountain lake with a wooden dock and canoe, snowy peaks behind |
| north_america / cuba | The green Viñales valley with tobacco fields and round limestone hills at sunrise, royal palms |
| north_america / jamaica | A turquoise beach cove with palm trees and colourful wooden fishing boats at sunset |
| south_america / brazil | The Amazon river winding through lush rainforest with toucans and giant leaves, golden light |
| south_america / argentina | Pampas grassland with grazing horses and a distant ranch house under a wide sunset sky |
| south_america / peru | Green Sacred Valley terraces with llamas and morning mist, Andes peaks behind |
| south_america / colombia | The Cocora valley with tall wax palms on misty green coffee hills |
| south_america / chile | The Atacama desert salt flats under a starry Milky Way sky, warm horizon glow |
| europe / italy | Rolling Tuscan hills with cypress trees, vineyards and a stone farmhouse at golden hour |
| europe / france | Provence lavender fields in rows with a stone farmhouse and sunflowers at sunset |
| europe / spain | A white Andalusian hill village with orange trees and flower pots on balconies, warm evening |
| europe / greece | Olive-grove terraces above the deep-blue Aegean sea with a small white chapel, bright afternoon |
| europe / germany | Rhine valley vineyards above the river with a riverboat and half-timbered houses at sunset |
| africa / morocco | Golden Sahara dunes at sunset with a camel caravan and hanging brass lanterns in the foreground |
| africa / egypt | The Nile at dusk with white felucca sails, palm trees and reed banks (no pyramids) |
| africa / nigeria | Lush green hills above a colourful riverside market with canoes at golden hour |
| africa / ethiopia | The Simien Mountains' green cliffs and terraced fields at sunrise, gelada monkeys on a ledge |
| africa / south_africa | Savanna with acacia trees, giraffes and elephants under a big orange sunset sky |
| oceania / australia | Red outback desert with kangaroos, spinifex grass and a huge sunset sky |
| oceania / new_zealand | Green rolling hills with sheep beside a turquoise alpine lake, snowy peaks |
| oceania / fiji | A turquoise lagoon with leaning coconut palms and a white sandbar at sunset |
| oceania / samoa | A jungle waterfall pouring into a clear pool, tropical flowers, black-sand beach glimpse |
| oceania / tonga | A blue bay with a humpback whale tail rising from the water, small palm islands at sunset |

## Checklist before sending

- 1080 × 1920 portrait, no transparency.
- No main landmark (it is already in the postcard), calm centre.
- Same glossy painted style as the postcard pictures, no text.

When the zip arrives the backgrounds go in `Resources/World/<continent>/<country>/`, and each
country's **Background Artwork** in LevelSelection.unity is switched to its new picture; the
postcard frame keeps the landmark.
