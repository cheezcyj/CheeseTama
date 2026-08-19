using System.Reflection;
using CheeseTama.Audio;
using CheeseTama.Save;
using NUnit.Framework;
using UnityEngine;

namespace CheeseTama.Tests.EditMode
{
    public sealed class AudioMasteringFeatureTests
    {
        [TestCase(-1f)]
        [TestCase(0f)]
        [TestCase(0.25f)]
        [TestCase(0.5f)]
        [TestCase(0.75f)]
        [TestCase(1f)]
        [TestCase(2f)]
        public void CrossfadeUsesComplementaryGainsWithoutSummedClipping(float progress)
        {
            AudioMasteringRules.GetComplementaryCrossfadeGains(progress, out var outgoing, out var incoming);

            Assert.That(outgoing, Is.InRange(0f, 1f));
            Assert.That(incoming, Is.InRange(0f, 1f));
            Assert.That(outgoing + incoming, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void SnapshotReportsAuthoredAssetsChannelsAndClampedOutput()
        {
            ClearDestroyedSingletonReference();
            var previousListenerVolume = AudioListener.volume;
            var root = new GameObject("Audio Mastering Snapshot Test");
            try
            {
                var controller = root.AddComponent<CheeseTamaAudioController>();
                controller.ReloadAudioAssets();
                controller.ApplyVolumeSettings(new GameSettingsSaveData
                {
                    masterVolume = 0.8f,
                    musicVolume = 0.5f,
                    effectVolume = 0.25f
                });

                var snapshot = controller.GetPlaybackSnapshot();
                Assert.That(snapshot.UsingAuthoredAudioAssets, Is.True);
                Assert.That(snapshot.LoadedAuthoredAssetCount, Is.EqualTo(8));
                Assert.That(snapshot.ActiveMusicClipName, Is.EqualTo("milkroom_loop"));
                Assert.That(snapshot.ListenerVolume, Is.EqualTo(0.8f).Within(0.001f));
                Assert.That(snapshot.MusicOutputVolume, Is.EqualTo(0.1f).Within(0.001f));
                Assert.That(snapshot.EffectOutputVolume, Is.EqualTo(0.105f).Within(0.001f));
                Assert.That(snapshot.CombinedMusicGain, Is.EqualTo(1f).Within(0.001f));
                Assert.That(root.GetComponents<AudioSource>(), Has.Length.EqualTo(2));
                Assert.That(root.GetComponentsInChildren<AudioSource>(true), Has.Length.EqualTo(3));
            }
            finally
            {
                Object.DestroyImmediate(root);
                AudioListener.volume = previousListenerVolume;
            }
        }

        [Test]
        public void RepeatedBackgroundRequestKeepsOneConfiguredMusicPair()
        {
            ClearDestroyedSingletonReference();
            var root = new GameObject("Audio Duplicate Music Guard Test");
            try
            {
                var controller = root.AddComponent<CheeseTamaAudioController>();
                var clip = Resources.Load<AudioClip>("Audio/milkroom_loop");

                controller.PlayBackgroundMusic(clip);
                controller.PlayBackgroundMusic(clip);

                var snapshot = controller.GetPlaybackSnapshot();
                Assert.That(root.GetComponents<AudioSource>(), Has.Length.EqualTo(2));
                Assert.That(root.GetComponentsInChildren<AudioSource>(true), Has.Length.EqualTo(3));
                Assert.That(snapshot.ActiveMusicClipName, Is.EqualTo("milkroom_loop"));
                Assert.That(snapshot.CombinedMusicGain, Is.EqualTo(1f).Within(0.001f));
                Assert.That(snapshot.ActiveMusicVoiceCount, Is.LessThanOrEqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void FocusAndPauseCallbacksOnlyResumeAfterBothInterruptionsClear()
        {
            ClearDestroyedSingletonReference();
            var root = new GameObject("Audio Interruption State Test");
            try
            {
                var controller = root.AddComponent<CheeseTamaAudioController>();
                controller.PlayBackgroundMusic(Resources.Load<AudioClip>("Audio/milkroom_loop"));

                InvokeLifecycle(controller, "OnApplicationFocus", false);
                Assert.That(controller.GetPlaybackSnapshot().IsInterrupted, Is.True);
                Assert.That(controller.GetPlaybackSnapshot().PausedForInterruption, Is.True);

                InvokeLifecycle(controller, "OnApplicationPause", true);
                InvokeLifecycle(controller, "OnApplicationFocus", true);
                Assert.That(controller.GetPlaybackSnapshot().IsInterrupted, Is.True);
                Assert.That(controller.GetPlaybackSnapshot().PausedForInterruption, Is.True);

                InvokeLifecycle(controller, "OnApplicationPause", false);
                var resumed = controller.GetPlaybackSnapshot();
                Assert.That(resumed.IsInterrupted, Is.False);
                Assert.That(resumed.PausedForInterruption, Is.False);
                Assert.That(resumed.MusicRequested, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void InvokeLifecycle(CheeseTamaAudioController controller, string methodName, bool value)
        {
            var method = typeof(CheeseTamaAudioController).GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(controller, new object[] { value });
        }

        private static void ClearDestroyedSingletonReference()
        {
            var existing = CheeseTamaAudioController.Instance;
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
                return;
            }

            if (!ReferenceEquals(existing, null))
            {
                typeof(CheeseTamaAudioController)
                    .GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic)
                    ?.SetValue(null, null);
            }
        }
    }
}
