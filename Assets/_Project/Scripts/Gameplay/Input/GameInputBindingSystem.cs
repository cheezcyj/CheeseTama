using System;
using System.Collections.Generic;
using CheeseTama.Core;
using CheeseTama.Save;
using UnityEngine;

namespace CheeseTama.Gameplay.Input
{
    public static class GameInputActionIds
    {
        public const string Care1 = "care_1";
        public const string Care2 = "care_2";
        public const string Care3 = "care_3";
        public const string Care4 = "care_4";
        public const string Care5 = "care_5";
        public const string Care6 = "care_6";
        public const string Collection = "open_collection";
        public const string Decorate = "open_decorate";
        public const string Cancel = "cancel";
    }

    public sealed class GameInputActionDefinition
    {
        public readonly string id;
        public readonly string displayName;
        public readonly KeyCode defaultPrimary;
        public readonly KeyCode defaultSecondary;

        public GameInputActionDefinition(
            string id,
            string displayName,
            KeyCode defaultPrimary,
            KeyCode defaultSecondary = KeyCode.None)
        {
            this.id = id;
            this.displayName = displayName;
            this.defaultPrimary = defaultPrimary;
            this.defaultSecondary = defaultSecondary;
        }
    }

    public static class GameInputBindingSystem
    {
        private static readonly GameInputActionDefinition[] Definitions =
        {
            new GameInputActionDefinition(GameInputActionIds.Care1, "우유", KeyCode.Alpha1, KeyCode.Keypad1),
            new GameInputActionDefinition(GameInputActionIds.Care2, "요리", KeyCode.Alpha2, KeyCode.Keypad2),
            new GameInputActionDefinition(GameInputActionIds.Care3, "간식", KeyCode.Alpha3, KeyCode.Keypad3),
            new GameInputActionDefinition(GameInputActionIds.Care4, "놀이", KeyCode.Alpha4, KeyCode.Keypad4),
            new GameInputActionDefinition(GameInputActionIds.Care5, "청소", KeyCode.Alpha5, KeyCode.Keypad5),
            new GameInputActionDefinition(GameInputActionIds.Care6, "휴식", KeyCode.Alpha6, KeyCode.Keypad6),
            new GameInputActionDefinition(GameInputActionIds.Collection, "도감", KeyCode.C),
            new GameInputActionDefinition(GameInputActionIds.Decorate, "꾸미기", KeyCode.D),
            new GameInputActionDefinition(GameInputActionIds.Cancel, "닫기", KeyCode.Escape)
        };

        public static IReadOnlyList<GameInputActionDefinition> All => Definitions;

        public static bool EnsureDefaults(GameInputBindingSaveData state)
        {
            if (state == null)
            {
                return false;
            }

            var changed = state.EnsureRuntimeDefaults();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var index = state.bindings.Count - 1; index >= 0; index -= 1)
            {
                var entry = state.bindings[index];
                var definition = FindDefinition(entry.actionId);
                if (definition == null || !seen.Add(entry.actionId))
                {
                    state.bindings.RemoveAt(index);
                    changed = true;
                    continue;
                }

                if (!TryParseBindableKey(entry.primaryKey, out _))
                {
                    entry.primaryKey = definition.defaultPrimary.ToString();
                    changed = true;
                }

                if (definition.defaultSecondary == KeyCode.None)
                {
                    if (!string.IsNullOrEmpty(entry.secondaryKey))
                    {
                        entry.secondaryKey = string.Empty;
                        changed = true;
                    }
                }
                else if (!TryParseBindableKey(entry.secondaryKey, out _))
                {
                    entry.secondaryKey = definition.defaultSecondary.ToString();
                    changed = true;
                }
            }

            foreach (var definition in Definitions)
            {
                if (FindEntry(state, definition.id) != null)
                {
                    continue;
                }

                state.bindings.Add(CreateDefaultEntry(definition));
                changed = true;
            }

            var occupiedKeys = new HashSet<KeyCode>();
            foreach (var definition in Definitions)
            {
                var entry = FindEntry(state, definition.id);
                if (!TryParseBindableKey(entry.primaryKey, out var primary)
                    || occupiedKeys.Contains(primary))
                {
                    primary = FindAvailablePrimaryKey(definition, occupiedKeys);
                    entry.primaryKey = primary.ToString();
                    changed = true;
                }

                occupiedKeys.Add(primary);
                if (TryParseBindableKey(entry.secondaryKey, out var secondary))
                {
                    if (occupiedKeys.Contains(secondary))
                    {
                        entry.secondaryKey = string.Empty;
                        changed = true;
                    }
                    else
                    {
                        occupiedKeys.Add(secondary);
                    }
                }
            }

            // Keep the serialized list in the same stable order as the public action catalog.
            // This makes repairs of partial or duplicated save data deterministic across reloads.
            for (var targetIndex = 0; targetIndex < Definitions.Length; targetIndex += 1)
            {
                var targetEntry = FindEntry(state, Definitions[targetIndex].id);
                var currentIndex = state.bindings.IndexOf(targetEntry);
                if (currentIndex == targetIndex)
                {
                    continue;
                }

                state.bindings.RemoveAt(currentIndex);
                state.bindings.Insert(targetIndex, targetEntry);
                changed = true;
            }

            return changed;
        }

        public static bool TryRebind(
            GameInputBindingSaveData state,
            string actionId,
            KeyCode newPrimary,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            if (state == null || FindDefinition(actionId) == null)
            {
                errorMessage = "바꿀 조작을 찾지 못했습니다.";
                return false;
            }

            if (!IsBindableKey(newPrimary) || IsReservedNavigationKey(newPrimary))
            {
                errorMessage = "이 키는 기본 확인·취소 조작을 위해 남겨 두었습니다.";
                return false;
            }

            EnsureDefaults(state);
            foreach (var entry in state.bindings)
            {
                if (entry == null || string.Equals(entry.actionId, actionId, StringComparison.Ordinal))
                {
                    continue;
                }

                if ((TryParseBindableKey(entry.primaryKey, out var primary) && primary == newPrimary)
                    || (TryParseBindableKey(entry.secondaryKey, out var secondary) && secondary == newPrimary))
                {
                    var occupied = FindDefinition(entry.actionId)?.displayName ?? entry.actionId;
                    errorMessage = $"{FormatKey(newPrimary)} 키는 이미 '{occupied}'에 사용 중입니다.";
                    return false;
                }
            }

            var target = FindEntry(state, actionId);
            target.primaryKey = newPrimary.ToString();
            return true;
        }

        public static bool ResetAction(GameInputBindingSaveData state, string actionId)
        {
            var definition = FindDefinition(actionId);
            if (state == null || definition == null)
            {
                return false;
            }

            var replacement = CreateDefaultEntry(definition);
            // A rejected reset is transactional: detect default-key conflicts before repairing or
            // otherwise mutating the supplied save object.
            if (HasResetConflict(state, actionId, replacement))
            {
                return false;
            }

            EnsureDefaults(state);
            var entry = FindEntry(state, actionId);
            foreach (var other in state.bindings)
            {
                if (other == null || ReferenceEquals(other, entry))
                {
                    continue;
                }

                if (UsesSerializedKey(other, replacement.primaryKey)
                    || (!string.IsNullOrEmpty(replacement.secondaryKey)
                        && UsesSerializedKey(other, replacement.secondaryKey)))
                {
                    return false;
                }
            }

            var changed = !string.Equals(entry.primaryKey, replacement.primaryKey, StringComparison.Ordinal)
                || !string.Equals(entry.secondaryKey, replacement.secondaryKey, StringComparison.Ordinal);
            entry.primaryKey = replacement.primaryKey;
            entry.secondaryKey = replacement.secondaryKey;
            return changed;
        }

        public static bool ResetAll(GameInputBindingSaveData state)
        {
            if (state == null)
            {
                return false;
            }

            state.bindings.Clear();
            return EnsureDefaults(state);
        }

        public static bool TryResolve(
            GameInputBindingSaveData state,
            string actionId,
            out KeyCode primary,
            out KeyCode secondary)
        {
            primary = KeyCode.None;
            secondary = KeyCode.None;
            var definition = FindDefinition(actionId);
            if (definition == null)
            {
                return false;
            }

            if (state != null)
            {
                EnsureDefaults(state);
                var entry = FindEntry(state, actionId);
                if (entry != null && TryParseBindableKey(entry.primaryKey, out primary))
                {
                    TryParseBindableKey(entry.secondaryKey, out secondary);
                    return true;
                }
            }

            primary = definition.defaultPrimary;
            secondary = definition.defaultSecondary;
            return true;
        }

        public static string FormatBinding(GameInputBindingSaveData state, string actionId)
        {
            if (!TryResolve(state, actionId, out var primary, out var secondary))
            {
                return "-";
            }

            return secondary == KeyCode.None
                ? FormatKey(primary)
                : $"{FormatKey(primary)} / {FormatKey(secondary)}";
        }

        public static string FormatKey(KeyCode key)
        {
            return key switch
            {
                KeyCode.Alpha1 => "1",
                KeyCode.Alpha2 => "2",
                KeyCode.Alpha3 => "3",
                KeyCode.Alpha4 => "4",
                KeyCode.Alpha5 => "5",
                KeyCode.Alpha6 => "6",
                KeyCode.Escape => "Esc",
                KeyCode.Return => "Enter",
                KeyCode.KeypadEnter => "Num Enter",
                _ => key.ToString()
            };
        }

        public static GameInputActionDefinition FindDefinition(string actionId)
        {
            if (string.IsNullOrWhiteSpace(actionId))
            {
                return null;
            }

            foreach (var definition in Definitions)
            {
                if (string.Equals(definition.id, actionId, StringComparison.Ordinal))
                {
                    return definition;
                }
            }

            return null;
        }

        public static bool IsBindableKey(KeyCode key)
        {
            if (key == KeyCode.None || key == KeyCode.F12)
            {
                return false;
            }

            var name = key.ToString();
            return !name.StartsWith("Mouse", StringComparison.Ordinal)
                && !name.StartsWith("Joystick", StringComparison.Ordinal);
        }

        private static bool IsReservedNavigationKey(KeyCode key)
        {
            return key == KeyCode.Escape
                || key == KeyCode.Return
                || key == KeyCode.KeypadEnter
                || key == KeyCode.Tab;
        }

        private static bool TryParseBindableKey(string serialized, out KeyCode key)
        {
            return Enum.TryParse(serialized, out key) && IsBindableKey(key);
        }

        private static GameInputBindingSaveEntry FindEntry(GameInputBindingSaveData state, string actionId)
        {
            if (state?.bindings == null)
            {
                return null;
            }

            foreach (var entry in state.bindings)
            {
                if (entry != null && string.Equals(entry.actionId, actionId, StringComparison.Ordinal))
                {
                    return entry;
                }
            }

            return null;
        }

        private static GameInputBindingSaveEntry CreateDefaultEntry(GameInputActionDefinition definition)
        {
            return new GameInputBindingSaveEntry
            {
                actionId = definition.id,
                primaryKey = definition.defaultPrimary.ToString(),
                secondaryKey = definition.defaultSecondary == KeyCode.None
                    ? string.Empty
                    : definition.defaultSecondary.ToString()
            };
        }

        private static bool UsesSerializedKey(GameInputBindingSaveEntry entry, string serializedKey)
        {
            return !string.IsNullOrEmpty(serializedKey)
                && (string.Equals(entry.primaryKey, serializedKey, StringComparison.Ordinal)
                    || string.Equals(entry.secondaryKey, serializedKey, StringComparison.Ordinal));
        }

        private static bool HasResetConflict(
            GameInputBindingSaveData state,
            string actionId,
            GameInputBindingSaveEntry replacement)
        {
            if (state.bindings == null)
            {
                return false;
            }

            TryParseBindableKey(replacement.primaryKey, out var replacementPrimary);
            TryParseBindableKey(replacement.secondaryKey, out var replacementSecondary);
            foreach (var other in state.bindings)
            {
                if (other == null
                    || string.Equals((other.actionId ?? string.Empty).Trim(), actionId, StringComparison.Ordinal))
                {
                    continue;
                }

                if ((TryParseBindableKey(other.primaryKey, out var primary)
                        && (primary == replacementPrimary
                            || (replacementSecondary != KeyCode.None && primary == replacementSecondary)))
                    || (TryParseBindableKey(other.secondaryKey, out var secondary)
                        && (secondary == replacementPrimary
                            || (replacementSecondary != KeyCode.None && secondary == replacementSecondary))))
                {
                    return true;
                }
            }

            return false;
        }

        private static KeyCode FindAvailablePrimaryKey(
            GameInputActionDefinition definition,
            ISet<KeyCode> occupiedKeys)
        {
            if (!occupiedKeys.Contains(definition.defaultPrimary))
            {
                return definition.defaultPrimary;
            }

            for (var value = (int)KeyCode.A; value <= (int)KeyCode.Z; value += 1)
            {
                var key = (KeyCode)value;
                if (!occupiedKeys.Contains(key))
                {
                    return key;
                }
            }

            for (var value = (int)KeyCode.Alpha0; value <= (int)KeyCode.Alpha9; value += 1)
            {
                var key = (KeyCode)value;
                if (!occupiedKeys.Contains(key))
                {
                    return key;
                }
            }

            return KeyCode.Space;
        }
    }

    public static class GameInputRouter
    {
        public static bool GameplayInputSuppressed { get; set; }

        public static bool WasPressed(string actionId)
        {
            if (GameplayInputSuppressed)
            {
                return false;
            }

            GameInputBindingSaveData bindings = null;
            var manager = GameManager.Instance;
            if (manager?.CurrentSave?.settings != null)
            {
                bindings = manager.CurrentSave.settings.inputBindings;
            }

            return GameInputBindingSystem.TryResolve(bindings, actionId, out var primary, out var secondary)
                && (UnityEngine.Input.GetKeyDown(primary)
                    || (secondary != KeyCode.None && UnityEngine.Input.GetKeyDown(secondary)));
        }
    }
}
