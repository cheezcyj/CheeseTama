using CheeseTama.Gameplay;
using CheeseTama.Utilities;
using UnityEngine;
using UnityEngine.Rendering;

namespace CheeseTama.UI
{
    public sealed class CheeseTamaVisualController : MonoBehaviour
    {
        [SerializeField] private Renderer targetRenderer;

        private const float CareReactionDuration = 0.68f;
        private const float CareReactionHopHeight = 0.13f;
        private const float CareReactionPunch = 0.055f;
        private const float CareReactionFlash = 0.1f;
        private const float HatchReactionDuration = 1.15f;
        private const float HatchReactionHopHeight = 0.36f;
        private const float HatchReactionPunch = 0.16f;
        private const float HatchReactionFlash = 0.32f;
        private const float EventReactionDuration = 0.92f;
        private const float EventReactionHopHeight = 0.18f;
        private const float EventReactionPunch = 0.08f;
        private const float EventReactionFlash = 0.4f;
        private const float EventCueDuration = 1.2f;

        private Transform modelRoot;
        private Transform visualRoot;
        private Transform outlineRoot;
        private Transform cheeseMarksRoot;
        private Transform bodyOutline;
        private Transform leftArmOutline;
        private Transform rightArmOutline;
        private Transform leftFootOutline;
        private Transform rightFootOutline;
        private Transform topCurlOutline;
        private Transform body;
        private Transform leftArm;
        private Transform rightArm;
        private Transform leftFoot;
        private Transform rightFoot;
        private Transform topCurl;
        private Transform topCurlTip;
        private Transform largeHighlight;
        private Transform smallHighlight;
        private Transform softShadow;
        private Transform crownBand;
        private Transform crownPointA;
        private Transform crownPointB;
        private Transform crownPointC;
        private Transform faceRoot;
        private Transform leftEye;
        private Transform rightEye;
        private Transform leftEyeSparkle;
        private Transform rightEyeSparkle;
        private Transform mouthLeft;
        private Transform mouthRight;
        private Transform mouthOpen;
        private Transform leftCheek;
        private Transform rightCheek;
        private Transform vfxRoot;
        private Transform eventCue;

        private Renderer bodyRenderer;
        private Renderer leftArmRenderer;
        private Renderer rightArmRenderer;
        private Renderer leftFootRenderer;
        private Renderer rightFootRenderer;
        private Renderer topCurlRenderer;
        private Renderer topCurlTipRenderer;
        private Renderer largeHighlightRenderer;
        private Renderer smallHighlightRenderer;
        private Renderer softShadowRenderer;
        private Renderer bodyOutlineRenderer;
        private Renderer leftArmOutlineRenderer;
        private Renderer rightArmOutlineRenderer;
        private Renderer leftFootOutlineRenderer;
        private Renderer rightFootOutlineRenderer;
        private Renderer topCurlOutlineRenderer;
        private Renderer crownBandRenderer;
        private Renderer crownPointARenderer;
        private Renderer crownPointBRenderer;
        private Renderer crownPointCRenderer;
        private Renderer leftEyeSparkleRenderer;
        private Renderer rightEyeSparkleRenderer;
        private Renderer eventCueRenderer;

        private readonly Transform[] cheeseHoles = new Transform[7];
        private readonly Renderer[] cheeseHoleRenderers = new Renderer[7];
        private readonly Transform[] cheeseSpeckles = new Transform[6];
        private readonly Renderer[] cheeseSpeckleRenderers = new Renderer[6];
        private static Mesh bodyBlobMesh;
        private static Mesh bodyOutlineBlobMesh;

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
            EnsureVisualRig();
            EnsureFaceRig();
            CaptureRestingPosition();
        }

        private void LateUpdate()
        {
            if (current == null)
            {
                return;
            }

            EnsureVisualRig();
            EnsureFaceRig();
            var stage = GetStage(current);
            var baseScale = GetStageRootScale(stage);
            var baseColor = GetStateColor(current, stage);
            UpdateRig(stage, baseColor);
            UpdateEventCue();

            if (!isReacting)
            {
                var time = Time.realtimeSinceStartup + idleSeed;
                var breath = Mathf.Sin(time * 1.65f) * 0.015f;
                var sway = Mathf.Sin(time * (stage == CharacterStage.Egg ? 0.55f : 0.82f)) * (stage == CharacterStage.Egg ? 0.5f : 1.15f);
                var targetScale = new Vector3(
                    baseScale.x * (1f + breath),
                    baseScale.y * (1f - breath * 0.35f),
                    baseScale.z * (1f + breath * 0.6f));

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
                visualRoot.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(normalized * Mathf.PI * 2f) * 2.1f * settle);
            }

            UpdateRig(stage, Color.Lerp(baseColor, reactionTintColor, arc * reactionFlash));

            if (normalized >= 1f)
            {
                isReacting = false;
                reactionTintColor = Color.white;
                transform.localPosition = restingLocalPosition;
                transform.localScale = baseScale;
                UpdateRig(stage, baseColor);
            }
        }

        public void Bind(CheeseTamaModel tama)
        {
            EnsureVisualRig();
            EnsureFaceRig();
            CaptureRestingPosition();
            current = tama;
            if (current == null)
            {
                return;
            }

            var stage = GetStage(current);
            if (!isReacting)
            {
                transform.localScale = GetStageRootScale(stage);
                transform.localPosition = restingLocalPosition;
            }

            UpdateRig(stage, GetStateColor(current, stage));
        }

        public void React(bool celebrate = false)
        {
            CaptureRestingPosition();
            HideEventCue();
            var stage = GetStage(current);
            reactionStartedAt = Time.realtimeSinceStartup;
            reactionDuration = celebrate ? HatchReactionDuration : CareReactionDuration;
            reactionHopHeight = celebrate ? HatchReactionHopHeight : CareReactionHopHeight;
            reactionPunch = celebrate ? HatchReactionPunch : CareReactionPunch;
            reactionFlash = celebrate ? HatchReactionFlash : CareReactionFlash;
            reactionTintColor = Color.white;
            isReacting = true;

            var baseScale = GetStageRootScale(stage);
            transform.localPosition = restingLocalPosition;
            var squash = celebrate ? 0.9f : 0.965f;
            var stretch = celebrate ? 1.12f : 1.035f;
            transform.localScale = new Vector3(baseScale.x * stretch, baseScale.y * squash, baseScale.z * stretch);
            UpdateRig(stage, Color.Lerp(GetStateColor(current, stage), Color.white, celebrate ? 0.2f : 0.08f));
        }

        public void ReactEvent(string eventId)
        {
            CaptureRestingPosition();
            EnsureEventCue();

            var stage = GetStage(current);
            reactionTintColor = GetEventColor(eventId);
            reactionStartedAt = Time.realtimeSinceStartup;
            reactionDuration = EventReactionDuration;
            reactionHopHeight = EventReactionHopHeight;
            reactionPunch = EventReactionPunch;
            reactionFlash = EventReactionFlash;
            isReacting = true;

            var baseScale = GetStageRootScale(stage);
            transform.localPosition = restingLocalPosition;
            transform.localScale = new Vector3(baseScale.x * 1.04f, baseScale.y * 0.96f, baseScale.z * 1.03f);
            UpdateRig(stage, Color.Lerp(GetStateColor(current, stage), reactionTintColor, 0.2f));
            ShowEventCue(eventId, reactionTintColor);
        }

        private void EnsureVisualRig()
        {
            DisableSpriteRendererIfPresent();
            DisableLegacyRootRenderer();

            modelRoot = GetOrCreateChild(transform, "ModelRoot");
            visualRoot = GetOrCreateChild(modelRoot, "VisualRoot");
            outlineRoot = GetOrCreateChild(visualRoot, "ToonOutline");
            cheeseMarksRoot = GetOrCreateChild(visualRoot, "CheeseMarks");
            RemoveLegacyVisualChild("Milk Belly Patch");
            RemoveLegacyVisualChild("Cream Cap");
            RemoveLegacyVisualChild("Left Jelly Stub");
            RemoveLegacyVisualChild("Right Jelly Stub");
            RemoveLegacyVisualChild("Highlight");
            for (var i = 0; i < cheeseHoles.Length; i += 1)
            {
                RemoveLegacyVisualChild($"Cheese Hole {i + 1}");
            }

            for (var i = 0; i < cheeseSpeckles.Length; i += 1)
            {
                RemoveLegacyVisualChild($"Cheese Speckle {i + 1}");
            }

            bodyOutline = GetOrCreatePrimitiveChild(outlineRoot, "Body Toon Outline", PrimitiveType.Sphere);
            leftArmOutline = GetOrCreatePrimitiveChild(outlineRoot, "Left Arm Toon Outline", PrimitiveType.Sphere);
            rightArmOutline = GetOrCreatePrimitiveChild(outlineRoot, "Right Arm Toon Outline", PrimitiveType.Sphere);
            leftFootOutline = GetOrCreatePrimitiveChild(outlineRoot, "Left Foot Toon Outline", PrimitiveType.Sphere);
            rightFootOutline = GetOrCreatePrimitiveChild(outlineRoot, "Right Foot Toon Outline", PrimitiveType.Sphere);
            topCurlOutline = GetOrCreatePrimitiveChild(outlineRoot, "Top Curl Toon Outline", PrimitiveType.Sphere);

            body = GetOrCreatePrimitiveChild(visualRoot, "Body", PrimitiveType.Sphere);
            ApplyBlobMesh(bodyOutline, true);
            ApplyBlobMesh(body, false);
            leftArm = GetOrCreatePrimitiveChild(visualRoot, "Left Soft Arm", PrimitiveType.Sphere);
            rightArm = GetOrCreatePrimitiveChild(visualRoot, "Right Soft Arm", PrimitiveType.Sphere);
            leftFoot = GetOrCreatePrimitiveChild(visualRoot, "Left Little Foot", PrimitiveType.Sphere);
            rightFoot = GetOrCreatePrimitiveChild(visualRoot, "Right Little Foot", PrimitiveType.Sphere);
            topCurl = GetOrCreatePrimitiveChild(visualRoot, "Top Curl", PrimitiveType.Sphere);
            topCurlTip = GetOrCreatePrimitiveChild(visualRoot, "Top Curl Tip", PrimitiveType.Sphere);
            largeHighlight = GetOrCreatePrimitiveChild(visualRoot, "Large Milk Highlight", PrimitiveType.Sphere);
            smallHighlight = GetOrCreatePrimitiveChild(visualRoot, "Small Milk Highlight", PrimitiveType.Sphere);
            softShadow = GetOrCreatePrimitiveChild(visualRoot, "SoftShadow", PrimitiveType.Sphere);
            crownBand = GetOrCreatePrimitiveChild(visualRoot, "Crown Band", PrimitiveType.Cube);
            crownPointA = GetOrCreatePrimitiveChild(visualRoot, "Crown Point A", PrimitiveType.Sphere);
            crownPointB = GetOrCreatePrimitiveChild(visualRoot, "Crown Point B", PrimitiveType.Sphere);
            crownPointC = GetOrCreatePrimitiveChild(visualRoot, "Crown Point C", PrimitiveType.Sphere);
            vfxRoot = GetOrCreateChild(visualRoot, "VFXRoot");

            for (var i = 0; i < cheeseHoles.Length; i += 1)
            {
                cheeseHoles[i] = GetOrCreatePrimitiveChild(cheeseMarksRoot, $"Cheese Hole {i + 1}", PrimitiveType.Sphere);
                cheeseHoleRenderers[i] = cheeseHoles[i].GetComponent<Renderer>();
                ConfigureOverlayRenderer(cheeseHoleRenderers[i]);
            }

            for (var i = 0; i < cheeseSpeckles.Length; i += 1)
            {
                cheeseSpeckles[i] = GetOrCreatePrimitiveChild(cheeseMarksRoot, $"Cheese Speckle {i + 1}", PrimitiveType.Sphere);
                cheeseSpeckleRenderers[i] = cheeseSpeckles[i].GetComponent<Renderer>();
                ConfigureOverlayRenderer(cheeseSpeckleRenderers[i]);
            }

            bodyRenderer = body.GetComponent<Renderer>();
            leftArmRenderer = leftArm.GetComponent<Renderer>();
            rightArmRenderer = rightArm.GetComponent<Renderer>();
            leftFootRenderer = leftFoot.GetComponent<Renderer>();
            rightFootRenderer = rightFoot.GetComponent<Renderer>();
            topCurlRenderer = topCurl.GetComponent<Renderer>();
            topCurlTipRenderer = topCurlTip.GetComponent<Renderer>();
            largeHighlightRenderer = largeHighlight.GetComponent<Renderer>();
            smallHighlightRenderer = smallHighlight.GetComponent<Renderer>();
            softShadowRenderer = softShadow.GetComponent<Renderer>();
            bodyOutlineRenderer = bodyOutline.GetComponent<Renderer>();
            leftArmOutlineRenderer = leftArmOutline.GetComponent<Renderer>();
            rightArmOutlineRenderer = rightArmOutline.GetComponent<Renderer>();
            leftFootOutlineRenderer = leftFootOutline.GetComponent<Renderer>();
            rightFootOutlineRenderer = rightFootOutline.GetComponent<Renderer>();
            topCurlOutlineRenderer = topCurlOutline.GetComponent<Renderer>();
            crownBandRenderer = crownBand.GetComponent<Renderer>();
            crownPointARenderer = crownPointA.GetComponent<Renderer>();
            crownPointBRenderer = crownPointB.GetComponent<Renderer>();
            crownPointCRenderer = crownPointC.GetComponent<Renderer>();
            targetRenderer = bodyRenderer;

            ConfigureOverlayRenderer(leftArmRenderer);
            ConfigureOverlayRenderer(rightArmRenderer);
            ConfigureOverlayRenderer(leftFootRenderer);
            ConfigureOverlayRenderer(rightFootRenderer);
            ConfigureOverlayRenderer(topCurlRenderer);
            ConfigureOverlayRenderer(topCurlTipRenderer);
            ConfigureOverlayRenderer(largeHighlightRenderer);
            ConfigureOverlayRenderer(smallHighlightRenderer);
            ConfigureOverlayRenderer(softShadowRenderer);
            ConfigureOverlayRenderer(bodyOutlineRenderer);
            ConfigureOverlayRenderer(leftArmOutlineRenderer);
            ConfigureOverlayRenderer(rightArmOutlineRenderer);
            ConfigureOverlayRenderer(leftFootOutlineRenderer);
            ConfigureOverlayRenderer(rightFootOutlineRenderer);
            ConfigureOverlayRenderer(topCurlOutlineRenderer);
            ConfigureOverlayRenderer(crownBandRenderer);
            ConfigureOverlayRenderer(crownPointARenderer);
            ConfigureOverlayRenderer(crownPointBRenderer);
            ConfigureOverlayRenderer(crownPointCRenderer);
            if (bodyRenderer != null)
            {
                bodyRenderer.shadowCastingMode = ShadowCastingMode.On;
                bodyRenderer.receiveShadows = true;
            }
        }

        private void EnsureFaceRig()
        {
            if (visualRoot == null)
            {
                EnsureVisualRig();
            }

            faceRoot = GetOrCreateChild(visualRoot, "FaceAnchor");
            RemoveLegacyFeature("Left Eye");
            RemoveLegacyFeature("Right Eye");
            RemoveLegacyFeature("Smile");
            RemoveLegacyFeature("Mouth");
            RemoveLegacyFeature("Cheese Hole A");
            RemoveLegacyFeature("Cheese Hole B");
            RemoveLegacyFeature("Cheese Hole C");

            leftEye = GetOrCreateFeature("Eye_L", PrimitiveType.Sphere, leftEye);
            rightEye = GetOrCreateFeature("Eye_R", PrimitiveType.Sphere, rightEye);
            leftEyeSparkle = GetOrCreateFeature("Eye Sparkle L", PrimitiveType.Sphere, leftEyeSparkle);
            rightEyeSparkle = GetOrCreateFeature("Eye Sparkle R", PrimitiveType.Sphere, rightEyeSparkle);
            mouthLeft = GetOrCreateFeature("Mouth Stroke L", PrimitiveType.Cube, mouthLeft);
            mouthRight = GetOrCreateFeature("Mouth Stroke R", PrimitiveType.Cube, mouthRight);
            mouthOpen = GetOrCreateFeature("Mouth Open", PrimitiveType.Sphere, mouthOpen);
            leftCheek = GetOrCreateFeature("Cheek_L", PrimitiveType.Sphere, leftCheek);
            rightCheek = GetOrCreateFeature("Cheek_R", PrimitiveType.Sphere, rightCheek);
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

        private static void ApplyBlobMesh(Transform target, bool outline)
        {
            if (target == null || !target.TryGetComponent<MeshFilter>(out var meshFilter))
            {
                return;
            }

            if (outline)
            {
                bodyOutlineBlobMesh ??= CreateCheeseTamaBlobMesh("M_CheeseTama_BodyOutline_Runtime", 1.04f);
                meshFilter.sharedMesh = bodyOutlineBlobMesh;
                return;
            }

            bodyBlobMesh ??= CreateCheeseTamaBlobMesh("M_CheeseTama_Body_Runtime", 1f);
            meshFilter.sharedMesh = bodyBlobMesh;
        }

        private static Mesh CreateCheeseTamaBlobMesh(string name, float inflate)
        {
            const int latitude = 28;
            const int longitude = 44;
            var vertices = new Vector3[(latitude + 1) * (longitude + 1)];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[latitude * longitude * 6];

            var vertexIndex = 0;
            for (var lat = 0; lat <= latitude; lat += 1)
            {
                var v = lat / (float)latitude;
                var theta = v * Mathf.PI;
                var yUnit = Mathf.Cos(theta);
                var ring = Mathf.Sin(theta);
                var height01 = (yUnit + 1f) * 0.5f;
                var lowerBelly = 1f + Mathf.Exp(-Mathf.Pow((height01 - 0.27f) / 0.32f, 2f)) * 0.18f;
                var crownTaper = Mathf.Lerp(0.92f, 1.04f, Mathf.SmoothStep(0f, 1f, height01));
                var frontRoundness = 1f + Mathf.Exp(-Mathf.Pow((height01 - 0.44f) / 0.5f, 2f)) * 0.08f;

                var y = yUnit * 0.5f;
                if (y < -0.34f)
                {
                    y = Mathf.Lerp(y, -0.39f, Mathf.InverseLerp(-0.34f, -0.5f, y) * 0.55f);
                }

                for (var lon = 0; lon <= longitude; lon += 1)
                {
                    var u = lon / (float)longitude;
                    var phi = u * Mathf.PI * 2f;
                    var ripple = 1f + Mathf.Sin(phi * 3.0f + height01 * 5.2f) * 0.018f;
                    var x = Mathf.Cos(phi) * ring * 0.5f * lowerBelly * crownTaper * ripple;
                    var z = Mathf.Sin(phi) * ring * 0.5f * (0.9f + frontRoundness * 0.09f);

                    vertices[vertexIndex] = new Vector3(x, y, z) * inflate;
                    uv[vertexIndex] = new Vector2(u, v);
                    vertexIndex += 1;
                }
            }

            var triangleIndex = 0;
            for (var lat = 0; lat < latitude; lat += 1)
            {
                for (var lon = 0; lon < longitude; lon += 1)
                {
                    var current = lat * (longitude + 1) + lon;
                    var next = current + longitude + 1;

                    triangles[triangleIndex++] = current;
                    triangles[triangleIndex++] = current + 1;
                    triangles[triangleIndex++] = next;
                    triangles[triangleIndex++] = current + 1;
                    triangles[triangleIndex++] = next + 1;
                    triangles[triangleIndex++] = next;
                }
            }

            var mesh = new Mesh
            {
                name = name,
                vertices = vertices,
                uv = uv,
                triangles = triangles,
                hideFlags = HideFlags.DontSave
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private void UpdateRig(CharacterStage stage, Color bodyColor)
        {
            UpdateBodyShape(stage);
            UpdateFace(stage, GetExpression(current));
            PaintBody(bodyColor, stage);
        }

        private void UpdateBodyShape(CharacterStage stage)
        {
            var isEgg = stage == CharacterStage.Egg;
            var hasCurl = stage >= CharacterStage.Soft;
            var hasCrown = stage == CharacterStage.Final && isReacting && reactionFlash >= HatchReactionFlash;
            var bodyHeight = isEgg ? 1.16f : stage >= CharacterStage.Mature ? 1.06f : 1.0f;
            var bodyWidth = isEgg ? 1.02f : stage >= CharacterStage.Mature ? 1.28f : 1.18f;
            var bodyDepth = isEgg ? 0.96f : stage >= CharacterStage.Mature ? 1.02f : 0.98f;

            ConfigurePart(bodyOutline, new Vector3(0f, isEgg ? -0.04f : -0.08f, 0.08f), new Vector3(bodyWidth + 0.08f, bodyHeight + 0.08f, bodyDepth + 0.05f), Quaternion.identity, true);
            ConfigurePart(leftArmOutline, new Vector3(-0.74f, -0.12f, -0.2f), new Vector3(0.3f, 0.34f, 0.22f), Quaternion.Euler(0f, 0f, 18f), !isEgg);
            ConfigurePart(rightArmOutline, new Vector3(0.74f, -0.12f, -0.2f), new Vector3(0.3f, 0.34f, 0.22f), Quaternion.Euler(0f, 0f, -18f), !isEgg);
            ConfigurePart(leftFootOutline, new Vector3(-0.38f, -0.77f, -0.28f), new Vector3(0.36f, 0.18f, 0.2f), Quaternion.Euler(0f, 0f, -4f), !isEgg);
            ConfigurePart(rightFootOutline, new Vector3(0.38f, -0.77f, -0.28f), new Vector3(0.36f, 0.18f, 0.2f), Quaternion.Euler(0f, 0f, 4f), !isEgg);
            ConfigurePart(topCurlOutline, new Vector3(0.17f, 0.73f, -0.08f), new Vector3(0.22f, 0.38f, 0.16f), Quaternion.Euler(0f, 0f, -25f), hasCurl);

            ConfigurePart(body, new Vector3(0f, isEgg ? -0.04f : -0.08f, 0f), new Vector3(bodyWidth, bodyHeight, bodyDepth), Quaternion.identity, true);
            ConfigurePart(leftArm, new Vector3(-0.72f, -0.12f, -0.28f), new Vector3(0.25f, 0.29f, 0.2f), Quaternion.Euler(0f, 0f, 18f), !isEgg);
            ConfigurePart(rightArm, new Vector3(0.72f, -0.12f, -0.28f), new Vector3(0.25f, 0.29f, 0.2f), Quaternion.Euler(0f, 0f, -18f), !isEgg);
            ConfigurePart(leftFoot, new Vector3(-0.38f, -0.75f, -0.36f), new Vector3(0.31f, 0.14f, 0.18f), Quaternion.Euler(0f, 0f, -4f), !isEgg);
            ConfigurePart(rightFoot, new Vector3(0.38f, -0.75f, -0.36f), new Vector3(0.31f, 0.14f, 0.18f), Quaternion.Euler(0f, 0f, 4f), !isEgg);
            ConfigurePart(topCurl, new Vector3(0.16f, 0.72f, -0.16f), new Vector3(0.18f, 0.34f, 0.14f), Quaternion.Euler(0f, 0f, -25f), hasCurl);
            ConfigurePart(topCurlTip, new Vector3(0.27f, 0.91f, -0.17f), new Vector3(0.13f, 0.09f, 0.1f), Quaternion.identity, hasCurl);
            ConfigurePart(largeHighlight, new Vector3(-0.39f, isEgg ? 0.42f : 0.32f, -0.78f), new Vector3(0.36f, 0.14f, 0.036f), Quaternion.Euler(0f, 0f, -20f), true);
            ConfigurePart(smallHighlight, new Vector3(-0.58f, isEgg ? 0.18f : 0.06f, -0.79f), new Vector3(0.15f, 0.075f, 0.03f), Quaternion.Euler(0f, 0f, -18f), true);
            ConfigurePart(softShadow, new Vector3(0f, -0.84f, 0.2f), new Vector3(isEgg ? 1.2f : 1.5f, 0.035f, 0.62f), Quaternion.identity, true);

            ConfigurePart(crownBand, new Vector3(0.05f, 0.95f, -0.08f), new Vector3(0.48f, 0.08f, 0.08f), Quaternion.Euler(0f, 0f, -5f), hasCrown);
            ConfigurePart(crownPointA, new Vector3(-0.16f, 1.08f, -0.08f), new Vector3(0.08f, 0.12f, 0.08f), Quaternion.identity, hasCrown);
            ConfigurePart(crownPointB, new Vector3(0.05f, 1.14f, -0.08f), new Vector3(0.09f, 0.14f, 0.09f), Quaternion.identity, hasCrown);
            ConfigurePart(crownPointC, new Vector3(0.26f, 1.08f, -0.08f), new Vector3(0.08f, 0.12f, 0.08f), Quaternion.identity, hasCrown);

            UpdateCheeseSpots(stage);
        }

        private void UpdateCheeseSpots(CharacterStage stage)
        {
            var isEgg = stage == CharacterStage.Egg;
            if (IsEmmentalCheeseTama(current))
            {
                SetSpot(cheeseHoles[0], new Vector3(-0.56f, 0.28f, -0.82f), new Vector3(0.18f, 0.24f, 0.028f), Quaternion.Euler(0f, 0f, 8f), true);
                SetSpot(cheeseHoles[1], new Vector3(0.42f, 0.48f, -0.8f), new Vector3(0.18f, 0.16f, 0.028f), Quaternion.Euler(0f, 0f, -12f), true);
                SetSpot(cheeseHoles[2], new Vector3(0.62f, -0.08f, -0.8f), new Vector3(0.13f, 0.18f, 0.026f), Quaternion.Euler(0f, 0f, 6f), true);
                SetSpot(cheeseHoles[3], new Vector3(-0.12f, 0.63f, -0.78f), new Vector3(0.1f, 0.075f, 0.024f), Quaternion.identity, true);
                SetSpot(cheeseHoles[4], new Vector3(0.18f, -0.44f, -0.76f), new Vector3(0.105f, 0.085f, 0.024f), Quaternion.identity, true);
                SetSpot(cheeseHoles[5], new Vector3(-0.66f, -0.2f, -0.78f), new Vector3(0.105f, 0.13f, 0.024f), Quaternion.Euler(0f, 0f, -8f), true);
                SetSpot(cheeseHoles[6], new Vector3(0.02f, 0.28f, -0.79f), new Vector3(0.06f, 0.048f, 0.022f), Quaternion.identity, true);
            }
            else
            {
                SetSpot(cheeseHoles[0], new Vector3(-0.58f, 0.24f, -0.82f), new Vector3(0.16f, 0.21f, 0.028f), Quaternion.Euler(0f, 0f, 8f), true);
                SetSpot(cheeseHoles[1], new Vector3(0.48f, 0.43f, -0.8f), new Vector3(0.16f, 0.13f, 0.028f), Quaternion.Euler(0f, 0f, -15f), true);
                SetSpot(cheeseHoles[2], new Vector3(0.62f, -0.08f, -0.79f), new Vector3(0.105f, 0.15f, 0.026f), Quaternion.Euler(0f, 0f, 4f), stage >= CharacterStage.Soft);
                SetSpot(cheeseHoles[3], new Vector3(-0.16f, 0.58f, -0.78f), new Vector3(0.075f, 0.055f, 0.024f), Quaternion.identity, false);
                SetSpot(cheeseHoles[4], new Vector3(0.2f, -0.48f, -0.76f), new Vector3(0.085f, 0.065f, 0.024f), Quaternion.identity, false);
                SetSpot(cheeseHoles[5], new Vector3(-0.68f, -0.24f, -0.78f), new Vector3(0.09f, 0.12f, 0.024f), Quaternion.identity, false);
                SetSpot(cheeseHoles[6], new Vector3(0.02f, 0.42f, -0.78f), new Vector3(0.05f, 0.038f, 0.022f), Quaternion.identity, false);
            }

            SetSpot(cheeseSpeckles[0], new Vector3(-0.34f, -0.3f, -0.82f), new Vector3(0.038f, 0.022f, 0.018f), Quaternion.Euler(0f, 0f, -10f), true);
            SetSpot(cheeseSpeckles[1], new Vector3(-0.08f, -0.43f, -0.81f), new Vector3(0.03f, 0.02f, 0.018f), Quaternion.identity, true);
            SetSpot(cheeseSpeckles[2], new Vector3(0.34f, 0.2f, -0.82f), new Vector3(0.032f, 0.022f, 0.018f), Quaternion.identity, true);
            SetSpot(cheeseSpeckles[3], new Vector3(-0.04f, 0.62f, -0.79f), new Vector3(0.026f, 0.02f, 0.018f), Quaternion.identity, !isEgg);
            SetSpot(cheeseSpeckles[4], new Vector3(0.48f, -0.3f, -0.78f), new Vector3(0.034f, 0.022f, 0.018f), Quaternion.identity, stage >= CharacterStage.Grown);
            SetSpot(cheeseSpeckles[5], new Vector3(-0.46f, 0.5f, -0.79f), new Vector3(0.032f, 0.02f, 0.018f), Quaternion.identity, true);
        }

        private void UpdateFace(CharacterStage stage, FaceExpression expression)
        {
            if (faceRoot == null)
            {
                return;
            }

            var isEgg = stage == CharacterStage.Egg;
            faceRoot.gameObject.SetActive(true);
            faceRoot.localPosition = new Vector3(0f, isEgg ? -0.04f : 0.02f, 0f);
            faceRoot.localRotation = Quaternion.identity;

            var eyeColor = new Color(0.16f, 0.08f, 0.04f);
            var mouthColor = new Color(0.24f, 0.12f, 0.06f);
            var cheekColor = new Color(1f, 0.48f, 0.34f);

            SetFeature(leftEye, new Vector3(-0.32f, 0.12f, -0.86f), new Vector3(0.19f, 0.25f, 0.046f), Quaternion.identity, true, eyeColor);
            SetFeature(rightEye, new Vector3(0.32f, 0.12f, -0.86f), new Vector3(0.19f, 0.25f, 0.046f), Quaternion.identity, true, eyeColor);
            SetFeature(leftEyeSparkle, new Vector3(-0.38f, 0.21f, -0.9f), new Vector3(0.052f, 0.064f, 0.018f), Quaternion.identity, true, Color.white);
            SetFeature(rightEyeSparkle, new Vector3(0.26f, 0.21f, -0.9f), new Vector3(0.052f, 0.064f, 0.018f), Quaternion.identity, true, Color.white);
            SetMouthSmile(mouthColor);
            SetFeature(mouthOpen, Vector3.zero, Vector3.one, Quaternion.identity, false, mouthColor);
            SetFeature(leftCheek, new Vector3(-0.53f, -0.11f, -0.84f), new Vector3(0.22f, 0.105f, 0.028f), Quaternion.identity, true, cheekColor);
            SetFeature(rightCheek, new Vector3(0.53f, -0.11f, -0.84f), new Vector3(0.22f, 0.105f, 0.028f), Quaternion.identity, true, cheekColor);

            if (expression == FaceExpression.Happy)
            {
                SetFeature(leftEye, new Vector3(-0.32f, 0.15f, -0.86f), new Vector3(0.2f, 0.25f, 0.046f), Quaternion.identity, true, eyeColor);
                SetFeature(rightEye, new Vector3(0.32f, 0.15f, -0.86f), new Vector3(0.2f, 0.25f, 0.046f), Quaternion.identity, true, eyeColor);
                SetMouthSmile(mouthColor, 0.12f, 0.17f);
                SetFeature(leftCheek, new Vector3(-0.54f, -0.08f, -0.84f), new Vector3(0.25f, 0.115f, 0.028f), Quaternion.identity, true, cheekColor);
                SetFeature(rightCheek, new Vector3(0.54f, -0.08f, -0.84f), new Vector3(0.25f, 0.115f, 0.028f), Quaternion.identity, true, cheekColor);
                return;
            }

            if (expression == FaceExpression.Sparkle)
            {
                SetFeature(leftEye, new Vector3(-0.32f, 0.17f, -0.86f), new Vector3(0.22f, 0.27f, 0.046f), Quaternion.identity, true, eyeColor);
                SetFeature(rightEye, new Vector3(0.32f, 0.17f, -0.86f), new Vector3(0.22f, 0.27f, 0.046f), Quaternion.identity, true, eyeColor);
                SetFeature(leftEyeSparkle, new Vector3(-0.39f, 0.27f, -0.9f), new Vector3(0.075f, 0.09f, 0.018f), Quaternion.identity, true, Color.white);
                SetFeature(rightEyeSparkle, new Vector3(0.25f, 0.27f, -0.9f), new Vector3(0.075f, 0.09f, 0.018f), Quaternion.identity, true, Color.white);
                SetMouthSmile(mouthColor, -0.12f, 0.18f);
                SetFeature(leftCheek, new Vector3(-0.54f, -0.08f, -0.84f), new Vector3(0.27f, 0.12f, 0.028f), Quaternion.identity, true, cheekColor);
                SetFeature(rightCheek, new Vector3(0.54f, -0.08f, -0.84f), new Vector3(0.27f, 0.12f, 0.028f), Quaternion.identity, true, cheekColor);
                return;
            }

            if (expression == FaceExpression.Sleepy)
            {
                SetFeature(leftEye, new Vector3(-0.32f, 0.1f, -0.86f), new Vector3(0.19f, 0.05f, 0.046f), Quaternion.identity, true, eyeColor);
                SetFeature(rightEye, new Vector3(0.32f, 0.1f, -0.86f), new Vector3(0.19f, 0.05f, 0.046f), Quaternion.identity, true, eyeColor);
                SetFeature(leftEyeSparkle, Vector3.zero, Vector3.one, Quaternion.identity, false, Color.white);
                SetFeature(rightEyeSparkle, Vector3.zero, Vector3.one, Quaternion.identity, false, Color.white);
                SetMouthSmile(mouthColor, 0.08f, 0.1f);
                return;
            }

            if (expression == FaceExpression.Hungry)
            {
                SetFeature(mouthLeft, Vector3.zero, Vector3.one, Quaternion.identity, false, mouthColor);
                SetFeature(mouthRight, Vector3.zero, Vector3.one, Quaternion.identity, false, mouthColor);
                SetFeature(mouthOpen, new Vector3(0f, -0.18f, -0.88f), new Vector3(0.085f, 0.095f, 0.025f), Quaternion.identity, true, mouthColor);
                return;
            }

            if (expression == FaceExpression.Surprised)
            {
                SetFeature(leftEye, new Vector3(-0.32f, 0.18f, -0.86f), new Vector3(0.22f, 0.27f, 0.046f), Quaternion.identity, true, eyeColor);
                SetFeature(rightEye, new Vector3(0.32f, 0.18f, -0.86f), new Vector3(0.22f, 0.27f, 0.046f), Quaternion.identity, true, eyeColor);
                SetFeature(mouthLeft, Vector3.zero, Vector3.one, Quaternion.identity, false, mouthColor);
                SetFeature(mouthRight, Vector3.zero, Vector3.one, Quaternion.identity, false, mouthColor);
                SetFeature(mouthOpen, new Vector3(0f, -0.18f, -0.88f), new Vector3(0.11f, 0.12f, 0.025f), Quaternion.identity, true, mouthColor);
                return;
            }

            if (expression == FaceExpression.Sad)
            {
                var sadCheek = Color.Lerp(cheekColor, Color.white, 0.35f);
                SetFeature(leftEye, new Vector3(-0.32f, 0.1f, -0.86f), new Vector3(0.15f, 0.17f, 0.046f), Quaternion.Euler(0f, 0f, -8f), true, eyeColor);
                SetFeature(rightEye, new Vector3(0.32f, 0.1f, -0.86f), new Vector3(0.15f, 0.17f, 0.046f), Quaternion.Euler(0f, 0f, 8f), true, eyeColor);
                SetFeature(leftEyeSparkle, Vector3.zero, Vector3.one, Quaternion.identity, false, Color.white);
                SetFeature(rightEyeSparkle, Vector3.zero, Vector3.one, Quaternion.identity, false, Color.white);
                SetFeature(mouthLeft, new Vector3(-0.055f, -0.22f, -0.88f), new Vector3(0.1f, 0.025f, 0.018f), Quaternion.Euler(0f, 0f, 16f), true, mouthColor);
                SetFeature(mouthRight, new Vector3(0.055f, -0.22f, -0.88f), new Vector3(0.1f, 0.025f, 0.018f), Quaternion.Euler(0f, 0f, -16f), true, mouthColor);
                SetFeature(leftCheek, new Vector3(-0.53f, -0.12f, -0.84f), new Vector3(0.18f, 0.08f, 0.028f), Quaternion.identity, true, sadCheek);
                SetFeature(rightCheek, new Vector3(0.53f, -0.12f, -0.84f), new Vector3(0.18f, 0.08f, 0.028f), Quaternion.identity, true, sadCheek);
                return;
            }

            if (expression == FaceExpression.Upset || expression == FaceExpression.Sick)
            {
                var eyeTint = expression == FaceExpression.Sick ? new Color(0.1f, 0.14f, 0.2f) : eyeColor;
                SetFeature(leftEye, new Vector3(-0.32f, 0.1f, -0.86f), new Vector3(0.15f, 0.13f, 0.046f), Quaternion.Euler(0f, 0f, -12f), true, eyeTint);
                SetFeature(rightEye, new Vector3(0.32f, 0.1f, -0.86f), new Vector3(0.15f, 0.13f, 0.046f), Quaternion.Euler(0f, 0f, 12f), true, eyeTint);
                SetFeature(mouthLeft, new Vector3(-0.055f, -0.2f, -0.88f), new Vector3(0.1f, 0.025f, 0.018f), Quaternion.Euler(0f, 0f, 14f), true, mouthColor);
                SetFeature(mouthRight, new Vector3(0.055f, -0.2f, -0.88f), new Vector3(0.1f, 0.025f, 0.018f), Quaternion.Euler(0f, 0f, -14f), true, mouthColor);
            }
        }

        private void SetMouthSmile(Color color, float y = -0.16f, float width = 0.13f)
        {
            SetFeature(mouthLeft, new Vector3(-0.055f, y, -0.88f), new Vector3(width, 0.025f, 0.018f), Quaternion.Euler(0f, 0f, -18f), true, color);
            SetFeature(mouthRight, new Vector3(0.055f, y, -0.88f), new Vector3(width, 0.025f, 0.018f), Quaternion.Euler(0f, 0f, 18f), true, color);
        }

        private void PaintBody(Color bodyColor, CharacterStage stage)
        {
            var armColor = Color.Lerp(bodyColor, new Color(0.96f, 0.58f, 0.12f), 0.18f);
            var emmentalGlow = IsEmmentalStarGlowActive(current);
            var holeColor = emmentalGlow ? new Color(1f, 0.72f, 0.18f) : new Color(0.93f, 0.58f, 0.12f);
            var speckColor = new Color(0.82f, 0.48f, 0.14f);
            var crownColor = new Color(1f, 0.72f, 0.16f);

            PaintFeature(bodyRenderer, bodyColor);
            PaintFeature(leftArmRenderer, armColor);
            PaintFeature(rightArmRenderer, armColor);
            PaintFeature(leftFootRenderer, armColor);
            PaintFeature(rightFootRenderer, armColor);
            PaintFeature(topCurlRenderer, bodyColor);
            PaintFeature(topCurlTipRenderer, bodyColor);
            PaintFeature(largeHighlightRenderer, new Color(1f, 0.98f, 0.86f));
            PaintFeature(smallHighlightRenderer, new Color(1f, 0.98f, 0.86f));
            PaintFeature(softShadowRenderer, new Color(0.26f, 0.17f, 0.08f, 0.28f));
            PaintFeature(bodyOutlineRenderer, new Color(0.42f, 0.22f, 0.08f));
            PaintFeature(leftArmOutlineRenderer, new Color(0.42f, 0.22f, 0.08f));
            PaintFeature(rightArmOutlineRenderer, new Color(0.42f, 0.22f, 0.08f));
            PaintFeature(leftFootOutlineRenderer, new Color(0.42f, 0.22f, 0.08f));
            PaintFeature(rightFootOutlineRenderer, new Color(0.42f, 0.22f, 0.08f));
            PaintFeature(topCurlOutlineRenderer, new Color(0.42f, 0.22f, 0.08f));
            PaintFeature(crownBandRenderer, crownColor);
            PaintFeature(crownPointARenderer, crownColor);
            PaintFeature(crownPointBRenderer, crownColor);
            PaintFeature(crownPointCRenderer, crownColor);

            foreach (var renderer in cheeseHoleRenderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                var color = stage == CharacterStage.Egg ? Color.Lerp(holeColor, Color.white, 0.28f) : holeColor;
                if (emmentalGlow)
                {
                    ToonMaterialUtility.Apply(renderer, ToonMaterialProfile.EnvironmentGlow, color);
                }
                else
                {
                    PaintFeature(renderer, color);
                }
            }

            foreach (var renderer in cheeseSpeckleRenderers)
            {
                PaintFeature(renderer, speckColor);
            }
        }

        private void SetFeature(Transform feature, Vector3 localPosition, Vector3 localScale, Quaternion localRotation, bool visible, Color color)
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

        private static void ConfigurePart(Transform part, Vector3 localPosition, Vector3 localScale, Quaternion localRotation, bool visible)
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

        private static void SetSpot(Transform spot, Vector3 localPosition, Vector3 localScale, Quaternion localRotation, bool visible)
        {
            ConfigurePart(spot, localPosition, localScale, localRotation, visible);
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

        private void RemoveLegacyVisualChild(string name)
        {
            if (visualRoot == null)
            {
                return;
            }

            var legacy = visualRoot.Find(name);
            if (legacy != null)
            {
                DestroySafely(legacy.gameObject);
            }
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

            ToonMaterialUtility.Apply(renderer, ToonMaterialUtility.InferProfile(renderer), color);
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

        private static CharacterStage GetStage(CheeseTamaModel tama)
        {
            if (tama == null || !tama.isHatched || tama.level < 10)
            {
                return CharacterStage.Egg;
            }

            if (tama.level >= 33)
            {
                return CharacterStage.Final;
            }

            if (tama.level >= 28)
            {
                return CharacterStage.Mature;
            }

            if (tama.level >= 20)
            {
                return CharacterStage.Grown;
            }

            if (tama.level >= 15)
            {
                return CharacterStage.Soft;
            }

            return CharacterStage.Hatchling;
        }

        private static bool IsEmmentalCheeseTama(CheeseTamaModel tama)
        {
            if (tama == null)
            {
                return false;
            }

            return ContainsIgnoreCase(tama.form, "emmental")
                || ContainsIgnoreCase(tama.evolutionId, "emmental");
        }

        private static bool IsEmmentalStarGlowActive(CheeseTamaModel tama)
        {
            if (!IsEmmentalCheeseTama(tama))
            {
                return false;
            }

            return (tama.stats != null && tama.stats.maturation >= 95)
                || tama.levelProgress >= 95;
        }

        private static bool ContainsIgnoreCase(string value, string token)
        {
            return !string.IsNullOrWhiteSpace(value)
                && value.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static Vector3 GetStageRootScale(CharacterStage stage)
        {
            return stage switch
            {
                CharacterStage.Egg => new Vector3(1.18f, 1.24f, 1.04f),
                CharacterStage.Hatchling => new Vector3(1.2f, 1.04f, 1.04f),
                CharacterStage.Soft => new Vector3(1.25f, 1.08f, 1.06f),
                CharacterStage.Grown => new Vector3(1.36f, 1.13f, 1.08f),
                CharacterStage.Mature => new Vector3(1.42f, 1.17f, 1.1f),
                CharacterStage.Final => new Vector3(1.48f, 1.2f, 1.12f),
                _ => Vector3.one
            };
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
                return FaceExpression.Sparkle;
            }

            if (tama.stats.mood < 35)
            {
                return FaceExpression.Sad;
            }

            if (tama.stats.mood > 80)
            {
                return FaceExpression.Happy;
            }

            return FaceExpression.Idle;
        }

        private static Color GetStateColor(CheeseTamaModel tama, CharacterStage stage)
        {
            var stageColor = stage == CharacterStage.Egg
                ? new Color(1f, 0.89f, 0.48f)
                : new Color(1f, 0.76f, 0.23f);

            if (tama == null || tama.stats == null)
            {
                return stageColor;
            }

            if (tama.stats.health < 35)
            {
                return Color.Lerp(stageColor, new Color(0.66f, 0.78f, 1f), 0.45f);
            }

            if (tama.stats.hunger < 25)
            {
                return Color.Lerp(stageColor, new Color(0.98f, 0.58f, 0.22f), 0.35f);
            }

            if (tama.stats.cleanliness < 35)
            {
                return Color.Lerp(stageColor, new Color(0.58f, 0.46f, 0.28f), 0.42f);
            }

            if (tama.stats.sleepiness > 75)
            {
                return Color.Lerp(stageColor, new Color(0.66f, 0.64f, 1f), 0.34f);
            }

            if (tama.stats.mood > 80)
            {
                return Color.Lerp(stageColor, new Color(1f, 0.92f, 0.36f), 0.32f);
            }

            return stageColor;
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

        private enum CharacterStage
        {
            Egg,
            Hatchling,
            Soft,
            Grown,
            Mature,
            Final
        }

        private enum FaceExpression
        {
            Idle,
            Happy,
            Hungry,
            Sleepy,
            Upset,
            Surprised,
            Sad,
            Sick,
            Sparkle
        }
    }
}
