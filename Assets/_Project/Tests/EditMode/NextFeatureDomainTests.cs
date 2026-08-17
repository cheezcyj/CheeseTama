using System.Collections.Generic;
using CheeseTama.Gameplay;
using CheeseTama.Gameplay.Growth;
using CheeseTama.Gameplay.Milk;
using CheeseTama.Gameplay.MiniGames;
using CheeseTama.Save;
using NUnit.Framework;

namespace CheeseTama.Tests.EditMode
{
    public sealed class NextFeatureDomainTests
    {
        [Test]
        public void StarRouteRequiresLevelThirtyThreeAndEveryMainMilkAtFive()
        {
            var tama = new CheeseTamaModel { level = 33 };
            var entries = CreateMainMilkGrowth(5);

            var complete = StarRouteSystem.Evaluate(tama, entries);
            Assert.That(complete.unlocked, Is.True);
            Assert.That(complete.completedMilkCount, Is.EqualTo(MilkCatalog.MainMilks.Length));

            entries[0].growthLevel = 4;
            var incompleteMilk = StarRouteSystem.Evaluate(tama, entries);
            Assert.That(incompleteMilk.unlocked, Is.False);

            entries[0].growthLevel = 5;
            tama.level = 32;
            var incompleteLevel = StarRouteSystem.Evaluate(tama, entries);
            Assert.That(incompleteLevel.unlocked, Is.False);
        }

        [Test]
        public void BouncyJumpScoringRewardsAccuracyAndCombo()
        {
            var edge = BouncyJumpMiniGameRules.CalculateAttemptScore(0.99f, 1);
            var center = BouncyJumpMiniGameRules.CalculateAttemptScore(0f, 1);
            var combo = BouncyJumpMiniGameRules.CalculateAttemptScore(0f, 5);

            Assert.That(edge, Is.GreaterThan(0));
            Assert.That(center, Is.GreaterThan(edge));
            Assert.That(combo, Is.GreaterThan(center));
            Assert.That(BouncyJumpMiniGameRules.CalculateAttemptScore(1f, 99), Is.Zero);
        }

        [TestCase(2, false)]
        [TestCase(3, true)]
        public void BouncyJumpCareThresholdIsExplicit(int successes, bool expected)
        {
            var result = BouncyJumpMiniGameRules.Complete(successes, 1, 500, 2);
            Assert.That(result.qualifiesForCare, Is.EqualTo(expected));
        }

        private static List<MilkGrowthSaveEntry> CreateMainMilkGrowth(int level)
        {
            var result = new List<MilkGrowthSaveEntry>();
            foreach (var milk in MilkCatalog.MainMilks)
            {
                result.Add(new MilkGrowthSaveEntry
                {
                    milkId = milk.id,
                    growthLevel = level
                });
            }

            return result;
        }
    }
}
