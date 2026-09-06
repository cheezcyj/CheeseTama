using System;
using CheeseTama.Gameplay.Input;
using CheeseTama.Gameplay.Story;
using CheeseTama.UI;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.Environment
{
    /// <summary>
    /// Owns the investigation-mode lifetime and its three Selectable hotspot controls. Unity
    /// Buttons provide the same command path for pointer clicks and keyboard submit. Domain state
    /// changes only after an explicit eligible hotspot command reaches the caller callback.
    /// </summary>
    public sealed class MilkroomInvestigationInputController : MonoBehaviour
    {
        public const string WindowHotspotId = "milkroom_window_trace";
        public const string ShelfHotspotId = "milkroom_shelf_trace";
        public const string DrawerHotspotId = "milkroom_drawer_trace";

        [SerializeField] private GameObject modeRoot;
        [SerializeField] private Button openButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button windowHotspotButton;
        [SerializeField] private Text windowHotspotLabel;
        [SerializeField] private Button shelfHotspotButton;
        [SerializeField] private Text shelfHotspotLabel;
        [SerializeField] private Button drawerHotspotButton;
        [SerializeField] private Text drawerHotspotLabel;
        [SerializeField] private MilkroomInvestigationPanelController panelController;
        [SerializeField] private KeyboardFocusScope focusScope;

        private Func<MilkroomInvestigationSnapshot> snapshotProvider;
        private Func<string, MilkroomInvestigationInspectResult> inspectCallback;
        private Func<bool> otherModalBlockingProvider;
        private Action<bool> blockingChanged;
        private bool configured;
        private bool modeActive;
        private bool applicationFocused = true;
        private bool applicationPaused;
        private bool blockingActive;

        public bool IsModeActive => modeActive && modeRoot != null && modeRoot.activeSelf;
        public bool IsBlockingGameplay => IsModeActive;

        /// <summary>
        /// isOtherModalBlocking must report only blockers outside this investigation mode. The
        /// onBlockingChanged callback is the integration hook that adds/removes this mode from the
        /// caller's authoritative modal aggregate.
        /// </summary>
        public void Configure(
            GameObject investigationRoot,
            Button openInvestigationButton,
            Button cancelInvestigationButton,
            Button windowButton,
            Text windowLabel,
            Button shelfButton,
            Text shelfLabel,
            Button drawerButton,
            Text drawerLabel,
            MilkroomInvestigationPanelController investigationPanel,
            Func<MilkroomInvestigationSnapshot> provideSnapshot,
            Func<string, MilkroomInvestigationInspectResult> inspectHotspot,
            Func<bool> isOtherModalBlocking = null,
            Action<bool> onBlockingChanged = null)
        {
            NotifyBlocking(false);
            UnbindButtons();
            modeRoot = investigationRoot;
            openButton = openInvestigationButton;
            cancelButton = cancelInvestigationButton;
            windowHotspotButton = windowButton;
            windowHotspotLabel = windowLabel;
            shelfHotspotButton = shelfButton;
            shelfHotspotLabel = shelfLabel;
            drawerHotspotButton = drawerButton;
            drawerHotspotLabel = drawerLabel;
            panelController = investigationPanel;
            snapshotProvider = provideSnapshot;
            inspectCallback = inspectHotspot;
            otherModalBlockingProvider = isOtherModalBlocking;
            blockingChanged = onBlockingChanged;
            configured = modeRoot != null
                && openButton != null
                && cancelButton != null
                && windowHotspotButton != null
                && shelfHotspotButton != null
                && drawerHotspotButton != null
                && panelController != null
                && snapshotProvider != null
                && inspectCallback != null;
            EnsureFocusScope();
            BindButtons();
            CancelMode();
        }

        public bool TryEnterMode()
        {
            if (!configured || !CanAcceptInvestigationInput())
            {
                return false;
            }

            var snapshot = snapshotProvider();
            if (snapshot == null || !snapshot.Visible)
            {
                CancelMode();
                return false;
            }

            modeActive = true;
            modeRoot.SetActive(true);
            NotifyBlocking(true);
            EnsureFocusScope();
            RefreshHotspotControls(snapshot);
            if (!panelController.ShowSnapshot(snapshot))
            {
                CancelMode();
                return false;
            }

            focusScope?.EnsureFocusWithinScope();
            return true;
        }

        /// <summary>
        /// Hides the entry point itself while the spoiler-safe snapshot is locked. The caller may
        /// invoke this after dream/save replacement events without opening investigation mode.
        /// </summary>
        public bool RefreshAvailability()
        {
            if (!configured || openButton == null || snapshotProvider == null)
            {
                if (openButton != null)
                {
                    openButton.gameObject.SetActive(false);
                }

                return false;
            }

            var snapshot = snapshotProvider();
            var available = snapshot?.Visible == true;
            openButton.gameObject.SetActive(available);
            if (!available && IsModeActive)
            {
                CancelMode();
            }

            return available;
        }

        public void CancelMode()
        {
            modeActive = false;
            panelController?.Hide();
            if (modeRoot != null)
            {
                modeRoot.SetActive(false);
            }

            NotifyBlocking(false);
            RefreshAvailability();
        }

        public bool RequestWindowHotspot()
        {
            return RequestHotspot(WindowHotspotId, windowHotspotButton);
        }

        public bool RequestShelfHotspot()
        {
            return RequestHotspot(ShelfHotspotId, shelfHotspotButton);
        }

        public bool RequestDrawerHotspot()
        {
            return RequestHotspot(DrawerHotspotId, drawerHotspotButton);
        }

        public void HandleApplicationFocus(bool hasFocus)
        {
            applicationFocused = hasFocus;
            if (!hasFocus)
            {
                CancelMode();
            }
        }

        public void HandleApplicationPause(bool paused)
        {
            applicationPaused = paused;
            if (paused)
            {
                CancelMode();
            }
        }

        private void OnEnable()
        {
            if (configured)
            {
                BindButtons();
                RefreshAvailability();
            }
        }

        private void OnDisable()
        {
            UnbindButtons();
            modeActive = false;
            panelController?.Hide();
            NotifyBlocking(false);
        }

        private void OnDestroy()
        {
            UnbindButtons();
            modeActive = false;
            NotifyBlocking(false);
            snapshotProvider = null;
            inspectCallback = null;
            otherModalBlockingProvider = null;
            blockingChanged = null;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            HandleApplicationFocus(hasFocus);
        }

        private void OnApplicationPause(bool paused)
        {
            HandleApplicationPause(paused);
        }

        private void Update()
        {
            if (!IsModeActive)
            {
                return;
            }

            if (IsOtherModalBlocking()
                || GameInputRouter.WasPressed(GameInputActionIds.Cancel))
            {
                CancelMode();
            }
        }

        private bool RequestHotspot(string hotspotId, Button sourceButton)
        {
            if (!IsModeActive
                || !CanAcceptInvestigationInput()
                || sourceButton == null
                || !sourceButton.IsInteractable()
                || !KeyboardFocusScope.IsInteractionAllowed(sourceButton.gameObject))
            {
                return false;
            }

            var before = snapshotProvider();
            var hotspot = FindHotspot(before, hotspotId);
            if (hotspot == null || !hotspot.Available)
            {
                return false;
            }

            var result = inspectCallback(hotspotId);
            if (result == null || !result.Snapshot.Visible)
            {
                CancelMode();
                return false;
            }

            RefreshHotspotControls(result.Snapshot);
            panelController.ShowInspection(result);
            return result.Applied
                || result.Status == MilkroomInvestigationInspectStatus.AlreadyDiscovered;
        }

        private void RefreshHotspotControls(MilkroomInvestigationSnapshot snapshot)
        {
            var showHotspots = snapshot != null && snapshot.Visible && !snapshot.IsRecall;
            RefreshHotspotControl(
                windowHotspotButton,
                windowHotspotLabel,
                FindHotspot(snapshot, WindowHotspotId),
                showHotspots);
            RefreshHotspotControl(
                shelfHotspotButton,
                shelfHotspotLabel,
                FindHotspot(snapshot, ShelfHotspotId),
                showHotspots);
            RefreshHotspotControl(
                drawerHotspotButton,
                drawerHotspotLabel,
                FindHotspot(snapshot, DrawerHotspotId),
                showHotspots);
        }

        private static void RefreshHotspotControl(
            Button button,
            Text label,
            MilkroomInvestigationHotspotView hotspot,
            bool visible)
        {
            if (button == null)
            {
                return;
            }

            var active = visible && hotspot != null;
            button.gameObject.SetActive(active);
            if (!active)
            {
                return;
            }

            button.interactable = hotspot.Available || hotspot.Discovered;
            if (label != null)
            {
                label.text = hotspot.Label
                    + (hotspot.Discovered
                        ? " · 기록됨"
                        : hotspot.Available ? string.Empty : " · 조건 필요");
            }
        }

        private static MilkroomInvestigationHotspotView FindHotspot(
            MilkroomInvestigationSnapshot snapshot,
            string hotspotId)
        {
            if (snapshot?.Hotspots == null)
            {
                return null;
            }

            for (var index = 0; index < snapshot.Hotspots.Count; index += 1)
            {
                var hotspot = snapshot.Hotspots[index];
                if (string.Equals(hotspot?.Id, hotspotId, StringComparison.Ordinal))
                {
                    return hotspot;
                }
            }

            return null;
        }

        private bool CanAcceptInvestigationInput()
        {
            return applicationFocused && !applicationPaused && !IsOtherModalBlocking();
        }

        private bool IsOtherModalBlocking()
        {
            return otherModalBlockingProvider?.Invoke() == true;
        }

        private void EnsureFocusScope()
        {
            if (modeRoot == null)
            {
                return;
            }

            focusScope = focusScope != null
                ? focusScope
                : modeRoot.GetComponent<KeyboardFocusScope>();
            if (focusScope == null)
            {
                focusScope = modeRoot.AddComponent<KeyboardFocusScope>();
            }

            focusScope.Configure(modeRoot.transform, true, true);
        }

        private void BindButtons()
        {
            if (!configured)
            {
                return;
            }

            openButton.onClick.RemoveListener(EnterFromButton);
            openButton.onClick.AddListener(EnterFromButton);
            cancelButton.onClick.RemoveListener(CancelMode);
            cancelButton.onClick.AddListener(CancelMode);
            windowHotspotButton.onClick.RemoveListener(RequestWindowFromButton);
            windowHotspotButton.onClick.AddListener(RequestWindowFromButton);
            shelfHotspotButton.onClick.RemoveListener(RequestShelfFromButton);
            shelfHotspotButton.onClick.AddListener(RequestShelfFromButton);
            drawerHotspotButton.onClick.RemoveListener(RequestDrawerFromButton);
            drawerHotspotButton.onClick.AddListener(RequestDrawerFromButton);
        }

        private void UnbindButtons()
        {
            openButton?.onClick.RemoveListener(EnterFromButton);
            cancelButton?.onClick.RemoveListener(CancelMode);
            windowHotspotButton?.onClick.RemoveListener(RequestWindowFromButton);
            shelfHotspotButton?.onClick.RemoveListener(RequestShelfFromButton);
            drawerHotspotButton?.onClick.RemoveListener(RequestDrawerFromButton);
        }

        private void EnterFromButton()
        {
            TryEnterMode();
        }

        private void RequestWindowFromButton()
        {
            RequestWindowHotspot();
        }

        private void RequestShelfFromButton()
        {
            RequestShelfHotspot();
        }

        private void RequestDrawerFromButton()
        {
            RequestDrawerHotspot();
        }

        private void NotifyBlocking(bool active)
        {
            if (blockingActive == active)
            {
                return;
            }

            blockingActive = active;
            blockingChanged?.Invoke(active);
        }
    }
}
