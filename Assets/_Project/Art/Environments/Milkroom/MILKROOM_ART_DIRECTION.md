# CheeseTama Milkroom Art Direction

## Goal

The Milkroom is a full 3D stylized toon diorama: soft milk-white light, warm wood, visible milk bottles, a central rug, and a fixed Game View camera composition.

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

## Environment Group Structure

```text
Milkroom Background
|-- RoomShell
|-- WindowSet
|-- FridgeSet
|-- MilkShelfSet
|-- BlendingTableSet
|-- ChalkboardSet
|-- Rug
|-- CozyChair
|-- Lamps
|-- Props
`-- ThemeVFXRoot
```

- `RoomShell`: 3D back wall, side walls, floor, baseboards, ceiling beam.
- `WindowSet`: 3D window glass, frame, curtains, warm morning glow.
- `FridgeSet`, `MilkShelfSet`, `BlendingTableSet`, `ChalkboardSet`: grouped functional props.
- `Rug`: central CheeseTama anchor and soft rug geometry.
- `Props`: plants, memos, loose milk bottles, foreground milk drops.
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
- Forced URP material that renders magenta when URP is not assigned.
- Horror or dungeon lighting.
- Realistic food props that make CheeseTama read as edible.
- Hidden late-game theme labels or conditions in the public UI.
