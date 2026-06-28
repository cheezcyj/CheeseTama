using CheeseTama.Gameplay;
using UnityEngine;

namespace CheeseTama.UI
{
    public sealed class CheeseTamaVisualController : MonoBehaviour
    {
        [SerializeField] private Renderer targetRenderer;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private const float ReactionDuration = 1.25f;
        private const float ReactionHopHeight = 1.6f;

        private readonly Vector3 eggScale = new Vector3(1.25f, 1.55f, 1.25f);
        private readonly Vector3 hatchedScale = new Vector3(1.45f, 1.2f, 1.45f);
        private readonly MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
        private CheeseTamaModel current;
        private Vector3 restingLocalPosition;
        private Vector3 restingWorldPosition;
        private bool hasRestingLocalPosition;
        private float reactionStartedAt;
        private bool isReacting;

        private void Awake()
        {
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

            var normalized = Mathf.Clamp01((Time.realtimeSinceStartup - reactionStartedAt) / ReactionDuration);
            var arc = Mathf.Sin(normalized * Mathf.PI);
            var hop = arc * ReactionHopHeight;
            var side = Mathf.Sin(normalized * Mathf.PI * 2f) * 0.22f;
            var punch = arc * 0.35f;
            var wobble = Mathf.Sin(normalized * Mathf.PI * 6f) * 0.1f;

            transform.position = restingWorldPosition + Vector3.up * hop + Vector3.right * side;
            transform.localScale = new Vector3(
                baseScale.x * (1f + punch + wobble),
                baseScale.y * (1f + punch * 0.65f - wobble),
                baseScale.z * (1f + punch + wobble));
            SetColor(Color.Lerp(baseColor, Color.white, arc * 0.5f));

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

        public void React()
        {
            CaptureRestingPosition();
            reactionStartedAt = Time.realtimeSinceStartup;
            isReacting = true;

            var baseScale = current != null && current.isHatched ? hatchedScale : eggScale;
            transform.position = restingWorldPosition + Vector3.up * ReactionHopHeight;
            transform.localScale = baseScale * 1.45f;
            SetColor(Color.white);
            Debug.Log($"CheeseTama visual reaction started on {gameObject.name}.");
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
            restingWorldPosition = transform.position;
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
