using UnityEngine;

namespace CheeseTama.Environment
{
    /// <summary>
    /// Deterministic camera-navigation state. Raw pointer input stays in the runtime adapter;
    /// this model only owns field-of-view and focus-offset rules so every aspect ratio uses the
    /// same clamping contract.
    /// </summary>
    public sealed class MilkroomViewportNavigationModel
    {
        public const float DefaultMinimumFieldOfView = 22f;
        public const float MinimumSupportedFieldOfView = 10f;
        public const float MaximumSupportedFieldOfView = 90f;

        private const float MinimumDistance = 0.01f;
        private const float MinimumAspect = 0.1f;
        private const float ChangeTolerance = 0.0001f;

        private readonly float minimumFieldOfView;
        private readonly float fitFieldOfView;
        private float fieldOfView;
        private Vector2 focusOffset;

        public MilkroomViewportNavigationModel(
            float fitFieldOfView,
            float minimumFieldOfView = DefaultMinimumFieldOfView)
        {
            this.fitFieldOfView = Mathf.Clamp(
                fitFieldOfView,
                MinimumSupportedFieldOfView,
                MaximumSupportedFieldOfView);
            this.minimumFieldOfView = Mathf.Clamp(
                minimumFieldOfView,
                MinimumSupportedFieldOfView,
                this.fitFieldOfView);
            fieldOfView = this.fitFieldOfView;
            focusOffset = Vector2.zero;
        }

        public float FieldOfView => fieldOfView;
        public float FitFieldOfView => fitFieldOfView;
        public float MinimumFieldOfView => minimumFieldOfView;
        public Vector2 FocusOffset => focusOffset;
        public bool IsAtFit => Mathf.Abs(fieldOfView - fitFieldOfView) <= ChangeTolerance
            && focusOffset.sqrMagnitude <= ChangeTolerance * ChangeTolerance;

        /// <summary>
        /// Applies a field-of-view delta. Positive values zoom in. The screen anchor uses the
        /// conventional viewport range (-1,-1 bottom-left to 1,1 top-right), preserving the
        /// world point below that anchor until a safety clamp is reached.
        /// </summary>
        public bool ZoomBy(
            float zoomInFieldOfViewDelta,
            Vector2 viewportAnchor,
            float aspect,
            float contentPlaneDistance,
            float safeBottomFraction)
        {
            var nextFieldOfView = Mathf.Clamp(
                fieldOfView - zoomInFieldOfViewDelta,
                minimumFieldOfView,
                fitFieldOfView);
            if (Mathf.Abs(nextFieldOfView - fieldOfView) <= ChangeTolerance)
            {
                return false;
            }

            var oldHalfExtents = CalculateHalfExtents(
                fieldOfView,
                aspect,
                contentPlaneDistance);
            var nextHalfExtents = CalculateHalfExtents(
                nextFieldOfView,
                aspect,
                contentPlaneDistance);
            var anchor = new Vector2(
                Mathf.Clamp(viewportAnchor.x, -1f, 1f),
                Mathf.Clamp(viewportAnchor.y, -1f, 1f));

            focusOffset += Vector2.Scale(oldHalfExtents - nextHalfExtents, anchor);
            fieldOfView = nextFieldOfView;
            focusOffset = ClampFocusOffset(
                focusOffset,
                fitFieldOfView,
                fieldOfView,
                aspect,
                contentPlaneDistance,
                safeBottomFraction);
            return true;
        }

        /// <summary>
        /// Pans using a normalized screen delta. Content follows the pointer, therefore the
        /// camera focus moves in the opposite direction. Upward camera travel is intentionally
        /// clamped out because it could push the character behind the persistent bottom bar.
        /// </summary>
        public bool PanByViewportDelta(
            Vector2 normalizedScreenDelta,
            float aspect,
            float contentPlaneDistance,
            float safeBottomFraction)
        {
            if (IsAtFit || normalizedScreenDelta.sqrMagnitude <= ChangeTolerance * ChangeTolerance)
            {
                return false;
            }

            var halfExtents = CalculateHalfExtents(
                fieldOfView,
                aspect,
                contentPlaneDistance);
            var requested = focusOffset - Vector2.Scale(normalizedScreenDelta, halfExtents * 2f);
            var clamped = ClampFocusOffset(
                requested,
                fitFieldOfView,
                fieldOfView,
                aspect,
                contentPlaneDistance,
                safeBottomFraction);
            if ((clamped - focusOffset).sqrMagnitude <= ChangeTolerance * ChangeTolerance)
            {
                return false;
            }

            focusOffset = clamped;
            return true;
        }

        public bool Reframe(
            float aspect,
            float contentPlaneDistance,
            float safeBottomFraction)
        {
            var clamped = ClampFocusOffset(
                focusOffset,
                fitFieldOfView,
                fieldOfView,
                aspect,
                contentPlaneDistance,
                safeBottomFraction);
            if ((clamped - focusOffset).sqrMagnitude <= ChangeTolerance * ChangeTolerance)
            {
                return false;
            }

            focusOffset = clamped;
            return true;
        }

        public bool Fit()
        {
            if (IsAtFit)
            {
                return false;
            }

            fieldOfView = fitFieldOfView;
            focusOffset = Vector2.zero;
            return true;
        }

        public static Vector2 CalculateHalfExtents(
            float fieldOfView,
            float aspect,
            float contentPlaneDistance)
        {
            var resolvedFieldOfView = Mathf.Clamp(
                fieldOfView,
                MinimumSupportedFieldOfView,
                MaximumSupportedFieldOfView);
            var resolvedAspect = Mathf.Max(MinimumAspect, aspect);
            var resolvedDistance = Mathf.Max(MinimumDistance, Mathf.Abs(contentPlaneDistance));
            var halfHeight = resolvedDistance
                * Mathf.Tan(resolvedFieldOfView * 0.5f * Mathf.Deg2Rad);
            return new Vector2(halfHeight * resolvedAspect, halfHeight);
        }

        public static Vector2 CalculatePanExtents(
            float fitFieldOfView,
            float currentFieldOfView,
            float aspect,
            float contentPlaneDistance,
            float safeBottomFraction)
        {
            var fit = CalculateHalfExtents(fitFieldOfView, aspect, contentPlaneDistance);
            var current = CalculateHalfExtents(
                Mathf.Min(currentFieldOfView, fitFieldOfView),
                aspect,
                contentPlaneDistance);
            var usableHeightFraction = 1f - Mathf.Clamp(safeBottomFraction, 0f, 0.9f);
            return new Vector2(
                Mathf.Max(0f, fit.x - current.x),
                Mathf.Max(0f, (fit.y - current.y) * usableHeightFraction));
        }

        public static Vector2 ClampFocusOffset(
            Vector2 requested,
            float fitFieldOfView,
            float currentFieldOfView,
            float aspect,
            float contentPlaneDistance,
            float safeBottomFraction)
        {
            var panExtents = CalculatePanExtents(
                fitFieldOfView,
                currentFieldOfView,
                aspect,
                contentPlaneDistance,
                safeBottomFraction);
            return new Vector2(
                Mathf.Clamp(requested.x, -panExtents.x, panExtents.x),
                // Positive camera Y would lower the character toward the persistent UI bars.
                Mathf.Clamp(requested.y, -panExtents.y, 0f));
        }
    }

    /// <summary>
    /// Pure policy shared by runtime guards and EditMode tests. A gesture may only start in the
    /// blank milkroom viewport while all authoritative UI/input systems are idle.
    /// </summary>
    public static class MilkroomViewportInputPolicy
    {
        public static bool CanBegin(
            bool navigationEnabled,
            bool pointerInsideViewport,
            bool pointerOverInteractiveUi,
            bool directManipulationActive,
            bool modalScopeActive,
            bool gameplayInputSuppressed)
        {
            return navigationEnabled
                && pointerInsideViewport
                && !pointerOverInteractiveUi
                && !directManipulationActive
                && !modalScopeActive
                && !gameplayInputSuppressed;
        }
    }
}
