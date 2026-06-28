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

        private readonly Vector3 eggScale = new Vector3(1.25f, 1.55f, 1.25f);
        private readonly Vector3 hatchedScale = new Vector3(1.45f, 1.2f, 1.45f);
        private MaterialPropertyBlock propertyBlock;
        private CheeseTamaModel current;
        private Vector3 restingLocalPosition;
        private bool hasRestingLocalPosition;
        private float reactionStartedAt;
        private float reactionDuration = CareReactionDuration;
        private float reactionHopHeight = CareReactionHopHeight;
        private float reactionPunch = CareReactionPunch;
        private float reactionFlash = CareReactionFlash;
        private bool isReacting;

        private void Awake()
        {
            EnsurePropertyBlock();
            EnsureRenderer();
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
            SetColor(Color.Lerp(baseColor, Color.white, arc * reactionFlash));

            if (normalized >= 1f)
            {
                isReacting = false;
                transform.localPosition = restingLocalPosition;
                transform.localScale = baseScale;
                SetColor(baseColor);
            }
        }

        public void Bind(CheeseTamaModel tama)
        {
            EnsureRenderer();
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
        }

        public void React(bool celebrate = false)
        {
            CaptureRestingPosition();
            reactionStartedAt = Time.realtimeSinceStartup;
            reactionDuration = celebrate ? HatchReactionDuration : CareReactionDuration;
            reactionHopHeight = celebrate ? HatchReactionHopHeight : CareReactionHopHeight;
            reactionPunch = celebrate ? HatchReactionPunch : CareReactionPunch;
            reactionFlash = celebrate ? HatchReactionFlash : CareReactionFlash;
            isReacting = true;

            var baseScale = current != null && current.isHatched ? hatchedScale : eggScale;
            transform.localPosition = restingLocalPosition;
            var squash = celebrate ? 0.94f : 0.98f;
            var stretch = celebrate ? 1.1f : 1.04f;
            transform.localScale = new Vector3(baseScale.x * stretch, baseScale.y * squash, baseScale.z * stretch);
            SetColor(Color.Lerp(GetStateColor(current), Color.white, celebrate ? 0.2f : 0.08f));
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

        private void EnsurePropertyBlock()
        {
            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }
        }

        private static Color GetStateColor(CheeseTamaModel tama)
        {
            if (tama == null || tama.stats == null)
            {
                return new Color(1f, 0.84f, 0.28f);
            }

            if (tama.isHatched)
            {
                return new Color(1f, 0.74f, 0.28f);
            }

            if (tama.stats.health < 35)
            {
                return new Color(0.75f, 0.82f, 0.95f);
            }

            if (tama.stats.mood > 80)
            {
                return new Color(1f, 0.9f, 0.38f);
            }

            if (tama.stats.cleanliness < 35)
            {
                return new Color(0.72f, 0.64f, 0.48f);
            }

            return new Color(1f, 0.84f, 0.28f);
        }
    }
}
