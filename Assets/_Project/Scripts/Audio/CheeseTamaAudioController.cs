using System;
using System.Collections;
using System.Collections.Generic;
using CheeseTama.Core;
using CheeseTama.Gameplay.Milk;
using CheeseTama.Save;
using UnityEngine;

namespace CheeseTama.Audio
{
    public readonly struct AudioPlaybackSnapshot
    {
        public AudioPlaybackSnapshot(
            bool usingAuthoredAudioAssets,
            int loadedAuthoredAssetCount,
            bool applicationFocused,
            bool applicationPaused,
            bool musicRequested,
            bool pausedForInterruption,
            bool crossfadeInProgress,
            int activeMusicVoiceCount,
            string activeMusicClipName,
            float activeMusicTimeSeconds,
            float combinedMusicGain,
            float listenerVolume,
            float musicOutputVolume,
            float effectOutputVolume)
        {
            UsingAuthoredAudioAssets = usingAuthoredAudioAssets;
            LoadedAuthoredAssetCount = loadedAuthoredAssetCount;
            ApplicationFocused = applicationFocused;
            ApplicationPaused = applicationPaused;
            MusicRequested = musicRequested;
            PausedForInterruption = pausedForInterruption;
            CrossfadeInProgress = crossfadeInProgress;
            ActiveMusicVoiceCount = activeMusicVoiceCount;
            ActiveMusicClipName = activeMusicClipName ?? string.Empty;
            ActiveMusicTimeSeconds = Mathf.Max(0f, activeMusicTimeSeconds);
            CombinedMusicGain = Mathf.Clamp01(combinedMusicGain);
            ListenerVolume = Mathf.Clamp01(listenerVolume);
            MusicOutputVolume = Mathf.Clamp01(musicOutputVolume);
            EffectOutputVolume = Mathf.Clamp01(effectOutputVolume);
        }

        public bool UsingAuthoredAudioAssets { get; }
        public int LoadedAuthoredAssetCount { get; }
        public bool ApplicationFocused { get; }
        public bool ApplicationPaused { get; }
        public bool MusicRequested { get; }
        public bool PausedForInterruption { get; }
        public bool CrossfadeInProgress { get; }
        public int ActiveMusicVoiceCount { get; }
        public string ActiveMusicClipName { get; }
        public float ActiveMusicTimeSeconds { get; }
        public float CombinedMusicGain { get; }
        public float ListenerVolume { get; }
        public float MusicOutputVolume { get; }
        public float EffectOutputVolume { get; }
        public bool IsInterrupted => ApplicationPaused || !ApplicationFocused;
    }

    public static class AudioMasteringRules
    {
        public static void GetComplementaryCrossfadeGains(float progress, out float outgoing, out float incoming)
        {
            incoming = Mathf.Clamp01(progress);
            outgoing = 1f - incoming;
        }

        public static float ClampOneShotScale(float requestedScale)
        {
            return Mathf.Clamp(requestedScale, 0f, 1f);
        }
    }

    public sealed class CheeseTamaAudioController : MonoBehaviour
    {
        private const int SampleRate = 22050;
        private const float BackgroundVolume = 0.2f;
        private const float EffectVolume = 0.42f;
        private const float DefaultMusicCrossfadeSeconds = 0.35f;
        private const double MinimumEffectRetriggerSeconds = 0.03d;
        private const string BackgroundClipPath = "Audio/milkroom_loop";
        private const string UiClickClipPath = "Audio/ui_click";
        private const string CareClipPath = "Audio/care";
        private const string PetClipPath = "Audio/pet";
        private const string RewardClipPath = "Audio/reward";
        private const string ReturnClipPath = "Audio/return";
        private const string MilkBlendClipPath = "Audio/milk_blend";
        private const string RareDiscoveryClipPath = "Audio/rare_discovery";

        private readonly List<AudioClip> generatedClips = new List<AudioClip>();

        private AudioSource musicSource;
        private AudioSource alternateMusicSource;
        private AudioSource effectSource;
        private AudioClip uiClickClip;
        private AudioClip careClip;
        private AudioClip petClip;
        private AudioClip rewardClip;
        private AudioClip returnClip;
        private AudioClip milkBlendClip;
        private AudioClip rareDiscoveryClip;
        private GameManager boundManager;
        private bool usingAuthoredAudioAssets;
        private int loadedAuthoredAssetCount;
        private float musicChannelVolume = 1f;
        private float effectChannelVolume = 1f;
        private float musicSourceGain = 1f;
        private float alternateMusicSourceGain;
        private bool applicationFocused = true;
        private bool applicationPaused;
        private bool musicRequested;
        private bool pausedForInterruption;
        private bool crossfadeInProgress;
        private Coroutine musicCrossfadeRoutine;
        private AudioClip lastEffectClip;
        private double lastEffectDspTime = double.NegativeInfinity;

        public static CheeseTamaAudioController Instance { get; private set; }
        public AudioSource MusicSource => GetDominantMusicSource();
        public AudioSource AlternateMusicSource => alternateMusicSource;
        public AudioSource EffectSource => effectSource;
        public bool UsingAuthoredAudioAssets => usingAuthoredAudioAssets;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                if (Instance.gameObject != gameObject)
                {
                    StopOwnedAudioSources();
                }

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
            if (!IsApplicationInterrupted())
            {
                ResumeAfterInterruption();
            }
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
            CollapseMusicCrossfade();
            PauseForInterruption();
        }

        private void OnDestroy()
        {
            BindManager(null);
            StopMusicCrossfade();
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

        private void OnApplicationFocus(bool hasFocus)
        {
            applicationFocused = hasFocus;
            RefreshApplicationInterruption();
        }

        private void OnApplicationPause(bool paused)
        {
            applicationPaused = paused;
            RefreshApplicationInterruption();
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
                boundManager.MilkBlendingChanged -= HandleMilkBlendingChanged;
                boundManager.SaveDataReplaced -= ApplySavedVolume;
            }

            boundManager = manager;
            if (boundManager != null)
            {
                boundManager.CareActionRegistered += HandleCareAction;
                boundManager.DailyRoutineCompleted += HandleDailyRoutineCompleted;
                boundManager.MilkBlendingChanged += HandleMilkBlendingChanged;
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

        public void ReloadAudioAssets()
        {
            EnsureSources();
            EnsureClips();
        }

        public AudioPlaybackSnapshot GetPlaybackSnapshot()
        {
            EnsureSources();
            var dominantSource = GetDominantMusicSource();
            var activeVoiceCount = 0;
            if (musicSource != null && musicSource.isPlaying)
            {
                activeVoiceCount += 1;
            }

            if (alternateMusicSource != null && alternateMusicSource.isPlaying)
            {
                activeVoiceCount += 1;
            }

            return new AudioPlaybackSnapshot(
                usingAuthoredAudioAssets,
                loadedAuthoredAssetCount,
                applicationFocused,
                applicationPaused,
                musicRequested,
                pausedForInterruption,
                crossfadeInProgress,
                activeVoiceCount,
                dominantSource != null && dominantSource.clip != null ? dominantSource.clip.name : string.Empty,
                dominantSource != null ? dominantSource.time : 0f,
                musicSourceGain + alternateMusicSourceGain,
                AudioListener.volume,
                dominantSource != null ? dominantSource.volume : 0f,
                effectSource != null ? effectSource.volume : 0f);
        }

        public void PlayBackgroundMusic(AudioClip clip, float crossfadeSeconds = DefaultMusicCrossfadeSeconds)
        {
            EnsureSources();
            musicRequested = clip != null;
            if (clip == null)
            {
                StopBackgroundMusic();
                return;
            }

            if (!Application.isPlaying)
            {
                musicSource.clip = clip;
                musicSourceGain = 1f;
                alternateMusicSourceGain = 0f;
                ApplySourceVolumes();
                return;
            }

            if (IsApplicationInterrupted())
            {
                StopMusicCrossfade();
                musicSource.Stop();
                alternateMusicSource.Stop();
                musicSource.clip = clip;
                musicSourceGain = 1f;
                alternateMusicSourceGain = 0f;
                ApplySourceVolumes();
                pausedForInterruption = true;
                return;
            }

            var current = GetPlayingMusicSource();
            if (current != null && current.clip == clip)
            {
                StopMusicCrossfade();
                var duplicate = current == musicSource ? alternateMusicSource : musicSource;
                if (duplicate != null)
                {
                    duplicate.Stop();
                }

                SetMusicGain(current, 1f);
                SetMusicGain(duplicate, 0f);
                ApplySourceVolumes();
                return;
            }

            var target = current == musicSource ? alternateMusicSource : musicSource;
            target.Stop();
            target.clip = clip;
            target.time = 0f;
            target.Play();
            SetMusicGain(target, 0f);

            StopMusicCrossfade();
            musicCrossfadeRoutine = StartCoroutine(CrossfadeMusic(current, target, crossfadeSeconds));
        }

        public void StopBackgroundMusic()
        {
            musicRequested = false;
            pausedForInterruption = false;
            StopMusicCrossfade();
            if (musicSource != null)
            {
                musicSource.Stop();
            }

            if (alternateMusicSource != null)
            {
                alternateMusicSource.Stop();
            }

            musicSourceGain = 1f;
            alternateMusicSourceGain = 0f;
            ApplySourceVolumes();
        }

        public void PlayMilkBlend(bool rareResult = false)
        {
            PlayEffect(rareResult ? rareDiscoveryClip : milkBlendClip, rareResult ? 1f : 0.9f);
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
            musicChannelVolume = settings.musicVolume;
            effectChannelVolume = settings.effectVolume;
            ApplySourceVolumes();
        }

        private void HandleCareAction(string actionId)
        {
            if (string.Equals(actionId, "pet", StringComparison.Ordinal))
            {
                PlayPet();
                return;
            }

            // Successful blending emits a richer result event immediately after
            // the generic care registration, so avoid stacking two cues.
            if (string.Equals(actionId, "blend", StringComparison.Ordinal))
            {
                return;
            }

            PlayEffect(careClip, 0.78f);
        }

        private void HandleDailyRoutineCompleted()
        {
            PlayReward();
        }

        private void HandleMilkBlendingChanged(MilkBlendResult result)
        {
            if (result == null || !result.applied)
            {
                return;
            }

            PlayMilkBlend(result.specialResult);
        }

        private void StartBackgroundMusic()
        {
            if (musicSource == null || musicSource.clip == null)
            {
                return;
            }

            PlayBackgroundMusic(musicSource.clip);
        }

        private void PlayEffect(AudioClip clip, float volumeScale)
        {
            if (!Application.isPlaying || effectSource == null || clip == null || IsApplicationInterrupted())
            {
                return;
            }

            var now = AudioSettings.dspTime;
            if (lastEffectClip == clip && now - lastEffectDspTime < MinimumEffectRetriggerSeconds)
            {
                return;
            }

            lastEffectClip = clip;
            lastEffectDspTime = now;
            effectSource.PlayOneShot(clip, AudioMasteringRules.ClampOneShotScale(volumeScale));
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

            if (alternateMusicSource == null)
            {
                var crossfadeVoice = transform.Find("Music Crossfade Voice");
                if (crossfadeVoice == null)
                {
                    var crossfadeObject = new GameObject("Music Crossfade Voice");
                    crossfadeObject.transform.SetParent(transform, false);
                    crossfadeVoice = crossfadeObject.transform;
                }

                alternateMusicSource = crossfadeVoice.GetComponent<AudioSource>();
                if (alternateMusicSource == null)
                {
                    alternateMusicSource = crossfadeVoice.gameObject.AddComponent<AudioSource>();
                }
            }

            ConfigureMusicSource(musicSource);
            ConfigureMusicSource(alternateMusicSource);

            effectSource.playOnAwake = false;
            effectSource.loop = false;
            effectSource.spatialBlend = 0f;
            ApplySourceVolumes();
        }

        private void EnsureClips()
        {
            var loadedAssetCount = 0;
            var backgroundClip = LoadAuthoredClip(BackgroundClipPath, ref loadedAssetCount);
            if (backgroundClip != null)
            {
                musicSource.clip = backgroundClip;
            }
            else if (musicSource.clip == null)
            {
                musicSource.clip = CreateBackgroundLoop();
            }

            uiClickClip = LoadAuthoredClip(UiClickClipPath, ref loadedAssetCount)
                ?? uiClickClip
                ?? CreateToneClip("CheeseTama UI Click", 0.08f, 620f, 880f, 0.18f);
            careClip = LoadAuthoredClip(CareClipPath, ref loadedAssetCount)
                ?? careClip
                ?? CreateToneClip("CheeseTama Care", 0.24f, 420f, 660f, 0.2f);
            petClip = LoadAuthoredClip(PetClipPath, ref loadedAssetCount)
                ?? petClip
                ?? CreateToneClip("CheeseTama Pet", 0.44f, 520f, 920f, 0.22f);
            rewardClip = LoadAuthoredClip(RewardClipPath, ref loadedAssetCount)
                ?? rewardClip
                ?? CreateRewardClip("CheeseTama Daily Reward", 0.78f);
            returnClip = LoadAuthoredClip(ReturnClipPath, ref loadedAssetCount)
                ?? returnClip
                ?? CreateRewardClip("CheeseTama Return", 0.58f);
            milkBlendClip = LoadAuthoredClip(MilkBlendClipPath, ref loadedAssetCount)
                ?? milkBlendClip
                ?? CreateToneClip("CheeseTama Milk Blend", 0.62f, 310f, 740f, 0.2f);
            rareDiscoveryClip = LoadAuthoredClip(RareDiscoveryClipPath, ref loadedAssetCount)
                ?? rareDiscoveryClip
                ?? CreateRewardClip("CheeseTama Rare Discovery", 0.96f);

            loadedAuthoredAssetCount = loadedAssetCount;
            usingAuthoredAudioAssets = loadedAssetCount == 8;
        }

        private static void ConfigureMusicSource(AudioSource source)
        {
            if (source == null)
            {
                return;
            }

            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
        }

        private void ApplySourceVolumes()
        {
            if (musicSource != null)
            {
                musicSource.volume = Mathf.Clamp01(BackgroundVolume * musicChannelVolume * musicSourceGain);
            }

            if (alternateMusicSource != null)
            {
                alternateMusicSource.volume = Mathf.Clamp01(
                    BackgroundVolume * musicChannelVolume * alternateMusicSourceGain);
            }

            if (effectSource != null)
            {
                effectSource.volume = Mathf.Clamp01(EffectVolume * effectChannelVolume);
            }
        }

        private AudioSource GetDominantMusicSource()
        {
            if (alternateMusicSource != null
                && (alternateMusicSource.isPlaying || alternateMusicSource.clip != null)
                && alternateMusicSourceGain > musicSourceGain)
            {
                return alternateMusicSource;
            }

            return musicSource;
        }

        private AudioSource GetPlayingMusicSource()
        {
            var primaryPlaying = musicSource != null && musicSource.isPlaying;
            var alternatePlaying = alternateMusicSource != null && alternateMusicSource.isPlaying;
            if (primaryPlaying && alternatePlaying)
            {
                return alternateMusicSourceGain > musicSourceGain ? alternateMusicSource : musicSource;
            }

            if (primaryPlaying)
            {
                return musicSource;
            }

            return alternatePlaying ? alternateMusicSource : null;
        }

        private float GetMusicGain(AudioSource source)
        {
            return source == alternateMusicSource ? alternateMusicSourceGain : musicSourceGain;
        }

        private void SetMusicGain(AudioSource source, float gain)
        {
            if (source == null)
            {
                return;
            }

            if (source == alternateMusicSource)
            {
                alternateMusicSourceGain = Mathf.Clamp01(gain);
            }
            else if (source == musicSource)
            {
                musicSourceGain = Mathf.Clamp01(gain);
            }
        }

        private IEnumerator CrossfadeMusic(AudioSource outgoing, AudioSource incoming, float durationSeconds)
        {
            crossfadeInProgress = true;
            var safeDuration = Mathf.Max(0f, durationSeconds);
            var outgoingStartGain = outgoing != null ? GetMusicGain(outgoing) : 0f;

            if (safeDuration <= 0f)
            {
                CompleteMusicCrossfade(outgoing, incoming);
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < safeDuration)
            {
                if (IsApplicationInterrupted())
                {
                    yield return null;
                    continue;
                }

                elapsed += Time.unscaledDeltaTime;
                AudioMasteringRules.GetComplementaryCrossfadeGains(
                    elapsed / safeDuration,
                    out var outgoingGain,
                    out var incomingGain);
                SetMusicGain(outgoing, outgoingStartGain * outgoingGain);
                SetMusicGain(incoming, incomingGain);
                ApplySourceVolumes();
                yield return null;
            }

            CompleteMusicCrossfade(outgoing, incoming);
        }

        private void CompleteMusicCrossfade(AudioSource outgoing, AudioSource incoming)
        {
            if (outgoing != null && outgoing != incoming)
            {
                outgoing.Stop();
                SetMusicGain(outgoing, 0f);
            }

            SetMusicGain(incoming, 1f);
            ApplySourceVolumes();
            crossfadeInProgress = false;
            musicCrossfadeRoutine = null;
        }

        private void StopMusicCrossfade()
        {
            if (musicCrossfadeRoutine != null)
            {
                StopCoroutine(musicCrossfadeRoutine);
                musicCrossfadeRoutine = null;
            }

            crossfadeInProgress = false;
        }

        private void CollapseMusicCrossfade()
        {
            if (!crossfadeInProgress)
            {
                return;
            }

            var dominant = GetDominantMusicSource();
            var other = dominant == musicSource ? alternateMusicSource : musicSource;
            StopMusicCrossfade();
            other?.Stop();
            SetMusicGain(dominant, 1f);
            SetMusicGain(other, 0f);
            ApplySourceVolumes();
        }

        private bool IsApplicationInterrupted()
        {
            return applicationPaused || !applicationFocused;
        }

        private void RefreshApplicationInterruption()
        {
            if (IsApplicationInterrupted())
            {
                PauseForInterruption();
                return;
            }

            ResumeAfterInterruption();
        }

        private void PauseForInterruption()
        {
            pausedForInterruption = pausedForInterruption || musicRequested;
            musicSource?.Pause();
            alternateMusicSource?.Pause();
            effectSource?.Pause();
        }

        private void ResumeAfterInterruption()
        {
            if (!pausedForInterruption)
            {
                return;
            }

            musicSource?.UnPause();
            alternateMusicSource?.UnPause();
            effectSource?.UnPause();
            pausedForInterruption = false;

            if (musicRequested && GetPlayingMusicSource() == null)
            {
                var source = GetDominantMusicSource();
                if (source != null && source.clip != null)
                {
                    PlayBackgroundMusic(source.clip, DefaultMusicCrossfadeSeconds);
                }
            }
        }

        private void StopOwnedAudioSources()
        {
            var sources = GetComponentsInChildren<AudioSource>(true);
            for (var index = 0; index < sources.Length; index += 1)
            {
                sources[index].Stop();
            }
        }

        private static AudioClip LoadAuthoredClip(string resourcePath, ref int loadedAssetCount)
        {
            var clip = Resources.Load<AudioClip>(resourcePath);
            if (clip != null)
            {
                loadedAssetCount += 1;
            }

            return clip;
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
