using System;
using CheeseTama.Gameplay.NewGameSetup;

namespace CheeseTama.Save
{
    [Serializable]
    public sealed class InitialTemperamentSeedSaveData
    {
        public string seedKey = string.Empty;
        public string dominantTraitId = string.Empty;
        public int balance;
        public int activity;
        public int expressiveness;
        public int composure;
        public int focus;

        public bool EnsureRuntimeDefaults()
        {
            var changed = false;
            changed |= NormalizeString(ref seedKey);
            changed |= NormalizeString(ref dominantTraitId);
            changed |= ClampScore(ref balance);
            changed |= ClampScore(ref activity);
            changed |= ClampScore(ref expressiveness);
            changed |= ClampScore(ref composure);
            changed |= ClampScore(ref focus);
            return changed;
        }

        public bool HasAnyValue()
        {
            return !string.IsNullOrEmpty(seedKey)
                || !string.IsNullOrEmpty(dominantTraitId)
                || balance != 0
                || activity != 0
                || expressiveness != 0
                || composure != 0
                || focus != 0;
        }

        public bool HasSameValues(InitialTemperamentSeedSaveData other)
        {
            return other != null
                && string.Equals(seedKey, other.seedKey, StringComparison.Ordinal)
                && string.Equals(dominantTraitId, other.dominantTraitId, StringComparison.Ordinal)
                && balance == other.balance
                && activity == other.activity
                && expressiveness == other.expressiveness
                && composure == other.composure
                && focus == other.focus;
        }

        private static bool NormalizeString(ref string value)
        {
            if (value != null)
            {
                return false;
            }

            value = string.Empty;
            return true;
        }

        private static bool ClampScore(ref int value)
        {
            var clamped = Math.Max(0, Math.Min(100, value));
            if (clamped == value)
            {
                return false;
            }

            value = clamped;
            return true;
        }
    }

    [Serializable]
    public sealed class NewGameSetupSaveData
    {
        public const int CurrentSchemaVersion = 2;

        public int schemaVersion = CurrentSchemaVersion;
        public NewGameSetupStep currentStep = NewGameSetupStep.EggSelection;
        public string selectedEggId = string.Empty;
        public string selectedFirstMilkId = string.Empty;
        public InitialTemperamentSeedSaveData temperamentSeed = new InitialTemperamentSeedSaveData();
        public bool completed;
        public bool skipped;
        public bool legacySuppressed;
        // Completion outcome is committed by GameManager in the same durable save.
        public bool outcomeApplied;

        public static NewGameSetupSaveData CreateForNewPlayer()
        {
            return new NewGameSetupSaveData
            {
                currentStep = NewGameSetupStep.EggSelection
            };
        }

        public static NewGameSetupSaveData CreateCompletedForLegacySave()
        {
            return new NewGameSetupSaveData
            {
                currentStep = NewGameSetupStep.Complete,
                completed = true,
                legacySuppressed = true,
                outcomeApplied = true,
                temperamentSeed = NewGameSetupCatalog.CreateNeutralSeed(
                    NewGameSetupCatalog.LegacySeedKey)
            };
        }

        public bool EnsureRuntimeDefaults()
        {
            var changed = schemaVersion != CurrentSchemaVersion;
            schemaVersion = CurrentSchemaVersion;

            changed |= NormalizeString(ref selectedEggId);
            changed |= NormalizeString(ref selectedFirstMilkId);
            if (temperamentSeed == null)
            {
                temperamentSeed = new InitialTemperamentSeedSaveData();
                changed = true;
            }

            changed |= temperamentSeed.EnsureRuntimeDefaults();

            var hasValidEgg = NewGameSetupCatalog.TryGetEgg(selectedEggId, out _);
            var hasValidMilk = NewGameSetupCatalog.TryGetFirstMilk(selectedFirstMilkId, out _);

            if (legacySuppressed)
            {
                changed |= SetCompletionFlags(skippedValue: false);
                if (!outcomeApplied)
                {
                    outcomeApplied = true;
                    changed = true;
                }
                changed |= ReplaceSeed(NewGameSetupCatalog.CreateNeutralSeed(
                    NewGameSetupCatalog.LegacySeedKey));
                return changed;
            }

            if (completed || currentStep == NewGameSetupStep.Complete)
            {
                changed |= SetCompletionFlags(skipped);
                if (skipped)
                {
                    if (!outcomeApplied)
                    {
                        outcomeApplied = true;
                        changed = true;
                    }

                    changed |= ReplaceSeed(NewGameSetupCatalog.CreateNeutralSeed(
                        NewGameSetupCatalog.SkippedSeedKey));
                    return changed;
                }

                if (hasValidEgg
                    && hasValidMilk
                    && NewGameSetupCatalog.TryCreateTemperamentSeed(
                        selectedEggId,
                        selectedFirstMilkId,
                        out var expectedSeed))
                {
                    changed |= ReplaceSeed(expectedSeed);
                    return changed;
                }

                selectedEggId = string.Empty;
                selectedFirstMilkId = string.Empty;
                skipped = true;
                changed = true;
                changed |= ReplaceSeed(NewGameSetupCatalog.CreateNeutralSeed(
                    NewGameSetupCatalog.RecoveredSeedKey));
                return changed;
            }

            if (skipped)
            {
                skipped = false;
                changed = true;
            }

            if (outcomeApplied)
            {
                outcomeApplied = false;
                changed = true;
            }

            var stepValue = (int)currentStep;
            if (stepValue < (int)NewGameSetupStep.EggSelection
                || stepValue > (int)NewGameSetupStep.FirstMilkSelection)
            {
                currentStep = NewGameSetupStep.EggSelection;
                changed = true;
            }

            if (!hasValidEgg && !string.IsNullOrEmpty(selectedEggId))
            {
                selectedEggId = string.Empty;
                changed = true;
            }

            if (!hasValidMilk && !string.IsNullOrEmpty(selectedFirstMilkId))
            {
                selectedFirstMilkId = string.Empty;
                changed = true;
            }

            if (currentStep == NewGameSetupStep.FirstMilkSelection && !hasValidEgg)
            {
                currentStep = NewGameSetupStep.EggSelection;
                changed = true;
            }

            if (temperamentSeed.HasAnyValue())
            {
                temperamentSeed = new InitialTemperamentSeedSaveData();
                changed = true;
            }

            return changed;
        }

        private bool SetCompletionFlags(bool skippedValue)
        {
            var changed = currentStep != NewGameSetupStep.Complete
                || !completed
                || skipped != skippedValue;
            currentStep = NewGameSetupStep.Complete;
            completed = true;
            skipped = skippedValue;
            return changed;
        }

        private bool ReplaceSeed(InitialTemperamentSeedSaveData replacement)
        {
            if (temperamentSeed != null && temperamentSeed.HasSameValues(replacement))
            {
                return false;
            }

            temperamentSeed = replacement ?? new InitialTemperamentSeedSaveData();
            return true;
        }

        private static bool NormalizeString(ref string value)
        {
            if (value != null)
            {
                return false;
            }

            value = string.Empty;
            return true;
        }
    }
}
