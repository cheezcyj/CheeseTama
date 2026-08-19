using System;
using System.IO;
using System.Reflection;
using System.Text;
using CheeseTama.Gameplay.Events;
using CheeseTama.Platform;
using CheeseTama.Save;
using NUnit.Framework;
using UnityEngine;

namespace CheeseTama.Tests.EditMode
{
    public sealed class WebSaveTransferFeatureTests
    {
        private static readonly DateTimeOffset ExportedUtc =
            new DateTimeOffset(2026, 8, 18, 3, 30, 0, TimeSpan.Zero);

        [Test]
        public void EnvelopeRoundTripPreservesContentAndBuildsPreview()
        {
            var source = CreateSave("브리", 17, 321);

            var serialized = SaveTransferCodec.TrySerialize(
                source,
                ExportedUtc,
                out var envelopeJson,
                out var errorMessage);
            var validation = SaveTransferCodec.Validate(envelopeJson);

            Assert.That(serialized, Is.True, errorMessage);
            Assert.That(validation.IsValid, Is.True, validation.Message);
            Assert.That(validation.Payload.IsValid(), Is.True);
            Assert.That(validation.Payload.slotId, Is.EqualTo(CloudSaveSlotRules.PrimarySlotId));
            Assert.That(validation.Preview.TamaName, Is.EqualTo("브리"));
            Assert.That(validation.Preview.Level, Is.EqualTo(17));
            Assert.That(validation.Preview.Coins, Is.EqualTo(321));
            Assert.That(validation.Preview.SaveSchemaVersion, Is.EqualTo(source.version));
        }

        [Test]
        public void ExportValidationAndPreviewDoNotMutateLiveSave()
        {
            var live = CreateSave("그대로", 12, 144);
            var before = JsonUtility.ToJson(live, true);

            Assert.That(
                SaveTransferCodec.TrySerialize(
                    live,
                    ExportedUtc,
                    out var envelopeJson,
                    out _),
                Is.True);
            var session = new SaveTransferImportSession();
            var validation = session.Begin(envelopeJson, live);

            Assert.That(validation.IsValid, Is.True, validation.Message);
            Assert.That(JsonUtility.ToJson(live, true), Is.EqualTo(before));
        }

        [Test]
        public void LegacySameSchemaSaveWithMissingOptionalFieldsRemainsImportable()
        {
            const string legacyContent =
                "{\"version\":\"0.1.0\",\"cheeseTama\":{\"name\":\"오래된 저장\",\"level\":8}}";
            var envelopeJson = BuildEnvelopeJson(legacyContent, "0.1.0");

            var validation = SaveTransferCodec.Validate(envelopeJson);

            Assert.That(validation.IsValid, Is.True, validation.Message);
            Assert.That(validation.Preview.TamaName, Is.EqualTo("오래된 저장"));
            Assert.That(validation.Preview.Level, Is.EqualTo(8));
        }

        [Test]
        public void TamperedContentHashIsRejected()
        {
            Assert.That(
                SaveTransferCodec.TrySerialize(
                    CreateSave("해시", 3, 10),
                    ExportedUtc,
                    out var envelopeJson,
                    out _),
                Is.True);
            var envelope = JsonUtility.FromJson<SaveTransferEnvelope>(envelopeJson);
            envelope.contentHash = new string('0', 64);

            var validation = SaveTransferCodec.Validate(JsonUtility.ToJson(envelope));

            Assert.That(validation.IsValid, Is.False);
            Assert.That(validation.Status, Is.EqualTo(SaveTransferValidationStatus.HashMismatch));
        }

        [Test]
        public void RecomputedEnvelopeRejectsUnsafeNameStatsAndCurrency()
        {
            var unsafeName = CreateSave(new string('가', SaveTransferCodec.MaximumImportedTamaNameLength + 1), 3, 10);
            var unsafeStats = CreateSave("상태", 3, 10);
            unsafeStats.cheeseTama.stats.health = 101;
            var unsafeCurrency = CreateSave("재화", 3, -1);

            AssertUnsafeSaveData(unsafeName);
            AssertUnsafeSaveData(unsafeStats);
            AssertUnsafeSaveData(unsafeCurrency);
        }

        [Test]
        public void RecomputedEnvelopeRejectsMalformedNamesAndProgressBounds()
        {
            var controlCharacterName = CreateSave("치즈\u0001타마", 3, 10);
            var oversizedLevel = CreateSave("레벨", 101, 10);
            var invalidProgress = CreateSave("진행도", 3, 10);
            invalidProgress.cheeseTama.levelProgress = 100;
            var oversizedCurrency = CreateSave("재화 상한", 3, SaveTransferCodec.MaximumImportedCurrency + 1);
            const string malformedUtf16Json =
                "{\"version\":\"0.1.0\",\"cheeseTama\":{\"name\":\"치즈\\ud800타마\",\"level\":3}}";
            var malformedUtf16 = SaveTransferCodec.Validate(
                BuildEnvelopeJson(malformedUtf16Json, "0.1.0"));

            AssertUnsafeSaveData(controlCharacterName);
            Assert.That(malformedUtf16.Status, Is.EqualTo(SaveTransferValidationStatus.UnsafeSaveData));
            AssertUnsafeSaveData(oversizedLevel);
            AssertUnsafeSaveData(invalidProgress);
            AssertUnsafeSaveData(oversizedCurrency);
        }

        [Test]
        public void ExcessiveJsonStructureIsRejectedBeforeObjectMaterialization()
        {
            var entries = new StringBuilder(160_000);
            for (var index = 0; index < 50_000; index += 1)
            {
                if (index > 0)
                {
                    entries.Append(',');
                }

                entries.Append("{}");
            }

            var contentJson =
                "{\"version\":\"0.1.0\",\"cheeseTama\":{\"name\":\"구조 검사\",\"level\":3},"
                + "\"memoryJournal\":{\"entries\":["
                + entries
                + "]}}";

            var validation = SaveTransferCodec.Validate(
                BuildEnvelopeJson(contentJson, "0.1.0"));

            Assert.That(validation.Status, Is.EqualTo(SaveTransferValidationStatus.UnsafeSaveData));
            Assert.That(validation.Message, Does.Contain("JSON 구조"));
        }

        [Test]
        public void ExcessiveEnvelopeStructureIsRejectedBeforeObjectMaterialization()
        {
            var padding = new StringBuilder(160_000);
            for (var index = 0; index < 50_000; index += 1)
            {
                if (index > 0)
                {
                    padding.Append(',');
                }

                padding.Append("{}");
            }

            var envelopeJson = "{\"formatVersion\":1,\"padding\":["
                + padding
                + "]}";

            var validation = SaveTransferCodec.Validate(envelopeJson);

            Assert.That(validation.Status, Is.EqualTo(SaveTransferValidationStatus.UnsafeSaveData));
            Assert.That(validation.Message, Does.Contain("JSON 구조"));
        }

        [Test]
        public void BrowserCallbacksRequirePendingRequestAndRejectOversizedPayload()
        {
            var gameObject = new GameObject("SaveTransferBridgeTest");
            try
            {
                var bridge = gameObject.AddComponent<SaveTransferFileBridge>();
                var completedCount = 0;
                var failedCount = 0;
                bridge.ImportCompleted += _ => completedCount += 1;
                bridge.ImportFailed += _ => failedCount += 1;

                bridge.OnBrowserImportCompleted("{}");
                bridge.OnBrowserImportFailed("unsolicited");
                Assert.That(completedCount, Is.Zero);
                Assert.That(failedCount, Is.Zero);

                var pendingField = typeof(SaveTransferFileBridge).GetField(
                    "importRequestPending",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(pendingField, Is.Not.Null);
                pendingField.SetValue(bridge, true);

                bridge.OnBrowserImportCompleted(
                    new string('x', SaveTransferCodec.MaximumEnvelopeBytes + 1));
                Assert.That(completedCount, Is.Zero);
                Assert.That(failedCount, Is.EqualTo(1));

                bridge.OnBrowserImportFailed("duplicate");
                Assert.That(failedCount, Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void RecomputedEnvelopeRejectsOversizedCollectionsAndIdentifiers()
        {
            var oversizedCollection = CreateSave("목록", 3, 10);
            for (var index = 0; index < 513; index += 1)
            {
                oversizedCollection.collections.milk.Add($"milk_{index}");
            }

            var oversizedIdentifier = CreateSave("식별자", 3, 10);
            oversizedIdentifier.collections.milk.Add(
                new string('x', SaveTransferCodec.MaximumImportedIdentifierLength + 1));

            AssertUnsafeSaveData(oversizedCollection);
            AssertUnsafeSaveData(oversizedIdentifier);
        }

        [Test]
        public void RecomputedEnvelopeRejectsOversizedReceiptCollectionsAndKeys()
        {
            var oversizedReceiptCollection = CreateSave("영수증 목록", 3, 10);
            for (var index = 0; index <= MilkBlendingSaveData.MaximumReceiptKeys; index += 1)
            {
                oversizedReceiptCollection.milkBlending.appliedReceiptKeys.Add($"blend_{index}");
            }

            var oversizedReceiptKey = CreateSave("영수증 키", 3, 10);
            oversizedReceiptKey.milkBlending.appliedReceiptKeys.Add(
                new string('r', SaveTransferCodec.MaximumImportedReceiptKeyLength + 1));

            AssertUnsafeSaveData(oversizedReceiptCollection);
            AssertUnsafeSaveData(oversizedReceiptKey);
        }

        [Test]
        public void RecomputedEnvelopeRejectsOversizedPendingAndMemoryText()
        {
            var oversizedPendingMessage = CreateSave("이벤트 문구", 3, 10);
            oversizedPendingMessage.randomEvents.pendingEvent.occurrenceId = "pending_oversized";
            oversizedPendingMessage.randomEvents.pendingEvent.eventId = RandomEventSystem.AmbientEvent.id;
            oversizedPendingMessage.randomEvents.pendingEvent.title = "이벤트";
            oversizedPendingMessage.randomEvents.pendingEvent.message = new string('m', 4097);

            var oversizedMemoryQuote = CreateSave("기억 문구", 3, 10);
            oversizedMemoryQuote.memoryJournal.entries.Add(new MemoryJournalEntrySaveData
            {
                id = "memory_oversized",
                idempotencyKey = "memory_receipt_oversized",
                tamaName = "기억 문구",
                title = "긴 기억",
                quote = new string('q', 4097)
            });

            AssertUnsafeSaveData(oversizedPendingMessage);
            AssertUnsafeSaveData(oversizedMemoryQuote);
        }

        [Test]
        public void PendingEventUsesCatalogPresentationAndRejectsUnknownEventIds()
        {
            var knownEvent = CreateSave("정규 이벤트", 3, 10);
            knownEvent.randomEvents.pendingEvent.occurrenceId = "pending_known";
            knownEvent.randomEvents.pendingEvent.eventId = RandomEventSystem.AmbientEvent.id;
            knownEvent.randomEvents.pendingEvent.title = "변조된 제목";
            knownEvent.randomEvents.pendingEvent.message = "변조된 문구";
            Assert.That(
                SaveTransferCodec.TrySerialize(knownEvent, ExportedUtc, out var envelopeJson, out var errorMessage),
                Is.True,
                errorMessage);

            var knownValidation = SaveTransferCodec.Validate(envelopeJson);
            var normalized = JsonUtility.FromJson<CheeseTamaSaveData>(knownValidation.Payload.contentJson);
            Assert.That(knownValidation.IsValid, Is.True, knownValidation.Message);
            Assert.That(
                normalized.randomEvents.pendingEvent.title,
                Is.EqualTo(RandomEventSystem.AmbientEvent.title));
            Assert.That(
                normalized.randomEvents.pendingEvent.message,
                Is.EqualTo(RandomEventSystem.AmbientEvent.message));

            var unknownEvent = CreateSave("알 수 없는 이벤트", 3, 10);
            unknownEvent.randomEvents.pendingEvent.occurrenceId = "pending_unknown";
            unknownEvent.randomEvents.pendingEvent.eventId = "event_not_in_catalog";
            unknownEvent.randomEvents.pendingEvent.title = "알 수 없음";
            unknownEvent.randomEvents.pendingEvent.message = "알 수 없음";
            AssertUnsafeSaveData(unknownEvent);
        }

        [Test]
        public void ValidatedPayloadUsesNormalizedCandidateJsonAndFreshHash()
        {
            var source = CreateSave("  Soft CheeseTama  ", 7, 80);
            Assert.That(
                SaveTransferCodec.TrySerialize(source, ExportedUtc, out var envelopeJson, out var errorMessage),
                Is.True,
                errorMessage);

            var validation = SaveTransferCodec.Validate(envelopeJson);
            var normalized = JsonUtility.FromJson<CheeseTamaSaveData>(validation.Payload.contentJson);

            Assert.That(validation.IsValid, Is.True, validation.Message);
            Assert.That(normalized.cheeseTama.name, Is.EqualTo("Soft CheeseTama"));
            Assert.That(validation.Payload.IsValid(), Is.True);
        }

        [Test]
        public void UnsupportedEnvelopeAndSaveSchemasAreRejected()
        {
            Assert.That(
                SaveTransferCodec.TrySerialize(
                    CreateSave("버전", 4, 20),
                    ExportedUtc,
                    out var envelopeJson,
                    out _),
                Is.True);
            var envelope = JsonUtility.FromJson<SaveTransferEnvelope>(envelopeJson);
            envelope.formatVersion = SaveTransferEnvelope.CurrentFormatVersion + 1;
            var unsupportedEnvelope = SaveTransferCodec.Validate(JsonUtility.ToJson(envelope));

            envelope.formatVersion = SaveTransferEnvelope.CurrentFormatVersion;
            envelope.saveSchemaVersion = "99.0.0";
            var unsupportedSave = SaveTransferCodec.Validate(JsonUtility.ToJson(envelope));

            Assert.That(
                unsupportedEnvelope.Status,
                Is.EqualTo(SaveTransferValidationStatus.UnsupportedEnvelopeVersion));
            Assert.That(
                unsupportedSave.Status,
                Is.EqualTo(SaveTransferValidationStatus.UnsupportedSaveSchema));
        }

        [Test]
        public void InvalidBase64AndOversizedEnvelopeAreRejectedBeforeApply()
        {
            Assert.That(
                SaveTransferCodec.TrySerialize(
                    CreateSave("용량", 2, 0),
                    ExportedUtc,
                    out var envelopeJson,
                    out _),
                Is.True);
            var envelope = JsonUtility.FromJson<SaveTransferEnvelope>(envelopeJson);
            envelope.content = "not-base64";

            var invalidContent = SaveTransferCodec.Validate(JsonUtility.ToJson(envelope));
            var oversized = SaveTransferCodec.Validate(
                new string('x', SaveTransferCodec.MaximumEnvelopeBytes + 1));

            Assert.That(invalidContent.Status, Is.EqualTo(SaveTransferValidationStatus.InvalidContent));
            Assert.That(oversized.Status, Is.EqualTo(SaveTransferValidationStatus.EnvelopeTooLarge));
        }

        [Test]
        public void ImportSessionRequiresExactConfirmationAndUnchangedLocalSnapshot()
        {
            var local = CreateSave("로컬", 5, 50);
            Assert.That(
                SaveTransferCodec.TrySerialize(
                    CreateSave("가져오기", 11, 900),
                    ExportedUtc,
                    out var envelopeJson,
                    out _),
                Is.True);
            var session = new SaveTransferImportSession();

            var validation = session.Begin(envelopeJson, local);
            var wrongConfirmation = session.Authorize("import save", local);
            local.economy.milkCoins += 1;
            var changedLocal = session.Authorize(
                SaveTransferImportSession.ConfirmationPhrase,
                local);

            Assert.That(validation.IsValid, Is.True, validation.Message);
            Assert.That(
                wrongConfirmation.Status,
                Is.EqualTo(SaveTransferApplyAuthorizationStatus.ConfirmationMismatch));
            Assert.That(
                changedLocal.Status,
                Is.EqualTo(SaveTransferApplyAuthorizationStatus.LocalSaveChanged));
            Assert.That(session.HasPendingImport, Is.False);
        }

        [Test]
        public void AuthorizedPayloadUsesAtomicSaveReplacementAndKeepsPreviousBackup()
        {
            var saveObject = new GameObject("Web Save Transfer Isolated Test");
            var saveManager = saveObject.AddComponent<SaveManager>();
            var fileName = $"cheesetama_transfer_test_{Guid.NewGuid():N}.json";
            saveManager.SetIsolatedSaveFileNameForTests(fileName);
            try
            {
                saveManager.DeleteSave();
                var previous = CreateSave("이전", 6, 60);
                saveManager.Save(previous);

                Assert.That(
                    SaveTransferCodec.TrySerialize(
                        CreateSave("새 저장", 19, 990),
                        ExportedUtc,
                        out var envelopeJson,
                        out _),
                    Is.True);
                var validation = SaveTransferCodec.Validate(envelopeJson);

                var replaced = saveManager.TryReplaceFromCloudPayload(
                    validation.Payload,
                    out var restored);

                Assert.That(replaced, Is.True);
                Assert.That(restored.cheeseTama.name, Is.EqualTo("새 저장"));
                Assert.That(File.Exists(saveManager.BackupFilePath), Is.True);
                var backup = JsonUtility.FromJson<CheeseTamaSaveData>(
                    File.ReadAllText(saveManager.BackupFilePath));
                Assert.That(backup.cheeseTama.name, Is.EqualTo("이전"));
            }
            finally
            {
                saveManager.DeleteSave();
                UnityEngine.Object.DestroyImmediate(saveObject);
            }
        }

        [Test]
        public void BrowserPluginExposesDownloadAndPickerEntrypoints()
        {
            var pluginPath = Path.Combine(
                Application.dataPath,
                "_Project",
                "Plugins",
                "WebGL",
                "CheeseTamaSaveTransfer.jslib");
            var source = File.ReadAllText(pluginPath);

            Assert.That(source, Does.Contain("CheeseTamaDownloadSaveTransfer"));
            Assert.That(source, Does.Contain("CheeseTamaPickSaveTransfer"));
            Assert.That(source, Does.Contain("file.size > maximumBytes"));
            Assert.That(source, Does.Contain("readAsText(file, 'utf-8')"));
            Assert.That(source, Does.Contain("input.addEventListener('cancel'"));
            Assert.That(source, Does.Contain("window.addEventListener('focus'"));
        }

        private static CheeseTamaSaveData CreateSave(string name, int level, int coins)
        {
            var save = new CheeseTamaSaveData();
            save.EnsureRuntimeDefaults();
            save.cheeseTama.name = name;
            save.cheeseTama.hasCustomName = true;
            save.cheeseTama.level = level;
            save.cheeseTama.lastSavedAtIso = ExportedUtc.AddMinutes(-10).ToString("O");
            save.economy.milkCoins = coins;
            return save;
        }

        private static void AssertUnsafeSaveData(CheeseTamaSaveData save)
        {
            Assert.That(
                SaveTransferCodec.TrySerialize(save, ExportedUtc, out var envelopeJson, out var errorMessage),
                Is.True,
                errorMessage);

            var validation = SaveTransferCodec.Validate(envelopeJson);
            Assert.That(validation.Status, Is.EqualTo(SaveTransferValidationStatus.UnsafeSaveData));
        }

        private static string BuildEnvelopeJson(string contentJson, string schemaVersion)
        {
            var envelope = new SaveTransferEnvelope
            {
                formatVersion = SaveTransferEnvelope.CurrentFormatVersion,
                exportedUtcIso = ExportedUtc.ToString("O"),
                saveSchemaVersion = schemaVersion,
                contentEncoding = SaveTransferEnvelope.Utf8Base64Encoding,
                contentHash = CloudSavePayload.ComputeContentHash(contentJson),
                content = Convert.ToBase64String(Encoding.UTF8.GetBytes(contentJson)),
                revision = ExportedUtc.UtcDateTime.Ticks,
                modifiedUtcTicks = ExportedUtc.UtcDateTime.Ticks
            };
            return JsonUtility.ToJson(envelope);
        }
    }
}
