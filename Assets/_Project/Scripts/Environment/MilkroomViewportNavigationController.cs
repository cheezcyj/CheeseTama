using System;
using System.Collections.Generic;
using CheeseTama.Gameplay.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CheeseTama.Environment
{
    /// <summary>
    /// Mouse, keyboard and two-finger adapter for milkroom viewport navigation. Gestures begin
    /// only on blank room space; modal focus scopes, ScrollRects, Selectables and active care
    /// gestures retain input authority.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera), typeof(MilkroomCameraFramer))]
    public sealed class MilkroomViewportNavigationController : MonoBehaviour
    {
        public const string ZoomInLabel = "확대";
        public const string ZoomOutLabel = "축소";
        public const string FitLabel = "맞춤";

        [SerializeField] private RectTransform blankViewportArea;
        [SerializeField] private Button zoomInButton;
        [SerializeField] private Button zoomOutButton;
        [SerializeField] private Button fitButton;
        [SerializeField] private Button settingsZoomInButton;
        [SerializeField] private Button settingsZoomOutButton;
        [SerializeField] private Button settingsFitButton;
        [SerializeField] private GameObject controlsRoot;
        [SerializeField] private float minimumFieldOfView =
            MilkroomViewportNavigationModel.DefaultMinimumFieldOfView;
        [SerializeField] private float contentPlaneZ = 0.08f;
        [SerializeField] private float wheelFieldOfViewStep = 1.5f;
        [SerializeField] private float buttonFieldOfViewStep = 2f;
        [SerializeField] private float pinchFieldOfViewRange = 18f;
        [SerializeField] private float keyboardPanViewportStep = 0.055f;

        private readonly List<RaycastResult> raycastResults = new List<RaycastResult>(16);
        private Camera targetCamera;
        private MilkroomCameraFramer cameraFramer;
        private MilkroomViewportNavigationModel model;
        private DirectManipulationInputTarget[] directManipulationTargets =
            Array.Empty<DirectManipulationInputTarget>();
        private Func<bool> interactionAllowed;
        private bool controlsBound;
        private bool touchGestureActive;
        private float previousTouchDistance;
        private Vector2 previousTouchMidpoint;
        private bool mousePanActive;
        private Vector2 previousMousePosition;

        public float CurrentFieldOfView => model?.FieldOfView
            ?? (targetCamera != null ? targetCamera.fieldOfView : 0f);
        public float CurrentZoomScale => model == null
            ? 1f
            : Mathf.Max(1f, model.FitFieldOfView / Mathf.Max(0.01f, model.FieldOfView));
        public Vector2 FocusOffset => model?.FocusOffset ?? Vector2.zero;
        public bool IsAtFit => model == null || model.IsAtFit;
        public bool IsTouchGestureActive => touchGestureActive;

        private void Awake()
        {
            ResolveReferences();
            InitializeModel();
            RefreshInteractionTargets();
        }

        private void OnEnable()
        {
            ResolveReferences();
            InitializeModel();
            BindControlListeners();
            ApplyModel();
        }

        private void OnDisable()
        {
            CancelPointerGestures();
            UnbindControlListeners();
        }

        private void OnDestroy()
        {
            UnbindControlListeners();
            if (controlsRoot != null)
            {
                controlsRoot.SetActive(true);
            }
            interactionAllowed = null;
            raycastResults.Clear();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                CancelPointerGestures();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                CancelPointerGestures();
            }
        }

        /// <summary>
        /// Builder integration entry point. The viewport may be null to use the whole screen;
        /// interactive UI is still excluded by EventSystem raycasts. Repeated calls are safe.
        /// </summary>
        public void Configure(
            RectTransform viewportArea,
            Button configuredZoomInButton,
            Button configuredZoomOutButton,
            Button configuredFitButton,
            Func<bool> isInteractionAllowed = null,
            Button configuredSettingsFitButton = null,
            Button configuredSettingsZoomInButton = null,
            Button configuredSettingsZoomOutButton = null)
        {
            UnbindControlListeners();
            blankViewportArea = viewportArea;
            zoomInButton = configuredZoomInButton;
            zoomOutButton = configuredZoomOutButton;
            fitButton = configuredFitButton;
            settingsFitButton = configuredSettingsFitButton;
            settingsZoomInButton = configuredSettingsZoomInButton;
            settingsZoomOutButton = configuredSettingsZoomOutButton;
            controlsRoot = ResolveControlsRoot(
                configuredZoomInButton,
                configuredZoomOutButton,
                configuredFitButton);
            interactionAllowed = isInteractionAllowed;
            ResolveReferences();
            InitializeModel();
            RefreshInteractionTargets();
            if (isActiveAndEnabled)
            {
                BindControlListeners();
            }

            ApplyModel();
            RefreshControlVisibility(CanUseNavigation());
        }

        public void RefreshInteractionTargets()
        {
            directManipulationTargets = UnityEngine.Object.FindObjectsByType<
                DirectManipulationInputTarget>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
        }

        public void ZoomIn()
        {
            TryZoom(buttonFieldOfViewStep, Vector2.zero, false);
        }

        public void ZoomOut()
        {
            TryZoom(-buttonFieldOfViewStep, Vector2.zero, false);
        }

        public void Fit()
        {
            TryFit(ignoreModalBlocking: false);
        }

        /// <summary>
        /// Settings owns the only visible Fit entry. It intentionally bypasses the modal input
        /// block that protects room pan/zoom while Settings itself is open.
        /// </summary>
        public void FitFromSettings()
        {
            TryFit(ignoreModalBlocking: true);
        }

        /// <summary>
        /// Settings owns the visible zoom controls. These entries intentionally bypass the
        /// modal block that protects direct room gestures while Settings itself is open.
        /// </summary>
        public void ZoomInFromSettings()
        {
            TryZoomFromSettings(buttonFieldOfViewStep, settingsZoomInButton);
        }

        public void ZoomOutFromSettings()
        {
            TryZoomFromSettings(-buttonFieldOfViewStep, settingsZoomOutButton);
        }

        private void TryZoomFromSettings(float delta, Button settingsEntry)
        {
            if (!isActiveAndEnabled
                || targetCamera == null
                || cameraFramer == null
                || settingsEntry == null
                || !settingsEntry.gameObject.activeInHierarchy
                || model == null
                || !model.ZoomBy(
                    delta,
                    Vector2.zero,
                    ResolveAspect(),
                    ResolveContentPlaneDistance(),
                    ResolveSafeBottomFraction()))
            {
                return;
            }

            ApplyModel();
        }

        private void TryFit(bool ignoreModalBlocking)
        {
            var settingsEntryAvailable = settingsFitButton != null
                && settingsFitButton.gameObject.activeInHierarchy;
            var allowed = ignoreModalBlocking
                ? isActiveAndEnabled
                    && targetCamera != null
                    && cameraFramer != null
                    && settingsEntryAvailable
                : CanUseNavigation();
            if (!allowed || model == null || !model.Fit())
            {
                return;
            }

            ApplyModel();
        }

        private void Update()
        {
            if (model == null)
            {
                ResolveReferences();
                InitializeModel();
                if (model == null)
                {
                    return;
                }
            }

            var navigationAvailable = CanUseNavigation();
            RefreshControlVisibility(navigationAvailable);
            if (!navigationAvailable)
            {
                CancelPointerGestures();
                return;
            }

            ReframeForCurrentScreen();
            HandleKeyboard();
            HandleTouch();
            if (UnityEngine.Input.touchCount > 0)
            {
                mousePanActive = false;
                return;
            }

            HandleMouse();
        }

        private void HandleKeyboard()
        {
            if (IsTextEntryFocused())
            {
                return;
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.Equals)
                || UnityEngine.Input.GetKeyDown(KeyCode.KeypadPlus))
            {
                TryZoom(buttonFieldOfViewStep, Vector2.zero, false);
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.Minus)
                || UnityEngine.Input.GetKeyDown(KeyCode.KeypadMinus))
            {
                TryZoom(-buttonFieldOfViewStep, Vector2.zero, false);
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.Home))
            {
                Fit();
            }

            var pan = Vector2.zero;
            if (IsSelectableFocused())
            {
                return;
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow))
            {
                pan.x = keyboardPanViewportStep;
            }
            else if (UnityEngine.Input.GetKeyDown(KeyCode.RightArrow))
            {
                pan.x = -keyboardPanViewportStep;
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.DownArrow))
            {
                pan.y = keyboardPanViewportStep;
            }
            else if (UnityEngine.Input.GetKeyDown(KeyCode.UpArrow))
            {
                pan.y = -keyboardPanViewportStep;
            }

            if (pan != Vector2.zero
                && model.PanByViewportDelta(
                    pan,
                    ResolveAspect(),
                    ResolveContentPlaneDistance(),
                    ResolveSafeBottomFraction()))
            {
                ApplyModel();
            }
        }

        private void HandleTouch()
        {
            if (UnityEngine.Input.touchCount != 2)
            {
                touchGestureActive = false;
                return;
            }

            var first = UnityEngine.Input.GetTouch(0);
            var second = UnityEngine.Input.GetTouch(1);
            if (first.phase == TouchPhase.Canceled
                || first.phase == TouchPhase.Ended
                || second.phase == TouchPhase.Canceled
                || second.phase == TouchPhase.Ended)
            {
                touchGestureActive = false;
                return;
            }

            var midpoint = (first.position + second.position) * 0.5f;
            var distance = Vector2.Distance(first.position, second.position);
            if (!touchGestureActive)
            {
                RefreshInteractionTargets();
                if (!CanBeginAt(first.position, first.fingerId)
                    || !CanBeginAt(second.position, second.fingerId))
                {
                    return;
                }

                previousTouchMidpoint = midpoint;
                previousTouchDistance = distance;
                touchGestureActive = true;
                return;
            }

            if (!CanContinueAt(first.position, first.fingerId)
                || !CanContinueAt(second.position, second.fingerId))
            {
                touchGestureActive = false;
                return;
            }

            var shortSide = Mathf.Max(1f, Mathf.Min(Screen.width, Screen.height));
            var zoomDelta = (distance - previousTouchDistance) / shortSide
                * pinchFieldOfViewRange;
            var changed = false;
            if (!Mathf.Approximately(zoomDelta, 0f))
            {
                changed |= model.ZoomBy(
                    zoomDelta,
                    ScreenPointToViewportAnchor(midpoint),
                    ResolveAspect(),
                    ResolveContentPlaneDistance(),
                    ResolveSafeBottomFraction());
            }

            var panDelta = ScreenDeltaToNormalized(midpoint - previousTouchMidpoint);
            changed |= model.PanByViewportDelta(
                panDelta,
                ResolveAspect(),
                ResolveContentPlaneDistance(),
                ResolveSafeBottomFraction());

            previousTouchMidpoint = midpoint;
            previousTouchDistance = distance;
            if (changed)
            {
                ApplyModel();
            }
        }

        private void HandleMouse()
        {
            var mousePosition = (Vector2)UnityEngine.Input.mousePosition;
            var scroll = UnityEngine.Input.mouseScrollDelta.y;
            if (!Mathf.Approximately(scroll, 0f) && CanBeginAt(mousePosition, -1))
            {
                TryZoom(
                    scroll * wheelFieldOfViewStep,
                    ScreenPointToViewportAnchor(mousePosition),
                    true);
            }

            if (UnityEngine.Input.GetMouseButtonDown(2))
            {
                RefreshInteractionTargets();
                mousePanActive = CanBeginAt(mousePosition, -1);
                previousMousePosition = mousePosition;
            }

            if (!mousePanActive)
            {
                return;
            }

            if (!UnityEngine.Input.GetMouseButton(2) || !CanContinueAt(mousePosition, -1))
            {
                mousePanActive = false;
                return;
            }

            var panDelta = ScreenDeltaToNormalized(mousePosition - previousMousePosition);
            previousMousePosition = mousePosition;
            if (model.PanByViewportDelta(
                panDelta,
                ResolveAspect(),
                ResolveContentPlaneDistance(),
                ResolveSafeBottomFraction()))
            {
                ApplyModel();
            }
        }

        private void TryZoom(float delta, Vector2 anchor, bool requirePointerPolicy)
        {
            if (!CanUseNavigation()
                || model == null
                || (requirePointerPolicy
                    && !CanBeginAt((Vector2)UnityEngine.Input.mousePosition, -1))
                || !model.ZoomBy(
                    delta,
                    anchor,
                    ResolveAspect(),
                    ResolveContentPlaneDistance(),
                    ResolveSafeBottomFraction()))
            {
                return;
            }

            ApplyModel();
        }

        private bool CanBeginAt(Vector2 screenPosition, int pointerId)
        {
            var modalActive = !KeyboardFocusScope.IsInteractionAllowed(gameObject);
            return MilkroomViewportInputPolicy.CanBegin(
                isActiveAndEnabled && (interactionAllowed == null || interactionAllowed.Invoke()),
                IsInsideViewport(screenPosition),
                IsPointerOverInteractiveUi(screenPosition, pointerId),
                HasActiveDirectManipulation(),
                modalActive,
                GameInputRouter.GameplayInputSuppressed);
        }

        private bool CanContinueAt(Vector2 screenPosition, int pointerId)
        {
            return CanUseNavigation()
                && IsInsideViewport(screenPosition)
                && !IsPointerOverInteractiveUi(screenPosition, pointerId)
                && !HasActiveDirectManipulation();
        }

        private bool CanUseNavigation()
        {
            return isActiveAndEnabled
                && targetCamera != null
                && cameraFramer != null
                && !GameInputRouter.GameplayInputSuppressed
                && KeyboardFocusScope.IsInteractionAllowed(gameObject)
                && (interactionAllowed == null || interactionAllowed.Invoke());
        }

        private bool IsInsideViewport(Vector2 screenPosition)
        {
            return blankViewportArea == null
                || (blankViewportArea.gameObject.activeInHierarchy
                    && RectTransformUtility.RectangleContainsScreenPoint(
                        blankViewportArea,
                        screenPosition,
                        ResolveUiCamera(blankViewportArea)));
        }

        private bool IsPointerOverInteractiveUi(Vector2 screenPosition, int pointerId)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return false;
            }

            var eventData = new PointerEventData(eventSystem)
            {
                position = screenPosition,
                pointerId = pointerId
            };
            raycastResults.Clear();
            eventSystem.RaycastAll(eventData, raycastResults);
            for (var index = 0; index < raycastResults.Count; index += 1)
            {
                var candidate = raycastResults[index].gameObject;
                if (candidate == null)
                {
                    continue;
                }

                if (candidate.GetComponentInParent<Selectable>() != null
                    || candidate.GetComponentInParent<ScrollRect>() != null
                    || candidate.GetComponentInParent<DirectManipulationInputTarget>() != null
                    || IsInsideNamedModal(candidate.transform))
                {
                    raycastResults.Clear();
                    return true;
                }
            }

            raycastResults.Clear();
            return false;
        }

        private bool HasActiveDirectManipulation()
        {
            for (var index = 0; index < directManipulationTargets.Length; index += 1)
            {
                var target = directManipulationTargets[index];
                if (target != null && target.IsGestureActive)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsInsideNamedModal(Transform candidate)
        {
            for (var current = candidate; current != null; current = current.parent)
            {
                if (!current.gameObject.activeInHierarchy)
                {
                    continue;
                }

                var objectName = current.name;
                if (!string.IsNullOrEmpty(objectName)
                    && (objectName.EndsWith(" Overlay", StringComparison.Ordinal)
                        || objectName.EndsWith(" Modal", StringComparison.Ordinal)))
                {
                    return true;
                }

                if (current.GetComponent<Canvas>() != null)
                {
                    break;
                }
            }

            return false;
        }

        private void ResolveReferences()
        {
            targetCamera ??= GetComponent<Camera>();
            cameraFramer ??= GetComponent<MilkroomCameraFramer>();
        }

        private void InitializeModel()
        {
            if (model != null || targetCamera == null)
            {
                return;
            }

            model = new MilkroomViewportNavigationModel(
                targetCamera.fieldOfView,
                minimumFieldOfView);
        }

        private void ReframeForCurrentScreen()
        {
            if (model.Reframe(
                ResolveAspect(),
                ResolveContentPlaneDistance(),
                ResolveSafeBottomFraction()))
            {
                ApplyModel();
            }
        }

        private void ApplyModel()
        {
            if (model == null || targetCamera == null || cameraFramer == null)
            {
                return;
            }

            targetCamera.fieldOfView = model.FieldOfView;
            cameraFramer.SetNavigationOffset(model.FocusOffset);
            RefreshControlState();
        }

        private float ResolveAspect()
        {
            if (targetCamera != null && targetCamera.aspect > 0f)
            {
                return targetCamera.aspect;
            }

            return Screen.height > 0 ? (float)Screen.width / Screen.height : 1f;
        }

        private float ResolveContentPlaneDistance()
        {
            return targetCamera != null
                ? Mathf.Abs(contentPlaneZ - targetCamera.transform.position.z)
                : 1f;
        }

        private float ResolveSafeBottomFraction()
        {
            return cameraFramer != null ? cameraFramer.CurrentSafeBottomFraction : 0f;
        }

        private Vector2 ScreenPointToViewportAnchor(Vector2 screenPosition)
        {
            var width = Mathf.Max(1f, Screen.width);
            var height = Mathf.Max(1f, Screen.height);
            return new Vector2(
                Mathf.Clamp(screenPosition.x / width * 2f - 1f, -1f, 1f),
                Mathf.Clamp(screenPosition.y / height * 2f - 1f, -1f, 1f));
        }

        private static Vector2 ScreenDeltaToNormalized(Vector2 screenDelta)
        {
            return new Vector2(
                screenDelta.x / Mathf.Max(1f, Screen.width),
                screenDelta.y / Mathf.Max(1f, Screen.height));
        }

        private static Camera ResolveUiCamera(RectTransform area)
        {
            var canvas = area != null ? area.GetComponentInParent<Canvas>() : null;
            return canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
        }

        private static bool IsTextEntryFocused()
        {
            var selected = EventSystem.current != null
                ? EventSystem.current.currentSelectedGameObject
                : null;
            if (selected == null)
            {
                return false;
            }

            if (selected.GetComponentInParent<InputField>() != null)
            {
                return true;
            }

            // Avoid a package dependency while still respecting a selected TMP_InputField.
            var behaviours = selected.GetComponentsInParent<MonoBehaviour>(true);
            for (var index = 0; index < behaviours.Length; index += 1)
            {
                if (behaviours[index] != null
                    && string.Equals(
                        behaviours[index].GetType().Name,
                        "TMP_InputField",
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsSelectableFocused()
        {
            var selected = EventSystem.current != null
                ? EventSystem.current.currentSelectedGameObject
                : null;
            return selected != null
                && selected.GetComponentInParent<Selectable>() != null;
        }

        private void BindControlListeners()
        {
            if (controlsBound)
            {
                return;
            }

            zoomInButton?.onClick.AddListener(ZoomIn);
            zoomOutButton?.onClick.AddListener(ZoomOut);
            fitButton?.onClick.AddListener(Fit);
            settingsZoomInButton?.onClick.AddListener(ZoomInFromSettings);
            settingsZoomOutButton?.onClick.AddListener(ZoomOutFromSettings);
            settingsFitButton?.onClick.AddListener(FitFromSettings);
            controlsBound = true;
            RefreshControlState();
        }

        private void UnbindControlListeners()
        {
            if (!controlsBound)
            {
                return;
            }

            zoomInButton?.onClick.RemoveListener(ZoomIn);
            zoomOutButton?.onClick.RemoveListener(ZoomOut);
            fitButton?.onClick.RemoveListener(Fit);
            settingsZoomInButton?.onClick.RemoveListener(ZoomInFromSettings);
            settingsZoomOutButton?.onClick.RemoveListener(ZoomOutFromSettings);
            settingsFitButton?.onClick.RemoveListener(FitFromSettings);
            controlsBound = false;
        }

        private void RefreshControlState()
        {
            if (model == null)
            {
                return;
            }

            if (zoomInButton != null)
            {
                zoomInButton.interactable = model.FieldOfView > model.MinimumFieldOfView + 0.0001f;
            }

            if (zoomOutButton != null)
            {
                zoomOutButton.interactable = model.FieldOfView < model.FitFieldOfView - 0.0001f;
            }

            if (fitButton != null)
            {
                fitButton.interactable = !model.IsAtFit;
            }

            if (settingsFitButton != null)
            {
                settingsFitButton.interactable = !model.IsAtFit;
            }

            if (settingsZoomInButton != null)
            {
                settingsZoomInButton.interactable =
                    model.FieldOfView > model.MinimumFieldOfView + 0.0001f;
            }

            if (settingsZoomOutButton != null)
            {
                settingsZoomOutButton.interactable =
                    model.FieldOfView < model.FitFieldOfView - 0.0001f;
            }
        }

        private void RefreshControlVisibility(bool navigationAvailable)
        {
            if (controlsRoot != null && controlsRoot.activeSelf != navigationAvailable)
            {
                controlsRoot.SetActive(navigationAvailable);
            }
        }

        private GameObject ResolveControlsRoot(
            Button configuredZoomInButton,
            Button configuredZoomOutButton,
            Button configuredFitButton)
        {
            var candidate = configuredZoomInButton != null
                ? configuredZoomInButton.transform.parent
                : configuredZoomOutButton != null
                    ? configuredZoomOutButton.transform.parent
                    : configuredFitButton != null
                        ? configuredFitButton.transform.parent
                        : null;
            if (candidate == null
                || candidate == transform
                || (configuredZoomInButton != null
                    && configuredZoomInButton.transform.parent != candidate)
                || (configuredZoomOutButton != null
                    && configuredZoomOutButton.transform.parent != candidate)
                || (configuredFitButton != null
                    && configuredFitButton.transform.parent != candidate))
            {
                return null;
            }

            return candidate.gameObject;
        }

        private void CancelPointerGestures()
        {
            touchGestureActive = false;
            mousePanActive = false;
        }
    }
}
