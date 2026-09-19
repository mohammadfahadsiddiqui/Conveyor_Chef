# Conveyor Chef World Map Assets

This folder is the expected home for the 20 generated World Map art files.

The World Map builder is intentionally designed like the fixed Main Menu composition:
- 1080 x 1920 authored reference frame
- whole root scales uniformly
- device safe area respected
- child RectTransforms are not rearranged in Play mode
- ocean artwork covers aspect-ratio letterbox space
- the scrollable map itself pans inside a masked viewport

## Required files

1. tropical_ocean_map_adventure.png
2. colorful_cartoon_north_america_map.png
3. colourful_south_america_game_map.png
4. vibrant_cartoon_europe_map.png
5. whimsical_africa_adventure_map.png
6. whimsical_isometric_asia_game_map.png
7. australia_and_oceania_adventure_map.png
8. conveyor_chef_world_map_logo.png
9. glossy_blue_game_back_button.png
10. glossy_blue_gear_settings_icon.png
11. glossy_blue_back_arrow_button.png
12. glossy_blue_right_arrow_button.png
13. glossy_chef_map_pin_icon.png
14. glossy_blue_map_pin_lock_icon.png
15. golden_magical_energy_burst.png
16. glossy_chef_s_game_ui_banner.png
17. locked_culinary_chapter_card.png
18. glossy_blue_game_ui_panel.png
19. drag_to_explore_game_button.png
20. ornate_golden_blue_compass_rose.png

Note: the South America file generated in this chat is named:
`colourful_south_america_game_map.png`

## Build the scene

After placing the PNG files here:

1. Let Unity finish importing.
2. Run **Conveyor Chef -> World Map -> Rebuild Responsive World Map**.
3. Run **Conveyor Chef -> World Map -> Validate World Map**.

The builder creates:
- `Assets/Project Data/Game/Scenes/WorldMap.unity`
- responsive fixed-frame WorldMapRoot
- safe-area fitting
- scrollable/pannable continent map
- chapter cards
- locked/unlocked continent state
- current-continent highlight
- previous/next continent navigation
- back-to-menu through the dedicated loading scene
- settings panel
- press feedback on buttons
- drag hint
- build-settings registration

## Progression

Each continent currently owns 15 levels.

- Chapter 1: North America — levels 1-15
- Chapter 2: South America — levels 16-30
- Chapter 3: Europe — levels 31-45
- Chapter 4: Africa — levels 46-60
- Chapter 5: Asia — levels 61-75
- Chapter 6: Australia / Oceania — levels 76-90

North America is available by default.
Every later continent unlocks after all 15 levels of the previous continent are completed.

Country-map navigation will be connected in the next implementation phase.

## Editable scene workflow

After Unity recompiles the project, the editor bootstrap automatically creates:

`Assets/Project Data/Game/Scenes/WorldMap.unity`

This is a normal serialized Unity scene. Open it from the Project window and edit the Canvas, RectTransforms, continent positions, card positions, sizes, anchors and visual hierarchy directly.

Your manual scene edits are treated as authoritative. The automatic bootstrap will **not** overwrite an existing `WorldMap.unity`. Only run **Conveyor Chef -> World Map -> Rebuild Responsive World Map** when you intentionally want to regenerate the entire scene.

You can also use **Conveyor Chef -> World Map -> Create/Open Editable World Map** to create the scene if it is missing and open it immediately.
