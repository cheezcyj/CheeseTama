using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using CheeseTama.Gameplay.Events;
using CheeseTama.Save;
using UnityEngine;

namespace CheeseTama.Platform
{
    [Serializable]
    public sealed class SaveTransferEnvelope
    {
        public const int CurrentFormatVersion = 1;
        public const string Utf8Base64Encoding = "utf8-base64";

        public int formatVersion = CurrentFormatVersion;
        public string exportedUtcIso = string.Empty;
        public string saveSchemaVersion = string.Empty;
        public string contentEncoding = Utf8Base64Encoding;
        public string contentHash = string.Empty;
        public string content = string.Empty;
        public long revision;
        public long modifiedUtcTicks;
    }

    public enum SaveTransferValidationStatus
    {
        Valid,
        MissingData,
        EnvelopeTooLarge,
        InvalidEnvelope,
        UnsupportedEnvelopeVersion,
        UnsupportedContentEncoding,
        InvalidExportTimestamp,
        InvalidContent,
        ContentTooLarge,
        HashMismatch,
        UnsupportedSaveSchema,
        UnsafeSaveData
    }

    public sealed class SaveTransferPreview
    {
        public SaveTransferPreview(
            string tamaName,
            int level,
            int coins,
            string saveSchemaVersion,
            DateTimeOffset exportedUtc,
            string lastSavedAtIso)
        {
            TamaName = string.IsNullOrWhiteSpace(tamaName) ? "CheeseTama" : tamaName;
            Level = Math.Max(1, level);
            Coins = Math.Max(0, coins);
            SaveSchemaVersion = saveSchemaVersion ?? string.Empty;
            ExportedUtc = exportedUtc;
            LastSavedAtIso = lastSavedAtIso ?? string.Empty;
        }

        public string TamaName { get; }
        public int Level { get; }
        public int Coins { get; }
        public string SaveSchemaVersion { get; }
        public DateTimeOffset ExportedUtc { get; }
        public string LastSavedAtIso { get; }

        public string ToSummary()
        {
            return $"{TamaName} · 레벨 {Level} · 코인 {Coins:N0}\n"
                + $"저장 형식 {SaveSchemaVersion} · 내보낸 시각 {ExportedUtc.ToLocalTime():yyyy-MM-dd HH:mm}";
        }
    }

    public sealed class SaveTransferValidationResult
    {
        private SaveTransferValidationResult(
            SaveTransferValidationStatus status,
            string message,
            CloudSavePayload payload,
            SaveTransferPreview preview)
        {
            Status = status;
            Message = message ?? string.Empty;
            Payload = payload;
            Preview = preview;
        }

        public SaveTransferValidationStatus Status { get; }
        public string Message { get; }
        public CloudSavePayload Payload { get; }
        public SaveTransferPreview Preview { get; }
        public bool IsValid => Status == SaveTransferValidationStatus.Valid
            && Payload != null
            && Preview != null;

        public static SaveTransferValidationResult Valid(
            CloudSavePayload payload,
            SaveTransferPreview preview)
        {
            return new SaveTransferValidationResult(
                SaveTransferValidationStatus.Valid,
                string.Empty,
                payload,
                preview);
        }

        public static SaveTransferValidationResult Invalid(
            SaveTransferValidationStatus status,
            string message)
        {
            return new SaveTransferValidationResult(status, message, null, null);
        }
    }

    public static class SaveTransferCodec
    {
        public const int MaximumContentBytes = 8 * 1024 * 1024;
        public const int MaximumEnvelopeBytes = 12 * 1024 * 1024;
        public const int MaximumImportedTamaNameLength = 32;
        public const int MaximumImportedIdentifierLength = 128;
        public const int MaximumImportedReceiptKeyLength = 256;
        public const int MaximumImportedCurrency = 1_000_000_000;

        private const int MaximumImportedJsonStringLength = 4096;
        private const int MaximumImportedTitleLength = 128;
        private const int MaximumImportedQuoteLength = 2048;
        private const int MaximumImportedTamaLevel = 100;
        private const int MaximumImportedCounter = 1_000_000_000;
        private const int MaximumImportedDailyCounter = 1_000_000;
        private const int MaximumImportedCollectionEntries = 512;
        private const int MaximumImportedInventoryEntries = 256;
        private const int MaximumImportedClaimKeys = 2048;
        private const int MaximumImportedInputBindings = 64;
        private const int MaximumImportedNpcRelationships = 64;
        private const int MaximumImportedRandomEventHistory = 512;
        private const int MaximumImportedFirstDayTasks = 64;
        private const int MaximumImportedWeeklyObjectives = 64;
        private const int MaximumImportedJsonStructuralTokens = 131_072;
        private const int MaximumImportedJsonNestingDepth = 64;

        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

        public static bool TrySerialize(
            CheeseTamaSaveData saveData,
            DateTimeOffset exportedUtc,
            out string envelopeJson,
            out string errorMessage)
        {
            envelopeJson = string.Empty;
            errorMessage = string.Empty;
            if (saveData == null)
            {
                errorMessage = "내보낼 저장 데이터를 찾지 못했습니다.";
                return false;
            }

            try
            {
                var contentJson = JsonUtility.ToJson(saveData, true);
                var contentBytes = Encoding.UTF8.GetBytes(contentJson);
                if (contentBytes.Length > MaximumContentBytes)
                {
                    errorMessage = "저장 데이터가 백업 가능 용량을 초과했습니다.";
                    return false;
                }

                var modifiedUtc = ResolveModifiedUtc(saveData, exportedUtc);
                var revision = Math.Max(0L, modifiedUtc.UtcDateTime.Ticks);
                var envelope = new SaveTransferEnvelope
                {
                    formatVersion = SaveTransferEnvelope.CurrentFormatVersion,
                    exportedUtcIso = exportedUtc.UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
                    saveSchemaVersion = saveData.version ?? string.Empty,
                    contentEncoding = SaveTransferEnvelope.Utf8Base64Encoding,
                    contentHash = CloudSavePayload.ComputeContentHash(contentJson),
                    content = Convert.ToBase64String(contentBytes),
                    revision = revision,
                    modifiedUtcTicks = modifiedUtc.UtcDateTime.Ticks
                };
                envelopeJson = JsonUtility.ToJson(envelope, true);
                if (Encoding.UTF8.GetByteCount(envelopeJson) > MaximumEnvelopeBytes)
                {
                    envelopeJson = string.Empty;
                    errorMessage = "백업 파일이 허용 용량을 초과했습니다.";
                    return false;
                }

                return true;
            }
            catch (ArgumentException)
            {
                errorMessage = "저장 데이터를 백업 형식으로 변환하지 못했습니다.";
                return false;
            }
        }

        public static SaveTransferValidationResult Validate(string envelopeJson)
        {
            if (string.IsNullOrWhiteSpace(envelopeJson))
            {
                return Invalid(
                    SaveTransferValidationStatus.MissingData,
                    "가져올 백업 파일이 비어 있습니다.");
            }

            if (Encoding.UTF8.GetByteCount(envelopeJson) > MaximumEnvelopeBytes)
            {
                return Invalid(
                    SaveTransferValidationStatus.EnvelopeTooLarge,
                    "백업 파일이 허용 용량을 초과했습니다.");
            }

            var trimmedEnvelope = envelopeJson.Trim();
            if (!LooksLikeJsonObject(trimmedEnvelope))
            {
                return Invalid(
                    SaveTransferValidationStatus.InvalidEnvelope,
                    "CheeseTama 백업 파일 형식이 아닙니다.");
            }

            if (!HasSafeJsonStructure(
                    trimmedEnvelope,
                    MaximumImportedJsonStructuralTokens,
                    MaximumImportedJsonNestingDepth))
            {
                return Invalid(
                    SaveTransferValidationStatus.UnsafeSaveData,
                    "백업 파일의 JSON 구조가 허용 복잡도를 초과했거나 올바르지 않습니다.");
            }

            SaveTransferEnvelope envelope;
            try
            {
                envelope = JsonUtility.FromJson<SaveTransferEnvelope>(trimmedEnvelope);
            }
            catch (ArgumentException)
            {
                return Invalid(
                    SaveTransferValidationStatus.InvalidEnvelope,
                    "백업 파일의 JSON을 읽을 수 없습니다.");
            }

            if (envelope == null)
            {
                return Invalid(
                    SaveTransferValidationStatus.InvalidEnvelope,
                    "CheeseTama 백업 정보를 찾지 못했습니다.");
            }

            if (envelope.formatVersion != SaveTransferEnvelope.CurrentFormatVersion)
            {
                return Invalid(
                    SaveTransferValidationStatus.UnsupportedEnvelopeVersion,
                    "지원하지 않는 백업 파일 버전입니다.");
            }

            if (!string.Equals(
                    envelope.contentEncoding,
                    SaveTransferEnvelope.Utf8Base64Encoding,
                    StringComparison.Ordinal))
            {
                return Invalid(
                    SaveTransferValidationStatus.UnsupportedContentEncoding,
                    "지원하지 않는 백업 데이터 인코딩입니다.");
            }

            if (!DateTimeOffset.TryParse(
                    envelope.exportedUtcIso,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var exportedUtc)
                || envelope.modifiedUtcTicks <= 0L
                || envelope.revision < 0L)
            {
                return Invalid(
                    SaveTransferValidationStatus.InvalidExportTimestamp,
                    "백업 파일의 생성 시각 정보가 올바르지 않습니다.");
            }

            DateTimeOffset modifiedUtc;
            try
            {
                modifiedUtc = new DateTimeOffset(envelope.modifiedUtcTicks, TimeSpan.Zero);
            }
            catch (ArgumentOutOfRangeException)
            {
                return Invalid(
                    SaveTransferValidationStatus.InvalidExportTimestamp,
                    "백업 파일의 저장 시각 정보가 올바르지 않습니다.");
            }

            byte[] contentBytes;
            try
            {
                contentBytes = Convert.FromBase64String(envelope.content ?? string.Empty);
            }
            catch (FormatException)
            {
                return Invalid(
                    SaveTransferValidationStatus.InvalidContent,
                    "백업 파일의 저장 데이터가 손상되었습니다.");
            }

            if (contentBytes.Length == 0)
            {
                return Invalid(
                    SaveTransferValidationStatus.InvalidContent,
                    "백업 파일에 저장 데이터가 없습니다.");
            }

            if (contentBytes.Length > MaximumContentBytes)
            {
                return Invalid(
                    SaveTransferValidationStatus.ContentTooLarge,
                    "백업 안의 저장 데이터가 허용 용량을 초과했습니다.");
            }

            string contentJson;
            try
            {
                contentJson = StrictUtf8.GetString(contentBytes);
            }
            catch (DecoderFallbackException)
            {
                return Invalid(
                    SaveTransferValidationStatus.InvalidContent,
                    "백업 안의 저장 데이터가 UTF-8 형식이 아닙니다.");
            }

            if (!string.Equals(
                    envelope.contentHash,
                    CloudSavePayload.ComputeContentHash(contentJson),
                    StringComparison.OrdinalIgnoreCase))
            {
                return Invalid(
                    SaveTransferValidationStatus.HashMismatch,
                    "백업 파일의 무결성 검증에 실패했습니다.");
            }

            var trimmedContent = contentJson.Trim();
            if (!LooksLikeJsonObject(trimmedContent)
                || !HasSerializedField(trimmedContent, "version")
                || !HasSerializedField(trimmedContent, "cheeseTama"))
            {
                return Invalid(
                    SaveTransferValidationStatus.InvalidContent,
                    "백업 안에 필수 저장 항목이 없습니다.");
            }

            if (!HasSafeJsonStringTokens(trimmedContent, MaximumImportedJsonStringLength))
            {
                return Invalid(
                    SaveTransferValidationStatus.UnsafeSaveData,
                    "백업 안의 문자열이 허용 길이를 초과했거나 Unicode 형식이 올바르지 않습니다.");
            }

            if (!HasSafeJsonStructure(
                    trimmedContent,
                    MaximumImportedJsonStructuralTokens,
                    MaximumImportedJsonNestingDepth))
            {
                return Invalid(
                    SaveTransferValidationStatus.UnsafeSaveData,
                    "백업 안의 JSON 구조가 허용 복잡도를 초과했거나 올바르지 않습니다.");
            }

            CheeseTamaSaveData candidate;
            try
            {
                candidate = JsonUtility.FromJson<CheeseTamaSaveData>(trimmedContent);
            }
            catch (ArgumentException)
            {
                return Invalid(
                    SaveTransferValidationStatus.InvalidContent,
                    "백업 안의 저장 데이터를 읽을 수 없습니다.");
            }

            if (candidate?.cheeseTama == null || string.IsNullOrWhiteSpace(candidate.version))
            {
                return Invalid(
                    SaveTransferValidationStatus.InvalidContent,
                    "백업 안의 저장 데이터 구조가 올바르지 않습니다.");
            }

            var supportedSchemaVersion = new CheeseTamaSaveData().version;
            if (!string.Equals(
                    envelope.saveSchemaVersion,
                    candidate.version,
                    StringComparison.Ordinal)
                || !string.Equals(
                    candidate.version,
                    supportedSchemaVersion,
                    StringComparison.Ordinal))
            {
                return Invalid(
                    SaveTransferValidationStatus.UnsupportedSaveSchema,
                    "현재 게임에서 지원하지 않는 저장 데이터 버전입니다.");
            }

            if (!TryValidateAndNormalizeCandidate(candidate, out var semanticError))
            {
                return Invalid(
                    SaveTransferValidationStatus.UnsafeSaveData,
                    semanticError);
            }

            string normalizedJson;
            try
            {
                normalizedJson = JsonUtility.ToJson(candidate, true);
            }
            catch (ArgumentException)
            {
                return Invalid(
                    SaveTransferValidationStatus.UnsafeSaveData,
                    "가져올 저장 데이터를 안전한 표준 형식으로 변환하지 못했습니다.");
            }

            if (string.IsNullOrWhiteSpace(normalizedJson)
                || Encoding.UTF8.GetByteCount(normalizedJson) > MaximumContentBytes)
            {
                return Invalid(
                    SaveTransferValidationStatus.ContentTooLarge,
                    "정규화한 저장 데이터가 허용 용량을 초과했습니다.");
            }

            var payload = CloudSavePayload.Create(
                CloudSaveSlotRules.PrimarySlotId,
                normalizedJson,
                envelope.revision,
                modifiedUtc);
            if (!payload.IsValid())
            {
                return Invalid(
                    SaveTransferValidationStatus.InvalidContent,
                    "가져올 저장 데이터를 안전한 교체 형식으로 준비하지 못했습니다.");
            }

            var preview = new SaveTransferPreview(
                candidate.cheeseTama.name,
                candidate.cheeseTama.level,
                candidate.economy?.milkCoins ?? 0,
                candidate.version,
                exportedUtc,
                candidate.cheeseTama.lastSavedAtIso);
            return SaveTransferValidationResult.Valid(payload, preview);
        }

        public static string ComputeSnapshotHash(CheeseTamaSaveData saveData)
        {
            return saveData == null
                ? string.Empty
                : CloudSavePayload.ComputeContentHash(JsonUtility.ToJson(saveData, true));
        }

        public static string CreateFileName(DateTimeOffset now, bool preImportBackup = false)
        {
            var kind = preImportBackup ? "before-import" : "save";
            return $"cheesetama-{kind}-{now.ToLocalTime():yyyyMMdd-HHmmss}.ctsave.json";
        }

        private static bool TryValidateAndNormalizeCandidate(
            CheeseTamaSaveData candidate,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            if (candidate?.cheeseTama == null)
            {
                return RejectSemantic("타마 정보가 없어 저장을 가져올 수 없습니다.", out errorMessage);
            }

            if (!TryNormalizeTamaName(candidate.cheeseTama.name, out var normalizedName))
            {
                return RejectSemantic(
                    $"타마 이름은 제어 문자 없이 UTF-16 {MaximumImportedTamaNameLength}자 이하여야 합니다.",
                    out errorMessage);
            }

            candidate.cheeseTama.name = normalizedName;
            if (!TryValidateCriticalRanges(candidate, out errorMessage)
                || !TryValidateListCapacities(candidate, out errorMessage))
            {
                return false;
            }

            // Missing legacy fields and bounded receipt collections use the same
            // migration path as a normal load. Grossly unsafe values are rejected
            // before this step so normalization cannot disguise a hostile payload.
            candidate.EnsureRuntimeDefaults();
            NormalizeMilkGrowthLevels(candidate.milkGrowth);

            if (!TryValidateCriticalRanges(candidate, out errorMessage)
                || !TryValidateListCapacities(candidate, out errorMessage)
                || !TryNormalizeAndValidateIdentifiers(candidate, out errorMessage)
                || !TryNormalizePendingEvent(candidate, out errorMessage)
                || !TryNormalizeMemoryJournal(candidate, out errorMessage))
            {
                return false;
            }

            return true;
        }

        private static bool TryValidateCriticalRanges(
            CheeseTamaSaveData candidate,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            var tama = candidate.cheeseTama;
            if (!IsBetween(tama.maxLevel, 1, MaximumImportedTamaLevel)
                || !IsBetween(tama.level, 1, tama.maxLevel)
                || !IsBetween(tama.levelProgress, 0, 99))
            {
                return RejectSemantic(
                    "타마 레벨 또는 레벨 진행도가 정상 범위를 벗어났습니다.",
                    out errorMessage);
            }

            var stats = tama.stats;
            if (stats != null
                && (!AllBetween(
                        0,
                        100,
                        stats.hunger,
                        stats.mood,
                        stats.cleanliness,
                        stats.sleepiness,
                        stats.health,
                        stats.maturation,
                        stats.affection,
                        stats.milkSatisfaction,
                        stats.overfullness,
                        stats.bodyChillIntensity,
                        stats.fermentedAftertasteIntensity,
                        stats.sleepRhythmDisruptionIntensity)
                    || !AllBetween(
                        0,
                        12,
                        stats.bodyChillHoursRemaining,
                        stats.fermentedAftertasteHoursRemaining,
                        stats.sleepRhythmDisruptionHoursRemaining)))
            {
                return RejectSemantic(
                    "타마의 핵심 상태 수치가 정상 범위를 벗어났습니다.",
                    out errorMessage);
            }

            var economy = candidate.economy;
            if (economy != null
                && !AllBetween(
                    0,
                    MaximumImportedCurrency,
                    economy.milkCoins,
                    economy.milkDrops,
                    economy.starDrops,
                    economy.affectionPoints,
                    economy.collectionFragments))
            {
                return RejectSemantic(
                    "재화 수치가 안전한 범위를 벗어났습니다.",
                    out errorMessage);
            }

            if (candidate.milkGrowth != null)
            {
                for (var index = 0; index < candidate.milkGrowth.Count; index += 1)
                {
                    var entry = candidate.milkGrowth[index];
                    if (entry != null
                        && (!IsBetween(entry.growthLevel, 0, 5)
                            || !IsBetween(entry.growthPoints, 0, MaximumImportedCounter)))
                    {
                        return RejectSemantic(
                            "우유 성장 수치가 안전한 범위를 벗어났습니다.",
                            out errorMessage);
                    }
                }
            }

            if (candidate.snackInventory != null)
            {
                for (var index = 0; index < candidate.snackInventory.Count; index += 1)
                {
                    var entry = candidate.snackInventory[index];
                    if (entry != null
                        && !IsBetween(entry.quantity, 0, MaximumImportedCounter))
                    {
                        return RejectSemantic(
                            "간식 보유 수량이 안전한 범위를 벗어났습니다.",
                            out errorMessage);
                    }
                }
            }

            var play = candidate.playMiniGames;
            if (play != null
                && !AllBetween(
                    0,
                    MaximumImportedCounter,
                    play.highestBouncyJumpScore,
                    play.totalBouncyJumpSessions,
                    play.totalBouncyJumpSuccesses))
            {
                return RejectSemantic(
                    "미니게임 기록이 안전한 범위를 벗어났습니다.",
                    out errorMessage);
            }

            var care = candidate.careHistory;
            if (care != null
                && !AllBetween(
                    0,
                    MaximumImportedCounter,
                    care.totalCareActions,
                    care.milkFeeds,
                    care.starMilkFeeds,
                    care.snacksFed,
                    care.cookings,
                    care.playSessions,
                    care.petSessions,
                    care.cleanings,
                    care.rests,
                    care.waitHours))
            {
                return RejectSemantic(
                    "누적 돌봄 기록이 안전한 범위를 벗어났습니다.",
                    out errorMessage);
            }

            var daily = candidate.dailyCare;
            if (daily != null
                && !AllBetween(
                    0,
                    MaximumImportedDailyCounter,
                    daily.milkFeeds,
                    daily.snacksFed,
                    daily.cookings,
                    daily.playSessions,
                    daily.cleanings,
                    daily.rests,
                    daily.completedRoutineCount))
            {
                return RejectSemantic(
                    "일일 돌봄 기록이 안전한 범위를 벗어났습니다.",
                    out errorMessage);
            }

            var session = candidate.milkroomSession;
            if (session != null
                && !AllBetween(
                    0,
                    MaximumImportedCounter,
                    session.todaySeconds,
                    session.currentSessionSeconds,
                    session.totalSeconds,
                    session.sessionsToday,
                    session.totalSessions,
                    session.highestClaimedSessionMinute,
                    session.todayMilkDropCatches,
                    session.totalMilkDropCatches))
            {
                return RejectSemantic(
                    "우유방 이용 기록이 안전한 범위를 벗어났습니다.",
                    out errorMessage);
            }

            var fantasy = candidate.fantasyPowder;
            if (fantasy != null
                && (!AllBetween(
                        0,
                        MaximumImportedCounter,
                        fantasy.powderQuantity,
                        fantasy.attemptCount)
                    || !IsBetween(
                        fantasy.pityHintLevel,
                        0,
                        FantasyPowderSaveData.MaximumPityHintLevel)))
            {
                return RejectSemantic(
                    "환상가루 진행 수치가 안전한 범위를 벗어났습니다.",
                    out errorMessage);
            }

            if (!TryValidateStarLegacyRanges(candidate.starLegacy, out errorMessage)
                || !TryValidateNpcRanges(candidate.npcVisits, out errorMessage)
                || !TryValidateMilkBlendingRanges(candidate.milkBlending, out errorMessage)
                || !TryValidateWeeklyRanges(candidate.weeklyCareJourney, out errorMessage)
                || !TryValidateSleepRanges(candidate.sleepSchedule, out errorMessage))
            {
                return false;
            }

            var lateGrowth = candidate.lateLevelGrowth;
            if (lateGrowth != null
                && (!IsBetween(lateGrowth.trackedLevel, 0, MaximumImportedTamaLevel)
                    || !IsBetween(lateGrowth.progressUnits, 0, MaximumImportedCounter)))
            {
                return RejectSemantic(
                    "후반 성장 진행도가 안전한 범위를 벗어났습니다.",
                    out errorMessage);
            }

            var temperament = candidate.newGameSetup?.temperamentSeed;
            if (temperament != null
                && !AllBetween(
                    0,
                    100,
                    temperament.balance,
                    temperament.activity,
                    temperament.expressiveness,
                    temperament.composure,
                    temperament.focus))
            {
                return RejectSemantic(
                    "초기 성향 수치가 정상 범위를 벗어났습니다.",
                    out errorMessage);
            }

            return true;
        }

        private static bool TryValidateStarLegacyRanges(
            StarLegacySaveData state,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            if (state == null)
            {
                return true;
            }

            if (!IsBetween(state.starEggGenerationCount, 0, MaximumImportedCounter)
                || !IsBetween(
                    state.starMilkCareCount,
                    0,
                    StarLegacySaveData.MaximumSignalCount)
                || !IsBetween(
                    state.fantasyResonance,
                    0,
                    StarLegacySaveData.MaximumSignalCount))
            {
                return RejectSemantic(
                    "별 계승 진행 수치가 안전한 범위를 벗어났습니다.",
                    out errorMessage);
            }

            var cycle = state.maturationCycle;
            if (cycle == null)
            {
                return true;
            }

            if (!IsBetween(cycle.progress, 0, 99)
                || !IsBetween(cycle.completedCycles, 0, MaximumImportedCounter)
                || !IsBetween(cycle.claimedCycles, 0, cycle.completedCycles))
            {
                return RejectSemantic(
                    "최종 숙성 주기 수치가 안전한 범위를 벗어났습니다.",
                    out errorMessage);
            }

            if (cycle.pendingRewards != null)
            {
                for (var index = 0; index < cycle.pendingRewards.Count; index += 1)
                {
                    var reward = cycle.pendingRewards[index];
                    if (reward != null
                        && (!IsBetween(reward.cycleNumber, 1, MaximumImportedCounter)
                            || !AllBetween(
                                0,
                                MaximumImportedCurrency,
                                reward.milkCoins,
                                reward.milkDrops,
                                reward.starDrops,
                                reward.fantasyPowder)))
                    {
                        return RejectSemantic(
                            "대기 중인 숙성 보상이 안전한 범위를 벗어났습니다.",
                            out errorMessage);
                    }
                }
            }

            return true;
        }

        private static bool TryValidateNpcRanges(
            NpcVisitSaveData state,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            if (state == null)
            {
                return true;
            }

            if (!IsBetween(state.visitsToday, 0, MaximumImportedDailyCounter)
                || (state.pending != null && !IsBetween(state.pending.storyStep, 0, 2)))
            {
                return RejectSemantic(
                    "NPC 방문 진행 수치가 안전한 범위를 벗어났습니다.",
                    out errorMessage);
            }

            if (state.relationships != null)
            {
                for (var index = 0; index < state.relationships.Count; index += 1)
                {
                    var relationship = state.relationships[index];
                    if (relationship != null
                        && (!IsBetween(relationship.visits, 0, MaximumImportedCounter)
                            || !IsBetween(relationship.affinity, 0, 99)
                            || !IsBetween(relationship.storyStep, 0, 2)))
                    {
                        return RejectSemantic(
                            "NPC 관계 수치가 안전한 범위를 벗어났습니다.",
                            out errorMessage);
                    }
                }
            }

            return true;
        }

        private static bool TryValidateMilkBlendingRanges(
            MilkBlendingSaveData state,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            if (state?.ingredientUsage == null)
            {
                return true;
            }

            for (var index = 0; index < state.ingredientUsage.Count; index += 1)
            {
                var usage = state.ingredientUsage[index];
                if (usage != null
                    && !IsBetween(usage.blendCount, 0, MaximumImportedCounter))
                {
                    return RejectSemantic(
                        "우유 블렌딩 이용 횟수가 안전한 범위를 벗어났습니다.",
                        out errorMessage);
                }
            }

            return true;
        }

        private static bool TryValidateWeeklyRanges(
            WeeklyCareJourneySaveData state,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            if (state?.objectives == null)
            {
                return true;
            }

            for (var index = 0; index < state.objectives.Count; index += 1)
            {
                var objective = state.objectives[index];
                if (objective != null
                    && !IsBetween(objective.progress, 0, MaximumImportedCounter))
                {
                    return RejectSemantic(
                        "주간 여정 진행도가 안전한 범위를 벗어났습니다.",
                        out errorMessage);
                }
            }

            return true;
        }

        private static bool TryValidateSleepRanges(
            SleepScheduleSaveData state,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            if (state == null)
            {
                return true;
            }

            if (state.activeSession != null
                && state.activeSession.scheduledHours != 0
                && !IsBetween(
                    state.activeSession.scheduledHours,
                    SleepScheduleSaveData.MinimumScheduledHours,
                    SleepScheduleSaveData.MaximumScheduledHours))
            {
                return RejectSemantic(
                    "수면 예약 시간이 정상 범위를 벗어났습니다.",
                    out errorMessage);
            }

            if (state.recoveryReceipts != null)
            {
                for (var index = 0; index < state.recoveryReceipts.Count; index += 1)
                {
                    var receipt = state.recoveryReceipts[index];
                    if (receipt != null
                        && (!IsBetween(
                                receipt.scheduledHours,
                                0,
                                SleepScheduleSaveData.MaximumScheduledHours)
                            || !IsBetween(
                                receipt.elapsedMinutes,
                                0,
                                SleepScheduleSaveData.MaximumScheduledHours * 60)
                            || !IsBetween(receipt.sleepinessDelta, -100, 0)
                            || !IsBetween(receipt.healthDelta, 0, 100)
                            || !IsBetween(receipt.moodDelta, 0, 100)))
                    {
                        return RejectSemantic(
                            "수면 회복 기록이 정상 범위를 벗어났습니다.",
                            out errorMessage);
                    }
                }
            }

            return true;
        }

        private static bool TryValidateListCapacities(
            CheeseTamaSaveData candidate,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            if (!HasSafeCount(candidate.milkGrowth, MaximumImportedInventoryEntries, "우유 성장", out errorMessage)
                || !HasSafeCount(candidate.claimedMilkGrowthRewardKeys, MaximumImportedClaimKeys, "우유 성장 보상", out errorMessage)
                || !HasSafeCount(candidate.snackInventory, MaximumImportedInventoryEntries, "간식 보관함", out errorMessage)
                || !HasSafeCount(candidate.decorations?.ownedItemIds, MaximumImportedCollectionEntries, "보유 장식", out errorMessage)
                || !HasSafeCount(candidate.decorations?.ownedThemeIds, MaximumImportedInventoryEntries, "보유 테마", out errorMessage)
                || !HasSafeCount(candidate.collections?.milk, MaximumImportedCollectionEntries, "우유 도감", out errorMessage)
                || !HasSafeCount(candidate.collections?.evolution, MaximumImportedCollectionEntries, "진화 도감", out errorMessage)
                || !HasSafeCount(candidate.collections?.events, MaximumImportedCollectionEntries, "이벤트 도감", out errorMessage)
                || !HasSafeCount(candidate.collections?.hiddenUnlockedOnly, MaximumImportedCollectionEntries, "숨은 도감", out errorMessage)
                || !HasSafeCount(candidate.collections?.claimedFragmentRewardKeys, MaximumImportedClaimKeys, "도감 보상", out errorMessage)
                || !HasSafeCount(candidate.randomEvents?.history, MaximumImportedRandomEventHistory, "랜덤 이벤트 기록", out errorMessage)
                || !HasSafeCount(candidate.randomEvents?.choiceReceipts, RandomEventSaveData.MaximumChoiceReceipts, "랜덤 이벤트 처리 기록", out errorMessage)
                || !HasSafeCount(candidate.settings?.inputBindings?.bindings, MaximumImportedInputBindings, "입력 설정", out errorMessage)
                || !HasSafeCount(candidate.firstDayJourney?.completedTaskIds, MaximumImportedFirstDayTasks, "첫날 여정", out errorMessage)
                || !HasSafeCount(candidate.memoryJournal?.entries, MemoryJournalSaveData.MaximumEntries, "기억 일지", out errorMessage)
                || !HasSafeCount(candidate.fantasyPowder?.discoveredHiddenRecipeIds, FantasyPowderSaveData.MaximumDiscoveredRecipeIds, "숨은 레시피", out errorMessage)
                || !HasSafeCount(candidate.fantasyPowder?.appliedReceiptKeys, FantasyPowderSaveData.MaximumReceiptKeys, "환상가루 처리 기록", out errorMessage)
                || !HasSafeCount(candidate.starLegacy?.appliedEvolutionReceiptKeys, StarLegacySaveData.MaximumEvolutionReceiptKeys, "별 진화 처리 기록", out errorMessage)
                || !HasSafeCount(candidate.starLegacy?.maturationCycle?.pendingRewards, FinalMaturationCycleSaveData.MaximumPendingRewards, "숙성 대기 보상", out errorMessage)
                || !HasSafeCount(candidate.starLegacy?.maturationCycle?.appliedProgressReceiptKeys, FinalMaturationCycleSaveData.MaximumReceiptKeys, "숙성 진행 처리 기록", out errorMessage)
                || !HasSafeCount(candidate.starLegacy?.maturationCycle?.appliedClaimReceiptKeys, FinalMaturationCycleSaveData.MaximumReceiptKeys, "숙성 보상 처리 기록", out errorMessage)
                || !HasSafeCount(candidate.npcVisits?.relationships, MaximumImportedNpcRelationships, "NPC 관계", out errorMessage)
                || !HasSafeCount(candidate.npcVisits?.receipts, NpcVisitSaveData.MaximumReceipts, "NPC 방문 처리 기록", out errorMessage)
                || !HasSafeCount(candidate.npcRelationshipQuests?.claimReceipts, NpcRelationshipQuestSaveData.MaximumClaimReceipts, "NPC 퀘스트 처리 기록", out errorMessage)
                || !HasSafeCount(candidate.npcRelationshipEpisodes?.completedEpisodeIds, NpcRelationshipEpisodeSaveData.MaximumCompletedEpisodeIds, "NPC 에피소드", out errorMessage)
                || !HasSafeCount(candidate.npcRelationshipEpisodes?.keepsakeIds, NpcRelationshipEpisodeSaveData.MaximumKeepsakeIds, "NPC 기념품", out errorMessage)
                || !HasSafeCount(candidate.npcRelationshipEpisodes?.receipts, NpcRelationshipEpisodeSaveData.MaximumReceipts, "NPC 에피소드 처리 기록", out errorMessage)
                || !HasSafeCount(candidate.milkBlending?.ingredientUsage, MilkBlendingSaveData.MaximumUsageEntries, "블렌딩 이용 기록", out errorMessage)
                || !HasSafeCount(candidate.milkBlending?.discoveredResultIds, MilkBlendingSaveData.MaximumDiscoveredResultIds, "블렌딩 발견 결과", out errorMessage)
                || !HasSafeCount(candidate.milkBlending?.appliedReceiptKeys, MilkBlendingSaveData.MaximumReceiptKeys, "블렌딩 처리 기록", out errorMessage)
                || !HasSafeCount(candidate.milkBlending?.masteryResearchRecordIds, MilkBlendingSaveData.MaximumMasteryResearchRecords, "블렌딩 연구 기록", out errorMessage)
                || !HasSafeCount(candidate.autonomousLife?.firstDiscoveries, AutonomousLifeSaveData.MaximumDiscoveries, "자율 행동 발견", out errorMessage)
                || !HasSafeCount(candidate.sleepSchedule?.recoveryReceipts, SleepScheduleSaveData.MaximumRecoveryReceipts, "수면 회복 기록", out errorMessage)
                || !HasSafeCount(candidate.weeklyCareJourney?.objectives, MaximumImportedWeeklyObjectives, "주간 목표", out errorMessage)
                || !HasSafeCount(candidate.weeklyCareJourney?.eventReceipts, WeeklyCareJourneySaveData.MaximumEventReceipts, "주간 이벤트 처리 기록", out errorMessage)
                || !HasSafeCount(candidate.weeklyCareJourney?.rewardReceipts, WeeklyCareJourneySaveData.MaximumRewardReceipts, "주간 보상 처리 기록", out errorMessage)
                || !HasSafeCount(candidate.decorationWorkshop?.ownedVariantIds, DecorationWorkshopSaveData.MaximumOwnedVariantIds, "공방 보유 변형", out errorMessage)
                || !HasSafeCount(candidate.decorationWorkshop?.appliedCraftReceiptKeys, DecorationWorkshopSaveData.MaximumCraftReceiptKeys, "공방 제작 처리 기록", out errorMessage)
                || !HasSafeCount(candidate.decorationWorkshop?.selectedVariants, DecorationWorkshopSaveData.MaximumSelectedVariants, "공방 선택 변형", out errorMessage)
                || !HasSafeCount(candidate.collectionSetAlbum?.revealedHiddenSetIds, CollectionSetAlbumSaveData.MaximumRevealedSetIds, "도감 숨은 세트", out errorMessage)
                || !HasSafeCount(candidate.collectionSetAlbum?.claimedSetIds, CollectionSetAlbumSaveData.MaximumClaimedSetIds, "도감 완료 세트", out errorMessage)
                || !HasSafeCount(candidate.collectionSetAlbum?.appliedClaimReceiptKeys, CollectionSetAlbumSaveData.MaximumClaimReceiptKeys, "도감 세트 처리 기록", out errorMessage))
            {
                return false;
            }

            return true;
        }

        private static bool HasSafeCount<T>(
            ICollection<T> values,
            int maximum,
            string label,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            return values == null || values.Count <= maximum
                ? true
                : RejectSemantic(
                    $"{label} 목록이 허용 개수 {maximum}개를 초과했습니다.",
                    out errorMessage);
        }

        private static bool TryNormalizeAndValidateIdentifiers(
            CheeseTamaSaveData candidate,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            var tama = candidate.cheeseTama;
            if (!TryNormalizeRequiredIdentifier(ref candidate.playerId, "플레이어 ID", out errorMessage)
                || !TryNormalizeRequiredIdentifier(ref tama.id, "타마 ID", out errorMessage)
                || !TryNormalizeRequiredIdentifier(ref tama.eggType, "알 종류 ID", out errorMessage)
                || !TryNormalizeRequiredIdentifier(ref tama.form, "타마 형태 ID", out errorMessage)
                || !TryNormalizeOptionalIdentifier(ref tama.evolutionId, "진화 ID", out errorMessage)
                || !TryNormalizeRequiredIdentifier(ref candidate.milkroomThemeId, "우유방 테마 ID", out errorMessage))
            {
                return false;
            }

            if (!TryNormalizeIdentifierList(candidate.claimedMilkGrowthRewardKeys, "우유 성장 보상 키", MaximumImportedReceiptKeyLength, out errorMessage)
                || !TryNormalizeIdentifierList(candidate.decorations.ownedItemIds, "보유 장식 ID", MaximumImportedIdentifierLength, out errorMessage)
                || !TryNormalizeIdentifierList(candidate.decorations.ownedThemeIds, "보유 테마 ID", MaximumImportedIdentifierLength, out errorMessage)
                || !TryNormalizeRequiredIdentifier(ref candidate.decorations.equippedWallId, "벽 장식 ID", out errorMessage)
                || !TryNormalizeRequiredIdentifier(ref candidate.decorations.equippedFloorId, "바닥 장식 ID", out errorMessage)
                || !TryNormalizeRequiredIdentifier(ref candidate.decorations.equippedAccentId, "포인트 장식 ID", out errorMessage)
                || !TryNormalizeRequiredIdentifier(ref candidate.decorations.equippedWindowId, "창가 장식 ID", out errorMessage)
                || !TryNormalizeRequiredIdentifier(ref candidate.decorations.equippedShelfId, "선반 장식 ID", out errorMessage)
                || !TryNormalizeRequiredIdentifier(ref candidate.decorations.equippedBedsideId, "침대 장식 ID", out errorMessage)
                || !TryNormalizeIdentifierList(candidate.collections.milk, "우유 도감 ID", MaximumImportedIdentifierLength, out errorMessage)
                || !TryNormalizeIdentifierList(candidate.collections.evolution, "진화 도감 ID", MaximumImportedIdentifierLength, out errorMessage)
                || !TryNormalizeIdentifierList(candidate.collections.events, "이벤트 도감 ID", MaximumImportedIdentifierLength, out errorMessage)
                || !TryNormalizeIdentifierList(candidate.collections.claimedFragmentRewardKeys, "도감 보상 키", MaximumImportedReceiptKeyLength, out errorMessage)
                || !TryNormalizeIdentifierList(candidate.firstDayJourney.completedTaskIds, "첫날 여정 ID", MaximumImportedIdentifierLength, out errorMessage)
                || !TryNormalizeIdentifierList(candidate.fantasyPowder.discoveredHiddenRecipeIds, "숨은 레시피 ID", MaximumImportedIdentifierLength, out errorMessage)
                || !TryNormalizeIdentifierList(candidate.fantasyPowder.appliedReceiptKeys, "환상가루 처리 키", MaximumImportedReceiptKeyLength, out errorMessage)
                || !TryNormalizeIdentifierList(candidate.starLegacy.appliedEvolutionReceiptKeys, "별 진화 처리 키", MaximumImportedReceiptKeyLength, out errorMessage)
                || !TryNormalizeIdentifierList(candidate.starLegacy.maturationCycle.appliedProgressReceiptKeys, "숙성 진행 처리 키", MaximumImportedReceiptKeyLength, out errorMessage)
                || !TryNormalizeIdentifierList(candidate.starLegacy.maturationCycle.appliedClaimReceiptKeys, "숙성 보상 처리 키", MaximumImportedReceiptKeyLength, out errorMessage)
                || !TryNormalizeIdentifierList(candidate.npcRelationshipEpisodes.completedEpisodeIds, "NPC 에피소드 ID", MaximumImportedIdentifierLength, out errorMessage)
                || !TryNormalizeIdentifierList(candidate.npcRelationshipEpisodes.keepsakeIds, "NPC 기념품 ID", MaximumImportedIdentifierLength, out errorMessage)
                || !TryNormalizeIdentifierList(candidate.milkBlending.discoveredResultIds, "블렌딩 결과 ID", MaximumImportedIdentifierLength, out errorMessage)
                || !TryNormalizeIdentifierList(candidate.milkBlending.appliedReceiptKeys, "블렌딩 처리 키", MaximumImportedReceiptKeyLength, out errorMessage)
                || !TryNormalizeIdentifierList(candidate.milkBlending.masteryResearchRecordIds, "블렌딩 연구 ID", MaximumImportedIdentifierLength, out errorMessage)
                || !TryNormalizeIdentifierList(candidate.decorationWorkshop.ownedVariantIds, "공방 변형 ID", MaximumImportedIdentifierLength, out errorMessage)
                || !TryNormalizeIdentifierList(candidate.decorationWorkshop.appliedCraftReceiptKeys, "공방 제작 처리 키", MaximumImportedReceiptKeyLength, out errorMessage)
                || !TryNormalizeIdentifierList(candidate.collectionSetAlbum.revealedHiddenSetIds, "도감 숨은 세트 ID", MaximumImportedIdentifierLength, out errorMessage)
                || !TryNormalizeIdentifierList(candidate.collectionSetAlbum.claimedSetIds, "도감 완료 세트 ID", MaximumImportedIdentifierLength, out errorMessage)
                || !TryNormalizeIdentifierList(candidate.collectionSetAlbum.appliedClaimReceiptKeys, "도감 세트 처리 키", MaximumImportedReceiptKeyLength, out errorMessage))
            {
                return false;
            }

            return TryNormalizeInventoryIdentifiers(candidate, out errorMessage)
                && TryNormalizeCollectionIdentifiers(candidate, out errorMessage)
                && TryNormalizeReceiptIdentifiers(candidate, out errorMessage);
        }

        private static bool TryNormalizeInventoryIdentifiers(
            CheeseTamaSaveData candidate,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            for (var index = 0; index < candidate.milkGrowth.Count; index += 1)
            {
                var entry = candidate.milkGrowth[index];
                if (entry == null
                    || !TryNormalizeRequiredIdentifier(ref entry.milkId, "우유 성장 ID", out errorMessage))
                {
                    return entry != null || !string.IsNullOrEmpty(errorMessage)
                        ? false
                        : RejectSemantic("우유 성장 목록에 빈 항목이 있습니다.", out errorMessage);
                }
            }

            for (var index = 0; index < candidate.snackInventory.Count; index += 1)
            {
                var entry = candidate.snackInventory[index];
                if (entry == null)
                {
                    return RejectSemantic("간식 보관함에 빈 항목이 있습니다.", out errorMessage);
                }

                if (!TryNormalizeRequiredIdentifier(ref entry.snackId, "간식 ID", out errorMessage))
                {
                    return false;
                }
            }

            for (var index = 0; index < candidate.milkBlending.ingredientUsage.Count; index += 1)
            {
                var entry = candidate.milkBlending.ingredientUsage[index];
                if (entry == null)
                {
                    return RejectSemantic("블렌딩 이용 기록에 빈 항목이 있습니다.", out errorMessage);
                }

                if (!TryNormalizeRequiredIdentifier(ref entry.ingredientId, "블렌딩 재료 ID", out errorMessage)
                    || !TryNormalizeRequiredIdentifier(ref entry.resultSnackId, "블렌딩 결과 ID", out errorMessage))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TryNormalizeCollectionIdentifiers(
            CheeseTamaSaveData candidate,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            for (var index = 0; index < candidate.collections.hiddenUnlockedOnly.Count; index += 1)
            {
                var entry = candidate.collections.hiddenUnlockedOnly[index];
                if (entry == null
                    || !TryNormalizeRequiredIdentifier(ref entry.id, "숨은 도감 ID", out errorMessage))
                {
                    return entry != null || !string.IsNullOrEmpty(errorMessage)
                        ? false
                        : RejectSemantic("숨은 도감에 빈 항목이 있습니다.", out errorMessage);
                }
            }

            for (var index = 0; index < candidate.randomEvents.history.Count; index += 1)
            {
                var entry = candidate.randomEvents.history[index];
                if (entry == null)
                {
                    return RejectSemantic("랜덤 이벤트 기록에 빈 항목이 있습니다.", out errorMessage);
                }

                if (!TryNormalizeRequiredIdentifier(ref entry.eventId, "랜덤 이벤트 ID", out errorMessage))
                {
                    return false;
                }
            }

            for (var index = 0; index < candidate.autonomousLife.firstDiscoveries.Count; index += 1)
            {
                var entry = candidate.autonomousLife.firstDiscoveries[index];
                if (entry == null
                    || !TryNormalizeRequiredIdentifier(ref entry.behaviourId, "자율 행동 ID", out errorMessage))
                {
                    return entry != null || !string.IsNullOrEmpty(errorMessage)
                        ? false
                        : RejectSemantic("자율 행동 발견 기록에 빈 항목이 있습니다.", out errorMessage);
                }
            }

            return true;
        }

        private static bool TryNormalizeReceiptIdentifiers(
            CheeseTamaSaveData candidate,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            for (var index = 0; index < candidate.randomEvents.choiceReceipts.Count; index += 1)
            {
                var receipt = candidate.randomEvents.choiceReceipts[index];
                if (receipt == null
                    || !TryNormalizeRequiredIdentifier(ref receipt.occurrenceId, "이벤트 발생 ID", out errorMessage)
                    || !TryNormalizeRequiredIdentifier(ref receipt.eventId, "이벤트 ID", out errorMessage)
                    || !TryNormalizeRequiredIdentifier(ref receipt.choiceId, "이벤트 선택 ID", out errorMessage))
                {
                    return receipt != null || !string.IsNullOrEmpty(errorMessage)
                        ? false
                        : RejectSemantic("랜덤 이벤트 처리 기록에 빈 항목이 있습니다.", out errorMessage);
                }
            }

            for (var index = 0; index < candidate.npcVisits.relationships.Count; index += 1)
            {
                var entry = candidate.npcVisits.relationships[index];
                if (entry == null
                    || !TryNormalizeRequiredIdentifier(ref entry.npcId, "NPC 관계 ID", out errorMessage))
                {
                    return entry != null || !string.IsNullOrEmpty(errorMessage)
                        ? false
                        : RejectSemantic("NPC 관계 목록에 빈 항목이 있습니다.", out errorMessage);
                }
            }

            for (var index = 0; index < candidate.npcVisits.receipts.Count; index += 1)
            {
                var receipt = candidate.npcVisits.receipts[index];
                if (receipt == null
                    || !TryNormalizeRequiredIdentifier(ref receipt.occurrenceId, "NPC 방문 발생 ID", out errorMessage)
                    || !TryNormalizeRequiredIdentifier(ref receipt.npcId, "NPC ID", out errorMessage)
                    || !TryNormalizeRequiredIdentifier(ref receipt.choiceId, "NPC 선택 ID", out errorMessage))
                {
                    return receipt != null || !string.IsNullOrEmpty(errorMessage)
                        ? false
                        : RejectSemantic("NPC 방문 처리 기록에 빈 항목이 있습니다.", out errorMessage);
                }
            }

            for (var index = 0; index < candidate.npcRelationshipQuests.claimReceipts.Count; index += 1)
            {
                var receipt = candidate.npcRelationshipQuests.claimReceipts[index];
                if (receipt == null
                    || !TryNormalizeRequiredIdentifier(ref receipt.claimReceiptId, "NPC 퀘스트 처리 ID", out errorMessage)
                    || !TryNormalizeRequiredIdentifier(ref receipt.offerId, "NPC 제안 ID", out errorMessage)
                    || !TryNormalizeRequiredIdentifier(ref receipt.npcId, "NPC ID", out errorMessage)
                    || !TryNormalizeRequiredIdentifier(ref receipt.questId, "NPC 퀘스트 ID", out errorMessage))
                {
                    return receipt != null || !string.IsNullOrEmpty(errorMessage)
                        ? false
                        : RejectSemantic("NPC 퀘스트 처리 기록에 빈 항목이 있습니다.", out errorMessage);
                }
            }

            for (var index = 0; index < candidate.npcRelationshipEpisodes.receipts.Count; index += 1)
            {
                var receipt = candidate.npcRelationshipEpisodes.receipts[index];
                if (receipt == null
                    || !TryNormalizeRequiredIdentifier(ref receipt.receiptId, "NPC 에피소드 처리 ID", out errorMessage)
                    || !TryNormalizeRequiredIdentifier(ref receipt.episodeId, "NPC 에피소드 ID", out errorMessage)
                    || !TryNormalizeRequiredIdentifier(ref receipt.npcId, "NPC ID", out errorMessage)
                    || !TryNormalizeRequiredIdentifier(ref receipt.choiceId, "NPC 에피소드 선택 ID", out errorMessage))
                {
                    return receipt != null || !string.IsNullOrEmpty(errorMessage)
                        ? false
                        : RejectSemantic("NPC 에피소드 처리 기록에 빈 항목이 있습니다.", out errorMessage);
                }
            }

            for (var index = 0; index < candidate.sleepSchedule.recoveryReceipts.Count; index += 1)
            {
                var receipt = candidate.sleepSchedule.recoveryReceipts[index];
                if (receipt == null
                    || !TryNormalizeRequiredIdentifier(ref receipt.receiptKey, "수면 회복 처리 키", MaximumImportedReceiptKeyLength, out errorMessage))
                {
                    return receipt != null || !string.IsNullOrEmpty(errorMessage)
                        ? false
                        : RejectSemantic("수면 회복 기록에 빈 항목이 있습니다.", out errorMessage);
                }
            }

            return true;
        }

        private static bool TryNormalizePendingEvent(
            CheeseTamaSaveData candidate,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            var pending = candidate.randomEvents?.pendingEvent;
            if (pending == null || !pending.HasValue)
            {
                return true;
            }

            if (!TryNormalizeRequiredIdentifier(
                    ref pending.occurrenceId,
                    "대기 이벤트 발생 ID",
                    MaximumImportedReceiptKeyLength,
                    out errorMessage)
                || !TryNormalizeRequiredIdentifier(
                    ref pending.eventId,
                    "대기 이벤트 ID",
                    out errorMessage))
            {
                return false;
            }

            if (!RandomEventSystem.TryGetDefinition(pending.eventId, out var definition))
            {
                return RejectSemantic(
                    "알 수 없는 대기 이벤트가 포함되어 있습니다.",
                    out errorMessage);
            }

            // Presentation text is authoritative catalog data, not imported content.
            pending.title = definition.title;
            pending.message = definition.message;
            return true;
        }

        private static bool TryNormalizeMemoryJournal(
            CheeseTamaSaveData candidate,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            var journal = candidate.memoryJournal;
            if (journal == null)
            {
                return true;
            }

            if (!TryNormalizeOptionalIdentifier(
                ref journal.lastRecalledMemoryId,
                "최근 회상 기억 ID",
                out errorMessage))
            {
                return false;
            }

            for (var index = 0; index < journal.entries.Count; index += 1)
            {
                var entry = journal.entries[index];
                if (entry == null)
                {
                    return RejectSemantic("기억 일지에 빈 항목이 있습니다.", out errorMessage);
                }

                if (!TryNormalizeRequiredIdentifier(ref entry.id, "기억 ID", out errorMessage)
                    || !TryNormalizeRequiredIdentifier(
                        ref entry.idempotencyKey,
                        "기억 중복 방지 키",
                        MaximumImportedReceiptKeyLength,
                        out errorMessage)
                    || !TryNormalizeOptionalIdentifier(ref entry.sourceId, "기억 출처 ID", out errorMessage)
                    || !TryNormalizeOptionalIdentifier(ref entry.occurrenceId, "기억 발생 ID", out errorMessage)
                    || !TryNormalizeOptionalIdentifier(ref entry.detailId, "기억 상세 ID", out errorMessage)
                    || !TryNormalizeOptionalIdentifier(ref entry.dateKey, "기억 날짜 키", out errorMessage)
                    || !TryNormalizeOptionalIdentifier(ref entry.occurredAtIso, "기억 발생 시각", out errorMessage)
                    || !TryNormalizeOptionalIdentifier(ref entry.formId, "기억 형태 ID", out errorMessage)
                    || !TryNormalizeOptionalIdentifier(ref entry.hiddenUnlockId, "기억 해금 ID", out errorMessage)
                    || !TryNormalizeDisplayText(
                        ref entry.tamaName,
                        "기억 속 타마 이름",
                        MaximumImportedTamaNameLength,
                        true,
                        out errorMessage)
                    || !TryNormalizeDisplayText(
                        ref entry.title,
                        "기억 제목",
                        MaximumImportedTitleLength,
                        true,
                        out errorMessage)
                    || !TryNormalizeDisplayText(
                        ref entry.quote,
                        "기억 문장",
                        MaximumImportedQuoteLength,
                        false,
                        out errorMessage))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TryNormalizeDisplayText(
            ref string value,
            string label,
            int maximumLength,
            bool required,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            var normalized = (value ?? string.Empty).Trim();
            if ((required && normalized.Length == 0)
                || normalized.Length > maximumLength
                || !HasValidUtf16(normalized)
                || ContainsUnsafeDisplayControl(normalized))
            {
                return RejectSemantic(
                    $"{label}가 비어 있거나 허용 길이 {maximumLength}자를 초과했습니다.",
                    out errorMessage);
            }

            value = normalized;
            return true;
        }

        private static bool TryNormalizeIdentifierList(
            IList<string> values,
            string label,
            int maximumLength,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            if (values == null)
            {
                return true;
            }

            for (var index = 0; index < values.Count; index += 1)
            {
                var value = values[index];
                if (!TryNormalizeRequiredIdentifier(
                    ref value,
                    label,
                    maximumLength,
                    out errorMessage))
                {
                    return false;
                }

                values[index] = value;
            }

            return true;
        }

        private static bool TryNormalizeRequiredIdentifier(
            ref string value,
            string label,
            out string errorMessage)
        {
            return TryNormalizeRequiredIdentifier(
                ref value,
                label,
                MaximumImportedIdentifierLength,
                out errorMessage);
        }

        private static bool TryNormalizeRequiredIdentifier(
            ref string value,
            string label,
            int maximumLength,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            var normalized = (value ?? string.Empty).Trim();
            if (normalized.Length == 0
                || normalized.Length > maximumLength
                || !HasValidUtf16(normalized)
                || ContainsControlCharacter(normalized))
            {
                return RejectSemantic(
                    $"{label}가 비어 있거나 허용 길이 {maximumLength}자를 초과했습니다.",
                    out errorMessage);
            }

            value = normalized;
            return true;
        }

        private static bool TryNormalizeOptionalIdentifier(
            ref string value,
            string label,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            var normalized = (value ?? string.Empty).Trim();
            if (normalized.Length > MaximumImportedIdentifierLength
                || !HasValidUtf16(normalized)
                || ContainsControlCharacter(normalized))
            {
                return RejectSemantic(
                    $"{label}가 허용 길이 {MaximumImportedIdentifierLength}자를 초과했습니다.",
                    out errorMessage);
            }

            value = normalized;
            return true;
        }

        private static bool TryNormalizeTamaName(string value, out string normalized)
        {
            normalized = (value ?? string.Empty).Trim();
            if (normalized.Length == 0)
            {
                normalized = "CheeseTama";
            }

            return normalized.Length <= MaximumImportedTamaNameLength
                && HasValidUtf16(normalized)
                && !ContainsControlCharacter(normalized);
        }

        private static void NormalizeMilkGrowthLevels(IList<MilkGrowthSaveEntry> entries)
        {
            if (entries == null)
            {
                return;
            }

            for (var index = 0; index < entries.Count; index += 1)
            {
                var entry = entries[index];
                if (entry == null)
                {
                    continue;
                }

                entry.growthLevel = entry.growthPoints <= 0
                    ? 0
                    : Math.Min(5, entry.growthPoints / 10 + 1);
            }
        }

        private static bool IsBetween(int value, int minimum, int maximum)
        {
            return value >= minimum && value <= maximum;
        }

        private static bool AllBetween(int minimum, int maximum, params int[] values)
        {
            if (values == null)
            {
                return true;
            }

            for (var index = 0; index < values.Length; index += 1)
            {
                if (!IsBetween(values[index], minimum, maximum))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool HasValidUtf16(string value)
        {
            if (value == null)
            {
                return true;
            }

            for (var index = 0; index < value.Length; index += 1)
            {
                var current = value[index];
                if (char.IsHighSurrogate(current))
                {
                    if (index + 1 >= value.Length || !char.IsLowSurrogate(value[index + 1]))
                    {
                        return false;
                    }

                    index += 1;
                }
                else if (char.IsLowSurrogate(current))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool HasSafeJsonStringTokens(string json, int maximumDecodedLength)
        {
            if (string.IsNullOrEmpty(json))
            {
                return true;
            }

            var insideString = false;
            var decodedLength = 0;
            for (var index = 0; index < json.Length; index += 1)
            {
                var current = json[index];
                if (!insideString)
                {
                    if (current == '"')
                    {
                        insideString = true;
                        decodedLength = 0;
                    }

                    continue;
                }

                if (current == '"')
                {
                    insideString = false;
                    continue;
                }

                if (current != '\\')
                {
                    if (current < 0x20 || char.IsLowSurrogate(current))
                    {
                        return false;
                    }

                    if (char.IsHighSurrogate(current))
                    {
                        if (index + 1 >= json.Length || !char.IsLowSurrogate(json[index + 1]))
                        {
                            return false;
                        }

                        index += 1;
                        decodedLength += 2;
                    }
                    else
                    {
                        decodedLength += 1;
                    }

                    if (decodedLength > maximumDecodedLength)
                    {
                        return false;
                    }

                    continue;
                }

                index += 1;
                if (index >= json.Length)
                {
                    return false;
                }

                var escaped = json[index];
                if (escaped == 'u')
                {
                    if (!TryReadJsonHexQuad(json, index + 1, out var codeUnit))
                    {
                        return false;
                    }

                    if (codeUnit >= 0xD800 && codeUnit <= 0xDBFF)
                    {
                        if (index + 10 >= json.Length
                            || json[index + 5] != '\\'
                            || json[index + 6] != 'u'
                            || !TryReadJsonHexQuad(json, index + 7, out var lowSurrogate)
                            || lowSurrogate < 0xDC00
                            || lowSurrogate > 0xDFFF)
                        {
                            return false;
                        }

                        index += 10;
                        decodedLength += 2;
                    }
                    else
                    {
                        if (codeUnit >= 0xDC00 && codeUnit <= 0xDFFF)
                        {
                            return false;
                        }

                        index += 4;
                        decodedLength += 1;
                    }
                }
                else if (escaped == '"'
                    || escaped == '\\'
                    || escaped == '/'
                    || escaped == 'b'
                    || escaped == 'f'
                    || escaped == 'n'
                    || escaped == 'r'
                    || escaped == 't')
                {
                    decodedLength += 1;
                }
                else
                {
                    return false;
                }

                if (decodedLength > maximumDecodedLength)
                {
                    return false;
                }
            }

            return !insideString;
        }

        private static bool HasSafeJsonStructure(
            string json,
            int maximumStructuralTokens,
            int maximumNestingDepth)
        {
            if (string.IsNullOrEmpty(json)
                || maximumStructuralTokens <= 0
                || maximumNestingDepth <= 0)
            {
                return false;
            }

            var stack = new char[maximumNestingDepth];
            var depth = 0;
            var structuralTokens = 0;
            var inString = false;
            var escaped = false;

            for (var index = 0; index < json.Length; index += 1)
            {
                var character = json[index];
                if (inString)
                {
                    if (escaped)
                    {
                        escaped = false;
                    }
                    else if (character == '\\')
                    {
                        escaped = true;
                    }
                    else if (character == '"')
                    {
                        inString = false;
                    }

                    continue;
                }

                if (character == '"')
                {
                    inString = true;
                    continue;
                }

                if (character == '{' || character == '[')
                {
                    if (depth >= maximumNestingDepth)
                    {
                        return false;
                    }

                    stack[depth] = character;
                    depth += 1;
                    structuralTokens += 1;
                }
                else if (character == '}' || character == ']')
                {
                    if (depth <= 0)
                    {
                        return false;
                    }

                    var expectedOpening = character == '}' ? '{' : '[';
                    if (stack[depth - 1] != expectedOpening)
                    {
                        return false;
                    }

                    depth -= 1;
                    structuralTokens += 1;
                }
                else if (character == ',' || character == ':')
                {
                    structuralTokens += 1;
                }

                if (structuralTokens > maximumStructuralTokens)
                {
                    return false;
                }
            }

            return !inString && !escaped && depth == 0;
        }

        private static bool TryReadJsonHexQuad(string value, int startIndex, out int codeUnit)
        {
            codeUnit = 0;
            if (startIndex < 0 || startIndex + 4 > (value?.Length ?? 0))
            {
                return false;
            }

            for (var offset = 0; offset < 4; offset += 1)
            {
                var current = value[startIndex + offset];
                int digit;
                if (current >= '0' && current <= '9')
                {
                    digit = current - '0';
                }
                else if (current >= 'a' && current <= 'f')
                {
                    digit = current - 'a' + 10;
                }
                else if (current >= 'A' && current <= 'F')
                {
                    digit = current - 'A' + 10;
                }
                else
                {
                    return false;
                }

                codeUnit = (codeUnit << 4) | digit;
            }

            return true;
        }

        private static bool ContainsControlCharacter(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            for (var index = 0; index < value.Length; index += 1)
            {
                if (char.IsControl(value[index]))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsUnsafeDisplayControl(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            for (var index = 0; index < value.Length; index += 1)
            {
                var current = value[index];
                if (char.IsControl(current)
                    && current != '\n'
                    && current != '\r'
                    && current != '\t')
                {
                    return true;
                }
            }

            return false;
        }

        private static bool RejectSemantic(string message, out string errorMessage)
        {
            errorMessage = message ?? "가져올 저장 데이터가 안전 범위를 벗어났습니다.";
            return false;
        }

        private static SaveTransferValidationResult Invalid(
            SaveTransferValidationStatus status,
            string message)
        {
            return SaveTransferValidationResult.Invalid(status, message);
        }

        private static DateTimeOffset ResolveModifiedUtc(
            CheeseTamaSaveData saveData,
            DateTimeOffset fallback)
        {
            return DateTimeOffset.TryParse(
                saveData.cheeseTama?.lastSavedAtIso,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var parsed)
                && parsed.UtcDateTime.Ticks > 0L
                    ? parsed
                    : fallback;
        }

        private static bool LooksLikeJsonObject(string value)
        {
            return !string.IsNullOrEmpty(value)
                && value.Length >= 2
                && value[0] == '{'
                && value[value.Length - 1] == '}';
        }

        private static bool HasSerializedField(string json, string fieldName)
        {
            var fieldToken = $"\"{fieldName}\"";
            var searchIndex = 0;
            while (searchIndex < json.Length)
            {
                var fieldIndex = json.IndexOf(fieldToken, searchIndex, StringComparison.Ordinal);
                if (fieldIndex < 0)
                {
                    return false;
                }

                var separatorIndex = fieldIndex + fieldToken.Length;
                while (separatorIndex < json.Length && char.IsWhiteSpace(json[separatorIndex]))
                {
                    separatorIndex += 1;
                }

                if (separatorIndex < json.Length && json[separatorIndex] == ':')
                {
                    return true;
                }

                searchIndex = fieldIndex + fieldToken.Length;
            }

            return false;
        }
    }

    public enum SaveTransferApplyAuthorizationStatus
    {
        Authorized,
        NoPendingImport,
        ConfirmationMismatch,
        LocalSaveChanged,
        MissingCurrentSave
    }

    public readonly struct SaveTransferApplyAuthorization
    {
        public SaveTransferApplyAuthorization(
            SaveTransferApplyAuthorizationStatus status,
            string message,
            CloudSavePayload payload)
        {
            Status = status;
            Message = message ?? string.Empty;
            Payload = payload;
        }

        public SaveTransferApplyAuthorizationStatus Status { get; }
        public string Message { get; }
        public CloudSavePayload Payload { get; }
        public bool IsAuthorized => Status == SaveTransferApplyAuthorizationStatus.Authorized
            && Payload != null;
    }

    public sealed class SaveTransferImportSession
    {
        public const string ConfirmationPhrase = "IMPORT SAVE";

        private CloudSavePayload pendingPayload;
        private SaveTransferPreview pendingPreview;
        private string expectedLocalHash = string.Empty;

        public bool HasPendingImport => pendingPayload != null && pendingPreview != null;
        public SaveTransferPreview PendingPreview => pendingPreview;

        public SaveTransferValidationResult Begin(
            string envelopeJson,
            CheeseTamaSaveData currentSave)
        {
            Clear();
            var validation = SaveTransferCodec.Validate(envelopeJson);
            if (!validation.IsValid)
            {
                return validation;
            }

            if (currentSave == null)
            {
                return SaveTransferValidationResult.Invalid(
                    SaveTransferValidationStatus.InvalidContent,
                    "현재 로컬 저장을 확인할 수 없어 가져오기를 준비하지 않았습니다.");
            }

            pendingPayload = validation.Payload;
            pendingPreview = validation.Preview;
            expectedLocalHash = SaveTransferCodec.ComputeSnapshotHash(currentSave);
            return validation;
        }

        public SaveTransferApplyAuthorization Authorize(
            string confirmation,
            CheeseTamaSaveData currentSave)
        {
            if (!HasPendingImport)
            {
                return Failure(
                    SaveTransferApplyAuthorizationStatus.NoPendingImport,
                    "먼저 가져올 백업 파일을 선택하세요.");
            }

            if (currentSave == null)
            {
                return Failure(
                    SaveTransferApplyAuthorizationStatus.MissingCurrentSave,
                    "현재 로컬 저장을 확인할 수 없어 덮어쓰지 않았습니다.");
            }

            if (!string.Equals(
                    confirmation?.Trim(),
                    ConfirmationPhrase,
                    StringComparison.Ordinal))
            {
                return Failure(
                    SaveTransferApplyAuthorizationStatus.ConfirmationMismatch,
                    $"가져오려면 {ConfirmationPhrase}를 정확히 입력하세요.");
            }

            if (!string.Equals(
                    expectedLocalHash,
                    SaveTransferCodec.ComputeSnapshotHash(currentSave),
                    StringComparison.OrdinalIgnoreCase))
            {
                Clear();
                return Failure(
                    SaveTransferApplyAuthorizationStatus.LocalSaveChanged,
                    "파일 선택 후 로컬 저장이 변경되었습니다. 파일을 다시 선택해 주세요.");
            }

            return new SaveTransferApplyAuthorization(
                SaveTransferApplyAuthorizationStatus.Authorized,
                string.Empty,
                pendingPayload);
        }

        public void Clear()
        {
            pendingPayload = null;
            pendingPreview = null;
            expectedLocalHash = string.Empty;
        }

        private static SaveTransferApplyAuthorization Failure(
            SaveTransferApplyAuthorizationStatus status,
            string message)
        {
            return new SaveTransferApplyAuthorization(status, message, null);
        }
    }
}
