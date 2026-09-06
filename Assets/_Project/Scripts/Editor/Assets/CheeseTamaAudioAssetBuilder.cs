using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace CheeseTama.Editor
{
    public static class CheeseTamaAudioAssetBuilder
    {
        private const int SampleRate = 22050;
        private const string AudioDirectory = "Assets/_Project/Resources/Audio";

        [MenuItem("CheeseTama/Audio/Build Authored Audio Assets")]
        public static void BuildAuthoredAudioAssets()
        {
            Directory.CreateDirectory(AudioDirectory);

            var assets = new[]
            {
                new AudioAsset("milkroom_loop.wav", BuildBackgroundLoop(), true),
                new AudioAsset("ui_click.wav", BuildSweep(0.09f, 660f, 980f, 0.2f), false),
                new AudioAsset("care.wav", BuildCareCue(), false),
                new AudioAsset("pet.wav", BuildPetCue(), false),
                new AudioAsset("reward.wav", BuildArpeggio(0.82f, new[] { 523.25f, 659.25f, 783.99f }), false),
                new AudioAsset("return.wav", BuildArpeggio(0.64f, new[] { 392f, 523.25f, 659.25f }), false),
                new AudioAsset("milk_blend.wav", BuildMilkBlendCue(), false),
                new AudioAsset("rare_discovery.wav", BuildRareDiscoveryCue(), false)
            };

            for (var index = 0; index < assets.Length; index += 1)
            {
                var assetPath = $"{AudioDirectory}/{assets[index].FileName}";
                WriteWaveIfChanged(assetPath, assets[index].Samples);
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            for (var index = 0; index < assets.Length; index += 1)
            {
                ConfigureImporter(
                    $"{AudioDirectory}/{assets[index].FileName}",
                    assets[index].IsMusic);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log($"CheeseTama authored audio assets are ready: {assets.Length} WAV files.");
        }

        private static float[] BuildBackgroundLoop()
        {
            const float durationSeconds = 12f;
            const float noteSeconds = 1.5f;
            var notes = new[] { 261.63f, 329.63f, 392f, 329.63f, 293.66f, 349.23f, 440f, 349.23f };
            var samples = CreateSamples(durationSeconds);
            for (var index = 0; index < samples.Length; index += 1)
            {
                var time = index / (float)SampleRate;
                var noteIndex = Mathf.FloorToInt(time / noteSeconds) % notes.Length;
                var noteTime = time % noteSeconds;
                var noteEnvelope = SmoothEnvelope(noteTime / noteSeconds, 0.12f, 0.68f);
                var chordPulse = 0.72f + Mathf.Sin(time * Mathf.PI * 2f / 3f) * 0.08f;
                var fundamental = Mathf.Sin(2f * Mathf.PI * notes[noteIndex] * time);
                var overtone = Mathf.Sin(2f * Mathf.PI * notes[noteIndex] * 2f * time) * 0.22f;
                var warmBed = Mathf.Sin(2f * Mathf.PI * 65.41f * time) * 0.1f;
                samples[index] = (fundamental + overtone) * noteEnvelope * chordPulse * 0.105f
                    + warmBed * 0.075f;
            }

            ApplyLoopCrossfade(samples, Mathf.CeilToInt(0.28f * SampleRate));
            return samples;
        }

        private static float[] BuildSweep(
            float durationSeconds,
            float startFrequency,
            float endFrequency,
            float amplitude)
        {
            var samples = CreateSamples(durationSeconds);
            var phase = 0f;
            for (var index = 0; index < samples.Length; index += 1)
            {
                var normalized = index / (float)Mathf.Max(1, samples.Length - 1);
                var frequency = Mathf.Lerp(startFrequency, endFrequency, normalized);
                phase += 2f * Mathf.PI * frequency / SampleRate;
                samples[index] = Mathf.Sin(phase)
                    * SmoothEnvelope(normalized, 0.08f, 0.62f)
                    * amplitude;
            }

            return samples;
        }

        private static float[] BuildCareCue()
        {
            const float durationSeconds = 0.28f;
            var samples = CreateSamples(durationSeconds);
            for (var index = 0; index < samples.Length; index += 1)
            {
                var time = index / (float)SampleRate;
                var normalized = time / durationSeconds;
                var baseTone = Mathf.Sin(2f * Mathf.PI * 440f * time);
                var answerTone = Mathf.Sin(2f * Mathf.PI * 659.25f * time) * Mathf.Clamp01((normalized - 0.34f) * 3.5f);
                samples[index] = (baseTone * 0.15f + answerTone * 0.1f)
                    * SmoothEnvelope(normalized, 0.06f, 0.64f);
            }

            return samples;
        }

        private static float[] BuildPetCue()
        {
            const float durationSeconds = 0.46f;
            var samples = CreateSamples(durationSeconds);
            for (var index = 0; index < samples.Length; index += 1)
            {
                var time = index / (float)SampleRate;
                var normalized = time / durationSeconds;
                var vibrato = Mathf.Sin(2f * Mathf.PI * 7f * time) * 9f;
                var tone = Mathf.Sin(2f * Mathf.PI * (560f + vibrato) * time);
                var sparkle = Mathf.Sin(2f * Mathf.PI * 1120f * time) * 0.18f;
                samples[index] = (tone + sparkle)
                    * SmoothEnvelope(normalized, 0.08f, 0.58f)
                    * 0.17f;
            }

            return samples;
        }

        private static float[] BuildArpeggio(float durationSeconds, float[] notes)
        {
            var samples = CreateSamples(durationSeconds);
            var noteLength = durationSeconds / notes.Length;
            for (var index = 0; index < samples.Length; index += 1)
            {
                var time = index / (float)SampleRate;
                var noteIndex = Mathf.Min(notes.Length - 1, Mathf.FloorToInt(time / noteLength));
                var noteTime = time - noteIndex * noteLength;
                var localNormalized = noteTime / noteLength;
                var tone = Mathf.Sin(2f * Mathf.PI * notes[noteIndex] * noteTime);
                var overtone = Mathf.Sin(2f * Mathf.PI * notes[noteIndex] * 2f * noteTime) * 0.16f;
                samples[index] = (tone + overtone)
                    * SmoothEnvelope(localNormalized, 0.08f, 0.62f)
                    * 0.18f;
            }

            ApplyEdgeFade(samples, 0.008f, 0.05f);
            return samples;
        }

        private static float[] BuildMilkBlendCue()
        {
            const float durationSeconds = 0.72f;
            var samples = CreateSamples(durationSeconds);
            var phase = 0f;
            uint noiseState = 0x5EED1234u;
            for (var index = 0; index < samples.Length; index += 1)
            {
                var time = index / (float)SampleRate;
                var normalized = time / durationSeconds;
                var frequency = Mathf.Lerp(270f, 720f, Mathf.SmoothStep(0f, 1f, normalized));
                phase += 2f * Mathf.PI * frequency / SampleRate;
                noiseState = noiseState * 1664525u + 1013904223u;
                var noise = ((noiseState >> 9) / 8388607f * 2f - 1f) * 0.018f;
                var bubbleA = Mathf.Sin(2f * Mathf.PI * 980f * time)
                    * Mathf.Exp(-Mathf.Pow((normalized - 0.38f) * 15f, 2f));
                var bubbleB = Mathf.Sin(2f * Mathf.PI * 1280f * time)
                    * Mathf.Exp(-Mathf.Pow((normalized - 0.72f) * 18f, 2f));
                samples[index] = (Mathf.Sin(phase) * 0.14f + bubbleA * 0.08f + bubbleB * 0.07f + noise)
                    * SmoothEnvelope(normalized, 0.06f, 0.7f);
            }

            return samples;
        }

        private static float[] BuildRareDiscoveryCue()
        {
            const float durationSeconds = 1.08f;
            var notes = new[] { 523.25f, 659.25f, 783.99f, 1046.5f };
            var samples = CreateSamples(durationSeconds);
            var noteLength = durationSeconds / notes.Length;
            for (var index = 0; index < samples.Length; index += 1)
            {
                var time = index / (float)SampleRate;
                var noteIndex = Mathf.Min(notes.Length - 1, Mathf.FloorToInt(time / noteLength));
                var noteTime = time - noteIndex * noteLength;
                var normalized = noteTime / noteLength;
                var bell = Mathf.Sin(2f * Mathf.PI * notes[noteIndex] * noteTime);
                var shimmer = Mathf.Sin(2f * Mathf.PI * notes[noteIndex] * 3f * noteTime) * 0.22f;
                var highSparkle = Mathf.Sin(2f * Mathf.PI * 1567.98f * time)
                    * Mathf.Clamp01((time - 0.45f) * 2.4f)
                    * 0.08f;
                samples[index] = (bell + shimmer)
                    * SmoothEnvelope(normalized, 0.045f, 0.68f)
                    * 0.18f
                    + highSparkle * SmoothEnvelope(time / durationSeconds, 0.1f, 0.72f);
            }

            ApplyEdgeFade(samples, 0.006f, 0.08f);
            return samples;
        }

        private static float[] CreateSamples(float durationSeconds)
        {
            return new float[Mathf.Max(1, Mathf.CeilToInt(durationSeconds * SampleRate))];
        }

        private static float SmoothEnvelope(float normalized, float attackEnd, float releaseStart)
        {
            var attack = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(normalized / Mathf.Max(0.001f, attackEnd)));
            var release = Mathf.SmoothStep(
                1f,
                0f,
                Mathf.Clamp01((normalized - releaseStart) / Mathf.Max(0.001f, 1f - releaseStart)));
            return attack * release;
        }

        private static void ApplyEdgeFade(float[] samples, float attackSeconds, float releaseSeconds)
        {
            var attackSamples = Mathf.Clamp(Mathf.CeilToInt(attackSeconds * SampleRate), 1, samples.Length);
            var releaseSamples = Mathf.Clamp(Mathf.CeilToInt(releaseSeconds * SampleRate), 1, samples.Length);
            for (var index = 0; index < attackSamples; index += 1)
            {
                samples[index] *= index / (float)attackSamples;
            }

            for (var index = 0; index < releaseSamples; index += 1)
            {
                var sampleIndex = samples.Length - 1 - index;
                samples[sampleIndex] *= index / (float)releaseSamples;
            }
        }

        private static void ApplyLoopCrossfade(float[] samples, int crossfadeSamples)
        {
            var safeCount = Mathf.Clamp(crossfadeSamples, 1, samples.Length / 2);
            for (var index = 0; index < safeCount; index += 1)
            {
                var blend = index / (float)Mathf.Max(1, safeCount - 1);
                var endIndex = samples.Length - safeCount + index;
                var blended = Mathf.Lerp(samples[endIndex], samples[index], blend);
                samples[endIndex] = blended;
                samples[index] = blended;
            }
        }

        private static void WriteWaveIfChanged(string assetPath, float[] samples)
        {
            var bytes = EncodePcmWave(samples);
            if (File.Exists(assetPath))
            {
                var existing = File.ReadAllBytes(assetPath);
                if (existing.Length == bytes.Length && AreEqual(existing, bytes))
                {
                    return;
                }
            }

            File.WriteAllBytes(assetPath, bytes);
        }

        private static byte[] EncodePcmWave(float[] samples)
        {
            using var stream = new MemoryStream(44 + samples.Length * 2);
            using var writer = new BinaryWriter(stream, Encoding.ASCII, true);
            var dataSize = samples.Length * 2;
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + dataSize);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)1);
            writer.Write(SampleRate);
            writer.Write(SampleRate * 2);
            writer.Write((short)2);
            writer.Write((short)16);
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(dataSize);

            for (var index = 0; index < samples.Length; index += 1)
            {
                var sample = Mathf.Clamp(samples[index], -1f, 1f);
                writer.Write((short)Mathf.RoundToInt(sample * short.MaxValue));
            }

            writer.Flush();
            return stream.ToArray();
        }

        private static bool AreEqual(byte[] left, byte[] right)
        {
            for (var index = 0; index < left.Length; index += 1)
            {
                if (left[index] != right[index])
                {
                    return false;
                }
            }

            return true;
        }

        private static void ConfigureImporter(string assetPath, bool isMusic)
        {
            if (AssetImporter.GetAtPath(assetPath) is not AudioImporter importer)
            {
                throw new InvalidOperationException($"Audio importer was not created for {assetPath}.");
            }

            importer.forceToMono = true;
            importer.loadInBackground = isMusic;
            importer.ambisonic = false;
            importer.defaultSampleSettings = new AudioImporterSampleSettings
            {
                loadType = isMusic
                    ? AudioClipLoadType.CompressedInMemory
                    : AudioClipLoadType.DecompressOnLoad,
                compressionFormat = AudioCompressionFormat.Vorbis,
                quality = isMusic ? 0.72f : 0.78f,
                sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate
            };
            importer.SaveAndReimport();
        }

        private readonly struct AudioAsset
        {
            public AudioAsset(string fileName, float[] samples, bool isMusic)
            {
                FileName = fileName;
                Samples = samples;
                IsMusic = isMusic;
            }

            public string FileName { get; }
            public float[] Samples { get; }
            public bool IsMusic { get; }
        }
    }
}
