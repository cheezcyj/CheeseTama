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

        private const string DetailProxyObjectName = "__CheeseTama Detail Proxy";

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
        private Func<bool> pointerCapturedCallback;
        private Collider[] interactionColliders = Array.Empty<Collider>();
        private MilkroomPropPointerRelay[] preciseHitTargets =
            Array.Empty<MilkroomPropPointerRelay>();
        private readonly HashSet<MilkroomPropPointerRelay> hoveredPreciseHitTargets =
            new HashSet<MilkroomPropPointerRelay>();
        private HighlightState[] highlightStates = Array.Empty<HighlightState>();
        private bool rootPointerHovered;
        private bool pointerHovered;
        private bool keyboardFocused;
        private bool highlightApplied;
        private bool pointerCapturedOnPress;

        public MilkroomPropRoute Route => route;
        public Collider InteractionCollider => interactionCollider;
        public bool IsConfigured => IsSupportedRoute(route)
            && interactionCollider != null
            && routeCallback != null;
        public bool IsPointerHovered => pointerHovered;
        public bool IsKeyboardFocused => keyboardFocused;
        public bool IsHighlighted => highlightApplied;
        public int HighlightRendererCount => highlightStates.Length;
        public int InteractionColliderCount => interactionColliders.Length;

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
            Renderer[] renderers = null,
            Func<bool> isPointerCaptured = null,
            bool usePreciseRendererHitTargets = false)
        {
            ClearFocusAndRestoreHighlight();
            var previousBroadCollider = preciseHitTargets.Length == 0
                ? interactionCollider
                : null;
            DeactivatePreciseHitTargets();

            route = IsSupportedRoute(interactionRoute)
                ? interactionRoute
                : MilkroomPropRoute.None;
            routeCallback = tryOpenRoute;
            blockerCallback = isInteractionBlocked;
            pointerCapturedCallback = isPointerCaptured;

            highlightRenderers = ResolveRenderers(renderers);
            RebuildHighlightStates();
            if (collider == null
                && usePreciseRendererHitTargets
                && TryConfigurePreciseHitTargets())
            {
                if (previousBroadCollider != null)
                {
                    previousBroadCollider.enabled = false;
                }

                return;
            }

            var resolvedCollider = collider != null ? collider : GetComponent<Collider>();
            if (resolvedCollider == null)
            {
                resolvedCollider = gameObject.AddComponent<BoxCollider>();
            }

            if (previousBroadCollider != null && previousBroadCollider != resolvedCollider)
            {
                previousBroadCollider.enabled = false;
            }

            interactionCollider = resolvedCollider;
            interactionColliders = new[] { resolvedCollider };
            if (!(resolvedCollider is MeshCollider meshCollider) || meshCollider.convex)
            {
                interactionCollider.isTrigger = true;
            }

            interactionCollider.enabled = true;
            if (collider == null && interactionCollider is BoxCollider boxCollider)
            {
                FitColliderToRenderers(boxCollider);
            }
        }

        public void Unconfigure()
        {
            ClearFocusAndRestoreHighlight();
            DisableInteractionColliders();
            route = MilkroomPropRoute.None;
            routeCallback = null;
            blockerCallback = null;
            pointerCapturedCallback = null;
            pointerCapturedOnPress = false;
            DeactivatePreciseHitTargets();
            interactionCollider = null;
        }

        public bool TryRaycast(Ray ray, out RaycastHit closestHit, float maxDistance = 1000f)
        {
            closestHit = default;
            var found = false;
            for (var index = 0; index < interactionColliders.Length; index += 1)
            {
                var candidate = interactionColliders[index];
                if (candidate == null
                    || !candidate.enabled
                    || !candidate.Raycast(ray, out var hit, maxDistance)
                    || (found && hit.distance >= closestHit.distance))
                {
                    continue;
                }

                closestHit = hit;
                found = true;
            }

            return found;
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
            if (IsPointerOverUi() || IsInteractionBlocked() || IsPointerCaptured())
            {
                return;
            }

            rootPointerHovered = true;
            RefreshPointerHover();
        }

        private void OnMouseExit()
        {
            rootPointerHovered = false;
            RefreshPointerHover();
        }

        private void OnMouseDown()
        {
            pointerCapturedOnPress = IsPointerCaptured();
        }

        private void OnMouseUpAsButton()
        {
            HandleMouseUpAsButton();
        }

        internal void NotifyPrecisePointerEnter(MilkroomPropPointerRelay source)
        {
            if (source == null
                || !isActiveAndEnabled
                || IsPointerOverUi()
                || IsInteractionBlocked()
                || IsPointerCaptured())
            {
                return;
            }

            hoveredPreciseHitTargets.Add(source);
            RefreshPointerHover();
        }

        internal void NotifyPrecisePointerExit(MilkroomPropPointerRelay source)
        {
            if (source != null)
            {
                hoveredPreciseHitTargets.Remove(source);
            }

            RefreshPointerHover();
        }

        internal void NotifyPrecisePointerDown()
        {
            OnMouseDown();
        }

        internal void NotifyPrecisePointerUpAsButton()
        {
            HandleMouseUpAsButton();
        }

        private void HandleMouseUpAsButton()
        {
            var characterOwnedGesture = pointerCapturedOnPress || IsPointerCaptured();
            pointerCapturedOnPress = false;
            if (!IsPointerOverUi() && !characterOwnedGesture)
            {
                TryActivate();
            }
        }

        private void OnDisable()
        {
            pointerCapturedOnPress = false;
            ClearFocusAndRestoreHighlight();
        }

        private void OnDestroy()
        {
            DisableInteractionColliders();
            DeactivatePreciseHitTargets();
            RestoreHighlight();
        }

        private bool IsInteractionBlocked()
        {
            return blockerCallback != null && blockerCallback();
        }

        private bool IsPointerCaptured()
        {
            return pointerCapturedCallback != null && pointerCapturedCallback();
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

        private bool TryConfigurePreciseHitTargets()
        {
            var colliders = new List<Collider>(highlightRenderers.Length);
            var relays = new List<MilkroomPropPointerRelay>(highlightRenderers.Length);
            var configuredObjects = new HashSet<GameObject>();
            for (var index = 0; index < highlightRenderers.Length; index += 1)
            {
                var targetRenderer = highlightRenderers[index];
                if (targetRenderer == null
                    || IsVisualOnlyDetailProxy(targetRenderer)
                    || !configuredObjects.Add(targetRenderer.gameObject))
                {
                    continue;
                }

                Mesh targetMesh = null;
                if (targetRenderer is SkinnedMeshRenderer skinnedRenderer)
                {
                    targetMesh = skinnedRenderer.sharedMesh;
                }
                else
                {
                    var meshFilter = targetRenderer.GetComponent<MeshFilter>();
                    targetMesh = meshFilter != null ? meshFilter.sharedMesh : null;
                }

                if (targetMesh == null)
                {
                    continue;
                }

                var relay = targetRenderer.GetComponent<MilkroomPropPointerRelay>();
                if (relay == null)
                {
                    relay = targetRenderer.gameObject.AddComponent<MilkroomPropPointerRelay>();
                }

                var preciseCollider = relay.Configure(this, targetMesh);
                if (preciseCollider == null)
                {
                    continue;
                }

                relays.Add(relay);
                colliders.Add(preciseCollider);
            }

            if (colliders.Count == 0)
            {
                return false;
            }

            preciseHitTargets = relays.ToArray();
            interactionColliders = colliders.ToArray();
            interactionCollider = interactionColliders[0];
            return true;
        }

        private void DeactivatePreciseHitTargets()
        {
            for (var index = 0; index < preciseHitTargets.Length; index += 1)
            {
                var target = preciseHitTargets[index];
                if (target != null)
                {
                    target.Deactivate(this);
                }
            }

            preciseHitTargets = Array.Empty<MilkroomPropPointerRelay>();
            hoveredPreciseHitTargets.Clear();
            rootPointerHovered = false;
            pointerHovered = false;
            interactionColliders = Array.Empty<Collider>();
        }

        private void DisableInteractionColliders()
        {
            for (var index = 0; index < interactionColliders.Length; index += 1)
            {
                if (interactionColliders[index] != null)
                {
                    interactionColliders[index].enabled = false;
                }
            }
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
            rootPointerHovered = false;
            hoveredPreciseHitTargets.Clear();
            pointerHovered = false;
            keyboardFocused = false;
            RestoreHighlight();
        }

        private void RefreshPointerHover()
        {
            pointerHovered = rootPointerHovered || hoveredPreciseHitTargets.Count > 0;
            RefreshHighlight();
        }

        private void FitColliderToRenderers(BoxCollider boxCollider)
        {
            var hasBounds = false;
            var localBounds = new Bounds();
            for (var index = 0; index < highlightRenderers.Length; index += 1)
            {
                var target = highlightRenderers[index];
                if (target == null || IsVisualOnlyDetailProxy(target))
                {
                    continue;
                }

                EncapsulateRendererLocalBounds(target, ref localBounds, ref hasBounds);
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

        private void EncapsulateRendererLocalBounds(
            Renderer target,
            ref Bounds localBounds,
            ref bool hasBounds)
        {
            var rendererBounds = target.localBounds;
            var center = rendererBounds.center;
            var extents = rendererBounds.extents;
            for (var x = -1; x <= 1; x += 2)
            {
                for (var y = -1; y <= 1; y += 2)
                {
                    for (var z = -1; z <= 1; z += 2)
                    {
                        var rendererLocalPoint = center
                            + Vector3.Scale(extents, new Vector3(x, y, z));
                        var worldPoint = target.transform.TransformPoint(rendererLocalPoint);
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

        private static bool IsVisualOnlyDetailProxy(Renderer renderer)
        {
            // LOD proxies still receive highlight feedback, but their coarse envelope
            // must not intercept pointer rays or expand an authored prop's hit bounds.
            for (var current = renderer.transform; current != null; current = current.parent)
            {
                if (current.name == DetailProxyObjectName)
                {
                    return true;
                }
            }

            return false;
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
