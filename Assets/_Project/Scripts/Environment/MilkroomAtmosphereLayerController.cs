using System;
using CheeseTama.Core;
using CheeseTama.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.Environment
{
    public enum MilkroomTimeBand
    {
        Morning = 0,
        Afternoon = 1,
        Evening = 2,
        Night = 3
    }

    public enum MilkroomAtmosphereCondition
    {
        Normal = 0,
        Hungry = 1,
        Sleepy = 2,
        Messy = 3,
        Sick = 4
    }

    public readonly struct MilkroomAtmosphereLayerData
    {
        public MilkroomAtmosphereLayerData(
            MilkroomTimeBand timeBand,
            MilkroomAtmosphereCondition condition,
            Color overlayColor,
            float overlayOpacity,
            Color auxiliaryLightColor,
            float auxiliaryLightIntensity)
        {
            this.timeBand = timeBand;
            this.condition = condition;
            this.overlayColor = overlayColor;
            this.overlayOpacity = Mathf.Clamp01(overlayOpacity);
            this.auxiliaryLightColor = auxiliaryLightColor;
            this.auxiliaryLightIntensity = Mathf.Max(0f, auxiliaryLightIntensity);
        }

        public readonly MilkroomTimeBand timeBand;
        public readonly MilkroomAtmosphereCondition condition;
        public readonly Color overlayColor;
        public readonly float overlayOpacity;
        public readonly Color auxiliaryLightColor;
        public readonly float auxiliaryLightIntensity;
    }

    public static class MilkroomAtmosphereLayerRules
    {
        public const int MorningStartsAtHour = 5;
        public const int AfternoonStartsAtHour = 11;
        public const int EveningStartsAtHour = 17;
        public const int NightStartsAtHour = 22;

        public const int HungryThreshold = 25;
        public const int MessyThreshold = 35;
        public const int SickThreshold = 35;
        public const int SleepyThreshold = 75;

        public const float MaximumOverlayOpacity = 0.16f;
        public const float MaximumAuxiliaryLightIntensity = 0.32f;

        public static MilkroomAtmosphereLayerData Evaluate(
            DateTimeOffset localTime,
            CheeseTamaModel tama)
        {
            var timeBand = ResolveTimeBand(localTime.Hour);
            var condition = ResolveCondition(tama);
            ResolveTimeLayer(
                timeBand,
                out var timeColor,
                out var timeOpacity,
                out var lightColor,
                out var lightIntensity);

            if (condition == MilkroomAtmosphereCondition.Normal)
            {
                return new MilkroomAtmosphereLayerData(
                    timeBand,
                    condition,
                    timeColor,
                    Mathf.Min(MaximumOverlayOpacity, timeOpacity),
                    lightColor,
                    Mathf.Min(MaximumAuxiliaryLightIntensity, lightIntensity));
            }

            ResolveConditionLayer(
                condition,
                out var conditionColor,
                out var conditionOpacity,
                out var conditionLightColor,
                out var conditionLightIntensity);

            return new MilkroomAtmosphereLayerData(
                timeBand,
                condition,
                Color.Lerp(timeColor, conditionColor, 0.62f),
                Mathf.Min(MaximumOverlayOpacity, timeOpacity + conditionOpacity),
                Color.Lerp(lightColor, conditionLightColor, 0.58f),
                Mathf.Min(
                    MaximumAuxiliaryLightIntensity,
                    lightIntensity + conditionLightIntensity));
        }

        public static MilkroomTimeBand ResolveTimeBand(int hour)
        {
            var normalizedHour = ((hour % 24) + 24) % 24;
            if (normalizedHour >= NightStartsAtHour || normalizedHour < MorningStartsAtHour)
            {
                return MilkroomTimeBand.Night;
            }

            if (normalizedHour < AfternoonStartsAtHour)
            {
                return MilkroomTimeBand.Morning;
            }

            if (normalizedHour < EveningStartsAtHour)
            {
                return MilkroomTimeBand.Afternoon;
            }

            return MilkroomTimeBand.Evening;
        }

        public static MilkroomAtmosphereCondition ResolveCondition(CheeseTamaModel tama)
        {
            if (tama?.stats == null)
            {
                return MilkroomAtmosphereCondition.Normal;
            }

            // Match the character condition presentation priority so the room never
            // communicates a different primary need from CheeseTama itself.
            if (tama.stats.health < SickThreshold)
            {
                return MilkroomAtmosphereCondition.Sick;
            }

            if (tama.stats.hunger < HungryThreshold)
            {
                return MilkroomAtmosphereCondition.Hungry;
            }

            if (tama.stats.cleanliness < MessyThreshold)
            {
                return MilkroomAtmosphereCondition.Messy;
            }

            if (tama.stats.sleepiness > SleepyThreshold)
            {
                return MilkroomAtmosphereCondition.Sleepy;
            }

            return MilkroomAtmosphereCondition.Normal;
        }

        private static void ResolveTimeLayer(
            MilkroomTimeBand timeBand,
            out Color overlayColor,
            out float overlayOpacity,
            out Color lightColor,
            out float lightIntensity)
        {
            switch (timeBand)
            {
                case MilkroomTimeBand.Morning:
                    overlayColor = new Color(1f, 0.91f, 0.68f);
                    overlayOpacity = 0.025f;
                    lightColor = new Color(1f, 0.88f, 0.66f);
                    lightIntensity = 0.08f;
                    return;

                case MilkroomTimeBand.Afternoon:
                    overlayColor = new Color(0.84f, 0.94f, 1f);
                    overlayOpacity = 0.015f;
                    lightColor = new Color(0.88f, 0.95f, 1f);
                    lightIntensity = 0.06f;
                    return;

                case MilkroomTimeBand.Evening:
                    overlayColor = new Color(1f, 0.62f, 0.36f);
                    overlayOpacity = 0.055f;
                    lightColor = new Color(1f, 0.58f, 0.3f);
                    lightIntensity = 0.12f;
                    return;

                default:
                    overlayColor = new Color(0.28f, 0.4f, 0.72f);
                    overlayOpacity = 0.08f;
                    lightColor = new Color(0.42f, 0.56f, 0.92f);
                    lightIntensity = 0.1f;
                    return;
            }
        }

        private static void ResolveConditionLayer(
            MilkroomAtmosphereCondition condition,
            out Color overlayColor,
            out float overlayOpacity,
            out Color lightColor,
            out float lightIntensity)
        {
            switch (condition)
            {
                case MilkroomAtmosphereCondition.Hungry:
                    overlayColor = new Color(0.72f, 0.58f, 0.9f);
                    overlayOpacity = 0.025f;
                    lightColor = new Color(0.78f, 0.66f, 0.96f);
                    lightIntensity = 0.06f;
                    return;

                case MilkroomAtmosphereCondition.Sleepy:
                    overlayColor = new Color(0.4f, 0.48f, 0.76f);
                    overlayOpacity = 0.035f;
                    lightColor = new Color(0.52f, 0.58f, 0.9f);
                    lightIntensity = 0.045f;
                    return;

                case MilkroomAtmosphereCondition.Messy:
                    overlayColor = new Color(0.46f, 0.36f, 0.25f);
                    overlayOpacity = 0.045f;
                    lightColor = new Color(0.69f, 0.54f, 0.36f);
                    lightIntensity = 0.04f;
                    return;

                default:
                    overlayColor = new Color(0.48f, 0.73f, 0.66f);
                    overlayOpacity = 0.055f;
                    lightColor = new Color(0.58f, 0.9f, 0.79f);
                    lightIntensity = 0.07f;
                    return;
            }
        }
    }

    /// <summary>
    /// Applies only a transparent overlay and a dedicated auxiliary light. It never
    /// writes to MilkroomThemeController, the theme palette, RenderSettings, camera,
    /// or the theme-owned key/fill/rim lights.
    /// </summary>
    public sealed class MilkroomAtmosphereLayerController : MonoBehaviour
    {
        private const float RefreshIntervalSeconds = 1f;

        [SerializeField] private Image colorOverlay;
        [SerializeField] private Light auxiliaryLight;

        private CheeseTamaModel boundTama;
        private float refreshAccumulator;
        private MilkroomAtmosphereLayerData currentLayer;

        public MilkroomAtmosphereLayerData CurrentLayer => currentLayer;
        public bool IsConfigured => colorOverlay != null || auxiliaryLight != null;

        public void Configure(Image overlay, Light atmosphereLight)
        {
            colorOverlay = overlay;
            auxiliaryLight = atmosphereLight;
            if (colorOverlay != null)
            {
                colorOverlay.raycastTarget = false;
            }

            RefreshNow();
        }

        public void Bind(CheeseTamaModel tama)
        {
            boundTama = tama;
            RefreshNow();
        }

        public void RefreshNow()
        {
            Refresh(DateTimeOffset.Now);
        }

        public void Refresh(DateTimeOffset localTime)
        {
            // Reload/reset replaces the model instance. Prefer the live authority when
            // available while retaining explicit Bind support for previews and tests.
            var liveTama = GameManager.Instance?.CurrentTama;
            if (liveTama != null && !ReferenceEquals(liveTama, boundTama))
            {
                boundTama = liveTama;
            }

            currentLayer = MilkroomAtmosphereLayerRules.Evaluate(localTime, boundTama);
            Apply(currentLayer);
            refreshAccumulator = 0f;
        }

        private void OnEnable()
        {
            RefreshNow();
        }

        private void OnDisable()
        {
            if (colorOverlay != null)
            {
                var color = colorOverlay.color;
                color.a = 0f;
                colorOverlay.color = color;
            }

            if (auxiliaryLight != null)
            {
                auxiliaryLight.intensity = 0f;
            }
        }

        private void Update()
        {
            refreshAccumulator += Mathf.Max(0f, Time.unscaledDeltaTime);
            if (refreshAccumulator >= RefreshIntervalSeconds)
            {
                RefreshNow();
            }
        }

        private void Apply(MilkroomAtmosphereLayerData layer)
        {
            if (colorOverlay != null)
            {
                var overlayColor = layer.overlayColor;
                overlayColor.a = layer.overlayOpacity;
                colorOverlay.color = overlayColor;
                colorOverlay.raycastTarget = false;
            }

            if (auxiliaryLight != null)
            {
                auxiliaryLight.color = layer.auxiliaryLightColor;
                auxiliaryLight.intensity = layer.auxiliaryLightIntensity;
            }
        }
    }
}
