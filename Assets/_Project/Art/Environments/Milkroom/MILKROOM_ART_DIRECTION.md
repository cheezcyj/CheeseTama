# CheeseTama Milkroom Art Direction

## Goal

The Milkroom is a 3D room staged with a front-facing camera and toon-rendered 2D readability: soft milk-white light, warm wood, visible milk bottles, a central rug, and gentle seasonal/time-of-day variation.

## Runtime Scene Structure

```text
MilkroomSceneRoot
|-- CameraRig
|-- Lighting
|-- Environment
|-- Character
|-- VFX
`-- UI
```

## Environment Layer Structure

```text
Milkroom Background
|-- BackgroundRoot
|-- MidgroundRoot
|-- PlayAreaRoot
|-- ForegroundRoot
`-- ThemeVFXRoot
```

- `BackgroundRoot`: wall, window, curtains, hanging lights.
- `MidgroundRoot`: floor, furniture, fridge, shelves, bottles, chalkboard.
- `PlayAreaRoot`: central rug and CheeseTama anchor.
- `ForegroundRoot`: soft milk drops and edge accents.
- `ThemeVFXRoot`: rain streaks, night stars, evening light beams.

## Base Props

- Big window with curtains.
- Central soft round rug.
- Milk bottle shelves and jars.
- Fridge with a small friendly face.
- Armchair/cushion, dresser, table lamp, plant, chalkboard, blending table.
- Small foreground milk drops for depth.

## Theme IDs

- `milkroom_morning`: warm cream/butter daylight.
- `milkroom_evening`: honey-orange sunset.
- `milkroom_night`: navy/lavender moonlight.
- `milkroom_rainy`: blue-gray rainy indoor warmth.

## Avoid

- Empty default Unity room.
- Cold gray placeholder look.
- Single flat background plane pretending to be a room.
- Horror or dungeon lighting.
- Realistic food props that make CheeseTama read as edible.
- Hidden late-game theme labels or conditions in the public UI.
