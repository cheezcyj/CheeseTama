using CheeseTama.Data;
using CheeseTama.Gameplay;
using CheeseTama.Gameplay.Growth;
using CheeseTama.Utilities;
using UnityEngine;

namespace CheeseTama.UI
{
    public enum CheeseTamaVisualAction
    {
        Neutral,
        FeedMilk,
        FeedSnack,
        Play,
        Clean,
        Rest,
        Cook,
        LevelUp,
        Hatch,
        Event
    }

    // Displays the stage-specific CheeseTama 3D mesh and provides lightweight
    // squash/hop reactions. Growth visuals are supplied by CheeseTamaGrowthVisualSet.
    public sealed class CheeseTamaVisualController : MonoBehaviour
    {
        [SerializeField] private GameObject modelPrefab;
        [SerializeField] private Transform modelInstance;
        [SerializeField] private float modelYawDegrees = 180f;
        [SerializeField] private float modelScale = 1.7f;
        [SerializeField] private CheeseTamaGrowthVisualSet growthVisualSet;

        private const float CareReactionDuration = 0.68f;
        private const float CareReactionHopHeight = 0.16f;
        private const float CareReactionPunch = 0.09f;
        private const float HatchReactionDuration = 1.15f;
        private const float HatchReactionHopHeight = 0.42f;
        private const float HatchReactionPunch = 0.2f;
        private const float EventReactionDuration = 0.92f;
        private const float EventReactionHopHeight = 0.22f;
        private const float EventReactionPunch = 0.12f;
        private const float PropZ = -0.92f;
        private const float ExpressionHoldPadding = 0.35f;

        private enum CareCondition
        {
            Normal,
            Hungry,
            Sleepy,
            Messy,
            Sick
        }

        private CheeseTamaModel current;
        private CheeseTamaGrowthStage activeGrowthStage;
        private GameObject activeStagePrefab;
        private bool hasActiveGrowthStage;
        private Vector3 restingLocalPosition;
        private bool hasRestingLocalPosition;
        private float idleSeed;

        private float reactionStartedAt;
        private float reactionDuration = CareReactionDuration;
        private float reactionHopHeight = CareReactionHopHeight;
        private float reactionPunch = CareReactionPunch;
        private bool isReacting;
        private CheeseTamaVisualAction activeAction = CheeseTamaVisualAction.Neutral;

        private Renderer[] modelRenderers;
        private MaterialPropertyBlock propertyBlock;
        private Color flashColor = Color.white;
        private float flashStrength;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int ConditionColorId = Shader.PropertyToID("_ConditionColor");
        private static readonly int ConditionStrengthId = Shader.PropertyToID("_ConditionStrength");
        private static readonly int ConditionValueScaleId = Shader.PropertyToID("_ConditionValueScale");

        private Transform expressionRoot;

        private Transform conditionRoot;
        private Transform conditionBackdrop;
        private Transform hungryConditionMark;
        private Transform sleepyConditionMark;
        private Transform messyConditionRoot;
        private Transform sickConditionRoot;
        private CareCondition activeCondition;
        private bool hasActiveCondition;

        private Transform propRoot;
        private Transform milkBottleRoot;
        private Transform snackBiteRoot;
        private Transform playBall;
        private Transform cleanBubbleRoot;
        private Transform restDreamRoot;
        private Transform sparkleRoot;
        private Transform cookSteamRoot;

        private CheeseTamaExpression forcedExpression = CheeseTamaExpression.Idle;
        private float forcedExpressionUntil;
        private bool hasForcedExpression;

        private void Awake()
        {
            idleSeed = Random.Range(0f, 100f);
            EnsureGrowthVisualSet();
            EnsureModel();
            EnsureExpressionRig();
            EnsureConditionIndicator();
            RefreshConditionIndicator(true);
            CaptureRestingPosition();
        }

        private void EnsureModel()
        {
            EnsureGrowthVisualSet();
            ReconcileGeneratedModels();
            var desiredStage = CheeseTamaGrowthStageCatalog.Resolve(current);
            var desiredStagePrefab = growthVisualSet != null
                ? growthVisualSet.GetPrefab(desiredStage)
                : null;

            if (modelInstance == null)
            {
                var existing = transform.Find("GeneratedModel");
                if (existing != null)
                {
                    modelInstance = existing;
                }
            }

            var shouldReplaceStageModel = desiredStagePrefab != null
                && (!hasActiveGrowthStage
                    || activeGrowthStage != desiredStage
                    || activeStagePrefab != desiredStagePrefab);

            if (shouldReplaceStageModel)
            {
                ReplaceModel(desiredStagePrefab);
                activeGrowthStage = desiredStage;
                activeStagePrefab = desiredStagePrefab;
                hasActiveGrowthStage = true;
            }
            else if (modelInstance == null && modelPrefab != null)
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

        private void ReconcileGeneratedModels()
        {
            var selected = modelInstance;
            if (selected != null && (selected.parent != transform || selected.name != "GeneratedModel"))
            {
                selected = null;
            }

            for (var index = transform.childCount - 1; index >= 0; index -= 1)
            {
                var child = transform.GetChild(index);
                if (child.name != "GeneratedModel")
                {
                    continue;
                }

                if (selected == null)
                {
                    selected = child;
                    continue;
                }

                if (child == selected)
                {
                    continue;
                }

                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }

            modelInstance = selected;
        }

        private void EnsureGrowthVisualSet()
        {
            if (growthVisualSet == null)
            {
                growthVisualSet = Resources.Load<CheeseTamaGrowthVisualSet>("CheeseTamaGrowthVisualSet");
            }
        }

        private void ReplaceModel(GameObject prefab)
        {
            if (prefab == null)
            {
                return;
            }

            if (modelInstance != null)
            {
                var previous = modelInstance.gameObject;
                modelInstance = null;
                previous.SetActive(false);
                Destroy(previous);
            }

            var go = Instantiate(prefab, transform);
            go.name = "GeneratedModel";
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.Euler(0f, modelYawDegrees, 0f);
            go.transform.localScale = Vector3.one * modelScale;
            modelInstance = go.transform;
        }

        private void LateUpdate()
        {
            EnsureModel();

            EnsureExpressionRig();
            EnsureConditionIndicator();
            RefreshConditionIndicator(false);
            UpdateConditionIndicatorMotion();
            ApplyExpression(ResolveExpression());

            var baseScale = GetStageRootScale(CheeseTamaGrowthStageCatalog.Resolve(current));

            if (!isReacting)
            {
                HideActionProps();

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
            var roll = Mathf.Sin(normalized * Mathf.PI * 2f) * 3.5f * settle;
            var pitch = 0f;

            ApplyActionMotion(normalized, arc, settle, ref hop, ref side, ref punch, ref roll, ref pitch);

            transform.localPosition = restingLocalPosition + Vector3.up * hop + Vector3.right * side;
            transform.localScale = new Vector3(
                baseScale.x * (1f + punch * 0.6f),
                baseScale.y * (1f - punch),
                baseScale.z * (1f + punch * 0.6f));

            if (modelInstance != null)
            {
                modelInstance.localRotation = Quaternion.Euler(pitch, modelYawDegrees, roll);
            }

            UpdateActionProps(normalized, arc, settle);
            ApplyFlash(arc * flashStrength);

            if (normalized >= 1f)
            {
                isReacting = false;
                activeAction = CheeseTamaVisualAction.Neutral;
                flashStrength = 0f;
                transform.localPosition = restingLocalPosition;
                transform.localScale = baseScale;
                HideActionProps();
                ApplyFlash(0f);
            }
        }

        public void Bind(CheeseTamaModel tama)
        {
            current = tama;
            EnsureModel();
            EnsureExpressionRig();
            EnsureConditionIndicator();
            CaptureRestingPosition();
            if (current == null)
            {
                RefreshConditionIndicator(true);
                ApplyFlash(0f);
                return;
            }

            if (!isReacting)
            {
                transform.localScale = GetStageRootScale(CheeseTamaGrowthStageCatalog.Resolve(current));
                transform.localPosition = restingLocalPosition;
            }

            ApplyExpression(ResolveExpression());
            RefreshConditionIndicator(true);
            ApplyFlash(0f);
        }

        public void React(bool celebrate = false)
        {
            ReactAction(celebrate ? CheeseTamaVisualAction.Hatch : CheeseTamaVisualAction.Neutral, celebrate);
        }

        public bool IsReacting => isReacting;

        public void ReactAction(CheeseTamaVisualAction action, bool celebrate = false)
        {
            EnsureModel();
            EnsureExpressionRig();
            CaptureRestingPosition();

            activeAction = celebrate ? CheeseTamaVisualAction.Hatch : action;
            reactionStartedAt = Time.realtimeSinceStartup;
            ConfigureReactionProfile(activeAction, celebrate);
            ForceExpression(GetActionExpression(activeAction), reactionDuration + ExpressionHoldPadding);
            isReacting = true;
            UpdateActionProps(0f, 0f, 1f);
        }

        public void ReactEvent(string eventId)
        {
            ReactEvent(eventId, CheeseTamaVisualAction.Event);
        }

        public void ReactEvent(string eventId, CheeseTamaVisualAction fallbackAction)
        {
            EnsureModel();
            EnsureExpressionRig();
            CaptureRestingPosition();

            activeAction = fallbackAction == CheeseTamaVisualAction.Neutral
                ? CheeseTamaVisualAction.Event
                : fallbackAction;
            reactionStartedAt = Time.realtimeSinceStartup;
            reactionDuration = EventReactionDuration;
            reactionHopHeight = EventReactionHopHeight;
            reactionPunch = EventReactionPunch;
            flashColor = GetEventColor(eventId);
            flashStrength = 0.4f;
            ForceExpression(GetEventExpression(eventId), reactionDuration + ExpressionHoldPadding);
            isReacting = true;
            UpdateActionProps(0f, 0f, 1f);
        }

        private void ConfigureReactionProfile(CheeseTamaVisualAction action, bool celebrate)
        {
            if (celebrate || action == CheeseTamaVisualAction.Hatch || action == CheeseTamaVisualAction.LevelUp)
            {
                reactionDuration = HatchReactionDuration;
                reactionHopHeight = action == CheeseTamaVisualAction.LevelUp ? 0.32f : HatchReactionHopHeight;
                reactionPunch = action == CheeseTamaVisualAction.LevelUp ? 0.17f : HatchReactionPunch;
                flashColor = new Color(1f, 0.95f, 0.6f);
                flashStrength = 0.45f;
                return;
            }

            switch (action)
            {
                case CheeseTamaVisualAction.FeedMilk:
                    reactionDuration = 0.96f;
                    reactionHopHeight = 0.16f;
                    reactionPunch = 0.08f;
                    flashColor = new Color(1f, 0.98f, 0.84f);
                    flashStrength = 0.2f;
                    break;
                case CheeseTamaVisualAction.FeedSnack:
                    reactionDuration = 0.78f;
                    reactionHopHeight = 0.13f;
                    reactionPunch = 0.075f;
                    flashColor = new Color(1f, 0.88f, 0.58f);
                    flashStrength = 0.22f;
                    break;
                case CheeseTamaVisualAction.Play:
                    reactionDuration = 0.92f;
                    reactionHopHeight = 0.25f;
                    reactionPunch = 0.12f;
                    flashColor = new Color(1f, 0.92f, 0.32f);
                    flashStrength = 0.24f;
                    break;
                case CheeseTamaVisualAction.Clean:
                    reactionDuration = 0.82f;
                    reactionHopHeight = 0.11f;
                    reactionPunch = 0.06f;
                    flashColor = new Color(0.74f, 0.92f, 1f);
                    flashStrength = 0.24f;
                    break;
                case CheeseTamaVisualAction.Rest:
                    reactionDuration = 1.15f;
                    reactionHopHeight = 0.055f;
                    reactionPunch = 0.035f;
                    flashColor = new Color(0.68f, 0.64f, 1f);
                    flashStrength = 0.18f;
                    break;
                case CheeseTamaVisualAction.Cook:
                    reactionDuration = 0.9f;
                    reactionHopHeight = 0.18f;
                    reactionPunch = 0.08f;
                    flashColor = new Color(1f, 0.78f, 0.32f);
                    flashStrength = 0.28f;
                    break;
                default:
                    reactionDuration = CareReactionDuration;
                    reactionHopHeight = CareReactionHopHeight;
                    reactionPunch = CareReactionPunch;
                    flashColor = new Color(1f, 1f, 0.9f);
                    flashStrength = 0.18f;
                    break;
            }
        }

        private void ApplyActionMotion(
            float normalized,
            float arc,
            float settle,
            ref float hop,
            ref float side,
            ref float punch,
            ref float roll,
            ref float pitch)
        {
            switch (activeAction)
            {
                case CheeseTamaVisualAction.FeedMilk:
                    hop *= 0.85f;
                    side = Mathf.Sin(normalized * Mathf.PI * 3f) * 0.018f * settle;
                    pitch = -7f * arc;
                    roll = Mathf.Sin(normalized * Mathf.PI * 2f) * 2.2f * settle;
                    break;
                case CheeseTamaVisualAction.FeedSnack:
                    hop *= 0.75f;
                    side = Mathf.Sin(normalized * Mathf.PI * 4f) * 0.026f * settle;
                    pitch = -4f * arc;
                    roll = Mathf.Sin(normalized * Mathf.PI * 2f) * 3.2f * settle;
                    break;
                case CheeseTamaVisualAction.Play:
                    hop += Mathf.Abs(Mathf.Sin(normalized * Mathf.PI * 4f)) * 0.075f * settle;
                    side = Mathf.Sin(normalized * Mathf.PI * 2f) * 0.07f * settle;
                    roll = Mathf.Sin(normalized * Mathf.PI * 4f) * 9f * settle;
                    punch *= 1.15f;
                    break;
                case CheeseTamaVisualAction.Clean:
                    hop *= 0.55f;
                    side = Mathf.Sin(normalized * Mathf.PI * 6f) * 0.045f * settle;
                    roll = Mathf.Sin(normalized * Mathf.PI * 6f) * 5f * settle;
                    punch *= 0.75f;
                    break;
                case CheeseTamaVisualAction.Rest:
                    hop *= 0.35f;
                    side = 0f;
                    roll = Mathf.Sin(normalized * Mathf.PI) * -4f * settle;
                    punch *= 0.45f;
                    break;
                case CheeseTamaVisualAction.Cook:
                    side = Mathf.Sin(normalized * Mathf.PI * 2f) * 0.035f * settle;
                    roll = Mathf.Sin(normalized * Mathf.PI * 3f) * 4.5f * settle;
                    break;
                case CheeseTamaVisualAction.LevelUp:
                case CheeseTamaVisualAction.Hatch:
                    side = Mathf.Sin(normalized * Mathf.PI * 2f) * 0.06f * settle;
                    roll = Mathf.Sin(normalized * Mathf.PI * 4f) * 7f * settle;
                    break;
            }
        }

        private void EnsureExpressionRig()
        {
            RemoveExpressionOverlay();

            propRoot = EnsureChild(transform, "Action Prop Overlay");
            propRoot.localPosition = Vector3.zero;
            propRoot.localRotation = Quaternion.identity;
            propRoot.localScale = Vector3.one;

            EnsureActionProps();
        }

        private void EnsureConditionIndicator()
        {
            var rebuilt = conditionRoot == null
                || conditionBackdrop == null
                || hungryConditionMark == null
                || sleepyConditionMark == null
                || messyConditionRoot == null
                || sickConditionRoot == null;

            if (conditionRoot == null)
            {
                conditionRoot = EnsureChild(transform, "Condition Overlay");
                conditionRoot.localRotation = Quaternion.identity;
            }

            if (conditionBackdrop == null)
            {
                conditionBackdrop = EnsurePrimitive(
                    conditionRoot,
                    "Condition Backdrop",
                    PrimitiveType.Sphere,
                    new Color(1f, 0.96f, 0.82f),
                    ToonMaterialProfile.CharacterHighlight);
                SetPart(
                    conditionBackdrop,
                    Vector3.zero,
                    new Vector3(0.29f, 0.2f, 0.028f),
                    Quaternion.identity);
            }

            if (hungryConditionMark == null)
            {
                hungryConditionMark = EnsureText(
                    conditionRoot,
                    "Hungry Condition Mark",
                    "!",
                    new Color(0.35f, 0.16f, 0.58f),
                    0.055f);
                SetPart(
                    hungryConditionMark,
                    new Vector3(0f, -0.005f, -0.045f),
                    Vector3.one,
                    Quaternion.Euler(0f, 180f, 0f));
            }

            if (sleepyConditionMark == null)
            {
                sleepyConditionMark = EnsureText(
                    conditionRoot,
                    "Sleepy Condition Mark",
                    "Z",
                    new Color(0.48f, 0.44f, 0.78f),
                    0.06f);
                SetPart(
                    sleepyConditionMark,
                    new Vector3(0f, -0.002f, -0.045f),
                    Vector3.one,
                    Quaternion.Euler(0f, 180f, 0f));
            }

            if (messyConditionRoot == null)
            {
                messyConditionRoot = EnsureChild(conditionRoot, "Messy Condition Mark");
                var dustLarge = EnsurePrimitive(
                    messyConditionRoot,
                    "Dust Large",
                    PrimitiveType.Sphere,
                    new Color(0.5f, 0.37f, 0.22f),
                    ToonMaterialProfile.CharacterMark);
                var dustMid = EnsurePrimitive(
                    messyConditionRoot,
                    "Dust Mid",
                    PrimitiveType.Sphere,
                    new Color(0.62f, 0.48f, 0.29f),
                    ToonMaterialProfile.CharacterMark);
                var dustSmall = EnsurePrimitive(
                    messyConditionRoot,
                    "Dust Small",
                    PrimitiveType.Sphere,
                    new Color(0.42f, 0.31f, 0.2f),
                    ToonMaterialProfile.CharacterMark);
                SetPart(dustLarge, new Vector3(-0.09f, -0.035f, -0.045f), new Vector3(0.07f, 0.07f, 0.018f), Quaternion.identity);
                SetPart(dustMid, new Vector3(0.02f, 0.05f, -0.045f), new Vector3(0.055f, 0.055f, 0.016f), Quaternion.identity);
                SetPart(dustSmall, new Vector3(0.105f, -0.055f, -0.045f), new Vector3(0.042f, 0.042f, 0.014f), Quaternion.identity);
            }

            if (sickConditionRoot == null)
            {
                sickConditionRoot = EnsureChild(conditionRoot, "Sick Condition Mark");
                var crossHorizontal = EnsurePrimitive(
                    sickConditionRoot,
                    "Cross Horizontal",
                    PrimitiveType.Cube,
                    new Color(0.28f, 0.62f, 0.69f),
                    ToonMaterialProfile.CharacterHighlight);
                var crossVertical = EnsurePrimitive(
                    sickConditionRoot,
                    "Cross Vertical",
                    PrimitiveType.Cube,
                    new Color(0.28f, 0.62f, 0.69f),
                    ToonMaterialProfile.CharacterHighlight);
                SetPart(crossHorizontal, new Vector3(0f, 0f, -0.045f), new Vector3(0.13f, 0.042f, 0.014f), Quaternion.identity);
                SetPart(crossVertical, new Vector3(0f, 0f, -0.047f), new Vector3(0.042f, 0.13f, 0.014f), Quaternion.identity);
            }

            if (rebuilt)
            {
                hasActiveCondition = false;
                conditionRoot.gameObject.SetActive(false);
            }
            else if (!hasActiveCondition)
            {
                conditionRoot.gameObject.SetActive(false);
            }
        }

        private void RefreshConditionIndicator(bool force)
        {
            var condition = ResolveCareCondition(current);
            if (!force && hasActiveCondition && condition == activeCondition)
            {
                return;
            }

            activeCondition = condition;
            hasActiveCondition = true;
            if (conditionRoot == null)
            {
                return;
            }

            var visible = condition != CareCondition.Normal;
            conditionRoot.gameObject.SetActive(visible);
            SetActive(hungryConditionMark, condition == CareCondition.Hungry);
            SetActive(sleepyConditionMark, condition == CareCondition.Sleepy);
            SetActive(messyConditionRoot, condition == CareCondition.Messy);
            SetActive(sickConditionRoot, condition == CareCondition.Sick);

            if (visible && conditionBackdrop != null)
            {
                SetPartColor(
                    conditionBackdrop,
                    GetConditionBackdropColor(condition),
                    ToonMaterialProfile.CharacterHighlight);
            }
        }

        private void UpdateConditionIndicatorMotion()
        {
            if (conditionRoot == null || !conditionRoot.gameObject.activeSelf)
            {
                return;
            }

            var position = ResolveConditionIndicatorPosition();
            position.y += Mathf.Sin((Time.realtimeSinceStartup + idleSeed) * 2.2f) * 0.025f;
            conditionRoot.localPosition = position;
            conditionRoot.localRotation = Quaternion.identity;
            var pulse = 1f + Mathf.Sin((Time.realtimeSinceStartup + idleSeed) * 2.6f) * 0.025f;
            conditionRoot.localScale = Vector3.one * pulse;
        }

        private Vector3 ResolveConditionIndicatorPosition()
        {
            var hasBounds = false;
            var bounds = new Bounds();
            if (modelRenderers != null)
            {
                foreach (var renderer in modelRenderers)
                {
                    if (renderer == null || !renderer.enabled)
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
            }

            if (!hasBounds)
            {
                return new Vector3(-0.72f, 0.78f, PropZ - 0.05f);
            }

            var topCenterWorld = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
            var topCenterLocal = transform.InverseTransformPoint(topCenterWorld);
            return new Vector3(topCenterLocal.x, topCenterLocal.y + 0.28f, PropZ - 0.05f);
        }

        private void RemoveExpressionOverlay()
        {
            var existing = expressionRoot != null ? expressionRoot : transform.Find("Expression Overlay");
            if (existing == null)
            {
                return;
            }

            existing.gameObject.SetActive(false);
            expressionRoot = null;
            if (Application.isPlaying)
            {
                Destroy(existing.gameObject);
                return;
            }

            DestroyImmediate(existing.gameObject);
        }

        private void EnsureActionProps()
        {
            milkBottleRoot = EnsureChild(propRoot, "Milk Bottle Action Prop");
            EnsurePrimitive(milkBottleRoot, "Milk Bottle Body", PrimitiveType.Capsule, new Color(0.96f, 0.98f, 0.9f), ToonMaterialProfile.EnvironmentGlass);
            EnsurePrimitive(milkBottleRoot, "Milk Bottle Fill", PrimitiveType.Capsule, new Color(0.78f, 0.92f, 1f), ToonMaterialProfile.EnvironmentGlass);
            EnsurePrimitive(milkBottleRoot, "Milk Bottle Cap", PrimitiveType.Cube, new Color(0.42f, 0.75f, 0.95f), ToonMaterialProfile.EnvironmentGlass);
            SetPart(milkBottleRoot.Find("Milk Bottle Body"), Vector3.zero, new Vector3(0.07f, 0.18f, 0.07f), Quaternion.identity);
            SetPart(milkBottleRoot.Find("Milk Bottle Fill"), new Vector3(0f, -0.035f, -0.005f), new Vector3(0.055f, 0.13f, 0.055f), Quaternion.identity);
            SetPart(milkBottleRoot.Find("Milk Bottle Cap"), new Vector3(0f, 0.18f, 0f), new Vector3(0.09f, 0.035f, 0.09f), Quaternion.identity);

            snackBiteRoot = EnsureChild(propRoot, "Snack Bite Action Prop");
            EnsurePrimitive(snackBiteRoot, "Snack Cracker Body", PrimitiveType.Sphere, new Color(1f, 0.72f, 0.26f), ToonMaterialProfile.CharacterMark);
            EnsurePrimitive(snackBiteRoot, "Snack Cheese Spot A", PrimitiveType.Sphere, new Color(1f, 0.9f, 0.42f), ToonMaterialProfile.CharacterHighlight);
            EnsurePrimitive(snackBiteRoot, "Snack Cheese Spot B", PrimitiveType.Sphere, new Color(0.86f, 0.42f, 0.12f), ToonMaterialProfile.CharacterMark);
            EnsurePrimitive(snackBiteRoot, "Snack Crumb A", PrimitiveType.Sphere, new Color(1f, 0.78f, 0.34f), ToonMaterialProfile.CharacterMark);
            EnsurePrimitive(snackBiteRoot, "Snack Crumb B", PrimitiveType.Sphere, new Color(0.94f, 0.58f, 0.18f), ToonMaterialProfile.CharacterMark);
            SetPart(snackBiteRoot.Find("Snack Cracker Body"), Vector3.zero, new Vector3(0.11f, 0.08f, 0.018f), Quaternion.identity);
            SetPart(snackBiteRoot.Find("Snack Cheese Spot A"), new Vector3(-0.025f, 0.014f, -0.012f), new Vector3(0.026f, 0.019f, 0.006f), Quaternion.identity);
            SetPart(snackBiteRoot.Find("Snack Cheese Spot B"), new Vector3(0.036f, -0.01f, -0.012f), new Vector3(0.019f, 0.014f, 0.006f), Quaternion.identity);
            SetPart(snackBiteRoot.Find("Snack Crumb A"), new Vector3(0.13f, 0.045f, 0f), new Vector3(0.018f, 0.018f, 0.006f), Quaternion.identity);
            SetPart(snackBiteRoot.Find("Snack Crumb B"), new Vector3(0.16f, -0.015f, 0f), new Vector3(0.014f, 0.014f, 0.006f), Quaternion.identity);

            playBall = EnsurePrimitive(propRoot, "Play Ball Action Prop", PrimitiveType.Sphere, new Color(0.45f, 0.82f, 1f), ToonMaterialProfile.EnvironmentGlass);

            cleanBubbleRoot = EnsureChild(propRoot, "Clean Bubble Action Prop");
            EnsurePrimitive(cleanBubbleRoot, "Clean Bubble Large", PrimitiveType.Sphere, new Color(0.82f, 0.95f, 1f), ToonMaterialProfile.EnvironmentGlass);
            EnsurePrimitive(cleanBubbleRoot, "Clean Bubble Mid", PrimitiveType.Sphere, new Color(0.9f, 0.98f, 1f), ToonMaterialProfile.EnvironmentGlass);
            EnsurePrimitive(cleanBubbleRoot, "Clean Bubble Small", PrimitiveType.Sphere, new Color(0.74f, 0.9f, 1f), ToonMaterialProfile.EnvironmentGlass);
            SetPart(cleanBubbleRoot.Find("Clean Bubble Large"), new Vector3(-0.11f, 0.02f, 0f), new Vector3(0.065f, 0.065f, 0.02f), Quaternion.identity);
            SetPart(cleanBubbleRoot.Find("Clean Bubble Mid"), new Vector3(0.03f, 0.09f, 0f), new Vector3(0.048f, 0.048f, 0.018f), Quaternion.identity);
            SetPart(cleanBubbleRoot.Find("Clean Bubble Small"), new Vector3(0.13f, 0.0f, 0f), new Vector3(0.036f, 0.036f, 0.015f), Quaternion.identity);

            restDreamRoot = EnsureChild(propRoot, "Rest Dream Action Prop");
            EnsureText(restDreamRoot, "Rest Dream Text", "Z", new Color(0.68f, 0.64f, 1f), 0.22f);
            EnsurePrimitive(restDreamRoot, "Rest Dream Dot A", PrimitiveType.Sphere, new Color(0.68f, 0.64f, 1f), ToonMaterialProfile.CharacterHighlight);
            EnsurePrimitive(restDreamRoot, "Rest Dream Dot B", PrimitiveType.Sphere, new Color(0.68f, 0.64f, 1f), ToonMaterialProfile.CharacterHighlight);
            SetPart(restDreamRoot.Find("Rest Dream Text"), new Vector3(0.08f, 0.06f, 0f), Vector3.one, Quaternion.Euler(0f, 180f, 0f));
            SetPart(restDreamRoot.Find("Rest Dream Dot A"), new Vector3(-0.08f, -0.02f, 0f), new Vector3(0.025f, 0.025f, 0.01f), Quaternion.identity);
            SetPart(restDreamRoot.Find("Rest Dream Dot B"), new Vector3(-0.15f, -0.08f, 0f), new Vector3(0.018f, 0.018f, 0.01f), Quaternion.identity);

            sparkleRoot = EnsureChild(propRoot, "Sparkle Action Prop");
            EnsurePrimitive(sparkleRoot, "Sparkle Top", PrimitiveType.Cube, new Color(1f, 0.88f, 0.2f), ToonMaterialProfile.CharacterHighlight);
            EnsurePrimitive(sparkleRoot, "Sparkle Left", PrimitiveType.Cube, new Color(1f, 0.94f, 0.45f), ToonMaterialProfile.CharacterHighlight);
            EnsurePrimitive(sparkleRoot, "Sparkle Right", PrimitiveType.Cube, new Color(1f, 0.78f, 0.25f), ToonMaterialProfile.CharacterHighlight);
            SetPart(sparkleRoot.Find("Sparkle Top"), new Vector3(0f, 0.12f, 0f), new Vector3(0.035f, 0.11f, 0.012f), Quaternion.Euler(0f, 0f, 45f));
            SetPart(sparkleRoot.Find("Sparkle Left"), new Vector3(-0.11f, -0.02f, 0f), new Vector3(0.028f, 0.09f, 0.012f), Quaternion.Euler(0f, 0f, 45f));
            SetPart(sparkleRoot.Find("Sparkle Right"), new Vector3(0.12f, -0.05f, 0f), new Vector3(0.026f, 0.08f, 0.012f), Quaternion.Euler(0f, 0f, 45f));

            cookSteamRoot = EnsureChild(propRoot, "Cook Steam Action Prop");
            EnsurePrimitive(cookSteamRoot, "Cook Steam A", PrimitiveType.Capsule, new Color(1f, 0.95f, 0.8f), ToonMaterialProfile.EnvironmentGlow);
            EnsurePrimitive(cookSteamRoot, "Cook Steam B", PrimitiveType.Capsule, new Color(1f, 0.88f, 0.58f), ToonMaterialProfile.EnvironmentGlow);
            EnsurePrimitive(cookSteamRoot, "Cook Steam C", PrimitiveType.Capsule, new Color(1f, 0.95f, 0.72f), ToonMaterialProfile.EnvironmentGlow);
            SetPart(cookSteamRoot.Find("Cook Steam A"), new Vector3(-0.08f, 0f, 0f), new Vector3(0.018f, 0.08f, 0.018f), Quaternion.Euler(0f, 0f, -20f));
            SetPart(cookSteamRoot.Find("Cook Steam B"), new Vector3(0.02f, 0.04f, 0f), new Vector3(0.016f, 0.09f, 0.016f), Quaternion.Euler(0f, 0f, 15f));
            SetPart(cookSteamRoot.Find("Cook Steam C"), new Vector3(0.11f, -0.01f, 0f), new Vector3(0.014f, 0.075f, 0.014f), Quaternion.Euler(0f, 0f, 28f));

            HideActionProps();
        }

        private void UpdateActionProps(float normalized, float arc, float settle)
        {
            HideActionProps();

            switch (activeAction)
            {
                case CheeseTamaVisualAction.FeedMilk:
                    SetActive(milkBottleRoot, true);
                    SetPart(
                        milkBottleRoot,
                        new Vector3(-0.38f + Mathf.Sin(normalized * Mathf.PI * 2f) * 0.035f, -0.02f + arc * 0.055f, PropZ),
                        Vector3.one,
                        Quaternion.Euler(0f, 0f, -22f + arc * 28f));
                    break;
                case CheeseTamaVisualAction.FeedSnack:
                    SetActive(snackBiteRoot, true);
                    SetPart(
                        snackBiteRoot,
                        new Vector3(-0.34f + normalized * 0.18f, -0.03f + arc * 0.06f, PropZ),
                        Vector3.one * (0.95f + arc * 0.14f),
                        Quaternion.Euler(0f, 0f, -16f + normalized * 42f));
                    break;
                case CheeseTamaVisualAction.Play:
                    SetActive(playBall, true);
                    SetPart(
                        playBall,
                        new Vector3(Mathf.Lerp(-0.62f, 0.62f, normalized), -0.4f + Mathf.Abs(Mathf.Sin(normalized * Mathf.PI * 2f)) * 0.12f, PropZ),
                        new Vector3(0.12f, 0.12f, 0.12f),
                        Quaternion.identity);
                    break;
                case CheeseTamaVisualAction.Clean:
                    SetActive(cleanBubbleRoot, true);
                    SetPart(
                        cleanBubbleRoot,
                        new Vector3(0.43f + Mathf.Sin(normalized * Mathf.PI * 3f) * 0.04f, 0.18f + arc * 0.07f, PropZ),
                        Vector3.one * (0.9f + arc * 0.25f),
                        Quaternion.Euler(0f, 0f, normalized * 55f));
                    break;
                case CheeseTamaVisualAction.Rest:
                    SetActive(restDreamRoot, true);
                    SetPart(
                        restDreamRoot,
                        new Vector3(0.42f, 0.58f + normalized * 0.15f, PropZ),
                        Vector3.one * (0.85f + arc * 0.18f),
                        Quaternion.identity);
                    break;
                case CheeseTamaVisualAction.Cook:
                    SetActive(cookSteamRoot, true);
                    SetPart(
                        cookSteamRoot,
                        new Vector3(-0.42f, 0.18f + normalized * 0.1f, PropZ),
                        Vector3.one * (0.9f + arc * 0.2f),
                        Quaternion.identity);
                    SetActive(sparkleRoot, true);
                    SetPart(
                        sparkleRoot,
                        new Vector3(0.44f, 0.44f + arc * 0.07f, PropZ),
                        Vector3.one * (0.8f + arc * 0.3f),
                        Quaternion.Euler(0f, 0f, normalized * 120f));
                    break;
                case CheeseTamaVisualAction.LevelUp:
                case CheeseTamaVisualAction.Hatch:
                case CheeseTamaVisualAction.Event:
                    SetActive(sparkleRoot, true);
                    SetPart(
                        sparkleRoot,
                        new Vector3(0.0f, 0.64f + arc * 0.16f, PropZ),
                        Vector3.one * (1f + arc * 0.45f),
                        Quaternion.Euler(0f, 0f, normalized * 180f));
                    break;
            }
        }

        private void HideActionProps()
        {
            SetActive(milkBottleRoot, false);
            SetActive(snackBiteRoot, false);
            SetActive(playBall, false);
            SetActive(cleanBubbleRoot, false);
            SetActive(restDreamRoot, false);
            SetActive(sparkleRoot, false);
            SetActive(cookSteamRoot, false);
        }

        private CheeseTamaExpression ResolveExpression()
        {
            if (hasForcedExpression && Time.realtimeSinceStartup < forcedExpressionUntil)
            {
                return forcedExpression;
            }

            hasForcedExpression = false;
            return GetStatusExpression(current);
        }

        private void ForceExpression(CheeseTamaExpression expression, float seconds)
        {
            forcedExpression = expression;
            forcedExpressionUntil = Time.realtimeSinceStartup + Mathf.Max(0f, seconds);
            hasForcedExpression = true;
            ApplyExpression(expression);
        }

        private void ApplyExpression(CheeseTamaExpression expression)
        {
            switch (expression)
            {
                case CheeseTamaExpression.Happy:
                    ApplyFace(
                        0.035f,
                        new Vector2(0.09f, 0.04f),
                        new Vector2(0.2f, 0.03f),
                        false,
                        true,
                        false,
                        9f,
                        new Color(1f, 0.46f, 0.34f));
                    break;
                case CheeseTamaExpression.Full:
                    ApplyFace(
                        0.025f,
                        new Vector2(0.095f, 0.035f),
                        new Vector2(0.24f, 0.036f),
                        false,
                        true,
                        false,
                        7f,
                        new Color(1f, 0.5f, 0.35f));
                    break;
                case CheeseTamaExpression.Hungry:
                    ApplyFace(
                        -0.015f,
                        new Vector2(0.065f, 0.095f),
                        new Vector2(0.05f, 0.045f),
                        true,
                        false,
                        true,
                        -6f,
                        new Color(0.8f, 0.62f, 0.52f));
                    break;
                case CheeseTamaExpression.Sleepy:
                    ApplyFace(
                        -0.01f,
                        new Vector2(0.095f, 0.018f),
                        new Vector2(0.13f, 0.028f),
                        true,
                        false,
                        false,
                        0f,
                        new Color(0.75f, 0.68f, 0.88f));
                    break;
                case CheeseTamaExpression.Sad:
                    ApplyFace(
                        -0.025f,
                        new Vector2(0.06f, 0.075f),
                        new Vector2(0.12f, 0.026f),
                        false,
                        false,
                        true,
                        -10f,
                        new Color(0.76f, 0.64f, 0.58f));
                    break;
                case CheeseTamaExpression.Upset:
                    ApplyFace(
                        -0.005f,
                        new Vector2(0.07f, 0.06f),
                        new Vector2(0.13f, 0.028f),
                        false,
                        false,
                        true,
                        12f,
                        new Color(0.9f, 0.42f, 0.32f));
                    break;
                case CheeseTamaExpression.Sick:
                    ApplyFace(
                        -0.02f,
                        new Vector2(0.06f, 0.045f),
                        new Vector2(0.07f, 0.035f),
                        true,
                        true,
                        true,
                        -6f,
                        new Color(0.62f, 0.78f, 0.9f));
                    break;
                case CheeseTamaExpression.Sparkle:
                    ApplyFace(
                        0.02f,
                        new Vector2(0.105f, 0.13f),
                        new Vector2(0.2f, 0.036f),
                        false,
                        true,
                        false,
                        0f,
                        new Color(1f, 0.48f, 0.34f));
                    SetActive(sparkleRoot, isReacting);
                    break;
                case CheeseTamaExpression.Surprised:
                    ApplyFace(
                        0.02f,
                        new Vector2(0.11f, 0.13f),
                        new Vector2(0.08f, 0.085f),
                        true,
                        true,
                        false,
                        0f,
                        new Color(1f, 0.5f, 0.35f));
                    break;
                default:
                    ApplyFace(
                        0f,
                        new Vector2(0.08f, 0.11f),
                        new Vector2(0.16f, 0.025f),
                        false,
                        false,
                        false,
                        0f,
                        new Color(1f, 0.46f, 0.34f));
                    break;
            }
        }

        private void ApplyFace(
            float verticalOffset,
            Vector2 eyeScale,
            Vector2 mouthScale,
            bool openMouth,
            bool showCheeks,
            bool showBrows,
            float eyeRoll,
            Color cheekColor)
        {
            // The imported CheeseTama model already owns the face. Keep runtime
            // reactions to body motion, flashes, and action props only.
        }

        private void ApplyFlash(float strength)
        {
            if (modelRenderers == null || modelRenderers.Length == 0)
            {
                return;
            }

            propertyBlock ??= new MaterialPropertyBlock();
            var tint = Color.Lerp(Color.white, flashColor, Mathf.Clamp01(strength));
            var condition = hasActiveCondition ? activeCondition : ResolveCareCondition(current);
            var conditionColor = GetConditionBodyColor(condition);
            var conditionStrength = GetConditionBodyStrength(condition);
            var conditionValueScale = GetConditionBodyValueScale(condition);
            foreach (var r in modelRenderers)
            {
                if (r == null)
                {
                    continue;
                }

                r.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(BaseColorId, tint);
                propertyBlock.SetColor(ColorId, tint);
                propertyBlock.SetColor(ConditionColorId, conditionColor);
                propertyBlock.SetFloat(ConditionStrengthId, conditionStrength);
                propertyBlock.SetFloat(ConditionValueScaleId, conditionValueScale);
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

        private static Transform EnsureChild(Transform parent, string childName)
        {
            var existing = parent.Find(childName);
            if (existing != null)
            {
                return existing;
            }

            var child = new GameObject(childName).transform;
            child.SetParent(parent, false);
            return child;
        }

        private static Transform EnsurePrimitive(
            Transform parent,
            string objectName,
            PrimitiveType primitiveType,
            Color color,
            ToonMaterialProfile profile)
        {
            var existing = parent.Find(objectName);
            if (existing != null)
            {
                SetPartColor(existing, color, profile);
                return existing;
            }

            var primitive = GameObject.CreatePrimitive(primitiveType);
            primitive.name = objectName;
            primitive.transform.SetParent(parent, false);
            DestroyCollider(primitive);
            SetPartColor(primitive.transform, color, profile);
            return primitive.transform;
        }

        private static Transform EnsureText(Transform parent, string objectName, string text, Color color, float size)
        {
            var existing = parent.Find(objectName);
            var textObject = existing != null ? existing.gameObject : new GameObject(objectName);
            textObject.transform.SetParent(parent, false);

            var mesh = textObject.GetComponent<TextMesh>();
            if (mesh == null)
            {
                mesh = textObject.AddComponent<TextMesh>();
            }

            mesh.text = text;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.characterSize = size;
            mesh.fontSize = 42;
            mesh.color = color;
            return textObject.transform;
        }

        private static void DestroyCollider(GameObject primitive)
        {
            var collider = primitive.GetComponent<Collider>();
            if (collider == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(collider);
            }
            else
            {
                Object.DestroyImmediate(collider);
            }
        }

        private static void SetPart(Transform part, Vector3 localPosition, Vector3 localScale, Quaternion localRotation)
        {
            if (part == null)
            {
                return;
            }

            part.localPosition = localPosition;
            part.localScale = localScale;
            part.localRotation = localRotation;
        }

        private static void SetPartColor(Transform part, Color color, ToonMaterialProfile profile)
        {
            if (part == null || !part.TryGetComponent(out Renderer renderer))
            {
                return;
            }

            ToonMaterialUtility.Apply(renderer, profile, color);
        }

        private static void SetActive(Transform part, bool active)
        {
            if (part != null && part.gameObject.activeSelf != active)
            {
                part.gameObject.SetActive(active);
            }
        }

        private static CheeseTamaExpression GetStatusExpression(CheeseTamaModel tama)
        {
            if (tama == null || tama.stats == null)
            {
                return CheeseTamaExpression.Idle;
            }

            switch (ResolveCareCondition(tama))
            {
                case CareCondition.Sick:
                    return CheeseTamaExpression.Sick;
                case CareCondition.Hungry:
                    return CheeseTamaExpression.Hungry;
                case CareCondition.Messy:
                    return CheeseTamaExpression.Upset;
                case CareCondition.Sleepy:
                    return CheeseTamaExpression.Sleepy;
            }

            if (tama.stats.mood < 35)
            {
                return CheeseTamaExpression.Sad;
            }

            if (tama.stats.hunger > 85 && tama.stats.milkSatisfaction > 70)
            {
                return CheeseTamaExpression.Full;
            }

            if (tama.stats.mood > 80)
            {
                return CheeseTamaExpression.Happy;
            }

            return CheeseTamaExpression.Idle;
        }

        private static CareCondition ResolveCareCondition(CheeseTamaModel tama)
        {
            if (tama == null || tama.stats == null)
            {
                return CareCondition.Normal;
            }

            if (tama.stats.health < 35)
            {
                return CareCondition.Sick;
            }

            if (tama.stats.hunger < 25)
            {
                return CareCondition.Hungry;
            }

            if (tama.stats.cleanliness < 35)
            {
                return CareCondition.Messy;
            }

            if (tama.stats.sleepiness > 75)
            {
                return CareCondition.Sleepy;
            }

            return CareCondition.Normal;
        }

        private static Color GetConditionBodyColor(CareCondition condition)
        {
            return condition switch
            {
                CareCondition.Hungry => new Color(0.78f, 0.68f, 0.92f),
                CareCondition.Sleepy => new Color(0.576f, 0.596f, 0.78f),
                CareCondition.Messy => new Color(0.26f, 0.28f, 0.30f),
                CareCondition.Sick => new Color(0.525f, 0.667f, 0.651f),
                _ => Color.white
            };
        }

        private static float GetConditionBodyStrength(CareCondition condition)
        {
            return condition switch
            {
                CareCondition.Hungry => 0.82f,
                CareCondition.Sleepy => 0.72f,
                CareCondition.Messy => 0.92f,
                CareCondition.Sick => 0.75f,
                _ => 0f
            };
        }

        private static float GetConditionBodyValueScale(CareCondition condition)
        {
            return condition switch
            {
                CareCondition.Hungry => 1.05f,
                CareCondition.Messy => 0.72f,
                _ => 1f
            };
        }

        private static Color GetConditionBackdropColor(CareCondition condition)
        {
            return condition switch
            {
                CareCondition.Hungry => new Color(0.9f, 0.82f, 1f),
                CareCondition.Sleepy => new Color(0.88f, 0.87f, 1f),
                CareCondition.Messy => new Color(0.69f, 0.57f, 0.42f),
                CareCondition.Sick => new Color(0.78f, 0.91f, 0.93f),
                _ => new Color(1f, 0.96f, 0.82f)
            };
        }

        private static CheeseTamaExpression GetActionExpression(CheeseTamaVisualAction action)
        {
            return action switch
            {
                CheeseTamaVisualAction.FeedMilk => CheeseTamaExpression.Full,
                CheeseTamaVisualAction.FeedSnack => CheeseTamaExpression.Full,
                CheeseTamaVisualAction.Play => CheeseTamaExpression.Happy,
                CheeseTamaVisualAction.Clean => CheeseTamaExpression.Surprised,
                CheeseTamaVisualAction.Rest => CheeseTamaExpression.Sleepy,
                CheeseTamaVisualAction.Cook => CheeseTamaExpression.Sparkle,
                CheeseTamaVisualAction.LevelUp => CheeseTamaExpression.Sparkle,
                CheeseTamaVisualAction.Hatch => CheeseTamaExpression.Sparkle,
                CheeseTamaVisualAction.Event => CheeseTamaExpression.Sparkle,
                _ => CheeseTamaExpression.Happy
            };
        }

        private static CheeseTamaExpression GetEventExpression(string eventId)
        {
            return eventId switch
            {
                "happy_wiggle" => CheeseTamaExpression.Happy,
                "small_fever" => CheeseTamaExpression.Sick,
                "hungry_peep" => CheeseTamaExpression.Hungry,
                "dusty_corner" => CheeseTamaExpression.Upset,
                "sleepy_yawn" => CheeseTamaExpression.Sleepy,
                "milk_drop_catch" => CheeseTamaExpression.Sparkle,
                "cheese_snack_fed" => CheeseTamaExpression.Full,
                "crumbly_snack" => CheeseTamaExpression.Upset,
                _ => CheeseTamaExpression.Sparkle
            };
        }

        private Vector3 GetStageRootScale(CheeseTamaGrowthStage stage)
        {
            if (growthVisualSet != null && growthVisualSet.GetPrefab(stage) != null)
            {
                return Vector3.one;
            }

            return stage switch
            {
                CheeseTamaGrowthStage.Egg => new Vector3(1.0f, 1.0f, 1.0f),
                CheeseTamaGrowthStage.Hatchling => new Vector3(1.05f, 1.05f, 1.05f),
                CheeseTamaGrowthStage.Soft => new Vector3(1.12f, 1.12f, 1.12f),
                CheeseTamaGrowthStage.Grown => new Vector3(1.2f, 1.2f, 1.2f),
                CheeseTamaGrowthStage.Mature => new Vector3(1.28f, 1.28f, 1.28f),
                CheeseTamaGrowthStage.Final => new Vector3(1.36f, 1.36f, 1.36f),
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
                "cheese_snack_fed" => new Color(1f, 0.86f, 0.46f),
                "crumbly_snack" => new Color(0.95f, 0.64f, 0.42f),
                _ => new Color(0.72f, 0.98f, 0.86f)
            };
        }

        private enum CheeseTamaExpression
        {
            Idle,
            Happy,
            Full,
            Hungry,
            Sleepy,
            Sad,
            Upset,
            Sick,
            Sparkle,
            Surprised
        }

    }
}
