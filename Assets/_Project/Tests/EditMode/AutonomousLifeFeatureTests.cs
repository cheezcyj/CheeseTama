using System;
using System.Collections.Generic;
using CheeseTama.Gameplay.Autonomy;
using CheeseTama.Gameplay.Decorations;
using CheeseTama.Gameplay.NewGameSetup;
using CheeseTama.Save;
using CheeseTama.UI;
using NUnit.Framework;
using UnityEngine;

namespace CheeseTama.Tests
{
    public sealed class AutonomousLifeFeatureTests
    {
        private static readonly DateTimeOffset FirstDiscoveryAt =
            new DateTimeOffset(2026, 8, 14, 15, 20, 0, TimeSpan.FromHours(9));

        [Test]
        public void IdleDelayIsAlwaysBetweenFortyFiveAndNinetySeconds()
        {
            var system = new AutonomousLifeSystem();

            Assert.That(
                system.ResolveIdleDelay(-10f),
                Is.EqualTo(AutonomousLifeSystem.MinimumIdleDelaySeconds));
            Assert.That(system.ResolveIdleDelay(0.5f), Is.EqualTo(67.5f));
            Assert.That(
                system.ResolveIdleDelay(10f),
                Is.EqualTo(AutonomousLifeSystem.MaximumIdleDelaySeconds));
        }

        [Test]
        public void NeutralHatchedContextAllowsAllSixFixedAnchorBehaviours()
        {
            var system = new AutonomousLifeSystem();
            var context = AutonomousLifeContext.CreateNeutral(
                14,
                AutonomousLifeAnchorMask.All);

            Assert.That(AutonomousLifeBehaviourCatalog.All.Count, Is.EqualTo(6));
            for (var index = 0; index < AutonomousLifeBehaviourCatalog.All.Count; index += 1)
            {
                var behaviour = AutonomousLifeBehaviourCatalog.All[index];
                Assert.That(
                    system.GetWeight(behaviour, context),
                    Is.GreaterThan(0f),
                    $"{behaviour} should have a neutral selection weight.");
            }
        }

        [Test]
        public void StateTimeTemperamentAndDecorationsChangeExpectedWeights()
        {
            var system = new AutonomousLifeSystem();
            var neutral = CreateContext(
                hour: 14,
                traitId: NewGameSetupCatalog.BalancedTraitId);
            var sleepyNight = CreateContext(
                hour: 23,
                sleepiness: 95,
                traitId: NewGameSetupCatalog.CalmTraitId,
                bedsideId: DecorationCatalog.StarPlushId);
            var expressiveEvening = CreateContext(
                hour: 19,
                mood: 95,
                traitId: NewGameSetupCatalog.ExpressiveTraitId,
                accentId: DecorationCatalog.StarLampId);
            var focusedShelf = CreateContext(
                hour: 9,
                traitId: NewGameSetupCatalog.FocusedTraitId,
                shelfId: DecorationCatalog.MemoryFrameId);
            var livelyPlay = CreateContext(
                hour: 14,
                traitId: NewGameSetupCatalog.LivelyTraitId,
                floorId: DecorationCatalog.CloudMatId);
            var decoratedWindow = CreateContext(
                hour: 23,
                traitId: NewGameSetupCatalog.CalmTraitId,
                windowId: DecorationCatalog.MoonCurtainId);

            Assert.That(
                system.GetWeight(AutonomousLifeBehaviour.Nap, sleepyNight),
                Is.GreaterThan(system.GetWeight(AutonomousLifeBehaviour.Nap, neutral)));
            Assert.That(
                system.GetWeight(AutonomousLifeBehaviour.Dance, expressiveEvening),
                Is.GreaterThan(system.GetWeight(AutonomousLifeBehaviour.Dance, neutral)));
            Assert.That(
                system.GetWeight(AutonomousLifeBehaviour.Shelf, focusedShelf),
                Is.GreaterThan(system.GetWeight(AutonomousLifeBehaviour.Shelf, neutral)));
            Assert.That(
                system.GetWeight(AutonomousLifeBehaviour.Play, livelyPlay),
                Is.GreaterThan(system.GetWeight(AutonomousLifeBehaviour.Play, neutral)));
            Assert.That(
                system.GetWeight(AutonomousLifeBehaviour.Window, decoratedWindow),
                Is.GreaterThan(system.GetWeight(AutonomousLifeBehaviour.Window, neutral)));
        }

        [Test]
        public void EggNeverSelectsShelfPlayOrDance()
        {
            var system = new AutonomousLifeSystem();
            var egg = new AutonomousLifeContext(
                12,
                false,
                80,
                80,
                90,
                20,
                100,
                NewGameSetupCatalog.LivelyTraitId,
                DecorationCatalog.CloudMatId,
                DecorationCatalog.StarLampId,
                DecorationCatalog.CreamCurtainId,
                DecorationCatalog.MemoryFrameId,
                DecorationCatalog.StarPlushId,
                AutonomousLifeAnchorMask.All);

            Assert.That(system.GetWeight(AutonomousLifeBehaviour.Shelf, egg), Is.Zero);
            Assert.That(system.GetWeight(AutonomousLifeBehaviour.Play, egg), Is.Zero);
            Assert.That(system.GetWeight(AutonomousLifeBehaviour.Dance, egg), Is.Zero);
            Assert.That(system.GetWeight(AutonomousLifeBehaviour.Idle, egg), Is.GreaterThan(0f));
        }

        [Test]
        public void SessionAllowsAtMostTwoAndRejectsImmediateRepetition()
        {
            var system = new AutonomousLifeSystem();
            var session = new AutonomousLifeSessionState();
            var context = CreateContext(
                hour: 14,
                traitId: NewGameSetupCatalog.LivelyTraitId,
                available: AutonomousLifeAnchorMask.Play | AutonomousLifeAnchorMask.Dance);

            var first = system.TrySelectAndStart(context, session, false, 0f, 0f);
            var second = system.TrySelectAndStart(context, session, false, 0f, 0f);
            var third = system.TrySelectAndStart(context, session, false, 0f, 0f);

            Assert.That(first.Status, Is.EqualTo(AutonomousLifeSelectionStatus.Selected));
            Assert.That(second.Status, Is.EqualTo(AutonomousLifeSelectionStatus.Selected));
            Assert.That(second.Behaviour, Is.Not.EqualTo(first.Behaviour));
            Assert.That(session.StartedBehaviourCount, Is.EqualTo(2));
            Assert.That(session.IsExhausted, Is.True);
            Assert.That(
                third.Status,
                Is.EqualTo(AutonomousLifeSelectionStatus.SessionLimitReached));
        }

        [Test]
        public void BlockedSelectionNeverConsumesSessionAllowance()
        {
            var system = new AutonomousLifeSystem();
            var session = new AutonomousLifeSessionState();

            var result = system.TrySelectAndStart(
                AutonomousLifeContext.CreateNeutral(12),
                session,
                true,
                0f,
                0f);

            Assert.That(
                result.Status,
                Is.EqualTo(AutonomousLifeSelectionStatus.InteractionBlocked));
            Assert.That(session.StartedBehaviourCount, Is.Zero);
            Assert.That(session.HasLastBehaviour, Is.False);
        }

        [Test]
        public void FirstDiscoveryIsStableAcrossDuplicateAndJsonRoundTrip()
        {
            var system = new AutonomousLifeSystem();
            var save = new AutonomousLifeSaveData();

            var first = system.RecordFirstDiscovery(
                save,
                AutonomousLifeBehaviour.Window,
                FirstDiscoveryAt);
            var duplicate = system.RecordFirstDiscovery(
                save,
                AutonomousLifeBehaviour.Window,
                FirstDiscoveryAt.AddDays(3));

            Assert.That(first.Status, Is.EqualTo(AutonomousLifeDiscoveryStatus.Recorded));
            Assert.That(
                duplicate.Status,
                Is.EqualTo(AutonomousLifeDiscoveryStatus.AlreadyRecorded));
            Assert.That(save.firstDiscoveries, Has.Count.EqualTo(1));
            Assert.That(
                save.firstDiscoveries[0].firstDiscoveredAtIso,
                Is.EqualTo(FirstDiscoveryAt.ToString("O")));

            var roundTrip = JsonUtility.FromJson<AutonomousLifeSaveData>(
                JsonUtility.ToJson(save));
            Assert.That(roundTrip.EnsureRuntimeDefaults(), Is.False);
            Assert.That(roundTrip.HasDiscovered(AutonomousLifeBehaviourCatalog.WindowId), Is.True);
            Assert.That(
                roundTrip.Find(AutonomousLifeBehaviourCatalog.WindowId).firstDiscoveredAtIso,
                Is.EqualTo(FirstDiscoveryAt.ToString("O")));
        }

        [Test]
        public void SaveContractNormalizesUnknownDuplicateAndNullEntries()
        {
            var save = new AutonomousLifeSaveData
            {
                schemaVersion = -4,
                firstDiscoveries = new List<AutonomousLifeDiscoverySaveEntry>
                {
                    new AutonomousLifeDiscoverySaveEntry
                    {
                        behaviourId = " play ",
                        firstDiscoveredAtIso = " first "
                    },
                    new AutonomousLifeDiscoverySaveEntry
                    {
                        behaviourId = "play",
                        firstDiscoveredAtIso = "duplicate"
                    },
                    new AutonomousLifeDiscoverySaveEntry
                    {
                        behaviourId = "unknown",
                        firstDiscoveredAtIso = "ignored"
                    },
                    null,
                    new AutonomousLifeDiscoverySaveEntry
                    {
                        behaviourId = " nap ",
                        firstDiscoveredAtIso = null
                    }
                }
            };

            Assert.That(save.EnsureRuntimeDefaults(), Is.True);
            Assert.That(save.schemaVersion, Is.EqualTo(AutonomousLifeSaveData.CurrentSchemaVersion));
            Assert.That(save.firstDiscoveries, Has.Count.EqualTo(2));
            Assert.That(save.firstDiscoveries[0].behaviourId, Is.EqualTo("play"));
            Assert.That(save.firstDiscoveries[0].firstDiscoveredAtIso, Is.EqualTo("first"));
            Assert.That(save.firstDiscoveries[1].behaviourId, Is.EqualTo("nap"));
            Assert.That(save.firstDiscoveries[1].firstDiscoveredAtIso, Is.Empty);
        }

        [Test]
        public void PresenterUsesFixedHorizontalTweenAndInterruptsImmediately()
        {
            var character = new GameObject("Autonomous Character");
            var idle = new GameObject("Autonomous Idle Anchor");
            var play = new GameObject("Autonomous Play Anchor");
            var presenterHost = new GameObject("Autonomous Presenter");
            var save = new AutonomousLifeSaveData();
            var rolls = new Queue<float>(new[] { 0f, 0.999f, 0f, 0.5f });
            var blocked = false;
            var started = new List<AutonomousLifeBehaviour>();
            var ended = new List<(AutonomousLifeBehaviour behaviour, bool interrupted)>();
            var persistCount = 0;

            try
            {
                character.transform.position = new Vector3(0f, 0.74f, 0f);
                idle.transform.position = new Vector3(0f, -4f, 0f);
                play.transform.position = new Vector3(4f, -8f, 2f);

                var presenter = presenterHost.AddComponent<AutonomousLifePresenter>();
                presenter.Configure(
                    character.transform,
                    new AutonomousLifeAnchorBindings(
                        idle.transform,
                        null,
                        null,
                        null,
                        play.transform,
                        null),
                    new AutonomousLifePresenterCallbacks(
                        () => CreateContext(
                            14,
                            traitId: NewGameSetupCatalog.LivelyTraitId),
                        () => save,
                        _ => persistCount += 1,
                        () => blocked,
                        behaviour => started.Add(behaviour),
                        (behaviour, interrupted) => ended.Add((behaviour, interrupted)),
                        nowProvider: () => FirstDiscoveryAt,
                        random01Provider: () => rolls.Count > 0 ? rolls.Dequeue() : 0.5f));

                Assert.That(presenter.Phase, Is.EqualTo(AutonomousLifePresentationPhase.Waiting));
                Assert.That(
                    presenter.SecondsUntilNextBehaviour,
                    Is.EqualTo(AutonomousLifeSystem.MinimumIdleDelaySeconds));

                presenter.Tick(AutonomousLifeSystem.MinimumIdleDelaySeconds);
                Assert.That(presenter.CurrentBehaviour, Is.EqualTo(AutonomousLifeBehaviour.Play));
                Assert.That(presenter.Phase, Is.EqualTo(AutonomousLifePresentationPhase.MovingToAnchor));
                Assert.That(started, Is.EqualTo(new[] { AutonomousLifeBehaviour.Play }));

                presenter.Tick(0.5f);
                Assert.That(character.transform.position.x, Is.InRange(0.01f, 3.99f));
                Assert.That(character.transform.position.y, Is.EqualTo(0.74f).Within(0.0001f));

                presenter.Tick(2f);
                Assert.That(presenter.Phase, Is.EqualTo(AutonomousLifePresentationPhase.Performing));
                Assert.That(character.transform.position.x, Is.EqualTo(4f).Within(0.0001f));
                Assert.That(character.transform.position.z, Is.EqualTo(2f).Within(0.0001f));
                Assert.That(character.transform.position.y, Is.EqualTo(0.74f).Within(0.0001f));
                Assert.That(persistCount, Is.EqualTo(1));
                Assert.That(save.HasDiscovered(AutonomousLifeBehaviourCatalog.PlayId), Is.True);

                blocked = true;
                presenter.Tick(0f);
                Assert.That(presenter.IsActive, Is.False);
                Assert.That(presenter.Phase, Is.EqualTo(AutonomousLifePresentationPhase.Waiting));
                Assert.That(character.transform.position, Is.EqualTo(new Vector3(0f, 0.74f, 0f)));
                Assert.That(ended, Has.Count.EqualTo(1));
                Assert.That(ended[0].behaviour, Is.EqualTo(AutonomousLifeBehaviour.Play));
                Assert.That(ended[0].interrupted, Is.True);

                var pausedCountdown = presenter.SecondsUntilNextBehaviour;
                presenter.Tick(100f);
                Assert.That(presenter.SecondsUntilNextBehaviour, Is.EqualTo(pausedCountdown));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(presenterHost);
                UnityEngine.Object.DestroyImmediate(play);
                UnityEngine.Object.DestroyImmediate(idle);
                UnityEngine.Object.DestroyImmediate(character);
            }
        }

        [Test]
        public void PresenterVisualMappingUsesExistingShortReactions()
        {
            Assert.That(
                AutonomousLifePresenter.ResolveVisualAction(AutonomousLifeBehaviour.Idle),
                Is.EqualTo(CheeseTamaVisualAction.Neutral));
            Assert.That(
                AutonomousLifePresenter.ResolveVisualAction(AutonomousLifeBehaviour.Nap),
                Is.EqualTo(CheeseTamaVisualAction.Rest));
            Assert.That(
                AutonomousLifePresenter.ResolveVisualAction(AutonomousLifeBehaviour.Window),
                Is.EqualTo(CheeseTamaVisualAction.Event));
            Assert.That(
                AutonomousLifePresenter.ResolveVisualAction(AutonomousLifeBehaviour.Shelf),
                Is.EqualTo(CheeseTamaVisualAction.Cook));
            Assert.That(
                AutonomousLifePresenter.ResolveVisualAction(AutonomousLifeBehaviour.Play),
                Is.EqualTo(CheeseTamaVisualAction.Play));
            Assert.That(
                AutonomousLifePresenter.ResolveVisualAction(AutonomousLifeBehaviour.Dance),
                Is.EqualTo(CheeseTamaVisualAction.Play));
        }

        private static AutonomousLifeContext CreateContext(
            int hour,
            int hunger = 80,
            int mood = 70,
            int cleanliness = 90,
            int sleepiness = 20,
            int health = 100,
            string traitId = NewGameSetupCatalog.BalancedTraitId,
            string floorId = DecorationCatalog.CreamRugId,
            string accentId = DecorationCatalog.MilkBottleId,
            string windowId = DecorationCatalog.CreamCurtainId,
            string shelfId = DecorationCatalog.CheeseClockId,
            string bedsideId = DecorationCatalog.MilkCushionId,
            AutonomousLifeAnchorMask available = AutonomousLifeAnchorMask.All)
        {
            return new AutonomousLifeContext(
                hour,
                true,
                hunger,
                mood,
                cleanliness,
                sleepiness,
                health,
                traitId,
                floorId,
                accentId,
                windowId,
                shelfId,
                bedsideId,
                available);
        }
    }
}
