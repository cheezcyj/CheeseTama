# Codex Character/UI Task Notes

## Done In Current Pass

- Runtime CheeseTama rig creates Body, FaceAnchor, CheeseMarks, Highlight, SoftShadow, and VFXRoot.
- Hatched CheeseTama supports idle, happy, hungry, sleepy, upset, surprised, sad, sick, and sparkle expression states.
- Idle breathing and softer squash/stretch reactions are handled in code.
- Base CheeseTama now limits visible cheese holes to the 2-5 range from the visual guide; 6-7 hole layouts are reserved for later variants.
- Milkroom bottom bar now focuses on six direct care actions only.
- Collection is moved to the top menu beside Settings.
- Save, Load, and Reset are moved into Settings > Data Management.
- Reset requires typing `RESET`.
- F12 Dev Panel is separated from the release-facing bottom bar.
- Wait +1h is available from the F12 Dev Panel for testing time progression.
- CheeseTama placeholder now follows the revised 3D toon direction: yellow pudding body, orange holes, glossy highlights, tiny limbs, top curl, toon material profiles, and warm outline geometry.
- Lv.33 crown parts are kept as celebration/cosmetic cues instead of always-on base-form parts.
- Status messages are shown in a dedicated bottom-center message bar.
- Dev Panel opens near the upper-right side so it does not cover the stat bar.
- Milkroom background now uses BackgroundRoot, MidgroundRoot, PlayAreaRoot, ForegroundRoot, and ThemeVFXRoot.
- Milkroom has controller scaffolding for morning, evening, night, and rainy palettes plus ambient VFX toggles.
- Milkroom now has a `MilkroomSceneRoot` hierarchy with CameraRig, Lighting, Environment, Character, VFX, and UI groups.
- Runtime renderers use URP Lit/Simple Lit toon material profiles instead of default whitebox materials.

## Next Character Work

- Replace primitive sphere placeholder with a custom blob mesh or imported model.
- Add real toon material assets and URP shader settings.
- Add face decal textures or sprites for cleaner expressions.
- Add special visual rules for star route and Emmental form later, without exposing hidden conditions early.
- Replace placeholder crown pieces with a cosmetic/evolution reward asset once the cosmetic route is defined.

## Next UI Work

- Add real settings tabs for sound, display, and controls.
- Add icon assets to the six bottom care action buttons and top menu buttons.
- Add toast notifications for save, discovery, and reward feedback.
- Add a proper Blend panel with recipe inputs and safe hidden-recipe handling.
- Improve Collection screen card layout while keeping hidden content fully invisible until unlocked.

## Next Milkroom Work

- Replace procedural primitive props with imported 3D furniture/room models using the same toon material profiles.
- Add a safe theme selection flow after unlock rules are finalized.
- Add weather/time transitions without exposing hidden route requirements.
