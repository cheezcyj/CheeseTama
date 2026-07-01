using CheeseTama.Gameplay;
using UnityEngine;

namespace CheeseTama.UI
{
    // Displays the AI-generated CheeseTama 3D mesh and provides lightweight
    // squash/hop reactions. The heavy procedural primitive rig was replaced by
    // the generated model (Assets/Characters/CheeseTama/CheeseTama_Model.prefab).
    public sealed class CheeseTamaVisualController : MonoBehaviour
    {
        [SerializeField] private GameObject modelPrefab;
        [SerializeField] private Transform modelInstance;
        [SerializeField] private float modelYawDegrees = 180f;
        [SerializeField] private float modelScale = 1.7f;

        private const float CareReactionDuration = 0.68f;
        private const float CareReactionHopHeight = 0.16f;
        private const float CareReactionPunch = 0.09f;
        private const float HatchReactionDuration = 1.15f;
        private const float HatchReactionHopHeight = 0.42f;
        private const float HatchReactionPunch = 0.2f;
        private const float EventReactionDuration = 0.92f;
        private const float EventReactionHopHeight = 0.22f;
        private const float EventReactionPunch = 0.12f;

        private CheeseTamaModel current;
        private Vector3 restingLocalPosition;
        private bool hasRestingLocalPosition;
        private float idleSeed;

        private float reactionStartedAt;
        private float reactionDuration = CareReactionDuration;
        private float reactionHopHeight = CareReactionHopHeight;
        private float reactionPunch = CareReactionPunch;
        private bool isReacting;

        private Renderer[] modelRenderers;
        private MaterialPropertyBlock propertyBlock;
        private Color flashColor = Color.white;
        private float flashStrength;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private void Awake()
        {
            idleSeed = Random.Range(0f, 100f);
            EnsureModel();
            CaptureRestingPosition();
        }

        private void EnsureModel()
        {
            if (modelInstance == null)
            {
                var existing = transform.Find("GeneratedModel");
                if (existing != null)
                {
                    modelInstance = existing;
                }
            }

            if (modelInstance == null && modelPrefab != null)
            {
                var go = Instantiate(modelPrefab, transform);
                go.name = "GeneratedModel";
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.Euler(0f, modelYawDegrees, 0f);
                go.transform.localScale = Vector3.one * modelScale;
                modelInstance = go.transform;
            }

            if (modelInstance != null)
            {
                modelInstance.localRotation = Quaternion.Euler(0f, modelYawDegrees, 0f);
                modelInstance.localScale = Vector3.one * modelScale;
                modelRenderers = modelInstance.GetComponentsInChildren<Renderer>(true);
                propertyBlock ??= new MaterialPropertyBlock();
            }

            DisableLegacyRootRenderer();
        }

        private void LateUpdate()
        {
            if (modelInstance == null)
            {
                EnsureModel();
            }

            var baseScale = GetStageRootScale(GetStage(current));

            if (!isReacting)
            {
                var time = Time.realtimeSinceStartup + idleSeed;
                var breath = Mathf.Sin(time * 1.7f) * 0.02f;
                var targetScale = new Vector3(
                    baseScale.x * (1f - breath * 0.4f),
                    baseScale.y * (1f + breath),
                    baseScale.z * (1f - breath * 0.4f));

                transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * 8f);
                transform.localPosition = Vector3.Lerp(
                    transform.localPosition,
                    restingLocalPosition + Vector3.up * Mathf.Abs(breath) * 0.06f,
                    Time.unscaledDeltaTime * 8f);

                if (modelInstance != null)
                {
                    var sway = Mathf.Sin(time * 0.9f) * 1.4f;
                    modelInstance.localRotation = Quaternion.Euler(0f, modelYawDegrees + sway, Mathf.Sin(time * 1.3f) * 1.2f);
                }

                ApplyFlash(0f);
                return;
            }

            var normalized = Mathf.Clamp01((Time.realtimeSinceStartup - reactionStartedAt) / reactionDuration);
            var arc = Mathf.Sin(normalized * Mathf.PI);
            var settle = 1f - Mathf.SmoothStep(0f, 1f, normalized);
            var hop = arc * reactionHopHeight;
            var side = Mathf.Sin(normalized * Mathf.PI * 2f) * 0.03f * settle;
            var punch = arc * reactionPunch;

            transform.localPosition = restingLocalPosition + Vector3.up * hop + Vector3.right * side;
            transform.localScale = new Vector3(
                baseScale.x * (1f + punch * 0.6f),
                baseScale.y * (1f - punch),
                baseScale.z * (1f + punch * 0.6f));

            if (modelInstance != null)
            {
                modelInstance.localRotation = Quaternion.Euler(0f, modelYawDegrees, Mathf.Sin(normalized * Mathf.PI * 2f) * 3.5f * settle);
            }

            ApplyFlash(arc * flashStrength);

            if (normalized >= 1f)
            {
                isReacting = false;
                flashStrength = 0f;
                transform.localPosition = restingLocalPosition;
                transform.localScale = baseScale;
                ApplyFlash(0f);
            }
        }

        public void Bind(CheeseTamaModel tama)
        {
            EnsureModel();
            CaptureRestingPosition();
            current = tama;
            if (current == null)
            {
                return;
            }

            if (!isReacting)
            {
                transform.localScale = GetStageRootScale(GetStage(current));
                transform.localPosition = restingLocalPosition;
            }
        }

        public void React(bool celebrate = false)
        {
            EnsureModel();
            CaptureRestingPosition();
            reactionStartedAt = Time.realtimeSinceStartup;
            reactionDuration = celebrate ? HatchReactionDuration : CareReactionDuration;
            reactionHopHeight = celebrate ? HatchReactionHopHeight : CareReactionHopHeight;
            reactionPunch = celebrate ? HatchReactionPunch : CareReactionPunch;
            flashColor = celebrate ? new Color(1f, 0.95f, 0.6f) : new Color(1f, 1f, 0.9f);
            flashStrength = celebrate ? 0.45f : 0.18f;
            isReacting = true;
        }

        public void ReactEvent(string eventId)
        {
            EnsureModel();
            CaptureRestingPosition();
            reactionStartedAt = Time.realtimeSinceStartup;
            reactionDuration = EventReactionDuration;
            reactionHopHeight = EventReactionHopHeight;
            reactionPunch = EventReactionPunch;
            flashColor = GetEventColor(eventId);
            flashStrength = 0.4f;
            isReacting = true;
        }

        private void ApplyFlash(float strength)
        {
            if (modelRenderers == null || modelRenderers.Length == 0)
            {
                return;
            }

            propertyBlock ??= new MaterialPropertyBlock();
            var tint = Color.Lerp(Color.white, flashColor, Mathf.Clamp01(strength));
            foreach (var r in modelRenderers)
            {
                if (r == null)
                {
                    continue;
                }

                r.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(BaseColorId, tint);
                propertyBlock.SetColor(ColorId, tint);
                r.SetPropertyBlock(propertyBlock);
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

        private void DisableLegacyRootRenderer()
        {
            var spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = false;
            }
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

        private static Vector3 GetStageRootScale(CharacterStage stage)
        {
            return stage switch
            {
                CharacterStage.Egg => new Vector3(1.0f, 1.0f, 1.0f),
                CharacterStage.Hatchling => new Vector3(1.05f, 1.05f, 1.05f),
                CharacterStage.Soft => new Vector3(1.12f, 1.12f, 1.12f),
                CharacterStage.Grown => new Vector3(1.2f, 1.2f, 1.2f),
                CharacterStage.Mature => new Vector3(1.28f, 1.28f, 1.28f),
                CharacterStage.Final => new Vector3(1.36f, 1.36f, 1.36f),
                _ => Vector3.one
            };
        }

        private static Color GetEventColor(string eventId)
        {
            return eventId switch
            {
                "happy_wiggle" => new Color(1f, 0.92f, 0.28f),
                "small_fever" => new Color(0.5f, 0.72f, 1f),
                "hungry_peep" => new Color(1f, 0.55f, 0.22f),
                "dusty_corner" => new Color(0.72f, 0.6f, 0.4f),
                "sleepy_yawn" => new Color(0.62f, 0.58f, 1f),
                "milk_drop_catch" => new Color(0.74f, 0.92f, 1f),
                _ => new Color(0.72f, 0.98f, 0.86f)
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
    }
}