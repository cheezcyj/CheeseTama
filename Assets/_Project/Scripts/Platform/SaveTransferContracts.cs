using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using CheeseTama.Gameplay.Decorations;
using CheeseTama.Gameplay.Events;
using CheeseTama.Gameplay.MiniGames;
using CheeseTama.Gameplay.Story;
using CheeseTama.Platform.Accounts;
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
                + $"백업한 때 · {ExportedUtc.ToLocalTime():yyyy-MM-dd HH:mm}";
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
            return new SaveTransferValidationResult(status, ToPlayerMessage(message), null, null);
        }

        private static string ToPlayerMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return "백업 파일을 읽지 못했어요. 다른 파일을 골라 주세요.";
            }

            if (message.IndexOf("스키마", StringComparison.OrdinalIgnoreCase) >= 0
                || message.IndexOf("버전", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "지금 게임에서는 열 수 없는 백업이에요. 더 새로운 게임에서 만든 파일인지 확인해 주세요.";
            }

            if (message.IndexOf("JSON", StringComparison.OrdinalIgnoreCase) >= 0
                || message.IndexOf("UTF-8", StringComparison.OrdinalIgnoreCase) >= 0
                || message.IndexOf("Unicode", StringComparison.OrdinalIgnoreCase) >= 0
                || message.IndexOf("인코딩", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "백업 파일 내용을 읽을 수 없어요. 다른 백업 파일을 골라 주세요.";
            }

            if (message.IndexOf("무결성", StringComparison.OrdinalIgnoreCase) >= 0
                || message.IndexOf("손상", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "백업 파일이 손상되어 열 수 없어요. 다른 백업 파일을 골라 주세요.";
            }

            if (message.IndexOf("식별자", StringComparison.OrdinalIgnoreCase) >= 0
                || message.IndexOf(" ID", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "백업 안의 일부 항목 이름이 너무 길거나 올바르지 않아요.";
            }

            return "백업 안의 게임 기록을 안전하게 사용할 수 없어요. 다른 백업 파일을 골라 주세요.";
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
        private const double MaximumImportedFutureClockSkewHours = 24d;

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
                errorMessage = "백업할 게임 기록을 찾지 못했어요.";
                return false;
            }

            try
            {
                var contentJson = JsonUtility.ToJson(saveData, true);
                var contentBytes = Encoding.UTF8.GetBytes(contentJson);
                if (contentBytes.Length > MaximumContentBytes)
                {
                    errorMessage = "게임 기록이 너무 커서 백업할 수 없어요.";
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
                    errorMessage = "백업 파일이 너무 커서 만들 수 없어요.";
                    return false;
                }

                return true;
            }
            catch (ArgumentException)
            {
                errorMessage = "백업 파일을 만들지 못했어요. 다시 시도해 주세요.";
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

            if (!TryValidateAndNormalizeSaveContent(
                    contentJson,
                    out var candidate,
                    out var normalizedJson,
                    out var contentStatus,
                    out var contentError))
            {
                return Invalid(contentStatus, contentError);
            }

            if (!string.Equals(
                    envelope.saveSchemaVersion,
                    candidate.version,
                    StringComparison.Ordinal))
            {
                return Invalid(
                    SaveTransferValidationStatus.UnsupportedSaveSchema,
                    "현재 게임에서 지원하지 않는 저장 데이터 버전입니다.");
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

        internal static bool TryValidateAndNormalizeSaveContent(
            string contentJson,
            out CheeseTamaSaveData candidate,
            out string normalizedJson,
            out SaveTransferValidationStatus status,
            out string errorMessage)
        {
            candidate = null;
            normalizedJson = string.Empty;
            status = SaveTransferValidationStatus.InvalidContent;
            errorMessage = "백업 안의 저장 데이터가 올바르지 않습니다.";
            if (string.IsNullOrWhiteSpace(contentJson))
            {
                return false;
            }

            if (Encoding.UTF8.GetByteCount(contentJson) > MaximumContentBytes)
            {
                status = SaveTransferValidationStatus.ContentTooLarge;
                errorMessage = "백업 안의 저장 데이터가 허용 용량을 초과했습니다.";
                return false;
            }

            var trimmedContent = contentJson.Trim();
            if (!LooksLikeJsonObject(trimmedContent)
                || !HasSerializedField(trimmedContent, "version")
                || !HasSerializedObjectField(trimmedContent, "cheeseTama"))
            {
                errorMessage = "백업 안에 필수 저장 항목이 없습니다.";
                return false;
            }

            if (!HasSafeJsonStringTokens(trimmedContent, MaximumImportedJsonStringLength))
            {
                status = SaveTransferValidationStatus.UnsafeSaveData;
                errorMessage = "백업 안의 문자열이 허용 길이를 초과했거나 Unicode 형식이 올바르지 않습니다.";
                return false;
            }

            if (!HasSafeJsonStructure(
                    trimmedContent,
                    MaximumImportedJsonStructuralTokens,
                    MaximumImportedJsonNestingDepth))
            {
                status = SaveTransferValidationStatus.UnsafeSaveData;
                errorMessage = "백업 안의 JSON 구조가 허용 복잡도를 초과했거나 올바르지 않습니다.";
                return false;
            }

            try
            {
                candidate = JsonUtility.FromJson<CheeseTamaSaveData>(trimmedContent);
            }
            catch (ArgumentException)
            {
                errorMessage = "백업 안의 저장 데이터를 읽을 수 없습니다.";
                return false;
            }

            if (candidate?.cheeseTama == null || string.IsNullOrWhiteSpace(candidate.version))
            {
                errorMessage = "백업 안의 저장 데이터 구조가 올바르지 않습니다.";
                return false;
            }

            // Revision-three roots own bounded collections and normalize aggressively. Validate
            // their raw materialized values before migration or EnsureRuntimeDefaults can trim,
            // drop, clamp, or replace hostile input with an apparently safe default.
            if (!TryValidateRevisionThreeRootContracts(candidate, out errorMessage))
            {
                status = SaveTransferValidationStatus.UnsafeSaveData;
                return false;
            }

            var serializedRevision = SaveSchemaMigrationHub.ResolveSerializedRevision(
                trimmedContent,
                candidate);
            if (!SaveSchemaMigrationHub.Migrate(candidate, serializedRevision).Succeeded)
            {
                status = SaveTransferValidationStatus.UnsupportedSaveSchema;
                errorMessage = "현재 게임에서 지원하지 않는 저장 데이터 스키마입니다.";
                return false;
            }

            var supportedSchemaVersion = new CheeseTamaSaveData().version;
            if (!string.Equals(
                    candidate.version,
                    supportedSchemaVersion,
                    StringComparison.Ordinal))
            {
                status = SaveTransferValidationStatus.UnsupportedSaveSchema;
                errorMessage = "현재 게임에서 지원하지 않는 저장 데이터 버전입니다.";
                return false;
            }

            if (!TryValidateAndNormalizeCandidate(candidate, out errorMessage))
            {
                status = SaveTransferValidationStatus.UnsafeSaveData;
                return false;
            }

            try
            {
                normalizedJson = JsonUtility.ToJson(candidate, true);
            }
            catch (ArgumentException)
            {
                status = SaveTransferValidationStatus.UnsafeSaveData;
                errorMessage = "가져올 저장 데이터를 안전한 표준 형식으로 변환하지 못했습니다.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(normalizedJson)
                || Encoding.UTF8.GetByteCount(normalizedJson) > MaximumContentBytes)
            {
                status = SaveTransferValidationStatus.ContentTooLarge;
                errorMessage = "정규화한 저장 데이터가 허용 용량을 초과했습니다.";
                return false;
            }

            return true;
        }

        private static bool TryValidateRevisionThreeRootContracts(
            CheeseTamaSaveData candidate,
            out string errorMessage)
        {
            var maximumAcceptedUtc = DateTimeOffset.UtcNow.AddHours(
                MaximumImportedFutureClockSkewHours);
            return TryValidateRawLifeChapters(
                    candidate?.lifeChapters,
                    maximumAcceptedUtc,
                    out errorMessage)
                && TryValidateRawMiniGameMastery(
                    candidate?.miniGameMastery,
                    maximumAcceptedUtc,
                    out errorMessage)
                && TryValidateRawMilkroomInvestigation(
                    candidate?.milkroomInvestigation,
                    maximumAcceptedUtc,
                    out errorMessage)
                && TryValidateRawStarLineage(
                    candidate?.starLineage,
                    maximumAcceptedUtc,
                    out errorMessage);
        }

        private static bool TryValidateRawLifeChapters(
            LifeChapterSaveData state,
            DateTimeOffset maximumAcceptedUtc,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            if (state == null)
            {
                return true;
            }

            if (state.schemaVersion != LifeChapterSaveData.CurrentSchemaVersion)
            {
                return RejectSemantic(
                    "생활 장의 저장 스키마를 지원하지 않습니다.",
                    out errorMessage);
            }

            if (!HasSafeCount(
                    state.progress,
                    LifeChapterSaveData.MaximumProgressEntries,
                    "생활 장 진행",
                    out errorMessage)
                || !HasSafeCount(
                    state.completions,
                    LifeChapterSaveData.MaximumCompletionEntries,
                    "생활 장 완료",
                    out errorMessage))
            {
                return false;
            }

            if (!IsSafeImportedTimestamp(
                    state.lastObservedAtIso,
                    maximumAcceptedUtc,
                    allowEmpty: true))
            {
                return RejectSemantic(
                    "생활 장의 관찰 시각이 허용 범위를 벗어났습니다.",
                    out errorMessage);
            }

            if (state.progress != null)
            {
                for (var index = 0; index < state.progress.Count; index += 1)
                {
                    var entry = state.progress[index];
                    if (entry == null
                        || !LifeChapterSaveData.IsStableToken(entry.chapterId))
                    {
                        return RejectSemantic(
                            "생활 장 진행 기록에 안전하지 않은 장 ID가 있습니다.",
                            out errorMessage);
                    }

                    if (!HasSafeCount(
                            entry.completedObjectiveIds,
                            LifeChapterSaveData.MaximumObjectiveIdsPerChapter,
                            "생활 장 목표",
                            out errorMessage))
                    {
                        return false;
                    }

                    if (entry.completedObjectiveIds == null)
                    {
                        continue;
                    }

                    for (var objectiveIndex = 0;
                        objectiveIndex < entry.completedObjectiveIds.Count;
                        objectiveIndex += 1)
                    {
                        if (!LifeChapterSaveData.IsStableToken(
                                entry.completedObjectiveIds[objectiveIndex]))
                        {
                            return RejectSemantic(
                                "생활 장 목표 기록에 안전하지 않은 목표 ID가 있습니다.",
                                out errorMessage);
                        }
                    }
                }
            }

            if (state.completions == null)
            {
                return true;
            }

            for (var index = 0; index < state.completions.Count; index += 1)
            {
                var completion = state.completions[index];
                if (completion == null
                    || !LifeChapterSaveData.IsStableToken(completion.chapterId)
                    || !LifeChapterSaveData.IsStableToken(completion.choiceId)
                    || !LifeChapterSaveData.IsReceiptToken(completion.receiptId))
                {
                    return RejectSemantic(
                        "생활 장 완료 기록에 안전하지 않은 ID가 있습니다.",
                        out errorMessage);
                }

                if (!IsSafeImportedTimestamp(
                        completion.completedAtIso,
                        maximumAcceptedUtc,
                        allowEmpty: false))
                {
                    return RejectSemantic(
                        "생활 장 완료 시각이 허용 범위를 벗어났습니다.",
                        out errorMessage);
                }
            }

            return true;
        }

        private static bool TryValidateRawMiniGameMastery(
            MiniGameMasterySaveData state,
            DateTimeOffset maximumAcceptedUtc,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            if (state == null)
            {
                return true;
            }

            if (state.schemaVersion != MiniGameMasterySaveData.CurrentSchemaVersion)
            {
                return RejectSemantic(
                    "생활놀이 숙련의 저장 스키마를 지원하지 않습니다.",
                    out errorMessage);
            }

            if (!HasSafeCount(
                    state.medalReceipts,
                    MiniGameMasterySaveData.MaximumReceiptCount,
                    "생활놀이 메달 처리 기록",
                    out errorMessage))
            {
                return false;
            }

            if (state.medalReceipts == null)
            {
                return true;
            }

            for (var index = 0; index < state.medalReceipts.Count; index += 1)
            {
                var receipt = state.medalReceipts[index];
                if (receipt == null
                    || !MiniGameMasterySaveData.IsStableToken(receipt.receiptId)
                    || !MiniGameMasterySaveData.IsStableToken(receipt.gameId)
                    || !MiniGameMasterySaveData.IsStableToken(receipt.medalId))
                {
                    return RejectSemantic(
                        "생활놀이 메달 처리 기록에 안전하지 않은 ID가 있습니다.",
                        out errorMessage);
                }

                if (!IsSafeImportedTimestamp(
                        receipt.achievedAtIso,
                        maximumAcceptedUtc,
                        allowEmpty: false))
                {
                    return RejectSemantic(
                        "생활놀이 메달 달성 시각이 허용 범위를 벗어났습니다.",
                        out errorMessage);
                }
            }

            return true;
        }

        private static bool TryValidateRawMilkroomInvestigation(
            MilkroomInvestigationSaveData state,
            DateTimeOffset maximumAcceptedUtc,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            if (state == null)
            {
                return true;
            }

            if (state.schemaVersion != MilkroomInvestigationSaveData.CurrentSchemaVersion)
            {
                return RejectSemantic(
                    "우유방 조사의 저장 스키마를 지원하지 않습니다.",
                    out errorMessage);
            }

            if (!HasSafeCount(
                    state.discoveredClueIds,
                    MilkroomInvestigationSaveData.MaximumClues,
                    "우유방 조사 단서",
                    out errorMessage)
                || !HasSafeCount(
                    state.completions,
                    MilkroomInvestigationSaveData.MaximumCompletions,
                    "우유방 조사 완료",
                    out errorMessage))
            {
                return false;
            }

            if (state.discoveredClueIds != null)
            {
                for (var index = 0; index < state.discoveredClueIds.Count; index += 1)
                {
                    if (!MilkroomInvestigationSaveData.IsStableToken(
                            state.discoveredClueIds[index]))
                    {
                        return RejectSemantic(
                            "우유방 조사 단서에 안전하지 않은 ID가 있습니다.",
                            out errorMessage);
                    }
                }
            }

            if (state.completions == null)
            {
                return true;
            }

            for (var index = 0; index < state.completions.Count; index += 1)
            {
                var completion = state.completions[index];
                if (completion == null
                    || !MilkroomInvestigationSaveData.IsStableToken(completion.episodeId)
                    || !MilkroomInvestigationSaveData.IsStableToken(completion.choiceId)
                    || !MilkroomInvestigationSaveData.IsReceiptToken(completion.receiptId))
                {
                    return RejectSemantic(
                        "우유방 조사 완료 기록에 안전하지 않은 ID가 있습니다.",
                        out errorMessage);
                }

                if (!IsSafeImportedTimestamp(
                        completion.completedAtIso,
                        maximumAcceptedUtc,
                        allowEmpty: false))
                {
                    return RejectSemantic(
                        "우유방 조사 완료 시각이 허용 범위를 벗어났습니다.",
                        out errorMessage);
                }
            }

            return true;
        }

        private static bool TryValidateRawStarLineage(
            StarLineageSaveData state,
            DateTimeOffset maximumAcceptedUtc,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            if (state == null)
            {
                return true;
            }

            if (state.schemaVersion != StarLineageSaveData.CurrentSchemaVersion)
            {
                return RejectSemantic(
                    "별 계보의 저장 스키마를 지원하지 않습니다.",
                    out errorMessage);
            }

            if (!HasSafeCount(
                    state.records,
                    StarLineageSaveData.MaximumRecords,
                    "별 계보",
                    out errorMessage)
                || !HasSafeCount(
                    state.appliedReceiptIds,
                    StarLineageSaveData.MaximumReceiptIds,
                    "별 계보 처리 기록",
                    out errorMessage))
            {
                return false;
            }

            if (!IsBetween(
                    state.pendingTraitGenerationNumber,
                    0,
                    MaximumImportedCounter)
                || !IsBetween(
                    state.activeTraitGenerationNumber,
                    0,
                    MaximumImportedCounter))
            {
                return RejectSemantic(
                    "별 계보 세대 수치가 안전한 범위를 벗어났습니다.",
                    out errorMessage);
            }

            if (!IsSafeRawToken(
                    state.activeTraitId,
                    MaximumImportedIdentifierLength,
                    allowEmpty: true)
                || (string.IsNullOrEmpty(state.activeTraitId)
                    && state.activeTraitGenerationNumber != 0))
            {
                return RejectSemantic(
                    "별 계보 활성 특성 정보가 올바르지 않습니다.",
                    out errorMessage);
            }

            if (state.appliedReceiptIds != null)
            {
                for (var index = 0; index < state.appliedReceiptIds.Count; index += 1)
                {
                    if (!IsSafeRawToken(
                            state.appliedReceiptIds[index],
                            MaximumImportedReceiptKeyLength,
                            allowEmpty: false))
                    {
                        return RejectSemantic(
                            "별 계보 처리 기록에 안전하지 않은 ID가 있습니다.",
                            out errorMessage);
                    }
                }
            }

            if (state.records == null)
            {
                return true;
            }

            for (var index = 0; index < state.records.Count; index += 1)
            {
                var record = state.records[index];
                if (record == null
                    || !IsBetween(
                        record.nextGenerationNumber,
                        1,
                        MaximumImportedCounter)
                    || !IsSafeRawToken(
                        record.recordId,
                        MaximumImportedIdentifierLength,
                        allowEmpty: false)
                    || !IsSafeRawToken(
                        record.tamaId,
                        MaximumImportedIdentifierLength,
                        allowEmpty: false)
                    || !IsSafeRawToken(
                        record.formId,
                        MaximumImportedIdentifierLength,
                        allowEmpty: true)
                    || !IsSafeRawToken(
                        record.evolutionId,
                        MaximumImportedIdentifierLength,
                        allowEmpty: true)
                    || !IsSafeRawToken(
                        record.primaryMilkId,
                        MaximumImportedIdentifierLength,
                        allowEmpty: true)
                    || !IsSafeRawToken(
                        record.careStyleId,
                        MaximumImportedIdentifierLength,
                        allowEmpty: true))
                {
                    return RejectSemantic(
                        "별 계보 기록의 세대 수치 또는 ID가 올바르지 않습니다.",
                        out errorMessage);
                }

                var displayName = record.displayName;
                if (!TryNormalizeDisplayText(
                        ref displayName,
                        "별 계보 이름",
                        MaximumImportedTitleLength,
                        required: false,
                        out errorMessage))
                {
                    return false;
                }

                if (!IsSafeImportedTimestamp(
                        record.completedAtIso,
                        maximumAcceptedUtc,
                        allowEmpty: false))
                {
                    return RejectSemantic(
                        "별 계보 완료 시각이 허용 범위를 벗어났습니다.",
                        out errorMessage);
                }
            }

            return true;
        }

        private static bool IsSafeRawToken(
            string value,
            int maximumLength,
            bool allowEmpty)
        {
            if (string.IsNullOrEmpty(value))
            {
                return allowEmpty;
            }

            if (value.Length > maximumLength
                || !string.Equals(value, value.Trim(), StringComparison.Ordinal))
            {
                return false;
            }

            for (var index = 0; index < value.Length; index += 1)
            {
                var character = value[index];
                var valid = (character >= 'a' && character <= 'z')
                    || (character >= 'A' && character <= 'Z')
                    || (character >= '0' && character <= '9')
                    || character == '_'
                    || character == '-'
                    || character == '.'
                    || character == ':';
                if (!valid)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsSafeImportedTimestamp(
            string value,
            DateTimeOffset maximumAcceptedUtc,
            bool allowEmpty)
        {
            if (string.IsNullOrEmpty(value))
            {
                return allowEmpty;
            }

            return DateTimeOffset.TryParse(
                    value,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var parsed)
                && parsed.UtcDateTime.Ticks > 0L
                && parsed.ToUniversalTime() <= maximumAcceptedUtc;
        }

        private static bool HasSerializedObjectField(string json, string fieldName)
        {
            var fieldToken = $"\"{fieldName}\"";
            var fieldIndex = json?.IndexOf(fieldToken, StringComparison.Ordinal) ?? -1;
            if (fieldIndex < 0)
            {
                return false;
            }

            var valueIndex = fieldIndex + fieldToken.Length;
            while (valueIndex < json.Length && char.IsWhiteSpace(json[valueIndex]))
            {
                valueIndex += 1;
            }

            if (valueIndex >= json.Length || json[valueIndex] != ':')
            {
                return false;
            }

            valueIndex += 1;
            while (valueIndex < json.Length && char.IsWhiteSpace(json[valueIndex]))
            {
                valueIndex += 1;
            }

            return valueIndex < json.Length && json[valueIndex] == '{';
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
            new DreamStorySeasonSystem().NormalizeState(candidate.dreamStorySeason);
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
                        stats.sleepRhythmDisruptionIntensity,
                        stats.heavinessIntensity,
                        stats.sugarOverloadIntensity,
                        stats.fantasyEchoIntensity,
                        stats.lethargyIntensity,
                        stats.stomachRiskIntensity,
                        stats.traitDistortionIntensity)
                    || !AllBetween(
                        0,
                        12,
                        stats.bodyChillHoursRemaining,
                        stats.fermentedAftertasteHoursRemaining,
                        stats.sleepRhythmDisruptionHoursRemaining,
                        stats.heavinessHoursRemaining,
                        stats.sugarOverloadHoursRemaining,
                        stats.fantasyEchoHoursRemaining,
                        stats.lethargyHoursRemaining,
                        stats.stomachRiskHoursRemaining,
                        stats.traitDistortionHoursRemaining)))
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
                    play.totalBouncyJumpSuccesses,
                    play.highestMilkDropScore,
                    play.totalMilkDropSessions,
                    play.totalMilkDropSuccesses,
                    play.highestCleaningScore,
                    play.totalCleaningSessions,
                    play.totalCleaningSuccesses,
                    play.highestBlueBallScore,
                    play.totalBlueBallSessions,
                    play.totalBlueBallSuccesses))
            {
                return RejectSemantic(
                    "미니게임 기록이 안전한 범위를 벗어났습니다.",
                    out errorMessage);
            }

            if (play?.recentSessions != null)
            {
                for (var index = 0; index < play.recentSessions.Count; index += 1)
                {
                    var entry = play.recentSessions[index];
                    if (entry != null
                        && (!IsBetween(entry.score, 0, PlayMiniGameSaveData.MaximumRecordedScore)
                            || !IsBetween(
                                entry.successes,
                                0,
                                PlayMiniGameSaveData.MaximumRecordedSuccesses)))
                    {
                        return RejectSemantic(
                            "최근 미니게임 결과가 안전한 범위를 벗어났습니다.",
                            out errorMessage);
                    }
                }
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
                || !TryValidateDecorationPlacementRanges(candidate.decorationPlacement, out errorMessage)
                || !TryValidateNpcAfterstoryRanges(candidate.npcAfterstory, out errorMessage)
                || !TryValidateDreamStoryRanges(candidate.dreamStorySeason, out errorMessage)
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

        private static bool TryValidateDreamStoryRanges(
            DreamStorySeasonSaveData state,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            if (state == null)
            {
                return true;
            }

            var maximumAcceptedUtc = DateTimeOffset.UtcNow.AddHours(24d);
            if (!IsSafeDreamStoryTimestamp(state.lastObservedAtIso, maximumAcceptedUtc))
            {
                return RejectSemantic(
                    "꿈 이야기의 관찰 시각이 허용 범위를 벗어났습니다.",
                    out errorMessage);
            }

            if (state.completions == null)
            {
                return true;
            }

            for (var index = 0; index < state.completions.Count; index += 1)
            {
                var completion = state.completions[index];
                if (completion != null
                    && !IsSafeDreamStoryTimestamp(
                        completion.completedAtIso,
                        maximumAcceptedUtc,
                        allowEmpty: false))
                {
                    return RejectSemantic(
                        "꿈 이야기 완료 시각이 허용 범위를 벗어났습니다.",
                        out errorMessage);
                }
            }

            return true;
        }

        private static bool IsSafeDreamStoryTimestamp(
            string value,
            DateTimeOffset maximumAcceptedUtc,
            bool allowEmpty = true)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return allowEmpty;
            }

            return DateTimeOffset.TryParse(
                    value,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var parsed)
                && parsed.UtcDateTime.Ticks > 0L
                && parsed.ToUniversalTime() <= maximumAcceptedUtc;
        }

        private static bool TryValidateNpcAfterstoryRanges(
            NpcAfterstorySaveData state,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            if (state == null)
            {
                return true;
            }

            var now = DateTimeOffset.UtcNow;
            if (!NpcAfterstorySaveData.IsReasonableObservedUtcTicks(
                    state.latestObservedUtcTicks,
                    now)
                || !NpcAfterstorySaveData.IsReasonableObservedWeekKey(
                    state.latestObservedWeekKey,
                    now))
            {
                return RejectSemantic(
                    "NPC 주간 부탁의 시각 기록이 허용 범위를 벗어났습니다.",
                    out errorMessage);
            }

            if (state.weeklyProgress != null)
            {
                for (var index = 0; index < state.weeklyProgress.Count; index += 1)
                {
                    var progress = state.weeklyProgress[index];
                    if (progress != null
                        && !NpcAfterstorySaveData.IsReasonableObservedUtcTicks(
                            progress.observedUtcTicks,
                            now))
                    {
                        return RejectSemantic(
                            "NPC 주간 부탁의 진행 시각이 허용 범위를 벗어났습니다.",
                            out errorMessage);
                    }
                }
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

        private static bool TryValidateDecorationPlacementRanges(
            DecorationPlacementSaveData state,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            if (state == null)
            {
                return true;
            }

            if (state.schemaVersion < 0
                || state.schemaVersion > DecorationPlacementSaveData.CurrentSchemaVersion)
            {
                return RejectSemantic(
                    "장식 배치 저장 버전을 지원하지 않습니다.",
                    out errorMessage);
            }

            var entries = state.entries;
            if (entries == null)
            {
                return true;
            }

            for (var index = 0; index < entries.Count; index += 1)
            {
                var entry = entries[index];
                if (entry == null
                    || !Enum.IsDefined(typeof(DecorationSlot), entry.slot)
                    || !DecorationPlacementSystem.IsMovableSlot((DecorationSlot)entry.slot)
                    || float.IsNaN(entry.normalizedX)
                    || float.IsInfinity(entry.normalizedX)
                    || float.IsNaN(entry.normalizedY)
                    || float.IsInfinity(entry.normalizedY)
                    || entry.normalizedX < DecorationPlacementSystem.MinimumNormalizedCoordinate
                    || entry.normalizedX > DecorationPlacementSystem.MaximumNormalizedCoordinate
                    || entry.normalizedY < DecorationPlacementSystem.MinimumNormalizedCoordinate
                    || entry.normalizedY > DecorationPlacementSystem.MaximumNormalizedCoordinate
                    || entry.rotationStep < 0
                    || entry.rotationStep >= DecorationPlacementSystem.RotationStepCount)
                {
                    return RejectSemantic(
                        "장식 배치 좌표가 안전한 범위를 벗어났습니다.",
                        out errorMessage);
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
                || !HasSafeCount(candidate.playMiniGames?.recentSessions, PlayMiniGameSaveData.MaximumRecentSessions, "최근 미니게임 결과", out errorMessage)
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
                || !HasSafeCount(candidate.npcVisits?.stainGuestWeeklyReceiptKeys, NpcVisitSaveData.MaximumStainGuestWeeklyReceipts, "얼룩 손님 주간 처리 기록", out errorMessage)
                || !HasSafeCount(candidate.decorationPlacement?.entries, DecorationPlacementSaveData.MaximumPlacementEntries, "장식 자유 배치", out errorMessage)
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
                || !HasSafeCount(candidate.collectionSetAlbum?.appliedClaimReceiptKeys, CollectionSetAlbumSaveData.MaximumClaimReceiptKeys, "도감 세트 처리 기록", out errorMessage)
                || !HasSafeCount(candidate.postgameResearch?.unlockedNodeIds, PostgameResearchSaveData.MaximumUnlockedNodeIds, "반복 성장 연구", out errorMessage)
                || !HasSafeCount(candidate.postgameResearch?.appliedReceiptKeys, PostgameResearchSaveData.MaximumReceiptKeys, "반복 성장 연구 처리 기록", out errorMessage)
                || !HasSafeCount(candidate.milkroomMystery?.completions, MilkroomMysterySaveData.MaximumCompletions, "밀크룸 미스터리 완료 기록", out errorMessage)
                || !HasSafeCount(candidate.npcAfterstory?.completions, NpcAfterstorySaveData.MaximumAfterstoryCompletions, "NPC 후일담 완료 기록", out errorMessage)
                || !HasSafeCount(candidate.npcAfterstory?.weeklyProgress, NpcAfterstorySaveData.MaximumWeeklyProgressEntries, "NPC 주간 부탁 진행 기록", out errorMessage)
                || !HasSafeCount(candidate.npcAfterstory?.weeklyReceipts, NpcAfterstorySaveData.MaximumWeeklyReceipts, "NPC 주간 부탁 처리 기록", out errorMessage)
                || !HasSafeCount(candidate.dreamStorySeason?.completions, DreamStorySeasonSaveData.MaximumCompletions, "꿈 이야기 완료 기록", out errorMessage)
                || !HasSafeCount(candidate.dreamStorySeason?.discoveredSignalIds, DreamStorySeasonSaveData.MaximumSignals, "꿈 이야기 발견 신호", out errorMessage))
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
                || !TryNormalizeOptionalIdentifier(ref candidate.decorations.equippedFloorId, "바닥 장식 ID", out errorMessage)
                || !TryNormalizeOptionalIdentifier(ref candidate.decorations.equippedAccentId, "포인트 장식 ID", out errorMessage)
                || !TryNormalizeOptionalIdentifier(ref candidate.decorations.equippedWindowId, "창가 장식 ID", out errorMessage)
                || !TryNormalizeOptionalIdentifier(ref candidate.decorations.equippedShelfId, "선반 장식 ID", out errorMessage)
                || !TryNormalizeOptionalIdentifier(ref candidate.decorations.equippedBedsideId, "침대 장식 ID", out errorMessage)
                || !TryNormalizeOptionalIdentifier(ref candidate.npcVisits.stainGuestEchoChoiceId, "얼룩 손님 반향 선택 ID", out errorMessage)
                || !TryNormalizeIdentifierList(candidate.npcVisits.stainGuestWeeklyReceiptKeys, "얼룩 손님 주간 처리 키", MaximumImportedReceiptKeyLength, out errorMessage)
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
                || !TryNormalizeIdentifierList(candidate.collectionSetAlbum.appliedClaimReceiptKeys, "도감 세트 처리 키", MaximumImportedReceiptKeyLength, out errorMessage)
                || !TryNormalizeIdentifierList(candidate.postgameResearch.unlockedNodeIds, "반복 성장 연구 ID", MaximumImportedIdentifierLength, out errorMessage)
                || !TryNormalizeIdentifierList(candidate.postgameResearch.appliedReceiptKeys, "반복 성장 연구 처리 키", MaximumImportedReceiptKeyLength, out errorMessage)
                || !TryNormalizeOptionalIdentifier(ref candidate.postgameResearch.activeProfileId, "반복 성장 연구 프로필 ID", out errorMessage)
                || !TryNormalizeOptionalIdentifier(ref candidate.npcAfterstory.displayedKeepsakeId, "NPC 전시 기념품 ID", out errorMessage)
                || !TryNormalizeOptionalIdentifier(ref candidate.npcAfterstory.latestObservedWeekKey, "NPC 주간 관찰 키", out errorMessage)
                || !TryNormalizeIdentifierList(candidate.dreamStorySeason.discoveredSignalIds, "꿈 이야기 발견 신호", DreamStorySeasonSaveData.MaximumTokenLength, out errorMessage))
            {
                return false;
            }

            return TryNormalizeInventoryIdentifiers(candidate, out errorMessage)
                && TryNormalizeCollectionIdentifiers(candidate, out errorMessage)
                && TryNormalizeMiniGameSessionIdentifiers(candidate, out errorMessage)
                && TryNormalizeReceiptIdentifiers(candidate, out errorMessage);
        }

        private static bool TryNormalizeMiniGameSessionIdentifiers(
            CheeseTamaSaveData candidate,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            var sessions = candidate.playMiniGames?.recentSessions;
            if (sessions == null)
            {
                return true;
            }

            for (var index = 0; index < sessions.Count; index += 1)
            {
                var entry = sessions[index];
                if (entry == null)
                {
                    return RejectSemantic(
                        "최근 미니게임 결과에 빈 항목이 있습니다.",
                        out errorMessage);
                }

                if (!TryNormalizeRequiredIdentifier(
                        ref entry.sessionId,
                        "미니게임 세션 ID",
                        MaximumImportedReceiptKeyLength,
                        out errorMessage)
                    || !TryNormalizeRequiredIdentifier(
                        ref entry.gameId,
                        "미니게임 ID",
                        out errorMessage))
                {
                    return false;
                }

                if (!string.Equals(entry.gameId, MiniGameRecordIds.MilkDrop, StringComparison.Ordinal)
                    && !string.Equals(entry.gameId, MiniGameRecordIds.Cleaning, StringComparison.Ordinal)
                    && !string.Equals(entry.gameId, MiniGameRecordIds.BouncyJump, StringComparison.Ordinal)
                    && !string.Equals(entry.gameId, MiniGameRecordIds.BlueBall, StringComparison.Ordinal))
                {
                    return RejectSemantic(
                        "알 수 없는 미니게임 결과가 포함되어 있습니다.",
                        out errorMessage);
                }

                if (!DateTimeOffset.TryParse(
                        entry.completedAtIso,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind,
                        out var completedAt))
                {
                    return RejectSemantic(
                        "미니게임 완료 시각이 올바르지 않습니다.",
                        out errorMessage);
                }

                entry.completedAtIso = completedAt.ToString("O", CultureInfo.InvariantCulture);
            }

            return true;
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

            for (var index = 0; index < candidate.decorationPlacement.entries.Count; index += 1)
            {
                var entry = candidate.decorationPlacement.entries[index];
                if (entry == null
                    || !TryNormalizeRequiredIdentifier(
                        ref entry.decorationId,
                        "자유 배치 장식 ID",
                        out errorMessage))
                {
                    return entry != null || !string.IsNullOrEmpty(errorMessage)
                        ? false
                        : RejectSemantic("자유 배치 장식에 빈 항목이 있습니다.", out errorMessage);
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

            for (var index = 0; index < candidate.milkroomMystery.completions.Count; index += 1)
            {
                var completion = candidate.milkroomMystery.completions[index];
                if (completion == null
                    || !TryNormalizeRequiredIdentifier(ref completion.chapterId, "미스터리 장 ID", out errorMessage)
                    || !TryNormalizeRequiredIdentifier(ref completion.choiceId, "미스터리 선택 ID", out errorMessage)
                    || !TryNormalizeRequiredIdentifier(ref completion.receiptId, "미스터리 처리 ID", MaximumImportedReceiptKeyLength, out errorMessage))
                {
                    return completion != null || !string.IsNullOrEmpty(errorMessage)
                        ? false
                        : RejectSemantic("미스터리 완료 기록에 빈 항목이 있습니다.", out errorMessage);
                }
            }

            for (var index = 0; index < candidate.dreamStorySeason.completions.Count; index += 1)
            {
                var completion = candidate.dreamStorySeason.completions[index];
                if (completion == null
                    || !TryNormalizeRequiredIdentifier(ref completion.episodeId, "꿈 이야기 에피소드 ID", out errorMessage)
                    || !TryNormalizeRequiredIdentifier(ref completion.choiceId, "꿈 이야기 선택 ID", out errorMessage)
                    || !TryNormalizeRequiredIdentifier(ref completion.receiptId, "꿈 이야기 처리 ID", MaximumImportedReceiptKeyLength, out errorMessage))
                {
                    return completion != null || !string.IsNullOrEmpty(errorMessage)
                        ? false
                        : RejectSemantic("꿈 이야기 완료 기록에 빈 항목이 있습니다.", out errorMessage);
                }
            }

            for (var index = 0; index < candidate.npcAfterstory.completions.Count; index += 1)
            {
                var completion = candidate.npcAfterstory.completions[index];
                if (completion == null
                    || !TryNormalizeRequiredIdentifier(ref completion.afterstoryId, "NPC 후일담 ID", out errorMessage)
                    || !TryNormalizeRequiredIdentifier(ref completion.npcId, "NPC ID", out errorMessage)
                    || !TryNormalizeRequiredIdentifier(ref completion.choiceId, "NPC 후일담 선택 ID", out errorMessage)
                    || !TryNormalizeRequiredIdentifier(ref completion.receiptId, "NPC 후일담 처리 ID", MaximumImportedReceiptKeyLength, out errorMessage)
                    || !TryNormalizeRequiredIdentifier(ref completion.keepsakeId, "NPC 기념품 ID", out errorMessage))
                {
                    return completion != null || !string.IsNullOrEmpty(errorMessage)
                        ? false
                        : RejectSemantic("NPC 후일담 완료 기록에 빈 항목이 있습니다.", out errorMessage);
                }
            }

            for (var index = 0; index < candidate.npcAfterstory.weeklyProgress.Count; index += 1)
            {
                var progress = candidate.npcAfterstory.weeklyProgress[index];
                if (progress == null
                    || !TryNormalizeRequiredIdentifier(ref progress.npcId, "NPC 주간 부탁 NPC ID", out errorMessage)
                    || !TryNormalizeRequiredIdentifier(ref progress.questId, "NPC 주간 부탁 ID", out errorMessage)
                    || !TryNormalizeRequiredIdentifier(ref progress.weekKey, "NPC 주간 부탁 주차 키", out errorMessage))
                {
                    return progress != null || !string.IsNullOrEmpty(errorMessage)
                        ? false
                        : RejectSemantic("NPC 주간 부탁 진행 기록에 빈 항목이 있습니다.", out errorMessage);
                }
            }

            for (var index = 0; index < candidate.npcAfterstory.weeklyReceipts.Count; index += 1)
            {
                var receipt = candidate.npcAfterstory.weeklyReceipts[index];
                if (receipt == null
                    || !TryNormalizeRequiredIdentifier(ref receipt.receiptId, "NPC 주간 부탁 처리 ID", MaximumImportedReceiptKeyLength, out errorMessage)
                    || !TryNormalizeRequiredIdentifier(ref receipt.npcId, "NPC 주간 부탁 NPC ID", out errorMessage)
                    || !TryNormalizeRequiredIdentifier(ref receipt.questId, "NPC 주간 부탁 ID", out errorMessage)
                    || !TryNormalizeRequiredIdentifier(ref receipt.weekKey, "NPC 주간 부탁 주차 키", out errorMessage))
                {
                    return receipt != null || !string.IsNullOrEmpty(errorMessage)
                        ? false
                        : RejectSemantic("NPC 주간 부탁 처리 기록에 빈 항목이 있습니다.", out errorMessage);
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
            string _)
        {
            return SaveTransferValidationResult.Invalid(
                status,
                ResolveUserFacingValidationMessage(status));
        }

        private static string ResolveUserFacingValidationMessage(
            SaveTransferValidationStatus status)
        {
            return status switch
            {
                SaveTransferValidationStatus.MissingData =>
                    "선택한 백업 파일이 비어 있어요.",
                SaveTransferValidationStatus.EnvelopeTooLarge =>
                    "이 백업 파일은 너무 커서 불러올 수 없어요.",
                SaveTransferValidationStatus.ContentTooLarge =>
                    "이 백업 파일은 너무 커서 불러올 수 없어요.",
                SaveTransferValidationStatus.UnsupportedEnvelopeVersion =>
                    "이 백업은 다른 버전에서 만들어져서 지금 불러올 수 없어요.",
                SaveTransferValidationStatus.UnsupportedContentEncoding =>
                    "이 백업은 다른 버전에서 만들어져서 지금 불러올 수 없어요.",
                SaveTransferValidationStatus.UnsupportedSaveSchema =>
                    "이 백업은 다른 버전에서 만들어져서 지금 불러올 수 없어요.",
                SaveTransferValidationStatus.InvalidEnvelope =>
                    "이 파일은 치즈타마 백업이 아니거나 파일이 망가졌어요.",
                SaveTransferValidationStatus.InvalidExportTimestamp =>
                    "이 파일은 치즈타마 백업이 아니거나 파일이 망가졌어요.",
                SaveTransferValidationStatus.InvalidContent =>
                    "이 파일은 치즈타마 백업이 아니거나 파일이 망가졌어요.",
                SaveTransferValidationStatus.HashMismatch =>
                    "이 파일은 치즈타마 백업이 아니거나 파일이 망가졌어요.",
                SaveTransferValidationStatus.UnsafeSaveData =>
                    "이 파일은 치즈타마 백업이 아니거나 파일이 망가졌어요.",
                _ => "이 백업 파일을 불러올 수 없어요. 다른 파일을 골라 주세요."
            };
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
        LocalAccountChanged,
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
        public const string ConfirmationPhrase = "백업 불러오기";

        private CloudSavePayload pendingPayload;
        private SaveTransferPreview pendingPreview;
        private string expectedLocalHash = string.Empty;
        private string expectedLocalAccountScope = string.Empty;

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
                    "현재 게임 기록을 확인하지 못해 백업을 불러오지 않았어요.");
            }

            pendingPayload = validation.Payload;
            pendingPreview = validation.Preview;
            expectedLocalHash = SaveTransferCodec.ComputeSnapshotHash(currentSave);
            expectedLocalAccountScope = CaptureLocalAccountScope();
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
                    "먼저 불러올 백업 파일을 골라 주세요.");
            }

            if (currentSave == null)
            {
                return Failure(
                    SaveTransferApplyAuthorizationStatus.MissingCurrentSave,
                    "현재 게임 기록을 확인하지 못해 백업을 불러오지 않았어요.");
            }

            if (!string.Equals(
                    expectedLocalAccountScope,
                    CaptureLocalAccountScope(),
                    StringComparison.Ordinal))
            {
                Clear();
                return Failure(
                    SaveTransferApplyAuthorizationStatus.LocalAccountChanged,
                    "파일을 고른 뒤 로그인한 계정이 바뀌었어요. 파일을 다시 골라 주세요.");
            }

            if (!string.Equals(
                    confirmation?.Trim(),
                    ConfirmationPhrase,
                    StringComparison.Ordinal))
            {
                return Failure(
                    SaveTransferApplyAuthorizationStatus.ConfirmationMismatch,
                    $"확인 칸에 ‘{ConfirmationPhrase}’라고 입력해 주세요.");
            }

            if (!string.Equals(
                    expectedLocalHash,
                    SaveTransferCodec.ComputeSnapshotHash(currentSave),
                    StringComparison.OrdinalIgnoreCase))
            {
                Clear();
                return Failure(
                    SaveTransferApplyAuthorizationStatus.LocalSaveChanged,
                    "파일을 고른 뒤 게임 기록이 바뀌었어요. 파일을 다시 골라 주세요.");
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
            expectedLocalAccountScope = string.Empty;
        }

        private static string CaptureLocalAccountScope()
        {
            LocalAccountRuntime.EnsureInitialized();
            var snapshot = LocalAccountRuntime.Snapshot;
            return snapshot.IsSignedIn && LocalAccountId.IsValid(snapshot.AccountId)
                ? "account:" + snapshot.AccountId
                : "guest";
        }

        private static SaveTransferApplyAuthorization Failure(
            SaveTransferApplyAuthorizationStatus status,
            string message)
        {
            return new SaveTransferApplyAuthorization(status, message, null);
        }
    }
}
