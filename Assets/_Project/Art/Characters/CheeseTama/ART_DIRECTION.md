# CheeseTama Character Art Direction

## Goal

CheeseTama is a full 3D stylized toon character: a warm yellow cheese creature with a soft pudding body, glossy milk highlights, orange cheese marks, big sparkling eyes, peach cheeks, tiny limbs, and gentle squash/stretch motion.

## Core Silhouette

- Lv.1: taller cheese egg, pale yellow, glossy, small cheese spots.
- Lv.10: hatchling with visible face and small body.
- Lv.15: soft CheeseTama with a small curled top.
- Lv.20: wider grown body with arms and feet.
- Lv.28: mature body with a stronger pudding shape and up to five visible base cheese marks.
- Lv.33: final form stays soft and round; crown is reserved for celebration/cosmetic moments, not the default base silhouette.

## Shape Details

- Body: rounded blob between cheese pudding and jelly.
- Surface: smooth, glossy, and soft, never rough realistic cheese.
- Face: large dark brown eyes with white sparkle dots.
- Mouth: small cute "w" smile or small open mouth for surprise/hunger.
- Cheeks: peach-pink oval blush.
- Holes: orange oval patches, asymmetric like the concept art. Base CheeseTama shows 2-5 visible holes only; 6-7 hole layouts are reserved for later variants such as Emmental.
- Limbs: tiny soft arms and oval feet, secondary to the body silhouette.
- Top curl: appears from the soft/grown stage.
- Shadow: soft oval under the body.

## Palette

- Body: butter yellow and cream yellow.
- Holes: warm orange.
- Highlight: milk white.
- Face: deep coffee brown.
- Cheeks: peach pink.
- Crown: honey gold.
- Shadow: soft warm brown.

## Runtime Rig

```text
CheeseTamaRoot
|-- ModelRoot
|   `-- VisualRoot
|       |-- ToonOutline
|       |-- Body
|       |-- FaceAnchor
|       |-- Left/Right Soft Arm
|       |-- Left/Right Little Foot
|       |-- Top Curl
|       |-- CheeseMarks
|       |   |-- Cheese Hole 1..7
|       |   `-- Cheese Speckle 1..6
|       |-- Large/Small Milk Highlight
|       |-- SoftShadow
|       |-- Crown parts
|       `-- VFXRoot
```

## Rendering

- Use active-pipeline-safe toon material profiles until dedicated Shader Graph assets are built. Do not force URP shaders when no URP pipeline asset is assigned.
- Use warm rim/outline geometry to prevent the character from reading as a raw sphere primitive.
- Keep the camera front-facing and stable so the 3D model reads like a clean 2D mascot illustration.
- Prefer grouped 3D mesh replacement later: Body, FaceAnchor, CheeseMarks, Highlights, Shadow, VFXRoot.

## Expressions

- idle
- happy
- hungry
- sleepy
- upset
- surprised
- sad
- sick
- sparkle

## Motion

- Idle breathing should be visible but subtle.
- Care reactions should be a small squash/stretch pop.
- Hatch reactions can be larger, but should still feel soft.
- Sparkle/crown cues should read as a temporary celebration, not a permanent base-form requirement.
- Avoid extreme hops that make the character feel like a ball.

## Avoid

- Cube body, hard edges, hard plastic toy, realistic cheese block, Minecraft-like pet.
- Raw unstyled sphere primitive without toon material, outline, face rig, or highlights.
- Tiny eyes, stiff centered expression, or generic low-poly mascot proportions.
- Cream cap or milk belly patch motifs; the concept uses yellow cheese body, orange holes, and white highlights.
