using System.Collections.Generic;
using CheeseTama.Gameplay.Milk;
using NUnit.Framework;

namespace CheeseTama.Tests.EditMode
{
    public sealed class MilkGrowthMilestoneRewardTests
    {
        [Test]
        public void ReachingEachMilestoneGrantsItExactlyOnce()
        {
            var claimed = new List<string>();

            var first = MilkGrowthMilestoneRewardSystem.ClaimReachedMilestones(
                MilkCatalog.BasicMilkId,
                3,
                claimed);
            var duplicate = MilkGrowthMilestoneRewardSystem.ClaimReachedMilestones(
                MilkCatalog.BasicMilkId,
                3,
                claimed);
            var final = MilkGrowthMilestoneRewardSystem.ClaimReachedMilestones(
                MilkCatalog.BasicMilkId,
                5,
                claimed);

            Assert.That(first.granted, Is.True);
            Assert.That(first.claimedKeys.Count, Is.EqualTo(2));
            Assert.That(first.milkCoins, Is.EqualTo(4));
            Assert.That(first.milkDrops, Is.EqualTo(2));
            Assert.That(duplicate.granted, Is.False);
            Assert.That(final.claimedKeys.Count, Is.EqualTo(2));
            Assert.That(final.milkCoins, Is.EqualTo(20));
            Assert.That(final.milkDrops, Is.EqualTo(3));
            Assert.That(final.collectionFragments, Is.EqualTo(2));
            Assert.That(claimed.Count, Is.EqualTo(4));
        }

        [Test]
        public void ClaimsAreIndependentPerMilk()
        {
            var claimed = new List<string>();
            var basic = MilkGrowthMilestoneRewardSystem.ClaimReachedMilestones(
                MilkCatalog.BasicMilkId,
                2,
                claimed);
            var warm = MilkGrowthMilestoneRewardSystem.ClaimReachedMilestones(
                MilkCatalog.WarmMilkId,
                2,
                claimed);

            Assert.That(basic.granted, Is.True);
            Assert.That(warm.granted, Is.True);
            Assert.That(claimed, Has.Count.EqualTo(2));
        }
    }
}
