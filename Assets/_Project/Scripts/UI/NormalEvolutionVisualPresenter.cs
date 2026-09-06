using CheeseTama.Gameplay;
using CheeseTama.Gameplay.Growth;
using CheeseTama.Utilities;
using UnityEngine;

namespace CheeseTama.UI
{
    /// <summary>
    /// Adds lightweight, rounded accents to the active generated model without
    /// changing the imported mesh or its materials. The presenter observes the
    /// visual controller's replaceable ModelInstance and rebuilds only when the
    /// model reference or normal evolution id changes.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class NormalEvolutionVisualPresenter : MonoBehaviour
    {
        public const string GeneratedRootName = "Normal Evolution Visual Accents";

        private const float SignatureReactionDuration = 1.15f;
        private const float MinimumBoundsSize = 0.25f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField] private CheeseTamaVisualController visualController;

        private MaterialPropertyBlock propertyBlock;
        private CheeseTamaModel boundTama;
        private Transform explicitModelRoot;
        private Transform observedModelRoot;
        private Transform generatedRoot;
        private NormalEvolutionVisualProfile activeProfile;
        private Renderer[] accentRenderers = System.Array.Empty<Renderer>();
        private Color[] accentColors = System.Array.Empty<Color>();
        private string observedEvolutionId = string.Empty;
        private float reactionStartedAt = float.NegativeInfinity;
        private bool reactionActive;

        public NormalEvolutionVisualProfile ActiveProfile => activeProfile;
        public string ActiveEvolutionId => activeProfile?.EvolutionId ?? string.Empty;
        public Transform GeneratedRoot => generatedRoot;
        public int GeneratedAccentCount => generatedRoot != null ? generatedRoot.childCount : 0;

        public void Configure(CheeseTamaVisualController controller)
        {
            if (visualController == controller)
            {
                RefreshNow();
                return;
            }

            visualController = controller;
            explicitModelRoot = null;
            RefreshNow();
        }

        /// <summary>
        /// Runtime integration path. The current model is read from the configured
        /// CheeseTamaVisualController and replacement is detected in LateUpdate.
        /// </summary>
        public void Bind(CheeseTamaModel tama)
        {
            boundTama = tama;
            explicitModelRoot = null;
            RefreshNow();
        }

        /// <summary>
        /// Explicit model path for isolated presenters, previews, and EditMode tests.
        /// Rebinding to another Transform exercises the same replacement cleanup.
        /// </summary>
        public void Bind(CheeseTamaModel tama, Transform modelRoot)
        {
            boundTama = tama;
            explicitModelRoot = modelRoot;
            RefreshNow();
        }

        public bool RefreshNow(bool force = false)
        {
            var desiredModel = ResolveModelRoot();
            var desiredEvolutionId = boundTama?.evolutionId ?? string.Empty;
            var hasProfile = NormalEvolutionVisualCatalog.TryGet(
                desiredEvolutionId,
                out var desiredProfile);

            if (!hasProfile || desiredModel == null)
            {
                ClearGeneratedVisuals(clearBinding: false);
                observedModelRoot = desiredModel;
                observedEvolutionId = desiredEvolutionId;
                activeProfile = null;
                return false;
            }

            var unchanged = !force
                && observedModelRoot == desiredModel
                && string.Equals(observedEvolutionId, desiredEvolutionId, System.StringComparison.Ordinal)
                && generatedRoot != null
                && generatedRoot.parent == desiredModel;
            if (unchanged)
            {
                RefreshAccentColors();
                return true;
            }

            ClearGeneratedVisuals(clearBinding: false);
            RemoveStaleGeneratedRoot(desiredModel);

            observedModelRoot = desiredModel;
            observedEvolutionId = desiredEvolutionId;
            activeProfile = desiredProfile;
            generatedRoot = new GameObject(GeneratedRootName).transform;
            generatedRoot.SetParent(desiredModel, false);
            BuildAccents(desiredModel, desiredProfile);
            ResetReactionPose();
            return true;
        }

        public bool PlaySignatureReaction()
        {
            if (!RefreshNow() || generatedRoot == null || activeProfile == null)
            {
                return false;
            }

            reactionStartedAt = Time.unscaledTime;
            reactionActive = true;
            return true;
        }

        /// <summary>
        /// Removes every object owned by this presenter and clears its bound model.
        /// A later Bind call can safely reuse the component.
        /// </summary>
        public void Release()
        {
            ClearGeneratedVisuals(clearBinding: true);
        }

        private void OnEnable()
        {
            RefreshNow();
        }

        private void LateUpdate()
        {
            var desiredModel = ResolveModelRoot();
            var desiredEvolutionId = boundTama?.evolutionId ?? string.Empty;
            if (desiredModel != observedModelRoot
                || !string.Equals(
                    desiredEvolutionId,
                    observedEvolutionId,
                    System.StringComparison.Ordinal)
                || (activeProfile != null && generatedRoot == null))
            {
                RefreshNow(force: true);
            }

            UpdateSignatureReaction();
            RefreshAccentColors();
        }

        private void OnDisable()
        {
            ClearGeneratedVisuals(clearBinding: false);
        }

        private void OnDestroy()
        {
            ClearGeneratedVisuals(clearBinding: true);
        }

        private Transform ResolveModelRoot()
        {
            return explicitModelRoot != null
                ? explicitModelRoot
                : visualController != null
                    ? visualController.ModelInstance
                    : null;
        }

        private void BuildAccents(Transform modelRoot, NormalEvolutionVisualProfile profile)
        {
            if (generatedRoot == null || profile == null)
            {
                return;
            }

            var bounds = ResolveModelBounds(modelRoot);
            var viewerDirection = ResolveViewerDirection(bounds.center);
            var right = transform.right.normalized;
            var up = transform.up.normalized;
            var horizontalRadius = ProjectedRadius(bounds.extents, right);
            var verticalRadius = ProjectedRadius(bounds.extents, up);
            var frontRadius = ProjectedRadius(bounds.extents, viewerDirection);
            var baseSize = Mathf.Max(
                MinimumBoundsSize,
                Mathf.Min(bounds.size.x, bounds.size.y));
            accentRenderers = new Renderer[profile.Accents.Count];
            accentColors = new Color[profile.Accents.Count];

            for (var index = 0; index < profile.Accents.Count; index += 1)
            {
                var definition = profile.Accents[index];
                if (definition == null
                    || !EvolutionVisualAccentDefinition.IsSoftPrimitive(definition.Primitive))
                {
                    continue;
                }

                var accent = GameObject.CreatePrimitive(definition.Primitive);
                accent.name = definition.Name;
                accent.transform.SetParent(generatedRoot, true);

                var collider = accent.GetComponent<Collider>();
                if (collider != null)
                {
                    collider.enabled = false;
                    DestroyOwnedObject(collider);
                }

                var normalized = definition.NormalizedPosition;
                accent.transform.position = bounds.center
                    + right * (normalized.x * horizontalRadius)
                    + up * (normalized.y * verticalRadius)
                    + viewerDirection * (frontRadius + Mathf.Max(0f, normalized.z) * baseSize);
                accent.transform.rotation = Quaternion.LookRotation(viewerDirection, up)
                    * Quaternion.Euler(definition.EulerAngles);
                SetWorldScale(
                    accent.transform,
                    Vector3.Scale(definition.NormalizedScale, Vector3.one * baseSize));

                var renderer = accent.GetComponent<Renderer>();
                var color = profile.ResolveColor(definition.ColorRole);
                accentRenderers[index] = renderer;
                accentColors[index] = color;
                ApplyAccentColor(renderer, color, configureMaterial: true);
            }
        }

        private Bounds ResolveModelBounds(Transform modelRoot)
        {
            var renderers = modelRoot != null
                ? modelRoot.GetComponentsInChildren<Renderer>(true)
                : System.Array.Empty<Renderer>();
            var hasBounds = false;
            var bounds = new Bounds(modelRoot != null ? modelRoot.position : transform.position, Vector3.one);
            for (var index = 0; index < renderers.Length; index += 1)
            {
                var renderer = renderers[index];
                if (renderer == null || IsGeneratedAccent(renderer.transform))
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            if (!hasBounds || bounds.size.sqrMagnitude < 0.0001f)
            {
                bounds = new Bounds(
                    modelRoot != null ? modelRoot.position : transform.position,
                    new Vector3(1.2f, 1.5f, 0.8f));
            }

            return bounds;
        }

        private bool IsGeneratedAccent(Transform candidate)
        {
            var cursor = candidate;
            while (cursor != null)
            {
                if (cursor == generatedRoot || cursor.name == GeneratedRootName)
                {
                    return true;
                }

                cursor = cursor.parent;
            }

            return false;
        }

        private Vector3 ResolveViewerDirection(Vector3 modelCenter)
        {
            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                var direction = mainCamera.transform.position - modelCenter;
                if (direction.sqrMagnitude > 0.0001f)
                {
                    return direction.normalized;
                }
            }

            var fallback = -transform.forward;
            return fallback.sqrMagnitude > 0.0001f ? fallback.normalized : Vector3.back;
        }

        private void ApplyAccentColor(
            Renderer renderer,
            Color color,
            bool configureMaterial)
        {
            if (renderer == null)
            {
                return;
            }

            if (configureMaterial && Application.isPlaying)
            {
                ToonMaterialUtility.Apply(renderer, ToonMaterialProfile.CharacterMark, color);
            }

            propertyBlock ??= new MaterialPropertyBlock();
            renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BaseColorId, color);
            propertyBlock.SetColor(ColorId, color);
            renderer.SetPropertyBlock(propertyBlock);
        }

        private void RefreshAccentColors()
        {
            // CheeseTamaVisualController intentionally recolors every renderer
            // under ModelInstance for condition flashes. This presenter runs later
            // and restores only its owned accents while preserving the other block
            // values read by ApplyAccentColor.
            var count = Mathf.Min(accentRenderers.Length, accentColors.Length);
            for (var index = 0; index < count; index += 1)
            {
                ApplyAccentColor(
                    accentRenderers[index],
                    accentColors[index],
                    configureMaterial: false);
            }
        }

        private void UpdateSignatureReaction()
        {
            if (!reactionActive || generatedRoot == null || activeProfile == null)
            {
                return;
            }

            var normalized = (Time.unscaledTime - reactionStartedAt) / SignatureReactionDuration;
            if (normalized >= 1f)
            {
                reactionActive = false;
                ResetReactionPose();
                return;
            }

            var pose = NormalEvolutionReactionMotion.Evaluate(
                activeProfile.ReactionStyle,
                normalized);
            generatedRoot.localPosition = pose.LocalPosition;
            generatedRoot.localRotation = Quaternion.Euler(pose.LocalEulerAngles);
            generatedRoot.localScale = pose.LocalScale;
        }

        private void ResetReactionPose()
        {
            reactionActive = false;
            if (generatedRoot == null)
            {
                return;
            }

            generatedRoot.localPosition = Vector3.zero;
            generatedRoot.localRotation = Quaternion.identity;
            generatedRoot.localScale = Vector3.one;
        }

        private void ClearGeneratedVisuals(bool clearBinding)
        {
            reactionActive = false;
            if (generatedRoot != null)
            {
                DetachAndDestroy(generatedRoot);
            }

            generatedRoot = null;
            activeProfile = null;
            accentRenderers = System.Array.Empty<Renderer>();
            accentColors = System.Array.Empty<Color>();
            observedModelRoot = null;
            observedEvolutionId = string.Empty;
            if (!clearBinding)
            {
                return;
            }

            boundTama = null;
            explicitModelRoot = null;
        }

        private static void RemoveStaleGeneratedRoot(Transform modelRoot)
        {
            if (modelRoot == null)
            {
                return;
            }

            var stale = modelRoot.Find(GeneratedRootName);
            if (stale != null)
            {
                DetachAndDestroy(stale);
            }
        }

        private static void DetachAndDestroy(Transform ownedRoot)
        {
            if (ownedRoot == null)
            {
                return;
            }

            // Destroy is deferred during Play Mode. Detaching first prevents a
            // stale root from being rediscovered or counted in bounds while an
            // immediate refresh builds the replacement in the same frame.
            ownedRoot.SetParent(null, true);
            ownedRoot.name = GeneratedRootName + " (Released)";
            DestroyOwnedObject(ownedRoot.gameObject);
        }

        private static float ProjectedRadius(Vector3 extents, Vector3 direction)
        {
            direction = new Vector3(
                Mathf.Abs(direction.x),
                Mathf.Abs(direction.y),
                Mathf.Abs(direction.z));
            return Vector3.Dot(extents, direction);
        }

        private static void SetWorldScale(Transform target, Vector3 worldScale)
        {
            if (target == null)
            {
                return;
            }

            var parentScale = target.parent != null ? target.parent.lossyScale : Vector3.one;
            target.localScale = new Vector3(
                worldScale.x / Mathf.Max(0.0001f, Mathf.Abs(parentScale.x)),
                worldScale.y / Mathf.Max(0.0001f, Mathf.Abs(parentScale.y)),
                worldScale.z / Mathf.Max(0.0001f, Mathf.Abs(parentScale.z)));
        }

        private static void DestroyOwnedObject(Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }
    }
}
