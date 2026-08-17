using CheeseTama.Gameplay.Bond;
using CheeseTama.Gameplay.Milk;
using CheeseTama.Gameplay.NewGameSetup;
using CheeseTama.Save;
using CheeseTama.UI;
using NUnit.Framework;
using UnityEngine;

namespace CheeseTama.Tests.EditMode
{
    public sealed class BondReactionFeatureTests
    {
        [TestCase(0, BondTier.GettingAcquainted)]
        [TestCase(24, BondTier.GettingAcquainted)]
        [TestCase(25, BondTier.Comfortable)]
        [TestCase(49, BondTier.Comfortable)]
        [TestCase(50, BondTier.Trusted)]
        [TestCase(74, BondTier.Trusted)]
        [TestCase(75, BondTier.Close)]
        [TestCase(89, BondTier.Close)]
        [TestCase(90, BondTier.Inseparable)]
        [TestCase(100, BondTier.Inseparable)]
        [TestCase(999, BondTier.Inseparable)]
        [TestCase(-1, BondTier.GettingAcquainted)]
        public void ResolveTierUsesStableAffectionBoundaries(int affection, BondTier expected)
        {
            Assert.That(BondReactionSystem.ResolveTier(affection), Is.EqualTo(expected));
        }

        [Test]
        public void UnknownOrMissingTraitFallsBackToBalancedWithoutChangingInput()
        {
            var seed = new InitialTemperamentSeedSaveData
            {
                dominantTraitId = "future_trait",
                balance = 7
            };
            var system = new BondReactionSystem();

            var snapshot = system.Observe(seed, 52);

            Assert.That(snapshot.DominantTraitId, Is.EqualTo(NewGameSetupCatalog.BalancedTraitId));
            Assert.That(snapshot.Tier, Is.EqualTo(BondTier.Trusted));
            Assert.That(seed.dominantTraitId, Is.EqualTo("future_trait"));
            Assert.That(seed.balance, Is.EqualTo(7));
        }

        [TestCase(NewGameSetupCatalog.BalancedTraitId, BondInteraction.Feed, MilkCatalog.BasicMilkId)]
        [TestCase(NewGameSetupCatalog.LivelyTraitId, BondInteraction.Play, "")]
        [TestCase(NewGameSetupCatalog.ExpressiveTraitId, BondInteraction.Pet, "")]
        [TestCase(NewGameSetupCatalog.CalmTraitId, BondInteraction.Rest, "")]
        [TestCase(NewGameSetupCatalog.FocusedTraitId, BondInteraction.Cook, "")]
        public void EveryTemperamentHasAPositiveSignatureReaction(
            string traitId,
            BondInteraction interaction,
            string subjectId)
        {
            var system = new BondReactionSystem();
            var result = system.Evaluate(
                new InitialTemperamentSeedSaveData { dominantTraitId = traitId },
                60,
                interaction,
                subjectId);

            Assert.That(result.HasSpecialReaction, Is.True);
            Assert.That(result.IsSignatureReaction, Is.True);
            Assert.That(result.Dialogue.IsValid, Is.True);
            Assert.That(result.Dialogue.Text, Is.Not.Empty);
            Assert.That(result.Dialogue.LineId, Does.StartWith("bond_"));
        }

        [Test]
        public void FavoriteMilkGetsWarmDifferenceButUnrelatedActionKeepsExistingDialoguePath()
        {
            var system = new BondReactionSystem();
            var lively = new InitialTemperamentSeedSaveData
            {
                dominantTraitId = NewGameSetupCatalog.LivelyTraitId
            };

            var favorite = system.Evaluate(
                lively,
                40,
                BondInteraction.Feed,
                MilkCatalog.NuttyMilkId);
            var unrelated = system.Evaluate(
                lively,
                40,
                BondInteraction.Clean);

            Assert.That(favorite.HasSpecialReaction, Is.True);
            Assert.That(favorite.Dialogue.Text, Does.Contain("고소한"));
            Assert.That(unrelated.HasSpecialReaction, Is.False);
        }

        [Test]
        public void ReturnAndAmbientReactionsUnlockOnlyAtTheirBondTiers()
        {
            var system = new BondReactionSystem();
            var expressive = new InitialTemperamentSeedSaveData
            {
                dominantTraitId = NewGameSetupCatalog.ExpressiveTraitId
            };

            Assert.That(
                system.Evaluate(expressive, 49, BondInteraction.Return).HasSpecialReaction,
                Is.False);
            Assert.That(
                system.Evaluate(expressive, 50, BondInteraction.Return).HasSpecialReaction,
                Is.True);
            Assert.That(
                system.Evaluate(expressive, 74, BondInteraction.Ambient).HasSpecialReaction,
                Is.False);
            Assert.That(
                system.Evaluate(expressive, 75, BondInteraction.Ambient).HasSpecialReaction,
                Is.True);
        }

        [Test]
        public void EvaluationIsPresentationOnlyAndNeverMutatesOrPenalizesSave()
        {
            var save = new CheeseTamaSaveData();
            save.EnsureRuntimeDefaults();
            save.newGameSetup.temperamentSeed.dominantTraitId =
                NewGameSetupCatalog.ExpressiveTraitId;
            save.cheeseTama.stats.affection = 83;
            save.cheeseTama.stats.hunger = 27;
            save.cheeseTama.stats.mood = 41;
            var before = JsonUtility.ToJson(save);

            var result = new BondReactionSystem().Evaluate(
                save,
                BondInteraction.Clean);

            Assert.That(result.HasSpecialReaction, Is.False);
            Assert.That(JsonUtility.ToJson(save), Is.EqualTo(before));
        }

        [Test]
        public void PresenterMapsTraitCueWithoutOwningGameplayState()
        {
            var result = new BondReactionSystem().Evaluate(
                new InitialTemperamentSeedSaveData
                {
                    dominantTraitId = NewGameSetupCatalog.LivelyTraitId
                },
                90,
                BondInteraction.Return);

            Assert.That(result.HasSpecialReaction, Is.True);
            Assert.That(
                BondReactionPresenter.ResolveVisualAction(result),
                Is.EqualTo(CheeseTamaVisualAction.Play));
        }
    }
}
