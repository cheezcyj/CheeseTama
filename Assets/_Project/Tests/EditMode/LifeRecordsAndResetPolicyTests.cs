using System;
using System.Collections.Generic;
using CheeseTama.Gameplay.NpcVisits;
using CheeseTama.Gameplay.Records;
using CheeseTama.Gameplay.Reset;
using CheeseTama.Save;
using CheeseTama.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.Tests.EditMode
{
    public sealed class LifeRecordsAndResetPolicyTests
    {
        [Test]
        public void NullAndMissingFieldsProduceEmptyPublicSnapshot()
        {
            var system = new LifeRecordsSystem();

            var empty = system.BuildSnapshot(null);
            Assert.That(empty, Is.Not.Null);
            Assert.That(empty.HasAnyRecord, Is.False);
            Assert.That(empty.BouncyJump.HighestScore, Is.Zero);
            Assert.That(empty.Keepsakes, Is.Empty);
            Assert.That(empty.CompletedEpisodes, Is.Empty);

            var partialLegacy = new CheeseTamaSaveData
            {
                playMiniGames = null,
                sleepSchedule = null,
                npcRelationshipEpisodes = null
            };
            var partial = system.BuildSnapshot(partialLegacy);
            Assert.That(partial.HasAnyRecord, Is.False);
            Assert.That(partial.HasRecentSleep, Is.False);
        }

        [Test]
        public void SnapshotPublishesEarnedRecordsAndHidesUnknownOrUnearnedContent()
        {
            var save = BuildRecordedSave();
            var snapshot = new LifeRecordsSystem().BuildSnapshot(save);

            Assert.That(snapshot.BouncyJump.HighestScore, Is.EqualTo(48));
            Assert.That(snapshot.BouncyJump.TotalSessions, Is.EqualTo(3));
            Assert.That(snapshot.BouncyJump.TotalSuccesses, Is.EqualTo(99));

            Assert.That(snapshot.HasRecentSleep, Is.True);
            Assert.That(snapshot.Sleeps, Has.Count.EqualTo(2));
            Assert.That(snapshot.RecentSleep.ReceiptKey, Is.EqualTo("sleep-new"));
            Assert.That(snapshot.Sleeps[1].ReceiptKey, Is.EqualTo("sleep-old"));
            Assert.That(snapshot.RecentSleep.ScheduledHours, Is.EqualTo(8));
            Assert.That(snapshot.RecentSleep.ElapsedMinutes, Is.EqualTo(480));
            Assert.That(snapshot.RecentSleep.SleepinessDelta, Is.EqualTo(-100));
            Assert.That(snapshot.RecentSleep.HealthDelta, Is.EqualTo(100));

            Assert.That(snapshot.CompletedEpisodes, Has.Count.EqualTo(2));
            Assert.That(
                snapshot.CompletedEpisodes[0].EpisodeId,
                Is.EqualTo(NpcRelationshipEpisodeIds.DoctorFriend));
            Assert.That(snapshot.CompletedEpisodes[0].ChoiceLabel, Is.EqualTo("웃는 얼굴 표 만들기"));
            Assert.That(
                snapshot.CompletedEpisodes[1].EpisodeId,
                Is.EqualTo(NpcRelationshipEpisodeIds.CatFriend));
            Assert.That(snapshot.CompletedEpisodes[1].NpcDisplayName, Is.EqualTo("밀크냥"));

            Assert.That(snapshot.Keepsakes, Has.Count.EqualTo(2));
            Assert.That(snapshot.Keepsakes[0].Title, Is.EqualTo("건강 수첩"));
            Assert.That(snapshot.Keepsakes[1].Title, Is.EqualTo("발자국 지도"));
            Assert.That(
                HasValue(
                    snapshot.Keepsakes,
                    record => record.KeepsakeId == NpcRelationshipKeepsakeIds.DoctorSmallStethoscope),
                Is.False,
                "A known but unearned keepsake must not appear as a locked or named entry.");
            Assert.That(
                HasValue(snapshot.CompletedEpisodes, record => record.EpisodeId == "future_episode"),
                Is.False);
        }

        [Test]
        public void PartialLegacyReceiptRepairsPresentationWithoutMutatingSave()
        {
            var save = new CheeseTamaSaveData
            {
                npcRelationshipEpisodes = new NpcRelationshipEpisodeSaveData
                {
                    completedEpisodeIds = null,
                    keepsakeIds = null,
                    receipts = new List<NpcRelationshipEpisodeReceiptSaveData>
                    {
                        new NpcRelationshipEpisodeReceiptSaveData
                        {
                            receiptId = "legacy-receipt",
                            episodeId = NpcRelationshipEpisodeIds.FairyFriend,
                            npcId = NpcVisitSystem.FermentationFairyId,
                            choiceId = "choose_soft_aroma",
                            completedAtIso = "2026-08-18T10:00:00+09:00"
                        }
                    }
                }
            };
            var before = JsonUtility.ToJson(save);
            var system = new LifeRecordsSystem();

            var first = system.BuildSnapshot(save);
            var second = system.BuildSnapshot(save);

            Assert.That(JsonUtility.ToJson(save), Is.EqualTo(before));
            Assert.That(first.CompletedEpisodes, Has.Count.EqualTo(1));
            Assert.That(first.Keepsakes, Has.Count.EqualTo(1));
            Assert.That(first.Keepsakes[0].Title, Is.EqualTo("향기 주머니"));
            Assert.That(
                second.CompletedEpisodes[0].EpisodeId,
                Is.EqualTo(first.CompletedEpisodes[0].EpisodeId));
            Assert.That(
                second.Keepsakes[0].KeepsakeId,
                Is.EqualTo(first.Keepsakes[0].KeepsakeId));
        }

        [Test]
        public void ExistingSaveJsonRoundTripKeepsAlbumPresentationStable()
        {
            var source = BuildRecordedSave();
            var restored = JsonUtility.FromJson<CheeseTamaSaveData>(
                JsonUtility.ToJson(source));
            var system = new LifeRecordsSystem();
            var before = system.BuildSnapshot(source);
            var after = system.BuildSnapshot(restored);

            Assert.That(after.BouncyJump.HighestScore, Is.EqualTo(before.BouncyJump.HighestScore));
            Assert.That(after.RecentSleep.ReceiptKey, Is.EqualTo(before.RecentSleep.ReceiptKey));
            Assert.That(after.Sleeps.Count, Is.EqualTo(before.Sleeps.Count));
            Assert.That(after.Keepsakes.Count, Is.EqualTo(before.Keepsakes.Count));
            Assert.That(after.CompletedEpisodes.Count, Is.EqualTo(before.CompletedEpisodes.Count));
            Assert.That(
                after.CompletedEpisodes[0].ChoiceId,
                Is.EqualTo(before.CompletedEpisodes[0].ChoiceId));
        }

        [Test]
        public void AlbumPanelReplaysOnlyCompletedEpisodesAndDoesNotDuplicateListeners()
        {
            var host = new GameObject("Life Records Test Host");
            var root = new GameObject(
                LifeRecordsPanelController.OverlayObjectName,
                typeof(RectTransform),
                typeof(Image));
            root.transform.SetParent(host.transform);
            root.SetActive(false);

            try
            {
                var controller = host.AddComponent<LifeRecordsPanelController>();
                var title = CreateText(root.transform, "Title");
                var overview = CreateText(root.transform, "Overview");
                var episode = CreateText(root.transform, "Episode");
                var position = CreateText(root.transform, "Position");
                var previous = CreateButton(root.transform, "Previous");
                var next = CreateButton(root.transform, "Next");
                var close = CreateButton(root.transform, "Close");
                var snapshot = new LifeRecordsSystem().BuildSnapshot(BuildRecordedSave());
                var closeCount = 0;
                var blockingStates = new List<bool>();

                Configure(
                    controller,
                    root,
                    title,
                    overview,
                    episode,
                    position,
                    previous,
                    next,
                    close,
                    snapshot,
                    () => closeCount += 1,
                    blocked => blockingStates.Add(blocked));
                Configure(
                    controller,
                    root,
                    title,
                    overview,
                    episode,
                    position,
                    previous,
                    next,
                    close,
                    snapshot,
                    () => closeCount += 1,
                    blocked => blockingStates.Add(blocked));

                Assert.That(controller.Open(), Is.True);
                Assert.That(controller.BlocksGameplayInput, Is.True);
                Assert.That(title.text, Is.EqualTo("생활 기록 앨범"));
                Assert.That(overview.text, Does.Contain("최고 48점"));
                Assert.That(overview.text, Does.Contain("건강 수첩"));
                Assert.That(overview.text, Does.Not.Contain("작은 청진기"));
                Assert.That(episode.text, Does.Contain("친구가 된 날의 건강 수첩"));

                next.onClick.Invoke();
                Assert.That(controller.SelectedEpisodeIndex, Is.EqualTo(1));
                Assert.That(episode.text, Does.Contain("나란히 그린 발자국 지도"));
                Assert.That(episode.text, Does.Not.Contain("비밀 친구의 별 나침반"));

                close.onClick.Invoke();
                Assert.That(controller.BlocksGameplayInput, Is.False);
                Assert.That(closeCount, Is.EqualTo(1));
                Assert.That(blockingStates, Is.EqualTo(new[] { true, false }));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void CareProgressPreviewPreservesAccountProgressAndResetsCurrentCareOnly()
        {
            var preview = ProgressResetPolicy.BuildPreview(
                ProgressResetMode.CareProgressOnly);

            Assert.That(preview.IsSupported, Is.True);
            Assert.That(preview.IsDestructive, Is.True);
            Assert.That(preview.Title, Is.EqualTo("육성만 새로 시작"));
            Assert.That(preview.Preserves(ProgressResetDataCategory.PlayerIdentity), Is.True);
            Assert.That(preview.Preserves(ProgressResetDataCategory.GameSettings), Is.True);
            Assert.That(
                preview.Preserves(ProgressResetDataCategory.MilkroomThemeAndDecorations),
                Is.True);
            Assert.That(
                preview.Resets(ProgressResetDataCategory.CheeseTamaGrowthAndCare),
                Is.True);
            Assert.That(preview.Preserves(ProgressResetDataCategory.InventoryEconomyAndUnlocks), Is.True);
            Assert.That(preview.Preserves(ProgressResetDataCategory.CollectionsAndLifeRecords), Is.True);
            Assert.That(preview.Preserves(ProgressResetDataCategory.JourneysNpcAndStoryProgress), Is.True);
            Assert.That(preview.Resets(ProgressResetDataCategory.CheeseTamaGrowthAndCare), Is.True);
            Assert.That(preview.Resets(ProgressResetDataCategory.ActiveSessionsAndPendingEvents), Is.True);
            Assert.That(
                ProgressResetPolicy.MatchesConfirmation(preview, " RESET TAMA "),
                Is.True);
            Assert.That(
                ProgressResetPolicy.MatchesConfirmation(preview, "reset tama"),
                Is.False);
            Assert.That(
                ProgressResetPolicy.BuildSummary(preview),
                Does.Contain("보존: 로컬 플레이어 식별 정보"));
        }

        [Test]
        public void FullResetPreviewResetsEveryKnownCategory()
        {
            var preview = ProgressResetPolicy.BuildPreview(
                ProgressResetMode.FullLocalData);

            Assert.That(preview.IsSupported, Is.True);
            Assert.That(preview.PreservedCategories, Is.Empty);
            Assert.That(
                preview.ResetCategories.Count,
                Is.EqualTo(Enum.GetValues(typeof(ProgressResetDataCategory)).Length));
            Assert.That(preview.Resets(ProgressResetDataCategory.GameSettings), Is.True);
            Assert.That(
                preview.Resets(ProgressResetDataCategory.MilkroomThemeAndDecorations),
                Is.True);
            Assert.That(
                ProgressResetPolicy.MatchesConfirmation(preview, "RESET ALL"),
                Is.True);
        }

        [Test]
        public void UnknownResetModeFailsClosedWithoutDeletionPlan()
        {
            var preview = ProgressResetPolicy.BuildPreview((ProgressResetMode)999);

            Assert.That(preview.IsSupported, Is.False);
            Assert.That(preview.IsDestructive, Is.False);
            Assert.That(preview.PreservedCategories, Is.Empty);
            Assert.That(preview.ResetCategories, Is.Empty);
            Assert.That(preview.ConfirmationPhrase, Is.Empty);
            Assert.That(ProgressResetPolicy.MatchesConfirmation(preview, string.Empty), Is.False);
            Assert.That(
                ProgressResetPolicy.BuildSummary(preview),
                Is.EqualTo("지원하지 않는 초기화 방식이라 실행할 수 없습니다."));
        }

        [Test]
        public void ResetPreviewAndResultContractsAreRepeatableAndReadOnly()
        {
            var first = ProgressResetPolicy.BuildPreview(
                ProgressResetMode.CareProgressOnly);
            var second = ProgressResetPolicy.BuildPreview(
                ProgressResetMode.CareProgressOnly);

            Assert.That(second.ConfirmationPhrase, Is.EqualTo(first.ConfirmationPhrase));
            Assert.That(second.ResetCategories, Is.EqualTo(first.ResetCategories));
            Assert.That(
                () => ((IList<ProgressResetDataCategory>)first.ResetCategories).Add(
                    ProgressResetDataCategory.GameSettings),
                Throws.TypeOf<NotSupportedException>());

            var applied = ProgressResetResult.CreateApplied(first, true, "완료");
            var noChanges = ProgressResetResult.CreateApplied(first, false);
            var failed = ProgressResetResult.CreateFailure(
                ProgressResetResultStatus.ConfirmationMismatch,
                first,
                "확인 문구 불일치");

            Assert.That(applied.Status, Is.EqualTo(ProgressResetResultStatus.Applied));
            Assert.That(applied.Succeeded, Is.True);
            Assert.That(applied.StateChanged, Is.True);
            Assert.That(noChanges.Status, Is.EqualTo(ProgressResetResultStatus.NoChanges));
            Assert.That(noChanges.Succeeded, Is.True);
            Assert.That(noChanges.StateChanged, Is.False);
            Assert.That(failed.Succeeded, Is.False);
            Assert.That(failed.Message, Is.EqualTo("확인 문구 불일치"));
        }

        private static CheeseTamaSaveData BuildRecordedSave()
        {
            return new CheeseTamaSaveData
            {
                playMiniGames = new PlayMiniGameSaveData
                {
                    highestBouncyJumpScore = 48,
                    totalBouncyJumpSessions = 3,
                    totalBouncyJumpSuccesses = 99
                },
                sleepSchedule = new SleepScheduleSaveData
                {
                    recoveryReceipts = new List<SleepRecoveryReceiptSaveEntry>
                    {
                        new SleepRecoveryReceiptSaveEntry
                        {
                            receiptKey = "sleep-old",
                            claimedAtIso = "2026-08-17T10:00:00+09:00",
                            scheduledHours = 2,
                            elapsedMinutes = 120
                        },
                        null,
                        new SleepRecoveryReceiptSaveEntry
                        {
                            receiptKey = "  ",
                            claimedAtIso = "2026-08-19T10:00:00+09:00"
                        },
                        new SleepRecoveryReceiptSaveEntry
                        {
                            receiptKey = "sleep-new",
                            claimedAtIso = "2026-08-18T10:00:00+09:00",
                            scheduledHours = 99,
                            elapsedMinutes = 999,
                            sleepinessDelta = -999,
                            healthDelta = 999,
                            moodDelta = 4,
                            wasEarlyWake = true
                        }
                    }
                },
                npcRelationshipEpisodes = new NpcRelationshipEpisodeSaveData
                {
                    completedEpisodeIds = new List<string>
                    {
                        "future_episode",
                        NpcRelationshipEpisodeIds.DoctorFriend,
                        NpcRelationshipEpisodeIds.DoctorFriend
                    },
                    keepsakeIds = new List<string>
                    {
                        "future_keepsake",
                        NpcRelationshipKeepsakeIds.DoctorHealthNotebook
                    },
                    receipts = new List<NpcRelationshipEpisodeReceiptSaveData>
                    {
                        new NpcRelationshipEpisodeReceiptSaveData
                        {
                            receiptId = "doctor-receipt",
                            episodeId = NpcRelationshipEpisodeIds.DoctorFriend,
                            npcId = NpcVisitSystem.MilkyDoctorId,
                            choiceId = "draw_smile_chart",
                            completedAtIso = "2026-08-17T09:00:00+09:00"
                        },
                        new NpcRelationshipEpisodeReceiptSaveData
                        {
                            receiptId = "future-receipt",
                            episodeId = "future_episode",
                            npcId = "future_npc",
                            choiceId = "future_choice",
                            completedAtIso = "2026-08-18T09:00:00+09:00"
                        },
                        new NpcRelationshipEpisodeReceiptSaveData
                        {
                            receiptId = "cat-receipt",
                            episodeId = NpcRelationshipEpisodeIds.CatFriend,
                            npcId = NpcVisitSystem.MilkCatId,
                            choiceId = "mark_hidden_path",
                            completedAtIso = "2026-08-18T11:00:00+09:00"
                        }
                    }
                }
            };
        }

        private static bool HasValue<T>(
            IReadOnlyList<T> values,
            Func<T, bool> predicate)
        {
            for (var index = 0; index < values.Count; index += 1)
            {
                if (predicate(values[index]))
                {
                    return true;
                }
            }

            return false;
        }

        private static void Configure(
            LifeRecordsPanelController controller,
            GameObject root,
            Text title,
            Text overview,
            Text episode,
            Text position,
            Button previous,
            Button next,
            Button close,
            LifeRecordsSnapshot snapshot,
            Action closed,
            Action<bool> blockingChanged)
        {
            controller.Configure(
                root,
                title,
                overview,
                episode,
                position,
                previous,
                next,
                close,
                () => snapshot,
                closed,
                blockingChanged);
        }

        private static Text CreateText(Transform parent, string objectName)
        {
            var gameObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(Text));
            gameObject.transform.SetParent(parent);
            return gameObject.GetComponent<Text>();
        }

        private static Button CreateButton(Transform parent, string objectName)
        {
            var gameObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            gameObject.transform.SetParent(parent);
            return gameObject.GetComponent<Button>();
        }
    }
}
