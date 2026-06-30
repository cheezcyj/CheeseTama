# Codex Character/UI Task Notes

## Done In Current Pass

- Runtime CheeseTama rig creates Body, FaceAnchor, Highlight, SoftShadow, and VFXRoot.
- Hatched CheeseTama supports seven expression states.
- Idle breathing and softer squash/stretch reactions are handled in code.
- Milkroom bottom bar now focuses on six direct care actions only.
- Collection is moved to the top menu beside Settings.
- Save, Load, and Reset are moved into Settings > Data Management.
- Reset requires typing `RESET`.
- F12 Dev Panel is separated from the release-facing bottom bar.
- Wait +1h is available from the F12 Dev Panel for testing time progression.

## Next Character Work

- Replace primitive sphere placeholder with a custom blob mesh or imported model.
- Add real toon material assets and URP shader settings.
- Add face decal textures or sprites for cleaner expressions.
- Add special visual rules for star route and Emmental form later, without exposing hidden conditions early.

## Next UI Work

- Add real settings tabs for sound, display, and controls.
- Add icon assets to the six bottom care action buttons and top menu buttons.
- Add toast notifications for save, discovery, and reward feedback.
- Add a proper Blend panel with recipe inputs and safe hidden-recipe handling.
- Improve Collection screen card layout while keeping hidden content fully invisible until unlocked.
