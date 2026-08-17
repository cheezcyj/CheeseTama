using System;
using System.Collections.Generic;
using CheeseTama.Core;
using CheeseTama.Save;
using UnityEngine;

namespace CheeseTama.Audio
{
    public sealed class CheeseTamaAudioController : MonoBehaviour
    {
        private const int SampleRate = 22050;
        private const float BackgroundVolume = 0.2f;
        private const float EffectVolume = 0.42f;

        private readonly List<AudioClip> generatedClips = new List<AudioClip>();

        private AudioSource musicSource;
        private AudioSource effectSource;
        private AudioClip uiClickClip;
        private AudioClip careClip;
        private AudioClip petClip;
        private AudioClip rewardClip;
        private AudioClip returnClip;
        private GameManager boundManager;

        public static CheeseTamaAudioController Instance { get; private set; }
        public AudioSource MusicSource => musicSource;
        public AudioSource EffectSource => effectSource;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            EnsureAudioListener();
            EnsureSources();
            EnsureClips();
        }

        private void OnEnable()
        {
            BindManager(GameManager.Instance);
        }

        private void Start()
        {
            BindManager(GameManager.Instance);
            ApplySavedVolume();
            StartBackgroundMusic();
        }

        private void OnDisable()
        {
            BindManager(null);
        }

        private void OnDestroy()
        {
            BindManager(null);
            if (Instance == this)
            {
                Instance = null;
            }

            foreach (var clip in generatedClips)
            {
                if (clip != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(clip);
                    }
                    else
                    {
                        DestroyImmediate(clip);
                    }
                }
            }

            generatedClips.Clear();
        }

        public void BindManager(GameManager manager)
        {
            if (boundManager == manager)
            {
                return;
            }

            if (boundManager != null)
            {
                boundManager.CareActionRegistered -= HandleCareAction;
                boundManager.DailyRoutineCompleted -= HandleDailyRoutineCompleted;
                boundManager.SaveDataReplaced -= ApplySavedVolume;
            }

            boundManager = manager;
            if (boundManager != null)
            {
                boundManager.CareActionRegistered += HandleCareAction;
                boundManager.DailyRoutineCompleted += HandleDailyRoutineCompleted;
                boundManager.SaveDataReplaced += ApplySavedVolume;
                ApplySavedVolume();
            }
        }

        public void PlayUiClick()
        {
            PlayEffect(uiClickClip, 0.55f);
        }

        public void PlayPet()
        {
            PlayEffect(petClip, 0.9f);
        }

        public void PlayReturnSummary()
        {
            PlayEffect(returnClip, 0.9f);
        }

        public void PlayReward()
        {
            PlayEffect(rewardClip, 1f);
        }

        public void ApplySavedVolume()
        {
            var settings = boundManager?.CurrentSave?.settings;
            ApplyVolumeSettings(settings);
        }

        public void ApplyVolumeSettings(GameSettingsSaveData settings)
        {
            if (settings == null)
            {
                return;
            }

            settings.EnsureRuntimeDefaults();
            AudioListener.volume = settings.muteAudio ? 0f : settings.masterVolume;
            EnsureSources();
            musicSource.volume = BackgroundVolume * settings.musicVolume;
            effectSource.volume = EffectVolume * settings.effectVolume;
        }

        private void HandleCareAction(string actionId)
        {
            if (string.Equals(actionId, "pet", StringComparison.Ordinal))
            {
                PlayPet();
                return;
            }

            PlayEffect(careClip, 0.78f);
        }

        private void HandleDailyRoutineCompleted()
        {
            PlayReward();
        }

        private void StartBackgroundMusic()
        {
            if (!Application.isPlaying || musicSource == null || musicSource.clip == null || musicSource.isPlaying)
            {
                return;
            }

            musicSource.Play();
        }

        private void PlayEffect(AudioClip clip, float volumeScale)
        {
            if (!Application.isPlaying || effectSource == null || clip == null)
            {
                return;
            }

            effectSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
        }

        private void EnsureAudioListener()
        {
            var listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            if (listeners.Length == 0 && GetComponent<AudioListener>() == null)
            {
                gameObject.AddComponent<AudioListener>();
            }
        }

        private void EnsureSources()
        {
            var sources = GetComponents<AudioSource>();
            if (sources.Length > 0)
            {
                musicSource = sources[0];
            }
            else
            {
                musicSource = gameObject.AddComponent<AudioSource>();
            }

            if (sources.Length > 1)
            {
                effectSource = sources[1];
            }
            else
            {
                effectSource = gameObject.AddComponent<AudioSource>();
            }

            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;
            musicSource.volume = BackgroundVolume;

            effectSource.playOnAwake = false;
            effectSource.loop = false;
            effectSource.spatialBlend = 0f;
            effectSource.volume = EffectVolume;
        }

        private void EnsureClips()
        {
            if (musicSource.clip == null)
            {
                musicSource.clip = CreateBackgroundLoop();
            }

            uiClickClip ??= CreateToneClip("CheeseTama UI Click", 0.08f, 620f, 880f, 0.18f);
            careClip ??= CreateToneClip("CheeseTama Care", 0.24f, 420f, 660f, 0.2f);
            petClip ??= CreateToneClip("CheeseTama Pet", 0.44f, 520f, 920f, 0.22f);
            rewardClip ??= CreateRewardClip("CheeseTama Daily Reward", 0.78f);
            returnClip ??= CreateRewardClip("CheeseTama Return", 0.58f);
        }

        private AudioClip CreateBackgroundLoop()
        {
            const float durationSeconds = 12f;
            var sampleCount = Mathf.CeilToInt(durationSeconds * SampleRate);
            var samples = new float[sampleCount];
            var notes = new[] { 261.63f, 329.63f, 392f, 329.63f, 293.66f, 349.23f, 440f, 349.23f };
            const float noteSeconds = 1.5f;

            for (var index = 0; index < sampleCount; index += 1)
            {
                var time = index / (float)SampleRate;
                var noteIndex = Mathf.FloorToInt(time / noteSeconds) % notes.Length;
                var noteTime = time % noteSeconds;
                var envelope = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(noteTime / 0.18f))
                    * Mathf.SmoothStep(1f, 0f, Mathf.Clamp01((noteTime - 1.05f) / 0.45f));
                var fundamental = Mathf.Sin(2f * Mathf.PI * notes[noteIndex] * time);
                var overtone = Mathf.Sin(2f * Mathf.PI * notes[noteIndex] * 2f * time) * 0.22f;
                var ambience = Mathf.Sin(2f * Mathf.PI * 65.41f * time) * 0.08f;
                samples[index] = (fundamental + overtone) * envelope * 0.12f + ambience * 0.1f;
            }

            ApplyLoopCrossfade(samples, Mathf.CeilToInt(0.2f * SampleRate));
            return RegisterClip(CreateClip("CheeseTama Milkroom BGM", samples));
        }

        private AudioClip CreateToneClip(
            string clipName,
            float durationSeconds,
            float startFrequency,
            float endFrequency,
            float amplitude)
        {
            var sampleCount = Mathf.Max(1, Mathf.CeilToInt(durationSeconds * SampleRate));
            var samples = new float[sampleCount];
            for (var index = 0; index < sampleCount; index += 1)
            {
                var normalized = index / (float)Mathf.Max(1, sampleCount - 1);
                var time = index / (float)SampleRate;
                var frequency = Mathf.Lerp(startFrequency, endFrequency, normalized);
                var attack = Mathf.Clamp01(normalized / 0.08f);
                var release = Mathf.Clamp01((1f - normalized) / 0.28f);
                var envelope = Mathf.SmoothStep(0f, 1f, attack) * Mathf.SmoothStep(0f, 1f, release);
                samples[index] = Mathf.Sin(2f * Mathf.PI * frequency * time) * envelope * amplitude;
            }

            return RegisterClip(CreateClip(clipName, samples));
        }

        private AudioClip CreateRewardClip(string clipName, float durationSeconds)
        {
            var sampleCount = Mathf.Max(1, Mathf.CeilToInt(durationSeconds * SampleRate));
            var samples = new float[sampleCount];
            var notes = new[] { 523.25f, 659.25f, 783.99f };
            var noteLength = durationSeconds / notes.Length;
            for (var index = 0; index < sampleCount; index += 1)
            {
                var time = index / (float)SampleRate;
                var noteIndex = Mathf.Min(notes.Length - 1, Mathf.FloorToInt(time / noteLength));
                var noteTime = time - noteIndex * noteLength;
                var normalized = Mathf.Clamp01(noteTime / noteLength);
                var envelope = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(normalized / 0.08f))
                    * Mathf.SmoothStep(1f, 0f, Mathf.Clamp01((normalized - 0.62f) / 0.38f));
                samples[index] = Mathf.Sin(2f * Mathf.PI * notes[noteIndex] * time) * envelope * 0.2f;
            }

            return RegisterClip(CreateClip(clipName, samples));
        }

        private static AudioClip CreateClip(string clipName, float[] samples)
        {
            var clip = AudioClip.Create(clipName, samples.Length, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip RegisterClip(AudioClip clip)
        {
            if (clip != null)
            {
                generatedClips.Add(clip);
            }

            return clip;
        }

        private static void ApplyLoopCrossfade(float[] samples, int crossfadeSamples)
        {
            if (samples == null || samples.Length < 2)
            {
                return;
            }

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
    }
}
