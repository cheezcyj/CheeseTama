using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CheeseTama.Environment
{
    public enum MilkroomPropRoute
    {
        None = 0,
        SnackPanel = 1,
        MilkPanel = 2,
        CookingChoice = 3,
        SleepSchedule = 4
    }

    [DisallowMultipleComponent]
    public sealed class MilkroomPropInteraction : MonoBehaviour
    {
        public const float DefaultColliderPadding = 0.12f;
        public const float DefaultHoverBlend = 0.3f;

        private static readonly int GltfBaseColorId = Shader.PropertyToID("baseColorFactor");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField] private MilkroomPropRoute route;
        [SerializeField] private Collider interactionCollider;
        [SerializeField] private Renderer[] highlightRenderers = Array.Empty<Renderer>();
        [SerializeField] private Color hoverTint = new Color(1f, 0.78f, 0.24f, 1f);
        [SerializeField, Range(0f, 1f)] private float hoverBlend = DefaultHoverBlend;

        private Func<MilkroomPropRoute, bool> routeCallback;
        private Func<bool> blockerCallback;
        private HighlightState[] highlightStates = Array.Empty<HighlightState>();
        private bool pointerHovered;
        private bool keyboardFocused;
        private bool highlightApplied;

        public MilkroomPropRoute Route => route;
        public Collider InteractionCollider => interactionCollider;
        public bool IsConfigured => IsSupportedRoute(route)
            && interactionCollider != null
            && routeCallback != null;
        public bool IsPointerHovered => pointerHovered;
        public bool IsKeyboardFocused => keyboardFocused;
        public bool IsHighlighted => highlightApplied;
        public int HighlightRendererCount => highlightStates.Length;

        public static bool IsSupportedRoute(MilkroomPropRoute candidate)
        {
            return candidate == MilkroomPropRoute.SnackPanel
                || candidate == MilkroomPropRoute.MilkPanel
                || candidate == MilkroomPropRoute.CookingChoice
                || candidate == MilkroomPropRoute.SleepSchedule;
        }

        public void Configure(
            MilkroomPropRoute interactionRoute,
            Func<MilkroomPropRoute, bool> tryOpenRoute,
            Func<bool> isInteractionBlocked,
            Collider collider = null,
            Renderer[] renderers = null)
        {
            ClearFocusAndRestoreHighlight();

            route = IsSupportedRoute(interactionRoute)
                ? interactionRoute
                : MilkroomPropRoute.None;
            routeCallback = tryOpenRoute;
            blockerCallback = isInteractionBlocked;

            var resolvedCollider = collider != null ? collider : GetComponent<Collider>();
            if (resolvedCollider == null)
            {
                resolvedCollider = gameObject.AddComponent<BoxCollider>();
            }

            interactionCollider = resolvedCollider;
            interactionCollider.isTrigger = true;

            highlightRenderers = ResolveRenderers(renderers);
            RebuildHighlightStates();
            if (collider == null && interactionCollider is BoxCollider boxCollider)
            {
                FitColliderToRenderers(boxCollider);
            }
        }

        public void Unconfigure()
        {
            ClearFocusAndRestoreHighlight();
            route = MilkroomPropRoute.None;
            routeCallback = null;
            blockerCallback = null;
        }

        public bool TryActivate()
        {
            if (!isActiveAndEnabled
                || !IsConfigured
                || IsInteractionBlocked())
            {
                return false;
            }

            var activated = routeCallback(route);
            if (activated)
            {
                ClearFocusAndRestoreHighlight();
            }

            return activated;
        }

        public void SetKeyboardFocus(bool focused)
        {
            keyboardFocused = focused && IsConfigured && !IsInteractionBlocked();
            RefreshHighlight();
        }

        public void RefreshBlockingState()
        {
            if (IsInteractionBlocked())
            {
                ClearFocusAndRestoreHighlight();
                return;
            }

            RefreshHighlight();
        }

        private void OnMouseEnter()
        {
            if (IsPointerOverUi() || IsInteractionBlocked())
            {
                return;
            }

            pointerHovered = true;
            RefreshHighlight();
        }

        private void OnMouseExit()
        {
            pointerHovered = false;
            RefreshHighlight();
        }

        private void OnMouseUpAsButton()
        {
            if (!IsPointerOverUi())
            {
                TryActivate();
            }
        }

        private void OnDisable()
        {
            ClearFocusAndRestoreHighlight();
        }

        private void OnDestroy()
        {
            RestoreHighlight();
        }

        private bool IsInteractionBlocked()
        {
            return blockerCallback != null && blockerCallback();
        }

        private static bool IsPointerOverUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        private Renderer[] ResolveRenderers(Renderer[] configuredRenderers)
        {
            var candidates = configuredRenderers ?? GetComponentsInChildren<Renderer>(true);
            if (candidates == null || candidates.Length == 0)
            {
                return Array.Empty<Renderer>();
            }

            var unique = new List<Renderer>(candidates.Length);
            var seen = new HashSet<Renderer>();
            for (var index = 0; index < candidates.Length; index += 1)
            {
                var candidate = candidates[index];
                if (candidate != null && seen.Add(candidate))
                {
                    unique.Add(candidate);
                }
            }

            return unique.ToArray();
        }

        private void RebuildHighlightStates()
        {
            highlightStates = new HighlightState[highlightRenderers.Length];
            for (var index = 0; index < highlightRenderers.Length; index += 1)
            {
                highlightStates[index] = new HighlightState(highlightRenderers[index]);
            }
        }

        private void RefreshHighlight()
        {
            var shouldHighlight = IsConfigured
                && (pointerHovered || keyboardFocused)
                && !IsInteractionBlocked();
            if (shouldHighlight == highlightApplied)
            {
                return;
            }

            if (shouldHighlight)
            {
                ApplyHighlight();
            }
            else
            {
                RestoreHighlight();
            }
        }

        private void ApplyHighlight()
        {
            for (var index = 0; index < highlightStates.Length; index += 1)
            {
                highlightStates[index].Apply(hoverTint, hoverBlend);
            }

            highlightApplied = true;
        }

        private void RestoreHighlight()
        {
            if (!highlightApplied)
            {
                return;
            }

            for (var index = 0; index < highlightStates.Length; index += 1)
            {
                highlightStates[index].Restore();
            }

            highlightApplied = false;
        }

        private void ClearFocusAndRestoreHighlight()
        {
            pointerHovered = false;
            keyboardFocused = false;
            RestoreHighlight();
        }

        private void FitColliderToRenderers(BoxCollider boxCollider)
        {
            var hasBounds = false;
            var localBounds = new Bounds();
            for (var index = 0; index < highlightRenderers.Length; index += 1)
            {
                var target = highlightRenderers[index];
                if (target == null)
                {
                    continue;
                }

                EncapsulateWorldBounds(target.bounds, ref localBounds, ref hasBounds);
            }

            if (!hasBounds)
            {
                return;
            }

            localBounds.Expand(DefaultColliderPadding);
            boxCollider.center = localBounds.center;
            boxCollider.size = new Vector3(
                Mathf.Max(0.1f, localBounds.size.x),
                Mathf.Max(0.1f, localBounds.size.y),
                Mathf.Max(0.1f, localBounds.size.z));
        }

        private void EncapsulateWorldBounds(
            Bounds worldBounds,
            ref Bounds localBounds,
            ref bool hasBounds)
        {
            var center = worldBounds.center;
            var extents = worldBounds.extents;
            for (var x = -1; x <= 1; x += 2)
            {
                for (var y = -1; y <= 1; y += 2)
                {
                    for (var z = -1; z <= 1; z += 2)
                    {
                        var worldPoint = center + Vector3.Scale(extents, new Vector3(x, y, z));
                        var localPoint = transform.InverseTransformPoint(worldPoint);
                        if (!hasBounds)
                        {
                            localBounds = new Bounds(localPoint, Vector3.zero);
                            hasBounds = true;
                        }
                        else
                        {
                            localBounds.Encapsulate(localPoint);
                        }
                    }
                }
            }
        }

        private sealed class HighlightState
        {
            private readonly Renderer target;
            private readonly MaterialPropertyBlock originalBlock = new MaterialPropertyBlock();
            private readonly MaterialPropertyBlock workingBlock = new MaterialPropertyBlock();
            private bool originalWasEmpty;
            private bool captured;

            public HighlightState(Renderer renderer)
            {
                target = renderer;
            }

            public void Apply(Color tint, float blend)
            {
                if (target == null)
                {
                    return;
                }

                var material = target.sharedMaterial;
                var propertyId = ResolveColorProperty(material);
                if (material == null || propertyId < 0)
                {
                    return;
                }

                originalBlock.Clear();
                target.GetPropertyBlock(originalBlock);
                originalWasEmpty = originalBlock.isEmpty;
                captured = true;

                workingBlock.Clear();
                target.GetPropertyBlock(workingBlock);
                var baseColor = material.GetColor(propertyId);
                if (!originalWasEmpty)
                {
                    var overriddenColor = originalBlock.GetColor(propertyId);
                    if (overriddenColor != default)
                    {
                        baseColor = overriddenColor;
                    }
                }

                var highlightedColor = Color.Lerp(baseColor, tint, Mathf.Clamp01(blend));
                highlightedColor.a = baseColor.a;
                workingBlock.SetColor(propertyId, highlightedColor);
                target.SetPropertyBlock(workingBlock);
            }

            public void Restore()
            {
                if (!captured || target == null)
                {
                    return;
                }

                target.SetPropertyBlock(originalWasEmpty ? null : originalBlock);
                captured = false;
            }

            private static int ResolveColorProperty(Material material)
            {
                if (material == null)
                {
                    return -1;
                }

                if (material.HasProperty(GltfBaseColorId))
                {
                    return GltfBaseColorId;
                }

                if (material.HasProperty(BaseColorId))
                {
                    return BaseColorId;
                }

                return material.HasProperty(ColorId) ? ColorId : -1;
            }
        }
    }
}
