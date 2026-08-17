using System;
using CheeseTama.Gameplay.Milk;
using CheeseTama.Save;

namespace CheeseTama.Gameplay.Growth
{
    public enum StarEggSelectionStatus
    {
        Applied,
        AlreadySelected,
        MissingTama,
        StarEggLocked,
        JourneyAlreadyStarted
    }

    public enum StarEggGenerationEligibilityStatus
    {
        Eligible,
        MissingState,
        MissingTama,
        MissingUnlocks,
        StarRouteLocked,
        AlreadyStarEggGeneration,
        GenerationLimitReached
    }

    public enum StarEggGenerationStartStatus
    {
        Applied,
        MissingState,
        MissingTama,
        MissingUnlocks,
        StarRouteLocked,
        AlreadyStarEggGeneration,
        GenerationLimitReached,
        InvalidGenerationId
    }

    public readonly struct StarEggGenerationStartResult
    {
        public StarEggGenerationStartResult(
            StarEggGenerationStartStatus status,
            CheeseTamaModel nextTama,
            string previousTamaId,
            int generationNumber,
            string startedAtIso)
        {
            this.status = status;
            this.nextTama = nextTama;
            this.previousTamaId = previousTamaId ?? string.Empty;
            this.generationNumber = Math.Max(0, generationNumber);
            this.startedAtIso = startedAtIso ?? string.Empty;
        }

        public StarEggGenerationStartStatus status { get; }
        public CheeseTamaModel nextTama { get; }
        public string previousTamaId { get; }
        public int generationNumber { get; }
        public string startedAtIso { get; }
        public bool applied => status == StarEggGenerationStartStatus.Applied;
    }

    public enum EmmentalEvolutionAttemptStatus
    {
        Applied,
        AlreadyApplied,
        AlreadyEvolved,
        MissingState,
        MissingTama,
        InvalidReceipt,
        StarRouteLocked,
        NotStarEggOrigin,
        NotHatched,
        LevelTooLow,
        StarMilkSignalIncomplete,
        FantasySignalIncomplete,
        ConflictingSpecialEvolution
    }

    public readonly struct EmmentalEvolutionProgress
    {
        public EmmentalEvolutionProgress(
            bool visible,
            bool starRouteUnlocked,
            bool starEggOrigin,
            bool finalLevelReached,
            bool starMilkSignalReady,
            bool fantasySignalReady,
            bool evolved,
            int starMilkCareCount,
            int fantasyResonance,
            string indirectHint)
        {
            this.visible = visible;
            this.starRouteUnlocked = starRouteUnlocked;
            this.starEggOrigin = starEggOrigin;
            this.finalLevelReached = finalLevelReached;
            this.starMilkSignalReady = starMilkSignalReady;
            this.fantasySignalReady = fantasySignalReady;
            this.evolved = evolved;
            this.starMilkCareCount = Math.Max(0, starMilkCareCount);
            this.fantasyResonance = Math.Max(0, fantasyResonance);
            this.indirectHint = indirectHint ?? string.Empty;
        }

        public bool visible { get; }
        public bool starRouteUnlocked { get; }
        public bool starEggOrigin { get; }
        public bool finalLevelReached { get; }
        public bool starMilkSignalReady { get; }
        public bool fantasySignalReady { get; }
        public bool evolved { get; }
        public bool canEvolve => starRouteUnlocked
            && starEggOrigin
            && finalLevelReached
            && starMilkSignalReady
            && fantasySignalReady
            && !evolved;

        // These counts are intentionally domain-facing only. Visitor-facing UI should use indirectHint.
        public int starMilkCareCount { get; }
        public int fantasyResonance { get; }
        public string indirectHint { get; }
    }

    public readonly struct EmmentalEvolutionAttemptResult
    {
        public EmmentalEvolutionAttemptResult(
            EmmentalEvolutionAttemptStatus status,
            string receiptKey,
            NormalEvolutionResult evolution,
            string evolvedAtIso)
        {
            this.status = status;
            this.receiptKey = receiptKey ?? string.Empty;
            this.evolution = evolution;
            this.evolvedAtIso = evolvedAtIso ?? string.Empty;
        }

        public EmmentalEvolutionAttemptStatus status { get; }
        public string receiptKey { get; }
        public NormalEvolutionResult evolution { get; }
        public string evolvedAtIso { get; }
        public bool applied => status == EmmentalEvolutionAttemptStatus.Applied;
        public bool duplicateReceipt => status == EmmentalEvolutionAttemptStatus.AlreadyApplied;

        public EvolutionMilestoneData CreateMilestone(string occurrenceId, int level)
        {
            return evolution.HasEvolution
                ? new EvolutionMilestoneData(occurrenceId, evolution, level)
                : null;
        }
    }

    public sealed class StarEggEmmentalEvolutionSystem
    {
        public const string StarEggTypeId = "egg_star";
        public const string EmmentalEvolutionId = "emmental_cheesetama";
        public const int RequiredStarMilkCareCount = 7;
        public const int RequiredFantasyResonance = 7;
        public const int StarEggFantasyInfluenceMultiplier = 7;

        private static readonly NormalEvolutionProfile EmmentalProfile =
            new NormalEvolutionProfile(
                EmmentalEvolutionId,
                "에멘탈치즈타마",
                "일곱 개의 빛나는 구멍이 별자리처럼 이어진 별빛 루트의 치즈타마예요.",
                "반복된 별빛 돌봄과 설명하기 어려운 잔향이 하나의 무늬로 이어졌어요.",
                MilkCatalog.StarMilkId);

        public static NormalEvolutionProfile Profile => EmmentalProfile;

        public StarEggGenerationEligibilityStatus EvaluateNewGenerationEligibility(
            CheeseTamaModel tama,
            UnlockSaveData unlocks,
            StarLegacySaveData state)
        {
            if (state == null)
            {
                return StarEggGenerationEligibilityStatus.MissingState;
            }

            state.EnsureRuntimeDefaults();
            if (tama == null)
            {
                return StarEggGenerationEligibilityStatus.MissingTama;
            }

            if (unlocks == null)
            {
                return StarEggGenerationEligibilityStatus.MissingUnlocks;
            }

            if (!IsStarRouteUnlocked(unlocks, state))
            {
                return StarEggGenerationEligibilityStatus.StarRouteLocked;
            }

            if (IsStarEggOrigin(tama))
            {
                return StarEggGenerationEligibilityStatus.AlreadyStarEggGeneration;
            }

            return state.starEggGenerationCount >= int.MaxValue
                ? StarEggGenerationEligibilityStatus.GenerationLimitReached
                : StarEggGenerationEligibilityStatus.Eligible;
        }

        public StarEggGenerationStartResult TryBeginStarEggGeneration(
            CheeseTamaModel currentTama,
            UnlockSaveData unlocks,
            StarLegacySaveData state,
            string nextTamaId,
            DateTimeOffset startedAt)
        {
            var eligibility = EvaluateNewGenerationEligibility(currentTama, unlocks, state);
            if (eligibility != StarEggGenerationEligibilityStatus.Eligible)
            {
                return GenerationFailure(MapGenerationFailure(eligibility));
            }

            var normalizedTamaId = Normalize(nextTamaId);
            if (string.IsNullOrEmpty(normalizedTamaId)
                || string.Equals(normalizedTamaId, currentTama.id, StringComparison.Ordinal))
            {
                return GenerationFailure(StarEggGenerationStartStatus.InvalidGenerationId);
            }

            var startedAtIso = startedAt.ToString("O");
            var hasCustomName = currentTama.hasCustomName
                && !string.IsNullOrWhiteSpace(currentTama.name);
            var nextTama = new CheeseTamaModel
            {
                id = normalizedTamaId,
                name = hasCustomName ? currentTama.name : "CheeseTama",
                hasCustomName = hasCustomName,
                eggType = StarEggTypeId,
                isHatched = false,
                level = 1,
                levelProgress = 0,
                maxLevel = UnlockSystem.MaxLevel,
                form = "egg",
                evolutionId = string.Empty,
                createdAtIso = startedAtIso,
                lastSavedAtIso = startedAtIso,
                stats = CheeseTama.Gameplay.Stats.StatBlock.CreateDefault(),
                growthHistory = new GrowthHistory()
            };

            state.starRoutePermanentlyUnlocked = true;
            state.starEggGenerationCount += 1;
            state.currentGenerationTamaId = normalizedTamaId;
            state.currentGenerationStartedAtIso = startedAtIso;
            state.starMilkCareCount = 0;
            state.fantasyResonance = 0;
            state.emmentalEvolutionUnlocked = false;
            state.emmentalEvolutionAtIso = string.Empty;
            state.appliedEvolutionReceiptKeys.Clear();
            state.maturationCycle = new FinalMaturationCycleSaveData();
            SetRouteUnlockFlags(unlocks);

            return new StarEggGenerationStartResult(
                StarEggGenerationStartStatus.Applied,
                nextTama,
                currentTama.id,
                state.starEggGenerationCount,
                startedAtIso);
        }

        public bool ReconcileStarRouteUnlock(
            CheeseTamaModel tama,
            UnlockSaveData unlocks,
            StarLegacySaveData state,
            bool historicalUnlockEvidence = false)
        {
            if (unlocks == null || state == null)
            {
                return false;
            }

            var changed = state.EnsureRuntimeDefaults();
            var hasDurableEvidence = state.starRoutePermanentlyUnlocked
                || historicalUnlockEvidence
                || unlocks.starEggUnlocked
                || unlocks.starMilkUnlocked
                || unlocks.fantasyPowderEnabled
                || IsStarEggOrigin(tama)
                || state.emmentalEvolutionUnlocked;
            if (!hasDurableEvidence)
            {
                return changed;
            }

            if (!state.starRoutePermanentlyUnlocked)
            {
                state.starRoutePermanentlyUnlocked = true;
                changed = true;
            }

            if (IsStarEggOrigin(tama) && state.starEggGenerationCount == 0)
            {
                state.starEggGenerationCount = 1;
                state.currentGenerationTamaId = Normalize(tama.id);
                state.currentGenerationStartedAtIso = string.IsNullOrWhiteSpace(tama.createdAtIso)
                    ? string.Empty
                    : tama.createdAtIso;
                changed = true;
            }

            return SetRouteUnlockFlags(unlocks) || changed;
        }

        public bool MarkStarRoutePermanentlyUnlocked(
            UnlockSaveData unlocks,
            StarLegacySaveData state)
        {
            if (unlocks == null || state == null)
            {
                return false;
            }

            state.EnsureRuntimeDefaults();
            var changed = !state.starRoutePermanentlyUnlocked;
            state.starRoutePermanentlyUnlocked = true;
            return SetRouteUnlockFlags(unlocks) || changed;
        }

        public StarEggSelectionStatus TrySelectStarEgg(
            CheeseTamaModel tama,
            UnlockSaveData unlocks)
        {
            if (tama == null)
            {
                return StarEggSelectionStatus.MissingTama;
            }

            if (IsStarEggOrigin(tama))
            {
                return StarEggSelectionStatus.AlreadySelected;
            }

            if (unlocks == null || !unlocks.starEggUnlocked)
            {
                return StarEggSelectionStatus.StarEggLocked;
            }

            if (tama.isHatched
                || tama.level > 1
                || (!string.IsNullOrWhiteSpace(tama.form)
                    && !string.Equals(tama.form, "egg", StringComparison.Ordinal)))
            {
                return StarEggSelectionStatus.JourneyAlreadyStarted;
            }

            tama.eggType = StarEggTypeId;
            tama.form = "egg";
            tama.evolutionId = string.Empty;
            return StarEggSelectionStatus.Applied;
        }

        public EmmentalEvolutionProgress Evaluate(
            CheeseTamaModel tama,
            UnlockSaveData unlocks,
            StarLegacySaveData state)
        {
            state?.EnsureRuntimeDefaults();
            var starRouteUnlocked = IsStarRouteUnlocked(unlocks, state);
            var starEggOrigin = IsStarEggOrigin(tama);
            var finalLevelReached = tama != null
                && tama.level >= Math.Max(UnlockSystem.MaxLevel, tama.maxLevel);
            var evolved = IsEmmental(tama) || (state?.emmentalEvolutionUnlocked ?? false);
            var starMilkCareCount = state?.starMilkCareCount ?? 0;
            var fantasyResonance = state?.fantasyResonance ?? 0;
            var starMilkReady = starMilkCareCount >= RequiredStarMilkCareCount;
            var fantasyReady = fantasyResonance >= RequiredFantasyResonance;

            return new EmmentalEvolutionProgress(
                visible: starRouteUnlocked || evolved,
                starRouteUnlocked,
                starEggOrigin,
                finalLevelReached,
                starMilkReady,
                fantasyReady,
                evolved,
                starMilkCareCount,
                fantasyResonance,
                BuildIndirectHint(
                    starRouteUnlocked,
                    starEggOrigin,
                    finalLevelReached,
                    starMilkReady,
                    fantasyReady,
                    evolved));
        }

        public int RecordStarMilkCare(
            CheeseTamaModel tama,
            UnlockSaveData unlocks,
            StarLegacySaveData state,
            int requestedCount = 1)
        {
            if (state == null
                || unlocks == null
                || !IsStarRouteUnlocked(unlocks, state)
                || !IsStarEggOrigin(tama)
                || requestedCount <= 0
                || state.emmentalEvolutionUnlocked)
            {
                return 0;
            }

            state.EnsureRuntimeDefaults();
            var granted = Math.Min(
                requestedCount,
                StarLegacySaveData.MaximumSignalCount - state.starMilkCareCount);
            state.starMilkCareCount += granted;
            return granted;
        }

        public int RecordFantasyResonance(
            CheeseTamaModel tama,
            UnlockSaveData unlocks,
            StarLegacySaveData state,
            int baseInfluence = 1)
        {
            if (state == null
                || unlocks == null
                || (!unlocks.fantasyPowderEnabled
                    && !state.starRoutePermanentlyUnlocked)
                || tama == null
                || baseInfluence <= 0
                || state.emmentalEvolutionUnlocked)
            {
                return 0;
            }

            state.EnsureRuntimeDefaults();
            var multiplier = IsStarEggOrigin(tama)
                ? StarEggFantasyInfluenceMultiplier
                : 1;
            var requested = Math.Min(
                StarLegacySaveData.MaximumSignalCount,
                (long)baseInfluence * multiplier);
            var granted = (int)Math.Min(
                requested,
                StarLegacySaveData.MaximumSignalCount - state.fantasyResonance);
            state.fantasyResonance += granted;
            return granted;
        }

        public EmmentalEvolutionAttemptResult TryApplyEvolution(
            CheeseTamaModel tama,
            UnlockSaveData unlocks,
            StarLegacySaveData state,
            string receiptKey,
            DateTimeOffset evolvedAt)
        {
            var normalizedReceipt = Normalize(receiptKey);
            if (state == null)
            {
                return Failure(EmmentalEvolutionAttemptStatus.MissingState, normalizedReceipt);
            }

            state.EnsureRuntimeDefaults();
            if (string.IsNullOrEmpty(normalizedReceipt))
            {
                return Failure(EmmentalEvolutionAttemptStatus.InvalidReceipt, normalizedReceipt);
            }

            if (state.HasAppliedEvolutionReceipt(normalizedReceipt))
            {
                return Failure(EmmentalEvolutionAttemptStatus.AlreadyApplied, normalizedReceipt);
            }

            if (tama == null)
            {
                return Failure(EmmentalEvolutionAttemptStatus.MissingTama, normalizedReceipt);
            }

            if (IsEmmental(tama) || state.emmentalEvolutionUnlocked)
            {
                ReconcileAfterLoad(tama, state, evolvedAt);
                return new EmmentalEvolutionAttemptResult(
                    EmmentalEvolutionAttemptStatus.AlreadyEvolved,
                    normalizedReceipt,
                    BuildEvolutionResult(state),
                    state.emmentalEvolutionAtIso);
            }

            if (!IsStarRouteUnlocked(unlocks, state))
            {
                return Failure(EmmentalEvolutionAttemptStatus.StarRouteLocked, normalizedReceipt);
            }

            if (!IsStarEggOrigin(tama))
            {
                return Failure(EmmentalEvolutionAttemptStatus.NotStarEggOrigin, normalizedReceipt);
            }

            if (!tama.isHatched)
            {
                return Failure(EmmentalEvolutionAttemptStatus.NotHatched, normalizedReceipt);
            }

            if (tama.level < Math.Max(UnlockSystem.MaxLevel, tama.maxLevel))
            {
                return Failure(EmmentalEvolutionAttemptStatus.LevelTooLow, normalizedReceipt);
            }

            if (state.starMilkCareCount < RequiredStarMilkCareCount)
            {
                return Failure(
                    EmmentalEvolutionAttemptStatus.StarMilkSignalIncomplete,
                    normalizedReceipt);
            }

            if (state.fantasyResonance < RequiredFantasyResonance)
            {
                return Failure(
                    EmmentalEvolutionAttemptStatus.FantasySignalIncomplete,
                    normalizedReceipt);
            }

            if (!string.IsNullOrWhiteSpace(tama.evolutionId)
                && EvolutionSystem.FindNormalEvolution(tama.evolutionId) == null)
            {
                return Failure(
                    EmmentalEvolutionAttemptStatus.ConflictingSpecialEvolution,
                    normalizedReceipt);
            }

            var evolvedAtIso = evolvedAt.ToString("O");
            state.emmentalEvolutionUnlocked = true;
            state.emmentalEvolutionAtIso = evolvedAtIso;
            state.AddAppliedEvolutionReceipt(normalizedReceipt);
            tama.evolutionId = EmmentalEvolutionId;
            tama.form = EmmentalEvolutionId;
            var result = BuildEvolutionResult(state);
            return new EmmentalEvolutionAttemptResult(
                EmmentalEvolutionAttemptStatus.Applied,
                normalizedReceipt,
                result,
                evolvedAtIso);
        }

        public bool ReconcileAfterLoad(
            CheeseTamaModel tama,
            StarLegacySaveData state,
            DateTimeOffset observedAt)
        {
            if (tama == null || state == null || (!IsEmmental(tama) && !state.emmentalEvolutionUnlocked))
            {
                return false;
            }

            state.EnsureRuntimeDefaults();
            var changed = false;
            if (!state.emmentalEvolutionUnlocked)
            {
                state.emmentalEvolutionUnlocked = true;
                changed = true;
            }

            if (string.IsNullOrEmpty(state.emmentalEvolutionAtIso))
            {
                state.emmentalEvolutionAtIso = observedAt.ToString("O");
                changed = true;
            }

            if (!IsEmmental(tama))
            {
                tama.evolutionId = EmmentalEvolutionId;
                tama.form = EmmentalEvolutionId;
                changed = true;
            }

            return changed;
        }

        public static bool IsStarEggOrigin(CheeseTamaModel tama)
        {
            return tama != null
                && string.Equals(tama.eggType, StarEggTypeId, StringComparison.Ordinal);
        }

        public static bool IsEmmental(CheeseTamaModel tama)
        {
            return tama != null
                && string.Equals(tama.evolutionId, EmmentalEvolutionId, StringComparison.Ordinal);
        }

        public static bool IsStarRouteUnlocked(
            UnlockSaveData unlocks,
            StarLegacySaveData state)
        {
            return (state != null && state.starRoutePermanentlyUnlocked)
                || (unlocks != null && unlocks.starEggUnlocked && unlocks.starMilkUnlocked);
        }

        private static bool SetRouteUnlockFlags(UnlockSaveData unlocks)
        {
            if (unlocks == null)
            {
                return false;
            }

            var changed = !unlocks.starEggUnlocked
                || !unlocks.starMilkUnlocked
                || !unlocks.fantasyPowderEnabled;
            unlocks.starEggUnlocked = true;
            unlocks.starMilkUnlocked = true;
            unlocks.fantasyPowderEnabled = true;
            return changed;
        }

        private static StarEggGenerationStartStatus MapGenerationFailure(
            StarEggGenerationEligibilityStatus status)
        {
            switch (status)
            {
                case StarEggGenerationEligibilityStatus.MissingState:
                    return StarEggGenerationStartStatus.MissingState;
                case StarEggGenerationEligibilityStatus.MissingTama:
                    return StarEggGenerationStartStatus.MissingTama;
                case StarEggGenerationEligibilityStatus.MissingUnlocks:
                    return StarEggGenerationStartStatus.MissingUnlocks;
                case StarEggGenerationEligibilityStatus.StarRouteLocked:
                    return StarEggGenerationStartStatus.StarRouteLocked;
                case StarEggGenerationEligibilityStatus.AlreadyStarEggGeneration:
                    return StarEggGenerationStartStatus.AlreadyStarEggGeneration;
                case StarEggGenerationEligibilityStatus.GenerationLimitReached:
                    return StarEggGenerationStartStatus.GenerationLimitReached;
                default:
                    return StarEggGenerationStartStatus.StarRouteLocked;
            }
        }

        private static StarEggGenerationStartResult GenerationFailure(
            StarEggGenerationStartStatus status)
        {
            return new StarEggGenerationStartResult(status, null, string.Empty, 0, string.Empty);
        }

        private static NormalEvolutionResult BuildEvolutionResult(StarLegacySaveData state)
        {
            var score = state == null
                ? 0
                : Math.Min(
                    int.MaxValue,
                    (long)Math.Max(0, state.starMilkCareCount)
                        + Math.Max(0, state.fantasyResonance));
            return new NormalEvolutionResult(EmmentalProfile, (int)score);
        }

        private static EmmentalEvolutionAttemptResult Failure(
            EmmentalEvolutionAttemptStatus status,
            string receiptKey)
        {
            return new EmmentalEvolutionAttemptResult(
                status,
                receiptKey,
                default,
                string.Empty);
        }

        private static string BuildIndirectHint(
            bool starRouteUnlocked,
            bool starEggOrigin,
            bool finalLevelReached,
            bool starMilkReady,
            bool fantasyReady,
            bool evolved)
        {
            if (evolved)
            {
                return "일곱 개의 빈자리가 별자리처럼 이어져 있어요.";
            }

            if (!starRouteUnlocked)
            {
                return "아직 밀크룸 저편의 빛은 닿지 않아요.";
            }

            if (!starEggOrigin)
            {
                return "새로 열린 알의 빛이 조용히 다음 만남을 기다려요.";
            }

            if (!finalLevelReached)
            {
                return "별빛을 품은 알도 충분한 시간과 돌봄이 필요해요.";
            }

            if (!starMilkReady)
            {
                return "익숙한 별빛 우유가 몸속의 빈자리를 천천히 밝혀요.";
            }

            if (!fantasyReady)
            {
                return "희미한 잔향이 남았지만 아직 하나의 무늬가 되지는 않았어요.";
            }

            return "일곱 개의 빛이 서로를 찾았어요.";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
