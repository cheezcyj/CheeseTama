using System;
using CheeseTama.Gameplay.Journey;
using CheeseTama.Save;
using NUnit.Framework;

namespace CheeseTama.Tests
{
    public sealed class FirstDayJourneyFeatureTests
    {
        private static readonly DateTimeOffset Now =
            new DateTimeOffset(2026, 8, 14, 9, 0, 0, TimeSpan.FromHours(9));

        [Test]
        public void NewJourneyCompletesFromActionsInAnyOrder()
        {
            var state = FirstDayJourneySaveData.CreateForNewPlayer();

            Assert.That(FirstDayJourneySystem.TryRecordCareAction(state, "play", Now), Is.True);
            Assert.That(FirstDayJourneySystem.TryRecordCareAction(state, "cook", Now), Is.True);
            Assert.That(FirstDayJourneySystem.TryRecordCareAction(state, "feed_snack", Now), Is.True);
            Assert.That(FirstDayJourneySystem.TryRecordCareAction(state, "rest", Now), Is.True);
            Assert.That(FirstDayJourneySystem.TryRecordCollectionOpened(state, Now), Is.True);
            Assert.That(FirstDayJourneySystem.TryRecordCareAction(state, "clean", Now), Is.True);

            Assert.That(state.completed, Is.True);
            Assert.That(FirstDayJourneySystem.CountCompletedTasks(state), Is.EqualTo(6));
            Assert.That(state.completedAtIso, Is.Not.Empty);
        }

        [TestCase("feed_milk")]
        [TestCase("feed_warm_milk")]
        [TestCase("feed_star_milk")]
        [TestCase("feed_snack")]
        public void FeedingAliasesShareOneTask(string actionId)
        {
            var state = FirstDayJourneySaveData.CreateForNewPlayer();

            Assert.That(FirstDayJourneySystem.TryRecordCareAction(state, actionId, Now), Is.True);
            Assert.That(FirstDayJourneySystem.TryRecordCareAction(state, "feed_milk", Now), Is.False);
            Assert.That(FirstDayJourneySystem.CountCompletedTasks(state), Is.EqualTo(1));
        }

        [Test]
        public void UnknownAndDuplicateActionsDoNotChangeProgress()
        {
            var state = FirstDayJourneySaveData.CreateForNewPlayer();

            Assert.That(FirstDayJourneySystem.TryRecordCareAction(state, "pet", Now), Is.False);
            Assert.That(FirstDayJourneySystem.TryRecordCareAction(state, "play", Now), Is.True);
            Assert.That(FirstDayJourneySystem.TryRecordCareAction(state, "play", Now), Is.False);
            Assert.That(FirstDayJourneySystem.CountCompletedTasks(state), Is.EqualTo(1));
        }

        [Test]
        public void CompletionRewardIsGrantedExactlyOnce()
        {
            var state = FirstDayJourneySaveData.CreateForNewPlayer();
            foreach (var task in FirstDayJourneySystem.Tasks)
            {
                FirstDayJourneySystem.TryCompleteTask(state, task.Id, Now);
            }

            var first = FirstDayJourneySystem.ClaimCompletionReward(state);
            var second = FirstDayJourneySystem.ClaimCompletionReward(state);

            Assert.That(first.Granted, Is.True);
            Assert.That(first.MilkCoins, Is.EqualTo(20));
            Assert.That(first.MilkDrops, Is.EqualTo(5));
            Assert.That(first.CollectionFragments, Is.EqualTo(1));
            Assert.That(second.Granted, Is.False);
        }

        [Test]
        public void LegacyJourneyNeverSurfacesOrPaysAgain()
        {
            var state = FirstDayJourneySaveData.CreateCompletedForLegacySave();

            Assert.That(FirstDayJourneySystem.TryRecordCareAction(state, "play", Now), Is.False);
            Assert.That(FirstDayJourneySystem.ClaimCompletionReward(state).Granted, Is.False);
            Assert.That(state.completed, Is.True);
            Assert.That(state.rewardClaimed, Is.True);
        }

        [Test]
        public void RuntimeDefaultsRemoveUnknownAndDuplicateTaskIds()
        {
            var state = FirstDayJourneySaveData.CreateForNewPlayer();
            state.schemaVersion = 0;
            state.completedTaskIds.Add(FirstDayJourneySystem.PlayTaskId);
            state.completedTaskIds.Add("unknown");
            state.completedTaskIds.Add(FirstDayJourneySystem.PlayTaskId);

            Assert.That(state.EnsureRuntimeDefaults(), Is.True);
            Assert.That(state.schemaVersion, Is.EqualTo(FirstDayJourneySaveData.CurrentSchemaVersion));
            Assert.That(state.completedTaskIds, Is.EqualTo(new[] { FirstDayJourneySystem.PlayTaskId }));
        }
    }
}
