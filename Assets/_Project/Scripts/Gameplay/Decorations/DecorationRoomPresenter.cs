using CheeseTama.Core;
using CheeseTama.Environment;
using UnityEngine;

namespace CheeseTama.Gameplay.Decorations
{
    public sealed class DecorationRoomPresenter : MonoBehaviour
    {
        [SerializeField] private Renderer wallRenderer;
        [SerializeField] private Renderer floorRenderer;
        [SerializeField] private Transform accentAnchor;
        [SerializeField] private Transform windowAnchor;
        [SerializeField] private Transform shelfAnchor;
        [SerializeField] private Transform bedsideAnchor;

        private MaterialPropertyBlock propertyBlock;
        private GameManager manager;
        private MilkroomThemeController themeController;
        private GameObject accentVisual;
        private string appliedAccentId = string.Empty;
        private string appliedWindowId = string.Empty;
        private string appliedShelfId = string.Empty;
        private string appliedBedsideId = string.Empty;
        private GameObject windowVisual;
        private GameObject shelfVisual;
        private GameObject bedsideVisual;

        public void Configure(Renderer wall, Renderer floor, Transform accent)
        {
            Configure(wall, floor, accent, null, null, null);
        }

        public void Configure(
            Renderer wall,
            Renderer floor,
            Transform accent,
            Transform window,
            Transform shelf,
            Transform bedside)
        {
            wallRenderer = wall;
            floorRenderer = floor;
            accentAnchor = accent;
            windowAnchor = window;
            shelfAnchor = shelf;
            bedsideAnchor = bedside;
            themeController = GetComponent<MilkroomThemeController>()
                ?? GetComponentInParent<MilkroomThemeController>();
            BindManager();
            Refresh();
        }

        private void OnEnable()
        {
            BindManager();
            Refresh();
        }

        private void OnDisable()
        {
            if (manager != null)
            {
                manager.DecorationChanged -= Refresh;
                manager.SaveDataReplaced -= Refresh;
            }

            manager = null;
        }

        public void Refresh()
        {
            BindManager();
            var snapshot = manager != null
                ? manager.GetDecorationShopSnapshot()
                : DecorationShopSnapshot.CreateDefault();
            if (snapshot.equippedWallId == DecorationCatalog.CreamWallId)
            {
                themeController ??= GetComponent<MilkroomThemeController>()
                    ?? GetComponentInParent<MilkroomThemeController>();
                if (themeController != null)
                {
                    themeController.ApplyCurrentThemeToRenderer(wallRenderer);
                }
                else
                {
                    ClearColorOverride(wallRenderer);
                }
            }
            else
            {
                ApplyColor(wallRenderer, GetWallColor(snapshot.equippedWallId));
            }

            if (snapshot.equippedFloorId == DecorationCatalog.CreamRugId)
            {
                ClearColorOverride(floorRenderer);
            }
            else
            {
                ApplyColor(floorRenderer, GetFloorColor(snapshot.equippedFloorId));
            }

            RebuildAccent(snapshot.equippedAccentId);
            RebuildWindowSlot(snapshot.equippedWindowId);
            RebuildShelfSlot(snapshot.equippedShelfId);
            RebuildSimpleSlot(bedsideAnchor, ref bedsideVisual, ref appliedBedsideId,
                snapshot.equippedBedsideId, DecorationCatalog.StarPlushId,
                PrimitiveType.Sphere, new Vector3(0.34f, 0.28f, 0.22f),
                new Color(0.95f, 0.72f, 0.2f), new Color(0.92f, 0.92f, 0.84f));
        }

        private void RebuildWindowSlot(string itemId)
        {
            RemoveLegacyPlaceholder(windowAnchor);
            if (windowAnchor == null)
            {
                return;
            }

            if (windowVisual == null)
            {
                var existing = windowAnchor.Find("Equipped Window Decoration");
                windowVisual = existing != null ? existing.gameObject : null;
            }

            if (itemId == DecorationCatalog.CreamCurtainId)
            {
                DestroyUnityObject(windowVisual);
                windowVisual = null;
                appliedWindowId = string.Empty;
                return;
            }

            if (windowVisual != null
                && string.Equals(appliedWindowId, itemId, System.StringComparison.Ordinal))
            {
                return;
            }

            DestroyUnityObject(windowVisual);
            windowVisual = new GameObject("Equipped Window Decoration");
            windowVisual.transform.SetParent(windowAnchor, false);
            appliedWindowId = itemId ?? string.Empty;
            var moonStyle = itemId == DecorationCatalog.MoonCurtainId;
            var fabricColor = moonStyle
                ? new Color(0.34f, 0.40f, 0.70f, 1f)
                : new Color(0.96f, 0.86f, 0.67f, 1f);
            var trimColor = moonStyle
                ? new Color(0.82f, 0.86f, 1f, 1f)
                : new Color(0.78f, 0.54f, 0.24f, 1f);

            CreatePrimitivePart(
                windowVisual.transform,
                "Curtain Rod",
                PrimitiveType.Cylinder,
                new Vector3(0f, 0.08f, 0f),
                new Vector3(0.045f, 0.43f, 0.045f),
                Quaternion.Euler(0f, 0f, 90f),
                trimColor);
            CreatePrimitivePart(
                windowVisual.transform,
                "Left Soft Curtain",
                PrimitiveType.Capsule,
                new Vector3(-0.34f, -0.44f, 0f),
                new Vector3(0.20f, 0.55f, 0.08f),
                Quaternion.Euler(0f, 0f, -4f),
                fabricColor);
            CreatePrimitivePart(
                windowVisual.transform,
                "Right Soft Curtain",
                PrimitiveType.Capsule,
                new Vector3(0.34f, -0.44f, 0f),
                new Vector3(0.20f, 0.55f, 0.08f),
                Quaternion.Euler(0f, 0f, 4f),
                fabricColor);

            if (moonStyle)
            {
                CreatePrimitivePart(
                    windowVisual.transform,
                    "Moon Ornament",
                    PrimitiveType.Sphere,
                    new Vector3(0f, -0.06f, -0.08f),
                    new Vector3(0.10f, 0.10f, 0.035f),
                    Quaternion.identity,
                    new Color(1f, 0.88f, 0.38f, 1f));
            }
        }

        private void RebuildShelfSlot(string itemId)
        {
            RemoveLegacyPlaceholder(shelfAnchor);
            if (shelfAnchor == null)
            {
                return;
            }

            if (shelfVisual == null)
            {
                var existing = shelfAnchor.Find("Equipped Shelf Decoration");
                shelfVisual = existing != null ? existing.gameObject : null;
            }

            if (itemId == DecorationCatalog.CheeseClockId)
            {
                DestroyUnityObject(shelfVisual);
                shelfVisual = null;
                appliedShelfId = string.Empty;
                return;
            }

            if (shelfVisual != null
                && string.Equals(appliedShelfId, itemId, System.StringComparison.Ordinal))
            {
                return;
            }

            DestroyUnityObject(shelfVisual);
            shelfVisual = new GameObject("Equipped Shelf Decoration");
            shelfVisual.transform.SetParent(shelfAnchor, false);
            appliedShelfId = itemId ?? string.Empty;

            if (itemId == DecorationCatalog.MemoryFrameId)
            {
                var frameColor = new Color(0.72f, 0.43f, 0.18f, 1f);
                CreatePrimitivePart(shelfVisual.transform, "Frame Left", PrimitiveType.Cylinder,
                    new Vector3(-0.23f, 0f, 0f), new Vector3(0.035f, 0.28f, 0.035f),
                    Quaternion.identity, frameColor);
                CreatePrimitivePart(shelfVisual.transform, "Frame Right", PrimitiveType.Cylinder,
                    new Vector3(0.23f, 0f, 0f), new Vector3(0.035f, 0.28f, 0.035f),
                    Quaternion.identity, frameColor);
                CreatePrimitivePart(shelfVisual.transform, "Frame Top", PrimitiveType.Cylinder,
                    new Vector3(0f, 0.28f, 0f), new Vector3(0.035f, 0.23f, 0.035f),
                    Quaternion.Euler(0f, 0f, 90f), frameColor);
                CreatePrimitivePart(shelfVisual.transform, "Frame Bottom", PrimitiveType.Cylinder,
                    new Vector3(0f, -0.28f, 0f), new Vector3(0.035f, 0.23f, 0.035f),
                    Quaternion.Euler(0f, 0f, 90f), frameColor);
                CreatePrimitivePart(shelfVisual.transform, "Memory Portrait", PrimitiveType.Sphere,
                    new Vector3(0f, 0f, 0.02f), new Vector3(0.18f, 0.22f, 0.035f),
                    Quaternion.identity, new Color(0.96f, 0.75f, 0.48f, 1f));
                return;
            }

            CreatePrimitivePart(shelfVisual.transform, "Cheese Clock Face", PrimitiveType.Sphere,
                Vector3.zero, new Vector3(0.30f, 0.30f, 0.07f), Quaternion.identity,
                new Color(0.96f, 0.72f, 0.20f, 1f));
            CreatePrimitivePart(shelfVisual.transform, "Clock Hour Hand", PrimitiveType.Cylinder,
                new Vector3(-0.055f, 0.035f, -0.08f), new Vector3(0.018f, 0.10f, 0.018f),
                Quaternion.Euler(0f, 0f, 38f), new Color(0.35f, 0.23f, 0.14f, 1f));
            CreatePrimitivePart(shelfVisual.transform, "Clock Minute Hand", PrimitiveType.Cylinder,
                new Vector3(0.07f, 0.055f, -0.08f), new Vector3(0.014f, 0.14f, 0.014f),
                Quaternion.Euler(0f, 0f, -52f), new Color(0.35f, 0.23f, 0.14f, 1f));
        }

        private static void RemoveLegacyPlaceholder(Transform anchor)
        {
            if (anchor == null)
            {
                return;
            }

            var legacy = anchor.Find("Equipped Decoration Visual");
            if (legacy != null)
            {
                DestroyUnityObject(legacy.gameObject);
            }
        }

        private void CreatePrimitivePart(
            Transform parent,
            string name,
            PrimitiveType primitive,
            Vector3 localPosition,
            Vector3 localScale,
            Quaternion localRotation,
            Color color)
        {
            var part = GameObject.CreatePrimitive(primitive);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            part.transform.localRotation = localRotation;
            ApplyColor(part.GetComponent<Renderer>(), color);
            DestroyUnityObject(part.GetComponent<Collider>());
        }

        private void RebuildSimpleSlot(
            Transform anchor,
            ref GameObject visual,
            ref string appliedId,
            string itemId,
            string alternateId,
            PrimitiveType primitive,
            Vector3 scale,
            Color alternateColor,
            Color defaultColor)
        {
            if (anchor == null || (visual != null && appliedId == itemId))
            {
                return;
            }

            DestroyUnityObject(visual);
            visual = GameObject.CreatePrimitive(primitive);
            visual.name = "Equipped Decoration Visual";
            visual.transform.SetParent(anchor, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = scale;
            appliedId = itemId ?? string.Empty;
            ApplyColor(visual.GetComponent<Renderer>(), itemId == alternateId ? alternateColor : defaultColor);
            DestroyUnityObject(visual.GetComponent<Collider>());
        }

        private void BindManager()
        {
            var resolved = GameManager.Instance;
            if (resolved == manager)
            {
                return;
            }

            if (manager != null)
            {
                manager.DecorationChanged -= Refresh;
                manager.SaveDataReplaced -= Refresh;
            }

            manager = resolved;
            if (manager != null)
            {
                manager.DecorationChanged += Refresh;
                manager.SaveDataReplaced += Refresh;
            }
        }

        private void ApplyColor(Renderer target, Color color)
        {
            if (target == null)
            {
                return;
            }

            propertyBlock ??= new MaterialPropertyBlock();
            target.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_BaseColor", color);
            propertyBlock.SetColor("_Color", color);
            target.SetPropertyBlock(propertyBlock);
        }

        private static void ClearColorOverride(Renderer target)
        {
            if (target != null)
            {
                target.SetPropertyBlock(null);
            }
        }

        private void RebuildAccent(string accentId)
        {
            if (accentAnchor == null)
            {
                return;
            }

            if (accentVisual == null)
            {
                var existing = accentAnchor.Find("Equipped Accent Visual");
                accentVisual = existing != null ? existing.gameObject : null;
            }

            if (accentVisual != null && string.Equals(appliedAccentId, accentId, System.StringComparison.Ordinal))
            {
                return;
            }

            if (accentVisual != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(accentVisual);
                }
                else
                {
                    DestroyImmediate(accentVisual);
                }
            }

            accentVisual = new GameObject("Equipped Accent Visual");
            appliedAccentId = accentId ?? string.Empty;
            accentVisual.transform.SetParent(accentAnchor, false);
            if (accentId == DecorationCatalog.StarLampId)
            {
                var star = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                star.name = "Star Lamp Glow";
                star.transform.SetParent(accentVisual.transform, false);
                star.transform.localPosition = new Vector3(0f, 0.42f, 0f);
                star.transform.localScale = new Vector3(0.25f, 0.25f, 0.12f);
                var renderer = star.GetComponent<Renderer>();
                ApplyColor(renderer, new Color(1f, 0.82f, 0.26f, 1f));
                var collider = star.GetComponent<Collider>();
                if (collider != null)
                {
                    DestroyUnityObject(collider);
                }

                var light = accentVisual.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(1f, 0.72f, 0.32f);
                light.range = 2.4f;
                light.intensity = 0.55f;
            }
            else
            {
                var bottle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                bottle.name = "Milk Bottle";
                bottle.transform.SetParent(accentVisual.transform, false);
                bottle.transform.localPosition = new Vector3(0f, 0.22f, 0f);
                bottle.transform.localScale = new Vector3(0.14f, 0.24f, 0.14f);
                ApplyColor(bottle.GetComponent<Renderer>(), new Color(0.96f, 0.93f, 0.76f, 1f));
                var collider = bottle.GetComponent<Collider>();
                if (collider != null)
                {
                    DestroyUnityObject(collider);
                }
            }
        }

        private static void DestroyUnityObject(Object value)
        {
            if (value == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(value);
            }
            else
            {
                DestroyImmediate(value);
            }
        }

        private static Color GetWallColor(string id)
        {
            return id switch
            {
                DecorationCatalog.PeachWallId => new Color(1f, 0.67f, 0.57f, 1f),
                DecorationCatalog.StarlightWallId => new Color(0.32f, 0.38f, 0.66f, 1f),
                _ => new Color(0.96f, 0.89f, 0.7f, 1f)
            };
        }

        private static Color GetFloorColor(string id)
        {
            return id switch
            {
                DecorationCatalog.CheeseTileId => new Color(0.95f, 0.69f, 0.24f, 1f),
                DecorationCatalog.CloudMatId => new Color(0.78f, 0.88f, 0.94f, 1f),
                _ => new Color(0.83f, 0.58f, 0.3f, 1f)
            };
        }
    }
}
