using System;
using CheeseTama.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CheeseTama.Gameplay.Input
{
    public enum DirectManipulationKind
    {
        None,
        MilkBottleDrag,
        CircularStir,
        CleaningSwipe
    }

    public readonly struct DirectManipulationProgress
    {
        public DirectManipulationProgress(
            DirectManipulationKind kind,
            float normalizedProgress,
            Vector2 screenPosition,
            bool isActive)
        {
            Kind = kind;
            NormalizedProgress = Mathf.Clamp01(normalizedProgress);
            ScreenPosition = screenPosition;
            IsActive = isActive;
        }

        public DirectManipulationKind Kind { get; }
        public float NormalizedProgress { get; }
        public Vector2 ScreenPosition { get; }
        public bool IsActive { get; }
    }

    /// <summary>
    /// Owns the deterministic rules for one mouse or touch pointer sequence. Authority is never
    /// committed here; the UI adapter invokes its configured completion callback only after End
    /// accepts a release inside the authoritative target.
    /// </summary>
    public sealed class DirectManipulationGestureTracker
    {
        public const float DefaultMilkDragDistancePixels = 72f;
        public const float DefaultStirDegrees = 300f;
        public const float DefaultStirMinimumRadiusPixels = 24f;
        public const float DefaultStirMaximumRadiusPixels = 180f;
        public const float DefaultCleaningPathPixels = 120f;
        public const float DefaultCleaningSpanPixels = 48f;

        private const float PointerMovementSlopPixels = 8f;
        private const float MinimumPathSamplePixels = 0.5f;
        private const float MinimumAngularSampleDegrees = 0.25f;
        private const float MaximumAngularSampleDegrees = 90f;
        private const float CompletionTolerance = 0.0001f;

        private DirectManipulationKind kind;
        private float primaryRequirement;
        private float secondaryRequirement;
        private float minimumRadius;
        private float maximumRadius;
        private int pointerId;
        private Vector2 startPosition;
        private Vector2 currentPosition;
        private Vector2 stirCenter;
        private float travelledPath;
        private float maximumSpan;
        private float accumulatedStirDegrees;
        private float previousStirAngle;
        private int stirDirection;
        private bool hasStirSample;
        private bool active;
        private bool meaningfulMovement;
        private float progress;

        public DirectManipulationKind Kind => kind;
        public bool IsActive => active;
        public bool HadMeaningfulMovement => meaningfulMovement;
        public int PointerId => pointerId;
        public Vector2 StartPosition => startPosition;
        public Vector2 CurrentPosition => currentPosition;
        public float Progress => progress;
        public bool IsReadyToComplete => progress >= 1f - CompletionTolerance;

        public void ConfigureMilkBottleDrag(float requiredDistancePixels)
        {
            ConfigureCommon(
                DirectManipulationKind.MilkBottleDrag,
                Mathf.Max(1f, requiredDistancePixels),
                0f,
                0f,
                float.PositiveInfinity);
        }

        public void ConfigureCircularStir(
            float requiredDegrees,
            float minimumRadiusPixels,
            float maximumRadiusPixels)
        {
            var resolvedMinimum = Mathf.Max(1f, minimumRadiusPixels);
            var resolvedMaximum = float.IsInfinity(maximumRadiusPixels)
                ? float.PositiveInfinity
                : Mathf.Max(resolvedMinimum + 1f, maximumRadiusPixels);
            ConfigureCommon(
                DirectManipulationKind.CircularStir,
                Mathf.Max(30f, requiredDegrees),
                0f,
                resolvedMinimum,
                resolvedMaximum);
        }

        public void ConfigureCleaningSwipe(
            float requiredPathPixels,
            float requiredSpanPixels)
        {
            var path = Mathf.Max(1f, requiredPathPixels);
            ConfigureCommon(
                DirectManipulationKind.CleaningSwipe,
                path,
                Mathf.Clamp(requiredSpanPixels, 1f, path),
                0f,
                float.PositiveInfinity);
        }

        public bool Begin(int activePointerId, Vector2 position, Vector2 circularCenter)
        {
            if (active || kind == DirectManipulationKind.None)
            {
                return false;
            }

            pointerId = activePointerId;
            startPosition = position;
            currentPosition = position;
            stirCenter = circularCenter;
            travelledPath = 0f;
            maximumSpan = 0f;
            accumulatedStirDegrees = 0f;
            previousStirAngle = 0f;
            stirDirection = 0;
            hasStirSample = false;
            meaningfulMovement = false;
            progress = 0f;
            active = true;
            if (kind == DirectManipulationKind.CircularStir)
            {
                CaptureInitialStirSample(position);
            }

            return true;
        }

        public bool OwnsPointer(int activePointerId)
        {
            return active && activePointerId == pointerId;
        }

        public bool Track(int activePointerId, Vector2 position)
        {
            if (!OwnsPointer(activePointerId))
            {
                return false;
            }

            var previousPosition = currentPosition;
            currentPosition = position;
            var displacement = Vector2.Distance(startPosition, position);
            if (displacement >= PointerMovementSlopPixels)
            {
                meaningfulMovement = true;
            }

            switch (kind)
            {
                case DirectManipulationKind.MilkBottleDrag:
                    maximumSpan = Mathf.Max(maximumSpan, displacement);
                    progress = Mathf.Clamp01(maximumSpan / primaryRequirement);
                    break;
                case DirectManipulationKind.CircularStir:
                    TrackStir(position);
                    break;
                case DirectManipulationKind.CleaningSwipe:
                    var segmentLength = Vector2.Distance(previousPosition, position);
                    if (segmentLength >= MinimumPathSamplePixels)
                    {
                        travelledPath += segmentLength;
                    }

                    maximumSpan = Mathf.Max(maximumSpan, displacement);
                    progress = Mathf.Min(
                        Mathf.Clamp01(travelledPath / primaryRequirement),
                        Mathf.Clamp01(maximumSpan / secondaryRequirement));
                    break;
            }

            return true;
        }

        public bool End(
            int activePointerId,
            Vector2 position,
            bool releasedInsideTarget)
        {
            if (!OwnsPointer(activePointerId))
            {
                return false;
            }

            Track(activePointerId, position);
            var completed = releasedInsideTarget && IsReadyToComplete;
            active = false;
            hasStirSample = false;
            progress = completed ? 1f : 0f;
            return completed;
        }

        public bool Cancel()
        {
            var changed = active || progress > 0f;
            active = false;
            hasStirSample = false;
            meaningfulMovement = false;
            progress = 0f;
            return changed;
        }

        private void ConfigureCommon(
            DirectManipulationKind configuredKind,
            float configuredPrimaryRequirement,
            float configuredSecondaryRequirement,
            float configuredMinimumRadius,
            float configuredMaximumRadius)
        {
            Cancel();
            kind = configuredKind;
            primaryRequirement = configuredPrimaryRequirement;
            secondaryRequirement = configuredSecondaryRequirement;
            minimumRadius = configuredMinimumRadius;
            maximumRadius = configuredMaximumRadius;
        }

        private void CaptureInitialStirSample(Vector2 position)
        {
            var offset = position - stirCenter;
            var radius = offset.magnitude;
            if (radius < minimumRadius || radius > maximumRadius)
            {
                hasStirSample = false;
                return;
            }

            previousStirAngle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
            hasStirSample = true;
        }

        private void TrackStir(Vector2 position)
        {
            var offset = position - stirCenter;
            var radius = offset.magnitude;
            if (radius < minimumRadius || radius > maximumRadius)
            {
                // Restart angular sampling on re-entry so crossing the invalid center or leaving
                // the cup cannot create an artificial angle jump.
                hasStirSample = false;
                return;
            }

            var angle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
            if (!hasStirSample)
            {
                previousStirAngle = angle;
                hasStirSample = true;
                return;
            }

            var delta = Mathf.DeltaAngle(previousStirAngle, angle);
            previousStirAngle = angle;
            var absoluteDelta = Mathf.Abs(delta);
            if (absoluteDelta < MinimumAngularSampleDegrees
                || absoluteDelta > MaximumAngularSampleDegrees)
            {
                return;
            }

            meaningfulMovement = true;
            var sampleDirection = delta > 0f ? 1 : -1;
            if (stirDirection == 0)
            {
                stirDirection = sampleDirection;
                accumulatedStirDegrees = absoluteDelta;
            }
            else if (sampleDirection == stirDirection)
            {
                accumulatedStirDegrees += absoluteDelta;
            }
            else
            {
                accumulatedStirDegrees -= absoluteDelta;
                if (accumulatedStirDegrees < 0f)
                {
                    accumulatedStirDegrees = -accumulatedStirDegrees;
                    stirDirection = sampleDirection;
                }
            }

            progress = Mathf.Clamp01(accumulatedStirDegrees / primaryRequirement);
        }
    }

}
