# Conveyor Chef Country Map

The Country Map uses the same scene-authoring model as menu.unity and loading.unity.

- Canvas: Screen Space - Overlay
- CanvasScaler: Scale With Screen Size
- Reference: 1080 x 1920
- Match: 0.5
- Runtime scripts never rebuild or reposition the visual hierarchy
- All country artwork is serialized into Canvas > NEW Country Map after baking

## Current generated art pack

The first full country-map pack is Asia:

1. China
2. Japan
3. India
4. South Korea
5. Thailand

Each country owns 3 progression levels, preserving the existing 15-level-per-continent structure.

## Setup

1. Download ConveyorChef_CountryMap_Assets_ForUnity.zip from the chat.
2. In Unity choose Conveyor Chef > Country Map > 0. Import Generated Art Pack.
3. The placeholder CountryMap.unity is replaced by the complete serialized editable scene.
4. Open Assets/Project Data/Game/Scenes/CountryMap.unity.
5. Adjust any Image/RectTransform directly in Scene and Inspector.

The editor baker imports PNGs as Sprite (2D and UI), Single, no mip maps, alpha transparency enabled, 4096 max texture size, and uncompressed while authoring.
