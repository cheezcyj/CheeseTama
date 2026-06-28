# Changelog

## 0.1.0

- Created the initial Unity project structure.
- Added core gameplay, data, save, collection, and UI script skeletons.
- Added placeholder scenes for Boot, Milkroom, Collection, and Debug.
- Added runtime and editor starter scene builders for core managers and basic Milkroom UI.
- Added EventSystem setup and basic test button actions for the Milkroom scene.
- Rebind Milkroom UI and button actions at runtime when the scene already contains generated UI.
- Added basic care actions for feeding, playing, cleaning, resting, and visible progress feedback.
- Added visible last-save feedback plus Reload and Reset test buttons.
- Added CheeseTama placeholder color and pulse feedback for care actions.
- Restored the mesh renderer path and uses property blocks so the egg stays visible in Play Mode.
- Prefer the active GameManager instance and force scene rebinding after Play Mode scene load.
- Added per-button MilkroomCareButton components so care actions bind reliably in Play Mode.
- Made CheeseTama care feedback visibly hop, pulse, and flash after button actions.
- Reworked care feedback into a direct hop coroutine and added a visual-controller fallback lookup.
- Changed care feedback to an immediate realtime reaction and added logs for visual trigger diagnosis.
- Hold the care reaction at its peak first so Transform position changes are visible in Inspector.
- Move MaterialPropertyBlock creation out of the visual controller field initializer to satisfy Unity runtime rules.
- Tune CheeseTama care feedback into a shorter, gentler hop and remove temporary visual diagnostics.
- Add Hatch progress to the Milkroom status panel and celebrate the first hatch with a stronger visual reaction.
