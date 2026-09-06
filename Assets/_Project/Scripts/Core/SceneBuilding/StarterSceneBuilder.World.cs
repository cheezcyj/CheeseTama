using System.Collections.Generic;
using CheeseTama.Audio;
using CheeseTama.Data;
using CheeseTama.Environment;
using CheeseTama.Gameplay.Autonomy;
using CheeseTama.Gameplay.Growth;
using CheeseTama.Gameplay.Input;
using CheeseTama.Gameplay.Decorations;
using CheeseTama.Gameplay.Milk;
using CheeseTama.Gameplay.MiniGames;
using CheeseTama.Gameplay.NpcVisits;
using CheeseTama.Gameplay.Records;
using CheeseTama.Gameplay.Reset;
using CheeseTama.Gameplay.Story;
using CheeseTama.Gameplay.Snacks;
using CheeseTama.Platform;
using CheeseTama.Platform.Accounts;
using CheeseTama.Save;
using CheeseTama.UI;
using CheeseTama.Utilities;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CheeseTama.Core
{
    public static partial class StarterSceneBuilder
    {
        private static void ApplySavedMilkroomTheme(GameManager manager)
        {
            var themeId = MilkroomThemeController.MorningThemeId;
            var displaySave = ResolveSceneDisplaySave(manager);
            if (displaySave != null)
            {
                displaySave.EnsureRuntimeDefaults();
                themeId = displaySave.milkroomThemeId;
            }

            var themeController = Object.FindFirstObjectByType<MilkroomThemeController>();
            var lightingController = Object.FindFirstObjectByType<MilkroomLightingController>();
            var ambientController = Object.FindFirstObjectByType<MilkroomAmbientEventController>();
            themeController?.ApplyTheme(themeId);
            lightingController?.ApplyTheme(themeId);
            ambientController?.SetTheme(themeId);
        }

        private static void OrganizeMilkroomSceneHierarchy()
        {
            var sceneRoot = GameObject.Find("MilkroomSceneRoot");
            if (sceneRoot == null)
            {
                sceneRoot = new GameObject("MilkroomSceneRoot");
            }

            var cameraRig = GetOrCreateSceneGroup(sceneRoot.transform, "CameraRig");
            var lighting = GetOrCreateSceneGroup(sceneRoot.transform, "Lighting");
            var environment = GetOrCreateSceneGroup(sceneRoot.transform, "Environment");
            var character = GetOrCreateSceneGroup(sceneRoot.transform, "Character");
            var vfx = GetOrCreateSceneGroup(sceneRoot.transform, "VFX");
            var ui = GetOrCreateSceneGroup(sceneRoot.transform, "UI");

            ReparentIfFound("MainCamera", cameraRig);
            ReparentIfFound("Milkroom Camera", cameraRig);
            ReparentIfFound("Milkroom Key Light", lighting);
            ReparentIfFound("Milkroom Fill Light", lighting);
            ReparentIfFound("Milkroom Rim Light", lighting);
            ReparentIfFound("GlobalVolume", lighting);
            ReparentIfFound("Milkroom Background", environment);
            var autonomousMotionRoot = GameObject.Find("CheeseTama Autonomous Motion Root");
            if (autonomousMotionRoot != null)
            {
                ReparentIfFound("CheeseTama Autonomous Motion Root", character);
            }
            else
            {
                ReparentIfFound("CheeseTamaRoot", character);
                ReparentIfFound("CheeseTama Egg Placeholder", character);
            }
            var canvas = GameObject.Find("Milkroom Canvas");
            if (canvas != null) KeepOverlayCanvasAtSceneRoot(canvas.transform);
            ReparentIfFound("EventSystem", ui);

            var milkDrops = GetOrCreateSceneGroup(vfx, "MilkDrops");
            var softSparkles = GetOrCreateSceneGroup(vfx, "SoftSparkles");
            var cameraTarget = GetOrCreateSceneGroup(cameraRig, "CameraTarget");
            cameraTarget.localPosition = new Vector3(0f, -0.55f, 0.55f);
            milkDrops.localPosition = Vector3.zero;
            softSparkles.localPosition = Vector3.zero;
        }

        private static Transform GetOrCreateSceneGroup(Transform parent, string name)
        {
            var group = parent.Find(name);
            if (group != null)
            {
                return group;
            }

            var groupObject = new GameObject(name);
            groupObject.transform.SetParent(parent, false);
            return groupObject.transform;
        }

        private static void KeepOverlayCanvasAtSceneRoot(Transform canvasTransform)
        {
            var canvas = canvasTransform != null ? canvasTransform.GetComponent<Canvas>() : null;
            if (canvas == null || canvas.renderMode != RenderMode.ScreenSpaceOverlay
                || canvasTransform.parent == null) return;

            // Overlay canvases require a scene-root transform; a non-Canvas UI folder
            // is not a supported substitute, even when Canvas.isRootCanvas is true.
            canvasTransform.SetParent(null, false);
            canvasTransform.localScale = Vector3.one;
        }

        private static void ReparentIfFound(string objectName, Transform parent)
        {
            var target = GameObject.Find(objectName);
            if (target == null || target.transform == parent || target.transform.IsChildOf(parent))
            {
                return;
            }

            target.transform.SetParent(parent, true);
        }

        private static Camera EnsureCamera(string name)
        {
            var existing = Object.FindFirstObjectByType<Camera>();
            if (existing != null)
            {
                ConfigureMilkroomCamera(existing, name);
                return existing;
            }

            var cameraObject = new GameObject(name);
            var camera = cameraObject.AddComponent<Camera>();
            ConfigureMilkroomCamera(camera, name);
            return camera;
        }

        private static void ConfigureMilkroomCamera(Camera camera, string name)
        {
            camera.gameObject.name = name == "Milkroom Camera" ? "MainCamera" : name;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.96f, 0.92f, 0.84f);
            if (name == "Milkroom Camera" || name == "Debug Camera")
            {
                camera.orthographic = false;
                camera.fieldOfView = 33f;
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = 40f;
                camera.transform.position = new Vector3(0f, -0.78f, -11.8f);
                camera.transform.rotation = Quaternion.identity;

                if (name == "Milkroom Camera" && camera.GetComponent<MilkroomCameraFramer>() == null)
                {
                    camera.gameObject.AddComponent<MilkroomCameraFramer>();
                }
                else if (name == "Debug Camera")
                {
                    var framer = camera.GetComponent<MilkroomCameraFramer>();
                    if (framer != null)
                    {
                        DestroyObjectSafely(framer);
                    }
                }
            }
            else
            {
                camera.orthographic = true;
                camera.orthographicSize = 5f;
                camera.transform.position = new Vector3(0f, 0f, -10f);
                camera.transform.rotation = Quaternion.identity;
            }

            if (camera.gameObject.CompareTag("Untagged"))
            {
                camera.gameObject.tag = "MainCamera";
            }
        }

        private static void EnsureLight()
        {
            var palette = MilkroomThemePalette.For(MilkroomThemeController.MorningThemeId);
            var keyObject = GameObject.Find("Milkroom Key Light");
            if (keyObject == null)
            {
                keyObject = new GameObject("Milkroom Key Light");
            }

            var keyLight = keyObject.GetComponent<Light>();
            if (keyLight == null)
            {
                keyLight = keyObject.AddComponent<Light>();
            }

            keyLight.type = LightType.Directional;
            keyLight.color = Color.Lerp(palette.Glow, Color.white, MilkroomLightingController.KeyWhiteBlend);
            keyLight.intensity = MilkroomLightingController.DayKeyIntensity;
            keyLight.shadows = LightShadows.Soft;
            keyLight.shadowStrength = MilkroomLightingController.KeyShadowStrength;
            keyLight.shadowBias = MilkroomLightingController.KeyShadowBias;
            keyLight.shadowNormalBias = MilkroomLightingController.KeyShadowNormalBias;
            keyObject.transform.rotation = Quaternion.Euler(MilkroomLightingController.KeyRotationEuler);

            var fillObject = GameObject.Find("Milkroom Fill Light");
            if (fillObject == null)
            {
                fillObject = new GameObject("Milkroom Fill Light");
            }

            var fillLight = fillObject.GetComponent<Light>();
            if (fillLight == null)
            {
                fillLight = fillObject.AddComponent<Light>();
            }

            fillLight.type = LightType.Directional;
            fillLight.color = Color.Lerp(palette.WindowSky, Color.white, MilkroomLightingController.FillWhiteBlend);
            fillLight.intensity = MilkroomLightingController.DayFillIntensity;
            fillLight.shadows = LightShadows.None;
            fillObject.transform.rotation = Quaternion.Euler(MilkroomLightingController.FillRotationEuler);

            var rimObject = GameObject.Find("Milkroom Rim Light");
            if (rimObject == null)
            {
                rimObject = new GameObject("Milkroom Rim Light");
            }

            var rimLight = rimObject.GetComponent<Light>();
            if (rimLight == null)
            {
                rimLight = rimObject.AddComponent<Light>();
            }

            rimLight.type = LightType.Directional;
            rimLight.color = Color.Lerp(palette.Celestial, new Color(1f, 0.82f, 0.38f), 0.35f);
            rimLight.intensity = MilkroomLightingController.DayRimIntensity;
            rimLight.shadows = LightShadows.None;
            rimObject.transform.rotation = Quaternion.Euler(MilkroomLightingController.RimRotationEuler);

            var volumeObject = GameObject.Find("GlobalVolume");
            if (volumeObject == null)
            {
                volumeObject = new GameObject("GlobalVolume");
            }

            ConfigureGlobalVolumeIfAvailable(volumeObject);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = MilkroomLightingController.ResolveAmbientColor(
                MilkroomThemeController.MorningThemeId,
                palette);
        }

        private static void ConfigureGlobalVolumeIfAvailable(GameObject volumeObject)
        {
            var volumeType = System.Type.GetType("UnityEngine.Rendering.Volume, Unity.RenderPipelines.Core.Runtime");
            if (volumeType == null || volumeObject == null)
            {
                return;
            }

            var volume = volumeObject.GetComponent(volumeType);
            if (volume == null)
            {
                volume = volumeObject.AddComponent(volumeType);
            }

            SetVolumeMember(volume, "isGlobal", true);
            SetVolumeMember(volume, "priority", 0f);
            SetVolumeMember(volume, "weight", 0.35f);
        }

        private static void SetVolumeMember(Component volume, string memberName, object value)
        {
            if (volume == null)
            {
                return;
            }

            var type = volume.GetType();
            var property = type.GetProperty(memberName);
            if (property != null && property.CanWrite)
            {
                property.SetValue(volume, value);
                return;
            }

            var field = type.GetField(memberName);
            if (field != null)
            {
                field.SetValue(volume, value);
            }
        }

        private static void EnsureMilkroomBackground()
        {
            var existing = GameObject.Find("Milkroom Background");
            if (existing != null && Application.isPlaying && existing.transform.childCount > 0)
            {
                var existingRoot = existing.transform;
                var existingRoomShell = existingRoot.Find("RoomShell") ?? existingRoot;
                var existingPlayArea = existingRoot.Find("PlayArea") ?? existingRoot;
                var existingForeground = existingRoot.Find("Foreground") ?? existingRoot;
                var existingThemeVfxRoot = existingRoot.Find("ThemeVFXRoot")
                    ?? CreateGroupRoot(existingRoot, "ThemeVFXRoot");
                EnsureAmbientThemeVfx(existingThemeVfxRoot);
                AddMilkroomControllers(
                    existingRoot,
                    existingRoomShell,
                    existingRoot,
                    existingPlayArea,
                    existingForeground,
                    existingThemeVfxRoot);
                return;
            }

            if (existing != null)
            {
                DestroyObjectSafely(existing);
            }

            var root = new GameObject("Milkroom Background").transform;
            root.position = Vector3.zero;

            var roomShell = CreateGroupRoot(root, "RoomShell");
            var fridgeSet = CreateGroupRoot(root, "FridgeSet");
            var playArea = CreateGroupRoot(root, "PlayArea");
            var cozyChair = CreateGroupRoot(root, "CozyChair");
            var foreground = CreateGroupRoot(root, "Foreground");
            var themeVfxRoot = CreateGroupRoot(root, "ThemeVFXRoot");
            CreateAmbientThemeVfx(themeVfxRoot);

            CreateDioramaRoomShell(roomShell);
            CreateDioramaFridgeSet(fridgeSet);
            CreateDioramaCozyChair(cozyChair);
            CreateGroupRoot(playArea, "CheeseTamaAnchor").localPosition = new Vector3(0f, -0.28f, 0.05f);
            AddMilkroomControllers(root, roomShell, root, playArea, foreground, themeVfxRoot);
            EnsureGeneratedMilkroomProps(root);
        }

        private static void EnsureGeneratedMilkroomProps(Transform root)
        {
#if UNITY_EDITOR
            const float floorTop = -2.13f;

            // Hide the legacy primitive prop groups that the generated meshes replace.
            foreach (var groupName in new[] { "FridgeSet", "CozyChair" })
            {
                var group = root.Find(groupName);
                if (group != null)
                {
                    group.gameObject.SetActive(false);
                }
            }

            PlaceGeneratedProp(root, "Assets/Environments/Milkroom/Props/Fridge.prefab", "Fridge_Model",
                new Vector3(-1.75f, 0f, 2.0f), 2.1f, 180f, true, 0f, floorTop);
            PlaceGeneratedProp(root, "Assets/Environments/Milkroom/Props/MilkShelf.prefab", "MilkShelf_Model",
                new Vector3(2.65f, 0f, 2.34f), 1.3f, 180f, false, -0.15f, floorTop);
            PlaceGeneratedProp(root, "Assets/Environments/Milkroom/Props/CozyChair.prefab", "CozyChair_Model",
                new Vector3(-2.7f, 0f, 0.2f), 1.5f, 150f, true, 0f, floorTop);
            PlaceGeneratedProp(root, "Assets/Environments/Milkroom/Props/Window.prefab", "Window_Model",
                new Vector3(0.45f, 0f, 2.32f), 1.72f, 180f, false, -0.15f, floorTop);
            PlaceGeneratedProp(root, "Assets/Environments/Milkroom/Props/Rug.prefab", "Rug_Model",
                new Vector3(0.005f, 0f, 0.28f), RugPlacedHeight, 0f, true, 0f, floorTop);
            PlaceGeneratedProp(root, "Assets/Environments/Milkroom/Props/DresserTable.prefab", "DresserTable_Model",
                new Vector3(2.807f, 0f, 1.18f), 1.5f, 200f, true, 0f, floorTop);
            PlaceGeneratedProp(root, "Assets/Environments/Milkroom/Props/Chalkboard.prefab", "Chalkboard_Model",
                new Vector3(-2.78f, 0f, 2.41f), 1.18f, 0f, false, 0.05f, floorTop);
#endif
        }

#if UNITY_EDITOR
        private static void PlaceGeneratedProp(
            Transform parent,
            string prefabPath,
            string instanceName,
            Vector3 xzAnchor,
            float targetHeight,
            float yaw,
            bool onFloor,
            float centerY,
            float floorTop)
        {
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                return;
            }

            var existing = parent.Find(instanceName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var go = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab);
            go.name = instanceName;
            go.transform.SetParent(parent, false);
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = Vector3.one;

            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return;
            }

            var bounds = renderers[0].bounds;
            foreach (var r in renderers)
            {
                bounds.Encapsulate(r.bounds);
            }

            var scale = targetHeight / Mathf.Max(0.001f, bounds.size.y);
            go.transform.localScale = Vector3.one * scale;

            bounds = renderers[0].bounds;
            foreach (var r in renderers)
            {
                bounds.Encapsulate(r.bounds);
            }

            var posY = onFloor
                ? go.transform.position.y + (floorTop - bounds.min.y)
                : go.transform.position.y + (centerY - bounds.center.y);
            go.transform.position = new Vector3(xzAnchor.x, posY, xzAnchor.z);
        }
#endif

        private static void EnsureMilkroomPropInteractions(
            Transform canvasTransform,
            CheeseTamaPetInteractionController petInteraction)
        {
            if (!Application.isPlaying)
            {
                // Prop hotspots are runtime-only. Keeping their colliders out of
                // authored visual prefabs preserves the replacement asset bounds;
                // TryBindExistingSceneForRuntime recreates them before play begins.
                return;
            }

            var roomRoot = GameObject.Find("Milkroom Background")?.transform;
            var propController = roomRoot != null
                ? roomRoot.GetComponent<MilkroomPropController>()
                : null;
            if (propController == null || canvasTransform == null)
            {
                return;
            }

            propController.ConfigureInteractionRouting(
                route => TryOpenMilkroomPropRoute(canvasTransform, route),
                () => IsMilkroomPropInteractionBlocked(canvasTransform),
                () => petInteraction != null
                    && petInteraction.ContainsScreenPoint(Input.mousePosition));
            propController.ConfigureInteraction(
                roomRoot.Find("Fridge_Model"),
                MilkroomPropRoute.SnackPanel,
                usePreciseRendererHitTargets: true);
            propController.ConfigureInteraction(
                roomRoot.Find("MilkShelf_Model"),
                MilkroomPropRoute.MilkPanel);
            propController.ConfigureInteraction(
                roomRoot.Find("DresserTable_Model"),
                MilkroomPropRoute.CookingChoice);
            propController.ConfigureInteraction(
                roomRoot.Find("CozyChair_Model"),
                MilkroomPropRoute.SleepSchedule,
                usePreciseRendererHitTargets: true);
        }

        private static bool TryOpenMilkroomPropRoute(
            Transform canvasTransform,
            MilkroomPropRoute route)
        {
            switch (route)
            {
                case MilkroomPropRoute.SnackPanel:
                {
                    var controller = canvasTransform.GetComponent<SnackPanelController>();
                    if (controller == null)
                    {
                        return false;
                    }

                    controller.Open();
                    return true;
                }
                case MilkroomPropRoute.MilkPanel:
                {
                    var controller = canvasTransform.GetComponent<MilkPanelController>();
                    if (controller == null)
                    {
                        return false;
                    }

                    controller.Open();
                    return true;
                }
                case MilkroomPropRoute.CookingChoice:
                    return canvasTransform.GetComponent<CookingChoicePanelController>()?.Open() == true;
                case MilkroomPropRoute.SleepSchedule:
                {
                    // Keep the serialized legacy route value stable while the
                    // chair now performs the same immediate action as the
                    // bottom-bar Rest button.
                    var restButton = canvasTransform.Find("Bottom Action Bar/Sleep Button")
                        ?.GetComponent<Button>();
                    if (restButton == null
                        || !restButton.gameObject.activeInHierarchy
                        || !restButton.IsInteractable())
                    {
                        return false;
                    }

                    restButton.onClick.Invoke();
                    return true;
                }
                default:
                    return false;
            }
        }

        private static bool IsMilkroomPropInteractionBlocked(Transform canvasTransform)
        {
            if (canvasTransform == null || GameManager.Instance?.IsSleepScheduleActive == true)
            {
                return true;
            }

            foreach (var surfaceName in MilkroomPropBlockingSurfaceNames)
            {
                var surface = canvasTransform.Find(surfaceName);
                if (surface != null && surface.gameObject.activeInHierarchy)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsMilkroomPropInteractionBlockedExcept(
            Transform canvasTransform,
            string excludedSurfaceName)
        {
            if (canvasTransform == null || GameManager.Instance?.IsSleepScheduleActive == true)
            {
                return true;
            }

            foreach (var surfaceName in MilkroomPropBlockingSurfaceNames)
            {
                if (string.Equals(
                        surfaceName,
                        excludedSurfaceName,
                        System.StringComparison.Ordinal))
                {
                    continue;
                }

                var surface = canvasTransform.Find(surfaceName);
                if (surface != null && surface.gameObject.activeInHierarchy)
                {
                    return true;
                }
            }

            return false;
        }

        private static void EnsureAccessibleInputScopes(Transform canvasTransform)
        {
            if (canvasTransform == null)
            {
                return;
            }

            var pageScope = canvasTransform.GetComponent<KeyboardFocusScope>()
                ?? canvasTransform.gameObject.AddComponent<KeyboardFocusScope>();
            pageScope.Configure(canvasTransform, false, true, EventSystem.current);

            var descendants = canvasTransform.GetComponentsInChildren<Transform>(true);
            for (var index = 0; index < descendants.Length; index += 1)
            {
                var candidate = descendants[index];
                if (candidate == null
                    || candidate == canvasTransform
                    || !IsMilkroomBlockingSurfaceName(candidate.name))
                {
                    continue;
                }

                var modalScope = candidate.GetComponent<KeyboardFocusScope>()
                    ?? candidate.gameObject.AddComponent<KeyboardFocusScope>();
                modalScope.Configure(candidate, true, true, EventSystem.current);
            }
        }

        private static bool IsMilkroomBlockingSurfaceName(string candidateName)
        {
            for (var index = 0; index < MilkroomPropBlockingSurfaceNames.Length; index += 1)
            {
                if (string.Equals(
                        MilkroomPropBlockingSurfaceNames[index],
                        candidateName,
                        System.StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static System.Action<bool> CreateControlBlockingCallback(Transform canvasTransform)
        {
            var topMenu = canvasTransform != null
                ? canvasTransform.GetComponent<TopMenuController>()
                : null;
            var actionBar = canvasTransform?.Find("Bottom Action Bar")
                ?.GetComponent<BottomActionBarController>();
            var devPanel = canvasTransform != null
                ? canvasTransform.GetComponent<DevPanelController>()
                : null;
            var utilityBarTransform = canvasTransform?.Find("Milkroom Utility Bar");
            var utilityBarGroup = utilityBarTransform != null
                ? utilityBarTransform.GetComponent<CanvasGroup>()
                    ?? utilityBarTransform.gameObject.AddComponent<CanvasGroup>()
                : null;
            var notified = false;
            var topMenuWasEnabled = false;
            var actionBarWasEnabled = false;
            var devPanelWasEnabled = false;
            var utilityBarWasInteractable = false;
            var utilityBarWasBlockingRaycasts = false;

            return blocked =>
            {
                if (notified == blocked)
                {
                    return;
                }

                notified = blocked;
                if (blocked)
                {
                    topMenuWasEnabled = topMenu != null && topMenu.enabled;
                    actionBarWasEnabled = actionBar != null && actionBar.enabled;
                    devPanelWasEnabled = devPanel != null && devPanel.enabled;
                    utilityBarWasInteractable = utilityBarGroup != null
                        && utilityBarGroup.interactable;
                    utilityBarWasBlockingRaycasts = utilityBarGroup != null
                        && utilityBarGroup.blocksRaycasts;
                    if (topMenu != null) topMenu.enabled = false;
                    if (actionBar != null) actionBar.enabled = false;
                    if (devPanel != null) devPanel.enabled = false;
                    if (utilityBarGroup != null)
                    {
                        utilityBarGroup.interactable = false;
                        utilityBarGroup.blocksRaycasts = false;
                    }
                    return;
                }

                if (topMenu != null) topMenu.enabled = topMenuWasEnabled;
                if (actionBar != null) actionBar.enabled = actionBarWasEnabled;
                if (devPanel != null) devPanel.enabled = devPanelWasEnabled;
                if (utilityBarGroup != null)
                {
                    utilityBarGroup.interactable = utilityBarWasInteractable;
                    utilityBarGroup.blocksRaycasts = utilityBarWasBlockingRaycasts;
                }
            };
        }

        private static void MoveDirectChildIfExists(
            Transform currentParent,
            Transform nextParent,
            string childName)
        {
            if (currentParent == null || nextParent == null || string.IsNullOrWhiteSpace(childName))
            {
                return;
            }

            var child = currentParent.Find(childName);
            if (child != null && child.parent == currentParent)
            {
                child.SetParent(nextParent, false);
            }
        }

        private static Transform CreateGroupRoot(Transform parent, string name)
        {
            var group = new GameObject(name).transform;
            group.SetParent(parent, false);
            group.localPosition = Vector3.zero;
            group.localRotation = Quaternion.identity;
            group.localScale = Vector3.one;
            return group;
        }

        private static void AddMilkroomControllers(
            Transform root,
            Transform backgroundRoot,
            Transform midgroundRoot,
            Transform playAreaRoot,
            Transform foregroundRoot,
            Transform themeVfxRoot)
        {
            var propController = root.GetComponent<MilkroomPropController>();
            if (propController == null)
            {
                propController = root.gameObject.AddComponent<MilkroomPropController>();
            }

            propController.Configure(backgroundRoot, midgroundRoot, playAreaRoot, foregroundRoot, themeVfxRoot);

            var ambientController = root.GetComponent<MilkroomAmbientEventController>();
            if (ambientController == null)
            {
                ambientController = root.gameObject.AddComponent<MilkroomAmbientEventController>();
            }

            ambientController.Configure(themeVfxRoot);

            var themeController = root.GetComponent<MilkroomThemeController>();
            if (themeController == null)
            {
                themeController = root.gameObject.AddComponent<MilkroomThemeController>();
            }

            themeController.Configure(backgroundRoot, midgroundRoot, playAreaRoot, foregroundRoot, themeVfxRoot);
            themeController.ApplyTheme(MilkroomThemeController.MorningThemeId);

            var lightingController = root.GetComponent<MilkroomLightingController>();
            if (lightingController == null)
            {
                lightingController = root.gameObject.AddComponent<MilkroomLightingController>();
            }

            lightingController.ApplyTheme(MilkroomThemeController.MorningThemeId);

            var detailController = root.GetComponent<MilkroomPropDetailController>();
            if (detailController == null)
            {
                detailController = root.gameObject.AddComponent<MilkroomPropDetailController>();
            }

            detailController.Configure(root);
            var savedQuality = GameManager.Instance?.CurrentSave?.settings?.graphicsQualityPreset
                ?? (int)GraphicsQualityPreset.High;
            detailController.ApplyPreset(GraphicsQualityCatalog.Normalize(savedQuality));
        }

        private static void CreateDioramaRoomShell(Transform root)
        {
            CreateDecorPart(root, "BackWall", PrimitiveType.Cube, new Vector3(0f, -0.55f, 2.64f), new Vector3(7.9f, 3.15f, 0.24f), new Color(0.86f, 0.72f, 0.54f));
            CreateDecorPart(root, "LeftWall", PrimitiveType.Cube, new Vector3(-4.02f, -0.55f, 0.84f), new Vector3(0.24f, 3.15f, 3.6f), new Color(0.78f, 0.6f, 0.42f));
            CreateDecorPart(root, "RightWall", PrimitiveType.Cube, new Vector3(4.02f, -0.55f, 0.84f), new Vector3(0.24f, 3.15f, 3.6f), new Color(0.78f, 0.6f, 0.42f));
            CreateDecorPart(root, "Floor", PrimitiveType.Cube, new Vector3(0f, -2.24f, 0.9f), new Vector3(8.28f, 0.22f, 3.72f), new Color(0.5f, 0.29f, 0.14f));
            CreateDecorPart(root, "BackWall Baseboard", PrimitiveType.Cube, new Vector3(0f, -2.04f, 2.5f), new Vector3(7.65f, 0.18f, 0.1f), new Color(0.46f, 0.27f, 0.14f));
            CreateDecorPart(root, "LeftWall Baseboard", PrimitiveType.Cube, new Vector3(-3.82f, -2.04f, 0.76f), new Vector3(0.1f, 0.18f, 3.35f), new Color(0.46f, 0.27f, 0.14f));
            CreateDecorPart(root, "RightWall Baseboard", PrimitiveType.Cube, new Vector3(3.82f, -2.04f, 0.76f), new Vector3(0.1f, 0.18f, 3.35f), new Color(0.46f, 0.27f, 0.14f));
        }

        private static void CreateDioramaWindowSet(Transform root)
        {
            CreateDecorPart(root, "WindowGlass", PrimitiveType.Cube, new Vector3(-0.75f, 1.15f, 2.84f), new Vector3(2.35f, 1.45f, 0.08f), new Color(0.58f, 0.78f, 0.92f));
            CreateDecorPart(root, "Window Sun Glow", PrimitiveType.Sphere, new Vector3(-0.1f, 1.55f, 2.74f), new Vector3(0.5f, 0.5f, 0.08f), new Color(1f, 0.8f, 0.34f));
            CreateDecorPart(root, "Window Cloud Left", PrimitiveType.Sphere, new Vector3(-1.3f, 1.34f, 2.68f), new Vector3(0.46f, 0.16f, 0.05f), new Color(0.94f, 0.98f, 1f));
            CreateDecorPart(root, "Window Cloud Right", PrimitiveType.Sphere, new Vector3(-0.62f, 0.98f, 2.68f), new Vector3(0.42f, 0.14f, 0.05f), new Color(0.94f, 0.98f, 1f));

            var frameColor = new Color(0.96f, 0.83f, 0.58f);
            CreateDecorPart(root, "Window Arch Glow", PrimitiveType.Sphere, new Vector3(-0.75f, 1.92f, 2.72f), new Vector3(1.38f, 0.5f, 0.07f), new Color(0.95f, 0.83f, 0.56f));
            CreateDecorPart(root, "Window Arch Frame", PrimitiveType.Sphere, new Vector3(-0.75f, 1.91f, 2.57f), new Vector3(1.48f, 0.55f, 0.12f), new Color(0.68f, 0.39f, 0.18f));
            CreateDecorPart(root, "Window Arch Inner Cut", PrimitiveType.Sphere, new Vector3(-0.75f, 1.86f, 2.49f), new Vector3(1.22f, 0.38f, 0.12f), new Color(0.58f, 0.78f, 0.92f));
            CreateDecorPart(root, "WindowFrame Top", PrimitiveType.Cube, new Vector3(-0.75f, 1.92f, 2.62f), new Vector3(2.62f, 0.11f, 0.18f), frameColor);
            CreateDecorPart(root, "WindowFrame Bottom", PrimitiveType.Cube, new Vector3(-0.75f, 0.38f, 2.62f), new Vector3(2.62f, 0.13f, 0.18f), frameColor);
            CreateDecorPart(root, "WindowFrame Left", PrimitiveType.Cube, new Vector3(-2.06f, 1.15f, 2.62f), new Vector3(0.13f, 1.62f, 0.18f), frameColor);
            CreateDecorPart(root, "WindowFrame Right", PrimitiveType.Cube, new Vector3(0.56f, 1.15f, 2.62f), new Vector3(0.13f, 1.62f, 0.18f), frameColor);
            CreateDecorPart(root, "WindowFrame Vertical", PrimitiveType.Cube, new Vector3(-0.75f, 1.15f, 2.56f), new Vector3(0.09f, 1.5f, 0.14f), frameColor);
            CreateDecorPart(root, "WindowFrame Horizontal", PrimitiveType.Cube, new Vector3(-0.75f, 1.15f, 2.55f), new Vector3(2.42f, 0.09f, 0.14f), frameColor);
            CreateDecorPart(root, "Window Handle Left", PrimitiveType.Sphere, new Vector3(-0.88f, 1.08f, 2.42f), new Vector3(0.035f, 0.055f, 0.025f), new Color(0.82f, 0.56f, 0.24f));
            CreateDecorPart(root, "Window Handle Right", PrimitiveType.Sphere, new Vector3(-0.62f, 1.08f, 2.42f), new Vector3(0.035f, 0.055f, 0.025f), new Color(0.82f, 0.56f, 0.24f));

            CreateDecorPart(root, "Curtain Rod", PrimitiveType.Cylinder, new Vector3(-0.75f, 2.12f, 2.42f), new Vector3(0.035f, 1.92f, 0.035f), Quaternion.Euler(0f, 0f, 90f), new Color(0.64f, 0.38f, 0.19f));
            CreateDecorPart(root, "Curtain Rod Knob L", PrimitiveType.Sphere, new Vector3(-2.55f, 2.12f, 2.42f), new Vector3(0.12f, 0.12f, 0.08f), new Color(0.72f, 0.45f, 0.24f));
            CreateDecorPart(root, "Curtain Rod Knob R", PrimitiveType.Sphere, new Vector3(1.05f, 2.12f, 2.42f), new Vector3(0.12f, 0.12f, 0.08f), new Color(0.72f, 0.45f, 0.24f));
            CreateDecorPart(root, "Curtains Left", PrimitiveType.Cube, new Vector3(-2.38f, 1.12f, 2.46f), new Vector3(0.42f, 1.78f, 0.16f), new Color(0.98f, 0.82f, 0.64f));
            CreateDecorPart(root, "Curtains Right", PrimitiveType.Cube, new Vector3(0.88f, 1.12f, 2.46f), new Vector3(0.42f, 1.78f, 0.16f), new Color(0.98f, 0.82f, 0.64f));
            for (var i = 0; i < 4; i += 1)
            {
                var leftX = -2.55f + i * 0.13f;
                var rightX = 0.74f + i * 0.13f;
                var y = 1.18f - (i % 2) * 0.04f;
                CreateDecorPart(root, $"Curtain Left Fold {i + 1}", PrimitiveType.Cube, new Vector3(leftX, y, 2.28f), new Vector3(0.038f, 1.64f, 0.07f), new Color(1f, 0.9f, 0.72f));
                CreateDecorPart(root, $"Curtain Right Fold {i + 1}", PrimitiveType.Cube, new Vector3(rightX, y, 2.28f), new Vector3(0.038f, 1.64f, 0.07f), new Color(1f, 0.9f, 0.72f));
            }
            CreateDecorPart(root, "Curtains Left Tie", PrimitiveType.Cube, new Vector3(-2.24f, 0.85f, 2.3f), new Vector3(0.36f, 0.09f, 0.09f), new Color(0.78f, 0.48f, 0.24f));
            CreateDecorPart(root, "Curtains Right Tie", PrimitiveType.Cube, new Vector3(0.74f, 0.85f, 2.3f), new Vector3(0.36f, 0.09f, 0.09f), new Color(0.78f, 0.48f, 0.24f));
            CreateDecorPart(root, "Window Sill", PrimitiveType.Cube, new Vector3(-0.75f, 0.25f, 2.42f), new Vector3(2.75f, 0.13f, 0.24f), new Color(0.7f, 0.43f, 0.22f));
            CreateDecorPart(root, "Window Tiny Plant Pot", PrimitiveType.Cube, new Vector3(0.18f, 0.56f, 2.28f), new Vector3(0.22f, 0.16f, 0.08f), new Color(0.62f, 0.34f, 0.18f));
            CreateDecorPart(root, "Window Tiny Plant Leaf A", PrimitiveType.Sphere, new Vector3(0.08f, 0.75f, 2.22f), new Vector3(0.13f, 0.08f, 0.035f), new Color(0.34f, 0.62f, 0.34f));
            CreateDecorPart(root, "Window Tiny Plant Leaf B", PrimitiveType.Sphere, new Vector3(0.25f, 0.76f, 2.22f), new Vector3(0.13f, 0.08f, 0.035f), new Color(0.38f, 0.68f, 0.38f));
        }

        private static void CreateDioramaFridgeSet(Transform root)
        {
            CreateDecorPart(root, "Fridge Body Rounded", PrimitiveType.Cube, new Vector3(-3.35f, -0.55f, 1.72f), new Vector3(0.9f, 1.65f, 0.62f), new Color(0.94f, 0.9f, 0.78f));
            CreateDecorPart(root, "Fridge Top Round", PrimitiveType.Sphere, new Vector3(-3.35f, 0.32f, 1.72f), new Vector3(0.46f, 0.18f, 0.32f), new Color(0.98f, 0.95f, 0.84f));
            CreateDecorPart(root, "Fridge Top Left Corner", PrimitiveType.Sphere, new Vector3(-3.78f, 0.27f, 1.44f), new Vector3(0.12f, 0.18f, 0.12f), new Color(0.98f, 0.95f, 0.84f));
            CreateDecorPart(root, "Fridge Top Right Corner", PrimitiveType.Sphere, new Vector3(-2.92f, 0.27f, 1.44f), new Vector3(0.12f, 0.18f, 0.12f), new Color(0.98f, 0.95f, 0.84f));
            CreateDecorPart(root, "Fridge Lower Left Corner", PrimitiveType.Sphere, new Vector3(-3.78f, -1.34f, 1.44f), new Vector3(0.12f, 0.12f, 0.1f), new Color(0.9f, 0.84f, 0.72f));
            CreateDecorPart(root, "Fridge Lower Right Corner", PrimitiveType.Sphere, new Vector3(-2.92f, -1.34f, 1.44f), new Vector3(0.12f, 0.12f, 0.1f), new Color(0.9f, 0.84f, 0.72f));
            CreateDecorPart(root, "Fridge Soft Shine", PrimitiveType.Cube, new Vector3(-3.66f, -0.2f, 1.26f), new Vector3(0.055f, 1.02f, 0.035f), new Color(1f, 0.98f, 0.88f));
            CreateDecorPart(root, "Fridge Door Split", PrimitiveType.Cube, new Vector3(-3.35f, -0.32f, 1.36f), new Vector3(0.76f, 0.035f, 0.06f), new Color(0.74f, 0.58f, 0.4f));
            CreateDecorPart(root, "Fridge Handle", PrimitiveType.Cylinder, new Vector3(-2.94f, -0.36f, 1.3f), new Vector3(0.04f, 0.28f, 0.04f), new Color(0.68f, 0.43f, 0.22f));
            CreateDecorPart(root, "Fridge Face Eye L", PrimitiveType.Sphere, new Vector3(-3.48f, -0.7f, 1.29f), new Vector3(0.048f, 0.048f, 0.032f), new Color(0.24f, 0.14f, 0.08f));
            CreateDecorPart(root, "Fridge Face Eye R", PrimitiveType.Sphere, new Vector3(-3.22f, -0.7f, 1.29f), new Vector3(0.048f, 0.048f, 0.032f), new Color(0.24f, 0.14f, 0.08f));
            CreateDecorPart(root, "Fridge Smile", PrimitiveType.Cube, new Vector3(-3.35f, -0.84f, 1.26f), new Vector3(0.16f, 0.025f, 0.025f), new Color(0.24f, 0.14f, 0.08f));
            CreateDecorPart(root, "Fridge Milk Memo", PrimitiveType.Cube, new Vector3(-3.12f, 0.02f, 1.27f), new Vector3(0.22f, 0.18f, 0.035f), new Color(1f, 0.76f, 0.34f));
            CreateDecorPart(root, "Fridge Star Magnet", PrimitiveType.Sphere, new Vector3(-3.5f, 0.05f, 1.26f), new Vector3(0.08f, 0.08f, 0.025f), new Color(1f, 0.76f, 0.22f));
            CreateDecorPart(root, "Fridge Blue Memo", PrimitiveType.Cube, new Vector3(-3.56f, -0.18f, 1.25f), new Vector3(0.18f, 0.14f, 0.03f), new Color(0.64f, 0.82f, 0.92f));
        }

        private static void CreateDioramaMilkShelfSet(Transform root)
        {
            CreateDecorPart(root, "MilkShelf Back", PrimitiveType.Cube, new Vector3(1.75f, 0f, 2.38f), new Vector3(1.65f, 1.18f, 0.16f), new Color(0.5f, 0.3f, 0.16f));
            CreateDecorPart(root, "MilkShelf Top", PrimitiveType.Cube, new Vector3(1.75f, 0.56f, 2.08f), new Vector3(1.82f, 0.09f, 0.2f), new Color(0.68f, 0.42f, 0.22f));
            CreateDecorPart(root, "MilkShelf Middle", PrimitiveType.Cube, new Vector3(1.75f, 0.05f, 2.08f), new Vector3(1.82f, 0.09f, 0.2f), new Color(0.68f, 0.42f, 0.22f));
            CreateDecorPart(root, "MilkShelf Bottom", PrimitiveType.Cube, new Vector3(1.75f, -0.48f, 2.08f), new Vector3(1.82f, 0.09f, 0.2f), new Color(0.68f, 0.42f, 0.22f));
            CreateDecorPart(root, "MilkShelf Left Upright", PrimitiveType.Cube, new Vector3(0.9f, 0.06f, 1.98f), new Vector3(0.08f, 1.28f, 0.12f), new Color(0.58f, 0.34f, 0.17f));
            CreateDecorPart(root, "MilkShelf Right Upright", PrimitiveType.Cube, new Vector3(2.6f, 0.06f, 1.98f), new Vector3(0.08f, 1.28f, 0.12f), new Color(0.58f, 0.34f, 0.17f));
            CreateDecorPart(root, "MilkShelf Bracket L", PrimitiveType.Cube, new Vector3(1.0f, -0.22f, 1.96f), new Vector3(0.1f, 0.5f, 0.08f), Quaternion.Euler(0f, 0f, -35f), new Color(0.52f, 0.3f, 0.15f));
            CreateDecorPart(root, "MilkShelf Bracket R", PrimitiveType.Cube, new Vector3(2.5f, -0.22f, 1.96f), new Vector3(0.1f, 0.5f, 0.08f), Quaternion.Euler(0f, 0f, 35f), new Color(0.52f, 0.3f, 0.15f));

            for (var i = 0; i < 4; i += 1)
            {
                CreateMilkBottle(root, $"MilkShelf Bottle Top {i + 1}", new Vector3(1.13f + i * 0.38f, 0.82f, 1.94f), 0.32f);
            }

            for (var i = 0; i < 3; i += 1)
            {
                CreateMilkBottle(root, $"MilkShelf Bottle Lower {i + 1}", new Vector3(1.32f + i * 0.42f, 0.3f, 1.94f), 0.28f);
            }

            CreateCheeseBlock(root, "Shelf Cheese Sample", new Vector3(2.44f, -0.2f, 1.88f), 0.22f);
            CreateDecorPart(root, "Shelf Vine Stem", PrimitiveType.Cube, new Vector3(2.66f, 0.9f, 1.98f), new Vector3(0.035f, 0.62f, 0.035f), Quaternion.Euler(0f, 0f, -12f), new Color(0.25f, 0.46f, 0.22f));
            for (var i = 0; i < 5; i += 1)
            {
                var y = 1.14f - i * 0.15f;
                var x = 2.62f + (i % 2 == 0 ? -0.08f : 0.08f);
                CreateDecorPart(root, $"Shelf Vine Leaf {i + 1}", PrimitiveType.Sphere, new Vector3(x, y, 1.9f), new Vector3(0.1f, 0.055f, 0.028f), new Color(0.34f, 0.63f, 0.34f));
            }
        }

        private static void CreateDioramaBlendingTableSet(Transform root)
        {
            CreateDecorPart(root, "BlendingTable Top", PrimitiveType.Cube, new Vector3(2.9f, -1.15f, 1.0f), new Vector3(1.25f, 0.18f, 0.58f), new Color(0.66f, 0.39f, 0.2f));
            CreateDecorPart(root, "BlendingTable Cloth", PrimitiveType.Cube, new Vector3(2.9f, -1.03f, 0.7f), new Vector3(1.36f, 0.09f, 0.12f), new Color(1f, 0.88f, 0.64f));
            CreateDecorPart(root, "BlendingTable Body", PrimitiveType.Cube, new Vector3(2.9f, -1.42f, 1.06f), new Vector3(1.18f, 0.62f, 0.42f), new Color(0.58f, 0.33f, 0.16f));
            CreateDecorPart(root, "BlendingTable Leg L", PrimitiveType.Cube, new Vector3(2.42f, -1.62f, 1.02f), new Vector3(0.09f, 0.76f, 0.09f), new Color(0.48f, 0.27f, 0.13f));
            CreateDecorPart(root, "BlendingTable Leg R", PrimitiveType.Cube, new Vector3(3.38f, -1.62f, 1.02f), new Vector3(0.09f, 0.76f, 0.09f), new Color(0.48f, 0.27f, 0.13f));
            for (var i = 0; i < 3; i += 1)
            {
                var x = 2.5f + i * 0.4f;
                CreateDecorPart(root, $"Blending Drawer {i + 1}", PrimitiveType.Cube, new Vector3(x, -1.4f, 0.78f), new Vector3(0.32f, 0.22f, 0.045f), new Color(0.7f, 0.43f, 0.22f));
                CreateDecorPart(root, $"Blending Drawer Pull {i + 1}", PrimitiveType.Sphere, new Vector3(x, -1.4f, 0.73f), new Vector3(0.04f, 0.04f, 0.022f), new Color(0.98f, 0.7f, 0.28f));
            }

            CreateDecorPart(root, "Blending Bowl", PrimitiveType.Sphere, new Vector3(2.72f, -0.92f, 0.66f), new Vector3(0.28f, 0.12f, 0.12f), new Color(0.82f, 0.94f, 0.98f));
            CreateDecorPart(root, "Blending Spoon", PrimitiveType.Cube, new Vector3(3.1f, -0.86f, 0.62f), new Vector3(0.45f, 0.035f, 0.03f), new Color(0.82f, 0.6f, 0.34f));
            CreateMilkBottle(root, "Blending Milk Bottle", new Vector3(3.32f, -0.78f, 0.58f), 0.34f);
            CreateDecorPart(root, "Blender Base", PrimitiveType.Cube, new Vector3(3.55f, -0.86f, 0.66f), new Vector3(0.24f, 0.18f, 0.12f), new Color(0.9f, 0.78f, 0.58f));
            CreateDecorPart(root, "Blender Jar", PrimitiveType.Capsule, new Vector3(3.55f, -0.62f, 0.64f), new Vector3(0.14f, 0.2f, 0.08f), new Color(0.78f, 0.9f, 0.98f));
            CreateDecorPart(root, "Blender Milk Fill", PrimitiveType.Sphere, new Vector3(3.55f, -0.68f, 0.58f), new Vector3(0.12f, 0.06f, 0.03f), new Color(0.98f, 0.94f, 0.78f));
        }

        private static void CreateDioramaChalkboardSet(Transform root)
        {
            CreateDecorPart(root, "Chalkboard", PrimitiveType.Cube, new Vector3(-2.52f, 1.15f, 2.54f), new Vector3(0.92f, 0.72f, 0.08f), new Color(0.15f, 0.25f, 0.2f));
            CreateDecorPart(root, "Chalkboard Frame Top", PrimitiveType.Cube, new Vector3(-2.52f, 1.55f, 2.47f), new Vector3(1.08f, 0.08f, 0.06f), new Color(0.58f, 0.34f, 0.17f));
            CreateDecorPart(root, "Chalkboard Frame Bottom", PrimitiveType.Cube, new Vector3(-2.52f, 0.75f, 2.47f), new Vector3(1.08f, 0.08f, 0.06f), new Color(0.58f, 0.34f, 0.17f));
            CreateDecorPart(root, "Chalkboard Frame Left", PrimitiveType.Cube, new Vector3(-3.06f, 1.15f, 2.47f), new Vector3(0.08f, 0.82f, 0.06f), new Color(0.58f, 0.34f, 0.17f));
            CreateDecorPart(root, "Chalkboard Frame Right", PrimitiveType.Cube, new Vector3(-1.98f, 1.15f, 2.47f), new Vector3(0.08f, 0.82f, 0.06f), new Color(0.58f, 0.34f, 0.17f));
            CreateWorldLabel(root, "Chalkboard Text", "\uC6B0\uC720\uB294\n\uB9C8\uBC95", new Vector3(-2.52f, 1.16f, 2.39f), 0.075f, new Color(1f, 0.9f, 0.62f));
            CreateDecorPart(root, "Chalkboard Hanger L", PrimitiveType.Cube, new Vector3(-2.82f, 1.78f, 2.44f), new Vector3(0.03f, 0.42f, 0.03f), Quaternion.Euler(0f, 0f, -34f), new Color(0.64f, 0.42f, 0.2f));
            CreateDecorPart(root, "Chalkboard Hanger R", PrimitiveType.Cube, new Vector3(-2.22f, 1.78f, 2.44f), new Vector3(0.03f, 0.42f, 0.03f), Quaternion.Euler(0f, 0f, 34f), new Color(0.64f, 0.42f, 0.2f));
            CreateDecorPart(root, "Chalkboard Cheese Doodle", PrimitiveType.Sphere, new Vector3(-2.86f, 0.88f, 2.38f), new Vector3(0.08f, 0.06f, 0.018f), new Color(1f, 0.78f, 0.28f));
            CreateDecorPart(root, "Chalkboard Star Doodle", PrimitiveType.Sphere, new Vector3(-2.18f, 1.42f, 2.38f), new Vector3(0.045f, 0.045f, 0.016f), new Color(1f, 0.92f, 0.5f));
        }

        private static void CreateDioramaRug(Transform root)
        {
            CreateDecorPart(root, "Rug Base", PrimitiveType.Sphere, new Vector3(0f, -2.05f, 0.62f), new Vector3(1.92f, 0.13f, 0.82f), new Color(0.9f, 0.78f, 0.56f));
            CreateDecorPart(root, "Rug Soft Center", PrimitiveType.Sphere, new Vector3(0f, -2.0f, 0.52f), new Vector3(1.55f, 0.08f, 0.62f), new Color(1f, 0.9f, 0.68f));
            CreateDecorPart(root, "Rug Paw Center", PrimitiveType.Sphere, new Vector3(0f, -1.95f, 0.42f), new Vector3(0.32f, 0.04f, 0.08f), new Color(0.78f, 0.62f, 0.42f));
            CreateDecorPart(root, "Rug Paw Toe L", PrimitiveType.Sphere, new Vector3(-0.34f, -1.9f, 0.42f), new Vector3(0.13f, 0.035f, 0.06f), new Color(0.82f, 0.66f, 0.46f));
            CreateDecorPart(root, "Rug Paw Toe C", PrimitiveType.Sphere, new Vector3(0f, -1.86f, 0.42f), new Vector3(0.13f, 0.035f, 0.06f), new Color(0.82f, 0.66f, 0.46f));
            CreateDecorPart(root, "Rug Paw Toe R", PrimitiveType.Sphere, new Vector3(0.34f, -1.9f, 0.42f), new Vector3(0.13f, 0.035f, 0.06f), new Color(0.82f, 0.66f, 0.46f));
            for (var i = 0; i < 24; i += 1)
            {
                var angle = i / 24f * Mathf.PI * 2f;
                var x = Mathf.Cos(angle) * 1.78f;
                var z = 0.58f + Mathf.Sin(angle) * 0.72f;
                var width = 0.18f + (i % 3) * 0.018f;
                CreateDecorPart(root, $"Rug Tuft Rim {i + 1}", PrimitiveType.Sphere, new Vector3(x, -1.93f, z), new Vector3(width, 0.06f, 0.1f), Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f), new Color(0.96f, 0.86f, 0.66f));
            }

            for (var i = 0; i < 8; i += 1)
            {
                var x = -1.0f + i * 0.28f;
                CreateDecorPart(root, $"Rug Soft Stitch {i + 1}", PrimitiveType.Cube, new Vector3(x, -1.88f, 0.05f + (i % 2) * 0.08f), new Vector3(0.12f, 0.018f, 0.025f), Quaternion.Euler(0f, 18f, 0f), new Color(0.88f, 0.74f, 0.54f));
            }
        }

        private static void CreateDioramaCozyChair(Transform root)
        {
            CreateDecorPart(root, "CozyChair Back", PrimitiveType.Cube, new Vector3(-3.55f, -1.1f, 0.42f), new Vector3(0.92f, 0.8f, 0.34f), new Color(0.58f, 0.4f, 0.28f));
            CreateDecorPart(root, "CozyChair Seat", PrimitiveType.Cube, new Vector3(-3.55f, -1.62f, 0.12f), new Vector3(1.02f, 0.26f, 0.58f), new Color(0.72f, 0.52f, 0.36f));
            CreateDecorPart(root, "CozyChair Back Cushion", PrimitiveType.Sphere, new Vector3(-3.55f, -1.04f, 0.18f), new Vector3(0.78f, 0.42f, 0.12f), new Color(0.9f, 0.78f, 0.62f));
            CreateDecorPart(root, "CozyChair Seat Cushion", PrimitiveType.Sphere, new Vector3(-3.55f, -1.56f, -0.08f), new Vector3(0.86f, 0.18f, 0.26f), new Color(0.92f, 0.8f, 0.64f));
            CreateDecorPart(root, "CozyChair Arm L", PrimitiveType.Cube, new Vector3(-4.12f, -1.38f, 0.22f), new Vector3(0.16f, 0.52f, 0.48f), new Color(0.5f, 0.31f, 0.18f));
            CreateDecorPart(root, "CozyChair Arm R", PrimitiveType.Cube, new Vector3(-2.98f, -1.38f, 0.22f), new Vector3(0.16f, 0.52f, 0.48f), new Color(0.5f, 0.31f, 0.18f));
            CreateDecorPart(root, "CozyChair Arm L Round", PrimitiveType.Cylinder, new Vector3(-4.12f, -1.08f, 0.03f), new Vector3(0.08f, 0.34f, 0.08f), Quaternion.Euler(90f, 0f, 0f), new Color(0.64f, 0.42f, 0.23f));
            CreateDecorPart(root, "CozyChair Arm R Round", PrimitiveType.Cylinder, new Vector3(-2.98f, -1.08f, 0.03f), new Vector3(0.08f, 0.34f, 0.08f), Quaternion.Euler(90f, 0f, 0f), new Color(0.64f, 0.42f, 0.23f));
            CreateDecorPart(root, "CozyChair Butter Cushion", PrimitiveType.Cube, new Vector3(-3.55f, -1.22f, -0.04f), new Vector3(0.46f, 0.32f, 0.12f), new Color(1f, 0.72f, 0.28f));
            CreateDecorPart(root, "Butter Cushion Hole A", PrimitiveType.Sphere, new Vector3(-3.67f, -1.2f, -0.12f), new Vector3(0.05f, 0.045f, 0.018f), new Color(0.85f, 0.48f, 0.1f));
            CreateDecorPart(root, "Butter Cushion Hole B", PrimitiveType.Sphere, new Vector3(-3.45f, -1.26f, -0.12f), new Vector3(0.04f, 0.04f, 0.018f), new Color(0.85f, 0.48f, 0.1f));
            CreateDecorPart(root, "CozyChair Leg L", PrimitiveType.Cube, new Vector3(-3.96f, -1.88f, 0.2f), new Vector3(0.12f, 0.42f, 0.12f), new Color(0.48f, 0.28f, 0.14f));
            CreateDecorPart(root, "CozyChair Leg R", PrimitiveType.Cube, new Vector3(-3.14f, -1.88f, 0.2f), new Vector3(0.12f, 0.42f, 0.12f), new Color(0.48f, 0.28f, 0.14f));
        }

        private static void CreateDioramaLamps(Transform root)
        {
            CreateDecorPart(root, "Pendant Cord", PrimitiveType.Cube, new Vector3(0.2f, 2.55f, 1.55f), new Vector3(0.035f, 0.58f, 0.035f), new Color(0.34f, 0.2f, 0.1f));
            for (var i = 0; i < 4; i += 1)
            {
                CreateDecorPart(root, $"Pendant Chain Link {i + 1}", PrimitiveType.Cube, new Vector3(0.2f, 2.74f - i * 0.13f, 1.48f), new Vector3(0.05f, 0.07f, 0.025f), Quaternion.Euler(0f, 0f, i % 2 == 0 ? 45f : -45f), new Color(0.36f, 0.22f, 0.12f));
            }

            CreateDecorPart(root, "Pendant Warm Shade", PrimitiveType.Sphere, new Vector3(0.2f, 2.16f, 1.55f), new Vector3(0.36f, 0.2f, 0.24f), new Color(1f, 0.74f, 0.32f));
            for (var i = 0; i < 5; i += 1)
            {
                CreateDecorPart(root, $"Pendant Shade Scallop {i + 1}", PrimitiveType.Sphere, new Vector3(-0.08f + i * 0.14f, 2.04f, 1.36f), new Vector3(0.08f, 0.05f, 0.035f), new Color(1f, 0.86f, 0.46f));
            }

            CreateDecorPart(root, "Pendant Bulb", PrimitiveType.Sphere, new Vector3(0.2f, 1.98f, 1.42f), new Vector3(0.12f, 0.16f, 0.08f), new Color(1f, 0.96f, 0.72f));
            CreateDecorPart(root, "Pendant Warm Glow", PrimitiveType.Sphere, new Vector3(0.2f, 1.94f, 1.48f), new Vector3(0.54f, 0.22f, 0.28f), new Color(1f, 0.8f, 0.42f));
            CreateStarLamp(root, "Left Star Lamp", new Vector3(-3.86f, 1.68f, 2.34f), 0.24f);
            CreateStarLamp(root, "Window Hanging Star", new Vector3(-0.28f, 1.7f, 2.32f), 0.14f);
        }

        private static void CreateDioramaProps(Transform root)
        {
            CreateDecorPart(root, "Plant Pot", PrimitiveType.Cube, new Vector3(0.84f, -1.68f, 2.12f), new Vector3(0.34f, 0.24f, 0.28f), new Color(0.56f, 0.31f, 0.17f));
            CreateDecorPart(root, "Plant Leaf L", PrimitiveType.Sphere, new Vector3(0.68f, -1.38f, 2.04f), new Vector3(0.2f, 0.12f, 0.08f), new Color(0.32f, 0.58f, 0.32f));
            CreateDecorPart(root, "Plant Leaf R", PrimitiveType.Sphere, new Vector3(1.0f, -1.36f, 2.04f), new Vector3(0.2f, 0.12f, 0.08f), new Color(0.36f, 0.64f, 0.36f));
            CreateDecorPart(root, "Plant Leaf Tall A", PrimitiveType.Sphere, new Vector3(0.82f, -1.18f, 2.02f), new Vector3(0.12f, 0.24f, 0.06f), Quaternion.Euler(0f, 0f, -18f), new Color(0.28f, 0.55f, 0.28f));
            CreateDecorPart(root, "Plant Leaf Tall B", PrimitiveType.Sphere, new Vector3(0.98f, -1.18f, 2f), new Vector3(0.12f, 0.22f, 0.06f), Quaternion.Euler(0f, 0f, 22f), new Color(0.38f, 0.68f, 0.38f));
            CreateDecorPart(root, "Left Plant Pot", PrimitiveType.Cube, new Vector3(-4.18f, -1.64f, 1.68f), new Vector3(0.36f, 0.22f, 0.18f), new Color(0.58f, 0.33f, 0.18f));
            for (var i = 0; i < 6; i += 1)
            {
                var angle = -45f + i * 18f;
                var x = -4.18f + Mathf.Cos(angle * Mathf.Deg2Rad) * 0.18f;
                var y = -1.34f + i % 3 * 0.08f;
                CreateDecorPart(root, $"Left Plant Leaf {i + 1}", PrimitiveType.Sphere, new Vector3(x, y, 1.58f), new Vector3(0.22f, 0.1f, 0.05f), Quaternion.Euler(0f, 0f, angle), new Color(0.3f, 0.56f + i * 0.015f, 0.3f));
            }

            CreateDecorPart(root, "Wall Memo A", PrimitiveType.Cube, new Vector3(3.75f, 0.78f, 2.52f), new Vector3(0.28f, 0.22f, 0.035f), new Color(1f, 0.86f, 0.52f));
            CreateDecorPart(root, "Wall Memo B", PrimitiveType.Cube, new Vector3(3.42f, 0.42f, 2.52f), new Vector3(0.22f, 0.18f, 0.035f), new Color(0.78f, 0.92f, 1f));
            CreateDecorPart(root, "Wall Memo Pin A", PrimitiveType.Sphere, new Vector3(3.75f, 0.88f, 2.47f), new Vector3(0.025f, 0.025f, 0.012f), new Color(0.72f, 0.36f, 0.16f));
            CreateDecorPart(root, "Wall Memo Pin B", PrimitiveType.Sphere, new Vector3(3.42f, 0.5f, 2.47f), new Vector3(0.025f, 0.025f, 0.012f), new Color(0.72f, 0.36f, 0.16f));
            CreateMilkBottle(root, "Loose Milk Bottle", new Vector3(-0.96f, -1.66f, 0.12f), 0.3f);
            CreateCheeseBlock(root, "Foreground Cheese Cube", new Vector3(1.6f, -1.74f, -0.28f), 0.24f);
            CreateDecorPart(root, "Foreground Soft Milk Drop L", PrimitiveType.Sphere, new Vector3(-1.8f, -2.02f, -0.68f), new Vector3(0.22f, 0.045f, 0.08f), new Color(0.92f, 0.86f, 0.74f));
            CreateDecorPart(root, "Foreground Soft Milk Drop L Small", PrimitiveType.Sphere, new Vector3(-2.14f, -2f, -0.62f), new Vector3(0.11f, 0.03f, 0.04f), new Color(0.94f, 0.9f, 0.78f));
            CreateDecorPart(root, "Foreground Soft Milk Drop R", PrimitiveType.Sphere, new Vector3(2.1f, -2.02f, -0.62f), new Vector3(0.26f, 0.05f, 0.09f), new Color(0.92f, 0.86f, 0.74f));
            CreateDecorPart(root, "Foreground Soft Milk Drop R Small", PrimitiveType.Sphere, new Vector3(2.48f, -2f, -0.58f), new Vector3(0.12f, 0.03f, 0.04f), new Color(0.94f, 0.9f, 0.78f));
        }

        private static void CreateReferenceMilkroomComposition(Transform root)
        {
            CreateRug(root);
            CreateWindow(root);
            CreateLeftFurniture(root);
            CreateRightFurniture(root);
            CreateBlendingTable(root);
            CreateShelfGroup(root);
            CreateHangingLights(root);
            CreateMilkroomForeground(root);
            CreateDecorPart(root, "Reference Warm Window Wash", PrimitiveType.Sphere, new Vector3(0f, 1.64f, 2.22f), new Vector3(3.4f, 1.6f, 0.06f), new Color(1f, 0.78f, 0.42f));
            CreateDecorPart(root, "Reference Floor Sun Patch", PrimitiveType.Cube, new Vector3(0.24f, -2.08f, 0.12f), new Vector3(2.55f, 0.022f, 0.2f), Quaternion.Euler(0f, 25f, 0f), new Color(1f, 0.75f, 0.34f));
        }

        private static void CreateRug(Transform root)
        {
            CreateDecorPart(root, "Rug Outer Rim", PrimitiveType.Sphere, new Vector3(0f, -1.96f, 0.96f), new Vector3(2.85f, 0.34f, 0.68f), new Color(0.92f, 0.82f, 0.63f));
            CreateDecorPart(root, "Rug Inner Cream", PrimitiveType.Sphere, new Vector3(0f, -1.94f, 0.86f), new Vector3(2.38f, 0.22f, 0.52f), new Color(1f, 0.92f, 0.72f));
            CreateDecorPart(root, "Rug Paw Center", PrimitiveType.Sphere, new Vector3(0f, -1.93f, 0.76f), new Vector3(0.42f, 0.07f, 0.05f), new Color(0.84f, 0.7f, 0.5f));
            CreateDecorPart(root, "Rug Paw Toe L", PrimitiveType.Sphere, new Vector3(-0.38f, -1.8f, 0.75f), new Vector3(0.18f, 0.05f, 0.04f), new Color(0.86f, 0.72f, 0.52f));
            CreateDecorPart(root, "Rug Paw Toe LC", PrimitiveType.Sphere, new Vector3(-0.14f, -1.74f, 0.75f), new Vector3(0.18f, 0.05f, 0.04f), new Color(0.86f, 0.72f, 0.52f));
            CreateDecorPart(root, "Rug Paw Toe RC", PrimitiveType.Sphere, new Vector3(0.14f, -1.74f, 0.75f), new Vector3(0.18f, 0.05f, 0.04f), new Color(0.86f, 0.72f, 0.52f));
            CreateDecorPart(root, "Rug Paw Toe R", PrimitiveType.Sphere, new Vector3(0.38f, -1.8f, 0.75f), new Vector3(0.18f, 0.05f, 0.04f), new Color(0.86f, 0.72f, 0.52f));
        }

        private static void CreateWindow(Transform root)
        {
            CreateDecorPart(root, "Window Glow", PrimitiveType.Sphere, new Vector3(0f, 1.8f, 2.02f), new Vector3(3.15f, 2.15f, 0.05f), new Color(1f, 0.86f, 0.48f));
            CreateDecorPart(root, "Window Sky", PrimitiveType.Cube, new Vector3(0f, 1.72f, 1.72f), new Vector3(2.35f, 1.58f, 0.06f), new Color(0.64f, 0.83f, 0.95f));
            CreateDecorPart(root, "Window Sun Patch", PrimitiveType.Sphere, new Vector3(0.68f, 2.08f, 1.66f), new Vector3(0.36f, 0.36f, 0.04f), new Color(1f, 0.86f, 0.38f));
            CreateDecorPart(root, "Window Cloud A", PrimitiveType.Sphere, new Vector3(-0.62f, 1.9f, 1.64f), new Vector3(0.44f, 0.14f, 0.035f), new Color(0.96f, 0.98f, 1f));
            CreateDecorPart(root, "Window Cloud B", PrimitiveType.Sphere, new Vector3(-0.22f, 1.76f, 1.64f), new Vector3(0.38f, 0.12f, 0.035f), new Color(0.96f, 0.98f, 1f));
            CreateDecorPart(root, "Window Frame Top", PrimitiveType.Cube, new Vector3(0f, 2.52f, 1.52f), new Vector3(2.65f, 0.09f, 0.08f), new Color(0.98f, 0.88f, 0.66f));
            CreateDecorPart(root, "Window Frame Bottom", PrimitiveType.Cube, new Vector3(0f, 0.92f, 1.52f), new Vector3(2.65f, 0.11f, 0.08f), new Color(0.98f, 0.88f, 0.66f));
            CreateDecorPart(root, "Window Frame Left", PrimitiveType.Cube, new Vector3(-1.32f, 1.72f, 1.52f), new Vector3(0.11f, 1.65f, 0.08f), new Color(0.98f, 0.88f, 0.66f));
            CreateDecorPart(root, "Window Frame Right", PrimitiveType.Cube, new Vector3(1.32f, 1.72f, 1.52f), new Vector3(0.11f, 1.65f, 0.08f), new Color(0.98f, 0.88f, 0.66f));
            CreateDecorPart(root, "Window Cross Vertical", PrimitiveType.Cube, new Vector3(0f, 1.72f, 1.48f), new Vector3(0.08f, 1.5f, 0.08f), new Color(0.98f, 0.88f, 0.66f));
            CreateDecorPart(root, "Window Cross Horizontal", PrimitiveType.Cube, new Vector3(0f, 1.72f, 1.48f), new Vector3(2.42f, 0.08f, 0.08f), new Color(0.98f, 0.88f, 0.66f));
            CreateDecorPart(root, "Curtain Left", PrimitiveType.Cube, new Vector3(-1.62f, 1.72f, 1.35f), new Vector3(0.36f, 1.78f, 0.08f), new Color(1f, 0.91f, 0.76f));
            CreateDecorPart(root, "Curtain Right", PrimitiveType.Cube, new Vector3(1.62f, 1.72f, 1.35f), new Vector3(0.36f, 1.78f, 0.08f), new Color(1f, 0.91f, 0.76f));
            CreateDecorPart(root, "Curtain Left Tie", PrimitiveType.Cube, new Vector3(-1.5f, 1.28f, 1.28f), new Vector3(0.38f, 0.08f, 0.06f), new Color(0.84f, 0.56f, 0.3f));
            CreateDecorPart(root, "Curtain Right Tie", PrimitiveType.Cube, new Vector3(1.5f, 1.28f, 1.28f), new Vector3(0.38f, 0.08f, 0.06f), new Color(0.84f, 0.56f, 0.3f));
            CreateDecorPart(root, "Window Plant Pot", PrimitiveType.Cube, new Vector3(1.0f, 0.66f, 1.18f), new Vector3(0.34f, 0.22f, 0.08f), new Color(0.62f, 0.34f, 0.19f));
            CreateDecorPart(root, "Window Plant Leaf A", PrimitiveType.Sphere, new Vector3(0.88f, 0.88f, 1.12f), new Vector3(0.22f, 0.12f, 0.035f), new Color(0.37f, 0.63f, 0.37f));
            CreateDecorPart(root, "Window Plant Leaf B", PrimitiveType.Sphere, new Vector3(1.08f, 0.9f, 1.12f), new Vector3(0.22f, 0.12f, 0.035f), new Color(0.4f, 0.68f, 0.42f));
        }

        private static void CreateLeftFurniture(Transform root)
        {
            CreateDecorPart(root, "Left Armchair Back", PrimitiveType.Cube, new Vector3(-4.08f, -0.84f, 1.44f), new Vector3(0.72f, 0.82f, 0.14f), new Color(0.64f, 0.43f, 0.29f));
            CreateDecorPart(root, "Left Armchair Seat", PrimitiveType.Cube, new Vector3(-4.05f, -1.36f, 1.18f), new Vector3(0.9f, 0.32f, 0.16f), new Color(0.78f, 0.56f, 0.38f));
            CreateDecorPart(root, "Left Cushion", PrimitiveType.Cube, new Vector3(-3.95f, -0.98f, 1.04f), new Vector3(0.42f, 0.34f, 0.08f), new Color(1f, 0.78f, 0.36f));
            CreateDecorPart(root, "Fridge Body", PrimitiveType.Cube, new Vector3(-3.1f, -0.45f, 1.24f), new Vector3(0.82f, 1.7f, 0.16f), new Color(1f, 0.95f, 0.84f));
            CreateDecorPart(root, "Fridge Door Split", PrimitiveType.Cube, new Vector3(-3.1f, -0.22f, 1.1f), new Vector3(0.75f, 0.025f, 0.045f), new Color(0.82f, 0.67f, 0.48f));
            CreateDecorPart(root, "Fridge Handle", PrimitiveType.Cube, new Vector3(-2.78f, -0.25f, 1.04f), new Vector3(0.055f, 0.54f, 0.04f), new Color(0.68f, 0.43f, 0.22f));
            CreateDecorPart(root, "Fridge Face Eye L", PrimitiveType.Sphere, new Vector3(-3.22f, -0.68f, 1.0f), new Vector3(0.045f, 0.045f, 0.025f), new Color(0.32f, 0.18f, 0.1f));
            CreateDecorPart(root, "Fridge Face Eye R", PrimitiveType.Sphere, new Vector3(-2.98f, -0.68f, 1.0f), new Vector3(0.045f, 0.045f, 0.025f), new Color(0.32f, 0.18f, 0.1f));
            CreateDecorPart(root, "Fridge Smile", PrimitiveType.Cube, new Vector3(-3.1f, -0.82f, 0.98f), new Vector3(0.16f, 0.025f, 0.025f), new Color(0.32f, 0.18f, 0.1f));
            CreateCheeseBlock(root, "Floor Cheese Block", new Vector3(-2.62f, -1.66f, 0.94f), 0.34f);
        }

        private static void CreateRightFurniture(Transform root)
        {
            CreateDecorPart(root, "Right Dresser", PrimitiveType.Cube, new Vector3(3.35f, -1.08f, 1.24f), new Vector3(1.55f, 0.86f, 0.16f), new Color(0.62f, 0.37f, 0.19f));
            CreateDecorPart(root, "Right Dresser Top Cloth", PrimitiveType.Cube, new Vector3(3.35f, -0.58f, 1.08f), new Vector3(1.7f, 0.12f, 0.08f), new Color(1f, 0.92f, 0.76f));
            for (var i = 0; i < 3; i += 1)
            {
                var x = 2.86f + i * 0.48f;
                CreateDecorPart(root, $"Right Drawer {i + 1}", PrimitiveType.Cube, new Vector3(x, -1.12f, 1.02f), new Vector3(0.36f, 0.26f, 0.05f), new Color(0.74f, 0.46f, 0.24f));
                CreateDecorPart(root, $"Right Drawer Pull {i + 1}", PrimitiveType.Sphere, new Vector3(x, -1.12f, 0.96f), new Vector3(0.045f, 0.045f, 0.025f), new Color(0.95f, 0.7f, 0.32f));
            }

            CreateMilkBottle(root, "Big Bottle Table A", new Vector3(2.86f, -0.1f, 0.94f), 0.52f);
            CreateMilkBottle(root, "Big Bottle Table B", new Vector3(3.35f, -0.02f, 0.94f), 0.58f);
            CreateMilkBottle(root, "Big Bottle Table C", new Vector3(3.86f, -0.1f, 0.94f), 0.48f);
            CreateDecorPart(root, "Table Lamp Base", PrimitiveType.Cube, new Vector3(4.32f, -0.52f, 1.02f), new Vector3(0.16f, 0.34f, 0.05f), new Color(0.72f, 0.44f, 0.23f));
            CreateDecorPart(root, "Table Lamp Glow", PrimitiveType.Sphere, new Vector3(4.32f, -0.18f, 0.94f), new Vector3(0.42f, 0.34f, 0.05f), new Color(1f, 0.78f, 0.36f));
        }

        private static void CreateBlendingTable(Transform root)
        {
            var tableRoot = new GameObject("BlendingTable").transform;
            tableRoot.SetParent(root, false);
            tableRoot.localPosition = Vector3.zero;

            CreateDecorPart(tableRoot, "BlendingTable Top", PrimitiveType.Cube, new Vector3(-0.12f, -1.02f, 1.08f), new Vector3(1.18f, 0.16f, 0.16f), new Color(0.72f, 0.43f, 0.22f));
            CreateDecorPart(tableRoot, "BlendingTable Cloth", PrimitiveType.Cube, new Vector3(-0.12f, -0.92f, 0.94f), new Vector3(1.28f, 0.08f, 0.08f), new Color(1f, 0.9f, 0.68f));
            CreateDecorPart(tableRoot, "BlendingTable Leg L", PrimitiveType.Cube, new Vector3(-0.58f, -1.42f, 1.12f), new Vector3(0.08f, 0.72f, 0.08f), new Color(0.52f, 0.29f, 0.14f));
            CreateDecorPart(tableRoot, "BlendingTable Leg R", PrimitiveType.Cube, new Vector3(0.34f, -1.42f, 1.12f), new Vector3(0.08f, 0.72f, 0.08f), new Color(0.52f, 0.29f, 0.14f));
            CreateDecorPart(tableRoot, "Blending Bowl", PrimitiveType.Sphere, new Vector3(-0.26f, -0.72f, 0.9f), new Vector3(0.28f, 0.12f, 0.08f), new Color(0.84f, 0.94f, 0.98f));
            CreateDecorPart(tableRoot, "Blending Spoon", PrimitiveType.Cube, new Vector3(0.14f, -0.66f, 0.86f), new Vector3(0.42f, 0.035f, 0.025f), new Color(0.86f, 0.64f, 0.36f));
            CreateMilkBottle(tableRoot, "Blending Milk Bottle", new Vector3(0.42f, -0.6f, 0.88f), 0.34f);
        }

        private static void CreateShelfGroup(Transform root)
        {
            var shelfRoot = new GameObject("MilkShelf").transform;
            shelfRoot.SetParent(root, false);
            shelfRoot.localPosition = Vector3.zero;

            CreateDecorPart(shelfRoot, "Left Shelf Back Rail", PrimitiveType.Cube, new Vector3(-1.98f, 0.1f, 1.42f), new Vector3(1.28f, 0.9f, 0.08f), new Color(0.55f, 0.32f, 0.17f));
            CreateDecorPart(shelfRoot, "Left Shelf Top", PrimitiveType.Cube, new Vector3(-1.98f, 0.48f, 1.16f), new Vector3(1.42f, 0.08f, 0.08f), new Color(0.7f, 0.43f, 0.22f));
            CreateDecorPart(shelfRoot, "Left Shelf Bottom", PrimitiveType.Cube, new Vector3(-1.98f, -0.16f, 1.16f), new Vector3(1.42f, 0.08f, 0.08f), new Color(0.7f, 0.43f, 0.22f));
            for (var i = 0; i < 5; i += 1)
            {
                CreateMilkBottle(shelfRoot, $"Left Shelf Bottle {i + 1}", new Vector3(-2.48f + i * 0.25f, 0.68f, 1.04f), 0.3f);
                CreateMilkBottle(shelfRoot, $"Left Shelf Jar {i + 1}", new Vector3(-2.48f + i * 0.25f, 0.02f, 1.04f), 0.25f);
            }

            CreateDecorPart(shelfRoot, "Right Wall Shelf", PrimitiveType.Cube, new Vector3(3.28f, 1.06f, 1.2f), new Vector3(1.48f, 0.09f, 0.08f), new Color(0.7f, 0.43f, 0.22f));
            for (var i = 0; i < 5; i += 1)
            {
                CreateMilkBottle(shelfRoot, $"Right Shelf Bottle {i + 1}", new Vector3(2.72f + i * 0.28f, 1.32f, 1.04f), 0.28f);
            }

            CreateDecorPart(root, "Chalkboard", PrimitiveType.Cube, new Vector3(-2.88f, 1.48f, 1.12f), new Vector3(0.74f, 0.72f, 0.06f), new Color(0.18f, 0.28f, 0.21f));
            CreateDecorPart(root, "Chalkboard Frame Top", PrimitiveType.Cube, new Vector3(-2.88f, 1.91f, 1.08f), new Vector3(0.9f, 0.08f, 0.04f), new Color(0.64f, 0.38f, 0.18f));
            CreateDecorPart(root, "Chalkboard Frame Bottom", PrimitiveType.Cube, new Vector3(-2.88f, 1.05f, 1.08f), new Vector3(0.9f, 0.08f, 0.04f), new Color(0.64f, 0.38f, 0.18f));
            CreateDecorPart(root, "Chalkboard Frame Left", PrimitiveType.Cube, new Vector3(-3.33f, 1.48f, 1.08f), new Vector3(0.08f, 0.84f, 0.04f), new Color(0.64f, 0.38f, 0.18f));
            CreateDecorPart(root, "Chalkboard Frame Right", PrimitiveType.Cube, new Vector3(-2.43f, 1.48f, 1.08f), new Vector3(0.08f, 0.84f, 0.04f), new Color(0.64f, 0.38f, 0.18f));
            CreateWorldLabel(root, "Chalkboard Text", "\uC6B0\uC720\uB294\n\uB9C8\uBC95", new Vector3(-2.88f, 1.5f, 0.95f), 0.075f, new Color(1f, 0.9f, 0.62f));
        }

        private static void CreateHangingLights(Transform root)
        {
            CreateDecorPart(root, "Center Pendant Cord", PrimitiveType.Cube, new Vector3(0.18f, 2.88f, 1.18f), new Vector3(0.025f, 0.76f, 0.025f), new Color(0.42f, 0.25f, 0.13f));
            CreateDecorPart(root, "Center Pendant Glow", PrimitiveType.Sphere, new Vector3(0.18f, 2.36f, 1.08f), new Vector3(0.36f, 0.28f, 0.05f), new Color(1f, 0.78f, 0.34f));
            CreateStarLamp(root, "Left Star Lamp", new Vector3(-3.9f, 1.72f, 1.02f), 0.32f);
            CreateStarLamp(root, "Right Star Lamp", new Vector3(2.05f, 2.28f, 1.02f), 0.3f);
        }

        private static void CreateMilkroomForeground(Transform root)
        {
            CreateDecorPart(root, "Foreground Soft Milk Drop L", PrimitiveType.Sphere, new Vector3(-3.38f, -2.12f, 0.48f), new Vector3(0.28f, 0.055f, 0.04f), new Color(0.94f, 0.9f, 0.78f));
            CreateDecorPart(root, "Foreground Soft Milk Drop C", PrimitiveType.Sphere, new Vector3(2.2f, -2.1f, 0.48f), new Vector3(0.22f, 0.05f, 0.04f), new Color(0.94f, 0.9f, 0.78f));
            CreateDecorPart(root, "Foreground Soft Milk Drop R", PrimitiveType.Sphere, new Vector3(4.05f, -2.0f, 0.48f), new Vector3(0.34f, 0.06f, 0.04f), new Color(0.94f, 0.9f, 0.78f));
            CreateDecorPart(root, "Foreground Warm Vignette", PrimitiveType.Cube, new Vector3(0f, -2.98f, 0.36f), new Vector3(10.8f, 0.18f, 0.04f), new Color(0.35f, 0.2f, 0.12f));
        }

        private static void CreateAmbientThemeVfx(Transform root)
        {
            for (var i = 0; i < 12; i += 1)
            {
                var x = -4.6f + i * 0.84f;
                var y = 2.38f - (i % 4) * 0.34f;
                CreateDecorPart(root, $"Rain Streak {i + 1}", PrimitiveType.Cube, new Vector3(x, y, 0.62f), new Vector3(0.025f, 0.34f, 0.025f), new Color(0.62f, 0.74f, 0.82f));
            }

            for (var i = 0; i < 14; i += 1)
            {
                var x = -1.05f + (i % 7) * 0.36f;
                var y = 1.28f + (i / 7) * 0.38f;
                CreateDecorPart(root, $"Night Star Speckle {i + 1}", PrimitiveType.Sphere, new Vector3(x, y, 0.58f), new Vector3(0.035f, 0.035f, 0.018f), new Color(0.78f, 0.88f, 1f));
            }

            for (var i = 0; i < 18; i += 1)
            {
                var x = -3.4f + (i % 9) * 0.82f;
                var y = -0.8f + (i / 9) * 1.55f + (i % 3) * 0.18f;
                CreateDecorPart(root, $"Starlight Sparkle {i + 1}", PrimitiveType.Sphere, new Vector3(x, y, 0.5f), new Vector3(0.05f, 0.05f, 0.02f), new Color(0.86f, 0.76f, 1f));
            }

            for (var i = 0; i < 16; i += 1)
            {
                var x = -3.7f + (i % 8) * 1.02f;
                var y = 2.3f - (i / 8) * 1.25f - (i % 4) * 0.16f;
                CreateDecorPart(root, $"Winter Snowflake {i + 1}", PrimitiveType.Sphere, new Vector3(x, y, 0.52f), new Vector3(0.055f, 0.055f, 0.018f), new Color(0.92f, 0.98f, 1f));
            }

            for (var i = 0; i < 12; i += 1)
            {
                var x = -3.1f + (i % 6) * 1.18f;
                var y = -1.1f + (i / 6) * 1.9f + (i % 2) * 0.22f;
                CreateDecorPart(root, $"Vintage Dust Mote {i + 1}", PrimitiveType.Sphere, new Vector3(x, y, 0.54f), new Vector3(0.035f, 0.035f, 0.016f), new Color(0.92f, 0.72f, 0.38f));
            }
        }

        private static void EnsureAmbientThemeVfx(Transform root)
        {
            RemoveChildIfExists(root, "Evening Window Beam L");
            RemoveChildIfExists(root, "Evening Window Beam R");
            if (root != null && root.childCount == 0)
            {
                CreateAmbientThemeVfx(root);
            }
        }

        private static void CreateMilkBottle(Transform root, string name, Vector3 position, float size)
        {
            var bottleRoot = new GameObject(name).transform;
            bottleRoot.SetParent(root, false);
            bottleRoot.localPosition = position;

            CreateDecorPart(bottleRoot, "Bottle Body", PrimitiveType.Capsule, new Vector3(0f, 0f, 0f), new Vector3(size * 0.24f, size * 0.48f, size * 0.09f), new Color(0.84f, 0.94f, 0.98f));
            CreateDecorPart(bottleRoot, "Bottle Milk Fill", PrimitiveType.Capsule, new Vector3(0f, -size * 0.08f, -size * 0.012f), new Vector3(size * 0.2f, size * 0.34f, size * 0.07f), new Color(0.98f, 0.95f, 0.78f));
            CreateDecorPart(bottleRoot, "Bottle Neck", PrimitiveType.Cylinder, new Vector3(0f, size * 0.31f, -0.005f), new Vector3(size * 0.08f, size * 0.11f, size * 0.08f), new Color(0.86f, 0.95f, 1f));
            CreateDecorPart(bottleRoot, "Bottle Cap", PrimitiveType.Cube, new Vector3(0f, size * 0.43f, -0.02f), new Vector3(size * 0.18f, size * 0.07f, size * 0.055f), new Color(0.47f, 0.72f, 0.9f));
            CreateDecorPart(bottleRoot, "Bottle Label", PrimitiveType.Cube, new Vector3(0f, -size * 0.03f, -0.062f), new Vector3(size * 0.2f, size * 0.13f, size * 0.025f), new Color(1f, 0.86f, 0.56f));
            CreateDecorPart(bottleRoot, "Bottle Shine", PrimitiveType.Cube, new Vector3(-size * 0.08f, size * 0.08f, -size * 0.085f), new Vector3(size * 0.025f, size * 0.2f, size * 0.012f), new Color(1f, 1f, 0.94f));
            CreateWorldLabel(bottleRoot, "Bottle Milk Text", "\uC6B0\uC720", new Vector3(0f, -size * 0.03f, -size * 0.09f), size * 0.08f, new Color(0.28f, 0.56f, 0.76f));
            CreateDecorPart(bottleRoot, "Bottle Face Eye L", PrimitiveType.Sphere, new Vector3(-size * 0.05f, -size * 0.13f, -size * 0.09f), new Vector3(size * 0.018f, size * 0.018f, size * 0.008f), new Color(0.24f, 0.16f, 0.1f));
            CreateDecorPart(bottleRoot, "Bottle Face Eye R", PrimitiveType.Sphere, new Vector3(size * 0.05f, -size * 0.13f, -size * 0.09f), new Vector3(size * 0.018f, size * 0.018f, size * 0.008f), new Color(0.24f, 0.16f, 0.1f));
        }

        private static void CreateCheeseBlock(Transform root, string name, Vector3 position, float size)
        {
            var cheeseRoot = new GameObject(name).transform;
            cheeseRoot.SetParent(root, false);
            cheeseRoot.localPosition = position;

            CreateDecorPart(cheeseRoot, "Cheese Body", PrimitiveType.Cube, Vector3.zero, new Vector3(size, size * 0.62f, size * 0.16f), new Color(1f, 0.72f, 0.18f));
            CreateDecorPart(cheeseRoot, "Cheese Hole A", PrimitiveType.Sphere, new Vector3(-size * 0.22f, size * 0.08f, -size * 0.08f), new Vector3(size * 0.12f, size * 0.09f, size * 0.035f), new Color(0.85f, 0.48f, 0.09f));
            CreateDecorPart(cheeseRoot, "Cheese Hole B", PrimitiveType.Sphere, new Vector3(size * 0.16f, -size * 0.06f, -size * 0.08f), new Vector3(size * 0.1f, size * 0.08f, size * 0.035f), new Color(0.85f, 0.48f, 0.09f));
        }

        private static void CreateStarLamp(Transform root, string name, Vector3 position, float size)
        {
            var starRoot = new GameObject(name).transform;
            starRoot.SetParent(root, false);
            starRoot.localPosition = position;

            CreateDecorPart(starRoot, "Star Core", PrimitiveType.Sphere, Vector3.zero, new Vector3(size, size, size * 0.12f), new Color(1f, 0.86f, 0.34f));
            CreateDecorPart(starRoot, "Star Up", PrimitiveType.Cube, new Vector3(0f, size * 0.34f, 0f), new Vector3(size * 0.13f, size * 0.42f, size * 0.05f), new Color(1f, 0.86f, 0.34f));
            CreateDecorPart(starRoot, "Star Down", PrimitiveType.Cube, new Vector3(0f, -size * 0.34f, 0f), new Vector3(size * 0.13f, size * 0.42f, size * 0.05f), new Color(1f, 0.86f, 0.34f));
            CreateDecorPart(starRoot, "Star Left", PrimitiveType.Cube, new Vector3(-size * 0.34f, 0f, 0f), new Vector3(size * 0.42f, size * 0.13f, size * 0.05f), new Color(1f, 0.86f, 0.34f));
            CreateDecorPart(starRoot, "Star Right", PrimitiveType.Cube, new Vector3(size * 0.34f, 0f, 0f), new Vector3(size * 0.42f, size * 0.13f, size * 0.05f), new Color(1f, 0.86f, 0.34f));
        }

        private static Transform CreateDecorPart(Transform parent, string name, PrimitiveType primitive, Vector3 localPosition, Vector3 localScale, Color color)
        {
            return CreateDecorPart(parent, name, primitive, localPosition, localScale, Quaternion.identity, color);
        }

        private static Transform CreateDecorPart(Transform parent, string name, PrimitiveType primitive, Vector3 localPosition, Vector3 localScale, Quaternion localRotation, Color color)
        {
            var part = GameObject.CreatePrimitive(primitive);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            part.transform.localRotation = localRotation;

            var collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                DestroyObjectSafely(collider);
            }

            var renderer = part.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.shadowCastingMode = ShouldCastDecorShadow(name) ? ShadowCastingMode.On : ShadowCastingMode.Off;
                renderer.receiveShadows = !name.Contains("Glow") && !name.Contains("Sparkle") && !name.Contains("Rain");
                PaintDecorRenderer(renderer, AdjustMilkroomDecorColor(name, color));
            }

            return part.transform;
        }

        private static Color AdjustMilkroomDecorColor(string objectName, Color color)
        {
            var adjusted = color;
            var isGlow = objectName.Contains("Glow") || objectName.Contains("Sun") || objectName.Contains("Bulb");
            var isGlass = objectName.Contains("Glass") || objectName.Contains("Bottle") || objectName.Contains("Window Sky");
            var isWood = objectName.Contains("Wood") || objectName.Contains("Floor") || objectName.Contains("Shelf") || objectName.Contains("Dresser") || objectName.Contains("Chair") || objectName.Contains("Table") || objectName.Contains("Frame");

            if (isGlow)
            {
                adjusted = Color.Lerp(adjusted, new Color(0.86f, 0.56f, 0.24f, color.a), 0.28f) * 0.68f;
            }
            else if (isGlass)
            {
                adjusted = Color.Lerp(adjusted, new Color(0.58f, 0.74f, 0.82f, color.a), 0.18f) * 0.82f;
            }
            else if (isWood)
            {
                adjusted = Color.Lerp(adjusted, new Color(0.45f, 0.25f, 0.12f, color.a), 0.18f) * 0.86f;
            }
            else if (Mathf.Max(color.r, color.g, color.b) > 0.9f)
            {
                adjusted = Color.Lerp(adjusted, new Color(0.88f, 0.78f, 0.58f, color.a), 0.22f) * 0.82f;
            }
            else
            {
                adjusted *= 0.88f;
            }

            adjusted.a = color.a;
            return adjusted;
        }

        private static void CreateWorldLabel(Transform parent, string name, string text, Vector3 localPosition, float characterSize, Color color)
        {
            var labelObject = new GameObject(name);
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = localPosition;
            labelObject.transform.localRotation = Quaternion.identity;

            var label = labelObject.AddComponent<TextMesh>();
            label.text = text;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = characterSize;
            label.fontSize = 64;
            label.color = color;

            var renderer = labelObject.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        private static void PaintDecorRenderer(Renderer renderer, Color color)
        {
            ToonMaterialUtility.Apply(renderer, ToonMaterialUtility.InferProfile(renderer), color);
        }

        private static bool ShouldCastDecorShadow(string objectName)
        {
            return !objectName.Contains("Glow")
                && !objectName.Contains("Window Sky")
                && !objectName.Contains("Cloud")
                && !objectName.Contains("Rain")
                && !objectName.Contains("Star Speckle")
                && !objectName.Contains("Vignette");
        }

        private static CheeseTamaVisualController EnsureCheeseTamaPlaceholder()
        {
            var existing = GameObject.Find("CheeseTamaRoot");
            if (existing == null)
            {
                existing = GameObject.Find("CheeseTama Egg Placeholder");
            }

            if (existing != null)
            {
                existing.name = "CheeseTamaRoot";
                existing.transform.position = CheeseTamaRestingWorldPosition;
                existing.transform.localScale = Vector3.one;
                var existingController = GetOrCreateVisualController(existing);
                AlignCheeseTamaRestingPosition(existingController);
                return existingController;
            }

            var egg = new GameObject("CheeseTamaRoot");
            egg.transform.position = CheeseTamaRestingWorldPosition;
            egg.transform.localScale = Vector3.one;

            var controller = GetOrCreateVisualController(egg);
            AlignCheeseTamaRestingPosition(controller);
            return controller;
        }

        private static void AlignCheeseTamaRestingPosition(CheeseTamaVisualController controller)
        {
            controller?.SetRestingWorldPosition(CheeseTamaRestingWorldPosition);
        }

        private static CheeseTamaVisualController GetOrCreateVisualController(GameObject target)
        {
            var controller = target.GetComponent<CheeseTamaVisualController>();
            if (controller == null)
            {
                controller = target.AddComponent<CheeseTamaVisualController>();
            }

            EnsureGeneratedCharacterModel(target, controller);
            return controller;
        }

        private static void EnsureGeneratedCharacterModel(GameObject root, CheeseTamaVisualController controller)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                return;
            }

            var modelPrefab = ResolveEditorPreviewModelPrefab();
            if (modelPrefab == null)
            {
                return;
            }

            // Keep exactly one current growth-stage preview and discard legacy or duplicate rigs.
            var toRemove = new System.Collections.Generic.List<GameObject>();
            Transform modelTransform = null;
            foreach (Transform child in root.transform)
            {
                var source = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(child.gameObject);
                var isCurrentPreview = child.name == "GeneratedModel" && source == modelPrefab;
                if (modelTransform == null && isCurrentPreview)
                {
                    modelTransform = child;
                    continue;
                }

                toRemove.Add(child.gameObject);
            }

            foreach (var g in toRemove)
            {
                Object.DestroyImmediate(g);
            }

            if (modelTransform == null)
            {
                var model = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(modelPrefab);
                model.name = "GeneratedModel";
                model.transform.SetParent(root.transform, false);
                modelTransform = model.transform;
            }

            modelTransform.localPosition = Vector3.zero;
            modelTransform.localRotation = Quaternion.Euler(0f, CheeseTamaModelYaw, 0f);
            modelTransform.localScale = Vector3.one * CheeseTamaModelScale;
            modelTransform.gameObject.SetActive(true);

            var so = new UnityEditor.SerializedObject(controller);
            so.FindProperty("modelPrefab").objectReferenceValue = modelPrefab;
            so.FindProperty("modelInstance").objectReferenceValue = modelTransform;
            so.FindProperty("modelYawDegrees").floatValue = CheeseTamaModelYaw;
            so.FindProperty("modelScale").floatValue = CheeseTamaModelScale;
            so.ApplyModifiedProperties();
#endif
        }

#if UNITY_EDITOR
        private static GameObject ResolveEditorPreviewModelPrefab()
        {
            var visualSet = UnityEditor.AssetDatabase.LoadAssetAtPath<CheeseTamaGrowthVisualSet>(CheeseTamaGrowthVisualSetPath);
            var previewSave = LoadEditorPreviewSave();
            var previewStage = CheeseTamaGrowthStageCatalog.Resolve(previewSave?.cheeseTama);
            var stagePrefab = visualSet != null ? visualSet.GetPrefab(previewStage) : null;
            if (stagePrefab != null)
            {
                return stagePrefab;
            }

            return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(CheeseTamaEggPrefabPath);
        }
#endif
    }
}
