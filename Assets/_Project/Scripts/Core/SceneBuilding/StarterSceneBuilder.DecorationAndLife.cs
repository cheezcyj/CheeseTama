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
        private static void EnsureDecorationShop(Transform canvasTransform)
        {
            if (canvasTransform == null)
            {
                return;
            }

            var decorateOverlay = canvasTransform.Find("Decorate Overlay");
            if (decorateOverlay == null)
            {
                return;
            }

            var shopRoot = GetOrCreateFullScreenOverlay(
                canvasTransform,
                "Decoration Shop Overlay",
                new Color(0.1f, 0.06f, 0.02f, 0.7f));
            var card = GetOrCreatePanel(shopRoot.transform, "Decoration Shop Card", Vector2.zero, new Vector2(1180f, 820f));
            ConfigureCenteredRect(card.GetComponent<RectTransform>(), new Vector2(1180f, 820f));
            if (card.TryGetComponent(out Image cardImage))
            {
                cardImage.color = new Color(1f, 0.97f, 0.85f, 1f);
            }

            var heading = GetOrCreateText(card.transform, "Decoration Shop Title Text", "밀크룸 장식 상점", 30,
                TextAnchor.MiddleLeft, new Vector2(42f, -24f), new Vector2(480f, 50f));
            heading.fontStyle = FontStyle.Bold;
            var balance = GetOrCreateText(card.transform, "Decoration Shop Balance Text", "코인 0 · 우유방울 0 · 도감 조각 0", 18,
                TextAnchor.MiddleRight, new Vector2(540f, -28f), new Vector2(452f, 42f));

            var itemNames = new Text[DecorationCatalog.All.Length];
            var itemStates = new Text[DecorationCatalog.All.Length];
            var itemButtons = new Button[DecorationCatalog.All.Length];
            for (var index = 0; index < DecorationCatalog.All.Length; index += 1)
            {
                var column = index % 3;
                var row = index / 3;
                var x = 42f + column * 286f;
                var y = 96f + row * 92f;
                itemButtons[index] = GetOrCreateTopLeftButton(card.transform,
                    $"Decoration Item {index + 1} Button", string.Empty, new Vector2(x, -y), new Vector2(268f, 78f));
                itemNames[index] = GetOrCreateText(itemButtons[index].transform, "Item Name Text",
                    DecorationCatalog.All[index].displayName, 17, TextAnchor.MiddleLeft,
                    new Vector2(14f, -8f), new Vector2(238f, 26f));
                itemNames[index].fontStyle = FontStyle.Bold;
                itemNames[index].horizontalOverflow = HorizontalWrapMode.Wrap;
                itemNames[index].verticalOverflow = VerticalWrapMode.Truncate;
                itemNames[index].resizeTextForBestFit = true;
                itemNames[index].resizeTextMinSize = 13;
                itemNames[index].resizeTextMaxSize = 17;
                itemStates[index] = GetOrCreateText(itemButtons[index].transform, "Item State Text", "보유 상태", 13,
                    TextAnchor.MiddleLeft, new Vector2(14f, -40f), new Vector2(238f, 24f));
                itemStates[index].horizontalOverflow = HorizontalWrapMode.Wrap;
                itemStates[index].verticalOverflow = VerticalWrapMode.Truncate;
                itemStates[index].resizeTextForBestFit = true;
                itemStates[index].resizeTextMinSize = 10;
                itemStates[index].resizeTextMaxSize = 13;
            }

            var detail = GetOrCreateText(card.transform, "Decoration Detail Text", "장식을 선택해 주세요.", 18,
                TextAnchor.UpperLeft, new Vector2(910f, -112f), new Vector2(228f, 240f));
            detail.supportRichText = true;
            var status = GetOrCreateText(card.transform, "Decoration Status Text", string.Empty, 16,
                TextAnchor.UpperLeft, new Vector2(910f, -366f), new Vector2(228f, 94f));
            var purchase = GetOrCreateTopLeftButton(card.transform, "Decoration Purchase Button", "구매",
                new Vector2(910f, -488f), new Vector2(104f, 52f));
            var equip = GetOrCreateTopLeftButton(card.transform, "Decoration Equip Button", "장착",
                new Vector2(1030f, -488f), new Vector2(104f, 52f));
            var close = GetOrCreateTopLeftButton(card.transform, "Decoration Shop Close Button", "닫기",
                new Vector2(990f, -718f), new Vector2(144f, 52f));
            ApplyCareButtonStyle(purchase);
            ApplyCareButtonStyle(equip);
            ApplyCareButtonStyle(close);

            var controller = canvasTransform.GetComponent<DecorationShopPanelController>();
            if (controller == null)
            {
                controller = canvasTransform.gameObject.AddComponent<DecorationShopPanelController>();
            }

            controller.Configure(
                shopRoot,
                balance,
                detail,
                status,
                itemNames,
                itemStates,
                itemButtons,
                purchase,
                equip,
                close,
                () => ResolveDecorationManager()?.GetDecorationShopSnapshot()
                    ?? DecorationShopSnapshot.CreateDefault(),
                itemId =>
                {
                    var manager = ResolveDecorationManager();
                    return manager != null
                        ? manager.TryPurchaseDecoration(itemId)
                        : DecorationShopRules.Purchase(itemId, DecorationShopSnapshot.CreateDefault());
                },
                itemId =>
                {
                    var manager = ResolveDecorationManager();
                    return manager != null
                        ? manager.TryEquipDecoration(itemId)
                        : DecorationShopRules.Equip(itemId, DecorationShopSnapshot.CreateDefault());
                });

            var previewRect = decorateOverlay.Find("Decorate Preview Panel") as RectTransform;
            if (previewRect != null)
            {
                previewRect.sizeDelta = new Vector2(692f, 432f);
            }

            var openShopButton = GetOrCreateTopLeftButton(
                decorateOverlay,
                "Open Decoration Shop Button",
                "장식 상점",
                new Vector2(568f, -562f),
                new Vector2(144f, 42f));
            ApplyCareButtonStyle(openShopButton);
            openShopButton.onClick.RemoveAllListeners();
            openShopButton.onClick.AddListener(controller.Open);
        }

        private static GameManager ResolveDecorationManager()
        {
            if (GameManager.Instance != null)
            {
                return GameManager.Instance;
            }

            return Application.isPlaying ? EnsureCoreSystems() : null;
        }

        private static void EnsureDecorationRoomPresenter()
        {
            var background = GameObject.Find("Milkroom Background");
            if (background == null)
            {
                return;
            }

            var presenter = background.GetComponent<DecorationRoomPresenter>();
            if (presenter == null)
            {
                presenter = background.AddComponent<DecorationRoomPresenter>();
            }

            var shell = background.transform.Find("RoomShell");
            var wall = shell != null ? shell.Find("BackWall")?.GetComponent<Renderer>() : null;
            var rug = background.transform.Find("Rug_Model");
            var floor = rug != null
                ? rug.GetComponentInChildren<Renderer>(true)
                : shell != null ? shell.Find("Floor")?.GetComponent<Renderer>() : null;
            var anchor = background.transform.Find("Decoration Accent Anchor");
            if (anchor == null)
            {
                var anchorObject = new GameObject("Decoration Accent Anchor");
                anchorObject.transform.SetParent(background.transform, false);
                anchorObject.transform.localPosition = new Vector3(3.34f, -1.93f, 1.55f);
                anchor = anchorObject.transform;
            }

            Transform EnsureDecorationAnchor(string name, Vector3 localPosition)
            {
                var found = background.transform.Find(name);
                if (found != null)
                {
                    return found;
                }

                var created = new GameObject(name).transform;
                created.SetParent(background.transform, false);
                created.localPosition = localPosition;
                return created;
            }

            var windowAnchor = EnsureDecorationAnchor("Decoration Window Anchor", new Vector3(-3.15f, 0.62f, 1.42f));
            var shelfAnchor = EnsureDecorationAnchor("Decoration Shelf Anchor", new Vector3(3.05f, 0.18f, 1.36f));
            var bedsideAnchor = EnsureDecorationAnchor("Decoration Bedside Anchor", new Vector3(-2.85f, -1.72f, 1.2f));

            presenter.Configure(wall, floor, anchor, windowAnchor, shelfAnchor, bedsideAnchor);
            presenter.ConfigurePlacementProvider(() =>
            {
                var manager = ResolveDecorationManager();
                return manager != null
                    ? manager.GetDecorationPlacementSnapshot()
                    : DecorationPlacementSystem.MigrateFromLegacySlots(
                        DecorationCatalog.GetDefault(DecorationSlot.Accent)?.id,
                        DecorationCatalog.GetDefault(DecorationSlot.Shelf)?.id,
                        DecorationCatalog.GetDefault(DecorationSlot.Bedside)?.id);
            });
        }

        private static void EnsureDecorationPlacement(Transform canvasTransform)
        {
            if (canvasTransform == null)
            {
                return;
            }

            var decorateOverlay = canvasTransform.Find("Decorate Overlay");
            var presenter = FindMilkroomDecorationPresenter(canvasTransform);
            if (decorateOverlay == null || presenter == null)
            {
                return;
            }

            // Free placement is intentionally deferred. Keep the implementation and saved
            // placement data intact, but do not expose an unfinished entry point.
            var openButton = GetOrCreateTopLeftButton(
                decorateOverlay,
                "Open Decoration Placement Button",
                "자유 배치",
                new Vector2(410f, -562f),
                new Vector2(144f, 42f));
            ApplyCareButtonStyle(openButton);
            openButton.gameObject.SetActive(false);

            var overlay = GetOrCreateFullScreenOverlay(
                canvasTransform,
                DecorationPlacementOverlayController.OverlayObjectName,
                new Color(0.04f, 0.025f, 0.01f, 0.22f));
            var overlayRect = overlay.GetComponent<RectTransform>();
            var placementController = overlay.GetComponent<DecorationPlacementController>()
                ?? overlay.AddComponent<DecorationPlacementController>();
            placementController.Configure(
                overlayRect,
                presenter,
                () => ResolveDecorationManager()?.GetDecorationPlacementSnapshot()
                    ?? DecorationPlacementSystem.MigrateFromLegacySlots(
                        DecorationCatalog.GetDefault(DecorationSlot.Accent)?.id,
                        DecorationCatalog.GetDefault(DecorationSlot.Shelf)?.id,
                        DecorationCatalog.GetDefault(DecorationSlot.Bedside)?.id),
                snapshot => ResolveDecorationManager()?.TryCommitDecorationPlacement(snapshot)
                    == true,
                () => overlay.activeInHierarchy);

            var toolbar = GetOrCreatePanel(
                overlay.transform,
                "Decoration Placement Toolbar",
                Vector2.zero,
                new Vector2(1420f, 150f));
            var toolbarRect = toolbar.GetComponent<RectTransform>();
            toolbarRect.anchorMin = new Vector2(0.5f, 1f);
            toolbarRect.anchorMax = new Vector2(0.5f, 1f);
            toolbarRect.pivot = new Vector2(0.5f, 1f);
            toolbarRect.anchoredPosition = new Vector2(0f, -24f);
            toolbarRect.sizeDelta = new Vector2(1420f, 150f);
            if (toolbar.TryGetComponent(out Image toolbarImage))
            {
                toolbarImage.color = new Color(1f, 0.97f, 0.84f, 0.98f);
                toolbarImage.raycastTarget = true;
            }

            var title = GetOrCreateText(
                toolbar.transform,
                "Decoration Placement Title Text",
                "밀크룸 자유 배치",
                25,
                TextAnchor.MiddleLeft,
                new Vector2(24f, -16f),
                new Vector2(230f, 40f));
            title.fontStyle = FontStyle.Bold;
            var status = GetOrCreateText(
                toolbar.transform,
                "Decoration Placement Status Text",
                "이동할 장식을 고른 뒤 방 안에서 위치를 정해 주세요.",
                15,
                TextAnchor.MiddleLeft,
                new Vector2(264f, -18f),
                new Vector2(830f, 38f));

            RemoveChildIfExists(toolbar.transform, "Place Accent Button");
            RemoveChildIfExists(toolbar.transform, "Place Bedside Button");
            Button accent = null;
            Button bedside = null;
            var shelf = GetOrCreateTopLeftButton(toolbar.transform, "Place Shelf Button", "선반", new Vector2(24f, -78f), new Vector2(116f, 48f));
            var nudgeLeft = GetOrCreateTopLeftButton(toolbar.transform, "Move Placement Left Button", "←", new Vector2(160f, -78f), new Vector2(48f, 48f));
            var nudgeUp = GetOrCreateTopLeftButton(toolbar.transform, "Move Placement Up Button", "↑", new Vector2(214f, -78f), new Vector2(48f, 48f));
            var nudgeDown = GetOrCreateTopLeftButton(toolbar.transform, "Move Placement Down Button", "↓", new Vector2(268f, -78f), new Vector2(48f, 48f));
            var nudgeRight = GetOrCreateTopLeftButton(toolbar.transform, "Move Placement Right Button", "→", new Vector2(322f, -78f), new Vector2(48f, 48f));
            var rotateLeft = GetOrCreateTopLeftButton(toolbar.transform, "Rotate Placement Left Button", "↶ 15°", new Vector2(386f, -78f), new Vector2(86f, 48f));
            var rotateRight = GetOrCreateTopLeftButton(toolbar.transform, "Rotate Placement Right Button", "↷ 15°", new Vector2(478f, -78f), new Vector2(86f, 48f));
            var reset = GetOrCreateTopLeftButton(toolbar.transform, "Reset Placement Button", "기본 위치", new Vector2(574f, -78f), new Vector2(104f, 48f));
            var confirm = GetOrCreateTopLeftButton(toolbar.transform, "Confirm Placement Button", "배치 확인", new Vector2(694f, -78f), new Vector2(116f, 48f));
            var cancel = GetOrCreateTopLeftButton(toolbar.transform, "Cancel Placement Button", "변경 취소", new Vector2(822f, -78f), new Vector2(116f, 48f));
            var close = GetOrCreateTopLeftButton(toolbar.transform, "Close Placement Button", "닫기", new Vector2(950f, -78f), new Vector2(116f, 48f));
            ApplyCareButtonStyle(shelf);
            ApplyCareButtonStyle(nudgeLeft);
            ApplyCareButtonStyle(nudgeUp);
            ApplyCareButtonStyle(nudgeDown);
            ApplyCareButtonStyle(nudgeRight);
            ApplyCareButtonStyle(rotateLeft);
            ApplyCareButtonStyle(rotateRight);
            ApplyCareButtonStyle(reset);
            ApplyCareButtonStyle(confirm);
            ApplyCareButtonStyle(cancel);
            ApplyCareButtonStyle(close);

            var panelController = canvasTransform.GetComponent<DecorationPlacementOverlayController>()
                ?? canvasTransform.gameObject.AddComponent<DecorationPlacementOverlayController>();
            panelController.Configure(
                overlay,
                placementController,
                status,
                openButton,
                close,
                accent,
                shelf,
                bedside,
                confirm,
                cancel,
                rotateLeft,
                rotateRight,
                nudgeLeft,
                nudgeRight,
                nudgeUp,
                nudgeDown,
                reset,
                CreateControlBlockingCallback(canvasTransform));
            overlay.transform.SetAsLastSibling();
        }

        private static DecorationRoomPresenter FindMilkroomDecorationPresenter(Transform canvasTransform)
        {
            var targetCanvas = canvasTransform.GetComponent<Canvas>()
                ?? canvasTransform.GetComponentInParent<Canvas>();
            DecorationRoomPresenter unscopedCandidate = null;
            var candidates = Object.FindObjectsByType<DecorationRoomPresenter>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (var index = 0; index < candidates.Length; index += 1)
            {
                var candidate = candidates[index];
                if (candidate == null
                    || !string.Equals(
                        candidate.gameObject.name,
                        "Milkroom Background",
                        System.StringComparison.Ordinal))
                {
                    continue;
                }

                var candidateCanvas = candidate.GetComponentInParent<Canvas>();
                if (candidateCanvas == targetCanvas)
                {
                    return candidate;
                }

                if (candidateCanvas == null
                    && (unscopedCandidate == null
                        || candidate.GetInstanceID() > unscopedCandidate.GetInstanceID()))
                {
                    unscopedCandidate = candidate;
                }
            }

            return unscopedCandidate;
        }

        private static void EnsureMilkroomViewportNavigation(Transform canvasTransform)
        {
            if (canvasTransform == null)
            {
                return;
            }

            var camera = Camera.main ?? Object.FindFirstObjectByType<Camera>();
            if (camera == null)
            {
                return;
            }

            if (camera.GetComponent<MilkroomCameraFramer>() == null)
            {
                camera.gameObject.AddComponent<MilkroomCameraFramer>();
            }
            var navigation = camera.GetComponent<MilkroomViewportNavigationController>()
                ?? camera.gameObject.AddComponent<MilkroomViewportNavigationController>();

            var legacyControls = canvasTransform.Find("Milkroom Viewport Controls");
            RemoveChildIfExists(legacyControls, "Viewport Zoom Out Button");
            RemoveChildIfExists(legacyControls, "Viewport Zoom In Button");
            RemoveChildIfExists(legacyControls, "Viewport Fit Button");
            var utilityBar = GetOrCreateMilkroomUtilityBar(canvasTransform);
            RemoveChildIfExists(utilityBar, "Viewport Zoom Out Button");
            RemoveChildIfExists(utilityBar, "Viewport Zoom In Button");
            RemoveChildIfExists(utilityBar, "Viewport Fit Button");
            RemoveChildIfExists(canvasTransform, "Milkroom Viewport Controls");
            ReflowMilkroomUtilityButtons(utilityBar);
            var settingsZoomOut = canvasTransform.Find(
                    "Settings Modal/Settings Scroll View/Viewport/Content/Viewport Zoom Out Settings Button")
                ?.GetComponent<Button>();
            var settingsZoomIn = canvasTransform.Find(
                    "Settings Modal/Settings Scroll View/Viewport/Content/Viewport Zoom In Settings Button")
                ?.GetComponent<Button>();
            var settingsFit = canvasTransform.Find(
                    "Settings Modal/Settings Scroll View/Viewport/Content/Viewport Fit Settings Button")
                ?.GetComponent<Button>();
            navigation.Configure(
                null,
                null,
                null,
                null,
                () => !IsMilkroomPropInteractionBlocked(canvasTransform),
                settingsFit,
                settingsZoomIn,
                settingsZoomOut);
        }

        private static void RemoveNormalEvolutionVisualAccents(
            CheeseTamaVisualController visualController)
        {
            if (visualController == null)
            {
                return;
            }

            // Normal-evolution accents were previously drawn as primitive ribbons,
            // drops, and spots in front of the character. In particular, the
            // Mozzarella profile produced pale blue and white shapes over the face.
            // The authored growth model already carries the intended facial detail,
            // so remove both live components and any orphaned generated roots.
            foreach (var bridge in visualController.GetComponents<NormalEvolutionVisualBridge>())
            {
                bridge.enabled = false;
                DestroyObjectSafely(bridge);
            }

            foreach (var presenter in visualController.GetComponents<NormalEvolutionVisualPresenter>())
            {
                if (presenter.GeneratedRoot != null)
                {
                    presenter.GeneratedRoot.gameObject.SetActive(false);
                }

                presenter.enabled = false;
                presenter.Release();
                DestroyObjectSafely(presenter);
            }

            var modelRoot = visualController.ModelInstance;
            if (modelRoot == null)
            {
                return;
            }

            while (true)
            {
                var generatedRoot = modelRoot.Find(NormalEvolutionVisualPresenter.GeneratedRootName);
                if (generatedRoot == null)
                {
                    break;
                }

                generatedRoot.gameObject.SetActive(false);
                generatedRoot.SetParent(null, true);
                DestroyObjectSafely(generatedRoot.gameObject);
            }
        }

        private static void EnsureAutonomousLife(
            Transform canvasTransform,
            CheeseTamaVisualController visualController)
        {
            if (canvasTransform == null || visualController == null)
            {
                return;
            }

            var visualRoot = visualController.transform;
            var motionObject = GameObject.Find("CheeseTama Autonomous Motion Root");
            if (motionObject == null)
            {
                motionObject = new GameObject("CheeseTama Autonomous Motion Root");
                motionObject.transform.position = CheeseTamaRestingWorldPosition;
                motionObject.transform.rotation = Quaternion.identity;
                motionObject.transform.localScale = Vector3.one;
                motionObject.transform.SetParent(visualRoot.parent, true);
            }

            if (visualRoot.parent != motionObject.transform)
            {
                visualRoot.SetParent(motionObject.transform, true);
            }

            motionObject.transform.position = CheeseTamaRestingWorldPosition;
            visualController.SetRestingWorldPosition(CheeseTamaRestingWorldPosition);

            var sceneRoot = GameObject.Find("MilkroomSceneRoot");
            if (sceneRoot == null)
            {
                sceneRoot = new GameObject("MilkroomSceneRoot");
            }

            var environment = GetOrCreateSceneGroup(sceneRoot.transform, "Environment");
            var anchorRoot = GetOrCreateSceneGroup(environment, "Autonomous Life Anchors");
            var restingY = CheeseTamaRestingWorldPosition.y;
            var idle = EnsureAutonomousLifeAnchor(anchorRoot, "Idle Anchor", new Vector3(0f, restingY, 0.08f));
            var nap = EnsureAutonomousLifeAnchor(anchorRoot, "Nap Anchor", new Vector3(-2.05f, restingY, 0.25f));
            var window = EnsureAutonomousLifeAnchor(anchorRoot, "Window Anchor", new Vector3(-0.85f, restingY, 1.2f));
            var shelf = EnsureAutonomousLifeAnchor(anchorRoot, "Shelf Anchor", new Vector3(1.9f, restingY, 1.15f));
            var play = EnsureAutonomousLifeAnchor(anchorRoot, "Play Anchor", new Vector3(-1.15f, restingY, 0.15f));
            var dance = EnsureAutonomousLifeAnchor(anchorRoot, "Dance Anchor", new Vector3(1.15f, restingY, 0.15f));

            var presenter = motionObject.GetComponent<AutonomousLifePresenter>();
            if (presenter == null)
            {
                presenter = motionObject.AddComponent<AutonomousLifePresenter>();
            }

            var bridge = motionObject.GetComponent<AutonomousLifeBridge>();
            if (bridge == null)
            {
                bridge = motionObject.AddComponent<AutonomousLifeBridge>();
            }

            bridge.Configure(
                presenter,
                motionObject.transform,
                new AutonomousLifeAnchorBindings(idle, nap, window, shelf, play, dance),
                EnsureCoreSystems(),
                visualController,
                canvasTransform.GetComponent<CheeseTamaDialogueBridge>());
            presenter.ConfigureMovementObstacleProvider(
                CollectAutonomousLifeObstacleBounds,
                ResolveAutonomousLifeClearanceRadius(visualController));

            var toastBridge = canvasTransform.GetComponent<AutonomousLifeDiscoveryToastBridge>();
            if (toastBridge == null)
            {
                toastBridge = canvasTransform.gameObject.AddComponent<AutonomousLifeDiscoveryToastBridge>();
            }

            toastBridge.Configure(
                bridge,
                canvasTransform.GetComponentInChildren<MilkroomUIController>(true));
        }

        private static List<Bounds> CollectAutonomousLifeObstacleBounds()
        {
            var result = new List<Bounds>(7);
            var roomRoot = GameObject.Find("Milkroom Background")?.transform;
            if (roomRoot == null)
            {
                return result;
            }

            AddCombinedRendererBounds(roomRoot.Find("Fridge_Model"), result);
            AddCombinedRendererBounds(roomRoot.Find("MilkShelf_Model"), result);
            AddCombinedRendererBounds(roomRoot.Find("CozyChair_Model"), result);
            AddCombinedRendererBounds(roomRoot.Find("DresserTable_Model"), result);

            // Older authored scenes use primitive groups instead of generated prop roots.
            if (result.Count == 0)
            {
                AddCombinedRendererBounds(roomRoot.Find("FridgeSet"), result);
                AddCombinedRendererBounds(roomRoot.Find("CozyChair"), result);
            }

            var decorationPresenter = roomRoot.GetComponent<DecorationRoomPresenter>();
            if (decorationPresenter != null)
            {
                AddCombinedRendererBounds(
                    decorationPresenter.GetMovableVisual(DecorationSlot.Accent),
                    result);
                AddCombinedRendererBounds(
                    decorationPresenter.GetMovableVisual(DecorationSlot.Shelf),
                    result);
                AddCombinedRendererBounds(
                    decorationPresenter.GetMovableVisual(DecorationSlot.Bedside),
                    result);
            }

            return result;
        }

        private static void AddCombinedRendererBounds(
            Transform root,
            ICollection<Bounds> output)
        {
            if (root == null || output == null || !root.gameObject.activeInHierarchy)
            {
                return;
            }

            var renderers = root.GetComponentsInChildren<Renderer>(false);
            var found = false;
            var combined = default(Bounds);
            foreach (var renderer in renderers)
            {
                if (renderer == null || !renderer.enabled)
                {
                    continue;
                }

                if (!found)
                {
                    combined = renderer.bounds;
                    found = true;
                }
                else
                {
                    combined.Encapsulate(renderer.bounds);
                }
            }

            if (found)
            {
                output.Add(combined);
            }
        }

        private static float ResolveAutonomousLifeClearanceRadius(
            CheeseTamaVisualController visualController)
        {
            const float minimumClearance = 0.58f;
            const float animationAllowance = 0.18f;
            const float maximumClearance = 0.92f;
            var modelRoot = visualController?.ModelInstance ?? visualController?.transform;
            if (modelRoot == null)
            {
                return minimumClearance;
            }

            var renderers = modelRoot.GetComponentsInChildren<Renderer>(false);
            var found = false;
            var combined = default(Bounds);
            foreach (var renderer in renderers)
            {
                if (renderer == null || !renderer.enabled)
                {
                    continue;
                }

                if (!found)
                {
                    combined = renderer.bounds;
                    found = true;
                }
                else
                {
                    combined.Encapsulate(renderer.bounds);
                }
            }

            if (!found)
            {
                return minimumClearance;
            }

            return Mathf.Clamp(
                Mathf.Max(combined.extents.x, combined.extents.z) + animationAllowance,
                minimumClearance,
                maximumClearance);
        }

        private static Transform EnsureAutonomousLifeAnchor(
            Transform parent,
            string name,
            Vector3 worldPosition)
        {
            var anchor = parent.Find(name);
            if (anchor == null)
            {
                anchor = new GameObject(name).transform;
                anchor.SetParent(parent, false);
            }

            anchor.position = worldPosition;
            anchor.rotation = Quaternion.identity;
            anchor.localScale = Vector3.one;
            return anchor;
        }

        private static void EnsureMilkroomAtmosphere(Transform canvasTransform, CheeseTama.Gameplay.CheeseTamaModel tama)
        {
            if (canvasTransform == null)
            {
                return;
            }

            var overlayTransform = canvasTransform.Find("Milkroom Atmosphere Overlay");
            GameObject overlayObject;
            if (overlayTransform == null)
            {
                overlayObject = new GameObject(
                    "Milkroom Atmosphere Overlay",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                overlayTransform = overlayObject.transform;
                overlayTransform.SetParent(canvasTransform, false);
            }
            else
            {
                overlayObject = overlayTransform.gameObject;
            }

            var rect = overlayObject.GetComponent<RectTransform>();
            if (rect == null)
            {
                overlayObject.name = "Milkroom Atmosphere Overlay (Legacy)";
                DestroyObjectSafely(overlayObject);
                overlayObject = new GameObject(
                    "Milkroom Atmosphere Overlay",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                overlayTransform = overlayObject.transform;
                overlayTransform.SetParent(canvasTransform, false);
                rect = overlayObject.GetComponent<RectTransform>();
            }

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.SetAsFirstSibling();

            var overlay = overlayObject.GetComponent<Image>();
            if (overlay == null)
            {
                overlay = overlayObject.AddComponent<Image>();
            }

            overlay.raycastTarget = false;
            var controller = overlayObject.GetComponent<MilkroomAtmosphereLayerController>();
            if (controller == null)
            {
                controller = overlayObject.AddComponent<MilkroomAtmosphereLayerController>();
            }

            // The authored theme owns all room lights. The atmosphere layer therefore
            // uses only a subtle, non-interactive tint and never stacks another light.
            controller.Configure(overlay, null);
            controller.Bind(tama);

            var lightObject = GameObject.Find("Milkroom Atmosphere Light");
            if (lightObject != null)
            {
                DestroyObjectSafely(lightObject);
            }
        }

        private static void EnsureMilkroomSeasonalLayer(
            Transform canvasTransform,
            GameManager manager)
        {
            if (canvasTransform == null)
            {
                return;
            }

            const string LayerName = "Milkroom Seasonal Layer";
            var layerTransform = canvasTransform.Find(LayerName);
            GameObject layerObject;
            if (layerTransform == null)
            {
                layerObject = new GameObject(
                    LayerName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                layerTransform = layerObject.transform;
                layerTransform.SetParent(canvasTransform, false);
            }
            else
            {
                layerObject = layerTransform.gameObject;
            }

            var rect = layerObject.GetComponent<RectTransform>();
            if (rect == null)
            {
                layerObject.name = LayerName + " (Legacy)";
                DestroyObjectSafely(layerObject);
                layerObject = new GameObject(
                    LayerName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                layerTransform = layerObject.transform;
                layerTransform.SetParent(canvasTransform, false);
                rect = layerObject.GetComponent<RectTransform>();
            }

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            var atmosphereLayer = canvasTransform.Find("Milkroom Atmosphere Overlay");
            rect.SetSiblingIndex(atmosphereLayer != null ? 1 : 0);

            var overlay = layerObject.GetComponent<Image>();
            if (overlay == null)
            {
                overlay = layerObject.AddComponent<Image>();
            }

            overlay.raycastTarget = false;
            var controller = layerObject.GetComponent<MilkroomSeasonalLayerController>();
            if (controller == null)
            {
                controller = layerObject.AddComponent<MilkroomSeasonalLayerController>();
            }

            controller.Configure(overlay, manager);
        }

        private static CheeseTamaPetInteractionController EnsureCheeseTamaPetInteraction(
            Transform canvasTransform,
            MilkroomUIController milkroomUi,
            CheeseTamaVisualController visualController)
        {
            if (canvasTransform == null || visualController == null)
            {
                return null;
            }

            var target = visualController.gameObject;
            var interactionCollider = target.GetComponent<BoxCollider>();
            if (interactionCollider == null)
            {
                interactionCollider = target.AddComponent<BoxCollider>();
            }

            interactionCollider.isTrigger = true;
            var petController = target.GetComponent<CheeseTamaPetInteractionController>();
            if (petController == null)
            {
                petController = target.AddComponent<CheeseTamaPetInteractionController>();
            }

            petController.Configure(milkroomUi, visualController, canvasTransform);
            return petController;
        }
    }
}
