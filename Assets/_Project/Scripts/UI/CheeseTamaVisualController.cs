using CheeseTama.Gameplay;
using UnityEngine;
using UnityEngine.Rendering;

namespace CheeseTama.UI
{
    public sealed class CheeseTamaVisualController : MonoBehaviour
    {
        [SerializeField] private Renderer targetRenderer;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private const float CareReactionDuration = 0.68f;
        private const float CareReactionHopHeight = 0.14f;
        private const float CareReactionPunch = 0.055f;
        private const float CareReactionFlash = 0.1f;
        private const float HatchReactionDuration = 1.15f;
        private const float HatchReactionHopHeight = 0.38f;
        private const float HatchReactionPunch = 0.16f;
        private const float HatchReactionFlash = 0.32f;
        private const float EventReactionDuration = 0.92f;
        private const float EventReactionHopHeight = 0.18f;
        private const float EventReactionPunch = 0.08f;
        private const float EventReactionFlash = 0.4f;
        private const float EventCueDuration = 1.2f;

        private readonly Vector3 eggScale = new Vector3(1.18f, 1.42f, 1.04f);
        private readonly Vector3 hatchedScale = new Vector3(1.32f, 1.08f, 1.06f);

        private MaterialPropertyBlock propertyBlock;
        private Transform modelRoot;
        private Transform visualRoot;
        private Transform body;
        private Transform bellyPatch;
        private Transform creamCap;
        private Transform leftStub;
        private Transform rightStub;
        private Transform highlight;
        private Transform softShadow;
        private Transform vfxRoot;
        private Transform faceRoot;
        private Transform leftEye;
        private Transform rightEye;
        private Transform mouth;
        private Transform leftCheek;
        private Transform rightCheek;
        private Transform cheeseHoleA;
        private Transform cheeseHoleB;
        private Transform cheeseHoleC;
        private Transform eventCue;
        private Renderer bodyRenderer;
        private Renderer bellyPatchRenderer;
        private Renderer creamCapRenderer;
        private Renderer leftStubRenderer;
        private Renderer rightStubRenderer;
        private Renderer highlightRenderer;
        private Renderer softShadowRenderer;
        private Renderer eventCueRenderer;
        private CheeseTamaModel current;
        private Vector3 restingLocalPosition;
        private Vector3 eventCueBaseScale;
        private bool hasRestingLocalPosition;
        private float reactionStartedAt;
        private float reactionDuration = CareReactionDuration;
        private float reactionHopHeight = CareReactionHopHeight;
        private float reactionPunch = CareReactionPunch;
        private float reactionFlash = CareReactionFlash;
        private float eventCueStartedAt;
        private float idleSeed;
        private Color reactionTintColor = Color.white;
        private bool isReacting;
        private bool eventCueVisible;

        private void Awake()
        {
            idleSeed = Random.Range(0f, 100f);
            EnsurePropertyBlock();
            EnsureVisualRig();
            EnsureHatchedFeatures();
            CaptureRestingPosition();
        }

        private void LateUpdate()
        {
            if (current == null)
            {
                return;
            }

            EnsureVisualRig();
            var baseScale = current.isHatched ? hatchedScale : eggScale;
            var baseColor = GetStateColor(current);
            UpdateRigColors(baseColor);
            UpdateHatchedFeatureVisibility();
            UpdateHatchedFeatureAppearance();
            UpdateEventCue();

            if (!isReacting)
            {
                var time = Time.realtimeSinceStartup + idleSeed;
                var breath = Mathf.Sin(time * 1.75f) * 0.016f;
                var sway = current.isHatched ? Mathf.Sin(time * 0.85f) * 1.4f : Mathf.Sin(time * 0.65f) * 0.7f;
                var targetScale = new Vector3(
                    baseScale.x * (1f + breath),
                    baseScale.y * (1f - breath * 0.45f),
                    baseScale.z * (1f + breath * 0.65f));

                transform.localPosition = Vector3.Lerp(
                    transform.localPosition,
                    restingLocalPosition + Vector3.up * Mathf.Abs(breath) * 0.08f,
                    Time.unscaledDeltaTime * 8f);
                transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * 8f);

                if (visualRoot != null)
                {
                    visualRoot.localRotation = Quaternion.Lerp(
                        visualRoot.localRotation,
                        Quaternion.Euler(0f, 0f, sway),
                        Time.unscaledDeltaTime * 5f);
                }

                return;
            }

            var normalized = Mathf.Clamp01((Time.realtimeSinceStartup - reactionStartedAt) / reactionDuration);
            var arc = Mathf.Sin(normalized * Mathf.PI);
            var settle = 1f - Mathf.SmoothStep(0f, 1f, normalized);
            var hop = arc * reactionHopHeight;
            var side = Mathf.Sin(normalized * Mathf.PI * 2f) * 0.025f * settle;
            var punch = arc * reactionPunch;
            var wobble = Mathf.Sin(normalized * Mathf.PI * 4f) * 0.018f * settle;

            transform.localPosition = restingLocalPosition + Vector3.up * hop + Vector3.right * side;
            transform.localScale = new Vector3(
                baseScale.x * (1f + punch + wobble),
                baseScale.y * (1f - punch * 0.35f - wobble * 0.6f),
                baseScale.z * (1f + punch * 0.7f + wobble));

            if (visualRoot != null)
            {
                visualRoot.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(normalized * Mathf.PI * 2f) * 2.2f * settle);
            }

            UpdateRigColors(Color.Lerp(baseColor, reactionTintColor, arc * reactionFlash));

            if (normalized >= 1f)
            {
                isReacting = false;
                reactionTintColor = Color.white;
                transform.localPosition = restingLocalPosition;
                transform.localScale = baseScale;
                UpdateRigColors(baseColor);
            }
        }

        public void Bind(CheeseTamaModel tama)
        {
            EnsureVisualRig();
            EnsureHatchedFeatures();
            CaptureRestingPosition();
            current = tama;
            if (current == null)
            {
                return;
            }

            if (!isReacting)
            {
                transform.localScale = current.isHatched ? hatchedScale : eggScale;
                transform.localPosition = restingLocalPosition;
            }

            UpdateRigColors(GetStateColor(current));
            UpdateHatchedFeatureVisibility();
            UpdateHatchedFeatureAppearance();
        }

        public void React(bool celebrate = false)
        {
            CaptureRestingPosition();
            HideEventCue();
            reactionStartedAt = Time.realtimeSinceStartup;
            reactionDuration = celebrate ? HatchReactionDuration : CareReactionDuration;
            reactionHopHeight = celebrate ? HatchReactionHopHeight : CareReactionHopHeight;
            reactionPunch = celebrate ? HatchReactionPunch : CareReactionPunch;
            reactionFlash = celebrate ? HatchReactionFlash : CareReactionFlash;
            reactionTintColor = Color.white;
            isReacting = true;

            var baseScale = current != null && current.isHatched ? hatchedScale : eggScale;
            transform.localPosition = restingLocalPosition;
            var squash = celebrate ? 0.9f : 0.965f;
            var stretch = celebrate ? 1.12f : 1.035f;
            transform.localScale = new Vector3(baseScale.x * stretch, baseScale.y * squash, baseScale.z * stretch);
            UpdateRigColors(Color.Lerp(GetStateColor(current), Color.white, celebrate ? 0.2f : 0.08f));
        }

        public void ReactEvent(string eventId)
        {
            CaptureRestingPosition();
            EnsureEventCue();

            reactionTintColor = GetEventColor(eventId);
            reactionStartedAt = Time.realtimeSinceStartup;
            reactionDuration = EventReactionDuration;
            reactionHopHeight = EventReactionHopHeight;
            reactionPunch = EventReactionPunch;
            reactionFlash = EventReactionFlash;
            isReacting = true;

            var baseScale = current != null && current.isHatched ? hatchedScale : eggScale;
            transform.localPosition = restingLocalPosition;
            transform.localScale = new Vector3(baseScale.x * 1.04f, baseScale.y * 0.96f, baseScale.z * 1.03f);
            UpdateRigColors(Color.Lerp(GetStateColor(current), reactionTintColor, 0.2f));
            ShowEventCue(eventId, reactionTintColor);
        }

        private void EnsureVisualRig()
        {
            DisableSpriteRendererIfPresent();
            DisableLegacyRootRenderer();

            modelRoot = GetOrCreateChild(transform, "ModelRoot");
            visualRoot = GetOrCreateChild(modelRoot, "VisualRoot");
            body = GetOrCreatePrimitiveChild(visualRoot, "Body", PrimitiveType.Sphere);
            bellyPatch = GetOrCreatePrimitiveChild(visualRoot, "Milk Belly Patch", PrimitiveType.Sphere);
            creamCap = GetOrCreatePrimitiveChild(visualRoot, "Cream Cap", PrimitiveType.Sphere);
            leftStub = GetOrCreatePrimitiveChild(visualRoot, "Left Jelly Stub", PrimitiveType.Sphere);
            rightStub = GetOrCreatePrimitiveChild(visualRoot, "Right Jelly Stub", PrimitiveType.Sphere);
            highlight = GetOrCreatePrimitiveChild(visualRoot, "Highlight", PrimitiveType.Sphere);
            softShadow = GetOrCreatePrimitiveChild(visualRoot, "SoftShadow", PrimitiveType.Sphere);
            vfxRoot = GetOrCreateChild(visualRoot, "VFXRoot");

            bodyRenderer = body.GetComponent<Renderer>();
            bellyPatchRenderer = bellyPatch.GetComponent<Renderer>();
            creamCapRenderer = creamCap.GetComponent<Renderer>();
            leftStubRenderer = leftStub.GetComponent<Renderer>();
            rightStubRenderer = rightStub.GetComponent<Renderer>();
            highlightRenderer = highlight.GetComponent<Renderer>();
            softShadowRenderer = softShadow.GetComponent<Renderer>();
            targetRenderer = bodyRenderer;

            ConfigureOverlayRenderer(bellyPatchRenderer);
            ConfigureOverlayRenderer(creamCapRenderer);
            ConfigureOverlayRenderer(leftStubRenderer);
            ConfigureOverlayRenderer(rightStubRenderer);
            ConfigureOverlayRenderer(highlightRenderer);
            ConfigureOverlayRenderer(softShadowRenderer);
            if (bodyRenderer != null)
            {
                bodyRenderer.shadowCastingMode = ShadowCastingMode.On;
                bodyRenderer.receiveShadows = true;
            }

            UpdateVisualShape(current != null && current.isHatched);
        }

        private void EnsureHatchedFeatures()
        {
            if (visualRoot == null)
            {
                EnsureVisualRig();
            }

            if (faceRoot == null)
            {
                var oldRoot = transform.Find("Hatched Face Root");
                if (oldRoot != null)
                {
                    oldRoot.name = "FaceAnchor";
                    oldRoot.SetParent(visualRoot, false);
                    faceRoot = oldRoot;
                }
                else
                {
                    faceRoot = GetOrCreateChild(visualRoot, "FaceAnchor");
                }
            }

            RemoveLegacyFeature("Left Eye");
            RemoveLegacyFeature("Right Eye");
            RemoveLegacyFeature("Smile");
            RemoveLegacyFeature("Left Cheek");
            RemoveLegacyFeature("Right Cheek");

            leftEye = GetOrCreateFeature("Eye_L", PrimitiveType.Sphere, leftEye);
            rightEye = GetOrCreateFeature("Eye_R", PrimitiveType.Sphere, rightEye);
            mouth = GetOrCreateFeature("Mouth", PrimitiveType.Cube, mouth);
            leftCheek = GetOrCreateFeature("Cheek_L", PrimitiveType.Sphere, leftCheek);
            rightCheek = GetOrCreateFeature("Cheek_R", PrimitiveType.Sphere, rightCheek);
            cheeseHoleA = GetOrCreateFeature("Cheese Hole A", PrimitiveType.Sphere, cheeseHoleA);
            cheeseHoleB = GetOrCreateFeature("Cheese Hole B", PrimitiveType.Sphere, cheeseHoleB);
            cheeseHoleC = GetOrCreateFeature("Cheese Hole C", PrimitiveType.Sphere, cheeseHoleC);
            UpdateHatchedFeatureVisibility();
        }

        private Transform GetOrCreateFeature(string name, PrimitiveType primitive, Transform existing)
        {
            if (existing != null)
            {
                return existing;
            }

            var found = faceRoot.Find(name);
            if (found != null)
            {
                return found;
            }

            var feature = GameObject.CreatePrimitive(primitive);
            feature.name = name;
            feature.transform.SetParent(faceRoot, false);

            var collider = feature.GetComponent<Collider>();
            if (collider != null)
            {
                DestroySafely(collider);
            }

            ConfigureOverlayRenderer(feature.GetComponent<Renderer>());
            return feature.transform;
        }

        private void UpdateVisualShape(bool hatched)
        {
            if (body == null)
            {
                return;
            }

            body.localPosition = new Vector3(0f, hatched ? -0.02f : -0.04f, 0f);
            body.localRotation = Quaternion.identity;
            body.localScale = hatched
                ? new Vector3(1.12f, 0.88f, 0.86f)
                : new Vector3(0.94f, 1.12f, 0.82f);

            ConfigureBlobPart(bellyPatch, new Vector3(0f, hatched ? -0.13f : -0.18f, -0.64f), new Vector3(0.58f, 0.42f, 0.045f), Quaternion.identity, true);
            ConfigureBlobPart(creamCap, new Vector3(-0.12f, hatched ? 0.52f : 0.62f, -0.47f), new Vector3(0.5f, 0.18f, 0.18f), Quaternion.Euler(0f, 0f, -9f), true);
            ConfigureBlobPart(leftStub, new Vector3(-0.78f, -0.14f, -0.05f), new Vector3(0.26f, 0.18f, 0.2f), Quaternion.Euler(0f, 0f, 18f), hatched);
            ConfigureBlobPart(rightStub, new Vector3(0.78f, -0.14f, -0.05f), new Vector3(0.26f, 0.18f, 0.2f), Quaternion.Euler(0f, 0f, -18f), hatched);
            ConfigureBlobPart(highlight, new Vector3(-0.34f, hatched ? 0.22f : 0.32f, -0.67f), new Vector3(0.36f, 0.16f, 0.035f), Quaternion.Euler(0f, 0f, -14f), true);
            ConfigureBlobPart(softShadow, new Vector3(0f, -0.72f, 0.18f), new Vector3(hatched ? 1.34f : 1.08f, 0.035f, 0.54f), Quaternion.identity, true);
        }

        private static void ConfigureBlobPart(Transform part, Vector3 localPosition, Vector3 localScale, Quaternion localRotation, bool visible)
        {
            if (part == null)
            {
                return;
            }

            part.gameObject.SetActive(visible);
            part.localPosition = localPosition;
            part.localScale = localScale;
            part.localRotation = localRotation;
        }

        private void UpdateRigColors(Color bodyColor)
        {
            UpdateVisualShape(current != null && current.isHatched);
            PaintFeature(bodyRenderer, bodyColor);
            PaintFeature(bellyPatchRenderer, new Color(1f, 0.96f, 0.78f));
            PaintFeature(creamCapRenderer, new Color(1f, 0.98f, 0.86f));
            PaintFeature(leftStubRenderer, bodyColor);
            PaintFeature(rightStubRenderer, bodyColor);
            PaintFeature(highlightRenderer, new Color(1f, 0.98f, 0.86f));
            PaintFeature(softShadowRenderer, new Color(0.2f, 0.14f, 0.09f, 0.28f));
        }

        private void UpdateHatchedFeatureVisibility()
        {
            if (faceRoot == null)
            {
                return;
            }

            faceRoot.gameObject.SetActive(current != null);
        }

        private void UpdateHatchedFeatureAppearance()
        {
            if (current == null || faceRoot == null)
            {
                return;
            }

            ApplyFaceLayout(GetExpression(current));
        }

        private void ApplyFaceLayout(FaceExpression expression)
        {
            var eyeColor = new Color(0.16f, 0.1f, 0.07f);
            var mouthColor = new Color(0.25f, 0.13f, 0.08f);
            var cheekColor = new Color(1f, 0.52f, 0.36f);
            var holeColor = new Color(0.82f, 0.48f, 0.12f);
            var hatched = current != null && current.isHatched;

            faceRoot.localPosition = hatched ? new Vector3(0f, 0.02f, 0f) : new Vector3(0f, -0.05f, 0f);
            faceRoot.localRotation = Quaternion.identity;
            SetFeature(leftEye, new Vector3(-0.25f, 0.18f, -0.67f), new Vector3(0.17f, 0.19f, 0.04f), Quaternion.identity, true, eyeColor);
            SetFeature(rightEye, new Vector3(0.25f, 0.18f, -0.67f), new Vector3(0.17f, 0.19f, 0.04f), Quaternion.identity, true, eyeColor);
            SetFeature(mouth, new Vector3(0f, -0.12f, -0.69f), new Vector3(0.28f, 0.035f, 0.03f), Quaternion.identity, true, mouthColor);
            SetFeature(leftCheek, new Vector3(-0.42f, -0.02f, -0.65f), new Vector3(0.2f, 0.1f, 0.032f), Quaternion.identity, true, cheekColor);
            SetFeature(rightCheek, new Vector3(0.42f, -0.02f, -0.65f), new Vector3(0.2f, 0.1f, 0.032f), Quaternion.identity, true, cheekColor);
            SetFeature(cheeseHoleA, new Vector3(-0.18f, 0.42f, -0.65f), new Vector3(0.11f, 0.075f, 0.025f), Quaternion.identity, hatched, holeColor);
            SetFeature(cheeseHoleB, new Vector3(0.38f, 0.36f, -0.63f), new Vector3(0.08f, 0.06f, 0.025f), Quaternion.identity, hatched, holeColor);
            SetFeature(cheeseHoleC, new Vector3(0.14f, -0.38f, -0.63f), new Vector3(0.1f, 0.07f, 0.025f), Quaternion.identity, hatched, holeColor);

            if (expression == FaceExpression.Happy)
            {
                SetFeature(leftEye, new Vector3(-0.25f, 0.21f, -0.67f), new Vector3(0.19f, 0.19f, 0.04f), Quaternion.identity, true, eyeColor);
                SetFeature(rightEye, new Vector3(0.25f, 0.21f, -0.67f), new Vector3(0.19f, 0.19f, 0.04f), Quaternion.identity, true, eyeColor);
                SetFeature(mouth, new Vector3(0f, -0.08f, -0.69f), new Vector3(0.38f, 0.04f, 0.03f), Quaternion.identity, true, mouthColor);
                SetFeature(leftCheek, new Vector3(-0.44f, -0.01f, -0.65f), new Vector3(0.23f, 0.12f, 0.032f), Quaternion.identity, true, cheekColor);
                SetFeature(rightCheek, new Vector3(0.44f, -0.01f, -0.65f), new Vector3(0.23f, 0.12f, 0.032f), Quaternion.identity, true, cheekColor);
                return;
            }

            if (expression == FaceExpression.Sleepy)
            {
                SetFeature(leftEye, new Vector3(-0.25f, 0.14f, -0.67f), new Vector3(0.18f, 0.045f, 0.04f), Quaternion.identity, true, eyeColor);
                SetFeature(rightEye, new Vector3(0.25f, 0.14f, -0.67f), new Vector3(0.18f, 0.045f, 0.04f), Quaternion.identity, true, eyeColor);
                SetFeature(mouth, new Vector3(0f, -0.18f, -0.69f), new Vector3(0.18f, 0.035f, 0.03f), Quaternion.identity, true, mouthColor);
                SetFeature(leftCheek, Vector3.zero, Vector3.one, Quaternion.identity, false, cheekColor);
                SetFeature(rightCheek, Vector3.zero, Vector3.one, Quaternion.identity, false, cheekColor);
                return;
            }

            if (expression == FaceExpression.Sick)
            {
                var paleEye = new Color(0.1f, 0.14f, 0.2f);
                SetFeature(leftEye, new Vector3(-0.25f, 0.14f, -0.67f), new Vector3(0.13f, 0.09f, 0.04f), Quaternion.Euler(0f, 0f, -12f), true, paleEye);
                SetFeature(rightEye, new Vector3(0.25f, 0.14f, -0.67f), new Vector3(0.13f, 0.09f, 0.04f), Quaternion.Euler(0f, 0f, 12f), true, paleEye);
                SetFeature(mouth, new Vector3(0f, -0.22f, -0.69f), new Vector3(0.2f, 0.035f, 0.03f), Quaternion.Euler(0f, 0f, 180f), true, mouthColor);
                SetFeature(leftCheek, Vector3.zero, Vector3.one, Quaternion.identity, false, cheekColor);
                SetFeature(rightCheek, Vector3.zero, Vector3.one, Quaternion.identity, false, cheekColor);
                return;
            }

            if (expression == FaceExpression.Hungry)
            {
                SetFeature(leftEye, new Vector3(-0.25f, 0.17f, -0.67f), new Vector3(0.13f, 0.18f, 0.04f), Quaternion.identity, true, eyeColor);
                SetFeature(rightEye, new Vector3(0.25f, 0.17f, -0.67f), new Vector3(0.13f, 0.18f, 0.04f), Quaternion.identity, true, eyeColor);
                SetFeature(mouth, new Vector3(0f, -0.2f, -0.69f), new Vector3(0.09f, 0.09f, 0.03f), Quaternion.identity, true, mouthColor);
                SetFeature(leftCheek, new Vector3(-0.42f, -0.07f, -0.65f), new Vector3(0.13f, 0.07f, 0.032f), Quaternion.identity, true, new Color(1f, 0.62f, 0.38f));
                SetFeature(rightCheek, new Vector3(0.42f, -0.07f, -0.65f), new Vector3(0.13f, 0.07f, 0.032f), Quaternion.identity, true, new Color(1f, 0.62f, 0.38f));
                return;
            }

            if (expression == FaceExpression.Upset)
            {
                var mutedHoleColor = new Color(0.55f, 0.4f, 0.2f);
                SetFeature(mouth, new Vector3(0f, -0.18f, -0.69f), new Vector3(0.24f, 0.035f, 0.03f), Quaternion.Euler(0f, 0f, -5f), true, mouthColor);
                SetFeature(cheeseHoleA, new Vector3(-0.23f, 0.43f, -0.65f), new Vector3(0.13f, 0.09f, 0.025f), Quaternion.identity, hatched, mutedHoleColor);
                SetFeature(cheeseHoleB, new Vector3(0.36f, 0.35f, -0.63f), new Vector3(0.1f, 0.07f, 0.025f), Quaternion.identity, hatched, mutedHoleColor);
                SetFeature(cheeseHoleC, new Vector3(0.2f, -0.4f, -0.63f), new Vector3(0.13f, 0.09f, 0.025f), Quaternion.identity, hatched, mutedHoleColor);
                return;
            }

            if (expression == FaceExpression.Surprised)
            {
                SetFeature(leftEye, new Vector3(-0.26f, 0.22f, -0.67f), new Vector3(0.19f, 0.21f, 0.04f), Quaternion.identity, true, eyeColor);
                SetFeature(rightEye, new Vector3(0.26f, 0.22f, -0.67f), new Vector3(0.19f, 0.21f, 0.04f), Quaternion.identity, true, eyeColor);
                SetFeature(mouth, new Vector3(0f, -0.18f, -0.69f), new Vector3(0.11f, 0.11f, 0.032f), Quaternion.identity, true, mouthColor);
            }
        }

        private void SetFeature(
            Transform feature,
            Vector3 localPosition,
            Vector3 localScale,
            Quaternion localRotation,
            bool visible,
            Color color)
        {
            if (feature == null)
            {
                return;
            }

            feature.gameObject.SetActive(visible);
            if (!visible)
            {
                return;
            }

            feature.localPosition = localPosition;
            feature.localScale = localScale;
            feature.localRotation = localRotation;
            PaintFeature(feature.GetComponent<Renderer>(), color);
        }

        private void EnsureEventCue()
        {
            if (eventCue != null)
            {
                return;
            }

            if (vfxRoot == null)
            {
                EnsureVisualRig();
            }

            var cue = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cue.name = "Event Cue";
            cue.transform.SetParent(vfxRoot != null ? vfxRoot : transform, false);
            cue.transform.localPosition = new Vector3(0f, 1.04f, -0.12f);
            cue.transform.localScale = new Vector3(0.18f, 0.18f, 0.06f);

            var collider = cue.GetComponent<Collider>();
            if (collider != null)
            {
                DestroySafely(collider);
            }

            eventCue = cue.transform;
            eventCueRenderer = cue.GetComponent<Renderer>();
            ConfigureOverlayRenderer(eventCueRenderer);
            eventCue.gameObject.SetActive(false);
        }

        private void ShowEventCue(string eventId, Color color)
        {
            if (eventCue == null)
            {
                return;
            }

            eventCueStartedAt = Time.realtimeSinceStartup;
            eventCueVisible = true;
            eventCueBaseScale = GetEventCueScale(eventId);
            eventCue.localPosition = new Vector3(0f, 1.04f, -0.12f);
            eventCue.localRotation = Quaternion.identity;
            eventCue.localScale = eventCueBaseScale;
            eventCue.gameObject.SetActive(true);
            PaintFeature(eventCueRenderer, color);
        }

        private void UpdateEventCue()
        {
            if (!eventCueVisible || eventCue == null)
            {
                return;
            }

            var normalized = Mathf.Clamp01((Time.realtimeSinceStartup - eventCueStartedAt) / EventCueDuration);
            if (normalized >= 1f)
            {
                HideEventCue();
                return;
            }

            var arc = Mathf.Sin(normalized * Mathf.PI);
            var drift = normalized * 0.16f;
            var pulse = 1f + arc * 0.24f;
            eventCue.localPosition = new Vector3(0f, 1.04f + arc * 0.12f + drift, -0.12f);
            eventCue.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(normalized * Mathf.PI * 2f) * 12f);
            eventCue.localScale = eventCueBaseScale * pulse;
        }

        private void HideEventCue()
        {
            eventCueVisible = false;
            if (eventCue != null)
            {
                eventCue.gameObject.SetActive(false);
            }
        }

        private void CaptureRestingPosition()
        {
            if (hasRestingLocalPosition)
            {
                return;
            }

            restingLocalPosition = transform.localPosition;
            hasRestingLocalPosition = true;
        }

        private void DisableSpriteRendererIfPresent()
        {
            var spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = false;
            }
        }

        private void DisableLegacyRootRenderer()
        {
            var rootRenderer = GetComponent<Renderer>();
            if (rootRenderer != null && rootRenderer != bodyRenderer)
            {
                rootRenderer.enabled = false;
            }
        }

        private Transform GetOrCreateChild(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child != null)
            {
                return child;
            }

            var childObject = new GameObject(name);
            childObject.transform.SetParent(parent, false);
            return childObject.transform;
        }

        private Transform GetOrCreatePrimitiveChild(Transform parent, string name, PrimitiveType primitive)
        {
            var child = parent.Find(name);
            if (child == null)
            {
                var primitiveObject = GameObject.CreatePrimitive(primitive);
                primitiveObject.name = name;
                primitiveObject.transform.SetParent(parent, false);
                child = primitiveObject.transform;
            }

            var collider = child.GetComponent<Collider>();
            if (collider != null)
            {
                DestroySafely(collider);
            }

            return child;
        }

        private void ConfigureOverlayRenderer(Renderer renderer)
        {
            if (renderer == null)
            {
                return;
            }

            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private void RemoveLegacyFeature(string name)
        {
            if (faceRoot == null)
            {
                return;
            }

            var legacy = faceRoot.Find(name);
            if (legacy != null)
            {
                DestroySafely(legacy.gameObject);
            }
        }

        private void PaintFeature(Renderer renderer, Color color)
        {
            if (renderer == null)
            {
                return;
            }

            EnsurePropertyBlock();
            renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BaseColorId, color);
            propertyBlock.SetColor(ColorId, color);
            renderer.SetPropertyBlock(propertyBlock);
        }

        private static void DestroySafely(Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
                return;
            }

            DestroyImmediate(target);
        }

        private void EnsurePropertyBlock()
        {
            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }
        }

        private static FaceExpression GetExpression(CheeseTamaModel tama)
        {
            if (tama == null || tama.stats == null)
            {
                return FaceExpression.Idle;
            }

            if (tama.stats.health < 35)
            {
                return FaceExpression.Sick;
            }

            if (tama.stats.hunger < 25)
            {
                return FaceExpression.Hungry;
            }

            if (tama.stats.cleanliness < 35)
            {
                return FaceExpression.Upset;
            }

            if (tama.stats.sleepiness > 75)
            {
                return FaceExpression.Sleepy;
            }

            if (tama.stats.maturation > 95 || tama.levelProgress > 95)
            {
                return FaceExpression.Surprised;
            }

            if (tama.stats.mood > 80)
            {
                return FaceExpression.Happy;
            }

            return FaceExpression.Idle;
        }

        private static Color GetStateColor(CheeseTamaModel tama)
        {
            if (tama == null || tama.stats == null)
            {
                return new Color(1f, 0.84f, 0.28f);
            }

            if (tama.stats.health < 35)
            {
                return new Color(0.74f, 0.84f, 0.98f);
            }

            if (tama.stats.hunger < 25)
            {
                return new Color(0.98f, 0.72f, 0.4f);
            }

            if (tama.stats.cleanliness < 35)
            {
                return new Color(0.72f, 0.64f, 0.48f);
            }

            if (tama.stats.sleepiness > 75)
            {
                return new Color(0.72f, 0.72f, 0.92f);
            }

            if (tama.stats.mood > 80)
            {
                return new Color(1f, 0.9f, 0.42f);
            }

            if (tama.isHatched)
            {
                return new Color(1f, 0.78f, 0.34f);
            }

            return new Color(1f, 0.86f, 0.38f);
        }

        private static Color GetEventColor(string eventId)
        {
            return eventId switch
            {
                "happy_wiggle" => new Color(1f, 0.92f, 0.28f),
                "small_fever" => new Color(0.5f, 0.72f, 1f),
                "hungry_peep" => new Color(1f, 0.55f, 0.22f),
                "dusty_corner" => new Color(0.55f, 0.42f, 0.26f),
                "sleepy_yawn" => new Color(0.62f, 0.58f, 1f),
                "milk_drop_catch" => new Color(0.74f, 0.92f, 1f),
                _ => new Color(0.62f, 0.95f, 0.82f)
            };
        }

        private static Vector3 GetEventCueScale(string eventId)
        {
            return eventId switch
            {
                "sleepy_yawn" => new Vector3(0.24f, 0.12f, 0.05f),
                "dusty_corner" => new Vector3(0.14f, 0.14f, 0.05f),
                "happy_wiggle" => new Vector3(0.22f, 0.22f, 0.06f),
                "milk_drop_catch" => new Vector3(0.16f, 0.22f, 0.05f),
                _ => new Vector3(0.18f, 0.18f, 0.06f)
            };
        }

        private enum FaceExpression
        {
            Idle,
            Happy,
            Hungry,
            Sleepy,
            Upset,
            Surprised,
            Sick
        }
    }
}
