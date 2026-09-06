using System;
using CheeseTama.Core;
using CheeseTama.Gameplay.Events;
using CheeseTama.UI;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.Environment
{
    public readonly struct MilkroomSeasonalLayerData
    {
        public MilkroomSeasonalLayerData(
            MilkroomSeason season,
            Color tint,
            float opacity)
        {
            Season = season;
            Tint = tint;
            Opacity = Mathf.Clamp(opacity, 0f, 0.04f);
        }

        public MilkroomSeason Season { get; }
        public Color Tint { get; }
        public float Opacity { get; }
    }

    public static class MilkroomSeasonalLayerRules
    {
        public static MilkroomSeasonalLayerData Evaluate(DateTimeOffset localTime)
        {
            var season = SeasonalCareEventCatalog.ResolveSeason(localTime);
            return season switch
            {
                MilkroomSeason.Spring => new MilkroomSeasonalLayerData(
                    season,
                    new Color(1f, 0.73f, 0.82f),
                    0.012f),
                MilkroomSeason.Summer => new MilkroomSeasonalLayerData(
                    season,
                    new Color(0.55f, 0.9f, 1f),
                    0.01f),
                MilkroomSeason.Autumn => new MilkroomSeasonalLayerData(
                    season,
                    new Color(1f, 0.58f, 0.25f),
                    0.018f),
                _ => new MilkroomSeasonalLayerData(
                    season,
                    new Color(0.65f, 0.82f, 1f),
                    0.016f)
            };
        }
    }

    /// <summary>
    /// Adds a presentation-only calendar tint behind Milkroom UI. The authored theme
    /// remains authoritative; this layer never changes materials, lights, or saved theme.
    /// </summary>
    public sealed class MilkroomSeasonalLayerController : MonoBehaviour
    {
        private const float CalendarRefreshSeconds = 60f;
        private const float EventPulseSeconds = 3f;
        private const float EventPulseOpacity = 0.035f;

        [SerializeField] private Image colorOverlay;

        private GameManager boundManager;
        private float refreshAccumulator;
        private float pulseRemaining;
        private MilkroomSeasonalLayerData currentLayer;

        public MilkroomSeasonalLayerData CurrentLayer => currentLayer;
        public bool IsConfigured => colorOverlay != null;

        public void Configure(Image overlay, GameManager manager)
        {
            colorOverlay = overlay;
            if (colorOverlay != null)
            {
                colorOverlay.raycastTarget = false;
            }

            BindManager(manager);
            Refresh(DateTimeOffset.Now);
        }

        public void BindManager(GameManager manager)
        {
            if (ReferenceEquals(boundManager, manager))
            {
                return;
            }

            if (boundManager != null)
            {
                boundManager.CareEventAvailable -= HandleCareEventAvailable;
            }

            boundManager = manager;
            if (isActiveAndEnabled && boundManager != null)
            {
                boundManager.CareEventAvailable -= HandleCareEventAvailable;
                boundManager.CareEventAvailable += HandleCareEventAvailable;
            }
        }

        public void Refresh(DateTimeOffset localTime)
        {
            currentLayer = MilkroomSeasonalLayerRules.Evaluate(localTime);
            refreshAccumulator = 0f;
            Apply();
        }

        private void OnEnable()
        {
            if (boundManager == null)
            {
                boundManager = GameManager.Instance;
            }

            if (boundManager != null)
            {
                boundManager.CareEventAvailable -= HandleCareEventAvailable;
                boundManager.CareEventAvailable += HandleCareEventAvailable;
            }

            Refresh(DateTimeOffset.Now);
        }

        private void OnDisable()
        {
            if (boundManager != null)
            {
                boundManager.CareEventAvailable -= HandleCareEventAvailable;
            }

            pulseRemaining = 0f;
            if (colorOverlay != null)
            {
                var color = colorOverlay.color;
                color.a = 0f;
                colorOverlay.color = color;
            }
        }

        private void Update()
        {
            var delta = Mathf.Max(0f, Time.unscaledDeltaTime);
            refreshAccumulator += delta;
            if (refreshAccumulator >= CalendarRefreshSeconds)
            {
                Refresh(DateTimeOffset.Now);
            }

            if (pulseRemaining > 0f)
            {
                pulseRemaining = Mathf.Max(0f, pulseRemaining - delta);
                Apply();
            }
        }

        private void HandleCareEventAvailable(CareEventResult result)
        {
            if (SeasonalCareEventCatalog.Find(result.eventId) == null)
            {
                return;
            }

            pulseRemaining = AccessibilityRuntime.ReducedMotion ? 0f : EventPulseSeconds;
            Apply();
        }

        private void Apply()
        {
            if (colorOverlay == null)
            {
                return;
            }

            var color = currentLayer.Tint;
            var pulse = pulseRemaining <= 0f || AccessibilityRuntime.ReducedMotion
                ? 0f
                : EventPulseOpacity * Mathf.Clamp01(pulseRemaining / EventPulseSeconds);
            color.a = Mathf.Clamp(currentLayer.Opacity + pulse, 0f, 0.06f);
            colorOverlay.color = color;
            colorOverlay.raycastTarget = false;
        }
    }
}
