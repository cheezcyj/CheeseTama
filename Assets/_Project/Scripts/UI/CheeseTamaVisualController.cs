using CheeseTama.Gameplay;
using UnityEngine;

namespace CheeseTama.UI
{
    public sealed class CheeseTamaVisualController : MonoBehaviour
    {
        [SerializeField] private Renderer targetRenderer;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private readonly Vector3 eggScale = new Vector3(1.25f, 1.55f, 1.25f);
        private readonly Vector3 hatchedScale = new Vector3(1.45f, 1.2f, 1.45f);
        private readonly MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
        private CheeseTamaModel current;
        private float pulseTimer;

        private void Awake()
        {
            EnsureRenderer();
        }

        private void Update()
        {
            if (current == null)
            {
                return;
            }

            pulseTimer = Mathf.Max(0, pulseTimer - Time.deltaTime);
            var baseScale = current.isHatched ? hatchedScale : eggScale;
            var pulse = pulseTimer > 0 ? Mathf.Sin(pulseTimer * 18f) * 0.08f : 0f;
            transform.localScale = Vector3.Lerp(transform.localScale, baseScale * (1f + pulse), Time.deltaTime * 12f);
            SetColor(GetStateColor(current));
        }

        public void Bind(CheeseTamaModel tama)
        {
            EnsureRenderer();
            current = tama;
            if (current == null)
            {
                return;
            }

            transform.localScale = current.isHatched ? hatchedScale : eggScale;
            SetColor(GetStateColor(current));
        }

        public void React()
        {
            pulseTimer = 0.35f;
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

