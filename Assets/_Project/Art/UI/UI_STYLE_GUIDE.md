# CheeseTama UI Style Guide

## Layout Rule

The Milkroom first screen must separate direct care actions from system controls.

Bottom action bar:

```text
Milk / Blend / Snack / Play / Clean / Sleep
```

Top menu:

```text
Collection / Settings
```

System controls:

```text
Settings > Data Management > Save / Load / Reset
```

Debug controls:

```text
Debug scene or F12 Dev Panel in Editor/Development builds only
```

## Palette

| Token | Hex | Use |
|---|---|---|
| Milk White | `#FFF8E9` | background, cards |
| Cream Beige | `#F3E3C5` | panels |
| Butter Yellow | `#F6C75A` | primary buttons |
| Honey Gold | `#F2A93B` | highlight, reward |
| Mint Blue | `#9EDFD1` | clean/supportive states |
| Coffee Brown | `#7B5438` | text and face details |
| Danger Red | `#D85A4A` | destructive actions |

## Main Screen

```text
TopStatusBar
MainMilkroomArea
StatBar
BottomActionBar
SettingsModal
ConfirmResetDialog
DevPanel
```

## UX Rules

- Do not expose hidden late-game slots, counts, categories, or conditions.
- Keep save/load/reset away from the bottom action bar.
- Keep Collection in the top menu, not in the bottom care action bar.
- Reset must require an explicit confirmation input.
- Keep the default view focused on CheeseTama and care actions.
