using System.IO;
using CheeseTama.Audio;
using CheeseTama.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CheeseTama.Tests.EditMode
{
    public sealed class AudioAnimationFeatureTests
    {
        private static readonly string[] RequiredAudioResourcePaths =
        {
            "Audio/milkroom_loop",
            "Audio/ui_click",
            "Audio/care",
            "Audio/pet",
            "Audio/reward",
            "Audio/return",
            "Audio/milk_blend",
            "Audio/rare_discovery"
        };

        [Test]
        public void AuthoredAudioClipsAreImportedFromWaveFiles()
        {
            for (var index = 0; index < RequiredAudioResourcePaths.Length; index += 1)
            {
                var resourcePath = RequiredAudioResourcePaths[index];
                var clip = Resources.Load<AudioClip>(resourcePath);
                Assert.That(clip, Is.Not.Null, $"Missing authored audio resource: {resourcePath}");
                Assert.That(clip.length, Is.GreaterThan(0.05f), resourcePath);

                var assetPath = AssetDatabase.GetAssetPath(clip);
                Assert.That(Path.GetExtension(assetPath), Is.EqualTo(".wav").IgnoreCase, resourcePath);
                var header = File.ReadAllBytes(assetPath);
                Assert.That(header.Length, Is.GreaterThan(44), resourcePath);
                Assert.That(System.Text.Encoding.ASCII.GetString(header, 0, 4), Is.EqualTo("RIFF"));
                Assert.That(System.Text.Encoding.ASCII.GetString(header, 8, 4), Is.EqualTo("WAVE"));
            }
        }

        [Test]
        public void AudioControllerPrefersAuthoredResourcesOverRuntimeFallbacks()
        {
            var existing = CheeseTamaAudioController.Instance;
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var root = new GameObject("Authored Audio Controller Test");
            try
            {
                var controller = root.AddComponent<CheeseTamaAudioController>();
                controller.ReloadAudioAssets();

                Assert.That(controller.UsingAuthoredAudioAssets, Is.True);
                Assert.That(
                    controller.MusicSource.clip,
                    Is.SameAs(Resources.Load<AudioClip>("Audio/milkroom_loop")));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void MilkBlendAndRareDiscoveryUseDistinctVisibleProceduralActions()
        {
            var root = new GameObject("Milk Blend Visual Action Test");
            try
            {
                var controller = root.AddComponent<CheeseTamaVisualController>();

                controller.ReactMilkBlend();
                Assert.That(controller.ActiveAction, Is.EqualTo(CheeseTamaVisualAction.MilkBlend));
                Assert.That(controller.IsReacting, Is.True);
                var blendProp = root.transform.Find("Action Prop Overlay/Milk Blend Action Prop");
                Assert.That(blendProp, Is.Not.Null);
                Assert.That(blendProp.gameObject.activeSelf, Is.True);
                Assert.That(blendProp.Find("Blend Bowl"), Is.Not.Null);
                Assert.That(blendProp.Find("Blend Spoon"), Is.Not.Null);

                controller.ReactMilkBlend(true);
                Assert.That(controller.ActiveAction, Is.EqualTo(CheeseTamaVisualAction.RareDiscovery));
                Assert.That(blendProp.gameObject.activeSelf, Is.True);
                var sparkle = root.transform.Find("Action Prop Overlay/Sparkle Action Prop");
                Assert.That(sparkle, Is.Not.Null);
                Assert.That(sparkle.gameObject.activeSelf, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
