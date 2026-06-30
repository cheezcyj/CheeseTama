# CheeseTama Game Design Brief

This internal brief summarizes the implementation direction from the v1.8 planning document.

## Core Direction

CheeseTama is a PC-first cozy creature-raising simulation. The player checks in one to three times per day, stays in the Milkroom for roughly 10 to 30 minutes, cares for a CheeseTama egg, feeds milk-based items, manages status values, and gradually unlocks growth, collection, and room systems.

## MVP Scope

- Boot and Milkroom scene flow.
- CheeseTama egg creation and status management.
- Milk feeding with effects and light penalty hooks.
- Local JSON save/load.
- Milkroom stay-time tracking with light session rewards.
- Milk Coins and Milk Drops as early economy rewards.
- Level growth and Lv.10 hatch flag.
- Basic collection registration for milk, growth, and events.
- One prototype minigame loop: catching Milk Drops.
- Internal-only hidden collection data rules.

## Unity Setup Notes

- Open the project from `C:\Users\user\Documents\GitHub\CheeseTama`.
- Use the Unity menu item `CheeseTama > Build Starter Scenes` to populate and save the starter scenes.
- `Milkroom` includes a placeholder CheeseTama egg, status panel, and basic action buttons.
- Runtime bootstrapping also creates missing core systems when entering Play Mode.

## Implementation Rules

- Keep public-facing UI cozy and simple.
- Do not reveal hidden collection slots, names, rarity, category, counts, or conditions before unlock.
- Even after unlock, do not expose exact growth conditions in user-facing UI.
- Avoid permanent punishment from feeding penalties.
- Prefer PC mouse-first interactions while keeping input systems portable.
