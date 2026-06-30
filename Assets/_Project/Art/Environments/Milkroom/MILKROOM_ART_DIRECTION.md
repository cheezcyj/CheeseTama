# CheeseTama Milkroom Art Direction

## Goal

The Milkroom should feel like a cozy 2.5D cartoon room where CheeseTama lives: soft milk-white light, warm wood, visible milk bottles, a central rug, and gentle seasonal/time-of-day variation.

## Runtime Layer Structure

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
- Armchair/cushion, dresser, table lamp, plant, chalkboard.
- Small foreground milk drops for depth.

## Theme IDs

- `milkroom_morning`: warm cream/butter daylight.
- `milkroom_evening`: honey-orange sunset.
- `milkroom_night`: navy/lavender moonlight.
- `milkroom_rainy`: blue-gray rainy indoor warmth.

## Avoid

- Empty default Unity room.
- Cold gray placeholder look.
- Horror or dungeon lighting.
- Realistic food props that make CheeseTama read as edible.
- Hidden late-game theme labels or conditions in the public UI.
