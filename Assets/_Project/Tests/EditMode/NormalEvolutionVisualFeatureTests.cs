using System.Collections.Generic;
using CheeseTama.Gameplay;
using CheeseTama.Gameplay.Growth;
using CheeseTama.UI;
using NUnit.Framework;
using UnityEngine;

namespace CheeseTama.Tests
{
    public sealed class NormalEvolutionVisualFeatureTests
    {
        [Test]
        public void CatalogCoversExistingSixEvolutionIdsInStableOrder()
        {
            Assert.That(NormalEvolutionVisualCatalog.All.Count, Is.EqualTo(6));
            Assert.That(
                NormalEvolutionVisualCatalog.All[0].EvolutionId,
                Is.EqualTo(EvolutionSystem.CreamEvolutionId));
            Assert.That(
                NormalEvolutionVisualCatalog.All[1].EvolutionId,
                Is.EqualTo(EvolutionSystem.CheddarEvolutionId));
            Assert.That(
                NormalEvolutionVisualCatalog.All[2].EvolutionId,
                Is.EqualTo(EvolutionSystem.RicottaEvolutionId));
            Assert.That(
                NormalEvolutionVisualCatalog.All[3].EvolutionId,
                Is.EqualTo(EvolutionSystem.MozzarellaEvolutionId));
            Assert.That(
                NormalEvolutionVisualCatalog.All[4].EvolutionId,
                Is.EqualTo(EvolutionSystem.BlueEvolutionId));
            Assert.That(
                NormalEvolutionVisualCatalog.All[5].EvolutionId,
                Is.EqualTo(EvolutionSystem.CoffeeEvolutionId));

            var ids = new HashSet<string>();
            var patterns = new HashSet<NormalEvolutionVisualPattern>();
            var expressions = new HashSet<NormalEvolutionExpressionHint>();
            var reactions = new HashSet<NormalEvolutionReactionStyle>();
            var bodyTints = new HashSet<string>();
            foreach (var profile in NormalEvolutionVisualCatalog.All)
            {
                Assert.That(ids.Add(profile.EvolutionId), Is.True);
                Assert.That(patterns.Add(profile.Pattern), Is.True);
                Assert.That(expressions.Add(profile.ExpressionHint), Is.True);
                Assert.That(reactions.Add(profile.ReactionStyle), Is.True);
                Assert.That(
                    bodyTints.Add(
                        $"{profile.BodyTint.r:F3},{profile.BodyTint.g:F3},"
                        + $"{profile.BodyTint.b:F3},{profile.BodyTint.a:F3}"),
                    Is.True);
                Assert.That(profile.Accents.Count, Is.GreaterThanOrEqualTo(4));
                Assert.That(NormalEvolutionVisualCatalog.Find(profile.EvolutionId), Is.SameAs(profile));
                foreach (var accent in profile.Accents)
                {
                    Assert.That(
                        EvolutionVisualAccentDefinition.IsSoftPrimitive(accent.Primitive),
                        Is.True);
                    Assert.That(accent.Primitive, Is.Not.EqualTo(PrimitiveType.Cube));
                }
            }

            Assert.That(NormalEvolutionVisualCatalog.Find("unknown"), Is.Null);
        }

        [Test]
        public void SignatureReactionStylesProduceDistinctMidMotionPoses()
        {
            var poses = new HashSet<string>();
            foreach (var profile in NormalEvolutionVisualCatalog.All)
            {
                var pose = NormalEvolutionReactionMotion.Evaluate(profile.ReactionStyle, 0.35f);
                var key = $"{pose.LocalPosition.x:F4},{pose.LocalPosition.y:F4},"
                    + $"{pose.LocalEulerAngles.x:F4},{pose.LocalEulerAngles.y:F4},{pose.LocalEulerAngles.z:F4},"
                    + $"{pose.LocalScale.x:F4},{pose.LocalScale.y:F4},{pose.LocalScale.z:F4}";
                Assert.That(poses.Add(key), Is.True, profile.ReactionStyle.ToString());
            }
        }

        [Test]
        public void PresenterRefreshIsIdempotentAndCreatesRendererOnlyRoundedAccents()
        {
            var host = new GameObject("Evolution Presenter Host");
            var model = CreateModel("Model A");
            try
            {
                var presenter = host.AddComponent<NormalEvolutionVisualPresenter>();
                var tama = new CheeseTamaModel
                {
                    isHatched = true,
                    evolutionId = EvolutionSystem.CreamEvolutionId,
                    form = EvolutionSystem.CreamEvolutionId
                };

                presenter.Bind(tama, model.transform);
                var firstRoot = presenter.GeneratedRoot;
                var firstCount = presenter.GeneratedAccentCount;

                Assert.That(firstRoot, Is.Not.Null);
                Assert.That(firstCount, Is.EqualTo(NormalEvolutionVisualCatalog.Cream.Accents.Count));
                Assert.That(model.transform.Find(NormalEvolutionVisualPresenter.GeneratedRootName), Is.SameAs(firstRoot));
                Assert.That(firstRoot.GetComponentsInChildren<Collider>(true), Is.Empty);
                foreach (var filter in firstRoot.GetComponentsInChildren<MeshFilter>(true))
                {
                    Assert.That(filter.sharedMesh, Is.Not.Null);
                    Assert.That(filter.sharedMesh.name, Does.Not.Contain("Cube"));
                }

                var firstRenderer = firstRoot.GetChild(0).GetComponent<Renderer>();
                var colorBlock = new MaterialPropertyBlock();
                colorBlock.SetColor(Shader.PropertyToID("_Color"), Color.magenta);
                colorBlock.SetColor(Shader.PropertyToID("_BaseColor"), Color.magenta);
                firstRenderer.SetPropertyBlock(colorBlock);

                presenter.Bind(tama, model.transform);
                Assert.That(presenter.GeneratedRoot, Is.SameAs(firstRoot));
                firstRenderer.GetPropertyBlock(colorBlock);
                var expectedColor = NormalEvolutionVisualCatalog.Cream.ResolveColor(
                    NormalEvolutionVisualCatalog.Cream.Accents[0].ColorRole);
                Assert.That(
                    colorBlock.GetColor(Shader.PropertyToID("_Color")),
                    Is.EqualTo(expectedColor));
                Assert.That(presenter.RefreshNow(), Is.True);
                Assert.That(presenter.RefreshNow(), Is.True);
                Assert.That(presenter.GeneratedRoot, Is.SameAs(firstRoot));
                Assert.That(presenter.GeneratedAccentCount, Is.EqualTo(firstCount));
                Assert.That(CountDirectChildren(model.transform, NormalEvolutionVisualPresenter.GeneratedRootName), Is.EqualTo(1));
                Assert.That(presenter.PlaySignatureReaction(), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(model);
            }
        }

        [Test]
        public void PresenterMovesOwnedVisualsWhenModelIsReplacedAndReleaseRemovesThem()
        {
            var host = new GameObject("Evolution Presenter Host");
            var firstModel = CreateModel("Model A");
            var secondModel = CreateModel("Model B");
            try
            {
                var presenter = host.AddComponent<NormalEvolutionVisualPresenter>();
                var tama = new CheeseTamaModel
                {
                    isHatched = true,
                    evolutionId = EvolutionSystem.BlueEvolutionId,
                    form = EvolutionSystem.BlueEvolutionId
                };

                presenter.Bind(tama, firstModel.transform);
                Assert.That(firstModel.transform.Find(NormalEvolutionVisualPresenter.GeneratedRootName), Is.Not.Null);

                presenter.Bind(tama, secondModel.transform);
                Assert.That(firstModel.transform.Find(NormalEvolutionVisualPresenter.GeneratedRootName), Is.Null);
                Assert.That(secondModel.transform.Find(NormalEvolutionVisualPresenter.GeneratedRootName), Is.Not.Null);
                Assert.That(presenter.ActiveEvolutionId, Is.EqualTo(EvolutionSystem.BlueEvolutionId));

                presenter.Release();
                Assert.That(secondModel.transform.Find(NormalEvolutionVisualPresenter.GeneratedRootName), Is.Null);
                Assert.That(presenter.GeneratedRoot, Is.Null);
                Assert.That(presenter.ActiveProfile, Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(firstModel);
                Object.DestroyImmediate(secondModel);
            }
        }

        private static GameObject CreateModel(string name)
        {
            var root = new GameObject(name);
            var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            var collider = body.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            return root;
        }

        private static int CountDirectChildren(Transform parent, string childName)
        {
            var count = 0;
            for (var index = 0; index < parent.childCount; index += 1)
            {
                if (parent.GetChild(index).name == childName)
                {
                    count += 1;
                }
            }

            return count;
        }
    }
}
