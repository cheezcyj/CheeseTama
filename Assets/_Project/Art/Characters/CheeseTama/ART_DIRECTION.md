# CheeseTama Character Art Direction

## Goal

CheeseTama should read as a soft 2.5D cheese pudding creature, not a raw Unity primitive.
The first impression must be round, warm, gentle, and slightly bouncy.

## Shape

- Body: rounded blob between egg, jelly, and cheese pudding.
- Proportion: wider after hatching, taller while still an egg.
- Surface: smooth, soft, and milk-glossy.
- Face: large simple eyes, small mouth, cheeks, and minimal cheese holes.
- Shadow: soft oval under the body.

## Color

- Body: butter yellow and cream yellow.
- Highlight: milk white.
- Face: deep coffee brown.
- Cheeks: warm peach.
- Shadow: soft warm brown.

## Required Rig

```text
CheeseTamaRoot
├─ ModelRoot
│  └─ VisualRoot
│     ├─ Body
│     ├─ FaceAnchor
│     ├─ Highlight
│     ├─ SoftShadow
│     └─ VFXRoot
```

## Expressions

- idle
- happy
- hungry
- sleepy
- upset
- surprised
- sick

## Motion

- Idle breathing should be visible but subtle.
- Care reactions should be a small squash/stretch pop.
- Hatch reactions can be larger, but should still feel soft.
- Avoid extreme hops that make the character feel like a ball.

## Avoid

- Cube, hard edge, plastic toy, realistic cheese block, Minecraft-like pet.
- Tiny eyes or a stiff centered expression.
- Rough cheese texture on the base form.
