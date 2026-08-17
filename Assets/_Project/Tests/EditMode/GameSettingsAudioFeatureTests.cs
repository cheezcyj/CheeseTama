using System;
using System.IO;
using System.Reflection;
using CheeseTama.Audio;
using CheeseTama.Core;
using CheeseTama.Save;
using CheeseTama.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.Tests.EditMode
{
    public sealed class GameSettingsAudioFeatureTests
    {
        [Test]
        public void AudioChannelVolumesAreClampedAndSurviveJsonRoundTrip()
        {
            var settings = new GameSettingsSaveData
            {
                masterVolume = 0.8f,
                musicVolume = 0.35f,
                effectVolume = 0.65f
            };

            var json = JsonUtility.ToJson(settings);
            var restored = JsonUtility.FromJson<GameSettingsSaveData>(json);
            restored.EnsureRuntimeDefaults();

            Assert.That(restored.masterVolume, Is.EqualTo(0.8f).Within(0.001f));
            Assert.That(restored.musicVolume, Is.EqualTo(0.35f).Within(0.001f));
            Assert.That(restored.effectVolume, Is.EqualTo(0.65f).Within(0.001f));

            restored.musicVolume = -1f;
            restored.effectVolume = 2f;
            restored.EnsureRuntimeDefaults();
            Assert.That(restored.musicVolume, Is.Zero);
            Assert.That(restored.effectVolume, Is.EqualTo(1f));
        }

        [Test]
        public void AudioControllerAppliesIndependentMusicAndEffectScales()
        {
            var root = new GameObject("Audio Channel Settings Test");
            try
            {
                var controller = root.AddComponent<CheeseTamaAudioController>();
                controller.ApplyVolumeSettings(new GameSettingsSaveData
                {
                    masterVolume = 0.75f,
                    musicVolume = 0.5f,
                    effectVolume = 0.25f
                });

                Assert.That(controller.MusicSource.volume, Is.EqualTo(0.1f).Within(0.001f));
                Assert.That(controller.EffectSource.volume, Is.EqualTo(0.105f).Within(0.001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void LegacySaveWithoutChannelFieldsMigratesToAudibleDefaults()
        {
            var root = new GameObject("Legacy Audio Settings Migration");
            root.SetActive(false);
            var saveManager = root.AddComponent<SaveManager>();
            var isolatedName = $"cheesetama_audio_settings_{Guid.NewGuid():N}.json";
            typeof(SaveManager).GetField("saveFileName", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(saveManager, isolatedName);
            try
            {
                var save = SaveManager.CreateDefaultSave();
                var json = JsonUtility.ToJson(save, true)
                    .Replace("    \"musicVolume\": 1.0,\r\n", string.Empty)
                    .Replace("    \"effectVolume\": 1.0,\r\n", string.Empty)
                    .Replace("    \"musicVolume\": 1.0,\n", string.Empty)
                    .Replace("    \"effectVolume\": 1.0,\n", string.Empty);
                File.WriteAllText(saveManager.SaveFilePath, json);

                var restored = saveManager.LoadOrCreate();
                Assert.That(restored.settings.musicVolume, Is.EqualTo(1f));
                Assert.That(restored.settings.effectVolume, Is.EqualTo(1f));
                Assert.That(saveManager.LastLoadMigratedData, Is.True);
            }
            finally
            {
                saveManager.DeleteSave();
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RuntimeSettingsRebuildAddsIndependentChannelSliders()
        {
            var staleAudio = CheeseTamaAudioController.Instance;
            if (staleAudio == null && !ReferenceEquals(staleAudio, null))
            {
                typeof(CheeseTamaAudioController)
                    .GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic)
                    ?.SetValue(null, null);
            }

            var canvasObject = new GameObject("Milkroom Canvas", typeof(RectTransform), typeof(Canvas));
            var topMenu = new GameObject("Top Menu", typeof(RectTransform));
            topMenu.transform.SetParent(canvasObject.transform, false);
            var settingsButtonObject = new GameObject("Settings Button", typeof(RectTransform), typeof(Image), typeof(Button));
            settingsButtonObject.transform.SetParent(topMenu.transform, false);
            var controllerObject = new GameObject("Milkroom UI", typeof(RectTransform));
            controllerObject.transform.SetParent(canvasObject.transform, false);
            var milkroomUi = controllerObject.AddComponent<MilkroomUIController>();

            try
            {
                var method = typeof(StarterSceneBuilder).GetMethod(
                    "BuildMilkroomSettings",
                    BindingFlags.Static | BindingFlags.NonPublic);
                Assert.That(method, Is.Not.Null);

                var arguments = new object[]
                {
                    canvasObject.transform,
                    settingsButtonObject.GetComponent<Button>(),
                    milkroomUi,
                    null,
                    null
                };
                method.Invoke(null, arguments);

                var settings = canvasObject.transform.Find("Settings Modal");
                Assert.That(settings, Is.Not.Null);
                Assert.That(settings.Find("Music Volume Slider"), Is.Not.Null);
                Assert.That(settings.Find("Effect Volume Slider"), Is.Not.Null);
                Assert.That(settings.GetComponent<GameSettingsPanelController>(), Is.Not.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(canvasObject);
            }
        }
    }
}
