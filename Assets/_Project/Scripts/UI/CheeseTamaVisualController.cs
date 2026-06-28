using CheeseTama.Gameplay;
using UnityEngine;

namespace CheeseTama.UI
{
    public sealed class CheeseTamaVisualController : MonoBehaviour
    {
        [SerializeField] private Renderer targetRenderer;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private const float CareReactionDuration = 0.55f;
        private const float CareReactionHopHeight = 0.32f;
        private const float CareReactionPunch = 0.08f;
        private const float CareReactionFlash = 0.12f;
        private const float HatchReactionDuration = 1.15f;
        private const float HatchReactionHopHeight = 0.72f;
        private const float HatchReactionPunch = 0.2f;
        private const float HatchReactionFlash = 0.38f;
        private const float EventReactionDuration = 0.95f;
        private const float EventReactionHopHeight = 0.24f;
        private const float EventReactionPunch = 0.12f;
        private const float EventReactionFlash = 0.55f;
        private const float EventCueDuration = 1.2f;

        private readonly Vector3 eggScale = new Vector3(1.25f, 1.55f, 1.25f);
        private readonly Vector3 hatchedScale = new Vector3(1.45f, 1.2f, 1.45f);
        private MaterialPropertyBlock propertyBlock;
        private Transform faceRoot;
        private Transform leftEye;
        private Transform rightEye;
        private Transform smile;
        private Transform leftCheek;
        private Transform rightCheek;
        private Transform cheeseHoleA;
        private Transform cheeseHoleB;
        private Transform cheeseHoleC;
        private Transform eventCue;
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
        private Color reactionTintColor = Color.white;
        private bool isReacting;
        private bool eventCueVisible;

        private void Awake()
        {
            EnsurePropertyBlock();
            EnsureRenderer();
            EnsureHatchedFeatures();
            CaptureRestingPosition();
        }

        private void LateUpdate()
        {
            if (current == null)
            {
                return;
            }

            var baseScale = current.isHatched ? hatchedScale : eggScale;
            var baseColor = GetStateColor(current);
            UpdateHatchedFeatureVisibility();
            UpdateHatchedFeatureAppearance();
            UpdateEventCue();

            if (!isReacting)
            {
                transform.localPosition = Vector3.Lerp(transform.localPosition, restingLocalPosition, Time.unscaledDeltaTime * 12f);
                transform.localScale = Vector3.Lerp(transform.localScale, baseScale, Time.unscaledDeltaTime * 12f);
                SetColor(baseColor);
                return;
            }

            var normalized = Mathf.Clamp01((Time.realtimeSinceStartup - reactionStartedAt) / reactionDuration);
            var arc = Mathf.Sin(normalized * Mathf.PI);
            var settle = 1f - Mathf.SmoothStep(0f, 1f, normalized);
            var hop = arc * reactionHopHeight;
            var side = Mathf.Sin(normalized * Mathf.PI * 2f) * 0.035f * settle;
            var punch = arc * reactionPunch;
            var wobble = Mathf.Sin(normalized * Mathf.PI * 4f) * 0.025f * settle;

            transform.localPosition = restingLocalPosition + Vector3.up * hop + Vector3.right * side;
            transform.localScale = new Vector3(
                baseScale.x * (1f + punch + wobble),
                baseScale.y * (1f + punch * 0.65f - wobble),
                baseScale.z * (1f + punch + wobble));
            SetColor(Color.Lerp(baseColor, reactionTintColor, arc * reactionFlash));

            if (normalized >= 1f)
            {
                isReacting = false;
                reactionTintColor = Color.white;
                transform.localPosition = restingLocalPosition;
                transform.localScale = baseScale;
                SetColor(baseColor);
            }
        }

        public void Bind(CheeseTamaModel tama)
        {
            EnsureRenderer();
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

            SetColor(GetStateColor(current));
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
            var squash = celebrate ? 0.94f : 0.98f;
            var stretch = celebrate ? 1.1f : 1.04f;
            transform.localScale = new Vector3(baseScale.x * stretch, baseScale.y * squash, baseScale.z * stretch);
            SetColor(Color.Lerp(GetStateColor(current), Color.white, celebrate ? 0.2f : 0.08f));
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
            transform.localScale = new Vector3(baseScale.x * 1.05f, baseScale.y * 0.97f, baseScale.z * 1.05f);
            SetColor(Color.Lerp(GetStateColor(current), reactionTintColor, 0.22f));
            ShowEventCue(eventId, reactionTintColor);
        }

        private void EnsureRenderer()
        {
            DisableSpriteRendererIfPresent();

            if (targetRenderer == null)
            {
                targetRenderer = GetComponent<Renderer>();
            }

            if (targetRenderer != null)
            {
                targetRenderer.enabled = true;
            }
        }

        private void EnsureHatchedFeatures()
        {
            if (faceRoot != null)
            {
                return;
            }

            var root = new GameObject("Hatched Face Root");
            root.transform.SetParent(transform, false);
            faceRoot = root.transform;

            leftEye = CreateFeature("Left Eye", PrimitiveType.Sphere, new Vector3(-0.3f, 0.2f, -0.58f), new Vector3(0.14f, 0.14f, 0.04f), new Color(0.17f, 0.11f, 0.08f));
            rightEye = CreateFeature("Right Eye", PrimitiveType.Sphere, new Vector3(0.3f, 0.2f, -0.58f), new Vector3(0.14f, 0.14f, 0.04f), new Color(0.17f, 0.11f, 0.08f));
            smile = CreateFeature("Smile", PrimitiveType.Cube, new Vector3(0f, -0.16f, -0.6f), new Vector3(0.34f, 0.055f, 0.035f), new Color(0.24f, 0.13f, 0.08f));
            leftCheek = CreateFeature("Left Cheek", PrimitiveType.Sphere, new Vector3(-0.46f, -0.08f, -0.56f), new Vector3(0.18f, 0.1f, 0.035f), new Color(1f, 0.48f, 0.32f));
            rightCheek = CreateFeature("Right Cheek", PrimitiveType.Sphere, new Vector3(0.46f, -0.08f, -0.56f), new Vector3(0.18f, 0.1f, 0.035f), new Color(1f, 0.48f, 0.32f));
            cheeseHoleA = CreateFeature("Cheese Hole A", PrimitiveType.Sphere, new Vector3(-0.2f, 0.48f, -0.57f), new Vector3(0.11f, 0.08f, 0.025f), new Color(0.82f, 0.5f, 0.12f));
            cheeseHoleB = CreateFeature("Cheese Hole B", PrimitiveType.Sphere, new Vector3(0.42f, 0.46f, -0.55f), new Vector3(0.08f, 0.06f, 0.025f), new Color(0.82f, 0.5f, 0.12f));
            cheeseHoleC = CreateFeature("Cheese Hole C", PrimitiveType.Sphere, new Vector3(0.18f, -0.42f, -0.56f), new Vector3(0.1f, 0.075f, 0.025f), new Color(0.82f, 0.5f, 0.12f));
            UpdateHatchedFeatureVisibility();
        }

        private Transform CreateFeature(string name, PrimitiveType primitive, Vector3 localPosition, Vector3 localScale, Color color)
        {
            var feature = GameObject.CreatePrimitive(primitive);
            feature.name = name;
            feature.transform.SetParent(faceRoot, false);
            feature.transform.localPosition = localPosition;
            feature.transform.localRotation = Quaternion.identity;
            feature.transform.localScale = localScale;

            var collider = feature.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            var renderer = feature.GetComponent<Renderer>();
            PaintFeature(renderer, color);
            return feature.transform;
        }

        private void UpdateHatchedFeatureVisibility()
        {
            if (faceRoot == null)
            {
                return;
            }

            faceRoot.gameObject.SetActive(current != null && current.isHatched);
        }

        private void UpdateHatchedFeatureAppearance()
        {
            if (current == null || !current.isHatched || faceRoot == null)
            {
                return;
            }

            var expression = GetExpression(current);
            ApplyFaceLayout(expression);
        }

        private void ApplyFaceLayout(FaceExpression expression)
        {
            var eyeColor = new Color(0.17f, 0.11f, 0.08f);
            var mouthColor = new Color(0.24f, 0.13f, 0.08f);
            var cheekColor = new Color(1f, 0.48f, 0.32f);
            var holeColor = new Color(0.82f, 0.5f, 0.12f);

            SetFeature(leftEye, new Vector3(-0.3f, 0.2f, -0.58f), new Vector3(0.14f, 0.14f, 0.04f), Quaternion.identity, true, eyeColor);
            SetFeature(rightEye, new Vector3(0.3f, 0.2f, -0.58f), new Vector3(0.14f, 0.14f, 0.04f), Quaternion.identity, true, eyeColor);
            SetFeature(smile, new Vector3(0f, -0.16f, -0.6f), new Vector3(0.34f, 0.055f, 0.035f), Quaternion.identity, true, mouthColor);
            SetFeature(leftCheek, new Vector3(-0.46f, -0.08f, -0.56f), new Vector3(0.18f, 0.1f, 0.035f), Quaternion.identity, true, cheekColor);
            SetFeature(rightCheek, new Vector3(0.46f, -0.08f, -0.56f), new Vector3(0.18f, 0.1f, 0.035f), Quaternion.identity, true, cheekColor);
            SetFeature(cheeseHoleA, new Vector3(-0.2f, 0.48f, -0.57f), new Vector3(0.11f, 0.08f, 0.025f), Quaternion.identity, true, holeColor);
            SetFeature(cheeseHoleB, new Vector3(0.42f, 0.46f, -0.55f), new Vector3(0.08f, 0.06f, 0.025f), Quaternion.identity, true, holeColor);
            SetFeature(cheeseHoleC, new Vector3(0.18f, -0.42f, -0.56f), new Vector3(0.1f, 0.075f, 0.025f), Quaternion.identity, true, holeColor);

            if (expression == FaceExpression.Cheerful)
            {
                SetFeature(leftEye, new Vector3(-0.3f, 0.24f, -0.58f), new Vector3(0.16f, 0.16f, 0.04f), Quaternion.identity, true, eyeColor);
                SetFeature(rightEye, new Vector3(0.3f, 0.24f, -0.58f), new Vector3(0.16f, 0.16f, 0.04f), Quaternion.identity, true, eyeColor);
                SetFeature(smile, new Vector3(0f, -0.13f, -0.6f), new Vector3(0.46f, 0.07f, 0.035f), Quaternion.identity, true, mouthColor);
                SetFeature(leftCheek, new Vector3(-0.48f, -0.06f, -0.56f), new Vector3(0.21f, 0.12f, 0.035f), Quaternion.identity, true, cheekColor);
                SetFeature(rightCheek, new Vector3(0.48f, -0.06f, -0.56f), new Vector3(0.21f, 0.12f, 0.035f), Quaternion.identity, true, cheekColor);
                return;
            }

            if (expression == FaceExpression.Sleepy)
            {
                SetFeature(leftEye, new Vector3(-0.3f, 0.15f, -0.58f), new Vector3(0.17f, 0.045f, 0.04f), Quaternion.identity, true, eyeColor);
                SetFeature(rightEye, new Vector3(0.3f, 0.15f, -0.58f), new Vector3(0.17f, 0.045f, 0.04f), Quaternion.identity, true, eyeColor);
                SetFeature(smile, new Vector3(0f, -0.18f, -0.6f), new Vector3(0.26f, 0.04f, 0.035f), Quaternion.identity, true, mouthColor);
                SetFeature(leftCheek, Vector3.zero, Vector3.one, Quaternion.identity, false, cheekColor);
                SetFeature(rightCheek, Vector3.zero, Vector3.one, Quaternion.identity, false, cheekColor);
                return;
            }

            if (expression == FaceExpression.Unwell)
            {
                var paleEye = new Color(0.1f, 0.14f, 0.2f);
                SetFeature(leftEye, new Vector3(-0.3f, 0.16f, -0.58f), new Vector3(0.13f, 0.09f, 0.04f), Quaternion.Euler(0f, 0f, -12f), true, paleEye);
                SetFeature(rightEye, new Vector3(0.3f, 0.16f, -0.58f), new Vector3(0.13f, 0.09f, 0.04f), Quaternion.Euler(0f, 0f, 12f), true, paleEye);
                SetFeature(smile, new Vector3(0f, -0.23f, -0.6f), new Vector3(0.25f, 0.04f, 0.035f), Quaternion.Euler(0f, 0f, 180f), true, mouthColor);
                SetFeature(leftCheek, Vector3.zero, Vector3.one, Quaternion.identity, false, cheekColor);
                SetFeature(rightCheek, Vector3.zero, Vector3.one, Quaternion.identity, false, cheekColor);
                return;
            }

            if (expression == FaceExpression.Hungry)
            {
                SetFeature(leftEye, new Vector3(-0.3f, 0.2f, -0.58f), new Vector3(0.12f, 0.16f, 0.04f), Quaternion.identity, true, eyeColor);
                SetFeature(rightEye, new Vector3(0.3f, 0.2f, -0.58f), new Vector3(0.12f, 0.16f, 0.04f), Quaternion.identity, true, eyeColor);
                SetFeature(smile, new Vector3(0f, -0.2f, -0.6f), new Vector3(0.2f, 0.08f, 0.035f), Quaternion.identity, true, mouthColor);
                SetFeature(leftCheek, new Vector3(-0.45f, -0.08f, -0.56f), new Vector3(0.13f, 0.07f, 0.035f), Quaternion.identity, true, new Color(1f, 0.6f, 0.38f));
                SetFeature(rightCheek, new Vector3(0.45f, -0.08f, -0.56f), new Vector3(0.13f, 0.07f, 0.035f), Quaternion.identity, true, new Color(1f, 0.6f, 0.38f));
                return;
            }

            if (expression == FaceExpression.Messy)
            {
                var mutedHoleColor = new Color(0.55f, 0.4f, 0.2f);
                SetFeature(smile, new Vector3(0f, -0.18f, -0.6f), new Vector3(0.26f, 0.045f, 0.035f), Quaternion.Euler(0f, 0f, -6f), true, mouthColor);
                SetFeature(cheeseHoleA, new Vector3(-0.25f, 0.49f, -0.57f), new Vector3(0.13f, 0.09f, 0.025f), Quaternion.identity, true, mutedHoleColor);
                SetFeature(cheeseHoleB, new Vector3(0.38f, 0.4f, -0.55f), new Vector3(0.1f, 0.07f, 0.025f), Quaternion.identity, true, mutedHoleColor);
                SetFeature(cheeseHoleC, new Vector3(0.22f, -0.45f, -0.56f), new Vector3(0.13f, 0.09f, 0.025f), Quaternion.identity, true, mutedHoleColor);
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

            var cue = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cue.name = "Event Cue";
            cue.transform.SetParent(transform, false);
            cue.transform.localPosition = new Vector3(0f, 1.1f, -0.08f);
            cue.transform.localScale = new Vector3(0.18f, 0.18f, 0.06f);

            var collider = cue.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            eventCue = cue.transform;
            eventCueRenderer = cue.GetComponent<Renderer>();
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
            eventCue.localPosition = new Vector3(0f, 1.05f, -0.08f);
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
            var drift = normalized * 0.18f;
            var pulse = 1f + arc * 0.32f;
            eventCue.localPosition = new Vector3(0f, 1.05f + arc * 0.16f + drift, -0.08f);
            eventCue.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(normalized * Mathf.PI * 2f) * 14f);
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

        private void SetColor(Color color)
        {
            EnsurePropertyBlock();

            if (targetRenderer == null)
            {
                EnsureRenderer();
            }

            if (targetRenderer == null)
            {
                return;
            }

            targetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BaseColorId, color);
            propertyBlock.SetColor(ColorId, color);
            targetRenderer.SetPropertyBlock(propertyBlock);
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
                return FaceExpression.Default;
            }

            if (tama.stats.health < 35)
            {
                return FaceExpression.Unwell;
            }

            if (tama.stats.hunger < 25)
            {
                return FaceExpression.Hungry;
            }

            if (tama.stats.cleanliness < 35)
            {
                return FaceExpression.Messy;
            }

            if (tama.stats.sleepiness > 75)
            {
                return FaceExpression.Sleepy;
            }

            if (tama.stats.mood > 80)
            {
                return FaceExpression.Cheerful;
            }

            return FaceExpression.Default;
        }

        private static Color GetStateColor(CheeseTamaModel tama)
        {
            if (tama == null || tama.stats == null)
            {
                return new Color(1f, 0.84f, 0.28f);
            }

            if (tama.stats.health < 35)
            {
                return new Color(0.75f, 0.82f, 0.95f);
            }

            if (tama.stats.hunger < 25)
            {
                return new Color(0.96f, 0.72f, 0.44f);
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
                return new Color(1f, 0.9f, 0.38f);
            }

            if (tama.isHatched)
            {
                return new Color(1f, 0.74f, 0.28f);
            }

            return new Color(1f, 0.84f, 0.28f);
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
                _ => new Vector3(0.18f, 0.18f, 0.06f)
            };
        }

        private enum FaceExpression
        {
            Default,
            Cheerful,
            Sleepy,
            Hungry,
            Messy,
            Unwell
        }
    }
}
