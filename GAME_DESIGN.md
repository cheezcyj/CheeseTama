# CheeseTama Game Design Brief

This internal brief summarizes the current CheeseTama implementation direction and the working rules Codex should follow while building the Unity prototype.

## Current Direction

CheeseTama is a PC-first cozy creature-raising simulation. The player visits the Milkroom one to three times per day, stays for roughly 10 to 30 minutes, cares for a small cheese creature, feeds milk-based items, manages status values, and slowly grows collections and room systems.

The first public impression should stay simple and cozy: a cute CheeseTama egg grows into a soft yellow cheese-pudding creature. Deeper collection systems can exist internally, but they must not be advertised or exposed before unlock.

## Core Loop

1. Enter the Milkroom.
2. Check CheeseTama's current state and message.
3. Use care actions: Milk, Blend, Snack, Play, Clean, Sleep.
4. Watch CheeseTama react with squash/stretch, face changes, and event cues.
5. Stay in the room to earn light session rewards.
6. Review discovered records in Collection.
7. Save automatically after important interactions, with manual data tools in Settings.

## Current MVP Scope

- Boot, Milkroom, Collection, and Debug scenes.
- Runtime bootstrapping for missing core systems.
- Local JSON save/load/reset.
- CheeseTama model with level, hatch state, stats, growth history, and unlock data.
- Status values: hunger, mood, cleanliness, sleepiness, health, affection, maturation.
- Basic Milk and Star Milk growth tracking.
- Lv.10 hatch flag and staged visual changes.
- Milkroom stay-time tracking and light session rewards.
- Early economy: Milk Coins, Milk Drops, Collection Fragments.
- Basic collection registration for milk, growth, events, evolution, and hidden-unlocked-only records.
- Prototype minigame loop: Milk Drop catching.
- Random care events and small event cues.

## Character Art Direction

CheeseTama is a full 3D stylized toon character in a fixed-camera diorama scene: warm toon material, simple silhouette, rim/outline support, and soft squash/stretch motion.

### Visual Identity

- Warm yellow cheese-pudding body.
- Soft rounded blob silhouette.
- Orange cheese holes and small speckles.
- Large dark brown eyes with white sparkle dots.
- Peach blush cheeks.
- Small cute mouth: idle smile, happy smile, sleepy line, worried frown, or small open mouth.
- Tiny soft arms and little oval feet after hatch.
- Top curl from the soft/grown stage onward.
- Honey-gold crown parts are reserved for celebration/cosmetic cues, not always-on base-form readability.
- Milk-white glossy highlights and a soft oval shadow.

### Growth Silhouette

- Lv.1: CheeseTama egg, taller and paler.
- Lv.10: Hatchling state.
- Lv.15: Soft CheeseTama with top curl.
- Lv.20: Grown CheeseTama with wider body and limbs.
- Lv.28: Mature CheeseTama with stronger pudding shape and more spots.
- Lv.33: Final form with crown.

### Runtime Placeholder Rule

Until final art assets exist, the runtime visual controller may build the character procedurally with grouped primitive shapes, but it must not read as a raw primitive. Toon material profiles, outline geometry, face parts, cheese marks, glossy highlights, soft shadow, and motion are required.

## Milkroom UI Layout

The Milkroom UI must separate direct care actions from system/navigation tools.

### Top Status Bar

- CheeseTama name.
- Level and progress.
- Session time and daily stay time.
- Early economy counts.
- Top menu buttons: 도감, 꾸미기, 설정.

### Main View

- CheeseTama is centered and readable.
- The character must not be hidden by panels during normal play.
- Important feedback appears in a dedicated bottom-center Message Bar.

### Stat Bar

Shows the five core condition values:

- Hunger
- Mood
- Cleanliness
- Sleepiness
- Health

### Bottom Action Bar

Only direct care actions belong here:

```text
우유 / 조합 / 간식 / 놀이 / 청소 / 수면
```

Do not put Collection, Decorate, Save, Load, Reset, Debug, or Wait +1h in the bottom care bar.
Use number keys 1-6 for the six direct care actions.

### Settings

Collection and Decorate open from the top menu. Settings also opens from the top menu and currently contains Data Management:

- Manual Save
- Load
- Reset

Reset must require typing `RESET` before the destructive button becomes interactable.

### Dev Panel

The F12 Dev Panel is Editor/Development-only and must open away from the Stat Bar. It currently contains:

- Wait +1h
- Debug Scene

Debug/test tools should stay out of the release-facing bottom action bar.

## Collection Rules

- Collection is opened from the top menu.
- Discovered milk, growth, evolution, and event records can be shown.
- Hidden late-game collection records must be hidden until actually unlocked.
- Do not show hidden slots, names, rarity, category, total counts, or unlock conditions before unlock.
- Even after unlock, do not expose exact growth conditions in user-facing UI.

## Unity Setup Notes

- The user's editable backup folder is `C:\Users\user\Desktop\CheeseTama`.
- The tracked GitHub folder is `C:\Users\user\Documents\GitHub\CheeseTama`.
- Open the Unity project from the folder the user is actively testing.
- Use `Assets > Refresh` after code changes.
- Use `CheeseTama > Build Starter Scenes` to regenerate starter scene objects.
- Open `Assets/_Project/Scenes/Milkroom.unity` and press Play.

## Quick Test Checklist

### Milkroom

1. Open Milkroom and press Play.
2. Confirm top menu shows 도감, 꾸미기, 설정.
3. Confirm bottom bar shows only Milk, Blend, Snack, Play, Clean, Sleep.
4. Click each care action and confirm stats/message/character reaction update.
5. Confirm Message Bar is readable.
6. Press F12 and confirm Dev Panel does not cover the Stat Bar.
7. Use Wait +1h from Dev Panel and confirm time progression updates.

### Settings

1. Open Settings.
2. Confirm Save, Load, Reset do not overlap text.
3. Confirm Save and Load update the status/message.
4. Open Reset.
5. Confirm Reset button is disabled until `RESET` is typed.

### Character

1. Confirm Lv.1 egg has face, highlights, and cheese spots.
2. Use Debug scene hatch tools to check post-hatch forms.
3. Confirm hungry, sleepy, upset, sick, surprised, and happy expressions are visually distinct.
4. Confirm reactions are soft and not excessively high.

### Collection

1. Perform milk/care actions.
2. Open Collection from the top menu.
3. Confirm discovered records appear.
4. Confirm hidden records do not expose unrevealed slots or counts.

## Build Verification

When Unity has generated the project files, use:

```powershell
dotnet restore CheeseTama.csproj
dotnet build CheeseTama.csproj --no-restore
```

The active development environment has been using .NET SDK 10 for C# compile checks.

## Implementation Rules

- Keep the first-screen experience cozy, readable, and centered on CheeseTama.
- Keep direct care actions separate from system/debug/navigation controls.
- Keep hidden late-game content fully hidden before unlock.
- Avoid permanent punishment from feeding penalties.
- Prefer PC mouse-first interactions while keeping future input portability in mind.
- Use Korean Git commit messages.
- After meaningful changes, copy the backup folder changes into the tracked GitHub folder, commit, and push.

## Next Priorities

- Replace procedural character pieces with final art/model assets when available.
- Build a proper Blend panel with recipe inputs and safe hidden-recipe handling.
- Add icons to care and top-menu buttons.
- Improve Collection card layout.
- Add proper settings tabs for sound, display, and controls.
- Add tutorial guidance for the first session.
- Add audio and VFX polish.

## License

This project uses the MIT License. See `LICENSE` for details.
