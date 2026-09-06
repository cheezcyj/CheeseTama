using System;
using System.Collections.Generic;
using CheeseTama.Save;

namespace CheeseTama.Gameplay.Growth
{
    public static class StarLineageTraitIds
    {
        public const string GentleRecovery = "lineage_gentle_recovery";
        public const string PatientMaturation = "lineage_patient_maturation";
        public const string CuriousBlending = "lineage_curious_blending";

        public static bool IsKnown(string traitId)
        {
            return string.Equals(traitId, GentleRecovery, StringComparison.Ordinal)
                || string.Equals(traitId, PatientMaturation, StringComparison.Ordinal)
                || string.Equals(traitId, CuriousBlending, StringComparison.Ordinal);
        }
    }

    public sealed class StarLineageTraitDefinition
    {
        internal StarLineageTraitDefinition(string id, string displayName, string detail)
        {
            Id = id ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string Detail { get; }
    }

    public sealed class StarLineageRecordSnapshot
    {
        internal StarLineageRecordSnapshot(StarGenerationRecordSaveData source)
        {
            RecordId = source?.recordId ?? string.Empty;
            NextGenerationNumber = Math.Max(0, source?.nextGenerationNumber ?? 0);
            TamaId = source?.tamaId ?? string.Empty;
            DisplayName = source?.displayName ?? string.Empty;
            FormId = source?.formId ?? string.Empty;
            EvolutionId = source?.evolutionId ?? string.Empty;
            PrimaryMilkId = source?.primaryMilkId ?? string.Empty;
            CareStyleId = source?.careStyleId ?? string.Empty;
            CompletedAtIso = source?.completedAtIso ?? string.Empty;
        }

        public string RecordId { get; }
        public int NextGenerationNumber { get; }
        public string TamaId { get; }
        public string DisplayName { get; }
        public string FormId { get; }
        public string EvolutionId { get; }
        public string PrimaryMilkId { get; }
        public string CareStyleId { get; }
        public string CompletedAtIso { get; }
    }

    public sealed class StarLineageSnapshot
    {
        internal StarLineageSnapshot(
            IReadOnlyList<StarLineageRecordSnapshot> records,
            IReadOnlyList<StarLineageTraitDefinition> traits,
            int pendingGenerationNumber,
            string activeTraitId,
            int activeTraitGenerationNumber)
        {
            Records = records ?? Array.Empty<StarLineageRecordSnapshot>();
            Traits = traits ?? Array.Empty<StarLineageTraitDefinition>();
            PendingGenerationNumber = Math.Max(0, pendingGenerationNumber);
            ActiveTraitId = activeTraitId ?? string.Empty;
            ActiveTraitGenerationNumber = Math.Max(0, activeTraitGenerationNumber);
        }

        public IReadOnlyList<StarLineageRecordSnapshot> Records { get; }
        public IReadOnlyList<StarLineageTraitDefinition> Traits { get; }
        public int PendingGenerationNumber { get; }
        public string ActiveTraitId { get; }
        public int ActiveTraitGenerationNumber { get; }
        public bool RequiresTraitSelection => PendingGenerationNumber > 0;
        public bool Visible => Records.Count > 0 || RequiresTraitSelection || !string.IsNullOrEmpty(ActiveTraitId);

        public StarLineageTraitDefinition ActiveTrait
        {
            get
            {
                for (var index = 0; index < Traits.Count; index += 1)
                {
                    if (string.Equals(Traits[index].Id, ActiveTraitId, StringComparison.Ordinal))
                    {
                        return Traits[index];
                    }
                }

                return null;
            }
        }
    }

    public enum StarLineageTransitionStatus
    {
        Applied,
        MissingState,
        MissingTama,
        InvalidGeneration,
        InvalidReceipt,
        DuplicateReceipt,
        CapacityFull
    }

    public readonly struct StarLineageTransitionResult
    {
        internal StarLineageTransitionResult(StarLineageTransitionStatus status, string recordId)
        {
            Status = status;
            RecordId = recordId ?? string.Empty;
        }

        public StarLineageTransitionStatus Status { get; }
        public string RecordId { get; }
        public bool Applied => Status == StarLineageTransitionStatus.Applied;
    }

    public enum StarLineageSelectionStatus
    {
        Applied,
        MissingState,
        NoPendingSelection,
        UnknownTrait,
        InvalidReceipt,
        DuplicateReceipt,
        CapacityFull
    }

    public readonly struct StarLineageSelectionResult
    {
        internal StarLineageSelectionResult(
            StarLineageSelectionStatus status,
            string traitId,
            int generationNumber)
        {
            Status = status;
            TraitId = traitId ?? string.Empty;
            GenerationNumber = Math.Max(0, generationNumber);
        }

        public StarLineageSelectionStatus Status { get; }
        public string TraitId { get; }
        public int GenerationNumber { get; }
        public bool Applied => Status == StarLineageSelectionStatus.Applied;
    }

    public sealed class StarLineageSystem
    {
        public const int GentleRecoveryBonusPercent = 10;
        public const int PatientMaturationBonusPercent = 10;
        public const double CuriousBlendingChanceBonus = 0.01d;

        private static readonly StarLineageTraitDefinition[] TraitDefinitions =
        {
            new StarLineageTraitDefinition(
                StarLineageTraitIds.GentleRecovery,
                "다정한 손길",
                "청소와 휴식의 회복 효과가 10% 높아져요."),
            new StarLineageTraitDefinition(
                StarLineageTraitIds.PatientMaturation,
                "느긋한 숙성",
                "최종형 숙성 진행도가 10% 더 쌓여요."),
            new StarLineageTraitDefinition(
                StarLineageTraitIds.CuriousBlending,
                "호기심 많은 향",
                "우유를 섞을 때 특별한 음식이 나올 확률이 1% 높아져요.")
        };

        public IReadOnlyList<StarLineageTraitDefinition> AllTraits => TraitDefinitions;

        public bool NormalizeState(StarLineageSaveData state)
        {
            if (state == null)
            {
                return false;
            }

            var changed = state.EnsureRuntimeDefaults();
            if (!string.IsNullOrEmpty(state.activeTraitId)
                && !StarLineageTraitIds.IsKnown(state.activeTraitId))
            {
                state.activeTraitId = string.Empty;
                state.activeTraitGenerationNumber = 0;
                changed = true;
            }

            if (state.pendingTraitGenerationNumber > 0
                && state.activeTraitGenerationNumber == state.pendingTraitGenerationNumber)
            {
                state.pendingTraitGenerationNumber = 0;
                changed = true;
            }

            return changed;
        }

        public StarLineageSnapshot BuildSnapshot(StarLineageSaveData state)
        {
            if (state == null)
            {
                return new StarLineageSnapshot(null, TraitDefinitions, 0, string.Empty, 0);
            }

            NormalizeState(state);
            var records = new List<StarLineageRecordSnapshot>(state.records.Count);
            for (var index = state.records.Count - 1; index >= 0; index -= 1)
            {
                records.Add(new StarLineageRecordSnapshot(state.records[index]));
            }

            return new StarLineageSnapshot(
                records,
                TraitDefinitions,
                state.pendingTraitGenerationNumber,
                state.activeTraitId,
                state.activeTraitGenerationNumber);
        }

        public StarLineageTransitionResult RecordTransition(
            StarLineageSaveData state,
            CheeseTamaModel outgoingTama,
            int nextGenerationNumber,
            DateTimeOffset completedAt,
            string receiptId)
        {
            if (state == null)
            {
                return TransitionFailure(StarLineageTransitionStatus.MissingState);
            }

            if (outgoingTama == null)
            {
                return TransitionFailure(StarLineageTransitionStatus.MissingTama);
            }

            NormalizeState(state);
            if (nextGenerationNumber <= 0)
            {
                return TransitionFailure(StarLineageTransitionStatus.InvalidGeneration);
            }

            var normalizedReceipt = StarLineageSaveData.NormalizeToken(receiptId);
            if (string.IsNullOrEmpty(normalizedReceipt))
            {
                return TransitionFailure(StarLineageTransitionStatus.InvalidReceipt);
            }

            if (state.HasReceipt(normalizedReceipt))
            {
                return TransitionFailure(StarLineageTransitionStatus.DuplicateReceipt);
            }

            outgoingTama.EnsureRuntimeDefaults();
            var recordId = $"star_lineage_transition_{nextGenerationNumber:D4}";
            while (state.records.Count >= StarLineageSaveData.MaximumRecords)
            {
                state.records.RemoveAt(0);
            }

            state.records.Add(new StarGenerationRecordSaveData
            {
                recordId = recordId,
                nextGenerationNumber = nextGenerationNumber,
                tamaId = outgoingTama.id ?? string.Empty,
                displayName = outgoingTama.name ?? string.Empty,
                formId = outgoingTama.form ?? string.Empty,
                evolutionId = outgoingTama.evolutionId ?? string.Empty,
                primaryMilkId = outgoingTama.growthHistory?.mostUsedMilkId ?? string.Empty,
                careStyleId = outgoingTama.growthHistory?.careStyle ?? string.Empty,
                completedAtIso = completedAt.ToString("O")
            });
            state.pendingTraitGenerationNumber = nextGenerationNumber;
            state.activeTraitId = string.Empty;
            state.activeTraitGenerationNumber = 0;
            state.RecordReceipt(normalizedReceipt);
            return new StarLineageTransitionResult(StarLineageTransitionStatus.Applied, recordId);
        }

        public StarLineageSelectionResult TrySelectTrait(
            StarLineageSaveData state,
            string traitId,
            string receiptId)
        {
            if (state == null)
            {
                return SelectionFailure(StarLineageSelectionStatus.MissingState);
            }

            NormalizeState(state);
            var generationNumber = state.pendingTraitGenerationNumber;
            if (generationNumber <= 0)
            {
                return SelectionFailure(StarLineageSelectionStatus.NoPendingSelection);
            }

            var normalizedTrait = StarLineageSaveData.NormalizeToken(traitId);
            if (!StarLineageTraitIds.IsKnown(normalizedTrait))
            {
                return SelectionFailure(StarLineageSelectionStatus.UnknownTrait);
            }

            var normalizedReceipt = StarLineageSaveData.NormalizeToken(receiptId);
            if (string.IsNullOrEmpty(normalizedReceipt))
            {
                return SelectionFailure(StarLineageSelectionStatus.InvalidReceipt);
            }

            if (state.HasReceipt(normalizedReceipt))
            {
                return SelectionFailure(StarLineageSelectionStatus.DuplicateReceipt);
            }

            state.activeTraitId = normalizedTrait;
            state.activeTraitGenerationNumber = generationNumber;
            state.pendingTraitGenerationNumber = 0;
            state.RecordReceipt(normalizedReceipt);
            return new StarLineageSelectionResult(
                StarLineageSelectionStatus.Applied,
                normalizedTrait,
                generationNumber);
        }

        public int GetRecoveryBonusPercent(StarLineageSaveData state)
        {
            return HasActiveTrait(state, StarLineageTraitIds.GentleRecovery)
                ? GentleRecoveryBonusPercent
                : 0;
        }

        public int ApplyFinalMaturationProgress(int baseProgress, StarLineageSaveData state)
        {
            var safe = Math.Max(0, baseProgress);
            if (!HasActiveTrait(state, StarLineageTraitIds.PatientMaturation))
            {
                return safe;
            }

            var bonus = Math.Max(1, safe * PatientMaturationBonusPercent / 100);
            return safe > int.MaxValue - bonus ? int.MaxValue : safe + bonus;
        }

        public double ApplyBlendingSpecialResultRoll(double roll, StarLineageSaveData state)
        {
            var safe = Math.Max(0d, Math.Min(1d, roll));
            return HasActiveTrait(state, StarLineageTraitIds.CuriousBlending)
                ? Math.Max(0d, safe - CuriousBlendingChanceBonus)
                : safe;
        }

        private bool HasActiveTrait(StarLineageSaveData state, string traitId)
        {
            if (state == null)
            {
                return false;
            }

            NormalizeState(state);
            return state.pendingTraitGenerationNumber == 0
                && state.activeTraitGenerationNumber > 0
                && string.Equals(state.activeTraitId, traitId, StringComparison.Ordinal);
        }

        private static StarLineageTransitionResult TransitionFailure(StarLineageTransitionStatus status)
        {
            return new StarLineageTransitionResult(status, string.Empty);
        }

        private static StarLineageSelectionResult SelectionFailure(StarLineageSelectionStatus status)
        {
            return new StarLineageSelectionResult(status, string.Empty, 0);
        }
    }
}
