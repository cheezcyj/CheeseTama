using System.Reflection;
using CheeseTama.Environment;
using NUnit.Framework;
using UnityEngine;

namespace CheeseTama.Tests
{
    public sealed class MilkroomPropInteractionTests
    {
        [TestCase(MilkroomPropRoute.SnackPanel)]
        [TestCase(MilkroomPropRoute.MilkPanel)]
        [TestCase(MilkroomPropRoute.CookingChoice)]
        [TestCase(MilkroomPropRoute.SleepSchedule)]
        public void PublicRouteCatalogContainsEveryRequestedDestination(MilkroomPropRoute route)
        {
            Assert.That(MilkroomPropInteraction.IsSupportedRoute(route), Is.True);
        }

        [Test]
        public void PublicRouteCatalogRejectsNoneAndUnknownValues()
        {
            Assert.That(MilkroomPropInteraction.IsSupportedRoute(MilkroomPropRoute.None), Is.False);
            Assert.That(MilkroomPropInteraction.IsSupportedRoute((MilkroomPropRoute)999), Is.False);
        }

        [Test]
        public void RepeatedConfigureRepairsOneComponentAndOneCollider()
        {
            var room = new GameObject("Milkroom Prop Controller Test");
            var prop = new GameObject("Fridge Test Prop");
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                prop.transform.SetParent(room.transform, false);
                visual.transform.SetParent(prop.transform, false);
                var controller = room.AddComponent<MilkroomPropController>();
                var routed = MilkroomPropRoute.None;
                controller.ConfigureInteractionRouting(
                    route =>
                    {
                        routed = route;
                        return true;
                    },
                    () => false);

                var first = controller.ConfigureInteraction(
                    prop.transform,
                    MilkroomPropRoute.SnackPanel);
                var second = controller.ConfigureInteraction(
                    prop.transform,
                    MilkroomPropRoute.SnackPanel);

                Assert.That(second, Is.SameAs(first));
                Assert.That(prop.GetComponents<MilkroomPropInteraction>(), Has.Length.EqualTo(1));
                Assert.That(prop.GetComponents<Collider>(), Has.Length.EqualTo(1));
                Assert.That(first.InteractionCollider, Is.TypeOf<BoxCollider>());
                Assert.That(first.InteractionCollider.isTrigger, Is.True);
                Assert.That(((BoxCollider)first.InteractionCollider).size.sqrMagnitude, Is.GreaterThan(0.1f));
                Assert.That(controller.RegisteredInteractionCount, Is.EqualTo(1));
                Assert.That(controller.GetInteraction(MilkroomPropRoute.SnackPanel), Is.SameAs(first));

                Assert.That(controller.TryActivateRoute(MilkroomPropRoute.SnackPanel), Is.True);
                Assert.That(routed, Is.EqualTo(MilkroomPropRoute.SnackPanel));
            }
            finally
            {
                Object.DestroyImmediate(room);
            }
        }

        [Test]
        public void KeyboardRouteApiDoesNotRequireAVisualProp()
        {
            var room = new GameObject("Keyboard Route Test");
            try
            {
                var controller = room.AddComponent<MilkroomPropController>();
                var routed = MilkroomPropRoute.None;
                controller.ConfigureInteractionRouting(
                    route =>
                    {
                        routed = route;
                        return true;
                    },
                    () => false);

                Assert.That(controller.TryActivateRoute(MilkroomPropRoute.MilkPanel), Is.True);
                Assert.That(routed, Is.EqualTo(MilkroomPropRoute.MilkPanel));
                Assert.That(controller.TryActivateRoute((MilkroomPropRoute)999), Is.False);
                Assert.That(routed, Is.EqualTo(MilkroomPropRoute.MilkPanel));
            }
            finally
            {
                Object.DestroyImmediate(room);
            }
        }

        [Test]
        public void BlockerPreventsRouteAndClearsKeyboardHighlight()
        {
            var room = new GameObject("Blocked Prop Test");
            var prop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                prop.transform.SetParent(room.transform, false);
                var blocked = false;
                var routeCalls = 0;
                var controller = room.AddComponent<MilkroomPropController>();
                controller.ConfigureInteractionRouting(
                    _ =>
                    {
                        routeCalls += 1;
                        return true;
                    },
                    () => blocked);
                var interaction = controller.ConfigureInteraction(
                    prop.transform,
                    MilkroomPropRoute.CookingChoice);

                Assert.That(controller.SetRouteFocused(MilkroomPropRoute.CookingChoice, true), Is.True);
                Assert.That(interaction.IsKeyboardFocused, Is.True);
                Assert.That(interaction.IsHighlighted, Is.True);

                blocked = true;
                controller.RefreshInteractionBlockingState();
                Assert.That(interaction.IsKeyboardFocused, Is.False);
                Assert.That(interaction.IsHighlighted, Is.False);
                Assert.That(controller.TryActivateRoute(MilkroomPropRoute.CookingChoice), Is.False);
                Assert.That(interaction.TryActivate(), Is.False);
                Assert.That(routeCalls, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(room);
            }
        }

        [Test]
        public void HoverHighlightUsesAndRestoresPropertyBlockWithoutMaterialInstantiation()
        {
            var prop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Material material = null;
            try
            {
                var renderer = prop.GetComponent<Renderer>();
                var shader = Shader.Find("Universal Render Pipeline/Lit")
                    ?? Shader.Find("Standard")
                    ?? Shader.Find("Sprites/Default");
                Assert.That(shader, Is.Not.Null);
                material = new Material(shader);
                renderer.sharedMaterial = material;

                var colorProperty = material.HasProperty("baseColorFactor")
                    ? "baseColorFactor"
                    : material.HasProperty("_BaseColor")
                        ? "_BaseColor"
                        : "_Color";
                Assert.That(material.HasProperty(colorProperty), Is.True);
                material.SetColor(colorProperty, new Color(0.25f, 0.35f, 0.45f, 1f));

                var sentinelId = Shader.PropertyToID("_MilkroomInteractionTestSentinel");
                var originalBlock = new MaterialPropertyBlock();
                originalBlock.SetFloat(sentinelId, 0.42f);
                renderer.SetPropertyBlock(originalBlock);

                var originalMaterial = renderer.sharedMaterial;
                var interaction = prop.AddComponent<MilkroomPropInteraction>();
                interaction.Configure(
                    MilkroomPropRoute.SleepSchedule,
                    _ => true,
                    () => false,
                    prop.GetComponent<Collider>(),
                    new[] { renderer });

                interaction.SetKeyboardFocus(true);
                var highlightedBlock = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(highlightedBlock);
                Assert.That(interaction.IsHighlighted, Is.True);
                Assert.That(highlightedBlock.GetFloat(sentinelId), Is.EqualTo(0.42f).Within(0.0001f));
                Assert.That(
                    highlightedBlock.GetColor(colorProperty),
                    Is.Not.EqualTo(material.GetColor(colorProperty)));
                Assert.That(renderer.sharedMaterial, Is.SameAs(originalMaterial));

                interaction.SetKeyboardFocus(false);
                var restoredBlock = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(restoredBlock);
                Assert.That(interaction.IsHighlighted, Is.False);
                Assert.That(restoredBlock.GetFloat(sentinelId), Is.EqualTo(0.42f).Within(0.0001f));
                Assert.That(restoredBlock.GetColor(colorProperty), Is.EqualTo(default(Color)));
                Assert.That(renderer.sharedMaterial, Is.SameAs(originalMaterial));
            }
            finally
            {
                if (material != null)
                {
                    Object.DestroyImmediate(material);
                }

                Object.DestroyImmediate(prop);
            }
        }

        [Test]
        public void ColliderMouseReleaseUsesTheSameGuardedRouteCallback()
        {
            var prop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                var routeCalls = 0;
                var interaction = prop.AddComponent<MilkroomPropInteraction>();
                interaction.Configure(
                    MilkroomPropRoute.MilkPanel,
                    route =>
                    {
                        Assert.That(route, Is.EqualTo(MilkroomPropRoute.MilkPanel));
                        routeCalls += 1;
                        return true;
                    },
                    () => false,
                    prop.GetComponent<Collider>(),
                    new[] { prop.GetComponent<Renderer>() });

                var mouseRelease = typeof(MilkroomPropInteraction).GetMethod(
                    "OnMouseUpAsButton",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(mouseRelease, Is.Not.Null);
                mouseRelease.Invoke(interaction, null);
                Assert.That(routeCalls, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(prop);
            }
        }

        [Test]
        public void ReassigningARouteUnconfiguresThePreviousProp()
        {
            var room = new GameObject("Route Reassignment Test");
            var firstProp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var secondProp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                firstProp.transform.SetParent(room.transform, false);
                secondProp.transform.SetParent(room.transform, false);
                var controller = room.AddComponent<MilkroomPropController>();
                controller.ConfigureInteractionRouting(_ => true, () => false);
                var first = controller.ConfigureInteraction(
                    firstProp.transform,
                    MilkroomPropRoute.SleepSchedule);
                var second = controller.ConfigureInteraction(
                    secondProp.transform,
                    MilkroomPropRoute.SleepSchedule);

                Assert.That(first.IsConfigured, Is.False);
                Assert.That(first.TryActivate(), Is.False);
                first.SetKeyboardFocus(true);
                Assert.That(first.IsKeyboardFocused, Is.False);
                Assert.That(first.IsHighlighted, Is.False);
                Assert.That(second.IsConfigured, Is.True);
                Assert.That(controller.RegisteredInteractionCount, Is.EqualTo(1));
                Assert.That(controller.GetInteraction(MilkroomPropRoute.SleepSchedule), Is.SameAs(second));
            }
            finally
            {
                Object.DestroyImmediate(room);
            }
        }
    }
}
