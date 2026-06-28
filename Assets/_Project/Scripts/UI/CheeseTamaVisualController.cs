using CheeseTama.Gameplay;
using UnityEngine;

namespace CheeseTama.UI
{
    public sealed class CheeseTamaVisualController : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;

        private static Sprite circleSprite;
        private readonly Vector3 eggScale = new Vector3(1.25f, 1.55f, 1f);
        private readonly Vector3 hatchedScale = new Vector3(1.45f, 1.2f, 1f);
        private CheeseTamaModel current;
        private float pulseTimer;

        private void Awake()
        {
            EnsureSpriteRenderer();
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
            EnsureSpriteRenderer();
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

        private void EnsureSpriteRenderer()
        {
            DisableMeshRendering();

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            spriteRenderer.sprite = GetCircleSprite();
            spriteRenderer.sortingOrder = 0;
        }

        private void DisableMeshRendering()
        {
            var meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.enabled = false;
            }
        }

        private void SetColor(Color color)
        {
            EnsureSpriteRenderer();
            spriteRenderer.color = color;
        }

        private static Sprite GetCircleSprite()
        {
            if (circleSprite != null)
            {
                return circleSprite;
            }

            const int size = 96;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Runtime CheeseTama Circle",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            var radius = size * 0.46f;
            var softEdge = size * 0.04f;
            var pixels = new Color32[size * size];

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var distance = Vector2.Distance(new Vector2(x, y), center);
                    var alpha = Mathf.Clamp01((radius - distance) / softEdge);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            circleSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            circleSprite.name = "Runtime CheeseTama Circle";
            return circleSprite;
        }

        private static Color GetStateColor(CheeseTamaModel tama)
        {
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

