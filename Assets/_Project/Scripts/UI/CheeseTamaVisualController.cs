using CheeseTama.Gameplay;
using UnityEngine;

namespace CheeseTama.UI
{
    public sealed class CheeseTamaVisualController : MonoBehaviour
    {
        [SerializeField] private Renderer targetRenderer;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private const float ReactionDuration = 0.45f;
        private const float ReactionHopHeight = 0.42f;

        private readonly Vector3 eggScale = new Vector3(1.25f, 1.55f, 1.25f);
        private readonly Vector3 hatchedScale = new Vector3(1.45f, 1.2f, 1.45f);
        private readonly MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
        private CheeseTamaModel current;
        private Vector3 restingLocalPosition;
        private bool hasRestingLocalPosition;
        private float reactionTimer;

        private void Awake()
        {
            EnsureRenderer();
            CaptureRestingPosition();
        }

        private void Update()
        {
            if (current == null)
            {
                return;
            }

            reactionTimer = Mathf.Max(0f, reactionTimer - Time.deltaTime);
            var baseScale = current.isHatched ? hatchedScale : eggScale;
            var baseColor = GetStateColor(current);

            if (reactionTimer <= 0f)
            {
                transform.localPosition = Vector3.Lerp(transform.localPosition, restingLocalPosition, Time.deltaTime * 14f);
                transform.localScale = Vector3.Lerp(transform.localScale, baseScale, Time.deltaTime * 14f);
                SetColor(baseColor);
                return;
            }

            var normalized = 1f - reactionTimer / ReactionDuration;
            var hop = Mathf.Sin(normalized * Mathf.PI) * ReactionHopHeight;
            var punch = Mathf.Sin(normalized * Mathf.PI) * 0.18f;
            var wobble = Mathf.Sin(normalized * Mathf.PI * 4f) * 0.04f;
            var targetScale = new Vector3(
                baseScale.x * (1f + punch + wobble),
                baseScale.y * (1f + punch * 0.55f - wobble),
                baseScale.z * (1f + punch + wobble));

            transform.localPosition = Vector3.Lerp(transform.localPosition, restingLocalPosition + Vector3.up * hop, Time.deltaTime * 22f);
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * 22f);
            SetColor(Color.Lerp(baseColor, Color.white, Mathf.Sin(normalized * Mathf.PI) * 0.35f));
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

            transform.localScale = current.isHatched ? hatchedScale : eggScale;
            transform.localPosition = restingLocalPosition;
            SetColor(GetStateColor(current));
        }

        public void React()
        {
            CaptureRestingPosition();
            reactionTimer = ReactionDuration;
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
